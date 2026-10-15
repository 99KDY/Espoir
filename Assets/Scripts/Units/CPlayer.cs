using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CPlayer : CUnit
{
    [Header("이동 설정")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private LayerMask _groundLayer;

    [Header("입력 설정")]
    [SerializeField] private InputActionReference _moveClick;
    [SerializeField] private InputActionReference _pointerPosition;

    private Vector3 _targetPosition;

    private void OnEnable()
    {
        _moveClick.action.Enable();
        _pointerPosition.action.Enable();
    }

    private void OnDisable()
    {
        _moveClick.action.Disable();
        _pointerPosition.action.Disable();
    }

    private void Start()
    {
        _targetPosition = transform.position;
    }

    private void Update()
    {
        if (_moveClick.action.WasPressedThisFrame())
        {
            SetMoveTarget();
        }

        Move();
    }

    private void SetMoveTarget()
    {
        Vector2 mousePosition = _pointerPosition.action.ReadValue<Vector2>();
        Ray ray = _mainCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _groundLayer))
        {
            _targetPosition = hit.point;

            _targetPosition.y = transform.position.y;
        }
    }

    private void Move()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            _targetPosition,
            _moveSpeed * Time.deltaTime
        );
    }
}
