using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CQuickslotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text _cooldownText;
    [SerializeField] private TMP_Text _quantityText;
    [SerializeField] private Image _durationFill;
    [SerializeField] private Image _itemIcon;
    [SerializeField] private CTooltipUI _tooltipUI;

    [SerializeField] private bool _showingAvailable;

    private bool _hasItem;
    private string _name = "";
    private string _tooltip = "";

    public bool HasItem => _hasItem;

    public string Tooltip => $"{_name}\n<size=75%>{_tooltip}</size>";

    public void RefreshIteminfo(int id, int quantity)
    {
        CItemSO item = null;
        if (id != -1)
        {
            CItemSO.TryFind(id, out item);
        }

        if (item == null)
        {
            _hasItem = false;
            _itemIcon.sprite = null;
            _itemIcon.enabled = false;
            _durationFill.gameObject.SetActive(false);
            _quantityText.text = "";
            _name = "";
            _tooltip = "";
            this.RefreshCooldown(0f, 0f);
        }
        else
        {
            _hasItem = true;
            _itemIcon.sprite = item.ItemIcon;
            _itemIcon.enabled = true;
            _durationFill.gameObject.SetActive(true);
            _quantityText.text = quantity.ToString();
            _name = item.Name;
            _tooltip = item.Description;
        }
    }

    public void RefreshCooldown(float ratio, float remainCooldown)
    {
        _durationFill.fillAmount = Mathf.Clamp01(ratio);

        if (remainCooldown <= 0) _cooldownText.gameObject.SetActive(false);
        else
        {
            _cooldownText.text = remainCooldown.ToString("F1");
            _cooldownText.gameObject.SetActive(true);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_hasItem) _tooltipUI.Enable(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _tooltipUI.Disable();
    }
}
