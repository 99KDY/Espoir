using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CCompanionFollow : MonoBehaviour
{
    [Header("따라가기 설정")]
    [SerializeField] private Transform _target;
    [SerializeField] private Animator _animator;
    [SerializeField, Min(0.05f)] private float _pathInterval = 0.2f;
    [SerializeField, Min(0f)] private float _resumeMargin = 0.5f;
    [SerializeField, Min(0.1f)] private float _targetSearchRadius = 1.5f;

    [Header("바닥 접지 설정")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField, Min(0.01f)] private float _groundCheckDistance = 0.5f;
    [SerializeField] private float _footOffset;

    private static readonly int _IS_MOVING_HASH = Animator.StringToHash("IsMoving");

    private NavMeshAgent _agent;
    private float _nextPathTime;
    private bool _isHolding;

    /// <summary>
    /// 따라가기를 잠시 멈추거나 다시 시작합니다 (대화 중 제자리에 서 있게 할 때).
    /// </summary>
    public void SetHold(bool isHolding)
    {
        _isHolding = isHolding;
        if (isHolding && _agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }
    }

    /// <summary>
    /// 내비 위 지정한 자리로 바로 옮깁니다 (불러오기 뒤 자리 맞춤).
    /// </summary>
    public void PlaceAt(Vector3 position, Quaternion rotation)
    {
        if (_agent != null && _agent.isOnNavMesh) _agent.Warp(position);
        else transform.position = position;
        transform.rotation = rotation;
    }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        if (_animator == null)
        {
            CLogger.Error($"{name}: 동료 Animator 연결을 확인해주세요.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (!_agent.isOnNavMesh)
        {
            _animator.SetBool(_IS_MOVING_HASH, false);
            return;
        }

        if (_target == null || _isHolding)
        {
            _agent.isStopped = true;
            _animator.SetBool(_IS_MOVING_HASH, false);
            return;
        }

        Vector3 offset = _target.position - transform.position;
        offset.y = 0f;
        float distance = offset.magnitude;

        if (distance <= _agent.stoppingDistance)
        {
            _agent.isStopped = true;
        }

        else if (distance > _agent.stoppingDistance + _resumeMargin)
        {
            _agent.isStopped = false;
        }

        if (!_agent.isStopped && Time.time >= _nextPathTime)
        {
            _nextPathTime = Time.time + _pathInterval;

            NavMeshQueryFilter filter = new NavMeshQueryFilter
            {
                agentTypeID = _agent.agentTypeID,
                areaMask = _agent.areaMask
            };

            if (NavMesh.SamplePosition(_target.position, out NavMeshHit hit, _targetSearchRadius, filter))
            {
                _agent.SetDestination(hit.position);
            }
        }

        _animator.SetBool(_IS_MOVING_HASH, _agent.velocity.sqrMagnitude > 0.01f);
    }

    private void LateUpdate()
    {
        if (!_agent.isOnNavMesh)
        {
            return;
        }

        Vector3 origin = transform.position + Vector3.up * _groundCheckDistance;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
            _groundCheckDistance * 2f, _groundLayer, QueryTriggerInteraction.Ignore))
        {
            Vector3 position = _animator.transform.position;
            position.y = hit.point.y - _footOffset;
            _animator.transform.position = position;
        }
    }

    private void OnDisable()
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
        }

        if (_animator != null)
        {
            _animator.SetBool(_IS_MOVING_HASH, false);
        }
    }
}
