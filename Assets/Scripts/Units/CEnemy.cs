using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CEnemy : CUnit
{
    [Header("몬스터 정보 (테스트 전용, 우선시됨)")]
    [SerializeField] private CEnemyStatSO _targetStatSO;

    protected override void Awake()
    {    
        if (_targetStatSO != null)
        {
            Setup(_targetStatSO);
        }
        else
        {
            base.Awake();
        }
    }

    public void Setup(CEnemyStatSO target)
    {
        this._baseHpMax = target.Hp;
        this._baseAttack = target.Attack;
        this._baseDefence = target.Defence;

        this._hpMax = _baseHpMax;
        this._hpCurrent = _hpMax;
        this._attack = _baseAttack;
        this._defence = _baseDefence;
    }

    protected virtual void Start()
    {
        
    }

    protected virtual void Update()
    {
        
    }

    // 디버그용
    public void CallDie()
    {
        this.Die();
    }
}
