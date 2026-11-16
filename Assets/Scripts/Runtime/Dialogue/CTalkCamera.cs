using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 카메라에서 NPC 대화와 소환 연출의 시점으로 전환합니다.
/// CTestCamera의 추적 상태를 유지해 연출이 끝나면 게임 시점으로 돌아갑니다.
/// </summary>
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(90)]
public class CTalkCamera : MonoBehaviour
{
    [Header("대상")]
    [SerializeField] private CPlayer _player;
    [SerializeField] private LayerMask _groundMask = 1 << 3;
    [SerializeField] private LayerMask _blockMask = (1 << 0) | (1 << 3);

    [Header("카메라 전환")]
    [SerializeField, Min(0.01f)] private float _blendInTime = 0.9f;
    [SerializeField, Min(0.01f)] private float _blendOutTime = 0.7f;
    [SerializeField, Min(0f)] private float _talkSharpness = 4.5f;
    [SerializeField, Min(0f)] private float _cineSharpness = 3f;

    [Header("NPC 단독 카메라")]
    [SerializeField] private float _npcHeight = 1.46f;
    [SerializeField] private float _npcLookHeight = 1.27f;
    [SerializeField] private float _npcSide = 0.42f;
    [SerializeField] private float _npcLookSide = 0.06f;
    [SerializeField] private Vector2 _npcDistanceRange = new Vector2(1.5f, 2.7f);

    [Header("큰 NPC(석상) 카메라")]
    [SerializeField] private float _largeFov = 36f;
    [SerializeField] private float _largeScreenRatio = 0.62f;
    [SerializeField] private float _largeSideAngle = 0.45f;

    private readonly struct SPose
    {
        public readonly Vector3 position;
        public readonly Vector3 look;
        public readonly float fov;
        public readonly bool hideHero;

        public SPose(Vector3 position, Vector3 look, float fov, bool hideHero)
        {
            this.position = position;
            this.look = look;
            this.fov = fov;
            this.hideHero = hideHero;
        }
    }

    private Camera _camera;
    private CharacterController _playerController;
    private CTalkNpc _npc;
    private bool _isTalking;
    private float _cineTime = -1f;
    private float _cineDuration;
    private Vector3 _cinePosition;
    private Vector3 _cineLook;
    private float _cineFov;
    private float _shake;
    private float _blend;
    private float _time;
    private float _talkStartTime;
    private float _side = 1f;
    private bool _cutNow;
    private Vector3 _curPosition;
    private Vector3 _curLook;
    private float _curFov;
    private float _baseFov;
    private bool _hasBaseFov;
    private bool _heroHidden;
    private readonly List<Renderer> _HIDDEN_RENDERERS = new List<Renderer>();

    /// <summary>
    /// 0 = 게임 카메라, 1 = 대화/소환 카메라.
    /// </summary>
    public float Blend => _blend;

    public bool IsCinematic => _cineTime >= 0f;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_player != null) _playerController = _player.GetComponent<CharacterController>();
    }

    private void OnDisable()
    {
        ShowHero();
        if (_hasBaseFov) _camera.fieldOfView = _baseFov;
        _hasBaseFov = false;
        _blend = 0f;
    }

    /// <summary>
    /// 대화 카메라의 목표 시점을 정하고, 설정한 시간 동안 게임 시점에서 전환합니다.
    /// </summary>
    public void BeginTalk(CTalkNpc npc)
    {
        _npc = npc;
        _isTalking = true;
        _cutNow = true;
        _talkStartTime = _time;
        PickSide();
    }

    /// <summary>
    /// 대화 카메라를 끝냅니다. 소환 연출이 없으면 설정한 시간 동안 게임 시점으로 돌아갑니다.
    /// </summary>
    public void EndTalk()
    {
        _isTalking = false;
    }

    /// <summary>
    /// 소환 장면 카메라를 시작합니다. 지금 대화 카메라 자리에서 이어서 목표로 옮겨 갑니다.
    /// </summary>
    public void BeginCinematic(Vector3 position, Vector3 look, float fov, float duration)
    {
        _cinePosition = position;
        _cineLook = look;
        _cineFov = fov;
        _cineDuration = duration;
        _cineTime = 0f;
        _cutNow = false;

        if (_blend <= 0f)
        {
            _curPosition = transform.position;
            _curLook = transform.position + transform.forward * 10f;
            _curFov = _camera.fieldOfView;
        }
    }

    /// <summary>
    /// 소환 연출의 흔들림 세기를 정합니다. 카메라 위치는 유지하고 바라보는 점에만 적용합니다.
    /// </summary>
    public void SetShake(float amount)
    {
        _shake = amount;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        _time += dt;

        if (_cineTime >= 0f)
        {
            _cineTime += dt;
            if (_cineTime > _cineDuration) _cineTime = -1f;
        }

        bool isActive = _isTalking || _cineTime >= 0f;
        _blend = Mathf.Clamp01(_blend + (isActive ? dt / _blendInTime : -dt / _blendOutTime));

        if (_blend <= 0f)
        {
            if (_hasBaseFov)
            {
                _camera.fieldOfView = _baseFov;
                _hasBaseFov = false;
            }

            ShowHero();
            return;
        }

        if (!_hasBaseFov)
        {
            _baseFov = _camera.fieldOfView;
            _hasBaseFov = true;
        }

        Vector3 savePosition = transform.position;
        Vector3 saveLook = GetGameLookTarget();
        bool hideHero = false;

        if (_cineTime >= 0f && !_isTalking)
        {
            float k = 1f - Mathf.Exp(-dt * _cineSharpness);
            _curPosition = Vector3.Lerp(_curPosition, _cinePosition, k);
            _curLook = Vector3.Lerp(_curLook, _cineLook, k);
            _curFov += (_cineFov - _curFov) * k;

            if (_shake > 0f)
            {
                float a = _shake * 0.35f;
                _curLook += new Vector3(Random.value - 0.5f, Random.value - 0.5f, Random.value - 0.5f) * a;
            }
        }

        else if (_isTalking && _npc != null)
        {
            SPose target = GetTalkPose();
            hideHero = target.hideHero;

            if (_cutNow)
            {
                _curPosition = target.position;
                _curLook = target.look;
                _curFov = target.fov;
                _cutNow = false;
            }

            else
            {
                float k = 1f - Mathf.Exp(-dt * _talkSharpness);
                _curPosition = Vector3.Lerp(_curPosition, target.position, k);
                _curLook = Vector3.Lerp(_curLook, target.look, k);
                _curFov += (target.fov - _curFov) * k;
            }
        }

        float e = CCssEase.Smooth(_blend);
        Vector3 position = Vector3.Lerp(savePosition, _curPosition, e);
        Vector3 look = Vector3.Lerp(saveLook, _curLook, e);
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(look - position, Vector3.up));
        _camera.fieldOfView = _baseFov + (_curFov - _baseFov) * e;

        if (_isTalking && _npc != null && _player != null)
        {
            // 높이 차이에 따라 캐릭터가 기울어지지 않도록 수평 방향으로만 회전한다.
            Vector3 toNpc = _npc.FeetPosition - _player.transform.position;
            toNpc.y = 0f;

            if (toNpc.sqrMagnitude > 1e-4f)
            {
                Quaternion face = Quaternion.LookRotation(toNpc);
                _player.transform.rotation = Quaternion.Slerp(_player.transform.rotation, face, e);
            }
        }

        if (hideHero && e > 0.5f) HideHero();

        else ShowHero();
    }

    private SPose GetTalkPose()
    {
        Vector3 feet = GetPlayerFeet();
        Vector3 npcFeet = _npc.FeetPosition;
        Vector3 d = npcFeet - feet;
        d.y = 0f;
        float distance = d.magnitude;
        d = distance < 1e-3f ? Vector3.forward : d / distance;
        Vector3 right = new Vector3(-d.z, 0f, d.x);

        if (_npc.IsLarge)
        {
            // 큰 NPC는 전체 높이가 화면에 들어오도록 거리를 구하고, 측면에서 천천히 접근한다.
            float h = Mathf.Max(0.5f, _npc.Height);
            float th = Mathf.Tan(_largeFov * Mathf.Deg2Rad * 0.5f);
            float d0 = h / (_largeScreenRatio * 2f * th);
            float distanceToNpc = d0 * (1f + 0.09f * Mathf.Exp(-(_time - _talkStartTime) * 0.45f));
            Vector3 back = Quaternion.AngleAxis(_side * _largeSideAngle * Mathf.Rad2Deg, Vector3.up) * -d;
            Vector3 position = npcFeet + back * distanceToNpc;
            position.y = Mathf.Max(npcFeet.y + h * 0.42f, GetGroundHeight(position, npcFeet.y) + 1.5f);
            float viewHeight = 2f * distanceToNpc * th;
            Vector3 look = npcFeet;
            look.y = npcFeet.y + h * 0.5f - viewHeight * 0.12f;
            return new SPose(position, look, _largeFov, true);
        }

        float cd = Mathf.Clamp(distance - 0.3f, _npcDistanceRange.x, _npcDistanceRange.y);
        Vector3 npcPosition = npcFeet - d * cd + right * (_side * _npcSide);
        npcPosition.y += _npcHeight;
        Vector3 npcLook = npcFeet + right * (_side * _npcLookSide);
        npcLook.y += _npcLookHeight;
        float fov = Mathf.Min(40f, 27f * Mathf.Pow(2.7f / cd, 0.6f));
        return new SPose(npcPosition, npcLook, fov, distance - cd < 0.5f);
    }

    // 현재 게임 카메라에 가까운 쪽부터 검사해 건물에 가리지 않는 시점을 고른다.
    private void PickSide()
    {
        if (_npc == null) return;

        Vector3 feet = GetPlayerFeet();
        Vector3 npcFeet = _npc.FeetPosition;
        Vector3 mid = (feet + npcFeet) * 0.5f;
        Vector3 d = npcFeet - feet;
        d.y = 0f;
        d = d.sqrMagnitude < 1e-6f ? Vector3.forward : d.normalized;
        Vector3 right = new Vector3(-d.z, 0f, d.x);
        float preferred = Vector3.Dot(transform.position - mid, right) >= 0f ? 1f : -1f;

        foreach (float side in new[] { preferred, -preferred })
        {
            _side = side;
            SPose test = GetTalkPose();

            if (IsClear(test.position, test.look, 0.8f) && IsClear(test.position, npcFeet + Vector3.up * 1.2f, 0.85f) &&
                IsClear(test.position, feet + Vector3.up * 1.2f, 0.8f))
            {
                return;
            }
        }

        _side = preferred;
    }

    private bool IsClear(Vector3 from, Vector3 to, float fraction)
    {
        Vector3 end = Vector3.Lerp(from, to, fraction);
        return !Physics.Linecast(from, end, _blockMask, QueryTriggerInteraction.Ignore);
    }

    private Vector3 GetPlayerFeet()
    {
        if (_player == null) return transform.position;
        if (_playerController == null) return _player.transform.position;

        Bounds bounds = _playerController.bounds;
        return new Vector3(_player.transform.position.x, bounds.min.y, _player.transform.position.z);
    }

    // 카메라 정면선에서 플레이어와 가장 가까운 점을 게임 시점의 기준으로 삼는다.
    private Vector3 GetGameLookTarget()
    {
        Vector3 forward = transform.forward;
        Vector3 focus = _player != null ? _player.transform.position : transform.position + forward * 10f;
        float along = Mathf.Max(1f, Vector3.Dot(focus - transform.position, forward));
        return transform.position + forward * along;
    }

    private float GetGroundHeight(Vector3 position, float fallback)
    {
        Vector3 origin = new Vector3(position.x, position.y + 50f, position.z);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 200f,
            _groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }

        return fallback;
    }

    private void HideHero()
    {
        if (_heroHidden || _player == null) return;

        _heroHidden = true;
        _HIDDEN_RENDERERS.Clear();

        foreach (Renderer target in _player.GetComponentsInChildren<Renderer>())
        {
            if (!target.enabled) continue;

            target.enabled = false;
            _HIDDEN_RENDERERS.Add(target);
        }
    }

    private void ShowHero()
    {
        if (!_heroHidden) return;

        _heroHidden = false;

        foreach (Renderer target in _HIDDEN_RENDERERS)
        {
            if (target != null) target.enabled = true;
        }

        _HIDDEN_RENDERERS.Clear();
    }
}
