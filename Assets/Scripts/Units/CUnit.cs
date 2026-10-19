using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SDamageInfo
{
    public float damage;
    // public CSkill skill;
    public bool isFixed;
    public CStatusSO stateOnHit;

    public SDamageInfo (float damage, bool isFixed, CStatusSO stateOnHit)
    {
        this.damage = damage;
        this.isFixed = isFixed;
        this.stateOnHit = stateOnHit;
    }
    public SDamageInfo(float damage)
        : this(damage, false, null) { }
    public SDamageInfo(float damage, bool isFixed)
        : this(damage, isFixed, null) { }
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

    protected virtual float DamageCalculate(CUnit attacker, CUnit defender, SDamageInfo damage)
    {
        if (damage.isFixed)
        {   
            return damage.damage;
        }

        float finalDamage = damage.damage;
        finalDamage = finalDamage * (100f / (100f + Mathf.Max(defender.Defence, -99.9f)));
        // TODO: 상태이상을 적용해서 대미지 정리하기
        return finalDamage;
    }

    protected virtual float DamageCalculate(CUnit attacker, SDamageInfo damage)
    {
        return DamageCalculate(attacker, this, damage);
    }

    protected virtual void AfterHit(CUnit attacker, CUnit defender)
    {

    }

    public virtual void GetDamage(float damage)
    {
        _hpCurrent -= damage;
        _hpCurrent = Mathf.Max(_hpCurrent, 0f);

        if (_hpCurrent <= 0f)
        {
            Die();
        }
    }

    protected virtual void Die()
    {

    }

    public virtual void GetHeal(float heal)
    {
        _hpCurrent += heal;
        _hpCurrent = Mathf.Min(_hpCurrent, _hpMax);

        if (_hpCurrent <= 0f)
        {
            Die();
        }
    }
}
