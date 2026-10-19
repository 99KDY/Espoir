using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CEnemyUI : MonoBehaviour
{
    [SerializeField] private CEnemy _enemy;
    [SerializeField] private float _enemyHP;

    [SerializeField] private Image _enemyHpFill = null;

    [SerializeField] private TMP_Text _damageText = null;

    public event Action OnEnemySync;

    private float _currentEnemyHp;


    public void UpdateEnemyHP()
    {
        if(_enemyHpFill == null)
        {
            return;
        }


        float ratio = _enemyHP > 0 ? _currentEnemyHp / _enemyHP : 0f;

        OnEnemySync?.Invoke();

        _enemyHpFill.fillAmount = ratio;

    }

    public void ResetEnemyHP()
    {
        _currentEnemyHp = _enemyHP;

        UpdateEnemyHP();

        CLogger.Log($" 현재 체력 : {_currentEnemyHp} / 최대 최력 : {_enemyHP}");

    }


    private void ReceiveDamage(float damage)
    {
        damage = Mathf.Max(0, damage);

        _currentEnemyHp = Mathf.Max(_currentEnemyHp - damage);

        UpdateEnemyHP();

        if (_damageText != null)
        {
            _damageText.text = $"{damage}";
        }

        CLogger.Log($" 받은 피해 : {damage} / 현재 체력 : {_currentEnemyHp} / 최대 체력 : {_enemyHP}  ");

    }

    private void OnValidate()
    {
        _enemyHP = Mathf.Max(0, _enemyHP);

    }

}
