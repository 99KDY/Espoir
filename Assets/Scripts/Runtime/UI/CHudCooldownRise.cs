using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 슬롯 쿨타임을 칸 바닥에서 빛이 차오르는 모양으로 보여 준다.
/// CSkillUI · CQuickslotUI 가 남은 비율(1 → 0)을 쓰는 숨김 이미지(_source)의 fillAmount 를 읽기만 한다.
/// </summary>
[DefaultExecutionOrder(100)]
public class CHudCooldownRise : MonoBehaviour
{
    [SerializeField] private Image _source;
    [SerializeField] private RectTransform _rise;
    [SerializeField] private CanvasGroup _icon;
    [SerializeField, Range(0f, 1f)] private float _iconAlphaOnCooldown = 0.5f;

    private float _shownRemain = -1f;
    private bool _isSuppressed;

    /// <summary>지금 쿨타임 중인지(차오름이 보이는지). 칸 모습(CHudSlotStyle)이 읽는다.</summary>
    public bool IsCooling => _shownRemain > 0f;

    /// <summary>
    /// 켜면 쿨타임 차오름을 숨깁니다(R 레비아탄 버프·피니쉬처럼 칸이 다른 상태를 보여 줄 때).
    /// </summary>
    public bool IsSuppressed
    {
        get { return _isSuppressed; }
        set
        {
            if (_isSuppressed == value) return;

            _isSuppressed = value;
            _shownRemain = -1f;
        }
    }

    private void LateUpdate()
    {
        float remain = !_isSuppressed && _source != null && _source.gameObject.activeSelf ? _source.fillAmount : 0f;
        if (Mathf.Approximately(remain, _shownRemain)) return;

        _shownRemain = remain;
        bool isCooling = remain > 0f;

        if (_rise != null)
        {
            _rise.gameObject.SetActive(isCooling);
            _rise.anchorMax = new Vector2(1f, 1f - remain);
        }

        if (_icon != null)
        {
            _icon.alpha = isCooling ? _iconAlphaOnCooldown : 1f;
        }
    }
}
