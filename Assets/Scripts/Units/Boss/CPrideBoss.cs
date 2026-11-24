using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class CPrideBoss : CEnemy, IBoss
{

    public enum EPrideBossState
    {
        Idle = 0,
        NormalAttack,
        SubAttack,
        MainAttack,
        Dead
    }

    [Header("연결")]
    [SerializeField] private CPlayer _player;
    [SerializeField] private CPreviewPattern _previewPattern;
    [SerializeField] private CLinePreview _linePreviewPattern;
    [SerializeField] private GameObject _swordPrefab;

    [Header("평타 이펙트")]
    [SerializeField] private CEspoirBossFxPlayer _slamImpactPrefab;

    [Header("순간이동 이펙트")]
    [SerializeField] private GameObject _warpSwordPrefab;
    [SerializeField] private GameObject _warpArrivalFxPrefab;

    [Header("인식범위 (확인용)")]
    [SerializeField] private float _battleStartRadius = 20f;
    [SerializeField] private float _battleEndRadius = 25f;
    [SerializeField] private float _attackRadius = 1.5f; // 기본 평타 검찍기 공격범위
    [SerializeField] private float _attackDelay = 1.5f; // 기본 평타 검찍기 내려오는 시간

    [Header("평타 이동용")]
    [SerializeField] private LayerMask _groundMask = 1 << 0;
    [SerializeField] private LayerMask _moveObstacleMask = (1 << 0) | (1 << 6) | (1 << 7);

    [Header("메인 패턴용")]
    [SerializeField] private Material _mainBeamMaterial;
    [SerializeField, Min(1f)] private float _crossLaserRadius = 14f;

    [Header("디버그 로그")]
    [SerializeField] private bool _enableLog = false;


    private Rigidbody _rb;
    private CapsuleCollider _collider;

    private CancellationTokenSource _cts;
    private readonly List<CEspoirBossFxPlayer> _activeSlamImpacts = new List<CEspoirBossFxPlayer>();
    private Animator _animator;
    private float _realAttackDelay;
    private const float MainLaserSwordScale = 1.2f;

    private int _mainCounter = 0;
    private bool _nextMainIsCross = true;
    private bool _isRage = false;

    private Vector3 _arenaCenter;

    private bool CanFight => _player != null && _previewPattern != null
                             && HpCurrent > 0f && _player.HpCurrent > 0f;

    private static readonly int BackstepSlamTrigger = Animator.StringToHash("tBackstepSlam");
    private static readonly int IdleState = Animator.StringToHash("BaseLayer.Idle");

    protected override void Awake()
    {
        base.Awake();

        if (_player == null)
        {
            _player = FindFirstObjectByType<CPlayer>();
        }

        if (_player == null)
        {
            CLogger.Error("CPrideBoss : 플레이어를 찾을 수 없음");
            enabled = false;
        }

        _rb = GetComponent<Rigidbody>();
        _arenaCenter = _rb.position;
        _collider = GetComponent<CapsuleCollider>();
        _animator = GetComponentInChildren<Animator>(true);
        _realAttackDelay = _attackDelay;
        _isRage = true;
    }

    protected override void Update()
    {
        base.Update();

        if (!CanFight)
        {
            if (_cts != null) ResetBattleState();
            return;
        }

        float distanceSquared = GetPlayerDistanceSquared();

        if (_cts != null)
        {
            if (distanceSquared > _battleEndRadius * _battleEndRadius)
            {
                ResetBattleState();
            }
            return;
        }

        if (distanceSquared <= _battleStartRadius * _battleStartRadius)
        {
            _cts = new CancellationTokenSource();
            RunBattleLoop(_cts.Token).Forget();
        }
    }

    private async UniTask RunBattleLoop(CancellationToken token)
    {
        _realAttackDelay = _attackDelay; // 나중에 2페되면 이거 줄이면 될듯!
        try
        {
            while (!token.IsCancellationRequested)
            {
                await UniTask.Delay(1500, delayTiming: PlayerLoopTiming.LastUpdate,
                    cancellationToken: token);

                if (!CanFight ||
                    GetPlayerDistanceSquared() > _battleEndRadius * _battleEndRadius)
                {
                    return;
                }

                if (_mainCounter < 5)
                {
                    int subAttackRatio = _isRage ? 3 : 4;
                    switch(Random.Range(0, subAttackRatio))
                    {
                        case 0:
                            await RunSubAttack(token);
                            break;
                        default:
                            await RunNormalAttack(token);
                            break;
                    }
                    _mainCounter++;
                }
                else
                {
                    bool useCross = _isRage && _nextMainIsCross;

                    if (useCross)
                    {
                        await RunRotatingCrossLaserAttack(token);
                    }
                    else
                    {
                        await RunMainLaserAttack(token);
                    }

                    token.ThrowIfCancellationRequested();
                    _mainCounter = 0;
                    _nextMainIsCross = !useCross;
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // 전투 종료로 진행 중인 공격이 취소
        }
        finally
        {
            if (!token.IsCancellationRequested) ResetBattleState();
        }
    }

    private async UniTask RunNormalAttack(CancellationToken token)
    {
        Vector3 toPlayer = _player.transform.position - _rb.position;
        toPlayer.y = 0f;

        float distanceSquared = toPlayer.sqrMagnitude;


        // 2번: 4m 이내에서 50%.
        if (distanceSquared > 0.0001f && distanceSquared <= 4f * 4f && Random.value < 0.25f)
        {
            Vector3 forward = toPlayer.normalized;
            Vector3 backstepPoint = _rb.position - forward * 5f;

            if (TryGetMoveDestination(backstepPoint, out _)
                && !IsBackstepPathBlocked(-forward, 5f))
            {
                _rb.rotation = Quaternion.LookRotation(forward);
                Vector3 attackPoint = _rb.position + forward * 5f;

                if (_animator != null && _animator.isActiveAndEnabled)
                {
                    _animator.SetTrigger(BackstepSlamTrigger);
                }

                await UniTask.WhenAll(
                    SlamTask(attackPoint, _realAttackDelay * 0.75f, 0.15f, _attackRadius * 0.65f, token,
                        new SDamageInfo(100f, false, ERatioSwitch.Constant, false, null)),
                    BackstepTask(-forward, token));
                return;
            }
        }

        // 3번: 10m 이상에서 50%, 15m 이상에서 75%.
        if (_warpSwordPrefab != null && distanceSquared >= 10f * 10f)
        {
            float chaseChance = distanceSquared >= 15f * 15f ? 0.75f : 0.5f;
            Vector3 forward = toPlayer.normalized;
            Vector3 behindPlayer = _player.transform.position + forward * 2.5f;
            behindPlayer.y = _rb.position.y;
            Vector3 sideOffset = Vector3.Cross(Vector3.up, forward) * 2f;

            if (Random.value < chaseChance &&
                (TryGetMoveDestination(behindPlayer, out Vector3 destination) ||
                TryGetMoveDestination(behindPlayer + sideOffset, out destination)))
            {
                await FlyWarpSwordTask(destination, token);
                token.ThrowIfCancellationRequested();

                if (!CanFight || GetPlayerDistanceSquared() > _battleEndRadius * _battleEndRadius)
                {
                    return;
                }

                Vector3 landingPoint = destination;
                if (TryGetMoveDestination(landingPoint, out destination) ||
                    TryGetMoveDestination(landingPoint + sideOffset, out destination))
                {
                    _rb.velocity = Vector3.zero;
                    _rb.position = destination;

                    Vector3 facing = _player.transform.position - destination;
                    facing.y = 0f;
                    if (facing.sqrMagnitude > 0.0001f)
                    {
                        _rb.rotation = Quaternion.LookRotation(facing);
                    }

                    if (_warpArrivalFxPrefab != null)
                    {
                        Instantiate(_warpArrivalFxPrefab, destination, Quaternion.identity);
                    }
                }
                await SlamTask(_player.transform.position, _realAttackDelay * 0.75f, 0.15f, _attackRadius * 0.65f, token,
                        new SDamageInfo(100f, false, ERatioSwitch.Constant, false, null));
                return;
            }
        }

        if (distanceSquared > 4f * 4f && distanceSquared < 10f * 10f
            && _linePreviewPattern != null && _warpSwordPrefab != null
            && Random.value < 0.5f)
        {
            await RunAimedLineSword(token);
            return;
        }

        // 1번: 기본 낙하와 이동 불가 시 대체 공격.
        await SlamTask(_player.transform.position, _realAttackDelay * 1f, 0.2f, _attackRadius, token,
                        new SDamageInfo(100f, false, ERatioSwitch.Constant, false, null));
    }

    private async UniTask RunSubAttack(CancellationToken token)
    {
        if (GetPlayerDistanceSquared() <= 4f * 4f
            && _linePreviewPattern != null && _warpSwordPrefab != null)
        {
            await RunBackstepSwordVolley(token);
            return;
        }

        switch (Random.Range(0, 2))
        {
            case 0:
                await RunGiantSlam(token);
                break;

            case 1:
                await RunSeveralSlam(true, token);
                break;
        }
    }

    #region MainPattern
    private async UniTask RunMainLaserAttack(CancellationToken token)
    {
        if (!CanFight || _linePreviewPattern == null ||
            _warpSwordPrefab == null || _mainBeamMaterial == null) return;

        const int invincibleStatusId = 38;
        Rigidbody body = _rb;
        if (!TryGetMoveDestination(_arenaCenter, out Vector3 restPosition)) return;
        Vector3 groundOrigin = restPosition;

        if (TryGetGround(groundOrigin, out RaycastHit ground))
        {
            groundOrigin.y = ground.point.y;
        }

        bool arrivedAtCenter = false;
        bool wasKinematic = body.isKinematic;
        CStatus invincible = null;
        GameObject[] swords = new GameObject[3];

        try
        {
            if (FindStatus(invincibleStatusId) == null)
            {
                AddStatus(invincibleStatusId, duration: 0f);
                invincible = FindStatus(invincibleStatusId);
            }

            if (!wasKinematic) body.velocity = Vector3.zero;
            body.isKinematic = true;

            await MoveBossTask(restPosition, 0.45f, token);
            if (!CanFight) return;

            arrivedAtCenter = true;

            Vector3 forward = AimFrom(groundOrigin, Vector3.forward);
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            body.rotation = Quaternion.LookRotation(forward);

            for (int i = 0; i < swords.Length; i++)
            {
                Vector3 offset = right * ((i - 1) * 2.5f) + Vector3.up * (i == 1 ? 3.2f : 2.4f);

                swords[i] = Instantiate(_warpSwordPrefab, restPosition + offset, Quaternion.LookRotation(Vector3.down, forward));
                swords[i].transform.localScale *= MainLaserSwordScale;
                swords[i].transform.SetParent(body.transform, true);

                if (swords[i].TryGetComponent(out TrailRenderer trail))
                {
                    trail.emitting = false;
                }
            }

            await MoveBossTask(restPosition + Vector3.up * 3f, 0.45f, token);
            if (!CanFight) return;

            foreach (GameObject sword in swords)
            {
                sword.transform.SetParent(null, true);
            }

            Vector3[] spreadStarts = new Vector3[swords.Length];
            Vector3[] spreadDirections = new Vector3[swords.Length];

            for (int i = 0; i < swords.Length; i++)
            {
                spreadStarts[i] = groundOrigin + forward * 3f + right * ((i - 1) * 4.5f);
            }

            for (int volley = 0; volley < 2; volley++)
            {
                for (int i = 0; i < swords.Length; i++)
                {
                    float yaw;

                    switch (i)
                    {
                        case 0:
                            yaw = Random.Range(-45f, -20f);
                            break;
                        case 1:
                            yaw = Random.Range(-8f, 8f);
                            break;
                        default:
                            yaw = Random.Range(20f, 45f);
                            break;
                    }

                    Vector3 aim = AimFrom(spreadStarts[i], forward);
                    spreadDirections[i] = Quaternion.Euler(0f, yaw, 0f) * aim;
                }

                await RunMainLaserVolley(swords, spreadStarts, spreadDirections, false, token);

                if (!CanFight) return;
            }

            forward = AimFrom(groundOrigin, forward);
            body.rotation = Quaternion.LookRotation(forward);

            Vector3[] finalStarts = new Vector3[1] { groundOrigin + forward * 3f };
            Vector3[] finalDirections = new Vector3[1] { AimFrom(finalStarts[0], forward) };

            await RunMainLaserVolley(swords, finalStarts, finalDirections, true, token);

            if (!CanFight) return;

            await MoveBossTask(restPosition, 0.8f, token);
        }
        finally
        {
            foreach (GameObject sword in swords)
            {
                if (sword != null) Destroy(sword);
            }

            if (body != null)
            {
                if (arrivedAtCenter) body.position = restPosition;
                body.isKinematic = wasKinematic;
            }

            if (this != null && invincible != null) RemoveStatus(invincible);
        }

        if (CanFight)
        {
            await UniTask.Delay(1000, delayTiming: PlayerLoopTiming.LastUpdate,
                cancellationToken: token);
        }

        Vector3 AimFrom(Vector3 start, Vector3 fallback)
        {
            Vector3 direction = _player.transform.position - start;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : fallback;
        }
    }

    private async UniTask RunRotatingCrossLaserAttack(CancellationToken token)
    {
        if (!CanFight || _linePreviewPattern == null ||
            _warpSwordPrefab == null || _mainBeamMaterial == null) return;

        if (!TryGetMoveDestination(_arenaCenter, out Vector3 center)) return;

        const float moveTime = 0.45f;
        const float warningTime = 1f;
        const float warningAngle = 15f;
        const float fireTime = 5f;
        const float fireAngle = 135f;
        const float width = 3f;
        const float fadeInTime = 0.15f;
        const float fadeOutTime = 0.18f;
        const float returnTime = 0.3f;
        const float tipDistance = 3.3125f * MainLaserSwordScale;
        const float hitInterval = 1f;

        float radius = _crossLaserRadius;
        float halfWidth = width * 0.5f;
        Vector3 beamHeight = Vector3.up * 2.2f;
        const int invincibleStatusId = 38;
        CStatus invincible = null;
        bool startedHovering = false;
        float startYaw = _rb.rotation.eulerAngles.y;
        bool wasKinematic = _rb.isKinematic;
        GameObject crossRoot = null;

        Vector3[] axes = new Vector3[4]
        {
            Vector3.forward, Vector3.right, Vector3.back, Vector3.left
        };

        CLinePreview[] previews = new CLinePreview[axes.Length];
        LineRenderer[] beams = new LineRenderer[axes.Length];
        Transform[] swords = new Transform[axes.Length];
        Vector3[] swordPositions = new Vector3[axes.Length];
        SDamageInfo damage = new SDamageInfo(300f, false, ERatioSwitch.Constant, false, null);

        try
        {
            if (!wasKinematic) _rb.velocity = Vector3.zero;
            _rb.isKinematic = true;

            // 보스를 아레나 중앙으로 이동
            await MoveBossTask(center, moveTime, token);
            if (!CanFight) return;

            if (FindStatus(invincibleStatusId) == null)
            {
                AddStatus(invincibleStatusId, duration: 0f);
                invincible = FindStatus(invincibleStatusId);
            }

            startedHovering = true;
            await MoveBossTask(center + Vector3.up * 5f, 0.45f, token);
            if (!CanFight) return;

            float elapsed;

            crossRoot = new GameObject("Pride Rotating Cross");
            Transform pivot = crossRoot.transform;
            pivot.SetPositionAndRotation(center, Quaternion.Euler(0f, startYaw, 0f));

            for (int i = 0; i < axes.Length; i++)
            {
                Vector3 outward = pivot.TransformDirection(axes[i]);
                Vector3 groundStart = center + outward * radius;
                Vector3 beamStart = groundStart + beamHeight;
                Vector3 beamEnd = center + beamHeight;
                Vector3 swordPosition = beamStart + outward * tipDistance;

                GameObject sword = Instantiate(_warpSwordPrefab,
                    swordPosition, Quaternion.LookRotation(-outward), pivot);
                sword.transform.localScale *= MainLaserSwordScale;

                if (sword.TryGetComponent(out TrailRenderer trail))
                {
                    trail.emitting = false;
                }

                swords[i] = sword.transform;
                swordPositions[i] = sword.transform.localPosition;

                previews[i] = Instantiate(_linePreviewPattern, pivot);
                previews[i].Show(groundStart, center, width);

                // 빔의 두 끝점을 회전 중심 기준 로컬 좌표로 저장
                LineRenderer beam = ShowMainBeam(pivot, beamStart, beamEnd, width);
                beam.useWorldSpace = false;
                beam.SetPosition(0, pivot.InverseTransformPoint(beamStart));
                beam.SetPosition(1, pivot.InverseTransformPoint(beamEnd));
                beams[i] = beam;
            }

            elapsed = 0f;
            while (elapsed < warningTime)
            {
                await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, token);
                if (!CanFight) return;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / warningTime);
                pivot.rotation = Quaternion.Euler(0f, startYaw + warningAngle * t, 0f);

                float alpha = Mathf.SmoothStep(0f, 1f,
                    (elapsed - (warningTime - fadeInTime)) / fadeInTime);
                SetCrossLaserAlpha(beams, alpha);
            }

            for (int i = 0; i < previews.Length; i++)
            {
                previews[i].ClearPreview();
            }

            
            float nextHitTime = 0f;

            elapsed = 0f;

            while (elapsed < fireTime && CanFight)
            {
                float angle = startYaw + warningAngle + fireAngle * (elapsed / fireTime);
                pivot.rotation = Quaternion.Euler(0f, angle, 0f);

                if (elapsed >= nextHitTime)
                {
                    Vector3 local = pivot.InverseTransformPoint(_player.transform.position);
                    float x = Mathf.Abs(local.x);
                    float z = Mathf.Abs(local.z);

                    if ((x <= halfWidth && z <= radius) || (z <= halfWidth && x <= radius))
                    {
                        nextHitTime = elapsed + hitInterval;
                        _player.OnHit(this, damage);
                    }
                }

                await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, token);
                elapsed += Time.deltaTime;
            }

            if (!CanFight) return;
            pivot.rotation = Quaternion.Euler(0f, startYaw + warningAngle + fireAngle, 0f);

            // 검 회수
            elapsed = 0f;
            while (elapsed < returnTime)
            {
                await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, token);
                if (!CanFight) return;

                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / returnTime);
                SetCrossLaserAlpha(beams, 1f - Mathf.Clamp01(elapsed / fadeOutTime));

                for (int i = 0; i < swords.Length; i++)
                {
                    swords[i].localPosition = Vector3.Lerp(swordPositions[i], beamHeight, t);
                }
            }

            await MoveBossTask(center, 0.8f, token);
        }
        finally
        {
            if (crossRoot != null) Destroy(crossRoot);

            if (_rb != null)
            {
                if (startedHovering) _rb.position = center;
                _rb.isKinematic = wasKinematic;
            }

            if (this != null && invincible != null) RemoveStatus(invincible);
        }

        if (CanFight)
        {
            await UniTask.Delay(800, delayTiming: PlayerLoopTiming.LastUpdate,
                cancellationToken: token);
        }
    }

    private async UniTask RunMainLaserVolley(GameObject[] swords, Vector3[] starts, Vector3[] directions,
        bool isFinale, CancellationToken token)
    {
        int beamCount = starts.Length;
        float width = isFinale ? 12f : 3f;
        const float length = 22f;
        const float tipDistance = 3.3125f * MainLaserSwordScale;

        float activeTime = isFinale ? 2.5f : 0.75f;
        Vector3 beamHeight = Vector3.up * 2.2f;

        CLinePreview[] previews = new CLinePreview[beamCount];
        LineRenderer[] beams = new LineRenderer[beamCount];
        Transform[] firingSwords = new Transform[beamCount];
        Vector3[] initialPositions = new Vector3[beamCount];
        Quaternion[] initialRotations = new Quaternion[beamCount];

        SDamageInfo damage = new SDamageInfo(isFinale ? 1000f : 300f, false, ERatioSwitch.Constant, false, null);

        try
        {
            for (int i = 0; i < beamCount; i++)
            {
                Transform sword = swords[isFinale ? 1 : i].transform;
                firingSwords[i] = sword;
                initialPositions[i] = sword.position;
                initialRotations[i] = sword.rotation;

                previews[i] = Instantiate(_linePreviewPattern);
                previews[i].Show(starts[i], starts[i] + directions[i] * length, width);
            }

            const float turnTime = 0.25f;
            const float fadeInTime = 0.15f;
            float warningTime = isFinale ? 2f : 1.25f;
            float waitTime = warningTime - turnTime - fadeInTime;

            float turnElapsed = 0f;

            while (turnElapsed < turnTime)
            {
                await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, token);
                if (!CanFight) return;

                turnElapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, turnElapsed / turnTime);

                for (int i = 0; i < beamCount; i++)
                {
                    Vector3 position = starts[i] + beamHeight - directions[i] * tipDistance;
                    Quaternion rotation = Quaternion.LookRotation(directions[i]);

                    firingSwords[i].SetPositionAndRotation(
                        Vector3.Lerp(initialPositions[i], position, t),
                        Quaternion.Slerp(initialRotations[i], rotation, t));
                }
            }

            if (isFinale)
            {
                await GatherMainLaserCharge(swords, starts[0] + beamHeight, tipDistance, waitTime, token);
            }
            else
            {
                await UniTask.Delay(TimeSpan.FromSeconds(waitTime),
                    delayTiming: PlayerLoopTiming.LastUpdate, cancellationToken: token);
            }

            if (!CanFight) return;

            for (int i = 0; i < beamCount; i++)
            {
                Vector3 beamStart = starts[i] + beamHeight;
                beams[i] = ShowMainBeam(
                    firingSwords[i], beamStart, beamStart + directions[i] * length, width);
            }

            float fadeInElapsed = 0f;

            while (fadeInElapsed < fadeInTime)
            {
                await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, token);
                if (!CanFight) return;

                fadeInElapsed += Time.deltaTime;
                float alpha = Mathf.SmoothStep(0f, 1f, fadeInElapsed / fadeInTime);
                Color color = new Color(1f, 1f, 1f, alpha);

                foreach (LineRenderer beam in beams)
                {
                    beam.startColor = color;
                    beam.endColor = color;
                }
            }

            foreach (CLinePreview preview in previews)
            {
                preview.ClearPreview();
            }

            bool hit = false;
            float elapsed = 0f;

            while (elapsed < activeTime && CanFight)
            {
                if (!hit)
                {
                    for (int i = 0; i < beamCount; i++)
                    {
                        Vector3 offset = _player.transform.position - starts[i];
                        Vector3 direction = directions[i];
                        Vector3 right = Vector3.Cross(Vector3.up, direction);

                        float along = Vector3.Dot(offset, direction);
                        float across = Mathf.Abs(Vector3.Dot(offset, right));

                        if (along >= 0f && along <= length && across <= width * 0.5f)
                        {
                            hit = true;
                            _player.OnHit(this, damage);
                            break;
                        }
                    }
                }

                await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, token);
                elapsed += Time.deltaTime;
            }
            const float fadeTime = 0.18f;
            float fadeElapsed = 0f;

            while (fadeElapsed < fadeTime)
            {
                await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, token);
                fadeElapsed += Time.deltaTime;

                float alpha = 1f - Mathf.Clamp01(fadeElapsed / fadeTime);
                Color color = new Color(1f, 1f, 1f, alpha);

                foreach (LineRenderer beam in beams)
                {
                    beam.startColor = color;
                    beam.endColor = color;
                }
            }
        }
        finally
        {
            for (int i = 0; i < beamCount; i++)
            {
                if (previews[i] != null) Destroy(previews[i].gameObject);
                if (beams[i] != null) Destroy(beams[i].gameObject);
            }
        }
    }

    private static void SetCrossLaserAlpha(LineRenderer[] beams, float alpha)
    {
        Color color = new Color(1f, 1f, 1f, alpha);

        for (int i = 0; i < beams.Length; i++)
        {
            beams[i].startColor = color;
            beams[i].endColor = color;
        }
    }

    private LineRenderer ShowMainBeam(Transform parent, Vector3 start, Vector3 end, float width)
    {
        GameObject beamObject = new GameObject("Pride Main Beam");
        beamObject.transform.SetParent(parent, false);

        LineRenderer beam = beamObject.AddComponent<LineRenderer>();
        beam.sharedMaterial = _mainBeamMaterial;
        beam.useWorldSpace = true;
        beam.positionCount = 2;
        beam.startWidth = width;
        beam.endWidth = width;
        beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        beam.receiveShadows = false;
        beam.SetPosition(0, start);
        beam.SetPosition(1, end);

        beam.textureMode = LineTextureMode.Stretch;
        Color transparent = new Color(1f, 1f, 1f, 0f);
        beam.startColor = transparent;
        beam.endColor = transparent;

        return beam;
    }

    private async UniTask GatherMainLaserCharge(GameObject[] swords, Vector3 centerTip, float tipDistance,
        float waitTime, CancellationToken token)
    {
        Transform center = swords[1].transform;
        Transform[] sideSwords = new Transform[2]
        {
            swords[0].transform,
            swords[2].transform
        };

        Vector3[] initialPositions = new Vector3[2];
        Quaternion[] initialRotations = new Quaternion[2];
        Vector3[] chargePositions = new Vector3[2];
        Quaternion[] chargeRotations = new Quaternion[2];

        for (int i = 0; i < sideSwords.Length; i++)
        {
            initialPositions[i] = sideSwords[i].position;
            initialRotations[i] = sideSwords[i].rotation;

            float side = i == 0 ? -1f : 1f;
            chargePositions[i] = centerTip + center.right * (side * (tipDistance + 2f)) - center.forward * 1.5f;

            chargeRotations[i] = Quaternion.LookRotation(centerTip - chargePositions[i]);
        }

        const float aimTime = 0.25f;
        float aimElapsed = 0f;

        while (aimElapsed < aimTime)
        {
            await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, token);
            if (!CanFight) return;

            aimElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, aimElapsed / aimTime);

            for (int i = 0; i < sideSwords.Length; i++)
            {
                sideSwords[i].SetPositionAndRotation(
                    Vector3.Lerp(initialPositions[i], chargePositions[i], t),
                    Quaternion.Slerp(initialRotations[i], chargeRotations[i], t));
            }
        }

        Vector3[] starts = new Vector3[sideSwords.Length];
        for (int i = 0; i < sideSwords.Length; i++)
        {
            starts[i] = sideSwords[i].position + sideSwords[i].forward * tipDistance;
        }

        LineRenderer[] streaks = new LineRenderer[starts.Length];

        try
        {
            for (int i = 0; i < streaks.Length; i++)
            {
                streaks[i] = ShowMainBeam(center, starts[i], centerTip, 0.24f);
            }

            float chargeTime = waitTime - aimTime;
            float elapsed = 0f;

            while (elapsed < chargeTime)
            {
                await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, token);
                if (!CanFight) return;

                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / chargeTime);
                float head = Mathf.SmoothStep(0f, 1f, progress);
                float tail = Mathf.Max(0f, head - 0.35f);
                float alpha = Mathf.Clamp01(Mathf.Sin(Mathf.PI * progress));
                Color color = new Color(1f, 1f, 1f, alpha);

                for (int i = 0; i < streaks.Length; i++)
                {
                    LineRenderer streak = streaks[i];
                    streak.SetPosition(0, Vector3.Lerp(starts[i], centerTip, tail));
                    streak.SetPosition(1, Vector3.Lerp(starts[i], centerTip, head));
                    streak.startColor = color;
                    streak.endColor = color;
                }
            }
        }
        finally
        {
            foreach (LineRenderer streak in streaks)
            {
                if (streak != null) Destroy(streak.gameObject);
            }
        }
    }
    #endregion

    #region SubPattern
    private async UniTask RunGiantSlam(CancellationToken token)
    {
        const float fallingTime = 0.25f;
        const float escapeGap = 1f; // 도망칠 수 있게 적용하는 최소 회피간격
        Vector3 attackPoint = _player.transform.position;
        

        float aimedRadius = _attackRadius * 1.5f;
        float spreadRadius = _attackRadius * 1f;
        float innerDistance = aimedRadius + spreadRadius + escapeGap;

        for (int i = 0; i < 2; i++)
        {
            token.ThrowIfCancellationRequested();

            if (!CanFight || GetPlayerDistanceSquared() > _battleEndRadius * _battleEndRadius)
            {
                return;
            }

            float additionalTime = i % 2 == 1 ? 1.5f : 1.5f;

            float readyTime = Mathf.Max(fallingTime, _realAttackDelay * 0.75f * additionalTime);
            Vector3 center = _player.transform.position;
            int swordCount = i == 0 ? 1 : 5;
            UniTask[] slams = new UniTask[swordCount];

            bool hitApplied = false;

            bool TryConsumeHit()
            {
                if (hitApplied) return false;
                hitApplied = true;
                return true;
            }

            slams[0] = SlamTask(center, readyTime, fallingTime, aimedRadius, token,
                new SDamageInfo(200f, false, ERatioSwitch.Constant, false, null), 3f, TryConsumeHit);

            int spreadCount = slams.Length - 1;
            float angleOffset = Random.Range(0f, 360f);

            for (int j = 0; j < spreadCount; j++)
            {
                float angle = (angleOffset + j * (360f / spreadCount)
                    + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
                float distance = Random.Range(innerDistance, innerDistance + 3f);
                Vector3 direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                Vector3 point = center + direction * distance;

                slams[j + 1] = SlamTask(point, readyTime, fallingTime, spreadRadius, token,
                    new SDamageInfo(200f, false, ERatioSwitch.Constant, false, null), 2f, TryConsumeHit);
            }

            await UniTask.WhenAll(slams);
        }
    }

    private async UniTask RunSeveralSlam(bool special, CancellationToken token)
    {
        for (int i = 0; i < 5; i++)
        {
            token.ThrowIfCancellationRequested();

            if (!CanFight || GetPlayerDistanceSquared() > _battleEndRadius * _battleEndRadius)
            {
                return;
            }

            bool hitApplied = false;

            bool TryConsumeHit()
            {
                if (hitApplied) return false;
                hitApplied = true;
                return true;
            }

            SlamTask(_player.transform.position, _realAttackDelay, 0.2f, _attackRadius, token,
                new SDamageInfo(150f, false, ERatioSwitch.Constant, false, null), 1.5f, TryConsumeHit).Forget();


            if (special)
            {
                float angleOffset = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(_attackRadius * 2f + 0.5f, _attackRadius * 2f + 3f);
                Vector3 direction = new Vector3(Mathf.Sin(angleOffset), 0f, Mathf.Cos(angleOffset));
                Vector3 point = _player.transform.position + direction * distance;
                SlamTask(point, _realAttackDelay, 0.2f, _attackRadius, token,
                new SDamageInfo(150f, false, ERatioSwitch.Constant, false, null), 1.5f, TryConsumeHit).Forget();
            }

            await UniTask.Delay((int)(_realAttackDelay * 2000 / 3f),
                delayTiming: PlayerLoopTiming.LastUpdate,
                cancellationToken: token);
        }

        await UniTask.Delay(2000, delayTiming: PlayerLoopTiming.LastUpdate,
            cancellationToken: token);
    }

    private async UniTask RunBackstepSwordVolley(CancellationToken token)
    {
        if (_linePreviewPattern == null || _warpSwordPrefab == null || !CanFight) return;

        const float backstepDistance = 5f;
        const int swordCount = 3;

        Vector3 target = _rb.position;
        Vector3 forward = _player.transform.position - target;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f) return;
        forward.Normalize();

        Vector3 retreatPoint = target - forward * backstepDistance;
        if (!TryGetMoveDestination(retreatPoint, out _) ||
            IsBackstepPathBlocked(-forward, backstepDistance))
        {
            await RunAimedLineSword(token);
            return;
        }

        _rb.rotation = Quaternion.LookRotation(forward);

        if (_animator != null && _animator.isActiveAndEnabled)
        {
            _animator.SetTrigger(BackstepSlamTrigger);
        }

        await BackstepTask(-forward, token);
        token.ThrowIfCancellationRequested();

        if (!CanFight || GetPlayerDistanceSquared() > _battleEndRadius * _battleEndRadius)
        {
            return;
        }

        Vector3 launchCenter = _rb.position;
        SDamageInfo damage = new SDamageInfo(100f, false, ERatioSwitch.Constant, false, null);
        UniTask[] swords = new UniTask[swordCount];
        bool hitApplied = false;

        bool TryConsumeHit()
        {
            if (hitApplied) return false;
            hitApplied = true;
            return true;
        }

        for (int i = 0; i < swordCount; i++)
        {
            float sectorStart = -90f + i * 60f;
            float angle = Random.Range(sectorStart, sectorStart + 60f);
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * -forward;
            Vector3 start = launchCenter + direction * Random.Range(6f, 8f);

            if (TryGetGround(start, out RaycastHit ground))
            {
                start.y = ground.point.y;
            }

            const float beyondCrossing = 5f;
            const float timeToCrossing = 0.25f;

            Vector3 toCrossing = target - start;
            toCrossing.y = 0f;

            float distanceToCrossing = toCrossing.magnitude;
            float speed = distanceToCrossing / timeToCrossing;

            float pathLength = distanceToCrossing + beyondCrossing;
            Vector3 end = start + toCrossing.normalized * pathLength;
            float flightTime = pathLength / speed;

            swords[i] = LineSwordTask(start, end, 1.5f, 1f, flightTime, damage, token, TryConsumeHit);
        }

        await UniTask.WhenAll(swords);
    }
    #endregion

    private async UniTask BackstepTask(Vector3 direction, CancellationToken token)
    {
        // 0.25초 동안 뒤로 이동
        _rb.velocity = direction * 20f + Vector3.up * _rb.velocity.y;

        try
        {
            await UniTask.Delay(250, delayTiming: PlayerLoopTiming.LastUpdate,
                cancellationToken: token);
        }
        finally
        {
            if (_rb != null)
            {
                _rb.velocity = Vector3.up * _rb.velocity.y;
            }
        }
    }

    private async UniTask FlyWarpSwordTask(Vector3 destination, CancellationToken token)
    {
        const float flightTime = 0.25f;

        Vector3 height = Vector3.up * 1.6f;
        Vector3 from = _rb.position;
        Vector3 forward = Vector3.ProjectOnPlane(destination - from, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        Vector3 start = from + height + right * 1.4f;
        Vector3 end = destination + height;
        Vector3 control = (start + end) * 0.5f + right * 4f;

        GameObject sword = Instantiate(
            _warpSwordPrefab, start, Quaternion.LookRotation(control - start));

        try
        {
            await UniTask.Delay(250, delayTiming: PlayerLoopTiming.LastUpdate,
                cancellationToken: token);

            float elapsed = 0f;
            while (elapsed < flightTime)
            {
                await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, token);
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / flightTime);
                Vector3 a = Vector3.Lerp(start, control, t);
                Vector3 b = Vector3.Lerp(control, end, t);

                sword.transform.SetPositionAndRotation(
                    Vector3.Lerp(a, b, t),
                    Quaternion.LookRotation(b - a));
            }
        }
        finally
        {
            if (sword != null) Destroy(sword);
        }
    }
    private UniTask RunAimedLineSword(CancellationToken token)
    {
        if (!CanFight) return UniTask.CompletedTask;

        Vector3 start = _rb.position;
        Vector3 direction = _player.transform.position - start;
        direction.y = 0f;

        if (TryGetGround(start, out RaycastHit ground))
        {
            start.y = ground.point.y;
        }

        Vector3 end = start + direction.normalized * 15f;
        SDamageInfo damage = new SDamageInfo(100f, false, ERatioSwitch.Constant, false, null);

        return LineSwordTask(start, end, width: 1.5f, warningTime: 1.5f,
            flightTime:  0.15f, damage: damage, token: token);
    }



    private bool TryGetMoveDestination(Vector3 point, out Vector3 destination)
    {
        destination = point;

        if (!TryGetGround(point, out RaycastHit ground))
        {
            if (_enableLog)
            {
                CLogger.Warn($"[PrideBoss] 바닥 탐색 실패 : 목적지 {point}, Ground Mask {_groundMask.value}");
            }

            return false;
        }

        destination.y = ground.point.y + 0.02f;

        Bounds bounds = _collider.bounds;
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        float halfSegment = Mathf.Max(0f, bounds.extents.y - radius);
        Vector3 center = destination + (bounds.center - _rb.position);
        Vector3 verticalOffset = Vector3.up * halfSegment;
        Vector3 bottom = center - verticalOffset;
        Vector3 top = center + verticalOffset;

        bool blocked = Physics.CheckCapsule(bottom, top,
            radius, _moveObstacleMask, QueryTriggerInteraction.Ignore);

        if (blocked && _enableLog)
        {
            CLogger.Warn($"[PrideBoss] 목적지 겹침 : 위치 {destination}, 바닥 {ground.collider.name}");

            Collider[] blockers = Physics.OverlapCapsule(bottom, top,
                radius, _moveObstacleMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < blockers.Length; i++)
            {
                Collider obstacle = blockers[i];
                string layerName = LayerMask.LayerToName(obstacle.gameObject.layer);
                CLogger.Warn($"[PrideBoss] 목적지를 막은 콜라이더 {obstacle.name} ({layerName})");
            }
        }

        return !blocked;
    }

    private bool IsBackstepPathBlocked(Vector3 direction, float distance)
    {
        bool blocked = _rb.SweepTest(direction, out RaycastHit hit,
            distance, QueryTriggerInteraction.Ignore);

        if (blocked && _enableLog && hit.collider != null)
        {
            Collider obstacle = hit.collider;
            string layerName = LayerMask.LayerToName(obstacle.gameObject.layer);
            CLogger.Warn(
                $"[PrideBoss] 백스텝 경로 충돌 : {obstacle.name} ({layerName}), {hit.distance:F2}m / {distance:F2}m");
        }

        return blocked;
    }

    private bool TryGetGround(Vector3 point, out RaycastHit hit)
    {
        return Physics.Raycast(point + Vector3.up * 2f, Vector3.down,
            out hit, 4f, _groundMask, QueryTriggerInteraction.Ignore);
    }

    private float GetPlayerDistanceSquared()
    {
        Vector3 toPlayer = _player.transform.position - transform.position;
        toPlayer.y = 0f;
        return toPlayer.sqrMagnitude;
    }


    private async UniTask SlamTask(Vector3 attackPoint, float readyTime, float fallingTime, float radius, CancellationToken token,
        SDamageInfo damage, float swordScale = 1f, Func<bool> tryConsumeHit = null)
    {
        if (TryGetGround(attackPoint, out RaycastHit ground))
        {
            attackPoint.y = ground.point.y;
        }
        else
        {
            attackPoint.y = _rb.position.y;
        }

        CPreviewPattern preview = Instantiate(
            _previewPattern,
            attackPoint + Vector3.up * 0.05f,
            Quaternion.identity);

        GameObject sword = null;

        try
        {
            preview.ShowCirclePattern(64, radius);

            Vector3 airPoint = attackPoint + Vector3.up * 12f;
            if (_swordPrefab != null)
            {
                sword = Instantiate(_swordPrefab, airPoint, Quaternion.identity);
                sword.transform.localScale *= swordScale;
            }

            await UniTask.Delay(
                (int) (readyTime * 1000) - (int) (fallingTime * 1000),
                delayTiming: PlayerLoopTiming.LastUpdate,
                cancellationToken: token);

            float deltaT = 0f;
            while (deltaT < fallingTime)
            {
                await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, token);
                deltaT += Time.deltaTime;

                float t = Mathf.Clamp01(deltaT / fallingTime);
                if (sword != null)
                {
                    sword.transform.position = Vector3.Lerp(airPoint, attackPoint, t * t);
                }
            }

            preview.ClearPreview();

            if (HpCurrent <= 0f || _player == null || _player.HpCurrent <= 0f) return;

            PlaySlamImpact(preview.transform.position, radius);

            Vector3 offset = _player.transform.position - attackPoint;
            offset.y = 0f;

            if (offset.sqrMagnitude <= radius * radius &&
                (tryConsumeHit == null || tryConsumeHit()))
            {
                _player.OnHit(this, damage);
            }

            await UniTask.Delay(200, cancellationToken: token); 
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // 패턴 취소.
        }
        finally
        {
            if (preview != null) Destroy(preview.gameObject);
            if (sword != null) Destroy(sword);
        }
    }

    private void PlaySlamImpact(Vector3 previewCenter, float radius)
    {
        if (_slamImpactPrefab == null) return;

        _activeSlamImpacts.RemoveAll(impact => impact == null || impact.Finished);

        CEspoirBossFxPlayer fx = Instantiate(
            _slamImpactPrefab, previewCenter, Quaternion.identity);

        if (fx.Stage == null)
        {
            CLogger.Warn("착지 이펙트의 Data와 Library 연결 확인필요");
            Destroy(fx.gameObject);
            return;
        }

        fx.screenEffects = false;

        const float referenceRadius = 3.2f;

        fx.stageScale *= radius / referenceRadius;
        fx.ApplyStageScale();

        Vector3 flashOffset = new Vector3(0.810f, -2.445f, 3.099f);
        fx.SetElements(new CEspoirBossFxPlayer.CElementSetting[]
        {
        // 0번 GroundShockDistort는 섬광과 같은 높이에서 시작함
        new CEspoirBossFxPlayer.CElementSetting
        {
            objectIndex = 0,
            offset = flashOffset
        },
        new CEspoirBossFxPlayer.CElementSetting
        {
            isGroup = true,
            group = CEspoirBossFxPlayer.EEffectGroup.Shockwave,
            offset = new Vector3(0.810f, 0f, 3.099f)
        },
        new CEspoirBossFxPlayer.CElementSetting
        {
            isGroup = true,
            group = CEspoirBossFxPlayer.EEffectGroup.Flash,
            offset = flashOffset
        }
        });

        const float impactStart = 0.9f;
        const float impactDuration = 0.5f;

        float spawnedAt = Time.time;
        fx.clock = () => impactStart
            + Mathf.Min(Time.time - spawnedAt, impactDuration);

        Destroy(fx.gameObject, impactDuration);
        fx.Advance(0f);

        Transform groundRing = fx.Stage.Find("fx3_SubBurstField");
        if (groundRing == null)
        {
            CLogger.Warn($"{_slamImpactPrefab.name} : 착지 장판 없음");
            fx.Stop();
            return;
        }

        fx.transform.position += previewCenter - groundRing.position;
        _activeSlamImpacts.Add(fx);
    }

    private async UniTask LineSwordTask(Vector3 start, Vector3 end, float width, float warningTime, float flightTime,
        SDamageInfo damage, CancellationToken token, Func<bool> tryConsumeHit = null)
    {
        if (_linePreviewPattern == null || _warpSwordPrefab == null || !CanFight) return;
        if (width <= 0f || warningTime < 0f || flightTime <= 0f) return;

        const float referenceWidth = 1.5f;
        const float referenceTipOffset = 3.3125f;
        const float referenceSwordLength = 7.71f;

        Vector3 direction = end - start;
        direction.y = 0f;

        float length = direction.magnitude;
        if (length < 0.001f) return;

        Vector3 forward = direction / length;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        float swordScale = width / referenceWidth;
        float travelLength = length + referenceSwordLength * swordScale * 0.25f;
        float speed = length / flightTime;
        float tipOffset = referenceTipOffset * swordScale;
        Vector3 swordOffset = Vector3.up * 1.6f - forward * tipOffset;

        CLinePreview preview = Instantiate(_linePreviewPattern);
        GameObject sword = null;

        try
        {
            preview.Show(start, end, width);
            await UniTask.Delay(TimeSpan.FromSeconds(warningTime),
                                delayTiming: PlayerLoopTiming.LastUpdate,
                                cancellationToken: token);

            if (!CanFight) return;

            preview.ClearPreview();
            sword = Instantiate(_warpSwordPrefab, start + swordOffset,
                                Quaternion.LookRotation(forward));
            
            TrailRenderer trail = sword.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.time = 25f / speed;
                trail.widthMultiplier = 2f;
            }

            sword.transform.localScale *= swordScale;

            float elapsed = 0f;
            float distance = 0f;
            bool hit = false;
            Vector3 previousPlayerPosition = _player.transform.position;

            while (distance < travelLength)
            {
                await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, token);
                if (!CanFight) return;

                float previousDistance = distance;
                elapsed += Time.deltaTime;
                distance = Mathf.Min(elapsed * speed, travelLength);
                sword.transform.position = start + forward * distance + swordOffset;

                Vector3 playerPosition = _player.transform.position;
                float before = Vector3.Dot(previousPlayerPosition - start, forward) - previousDistance;
                float after = Vector3.Dot(playerPosition - start, forward) - distance;

                if (!hit && before * after <= 0f)
                {
                    float crossingTime = Mathf.InverseLerp(before, after, 0f);
                    float crossingDistance = Mathf.Lerp(previousDistance, distance, crossingTime);
                    Vector3 crossingPoint = Vector3.Lerp(
                        previousPlayerPosition, playerPosition, crossingTime);
                    float sideDistance = Mathf.Abs(Vector3.Dot(crossingPoint - start, right));

                    if (crossingDistance <= length && sideDistance <= width * 0.5f)
                    {
                        if (crossingDistance <= length && sideDistance <= width * 0.5f)
                        {
                            hit = true;

                            if (tryConsumeHit == null || tryConsumeHit())
                            {
                                _player.OnHit(this, damage);
                            }
                        }
                    }
                }

                previousPlayerPosition = playerPosition;
            }

            if (trail != null)
            {
                trail.emitting = false;

                foreach (MeshRenderer blade in sword.GetComponentsInChildren<MeshRenderer>())
                {
                    blade.enabled = false;
                }

                await UniTask.Delay(TimeSpan.FromSeconds(trail.time),
                    delayTiming: PlayerLoopTiming.LastUpdate, cancellationToken: token);
            }
        }
        finally
        {
            if (preview != null) Destroy(preview.gameObject);
            if (sword != null) Destroy(sword);
        }
    }

    private async UniTask MoveBossTask(Vector3 destination, float duration, CancellationToken token)
    {
        Vector3 start = _rb.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, token);
            if (!CanFight) return;

            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            _rb.position = Vector3.Lerp(start, destination, t);
        }
    }


    private void OnDisable()
    {
        ResetBattleState();
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public void ResetBattleState()
    {
        foreach (CEspoirBossFxPlayer fx in _activeSlamImpacts)
        {
            if (fx != null) fx.Stop();
        }

        if (_animator != null && _animator.isActiveAndEnabled)
        {
            _animator.ResetTrigger(BackstepSlamTrigger);

            if (_animator.IsInTransition(0) ||
                _animator.GetCurrentAnimatorStateInfo(0).fullPathHash != IdleState)
            {
                _animator.Play(IdleState, 0, 0f);
            }
        }

        _isRage = false;
        _mainCounter = 0;
        _nextMainIsCross = true;

        _activeSlamImpacts.Clear();
        CancellationTokenSource source = _cts;
        _cts = null;

        if (source == null) return;

        source.Cancel();
        source.Dispose();
    }
}