
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private float _atkTime = 5.0f;
    [SerializeField] private float _normalAtkRange = 7.0f;
    [SerializeField] private float _detectedRange = 15.0f;



    [Header("Pattern Range")]
    [SerializeField] private float _whirlRange = 15f;
    [SerializeField] private float _whirlInner = 7f;
    [SerializeField] private float _donutRange = 12f;
    [SerializeField] private float _donutInner = 7f;
    [SerializeField] private float _areaAngle = 30f;
    [SerializeField] private int _divideArea = 6;





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
    private Vector3 _patternPos;

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


        _patternPos = _previewPattern.transform.position;
        _target = _player.transform;
        _previewPattern = this.GetComponentInChildren<CPreviewPattern>();
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

        _onAttack = true;

        normalAtk = Random.Range(0, 4);
        
        CLogger.Log($"Normal Attack Pattern : {normalAtk}");

        switch (normalAtk)
        {
            case 0:
                _animator.SetTrigger(_fisrtNorm);
                NormalAttackDelay().Forget();

                break;

            case 1:
                _animator.SetTrigger(_secondNorm);
                NormalAttackDelay().Forget();
                break;

            case 2:
                _animator.SetTrigger(_thirdNorm);
                NormalAttackDelay().Forget();
                break;

            case 3:
                _animator.SetTrigger(_firstSub);
                TwiceAttack().Forget();

                break;

        }
    }


    private void SubAttackPattern()
    {
        if(_isSub == false)
        {
            return;
        }

        subAtk = Random.Range(1, 4);

        CLogger.Log($"Sub Attack Pattern : {subAtk}");
        switch (subAtk)
        {
            case 1:
                WhirlWindAttack().Forget();

                break;

            case 2:
                AlternationAttack().Forget();

                break;

            case 3:
                DonutAttack().Forget();
                       
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
        Vector3 targer = new Vector3(_target.position.x, this.transform.position.y, _target.position.z);

        Vector3 lookPlayer = targer - this.transform.position;

        float distance = lookPlayer.magnitude;
                
        if (distance <= _detectedRange)
        {
            if (_onAttack == false)
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

            else
            {
                CLogger.Log("Boss Animator Moving");
            }
        
        }
        

     


    }

    #region NormalAttack Patterns
    private async UniTask NormalAttackDelay()
    {

        await UniTask.Delay(2500, DelayType.UnscaledDeltaTime);
        

        _onAttack = false;

    }

    private void DoubleHookAttack()
    {

        int hit = NormalFindPlayer(3f);

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

            SDamageInfo damagePlayer = new SDamageInfo(0.1f, ERatioSwitch.ByMax);
            float lastHp = player.HpCurrent;

            player.OnHit(this, damagePlayer);

            CLogger.Log($"{player.name}의 현재 체력 : {player.HpCurrent}");
        }



    }

    private void BackSpinAttack()
    {
        int hit = NormalFindPlayer(3f);
        
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

            Vector3 damageDir = player.transform.position - this.transform.position;

            damageDir.y = 0f;

            float damageAngle = Vector3.SignedAngle(this.transform.forward, damageDir, Vector3.up);

            if(damageAngle >= 180f && damageAngle <=360f)
            {

                SDamageInfo damagePlayer = new SDamageInfo(0.2f, ERatioSwitch.ByMax);

                player.OnHit(this, damagePlayer);

                CLogger.Log($"{player.name}의 현재 체력 : {player.HpCurrent}");

            }
            

        }

    }

    private void SlamUpperAttack()
    {

        int hit = NormalFindPlayer(3f);

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

            SDamageInfo damagePlayer = new SDamageInfo(0.15f, ERatioSwitch.ByMax);
           
            player.OnHit(this, damagePlayer);

            CLogger.Log($"{player.name}의 현재 체력 : {player.HpCurrent}");
        }

    }
    #endregion


    #region whirlWind
    private async UniTask WhirlWindAttack()
    {

        if(_previewPattern != null)
        {
            _previewPattern.ShowDonutPattern(20, 15f, 3f);
        }
        else
        {
            CLogger.Error("PatternView 비어있음");
        }

        await UniTask.Delay(2300);
        _atkTimer = -0.8f;

        if (_previewPattern != null)
        {
            _previewPattern.ClearPreview();
        }
        
        
        _animator.SetTrigger(_secondSub);


    }

    private void WhirlWindDamage()
    {

        int hit = SubFindPlayer(_whirlRange);

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

            if(distance >= 3f && distance <= _whirlRange)
            {
                SDamageInfo damagePlayer = new SDamageInfo(0.1f, true, ERatioSwitch.ByMax);
                
                player.OnHit(this, damagePlayer);


                CLogger.Log($"{player.name}의 현재 체력 : {player.HpCurrent}");
            }

        }
    }
    #endregion

    #region DonutAttack

    private async UniTask DonutAttack()
    {
        if(_previewPattern != null)
        {
            _previewPattern.ShowDonutPattern(20, 18f, 10f);
        }

        else
        {
            CLogger.Error("PatternView 비어있음");
        }

        await UniTask.Delay(2000);
        _atkTimer = 0;

        if (_previewPattern != null)
        {
            _previewPattern.ClearPreview();
        }

        _animator.SetTrigger(_forthSub);

        _onAttack = true;
        await UniTask.Delay(2000);

        _onAttack = false;
    }

    private void DonutDamage()
    {
        int hit = SubFindPlayer(_donutRange);

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

            if (distance >= 7f && distance <= 18f)
            {
                SDamageInfo damagePlayer = new SDamageInfo(0.25f, true ,ERatioSwitch.ByMax);

                player.OnHit(this, damagePlayer);

                CLogger.Log($"{player.name}의 현재 체력 : {player.HpCurrent}");
            }
        }
    }

    #endregion

    #region Double Hook and Explosion Attack
    private async UniTask TwiceAttack()
    {
        if (_previewPattern != null)
        {
            _previewPattern.transform.position = _player.transform.position;
            _previewPattern.ShowCirclePattern(20, 8f);

        }

        else
        {
            CLogger.Error("PatternView 비어있음");
        }

        _onAttack = true;
        await UniTask.Delay(1500);
        _atkTimer = 0;

        if (_previewPattern != null)
        {
            _previewPattern.ClearPreview();
            _previewPattern.transform.position = _patternPos;
        }

        await UniTask.Delay(1600);
        _onAttack = false;
    }

    private void DoubleHookDamage()
    {

        int hit = NormalFindPlayer(3.5f);

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
            float lastHp = player.HpCurrent;

            player.OnHit(this, damagePlayer);

            CLogger.Log($"{player.name}의 현재 체력 : {player.HpCurrent}");
        }



    }

    private void OuterExplosionDamage()
    {
        int hit = SubFindPlayer(13f);

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

            if (distance >= 7f && distance <= 13f)
            {
                SDamageInfo damagePlayer = new SDamageInfo(0.25f, ERatioSwitch.ByMax);

                player.OnHit(this, damagePlayer);

                CLogger.Log($"{player.name}의 현재 체력 : {player.HpCurrent}");
            }
        }
    }

    #endregion


    #region 6-Direction Alternating AOE

    private async UniTask AlternationAttack()
    {
        _onAttack = true;
        if (_previewPattern != null)
        {
            _previewPattern.ShowMultiArcPattern(_divideArea, 15f, _areaAngle, 30f);
        }

        else
        {
            CLogger.Error("PatternView �������");
        }

        await UniTask.Delay(1000);

        if (_previewPattern != null)
        {
            _previewPattern.ClearPreview();
        }

        if (_previewPattern != null)
        {
            _previewPattern.ShowMultiArcPattern(_divideArea, 15f, _areaAngle, 0f);
        }

        else
        {
            CLogger.Error("PatternView �������");
        }

        await UniTask.Delay(1000); ;

        if (_previewPattern != null)
        {
            _previewPattern.ClearPreview();
        }

        _atkTimer = 0f;

        _animator.SetTrigger(_thirdSub);

        await UniTask.Delay(3100);
        _onAttack = false;
    }


    private void AlternationDamage(float offsetAngle)
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

            if(distance > 15f)
            {
                continue;
            }

            Vector3 playerDir = player.transform.position - this.transform.position;
            playerDir.y = 0;

            float playerAngle = Vector3.SignedAngle(this.transform.forward, playerDir, Vector3.up);

            bool isHit = false;
            float angleStep = 360f / _divideArea;

            for (int j = 0; j < _divideArea; j++)
            {
                float standardAngle = offsetAngle +(j * angleStep);
                float tickAngle = Mathf.DeltaAngle(standardAngle, playerAngle);
                
                if(Mathf.Abs(tickAngle) <= _areaAngle * 0.5f)
                { 
                    isHit = true;
                    break;
                }
            }


            if (isHit)
            {
                SDamageInfo damagePlayer = new SDamageInfo(0.2f, true, ERatioSwitch.ByMax);
                player.OnHit(this, damagePlayer);

                CLogger.Log($"{player.name}의 현재 체력 : {player.HpCurrent}");
            }

        }


    }


    private void AlternationDamage_First()
    {
        AlternationDamage(0f);


    }


    private void AlternationDamage_Second()
    {
        AlternationDamage(30f);
    }

    #endregion


    private int NormalFindPlayer(float radius)
    {
        Vector3 offset = this.transform.up * 1.0f + this.transform.forward * 2f;
                     
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

    private void OnValidate()
    {
        if (_player == null) return;
        Vector3 targetPos = _player.transform.position - this.transform.position; 

        float distance = targetPos.magnitude;

        distance = Mathf.Max(3.0f, distance);


    }

}