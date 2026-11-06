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

        if (_target == null)
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
