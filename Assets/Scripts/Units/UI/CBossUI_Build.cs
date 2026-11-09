using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class CBossUI_Build : MonoBehaviour
{
    #region 인스팩터
    [SerializeField] private CEnemy _target;

    [SerializeField] private Image _hpFill = null;
    [SerializeField] private TMP_Text _hpText = null;

    [SerializeField] private RectTransform _statusRoot = null;
    [SerializeField] private CStatusUI _statusPrefab = null;
    [SerializeField] private CTooltipUI _tooltip = null;

    [SerializeField] private TMP_Text _debugText = null;
    #endregion

    #region 내부변수

    private readonly Dictionary<CStatus, CStatusUI> _statusViews = new();
    private readonly HashSet<CStatus> _currentStatus = new();
    private readonly List<CStatus> _removedStatus = new();
    #endregion



    private void Start()
    {
        if (_target == null)
        {
            CLogger.Warn("CBossUI의 인스팩터에 플레이어 연결이 안되어 있음");
            return;
        }
    }

    private void LateUpdate()
    {
        UpdateHp();
        if (_debugText != null) UpdateDebug();
        UpdateStatus();
    }

    public void UpdateHp()
    {
        if (_hpFill == null)
        {
            return;
        }

        float ratio = _target.HpMax > 0 ? _target.HpCurrent / _target.HpMax : 0f;

        if (_hpText != null)
        {
            _hpText.text = $"{((_target.HpCurrent <= 0f) ? 0 : _target.HpCurrent):F0} / {_target.HpMax:F0}";
        }

        _hpFill.fillAmount = ratio;
    }

    public void UpdateDebug()
    {
        _debugText.text = $"ATK {_target.Attack:F1} / DEF {_target.Defence:F1} / Crit {_target.CriticalRate * 100:F1}% - {_target.CriticalDamage * 100:F1}%";
    }

    public void UpdateStatus()
    {
        if (_statusRoot == null || _statusPrefab == null)
        {
            return;
        }

        _currentStatus.Clear();
        _removedStatus.Clear();

        if (_target.Status != null)
        {
            for (int i = 0; i < _target.Status.Count; i++)
            {
                CStatus target = _target.Status[i];
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
}
