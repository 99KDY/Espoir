using TMPro;
using UnityEngine;

/// <summary>
/// NPC 머리 위에 이름과 칭호를 표시합니다.
/// 판 = 흐림 4px + rgba(8,10,18,.42) + 1px 흰 테(.4), 이름 Noto Serif KR 500 18px · 자간 .16em, 칭호 Noto Serif KR 400 13px · 자간
/// .34em · 금색.
/// 켜짐/꺼짐 전환은 talk.css 의 opacity · translate · scale transition 값 그대로.
/// </summary>
public class CNpcNameTagView : MonoBehaviour
{
    private static readonly Vector4 _IN_OPACITY = new Vector4(0.2f, 0.7f, 0.2f, 1f);
    private static readonly Vector4 _IN_MOVE = new Vector4(0.16f, 0.84f, 0.24f, 1f);
    private static readonly Vector4 _OUT = new Vector4(0.4f, 0f, 0.9f, 0.6f);

    [SerializeField] private RectTransform _root;
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private TMP_Text _name;
    [SerializeField] private TMP_Text _title;
    [SerializeField] private RectTransform _divider;

    [Header("웹 값 (px, 1080 기준)")]
    [SerializeField] private float _height = 38f;
    [SerializeField] private float _paddingX = 18f;
    [SerializeField] private float _gap = 12f;
    [SerializeField] private float _border = 1f;
    [SerializeField] private float _titleSpacingEm = 0.34f;
    [SerializeField] private float _titleMarginEm = 0.18f;
    [SerializeField] private float _titlePaddingLeft = 12f;
    [SerializeField] private float _liftAboveHead = 14f;

    private SCssTween _opacity;
    private SCssTween _translateY;
    private SCssTween _scale;
    private bool _isOn;
    private float _width;
    private Vector2 _anchor;

    public CTalkNpc Npc { get; private set; }

    /// <summary>
    /// NPC 이름과 칭호를 넣고 글자 폭에 맞춰 배경 크기를 계산합니다.
    /// </summary>
    public void Setup(CTalkNpc npc)
    {
        Npc = npc;
        _name.text = npc.DisplayName;
        _title.text = npc.Title;
        _name.ForceMeshUpdate();
        _title.ForceMeshUpdate();

        // CSS 글자 상자 = 글자 + 마지막 자간 (TMP preferredWidth 는 마지막 자간을 뺌)
        //  b: 상자(글자 + .16em) 에 margin-right -.16em → 차지하는 폭 = TMP 폭
        //  span: 왼쪽 테 1 + 안쪽 여백 12 + 글자 + .34em(칭호 자간) − .18em(margin-right)
        float nameWidth = _name.preferredWidth;
        float titleText = _title.preferredWidth;
        float titleWidth = _border + _titlePaddingLeft + titleText + (_titleSpacingEm - _titleMarginEm) *
            _title.fontSize;
        _width = _border * 2f + _paddingX * 2f + nameWidth + _gap + titleWidth;

        _root.sizeDelta = new Vector2(_width, _height);

        float x = _border + _paddingX;
        SetBox(_name.rectTransform, x, 5f, _name.preferredWidth + 4f, 26f);
        x += nameWidth + _gap;
        SetBox(_divider, x, 9f, 1f, 20f);
        SetBox(_title.rectTransform, x + _border + _titlePaddingLeft, 10f, titleText + 2f, 19f);

        _opacity.Snap(0f);
        _translateY.Snap(9f);
        _scale.Snap(0.965f);
        _isOn = false;
        Apply(0f);
    }

    /// <summary>
    /// 머리 위 화면 자리(캔버스 좌표, 왼쪽 위 기준 px)와 켜짐 여부를 넣습니다.
    /// </summary>
    public void Tick(Vector2 headCss, bool isOn, float now)
    {
        _anchor = headCss;

        if (isOn != _isOn)
        {
            _isOn = isOn;

            if (isOn)
            {
                _opacity.To(1f, now, 0.55f, 0f, _IN_OPACITY);
                _translateY.To(0f, now, 0.7f, 0f, _IN_MOVE);
                _scale.To(1f, now, 0.7f, 0f, _IN_MOVE);
            }

            else
            {
                _opacity.To(0f, now, 0.32f, 0f, _OUT);
                _translateY.To(9f, now, 0.36f, 0f, _OUT);
                _scale.To(0.965f, now, 0.36f, 0f, _OUT);
            }
        }

        Apply(now);
    }

    private void Apply(float now)
    {
        float opacity = _opacity.Value(now);
        float scale = _scale.Value(now);
        float translateY = _translateY.Value(now);
        _group.alpha = opacity;

        // CSS: left = nx, top = ny - 14, transform-origin = 상자 가운데, scale 은 translate(-50%,-100%) 뒤에 적용
        //  → 보이는 상자 가운데 = (nx + w(1-s)/2, ny - 14 + h(1/2 - s) + translateY)
        float centerX = _anchor.x + _width * (1f - scale) * 0.5f;
        float centerY = _anchor.y - _liftAboveHead + _height * (0.5f - scale) + translateY;
        RectTransform parent = (RectTransform)_root.parent;
        _root.anchoredPosition = new Vector2(centerX, parent.rect.height - centerY);
        _root.localScale = new Vector3(scale, scale, 1f);
    }

    private static void SetBox(RectTransform target, float left, float top, float width, float height)
    {
        target.anchorMin = new Vector2(0f, 1f);
        target.anchorMax = new Vector2(0f, 1f);
        target.pivot = new Vector2(0f, 1f);
        target.anchoredPosition = new Vector2(left, -top);
        target.sizeDelta = new Vector2(width, height);
    }
}
