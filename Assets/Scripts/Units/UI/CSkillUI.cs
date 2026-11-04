using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CSkillUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text _cooldownText;
    [SerializeField] private Image _durationFill;
    [SerializeField] private string _name;
    [SerializeField] private CTooltipUI _tooltipUI;
    [SerializeField][TextArea(1, 5)] private string _tooltipText;
    public string Tooltip => $"{_name}\n<size=75%>{_tooltipText}</size>";

    public void Refresh(float ratio, float remainCooldown)
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
        _tooltipUI.Enable(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _tooltipUI.Disable();
    }
}
