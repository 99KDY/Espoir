using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CTestEnemy : CEnemy
{

    #region 인스팩터
    [Header("Enemy Info")]
    [SerializeField] private string _name = "Skeleton";
    [SerializeField] private LayerMask _tartgetMask;


    [SerializeField] private float _moveSpeed = 3.0f;
    [SerializeField] private float _rotateSpeed = 10.0f;
    [SerializeField] private float _sightView = 8.0f;


    [Header("공격 타겟")]
    [SerializeField] private CPlayer _player;
    [SerializeField] private Transform _target;


    [SerializeField] private float _atkRange = 2.0f;

    [SerializeField] float _attackTimer = 0;
    
    [SerializeField] private bool _OnTarget = false;

    #endregion 

    private CPlayer Player => _player;

    private Vector3 _startPos;

    #region Animator
    [SerializeField] private Animator _animator;
    
    private int _hashMove = Animator.StringToHash("bMove");
    private int _hashAttack = Animator.StringToHash("tAttack");

    private bool _isAttack;
    private bool _isMove;
    #endregion



    protected override void Awake()
    {
        base.Awake();
        _target = Player.transform;
        _startPos = transform.position;
        _animator = GetComponent<Animator>();


    }

    protected override void Update()
    {
        // TODO : 플레이어를 쫓아가기
        OnTargetMove();



        // TODO : 주기적으로 공격하기
    }


    private void OnTargetMove()
    {
        if(_target == null)
        {
            return;
        }

        
        _OnTarget = true;

        Collider[] hit = Physics.OverlapSphere(transform.position, _sightView, _tartgetMask);

        Vector3 lastPos = transform.position;

        Vector3 toTarget = _target.position - transform.position;
        toTarget.y = 0.0f;

        _isMove = transform.position != lastPos;

        if(_OnTarget && toTarget.magnitude <= _sightView)
        {

            CLogger.Log($"목표 발견 : {_player.name} / 목표 거리 : {toTarget.magnitude}");

            transform.position = Vector3.Lerp
                (
                    lastPos,
                    _target.position,
                    _moveSpeed * Time.deltaTime
                );
            
            
            _animator.SetBool(_hashMove, _isMove);

        }

        if(toTarget.magnitude > _sightView)
        {
            _OnTarget = false;

            transform.position = Vector3.Lerp
                (
                    transform.position,
                    _startPos,
                    (1.5f * _moveSpeed) * Time.deltaTime
                );
            _animator.SetBool(_hashMove, _isMove);
        }

            
        

        transform.rotation = Quaternion.LookRotation( toTarget, Vector3.up );

    }

    private void OnAttack()
    {
        

        _isAttack = true;
        if(_isAttack)
        {
            _animator.SetTrigger(_hashAttack);
        }

    }

    private void SightView()
    {


    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _atkRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _sightView);

        if (_target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _target.position);
        }


    }


}
