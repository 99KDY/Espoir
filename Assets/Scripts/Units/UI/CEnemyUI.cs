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

    [SerializeField] private Image _enemyHpFill = null;

    [SerializeField] private TMP_Text _damageText = null;

    public event Action OnEnemySync;

    private float _maxEnemyHP;
    private float _currentEnemyHp;

    public float CurrentEnemyHp => _currentEnemyHp;


    private void Start()
    {
        _currentEnemyHp = _enemy.HpCurrent;
        _maxEnemyHP = _enemy.HpMax;
    }

    private void Update()
    {
        UpdateEnemyHP();
    }


    public void UpdateEnemyHP()
    {
        if(_enemyHpFill == null)
        {
            return;
        }

        _currentEnemyHp = _enemy.HpCurrent;
        _maxEnemyHP = _enemy.HpMax;

        float ratio = _maxEnemyHP > 0 ? _currentEnemyHp / _maxEnemyHP : 0f;

        // OnEnemySync?.Invoke();

        _enemyHpFill.fillAmount = ratio;
        _damageText.text = $"{_currentEnemyHp:F0}";
    }

    public void ResetEnemyHP()
    {
        _currentEnemyHp = _maxEnemyHP;

        UpdateEnemyHP();

        CLogger.Log($" 현재 체력 : {_currentEnemyHp} / 최대 최력 : {_maxEnemyHP}");

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

        CLogger.Log($" 받은 피해 : {damage} / 현재 체력 : {_currentEnemyHp} / 최대 체력 : {_maxEnemyHP}  ");

    }

    private void OnValidate()
    {
        _maxEnemyHP = Mathf.Max(0, _maxEnemyHP);

    }

}
