using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SEnemyPattern
{
    public string patternName;
    public string attackRatio;
    public List<CStatusSO> statusOnHit;
}

[CreateAssetMenu(fileName = "NewEnemyStat", menuName = "Espoir/EnemyStatSO")]
public class CEnemyStatSO : ScriptableObject
{
    #region 인스팩터
    [SerializeField] private string _name = "Enemy";
    [SerializeField] private float _hpMax;
    [SerializeField] private float _attack;
    [SerializeField] private float _defence;
    #endregion

    #region 프로퍼티
    public string Name => name;
    public float Hp => _hpMax;
    public float Attack => _attack;
    public float Defence => _defence;
    #endregion

    private void OnValidate()
    {
        _hpMax = Mathf.Max(1f, _hpMax);
        _attack = Mathf.Max(0f, _attack);
        _defence = Mathf.Max(0f, _defence);
    }
}
