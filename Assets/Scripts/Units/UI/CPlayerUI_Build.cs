using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Data;
using Unity.VisualScripting;


public class CPlayerUI_Build : MonoBehaviour
{
    #region 인스팩터
    [SerializeField] private CPlayer _player;
    
    [SerializeField] private Image _hpFill = null;
    [SerializeField] private Image _mpFill = null;

    [SerializeField] private TMP_Text _hpText = null;
    [SerializeField] private TMP_Text _mpText = null;

    [SerializeField] private TMP_Text _damageText = null;

    [SerializeField] private RectTransform _statusRoot = null;
    [SerializeField] private CStatusUI _statusPrefab = null;
    [SerializeField] private CTooltipUI _tooltip = null;
    #endregion

    #region 내부변수

    private float _currentHp;
    private float _maxHp;


    private readonly Dictionary<CStatus, CStatusUI> _statusViews = new();
    private readonly HashSet<CStatus> _currentStatus = new();
    private readonly List<CStatus> _removedStatus = new();

    #endregion



    #region 프로퍼티
    public float CurrentHP => _currentHp;
    public float MaxHP => _maxHp;
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

    private void LateUpdate()
    {
        UpdateHp();
        UpdateMp();
        UpdateStatus();
    }

    public void UpdateHp()
    {
        if(_hpFill == null)
        {
            return;
        }

        float ratio = _player.HpMax > 0 ? _player.HpCurrent / _player.HpMax : 0f;

        if(_hpText != null)
        {
            _hpText.text = $"{((_player.HpCurrent <= 0f) ? 0 : _player.HpCurrent):F0} / {_player.HpMax:F0}";
        }

        _hpFill.fillAmount = ratio;
    }


    public void UpdateMp()
    {
        if (_mpFill == null)
        {
            return;
        }

        float ratio = _player.MaxMp > 0 ? _player.CurrentMp / _player.MaxMp : 0f;

        if (_mpText != null)
        {
            _mpText.text = $"{((_player.CurrentMp<= 0f) ? 0 : _player.CurrentMp):F0} / {_player.MaxMp:F0}";
        }

        _mpFill.fillAmount = ratio;
    }

    public void UpdateStatus()
    {
        if (_statusRoot == null || _statusPrefab == null)
        {
            return;
        }

        _currentStatus.Clear();
        _removedStatus.Clear();
        if (_player.EquipmentStatus != null)
        {
            for (int i = 0; i < _player.EquipmentStatus.Count; i++)
            {
                CStatus target = _player.EquipmentStatus[i];
                if (target == null) continue;
                _currentStatus.Add(target);
                if (_statusViews.ContainsKey(target)) continue;
                CStatusUI view = Instantiate(_statusPrefab, _statusRoot);
                view.Bind(target, _tooltip);

                _statusViews.Add(target, view);
            }
        }

        if (_player.Status != null)
        {
            for (int i = 0; i < _player.Status.Count; i++)
            {
                CStatus target = _player.Status[i];
                if (target == null) continue;
                _currentStatus.Add(target);
                if (_statusViews.ContainsKey(target)) continue;
                CStatusUI view = Instantiate(_statusPrefab, _statusRoot);
                view.Bind(target, _tooltip);

                _statusViews.Add(target, view);
            }
        }

        foreach (KeyValuePair<CStatus, CStatusUI> pair in _statusViews)
        {
            if (_currentStatus.Contains(pair.Key))
            {
                pair.Value.Refresh();
            }
            else
            {
                _removedStatus.Add(pair.Key);
            }
        }

        if (_removedStatus.Count > 0)
        {
            for (int i = 0; i < _removedStatus.Count; i++)
            {
                CStatusUI target = _statusViews[_removedStatus[i]];
                _statusViews.Remove(_removedStatus[i]);
                target.gameObject.SetActive(false);
                Destroy(target.gameObject);
            }
        }
    }
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
