using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public struct SDamageInfo
{
    public float damage;
    // public CSkill skill;
    public bool isFixed;
    public ERatioSwitch ratioSwitch;
    public bool enableCritical;
    public List<CStatusSO> stateOnHit;

    public SDamageInfo(float damage, bool isFixed, ERatioSwitch ratioSwitch, bool enableCritical, List<CStatusSO> stateOnHit)
    {
        this.damage = damage;
        this.isFixed = isFixed;
        this.ratioSwitch = ratioSwitch;
        this.enableCritical = enableCritical;
        this.stateOnHit = stateOnHit;
    }

    public SDamageInfo (float damage, bool isFixed, bool enableCritical, List<CStatusSO> stateOnHit)
    {
        this.damage = damage;
        this.isFixed = isFixed;
        this.ratioSwitch = ERatioSwitch.Constant;
        this.enableCritical = enableCritical;
        this.stateOnHit = stateOnHit;
    }

    public SDamageInfo(float damage, ERatioSwitch ratioSwitch = ERatioSwitch.Constant)
        : this(damage, false, ratioSwitch, true, null) { }
    public SDamageInfo(float damage, bool isFixed, ERatioSwitch ratioSwitch = ERatioSwitch.Constant)
        : this(damage, isFixed, ratioSwitch, true, null) { }
    public SDamageInfo(float damage, bool isFixed, List<CStatusSO> stateOnHit)
    : this(damage, isFixed, true, stateOnHit) { }
}

public enum ERatioSwitch
{
    Constant,   // 고정치
    ByMax,      // 최대치 비례
    ByCurrent,  // 현재치 비례
    ByLose      // 잃은수치 비례
}

public abstract class CUnit : MonoBehaviour
{
    #region 인스펙터
    [Header("기본 스텟")]
    [SerializeField] protected float _baseHpMax;
    [SerializeField] protected float _baseAttack;
    [SerializeField] protected float _baseDefence;
    #endregion

    #region 내부 변수
    protected float _hpMaxAdd = 0;
    protected float _attackAdd = 0;
    protected float _defenceAdd = 0;

    protected float _hpMaxRatio = 1;
    protected float _attackRatio = 1;
    protected float _defenceRatio = 1;

    protected float _hpCurrent = 1;
    protected float _hpMax = 1;
    protected float _attack = 0;
    protected float _defence = 0;

    protected float _critRate = 0.05f;
    protected float _critDmg = 1.5f;

    protected bool _isStunned = false;

    protected List<CStatus> _statuses;
    #endregion

    #region 프로퍼티
    public float HpCurrent
    {
        get => _hpCurrent;
        // set => _hpCurrent = value;
    }

    public float HpMax
    {
        get => _hpMax;
        // set => _hpMax = value;
    }
    public float Attack
    {
        get => _attack;
        // set => _attack = value;
    }
    public float Defence
    {
        get => _defence;
        // set => _defence = value;
    }

    public float CriticalRate
    {
        get => _critRate;
    }

    public float CriticalDamage
    {
        get => _critDmg;
    }

    public bool IsStunned
    {
        get => _isStunned;
    }

    public List<CStatus> Status
    {
        get => _statuses;
        set => _statuses = value;
    }
    #endregion

    protected virtual void Awake()
    {
        _hpMax = _baseHpMax;
        _hpCurrent = _hpMax;
        _attack = _baseAttack;
        _defence = _baseDefence;
        _statuses = new List<CStatus>();
    }

    /// <summary>
    /// 객체가 공격을 받을 때 호출이 되는 함수입니다.]
    /// 일반적으로 defender는 this을 쓰는것도 괜찮습니다.
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="defender"></param>
    /// <param name="damage"></param>
    public virtual void OnHit(CUnit attacker, CUnit defender, SDamageInfo damage)
    {
        float finalDamage = DamageCalculate(attacker, defender, damage);
        defender.GetDamage(finalDamage);
        AfterHit(attacker, defender);
    }

    /// <summary>
    /// 객체가 공격을 받을 때 호출이 되는 함수입니다.]
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="defender"></param>
    /// <param name="damage"></param>
    public virtual void OnHit(CUnit attacker, SDamageInfo damage)
    {
        OnHit(attacker, this, damage);
    }

    protected virtual float DamageCalculate(CUnit attacker, CUnit defender, SDamageInfo damage, out bool isCritical)
    {
        isCritical = false;
        float finalDamage;
        switch (damage.ratioSwitch)
        {
            case ERatioSwitch.Constant:
                finalDamage = damage.damage;
                break;
            case ERatioSwitch.ByMax:
                finalDamage = defender.HpMax * damage.damage;
                break;
            case ERatioSwitch.ByCurrent:
                finalDamage = defender.HpCurrent * damage.damage;
                break;
            case ERatioSwitch.ByLose:
                finalDamage = (defender.HpMax - defender.HpCurrent) * damage.damage;
                break;
            default:
                finalDamage = damage.damage;
                break;
        }
        if (damage.enableCritical)
        {
            if (UnityEngine.Random.value < attacker._critRate)
            {
                finalDamage *= attacker._critDmg;
                isCritical = true;
            }
        }
        if (damage.isFixed)
        {   
            return damage.damage;
        }

        finalDamage = finalDamage * (100f / (100f + Mathf.Max(defender.Defence, -99.9f)));
        // TODO: 상태이상을 적용해서 대미지 정리하기
        return finalDamage;
    }

    protected virtual float DamageCalculate(CUnit attacker, CUnit defender, SDamageInfo damage)
    {
        return DamageCalculate(attacker, defender, damage, out bool temp);
    }

    protected virtual float DamageCalculate(CUnit attacker, SDamageInfo damage, out bool isCritical)
    {
        return DamageCalculate(attacker, this, damage, out isCritical);
    }

    protected virtual float DamageCalculate(CUnit attacker, SDamageInfo damage)
    {
        return DamageCalculate(attacker, this, damage);
    }

    protected virtual void AfterHit(CUnit attacker, CUnit defender)
    {

    }

    public virtual void GetDamage(float value, ERatioSwitch ratioSwitch = ERatioSwitch.Constant)
    {
        switch (ratioSwitch)
        {
            case ERatioSwitch.Constant:
                _hpCurrent -= value;
                break;
            case ERatioSwitch.ByMax:
                _hpCurrent -= (_hpMax * value);
                break;
            case ERatioSwitch.ByCurrent:
                _hpCurrent -= (_hpCurrent * value);
                break;
            case ERatioSwitch.ByLose:
                _hpCurrent -= ((_hpMax - _hpCurrent) * value);
                break;
        }
        _hpCurrent = Mathf.Min(_hpCurrent, _hpMax);
        _hpCurrent = Mathf.Max(_hpCurrent, 0f);

        if (_hpCurrent <= 0f)
        {
            Die();
        }
    }

    protected virtual void Die()
    {

    }

    public virtual void GetHeal(float value, ERatioSwitch ratioSwitch = ERatioSwitch.Constant)
    {
        switch (ratioSwitch)
        {
            case ERatioSwitch.Constant:
                _hpCurrent += value;
                break;
            case ERatioSwitch.ByMax:
                _hpCurrent += (_hpMax * value);
                break;
            case ERatioSwitch.ByCurrent:
                _hpCurrent += (_hpCurrent * value);
                break;
            case ERatioSwitch.ByLose:
                _hpCurrent += ((_hpMax - _hpCurrent) * value);
                break;
        }
        _hpCurrent = Mathf.Min(_hpCurrent, _hpMax);
        _hpCurrent = Mathf.Max(_hpCurrent, 0f);

        if (_hpCurrent <= 0f)
        {
            Die();
        }
    }

    public void GetStunned()
    {
        if (!_isStunned) _isStunned = true; 
    }

    public void ReleaseStunned(CStatus status)
    {
        int count = 0;
        for (int i = 0; i < _statuses.Count; i++)
        {
            if (_statuses[i].Trigger.HasFlag(EStatusTrigger.IsStun)) count++;
        }
        if (count <= 1) _isStunned = false;
    }
}