using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CNpcWander : MonoBehaviour
{
    private enum EWanderState
    {
        Pause,
        Moving,
    }

    private enum EMoveMode
    {
        Idle = 0,
        Walk = 1,
        Run = 2,
    }

    [Header("연결")]
    [SerializeField] private Animator _animator;
    [SerializeField] private CTalkNpc _talkNpc;

    [Header("이동 구역")]
    [SerializeField] private Transform _area;
    [SerializeField] private Vector2 _areaSize = new Vector2(64f, 3.4f);
    [SerializeField, Min(0f)] private float _heightTolerance = 0.3f;
    [SerializeField] private bool _startAtRandomPoint = true;

    [Header("이동 속도 (m/s)")]
    [SerializeField, Min(0.01f)] private float _walkSpeed = 0.36f;
    [SerializeField, Min(0.01f)] private float _runSpeed = 2f;
    [SerializeField, Range(0f, 1f)] private float _runChance = 0.8f;
    [SerializeField] private Vector2 _runDistance = new Vector2(6f, 24f);
    [SerializeField] private Vector2 _walkDistance = new Vector2(3f, 6f);

    [Header("멈춤 (초)")]
    [SerializeField, Range(0f, 1f)] private float _longPauseChance = 0.6f;
    [SerializeField] private Vector2 _longPause = new Vector2(2f, 6f);
    [SerializeField] private Vector2 _shortPause = new Vector2(0.6f, 1.4f);
    [SerializeField] private Vector2 _startPause = new Vector2(0.5f, 2.5f);

    [Header("방향")]
    [SerializeField, Range(0f, 180f)] private float _turnInPlaceAngle = 31.5f;
    [SerializeField, Min(0f)] private float _turnSharpness = 5f;

    [Header("동작")]
    [SerializeField, Min(0.01f)] private float _walkAnimSpeed = 0.25f;
    [SerializeField, Min(0.01f)] private float _runAnimSpeed = 1.38f;
    [SerializeField, Min(0f)] private float _runAnimThreshold = 1f;

    [Header("바닥 접지")]
    [SerializeField] private LayerMask _groundMask;
    [SerializeField, Min(0.01f)] private float _groundCheckDistance = 0.5f;

    private static readonly int _MOVE_MODE_HASH = Animator.StringToHash("MoveMode");
    private static readonly int _MOVE_RATE_HASH = Animator.StringToHash("MoveRate");

    private const float _ACCEL_RATE = 2.6f;
    private const float _DECEL_RATE = 4f;
    private const float _ARRIVE_DISTANCE = 0.35f;
    private const float _SLOW_DISTANCE = 2.2f;
    private const float _SLOW_MIN = 0.35f;
    private const float _BLOCKED_PAUSE = 0.4f;
    private const int _PICK_TRIES = 24;
    private const float _SAMPLE_RADIUS = 0.6f;

    private NavMeshAgent _agent;
    private Transform _model;
    private float _modelBaseY;
    private EWanderState _state = EWanderState.Pause;
    private float _pauseTime;
    private Vector3 _target;
    private bool _isRunning;
    private float _speed;
    private float _yaw;
    private float _targetYaw;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;

        if (_animator == null || _area == null)
        {
            CLogger.Error($"{name}: Animator와 이동 구역을 연결해주세요.");
            enabled = false;
            return;
        }

        _model = _animator.transform;
        _modelBaseY = _model.localPosition.y;
    }

    private void Start()
    {
        _yaw = transform.eulerAngles.y;
        _targetYaw = _yaw;

        if (_startAtRandomPoint && _agent.isOnNavMesh && TryPickPoint(out Vector3 point))
        {
            _agent.Warp(point);
            _yaw = Random.Range(0f, 360f);
            _targetYaw = _yaw;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        BeginPause(Random.Range(_startPause.x, _startPause.y));
    }

    private void OnDisable()
    {
        _speed = 0f;
        if (_animator != null) SetMove(EMoveMode.Idle, 1f);
    }

    private void Update()
    {
        if (!_agent.isOnNavMesh || IsMovementPaused())
        {
            _speed = 0f;
            _yaw = transform.eulerAngles.y;
            _targetYaw = _yaw;
            SetMove(EMoveMode.Idle, 1f);
            return;
        }

        float dt = Time.deltaTime;
        float targetSpeed = 0f;
        bool isTurning = false;

        if (_state == EWanderState.Pause)
        {
            _pauseTime -= dt;
            if (_pauseTime <= 0f) PickDestination();
        }

        if (_state == EWanderState.Moving)
        {
            Vector3 toTarget = _target - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (distance < _ARRIVE_DISTANCE)
            {
                Vector2 pause = Random.value < _longPauseChance ? _longPause : _shortPause;
                BeginPause(Random.Range(pause.x, pause.y));
            }

            else
            {
                _targetYaw = Quaternion.LookRotation(toTarget).eulerAngles.y;
                isTurning = Mathf.Abs(Mathf.DeltaAngle(_yaw, _targetYaw)) > _turnInPlaceAngle;
                float speed = _isRunning ? _runSpeed : _walkSpeed;
                targetSpeed = isTurning ? 0f : speed * Mathf.Min(1f, _SLOW_MIN + distance / _SLOW_DISTANCE);
            }
        }

        float rate = targetSpeed > _speed ? _ACCEL_RATE : _DECEL_RATE;
        _speed += (targetSpeed - _speed) * Mathf.Min(1f, dt * rate);

        _yaw = Mathf.LerpAngle(_yaw, _targetYaw, Mathf.Min(1f, dt * _turnSharpness));
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

        if (_state == EWanderState.Moving && _speed > 0.02f) MoveForward(dt);

        UpdateAnimation(isTurning);
    }

    private void LateUpdate()
    {
        Vector3 origin = transform.position + Vector3.up * _groundCheckDistance;
        Vector3 local = _model.localPosition;
        local.y = _modelBaseY;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, _groundCheckDistance * 2f, _groundMask,
            QueryTriggerInteraction.Ignore))
        {
            local.y += hit.point.y - transform.position.y;
        }

        _model.localPosition = local;
    }

    private bool IsMovementPaused()
    {
        if (_talkNpc != null && _talkNpc.IsTalking) return true;

        CGameManager manager = CGameManager.Instance;
        return manager != null && manager.GameState != EGameState.Idle;
    }

    private void MoveForward(float dt)
    {
        Vector3 forward = transform.forward;
        Vector3 before = transform.position;
        float wanted = _speed * dt;
        _agent.Move(forward * wanted);

        Vector3 moved = transform.position - before;
        moved.y = 0f;
        if (moved.magnitude < wanted * 0.5f) BeginPause(_BLOCKED_PAUSE);
    }

    private void UpdateAnimation(bool isTurning)
    {
        if (_speed > _runAnimThreshold) SetMove(EMoveMode.Run, Mathf.Max(0.7f, _speed / _runAnimSpeed));
        else if (_speed > 0.05f) SetMove(EMoveMode.Walk, Mathf.Max(0.55f, _speed / _walkAnimSpeed));
        else if (isTurning) SetMove(EMoveMode.Walk, 0.7f);
        else SetMove(EMoveMode.Idle, 1f);
    }

    private void SetMove(EMoveMode mode, float rate)
    {
        _animator.SetInteger(_MOVE_MODE_HASH, (int)mode);
        _animator.SetFloat(_MOVE_RATE_HASH, rate);
    }

    private void BeginPause(float duration)
    {
        _state = EWanderState.Pause;
        _pauseTime = duration;
    }

    private void PickDestination()
    {
        bool isRunning = Random.value < _runChance;
        Vector2 range = isRunning ? _runDistance : _walkDistance;
        Vector3 position = transform.position;

        for (int i = 0; i < _PICK_TRIES; i++)
        {
            if (!TryPickPoint(out Vector3 point)) continue;

            Vector3 offset = point - position;
            offset.y = 0f;
            float distance = offset.magnitude;

            if (distance < range.x || distance > range.y) continue;
            if (NavMesh.Raycast(position, point, out NavMeshHit _, _agent.areaMask)) continue;

            _target = point;
            _isRunning = isRunning;
            _state = EWanderState.Moving;
            return;
        }

        BeginPause(Random.Range(_shortPause.x, _shortPause.y));
    }

    private bool TryPickPoint(out Vector3 point)
    {
        Vector3 local = new Vector3((Random.value - 0.5f) * _areaSize.x, 0f, (Random.value - 0.5f) * _areaSize.y);
        Vector3 candidate = _area.position + _area.rotation * local;

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, _SAMPLE_RADIUS, _agent.areaMask) &&
            Mathf.Abs(hit.position.y - _area.position.y) <= _heightTolerance)
        {
            point = hit.position;
            return true;
        }

        point = candidate;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (_area == null) return;

        Gizmos.color = new Color(0.4f, 0.9f, 0.6f, 0.8f);
        Gizmos.matrix = Matrix4x4.TRS(_area.position, _area.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(_areaSize.x, 0.1f, _areaSize.y));
    }
}
