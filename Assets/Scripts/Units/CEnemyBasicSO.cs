using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EEnemyInfo
{
    normal = 0,
    firstboss,
    secondboss,
    thirdboss,
    lastboss

}


public class CEmemyBasicSO : MonoBehaviour 
{

    #region 인스팩터
    [SerializeField] string _enemyName = "Enemy";
    [SerializeField] private float _enemyHp;
    [SerializeField] private float _enemyAtk;
    [SerializeField] private float _enemyDef;
    #endregion

    #region 프로퍼티
    public string EEnemyName => _enemyName;
    public float EnemyHP => _enemyHp;
    public float EnemyAtk => _enemyAtk;
    public float EnemyDef => _enemyDef;

    #endregion

    #region 내부 변수
    protected float _currentHp;
    protected float _currentAtk;
    protected float _currentDef;

    #endregion

    void Start()
    {
        _currentHp = _enemyHp;
        _currentAtk = _enemyAtk;
        _currentDef = _enemyDef;
    }


    void Update()
    {
        
    }


    public virtual void OnHitPlayer()
    {
        // TODO : 플레이어에게 주는 히트방식
        
    }

    
    public virtual void OnDamaged(float damage)
    {
        _currentHp -= damage;

        if(_currentHp <= 0)
        {
            OnDie();
        }

    }

    protected virtual void OnDie()
    {
        // TODO : 죽는 애니메이션
        // TODO : 만들어둔 연출적 영상?
        // TODO : Player에게 힘 넘겨주기


    }


    private void OnValidate()
    {

        _currentHp = Mathf.Max(0, _currentHp);
        _currentAtk = Mathf.Max(0, _currentAtk);
        _currentDef = Mathf.Max(1.0f, _currentDef);

    }
}
