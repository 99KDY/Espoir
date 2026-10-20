using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;


public class CPlayerUI : MonoBehaviour
{
    #region 인스팩터
    [SerializeField] private CPlayer _player;
    
    [SerializeField] private Image _hpFill = null;

    [SerializeField] private TMP_Text _hpText = null;
    [SerializeField] private TMP_Text _damageText = null;

    #endregion

    #region 내부변수

    private float _currentHp;
    private float _maxHp;


    #endregion
    public event Action OnSync;



    #region 프로퍼티
    public float CurrentHP => _currentHp;
    // public float CurrentMp => 
    public float MaxHP => _maxHp;
    //public float MaxMP => this._maxMP;

    #endregion


    private void Start()
    {
        if(_player == null)
        {
            CLogger.Warn("CPlayerUI의 인스팩터에 플레이어 연결이 안되어 있음");
            return;
        }

        _maxHp = _player.HpMax;
        _currentHp = _player.HpCurrent;

        ResetHP();


    }

    private void OnEnable()
    {



    }


    private void OnDisable()
    {
        

    }

    public void UpdateHp()
    {
        if(_hpFill == null)
        {
            return;
        }

        float ratio = _maxHp > 0 ? _currentHp / _maxHp : 0f;

        OnSync?.Invoke();

        if (_currentHp <= 0f)
        {
            _currentHp = 0;
        }

        if(_hpText != null)
        {
            _hpText.text = $"{_currentHp} / {_maxHp}";
        }

        _hpFill.fillAmount = ratio;
    }


    //public void UpdateMp()
    //{
    //    if (_mpFill == null)
    //    {
    //        return;
    //    }

    //    float ratio = _maxMP > 0 ? _currentMp / _maxMP : 0f;

    //    OnSync?.Invoke();

    //    if (_currentMp <= 0)
    //    {
    //        _currentMp = 0;

    //    }

    //    _mpFill.fillAmount = ratio;
    //}

    public void ResetHP()
    {
        // TODO : 마을을 복귀했거나, 레이드 처음시도 or 재시도 떄에 전체 회복
        _currentHp = _maxHp;

        UpdateHp();
    }

    //public void ResetMP()
    //{
    //    // TODO : 마을을 복귀했거나, 레이드 처음시도 or 재시도 떄에 전체 회복
    //    _currentMp = _maxMP;

    //    UpdateMp();
    //}


    public void ReceiveDamage(float damage)
    {
        damage = Mathf.Max(0f , damage);

        _currentHp = Mathf.Max(0f, _currentHp - damage);

        UpdateHp();

        if(_damageText != null)
        {
            _damageText.text = $" - {damage}";
        }

        CLogger.Log($" 현재 채력 : {_currentHp} / 전체 체력 : {_maxHp} / 받은 피해 : {damage}");


    }

    private void OnValidate()
    {
        _maxHp = Mathf.Max(1.0f, _maxHp);
        
        //_maxMP = Mathf.Max(1.0f, _maxMP);

    }

}
