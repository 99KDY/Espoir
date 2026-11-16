using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 대화창(dh6) 선택지 한 줄: 번호 키 모양 + 글자, 올리면 왼쪽에 금빛 막대가 서고 줄이 밝아짐.
/// </summary>
public class CTalkChoiceView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private static readonly Color _GOLD = new Color(234f / 255f, 223f / 255f, 180f / 255f, 1f);
    private static readonly Color _EM_BG = new Color(8f / 255f, 10f / 255f, 18f / 255f, 0.4f);
    private static readonly Color _EM_BORDER = new Color(1f, 1f, 1f, 0.55f);
    private static readonly Color _EM_HOVER_TEXT = new Color(23f / 255f, 22f / 255f, 29f / 255f, 1f);
    private static readonly Color _ROW_HOVER = new Color(1f, 1f, 1f, 0.07f);

    [SerializeField] private RectTransform _row;
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private Image _background;
    [SerializeField] private RectTransform _separator;
    [SerializeField] private RectTransform _bar;
    [SerializeField] private RectTransform _numberBox;
    [SerializeField] private Image _numberFill;
    [SerializeField] private Image[] _numberBorders;
    [SerializeField] private TMP_Text _number;
    [SerializeField] private TMP_Text _label;

    [Header("웹 값 (px)")]
    [SerializeField] private float _paddingLeft = 24f;
    [SerializeField] private float _hoverPaddingLeft = 32f;
    [SerializeField] private float _numberSize = 24f;
    [SerializeField] private float _gap = 16f;
    [SerializeField] private float _enterOffset = 26f;
    [SerializeField] private float _enterDuration = 0.35f;

    private SCssTween _hover;
    private SCssTween _barScale;
    private SCssTween _padding;
    private float _enterStart;
    private float _enterDelay;
    private float _height;
    private float _top;
    private bool _isHover;

    /// <summary>
    /// 고르면 이 줄의 순서(0부터)를 넘깁니다.
    /// </summary>
    public event Action<int> Chosen;

    public int Index { get; private set; }

    /// <summary>
    /// 글자 · 번호 · 자리를 넣고 등장 애니메이션(ch-in .35s, 2·3번째 줄만 .06s · .12s 늦게)을 시작합니다.
    /// </summary>
    public void Show(int index, string text, float top, float width, float height, bool isLast, float now)
    {
        Index = index;
        _height = height;
        gameObject.SetActive(true);
        _label.text = text;
        _number.text = (index + 1).ToString();
        _top = top;
        _row.sizeDelta = new Vector2(width, height);
        _separator.gameObject.SetActive(!isLast);
        // ::before: left -1, top 12, bottom 12, 가운데 기준 scaleY (막대 pivot 은 왼쪽 가운데)
        _bar.anchoredPosition = new Vector2(-1f, -height * 0.5f);
        _separator.anchoredPosition = new Vector2(0f, -(height - 1f));
        _bar.sizeDelta = new Vector2(3f, Mathf.Max(0f, height - 24f));

        _isHover = false;
        _hover.Snap(0f);
        _barScale.Snap(0f);
        _padding.Snap(_paddingLeft);
        _enterStart = now;
        _enterDelay = index == 1 ? 0.06f : index == 2 ? 0.12f : 0f;
        Tick(now);
    }

    /// <summary>
    /// 숨기고 다시 쓸 수 있게 둡니다.
    /// </summary>
    public void Hide()
    {
        _isHover = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 확인용: 마우스를 올린 상태로 만듭니다.
    /// </summary>
    public void SetHover(bool isHover, float now)
    {
        if (_isHover == isHover) return;

        _isHover = isHover;
        float target = isHover ? 1f : 0f;
        _hover.To(target, now, 0.16f, 0f, CCssEase.EASE);
        _barScale.To(target, now, 0.18f, 0f, CCssEase.EASE);
        _padding.To(isHover ? _hoverPaddingLeft : _paddingLeft, now, 0.16f, 0f, CCssEase.EASE);
    }

    /// <summary>
    /// 매 프레임 전환 값을 반영합니다.
    /// </summary>
    public void Tick(float now)
    {
        float k = CCssEase.Evaluate(CCssEase.EASE, Mathf.Clamp01((now - _enterStart - _enterDelay) / _enterDuration));
        _group.alpha = k;
        // .ch 전체가 translateX 로 들어옴
        _row.anchoredPosition = new Vector2(1f + (1f - k) * _enterOffset, -_top);

        float hover = _hover.Value(now);
        _background.color = new Color(_ROW_HOVER.r, _ROW_HOVER.g, _ROW_HOVER.b, _ROW_HOVER.a * hover);
        _bar.localScale = new Vector3(1f, Mathf.Clamp01(_barScale.Value(now)), 1f);

        float padding = _padding.Value(now);
        float centerY = _height * 0.5f;
        _numberBox.anchoredPosition = new Vector2(padding, -(centerY - _numberSize * 0.5f));
        _label.rectTransform.anchoredPosition = new Vector2(padding + _numberSize + _gap, -(centerY - 12f));

        _numberFill.color = LerpPremultiplied(_EM_BG, _GOLD, hover);
        _number.color = Color.Lerp(Color.white, _EM_HOVER_TEXT, hover);

        // 테두리 색은 transition 에 없어서 바로 바뀜
        foreach (Image border in _numberBorders)
        {
            border.color = _isHover ? _GOLD : _EM_BORDER;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHover(true, Time.unscaledTime);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHover(false, Time.unscaledTime);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        Chosen?.Invoke(Index);
    }

    private static Color LerpPremultiplied(Color a, Color b, float t)
    {
        float alpha = Mathf.Lerp(a.a, b.a, t);
        if (alpha <= 1e-5f) return new Color(0f, 0f, 0f, 0f);

        Color premultiplied = Color.Lerp(new Color(a.r * a.a, a.g * a.a, a.b * a.a),
            new Color(b.r * b.a, b.g * b.a, b.b * b.a), t);
        return new Color(premultiplied.r / alpha, premultiplied.g / alpha, premultiplied.b / alpha, alpha);
    }
}
