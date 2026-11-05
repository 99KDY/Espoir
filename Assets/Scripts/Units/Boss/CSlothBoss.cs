
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using static UnityEngine.GraphicsBuffer;

public class CSlothBoss : CEnemy
{


    public enum ESlothBossState
    {
        Idle = 0,
        NormalAttack,
        SubAttack,
        MainAttack,
        Dead
    }



    #region 인스팩터
    [SerializeField] private CPlayer _player;

    [Header("Attack Player")]
    [SerializeField] private LayerMask _tartgetMask;
    [SerializeField] private bool _sightCheck = false;
    [SerializeField] private LayerMask _obtacleMask;
    [SerializeField] private Collider[] _hitCollider = new Collider[2];


    [Header("Boss State")]
    [SerializeField] private ESlothBossState _bossState;
    [SerializeField] private CPreviewPattern _previewPattern;

    [Header("Boss Info")]
    [SerializeField] private Animator _animator;
    [SerializeField] private float _rotateSpeed = 1000.0f;
    [SerializeField] private float _atkTime = 3.0f;
    [SerializeField] private float _normalAtkRange = 7.0f;
    [SerializeField] private float _detectedRange = 15.0f;



    [Header("훨윈드")]
    [SerializeField] private float _whirlRound = 10f;







    #endregion

    #region Animator 변수

    private int _fisrtNorm = Animator.StringToHash("nAttack01");
    private int _secondNorm = Animator.StringToHash("nAttack02");
    private int _thirdNorm = Animator.StringToHash("nAttack03");

    private int _firstSub = Animator.StringToHash("bAttack01");
    private int _secondSub = Animator.StringToHash("bAttack02");
    private int _thirdSub = Animator.StringToHash("bAttack03");
    private int _forthSub = Animator.StringToHash("bAttack04");

    private int _firstMain = Animator.StringToHash("pAttack01");
    private int _secondMain = Animator.StringToHash("pAttack02");
    private int _thirdMain = Animator.StringToHash("pAttack03");



    #endregion



    #region 내부변수

    private ESlothBossState _startState;
    private ESlothBossState _currentState;


    private CSoundManager _soundManager;
    private Transform _target;

    private float _atkTimer;


    private int normalAtk;
    private int subAtk;
    private int mainAtk;
    private bool _onAttack;


    private bool _isStop;
    private bool _isNormal;
    private bool _isSub;
    private bool _isMain;
    private bool _isDead;



    #endregion


    protected override void Awake()
    {
        base.Awake();

        if (_player == null)
        {
            _player = FindFirstObjectByType<CPlayer>();

            if (_player == null)
            {
                CLogger.Error("플레이어 찾을 수 없음");
            }

        }

        if(_soundManager == null)
        {
            _soundManager = FindFirstObjectByType<CSoundManager>();
            if(_soundManager == null)
            {
                CLogger.Warn("사운드 매니저 찾을 수 없음");
            }

        }

      

        _target = _player.transform;
        _previewPattern = this.GetComponent<CPreviewPattern>();
        _animator = this.GetComponent<Animator>();

    }

    protected override void Start()
    {
        base.Start();
        _startState = ESlothBossState.Idle;
        _currentState = _bossState;

    }


    protected override void Update()
    {
        base.Update();



    }

    private void FixedUpdate()
    {
        LookAtPlayer();

        if(_atkTimer > _atkTime)
        {
            BossState();

            _atkTimer = 0f;
        }
        else
        {
            _atkTimer += Time.fixedDeltaTime;        
        }
    }


    private void NormalAttackPattern()
    {
        if(_isNormal == false)
        {
            return;
        }

        normalAtk = Random.Range(0, 3);

        CLogger.Log($"Normal Attack Pattern : {normalAtk}");

        switch (normalAtk)
        {
            case 0:
                _animator.SetTrigger(_fisrtNorm);
                DoubleHookAttack();
                break;

            case 1:
                _animator.SetTrigger(_secondNorm);

                break;

            case 2:
                _animator.SetTrigger(_thirdNorm);

                break;

        }

    }


    private void SubAttackPattern()
    {
        if(_isSub == false)
        {
            return;
        }

        subAtk = Random.Range(0, 4);


        CLogger.Log($"Sub Attack Pattern : {subAtk}");
        switch (subAtk)
        {
            case 0:

                _animator.SetTrigger(_firstSub);
                
                break;

            case 1:

                _animator.SetTrigger(_secondSub);
                
                break;

            case 2:

                _animator.SetTrigger(_thirdSub);
                
                break;

            case 3:
                DonutAttack();
                _animator.SetTrigger(_forthSub);
                
                break;
        }
    }

    private void MainAttackPattern()
    {
        if (_isMain == false)
        {
            return;
        }

        mainAtk = Random.Range(0, 3);

        CLogger.Log($"Main Attack Pattern : {mainAtk}");

        switch (mainAtk)
        {
            case 0:
                _animator.SetTrigger(_firstMain);
                break;

            case 1:
                _animator.SetTrigger(_secondMain);
                break;

            case 2:
                _animator.SetTrigger(_thirdMain);
                break;
        }


    }



    private void BossState()
    {
        if (_player == null)
        {
            return;
        }

        if(_isStop == true)
        {
            return;
        }

        _onAttack = true;

        switch (_currentState)
        {
            case ESlothBossState.Idle:
                break;

            case ESlothBossState.NormalAttack:
                NormalAttackPattern();
                
                _isNormal = false;
                break;

            case ESlothBossState.SubAttack:
                SubAttackPattern();
                
                _isSub = false;
                break;

            case ESlothBossState.MainAttack:
                MainAttackPattern();
                
                _isMain = false;
                break;

            case ESlothBossState.Dead:
                break;
        }


    }


    private void LookAtPlayer()
    {
        Vector3 targer = new Vector3(_target.position.x, 0f, _target.position.z);

        Vector3 lookPlayer = targer - this.transform.position;

        float distance = lookPlayer.magnitude;

        if (distance <= _detectedRange)
        {
            Vector3 dir = lookPlayer.normalized;
            Quaternion lookRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, _rotateSpeed * Time.deltaTime);

            if (distance <= _normalAtkRange)
            {
                _isNormal = true;

                _currentState = ESlothBossState.NormalAttack;
            }

            else
            {
                _isNormal = false;
                _isSub = true;

                _currentState = ESlothBossState.SubAttack;
            }


        }

    }


    private void DoubleHookAttack()
    {

        int hit = NormalFindPlayer(6f);

        for (int i = 0; i < hit; i++)
        {
            if(_hitCollider == null)
            {
                continue;
            }

            CPlayer player = _hitCollider[i].GetComponent<CPlayer>();

            if (player == null || player.HpCurrent <= 0)
            {
                continue;
            }

            SDamageInfo damagePlayer = new SDamageInfo(this.Attack);
            float lastHp = player.HpCurrent;

            player.OnHit(this, damagePlayer);

            CLogger.Log($"{player.name}의 현재 체력 : {player.HpCurrent}");
        }



    }





    private void BackSpinAttack()
    {
        Vector3 startPos = this.transform.forward;
        Vector3 endPos = -this.transform.forward;


    }

    private void SlamUpperAttack()
    {

        int hit = NormalFindPlayer(7f);

        for (int i = 0; i < hit; i++)
        {
            if (_hitCollider == null)
            {
                continue;
            }

            CPlayer player = _hitCollider[i].GetComponent<CPlayer>();

            if (player == null || player.HpCurrent <= 0)
            {
                continue;
            }

            SDamageInfo damagePlayer = new SDamageInfo(this.Attack);
           
            player.OnHit(this, damagePlayer);

            CLogger.Log($"{player.name}의 현재 체력 : {player.HpCurrent}");
        }

    }

    #region whirlWind
    private async UniTask WhirlWindAttack()
    {
        if(_previewPattern != null)
        {
            _previewPattern.ShowDonutPattern(20, 15f, 7f);
        }

        await UniTask.Delay(1500);

        if (_previewPattern != null)
        {
            _previewPattern.ClearPreview();
        }

    }

    private void WhirlWindDamage()
    {

        //Vector3 offset = this.transform.right * 5.0f;

        //int hit = Physics.OverlapSphereNonAlloc(this.transform.position + offset, 5.0f, _hitCollider, _tartgetMask);

        int hit = SubFindPlayer(15f);

        for (int i = 0; i < hit; i++)
        {
            if (_hitCollider[i] == null)
            {
                continue;
            }

            CPlayer player = _hitCollider[i].GetComponent<CPlayer>();
            if(player == null || player.HpCurrent <= 0)
            {
                continue;
            }

            float distance = Vector3.Distance
                (
                    new Vector3 (this.transform.position.x , 0f , this.transform.position.z),
                    new Vector3( player.transform.position.x , 0f, player.transform.position.z)
                );

            if(distance >= 7f && distance <= 15f)
            {
                SDamageInfo damagePlayer = new SDamageInfo(this.Attack);
                
                player.OnHit(this, damagePlayer);


                CLogger.Log($"{player.name}의 현재 체력 : {player.HpCurrent}");
            }

        }
    }
    #endregion

    #region DonutAttack

    private async void DonutAttack()
    {
        if(_previewPattern != null)
        {
            _previewPattern.ShowDonutPattern(20, 15f, 10f);
        }
        await UniTask.Delay(1500);

        if (_previewPattern != null)
        {
            _previewPattern.ClearPreview();
        }
    }

    private void DonutDamage()
    {
        int hit = SubFindPlayer(15f);

        for (int i = 0; i < hit; i++)
        {
            if (_hitCollider[i] == null)
            {
                continue;
            }
            CPlayer player = _hitCollider[i].GetComponent<CPlayer>();
            if (player == null || player.HpCurrent <= 0)
            {
                continue;
            }

            float distance = Vector3.Distance
            (
                new Vector3(this.transform.position.x, 0f, this.transform.position.z),
                new Vector3(player.transform.position.x, 0f, player.transform.position.z)
            );

            if (distance >= 10f && distance <= 15f)
            {
                SDamageInfo damagePlayer = new SDamageInfo(this.Attack);

                player.OnHit(this, damagePlayer);
            }
        }
    }

    #endregion


    private int NormalFindPlayer(float radius)
    {
        Vector3 offset =  this.transform.up * 1.0f + this.transform.forward * 2f;


        int hit = Physics.OverlapSphereNonAlloc(this.transform.position + offset, radius, _hitCollider, _tartgetMask);

        for (int i = 0; i < _hitCollider.Length; i++)
        {
            if (_hitCollider[i] == null)
            {
                continue;
            }

            _target = _hitCollider[i].transform;


            break;
        }

        return hit;
    }


    private int SubFindPlayer(float radius)
    {
        

        int hit = Physics.OverlapSphereNonAlloc(this.transform.position, radius, _hitCollider, _tartgetMask);

        for (int i = 0; i < _hitCollider.Length; i++)
        {
            if (_hitCollider[i] == null)
            {
                continue;
            }

            _target = _hitCollider[i].transform;


            break;
        }

        return hit;
    }

}