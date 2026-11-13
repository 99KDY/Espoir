using System.Collections;
using TMPro;
using UnityEngine;

public class CTooltipUI : MonoBehaviour
{
    [SerializeField] Canvas _canvas;
    [SerializeField] private TMP_Text _tooltip;

    private CStatusUI _targetStatus;
    private CSkillUI _targetSkill;
    private CQuickslotUI _targetQuickslot;

    public CStatusUI TargetStatus => _targetStatus;

    private bool _isOn = false;

    void Start()
    {
        if (_tooltip == null)
        {
            CLogger.Error("툴팁 할당이 안되어있습니다.");
            enabled = false;
            return;
        }
        _tooltip.lineSpacing = -8f;
    }

    void Update()
    {
        if (_isOn)
        {
            FindNewPosition();
        }
    }

    private void FindNewPosition()
    {
        int flag = 0;
        Vector2 localPoint;
        Vector2 pivot = new Vector2(1, 1);
        RectTransform rectTransform = _tooltip.rectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle
                (
                    _canvas.GetComponent<RectTransform>(),
                    Input.mousePosition,
                    null,
                    out localPoint)
                )
        {
            if (localPoint.x < 0) flag += 1;
            if (localPoint.y < 0) flag += 2;

            switch (flag)
            {
                case 0:
                    pivot.x = 1;
                    pivot.y = 1;
                    _tooltip.alignment = TextAlignmentOptions.TopRight;
                    break;
                case 1:
                    pivot.x = 0;
                    pivot.y = 1;
                    _tooltip.alignment = TextAlignmentOptions.TopLeft;
                    break;
                case 2:
                    pivot.x = 1;
                    pivot.y = 0;
                    _tooltip.alignment = TextAlignmentOptions.BottomRight;
                    break;
                case 3:
                    pivot.x = 0;
                    pivot.y = 0;
                    _tooltip.alignment = TextAlignmentOptions.BottomLeft;
                    break;
            }

        }
        else
        {
            localPoint = Vector2.zero;
        }
        rectTransform.anchoredPosition = localPoint;
        rectTransform.pivot = pivot;
    }

    public void Enable(CStatusUI target)
    {
        _isOn = true;
        _targetStatus = target;
        _targetSkill = null;
        _targetQuickslot = null;
        _tooltip.gameObject.SetActive(true);
        _tooltip.text = target.Tooltip;
    }

    public void Enable(CSkillUI target)
    {
        _isOn = true;
        _targetSkill = target;
        _targetStatus = null;
        _targetQuickslot = null;
        _tooltip.gameObject.SetActive(true);
        _tooltip.text = target.Tooltip;
    }
    public void Enable(CQuickslotUI target)
    {
        _isOn = true;
        _targetQuickslot = target;
        _targetSkill = null;
        _targetStatus = null;
        _tooltip.gameObject.SetActive(true);
        _tooltip.text = target.Tooltip;
    }

    public void Disable()
    {
        _isOn = false;
        _targetStatus = null;
        _targetSkill = null;
        _targetQuickslot = null;
        _tooltip.gameObject.SetActive(false);
    }
}
