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
    [SerializeField] private float _maxHP;
    //[SerializeField] private float _maxMP;

    [SerializeField] private Image _hpFill = null;
    //[SerializeField] private Image _mpFill = null;

    [SerializeField] private TMP_Text _hpText = null;
    [SerializeField] private TMP_Text _damageText = null;

    // 뉴인풋 시스템에서 스킬 셋 하나 만들어 두자.
    //[SerializeField] private KeyCode _skill_Q = KeyCode.Q;
    //[SerializeField] private KeyCode _skill_W = KeyCode.W;
    //[SerializeField] private KeyCode _skill_E = KeyCode.E;
    //[SerializeField] private KeyCode _skill_R = KeyCode.R;

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

        _maxHP = _player.HpMax;
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

        float ratio = _maxHP > 0 ? _currentHp / _maxHP : 0f;

        OnSync?.Invoke();

        if (_currentHp <= 0f)
        {
            _currentHp = 0;
        }

        if(_hpText != null)
        {
            _hpText.text = $"{_currentHp} / {_maxHP}";
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
        _currentHp = _maxHP;

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

        CLogger.Log($" 현재 채력 : {_currentHp} / 전체 체력 : {_maxHP} / 받은 피해 : {damage}");


    }

    private void OnValidate()
    {
        _maxHP = Mathf.Max(1.0f, _maxHP);
        
        //_maxMP = Mathf.Max(1.0f, _maxMP);

    }

}
