using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.PostProcessing;

public partial class CCharacterInfoStage
{
    private struct SCameraPose
    {
        public float azimuth;
        public float distance;
        public float height;
        public float lookHeight;
        public float fieldOfView;
        public float screenX;
        public float face;
    }

    private const float _SHAKE_MIN = 0.002f;

    private readonly RaycastHit[] _RAY_HITS = new RaycastHit[16];
    private readonly List<RaycastResult> _UI_HITS = new List<RaycastResult>();
    private Camera _camera;
    private PostProcessLayer _postLayer;
    private Vector3 _feet;
    private Vector3 _baseForward = Vector3.forward;
    private Vector3 _baseLeft = Vector3.left;
    private float _startYaw;
    private SCameraPose _from;
    private SCameraPose _to;
    private SCameraPose _now;
    private float _tabProgress = 1f;
    private float _drift;
    private float _holdTime;
    private float _pushNow;
    private float _spin;
    private float _turn;
    private float _turnVelocity;
    private float _introProgress = 1f;
    private float _introRatio;
    private float _shake;
    private Vector2 _mouse;
    private Vector2 _mouseTarget;
    private bool _isDragging;
    private float _dragVelocity;
    private float _lastPressTime = float.NegativeInfinity;
    private PointerEventData _pointerData;

    private bool PrepareCamera()
    {
        if (_camera != null) return true;

        GameObject cameraObject = new GameObject("CharacterInfoStageCamera");
        cameraObject.transform.SetParent(_rig, false);
        cameraObject.SetActive(false);
        _camera = cameraObject.AddComponent<Camera>();
        _camera.CopyFrom(_gameCamera);
        _camera.depth = _gameCamera.depth + _settings.Camera.depthOffset;
        _camera.targetTexture = null;

        PostProcessLayer gameLayer = _gameCamera.GetComponent<PostProcessLayer>();
        PostProcessResources resources = _settings.Assets.postProcessResources;
        if (gameLayer == null || resources == null) return true;

        _postLayer = cameraObject.AddComponent<PostProcessLayer>();
        _postLayer.Init(resources);
        _postLayer.volumeTrigger = cameraObject.transform;
        _postLayer.volumeLayer = gameLayer.volumeLayer | (1 << _settings.Camera.volumeLayer);
        _postLayer.antialiasingMode = gameLayer.antialiasingMode;
        _postLayer.subpixelMorphologicalAntialiasing = gameLayer.subpixelMorphologicalAntialiasing;
        _postLayer.fastApproximateAntialiasing = gameLayer.fastApproximateAntialiasing;
        _postLayer.stopNaNPropagation = gameLayer.stopNaNPropagation;
        _postLayer.finalBlitToCameraTarget = gameLayer.finalBlitToCameraTarget;
        return true;
    }

    private void SortCustomEffects()
    {
        if (_postLayer == null || _postLayer.sortedBundles == null) return;

        PostProcessLayer gameLayer = _gameCamera.GetComponent<PostProcessLayer>();
        string focusName = typeof(CCharacterInfoFocus).AssemblyQualifiedName;

        foreach (KeyValuePair<PostProcessEvent, List<PostProcessLayer.SerializedBundleRef>> pair
            in _postLayer.sortedBundles)
        {
            List<PostProcessLayer.SerializedBundleRef> order = null;
            if (gameLayer != null && gameLayer.sortedBundles != null)
            {
                gameLayer.sortedBundles.TryGetValue(pair.Key, out order);
            }

            pair.Value.Sort((a, b) => EffectOrder(order, a.assemblyQualifiedName, focusName)
                .CompareTo(EffectOrder(order, b.assemblyQualifiedName, focusName)));
        }
    }

    private static int EffectOrder(List<PostProcessLayer.SerializedBundleRef> order, string typeName, string last)
    {
        if (typeName == last) return int.MaxValue;
        if (order == null) return 0;

        for (int i = 0; i < order.Count; i++)
        {
            if (order[i].assemblyQualifiedName == typeName) return i;
        }

        return order.Count;
    }

    private Vector3 StageToWorld(Vector3 point)
    {
        return _feet + _baseLeft * point.x + Vector3.up * point.y + _baseForward * point.z;
    }

    private Vector3 StageDirection(float yaw)
    {
        return _baseLeft * Mathf.Sin(yaw) + _baseForward * Mathf.Cos(yaw);
    }

    private void SetBase(Vector3 forward)
    {
        _baseForward = forward;
        _baseLeft = Vector3.Cross(forward, Vector3.up);
    }

    private void PickBase()
    {
        Vector3 toCamera = _gameCamera.transform.position - _feet;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude < 1e-4f) toCamera = -_playerVisual.forward;
        Vector3 first = toCamera.normalized;

        CCharacterInfoStageSettingsSO.SOcclusion occlusion = _settings.Occlusion;
        Vector3 best = first;
        int bestScore = int.MaxValue;

        for (int i = 0; i <= occlusion.baseTries * 2; i++)
        {
            // 현재 방향에서 가까운 후보부터 검사한다: 0, +1, -1, +2, -2.
            float offset = (i + 1) / 2 * occlusion.baseStep * (i % 2 == 1 ? 1f : -1f);
            SetBase(Quaternion.AngleAxis(offset * Mathf.Rad2Deg, Vector3.up) * first);
            int score = 0;

            foreach (CCharacterInfoStageSettingsSO.SShot shot in _settings.Shots)
            {
                GetShotCamera(ToPose(shot), 0f, out Vector3 position, out Vector3 _);
                if (Physics.CheckSphere(position, occlusion.cameraRadius, occlusion.mask,
                    QueryTriggerInteraction.Ignore))
                {
                    score += 10;
                }

                for (int k = 0; k < 3; k++)
                {
                    if (IsOccluded(position, _feet + Vector3.up * occlusion.bodyHeights[k])) score += 3;
                }

                if (score >= bestScore) break;
            }

            if (score < bestScore)
            {
                bestScore = score;
                best = _baseForward;
            }

            if (score == 0) break;
        }

        SetBase(best);
        Vector3 facing = _playerVisual.forward;
        _startYaw = Mathf.Atan2(Vector3.Dot(facing, _baseLeft), Vector3.Dot(facing, _baseForward));
    }

    private bool IsOccluded(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        float distance = direction.magnitude - 0.3f;
        if (distance <= 0.05f) return false;

        int count = Physics.RaycastNonAlloc(from, direction.normalized, _RAY_HITS, distance, _settings.Occlusion.mask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Transform hit = _RAY_HITS[i].transform;
            if (hit.IsChildOf(_player.transform)) continue;
            if (_companion != null && hit.IsChildOf(_companion.transform)) continue;
            return true;
        }

        return false;
    }

    private static SCameraPose ToPose(CCharacterInfoStageSettingsSO.SShot shot)
    {
        return new SCameraPose
        {
            azimuth = shot.azimuth, distance = shot.distance, height = shot.height, lookHeight = shot.lookHeight,
            fieldOfView = shot.fieldOfView, screenX = shot.screenX, face = shot.face,
        };
    }

    private static SCameraPose LerpPose(SCameraPose a, SCameraPose b, float t)
    {
        return new SCameraPose
        {
            azimuth = Mathf.LerpUnclamped(a.azimuth, b.azimuth, t),
            distance = Mathf.LerpUnclamped(a.distance, b.distance, t),
            height = Mathf.LerpUnclamped(a.height, b.height, t),
            lookHeight = Mathf.LerpUnclamped(a.lookHeight, b.lookHeight, t),
            fieldOfView = Mathf.LerpUnclamped(a.fieldOfView, b.fieldOfView, t),
            screenX = Mathf.LerpUnclamped(a.screenX, b.screenX, t),
            face = Mathf.LerpUnclamped(a.face, b.face, t),
        };
    }

    private void ResetCamera(int tab)
    {
        _spin = 0f;
        _turn = 0f;
        _turnVelocity = 0f;
        _shake = 0f;
        _lastPressTime = float.NegativeInfinity;
        _isDragging = false;
        _mouse = Vector2.zero;
        _mouseTarget = Vector2.zero;
        SetShotTarget(tab, true);
        _introProgress = 0f;
        _introRatio = 1f;
    }

    private void SetShotTarget(int tab, bool isInstant)
    {
        CCharacterInfoStageSettingsSO.SShot shot = _settings.Shots[Mathf.Clamp(tab, 0, _settings.Shots.Length - 1)];
        SCameraPose target = ToPose(shot);
        _from = isInstant ? target : _now;
        _to = target;

        if (!isInstant)
        {
            // 현재 줌 거리를 유지하고 회전각은 최단 경로로 보간한다.
            _from.distance *= 1f - _pushNow;
            float delta = target.azimuth - _from.azimuth;
            _to.azimuth = _from.azimuth + Mathf.Atan2(Mathf.Sin(delta), Mathf.Cos(delta));
        }

        _holdTime = 0f;
        _pushNow = 0f;
        _tabProgress = isInstant ? 1f : 0f;
        _drift = shot.drift;
        if (isInstant) _now = _to;
    }

    private void SnapFace()
    {
        _from.face = 0f;
        _to.face = 0f;
        _now.face = 0f;
        _turn = 0f;
        _turnVelocity = 0f;
        _spin = 0f;
        _isDragging = false;
    }

    private void ReadPointer(float deltaTime)
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 position = mouse.position.ReadValue();
        float width = Mathf.Max(1, Screen.width);
        float height = Mathf.Max(1, Screen.height);
        _mouseTarget.x = Mathf.Clamp(position.x / width * 2f - 1f, -1f, 1f);
        _mouseTarget.y = Mathf.Clamp(1f - position.y / height * 2f, -1f, 1f);

        CCharacterInfoStageSettingsSO.SCamera settings = _settings.Camera;

        if (_tab == _HELPER_TAB)
        {
            _isDragging = false;
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame && CanStartDrag(position))
        {
            _isDragging = true;
            _dragVelocity = 0f;
            _turnVelocity = 0f;

            if (_time - _lastPressTime < settings.doubleClickTime) _turn = 0f;
            _lastPressTime = _time;
        }

        if (!_isDragging) return;

        if (mouse.leftButton.isPressed)
        {
            float delta = mouse.delta.ReadValue().x * (1080f / height) * settings.dragAngle;
            _turn += delta;
            if (deltaTime > 1e-5f) _dragVelocity = delta / Mathf.Max(0.008f, deltaTime);
            return;
        }

        _isDragging = false;
        _turnVelocity = Mathf.Clamp(_dragVelocity * settings.dragThrow, -settings.dragMaxSpeed, settings.dragMaxSpeed);
    }

    private bool CanStartDrag(Vector2 position)
    {
        float band = _settings.Camera.dragBand * Screen.height;
        if (position.y < band || position.y > Screen.height - band) return false;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return true;

        if (_pointerData == null) _pointerData = new PointerEventData(eventSystem);
        _pointerData.position = position;
        _UI_HITS.Clear();
        eventSystem.RaycastAll(_pointerData, _UI_HITS);

        for (int i = 0; i < _UI_HITS.Count; i++)
        {
            if (_UI_HITS[i].gameObject.GetComponentInParent<CCharacterInfoPointerArea>() != null) return false;
        }

        return true;
    }

    private void StepCamera(float deltaTime, float blend)
    {
        CCharacterInfoStageSettingsSO.SCamera settings = _settings.Camera;

        _tabProgress = Mathf.Min(1f, _tabProgress + deltaTime / settings.tabTime);
        float eased = _tabProgress >= 1f ? 1f : 1f - Mathf.Pow(2f, -settings.whipPower * _tabProgress);
        _now = LerpPose(_from, _to, eased);

        _spin += _drift * deltaTime;
        _turn += _turnVelocity * deltaTime;
        _turnVelocity *= Mathf.Exp(-deltaTime * settings.dragDamping);

        if (_isSwapOn) TickSwap(deltaTime);

        SCameraPose pose = _now;
        if (_tabProgress >= 1f) _holdTime += deltaTime;
        float push = Mathf.Clamp01(_holdTime / settings.pushTime);
        push = push < 0.5f ? 4f * push * push * push : 1f - Mathf.Pow(-2f * push + 2f, 3f) / 2f;
        _pushNow = settings.push * push;
        pose.distance *= 1f - _pushNow;

        if (_introProgress < 1f)
        {
            _introProgress = Mathf.Min(1f, _introProgress + deltaTime / settings.introTime);
            float ratio = Mathf.Pow(1f - _introProgress, 4f);
            pose.azimuth += settings.introAzimuth * ratio;
            pose.distance *= 1f + settings.introDistance * ratio;
            pose.height += settings.introHeight * ratio;
            _introRatio = ratio;

            if (_introProgress >= 1f)
            {
                _introRatio = 0f;
                OnIntroLanded();
            }
        }

        float shake = _shake > _SHAKE_MIN ? _shake : 0f;
        pose.height += Mathf.Sin(_time * 61f) * shake * 0.02f;
        pose.azimuth += Mathf.Sin(_time * 47f + 1.7f) * shake * 0.007f;
        _shake *= Mathf.Exp(-deltaTime * settings.shakeDecay);

        _mouse += (_mouseTarget - _mouse) * (1f - Mathf.Exp(-deltaTime * settings.mouseFollow));
        pose.azimuth += _mouse.x * settings.parallaxAzimuth * settings.parallax;
        pose.height += _mouse.y * settings.parallaxHeight * settings.parallax;

        Vector3 position;
        Vector3 lookAt;
        float fieldOfView = pose.fieldOfView;
        float roll = 0f;

        if (_isSwapOn)
        {
            GetSwapCamera(shake, out position, out lookAt);
            fieldOfView = _swapKey.fieldOfView;
            roll = _swapKey.roll;
        }
        else
        {
            GetShotCamera(pose, _spin, out position, out lookAt);
        }

        Transform game = _gameCamera.transform;
        float gameReach = Mathf.Max(1f, Vector3.Dot(_feet - game.position, game.forward));
        Vector3 gameLook = game.position + game.forward * gameReach;
        Transform cameraTransform = _camera.transform;
        cameraTransform.position = Vector3.LerpUnclamped(game.position, position, blend);
        Vector3 direction = Vector3.LerpUnclamped(gameLook, lookAt, blend) - cameraTransform.position;
        if (direction.sqrMagnitude > 1e-8f) cameraTransform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        if (roll != 0f) cameraTransform.Rotate(0f, 0f, roll * blend * Mathf.Rad2Deg, Space.Self);
        _camera.fieldOfView = Mathf.LerpUnclamped(_gameCamera.fieldOfView, fieldOfView, blend);

        float yaw = _startYaw + Mathf.DeltaAngle(_startYaw * Mathf.Rad2Deg, (_now.face + _turn) * Mathf.Rad2Deg)
            * Mathf.Deg2Rad * blend;
        PlacePlayerCopy(yaw);
    }

    private void GetShotCamera(SCameraPose pose, float spin, out Vector3 position, out Vector3 lookAt)
    {
        float angle = pose.azimuth + spin;
        Vector3 forward = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        Vector3 stagePosition = forward * pose.distance;
        stagePosition.y = pose.height;

        Vector3 right = new Vector3(forward.z, 0f, -forward.x);
        float aspect = _camera != null && _camera.aspect > 0f ? _camera.aspect : 16f / 9f;
        float halfWidth = Mathf.Tan(pose.fieldOfView * Mathf.Deg2Rad * 0.5f) * aspect;
        float reach = Mathf.Sqrt(pose.distance * pose.distance +
            (pose.height - pose.lookHeight) * (pose.height - pose.lookHeight));
        Vector3 stageLook = new Vector3(0f, pose.lookHeight, 0f) - right * (pose.screenX * reach * halfWidth);

        position = StageToWorld(stagePosition);
        lookAt = StageToWorld(stageLook);
    }
}
