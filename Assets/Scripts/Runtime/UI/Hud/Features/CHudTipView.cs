using TMPro;
using UnityEngine;

/// <summary>
/// 하나의 이름표를 마우스가 가리키는 슬롯 위로 옮겨 표시합니다.
/// </summary>
[DefaultExecutionOrder(270)]
public class CHudTipView : MonoBehaviour
{
    [SerializeField] private RectTransform _box;
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private TMP_Text _text;

    [Header("표시 전환")]
    [SerializeField] private float _gap = 14f;
    [SerializeField] private Vector2 _padding = new Vector2(11f, 6f);
    [SerializeField] private float _border = 1f;
    [SerializeField] private float _lineHeight = 13f;
    [SerializeField] private float _transition = 0.16f;
    [SerializeField] private float _hiddenOffset = 6f;

    private RectTransform _target;
    private float _from;
    private float _to;
    private float _start = -10f;
    private readonly Vector3[] _CORNERS = new Vector3[4];

    private void Awake()
    {
        Apply(0f);
    }

    /// <summary>칸 위에 글자를 띄웁니다(이미 보이면 그 칸으로 옮김).</summary>
    public void Show(RectTransform slot, string text)
    {
        if (_text.text != text) _text.text = text;
        _target = slot;
        Go(1f);
    }

    /// <summary>그 칸의 이름표를 숨깁니다.</summary>
    public void Hide(RectTransform slot)
    {
        if (_target != slot) return;

        Go(0f);
    }

    private void Go(float to)
    {
        if (Mathf.Approximately(_to, to)) return;

        float now = Time.unscaledTime;
        _from = Current(now);
        _to = to;
        _start = now;
    }

    private float Current(float now)
    {
        return Mathf.Lerp(_from, _to, CHudEase.Ease(Mathf.Clamp01((now - _start) / _transition)));
    }

    private void LateUpdate()
    {
        float v = Current(Time.unscaledTime);
        Apply(v);
    }

    private void Apply(float v)
    {
        if (_group != null) _group.alpha = v;
        if (_box == null || _target == null || v <= 0f) return;

        // 글자 폭에 여백과 테두리를 더해 이름표 크기를 정한다.
        _text.ForceMeshUpdate();
        float w = Mathf.Ceil(_text.preferredWidth * 100f) / 100f + 2f * (_padding.x + _border);
        float h = _lineHeight + 2f * (_padding.y + _border);
        _box.sizeDelta = new Vector2(w, h);

        // 슬롯 위쪽 중앙을 기준으로 표시한다.
        _target.GetWorldCorners(_CORNERS);
        RectTransform parent = (RectTransform)_box.parent;
        Vector3 a = parent.InverseTransformPoint(_CORNERS[1]);
        Vector3 b = parent.InverseTransformPoint(_CORNERS[2]);
        _box.anchoredPosition = Vector2.zero;
        Vector2 basePos = (Vector2)_box.localPosition - _box.anchoredPosition;
        Vector2 top = new Vector2((a.x + b.x) * 0.5f, a.y);
        _box.anchoredPosition = top - basePos + new Vector2(0f, _gap - _hiddenOffset * (1f - v));
    }
}
