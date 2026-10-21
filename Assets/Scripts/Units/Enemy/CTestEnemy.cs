using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CTestEnemy : CEnemy
{
    public enum EEnemyState
    {
        Idle = 0,
        Moving,
        Attack,
        
    }

    #region 인스팩터
    [Header("Enemy Info")]
    [SerializeField] private string _name = "Skeleton";
    [SerializeField] private LayerMask _tartgetMask;

    [SerializeField] private float _offsetDir = 1.5f;
    [SerializeField] private float _moveSpeed = 3.0f;
    [SerializeField] private float _rotateSpeed = 10.0f;
    [SerializeField] private float _sightView = 6.0f;
    [SerializeField] private float _forgetTime = 1.0f;

    [Header("공격 타겟")]
    [SerializeField] private CPlayer _player;
    [SerializeField] private Transform _target;


    [SerializeField] private float _atkRange = 2.0f;

    [SerializeField] float _attackTimer = 0f;
    [SerializeField] float _forgetTimer = 0f;
    
    [SerializeField] private bool _OnTarget = false;

    #endregion 

    private CPlayer Player => _player;

    private Vector3 _startPos;
    private Rigidbody _rb;

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
        _rb = this.GetComponent<Rigidbody>();

    }

    protected override void Update()
    {
        // TODO : 플레이어를 쫓아가기
        OnTargetMove();



        // TODO : 주기적으로 공격하기
       
        if(_attackTimer > 2.0f)
        {
            OnAttack();
        }

        _attackTimer += Time.deltaTime;

    }



    private void OnTargetMove()
    {
        if(_target == null && _isAttack == true)
        {
            return;
        }


        Vector3 target = new Vector3(_target.position.x , 0f, _target.position.z);
        
        Collider[] hit = Physics.OverlapSphere(transform.position, _sightView, _tartgetMask);

        transform.rotation = Quaternion.LookRotation(target, Vector3.up);

        Vector3 toTarget = target - transform.position;

        float distance = toTarget.magnitude;

        _OnTarget = distance <= _sightView ? true : false;
                
        if(_OnTarget == true)
        {
            FollowPlayer(toTarget, distance);
            _forgetTimer = 0.0f;
        }

        else
        {
            if(_forgetTimer < _forgetTime)
            {
                _forgetTimer += Time.deltaTime;
                if (_isMove)
                {
                    FollowPlayer(toTarget, distance);
                }
            }

            else
            {
                _isMove = false;

                transform.position = Vector3.MoveTowards(transform.position, _startPos, (1.5f *_moveSpeed) * Time.deltaTime);

                // TODO : 필요하면 패트롤 함수 넣어서 움직이게 해도 좋다.
            }

        }


            _animator.SetBool(_hashMove, _isMove);


    }


    private void FollowPlayer(Vector3 target, float dis)
    {
       

        if (dis > _offsetDir)
        {

            _isMove = true;

            Vector3 n_toTarget = target.normalized;

            Vector3 targetPos = target - (n_toTarget * _offsetDir);


            transform.position = Vector3.MoveTowards(transform.position, target, _moveSpeed * Time.deltaTime);

        }

        else
        {
            _isMove = false;
        }



    }


    private void OnAttack()
    {

        if(_isMove == true)
        {
            return;
        }

        Vector3 toTarget = _player.transform.position - this.transform.position;
        
        if(toTarget.magnitude <= 2.0f)
        {
            _isAttack = true;
            if (_isAttack)
            {
                // TODO : TakeDamage 공격로직 추가 예정
                _animator.SetTrigger(_hashAttack);

            }

        }

        else if(toTarget.magnitude > 2.0f)
        {
            _isAttack= false;
        }

        _attackTimer = 0f;

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
