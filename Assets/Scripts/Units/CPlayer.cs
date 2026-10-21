using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class CPlayer : CUnit
{
    [Serializable]
    private struct SAttackHit
    {
        [Range(0f, 1f)] public float time;
        [Min(0f)] public float hitStopDuration;
    }

    [Serializable]
    private struct SAttackStep
    {
        public string stateName;
        public string recoverStateName;
        [Min(0f)] public float moveDistance;
        [Range(0f, 1f)] public float moveStart;
        [Range(0f, 1f)] public float moveEnd;
        public SAttackHit[] hits;
    }

    [Header("이동 설정")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 720f;

    [SerializeField] private Camera _mainCamera;
    [SerializeField] private LayerMask _groundLayer;

    [Header("대쉬 설정")]
    [SerializeField] private float _dashDistance = 3f;
    [SerializeField, Min(0.01f)] private float _dashDuration = 0.3f;
    [SerializeField] private float _dashCooldown = 1f;

    [Header("공격 설정")]
    [SerializeField] private CPlayerAttack _playerAttack;
    [SerializeField, Min(0f)] private float _attackRotationSpeed = 1080f;
    [Tooltip("이동 구간과 타격 시점은 각 공격 애니메이션의 진행률(0~1)입니다.")]
    [SerializeField] private SAttackStep[] _attackSteps;
    [SerializeField, Min(0f)] private float _comboGraceTime = 0.8f;

    [Header("입력 설정")]
    [SerializeField] private InputActionReference _moveClick;
    [SerializeField] private InputActionReference _pointerPosition;
    [SerializeField] private InputActionReference _attackInput;
    [SerializeField] private InputActionReference _dashInput;

    [Header("애니메이션 설정")]
    [SerializeField] private Animator _animator;

    private static readonly int _IS_MOVING_HASH = Animator.StringToHash("IsMoving");
    private static readonly int _ATTACK_HASH = Animator.StringToHash("Attack");
    private static readonly int _DASH_HASH = Animator.StringToHash("Dash");
    private static readonly int _COMBO_HASH = Animator.StringToHash("Combo");

    private int[] _attackStateHashes;
    private int[] _recoverStateHashes;

    private CharacterController _characterController;
    private float _verticalSpeed;
    private Vector3 _targetPosition;
    private bool _isAttacking;
    private bool _hasEnteredAttack;
    private int _nextHitIndex;
    private float _attackMoveProgress;
    private Vector3 _attackDirection;
    private Vector3 _queuedAttackDirection;
    private int _comboIndex;
    private bool _hasQueuedCombo;
    private bool _isHitStopped;
    private float _animatorSpeed;
    private CancellationTokenSource _hitStopCancellation;

    private Vector3 _dashDirection;
    private float _dashTimeRemaining;
    private float _dashCooldownRemaining;
    private bool _isDashing;
    private bool _hasEnteredDash;

    private void OnEnable()
    {
        _hitStopCancellation = new CancellationTokenSource();
        _moveClick.action.Enable();
        _pointerPosition.action.Enable();
        _attackInput.action.Enable();
        _dashInput.action.Enable();
    }

    private void OnDisable()
    {
        _hitStopCancellation?.Cancel();
        _hitStopCancellation?.Dispose();
        _hitStopCancellation = null;

        if (_isHitStopped)
        {
            _animator.speed = _animatorSpeed;
            _isHitStopped = false;
        }

        _moveClick.action.Disable();
        _pointerPosition.action.Disable();
        _attackInput.action.Disable();
        _dashInput.action.Disable();
    }

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _targetPosition = transform.position;

        if (!HasValidAttackSettings())
        {
            CLogger.Error($"{name}: 연격별 상태 이름, 이동 구간과 오름차순 타격 시점을 확인해주세요.");
            enabled = false;
            return;
        }

        _attackStateHashes = new int[_attackSteps.Length];
        _recoverStateHashes = new int[_attackSteps.Length];

        for (int i = 0; i < _attackSteps.Length; i++)
        {
            _attackStateHashes[i] = Animator.StringToHash(_attackSteps[i].stateName);

            if (!string.IsNullOrWhiteSpace(_attackSteps[i].recoverStateName))
            {
                _recoverStateHashes[i] = Animator.StringToHash(_attackSteps[i].recoverStateName);
            }
        }
    }

    private bool HasValidAttackSettings()
    {
        if (_attackSteps == null || _attackSteps.Length == 0)
        {
            return false;
        }

        foreach (SAttackStep step in _attackSteps)
        {
            if (string.IsNullOrWhiteSpace(step.stateName) ||
                !_animator.HasState(0, Animator.StringToHash(step.stateName)) ||
                step.moveDistance < 0f || step.moveStart < 0f || step.moveEnd > 1f ||
                step.moveStart >= step.moveEnd || step.hits == null || step.hits.Length == 0)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(step.recoverStateName) &&
                !_animator.HasState(0, Animator.StringToHash(step.recoverStateName)))
            {
                return false;
            }

            float previousTime = -1f;

            foreach (SAttackHit hit in step.hits)
            {
                if (hit.time < 0f || hit.time > 1f || hit.time <= previousTime || hit.hitStopDuration < 0f)
                {
                    return false;
                }

                previousTime = hit.time;
            }
        }

        return true;
    }

    private void Update()
    {
        ApplyGravity();
        _dashCooldownRemaining = Mathf.Max(_dashCooldownRemaining - Time.deltaTime, 0f);

        if (_isAttacking)
        {
            UpdateAttackState();
            return;
        }

        if (_isDashing)
        {
            UpdateDashState();
            return;
        }

        if (_dashInput.action.WasPressedThisFrame() &&
            _dashCooldownRemaining <= 0f)
        {
            StartDash();
            return;
        }

        if (_attackInput.action.WasPressedThisFrame())
        {
            StartAttack(CanContinueCombo() ? _COMBO_HASH : _ATTACK_HASH);
            return;
        }

        if (_moveClick.action.IsPressed())
        {
            SetMoveTarget();
        }

        Move();
    }

    private void StartAttack(int triggerHash)
    {
        _isAttacking = true;
        _hasEnteredAttack = false;
        _nextHitIndex = 0;
        _comboIndex = -1;
        _hasQueuedCombo = false;
        _attackMoveProgress = 0f;
        _attackDirection = GetAttackDirection();
        _targetPosition = transform.position;

        _animator.SetBool(_IS_MOVING_HASH, false);
        _animator.SetTrigger(triggerHash);
    }

    private bool CanContinueCombo()
    {
        AnimatorStateInfo currentState = _animator.GetCurrentAnimatorStateInfo(0);

        if (GetStateIndex(_recoverStateHashes, currentState.shortNameHash) < 0)
        {
            return false;
        }

        return currentState.normalizedTime * currentState.length <= _comboGraceTime;
    }

    private void UpdateAttackState()
    {
        AnimatorStateInfo currentState = _animator.GetCurrentAnimatorStateInfo(0);
        int comboIndex = GetStateIndex(_attackStateHashes, currentState.shortNameHash);

        if (comboIndex >= 0 && comboIndex != _comboIndex)
        {
            if (_hasQueuedCombo)
            {
                _attackDirection = _queuedAttackDirection;
            }

            _comboIndex = comboIndex;
            _nextHitIndex = 0;
            _attackMoveProgress = 0f;
            _hasQueuedCombo = false;
        }

        if (_attackInput.action.WasPressedThisFrame() && comboIndex >= 0 &&
            comboIndex < _attackStateHashes.Length - 1 && !_animator.IsInTransition(0))
        {
            _queuedAttackDirection = GetAttackDirection();
            _hasQueuedCombo = true;
            _animator.SetTrigger(_COMBO_HASH);
        }

        if (_isHitStopped)
        {
            return;
        }

        if (comboIndex >= 0)
        {
            _hasEnteredAttack = true;

            RotateAttack();
            SAttackStep step = _attackSteps[comboIndex];
            float moveProgress = Mathf.InverseLerp(step.moveStart, step.moveEnd, currentState.normalizedTime);
            float distance = Mathf.Max(moveProgress - _attackMoveProgress, 0f) * step.moveDistance;

            _characterController.Move(transform.forward * distance);
            _attackMoveProgress = moveProgress;
            _targetPosition = transform.position;

            while (_nextHitIndex < step.hits.Length &&
                   currentState.normalizedTime >= step.hits[_nextHitIndex].time)
            {
                SAttackHit hit = step.hits[_nextHitIndex];
                _nextHitIndex++;

                if (_playerAttack.Attack(this) && hit.hitStopDuration > 0f)
                {
                    HitStop(hit.hitStopDuration).Forget();
                    break;
                }
            }
        }

        else
        {
            _comboIndex = -1;

            bool isRecovering = GetStateIndex(_recoverStateHashes, currentState.shortNameHash) >= 0;

            if (!_animator.IsInTransition(0) && (_hasEnteredAttack || !isRecovering))
            {
                _isAttacking = false;
                _animator.ResetTrigger(_COMBO_HASH);
            }

            else
            {
                RotateAttack();
            }
        }
    }

    private Vector3 GetAttackDirection()
    {
        if (TryGetPointerGroundPosition(out Vector3 position))
        {
            Vector3 direction = position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
            {
                return direction.normalized;
            }
        }

        return transform.forward;
    }

    private void RotateAttack()
    {
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(_attackDirection),
            _attackRotationSpeed * Time.deltaTime
        );
    }

    private async UniTask HitStop(float duration)
    {
        _isHitStopped = true;
        _animatorSpeed = _animator.speed;
        _animator.speed = 0f;

        bool isCanceled = await UniTask.Delay(
            TimeSpan.FromSeconds(duration),
            ignoreTimeScale: true,
            cancellationToken: _hitStopCancellation.Token
        ).SuppressCancellationThrow();

        if (isCanceled)
        {
            return;
        }

        _animator.speed = _animatorSpeed;
        _isHitStopped = false;
    }

    private int GetStateIndex(int[] stateHashes, int stateHash)
    {
        for (int i = 0; i < stateHashes.Length; i++)
        {
            if (stateHashes[i] != 0 && stateHashes[i] == stateHash)
            {
                return i;
            }
        }

        return -1;
    }

    private void StartDash()
    {
        _isDashing = true;
        _hasEnteredDash = false;
        _dashCooldownRemaining = _dashCooldown;
        _targetPosition = transform.position;

        _dashDirection = transform.forward;
        _dashTimeRemaining = _dashDuration;

        _animator.SetBool(_IS_MOVING_HASH, false);
        _animator.SetTrigger(_DASH_HASH);
    }

    private void UpdateDashState()
    {
        float dashSpeed = _dashDistance / _dashDuration;

        float moveTime = Mathf.Min(Time.deltaTime, _dashTimeRemaining);
        _characterController.Move(_dashDirection * (dashSpeed * moveTime));
        _dashTimeRemaining = Mathf.Max(_dashTimeRemaining - Time.deltaTime, 0f);

        AnimatorStateInfo currentState = _animator.GetCurrentAnimatorStateInfo(0);

        if (currentState.shortNameHash == _DASH_HASH)
        {
            _hasEnteredDash = true;
        }

        else if (_hasEnteredDash &&
                 !_animator.IsInTransition(0) &&
                 _dashTimeRemaining <= 0f)
        {
            _isDashing = false;
            _targetPosition = transform.position;
        }
    }

    private void SetMoveTarget()
    {
        if (TryGetPointerGroundPosition(out Vector3 position))
        {
            _targetPosition = position;
        }
    }

    private bool TryGetPointerGroundPosition(out Vector3 position)
    {
        Vector2 mousePosition = _pointerPosition.action.ReadValue<Vector2>();
        Ray ray = _mainCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _groundLayer))
        {
            position = hit.point;
            return true;
        }

        position = default;
        return false;
    }

    private void ApplyGravity()
    {
        _verticalSpeed += Physics.gravity.y * Time.deltaTime;
        CollisionFlags collision = _characterController.Move(Vector3.up * (_verticalSpeed * Time.deltaTime));

        if ((collision & CollisionFlags.Below) != 0 && _verticalSpeed < 0f)
        {
            _verticalSpeed = 0f;
        }
    }

    private void Move()
    {
        Vector3 previousPosition = transform.position;

        Vector3 targetDirection = _targetPosition - previousPosition;
        targetDirection.y = 0f;

        _characterController.Move(Vector3.ClampMagnitude(targetDirection, _moveSpeed * Time.deltaTime));

        Vector3 moveDirection = transform.position - previousPosition;
        moveDirection.y = 0f;
        bool isMoving = moveDirection != Vector3.zero;

        if (isMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime
            );
        }

        _animator.SetBool(_IS_MOVING_HASH, isMoving);
    }
}
