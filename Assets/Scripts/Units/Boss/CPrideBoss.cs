using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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
    [SerializeField] private GameObject _swordPrefab;

    [Header("이펙트")]
    [SerializeField] private CEspoirBossFxPlayer _slamImpactPrefab;

    [Header("인식범위 (확인용)")]
    [SerializeField] private float _battleStartRadius = 20f;
    [SerializeField] private float _battleEndRadius = 25f;
    [SerializeField] private float _attackRadius = 3f;

    [Header("평타 이동")]
    [SerializeField] private LayerMask _groundMask = 1 << 3; // Ground
    [SerializeField] private LayerMask _moveObstacleMask = (1 << 0) | (1 << 6) | (1 << 7);

    [Header("디버그 로그")]
    [SerializeField] private bool _enableLog = false;


    private Rigidbody _rb;
    private CapsuleCollider _collider;

    private CancellationTokenSource _cts;
    private readonly List<CEspoirBossFxPlayer> _activeSlamImpacts = new List<CEspoirBossFxPlayer>();

    private bool CanFight => _player != null && _previewPattern != null
                             && HpCurrent > 0f && _player.HpCurrent > 0f;

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
        _collider = GetComponent<CapsuleCollider>();
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
        try
        {
            while (!token.IsCancellationRequested)
            {
                await UniTask.Delay(1500, delayTiming: PlayerLoopTiming.LastUpdate,
                    cancellationToken: token);

                if (!CanFight
                    || GetPlayerDistanceSquared() > _battleEndRadius * _battleEndRadius)
                {
                    return;
                }

                await RunNormalAttack(token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // 전투 종료로 진행 중인 공격이 취소됨.
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
        if (_enableLog) CLogger.Log($"보스와의 거리 - {toPlayer.magnitude}");


        // 2번: 4m 이내에서 50%.
        if (distanceSquared > 0.0001f && distanceSquared <= 4f * 4f && Random.value < 0.5f)
        {
            if (_enableLog) CLogger.Log("현재 거리 4m 이내, 백스탭 시도함");
            Vector3 forward = toPlayer.normalized;
            Vector3 backstepPoint = _rb.position - forward * 2f;

            if (TryGetMoveDestination(backstepPoint, out _)
                && !_rb.SweepTest(-forward, out _, 2f, QueryTriggerInteraction.Ignore))
            {
                _rb.rotation = Quaternion.LookRotation(forward);
                Vector3 attackPoint = _rb.position + forward * 2f;

                await UniTask.WhenAll(
                    SlamTask(attackPoint, 1f, 0.3f, 2f, token),
                    BackstepTask(-forward, token));
                return;
            }
        }

        // 3번: 10m 이상에서 50%, 15m 이상에서 75%.
        if (distanceSquared >= 10f * 10f)
        {
            float chaseChance = distanceSquared >= 15f * 15f ? 0.75f : 0.5f;
            if (_enableLog) CLogger.Log($"현재 거리 10m 이상, 확률 체크 {chaseChance}");
            Vector3 forward = toPlayer.normalized;
            Vector3 behindPlayer = _player.transform.position + forward * 2.5f;
            behindPlayer.y = _rb.position.y;

            if (Random.value < chaseChance && TryGetMoveDestination(behindPlayer, out Vector3 destination))
            {
                if (_enableLog) CLogger.Log("현재 거리 10m 이후, 돌진 시도함");
                _rb.velocity = Vector3.zero;
                _rb.position = destination;
                _rb.rotation = Quaternion.LookRotation(-forward);

                await SlamTask(_player.transform.position, 1f, 0.3f, 2f, token);
                return;
            }
        }

        // 1번: 기본 낙하와 이동 불가 시 대체 공격.
        await SlamTask(_player.transform.position, 2f, 0.3f, _attackRadius, token);
    }

    private async UniTask BackstepTask(Vector3 direction, CancellationToken token)
    {
        // 0.2초 동안 뒤로 이동한다. Y축 속도는 중력에 맡긴다.
        _rb.velocity = direction * 10f + Vector3.up * _rb.velocity.y;

        try
        {
            await UniTask.Delay(200, delayTiming: PlayerLoopTiming.LastUpdate,
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

    private bool TryGetMoveDestination(Vector3 point, out Vector3 destination)
    {
        destination = point;
        if (!TryGetGround(point, out RaycastHit ground)) return false;

        destination.y = ground.point.y + 0.02f;

        Bounds bounds = _collider.bounds;
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        float halfSegment = Mathf.Max(0f, bounds.extents.y - radius);
        Vector3 center = destination + (bounds.center - _rb.position);
        Vector3 verticalOffset = Vector3.up * halfSegment;

        return !Physics.CheckCapsule(center - verticalOffset, center + verticalOffset,
            radius, _moveObstacleMask, QueryTriggerInteraction.Ignore);
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

    private async UniTask SlamTask(Vector3 attackPoint, float readyTime, float fallingTime, float radius, CancellationToken token)
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

            if (offset.sqrMagnitude <= radius * radius)
            {
                _player.OnHit(this,
                    new SDamageInfo(100f, false, ERatioSwitch.Constant, false, null));
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
        // 0번 GroundShockDistort는 섬광과 같은 높이에서 시작한다.
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

        _activeSlamImpacts.Clear();
        CancellationTokenSource source = _cts;
        _cts = null;

        if (source == null) return;

        source.Cancel();
        source.Dispose();
    }
}