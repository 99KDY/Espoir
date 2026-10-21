using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAttackStat", menuName = "Espoir/AttackSO")]
public class CAttackSO : ScriptableObject
{
    [SerializeField] private float _attackRatio = 1f;
    [SerializeField] private bool _enableCritical = true;

}
