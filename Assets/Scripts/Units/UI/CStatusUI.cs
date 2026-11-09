using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CStatusUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _stackText;
    [SerializeField] private Image _durationFill;
    [SerializeField] private CTooltipUI _tooltip;

    private CStatus _targetStatus;
    public string Tooltip => $"{_targetStatus.Name}\n<size=75%>{_targetStatus.Tooltip}</size>";

    public void Bind(CStatus status, CTooltipUI tooltip)
    {
        _targetStatus = status;
        _iconImage.sprite = _targetStatus.Image;
        _stackText.text = "";
        _durationFill.fillAmount = 0f;
        _tooltip = tooltip;
        Refresh();
    }

    public void Refresh()
    {
        if (_targetStatus == null) return;
        if (_targetStatus.IsStackable)
        {
            _stackText.text = _targetStatus.Stack.ToString();
        }
        if (!_targetStatus.IsPermanent)
        {
            _durationFill.fillAmount = 1 - (_targetStatus.TimerRatio);
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        _tooltip.Enable(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _tooltip.Disable();
    }

    private void OnDestroy()
    {
        if (_tooltip.TargetStatus == this) _tooltip.Disable();
    }
}
