using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 배경, 테두리, 안팎 그림자, 배경 흐림과 둥근 모서리를 가진 HUD 상자를 그립니다.
/// 값은 정점 데이터로 넘겨 모든 상자가 한 재질(Espoir/UI/HudBox)을 함께 쓴다. 색은 CSS 처럼 sRGB 값 그대로 넣는다.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class CHudBox : MaskableGraphic
{
    /// <summary>
    /// CSS border-style 에 해당하는 테두리 모양.
    /// </summary>
    public enum EBorderStyle
    {
        Solid = 0,
        Dashed = 1,
        TopOnly = 2,
    }

    [Header("모양")]
    [SerializeField] private bool _isCircle;
    [SerializeField, Min(0f)] private float _radius;

    [Header("배경 (background-color · linear-gradient)")]
    [SerializeField] private Color _fill = new Color(14f / 255f, 14f / 255f, 26f / 255f, 0.4f);
    [SerializeField] private Color _fillTo = new Color(14f / 255f, 14f / 255f, 26f / 255f, 0.4f);
    [SerializeField] private bool _hasGradient;
    [SerializeField] private float _gradientAngle = 180f;

    [Header("테두리")]
    [SerializeField, Min(0f)] private float _borderWidth = 1f;
    [SerializeField] private Color _borderColor = new Color(1f, 240f / 255f, 222f / 255f, 0.9f);
    [SerializeField] private EBorderStyle _borderStyle = EBorderStyle.Solid;

    [Header("box-shadow (바깥 1개 · inset 1개, 단위 px)")]
    [SerializeField, Min(0f)] private float _shadowBlur;
    [SerializeField] private Color _shadowColor = Color.clear;
    [SerializeField] private float _shadowOffsetY;
    [SerializeField, Min(0f)] private float _insetBlur;
    [SerializeField] private Color _insetColor = Color.clear;

    [Header("backdrop-filter blur (표준편차 px, 0 = 없음)")]
    [SerializeField, Min(0f)] private float _backdropBlur = 6f;

    [Header("훑는 빛 띠 모드")]
    [SerializeField] private bool _isSheen;
    [SerializeField] private float _sheenOffset = -1.25f;

    /// <summary>배경색(그라데이션이면 시작 색).</summary>
    public Color Fill
    {
        get
        {
            return _fill;
        }
        set
        {
            if (_fill != value)
            {
                _fill = value;
                if (!_hasGradient) _fillTo = value;
                SetVerticesDirty();
            }
        }
    }

    /// <summary>테두리 색.</summary>
    public Color BorderColor
    {
        get
        {
            return _borderColor;
        }
        set
        {
            if (_borderColor != value)
            {
                _borderColor = value;
                SetVerticesDirty();
            }
        }
    }

    /// <summary>테두리 두께(px).</summary>
    public float BorderWidth
    {
        get
        {
            return _borderWidth;
        }
        set
        {
            if (!Mathf.Approximately(_borderWidth, value))
            {
                _borderWidth = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }
    }

    /// <summary>테두리 모양(실선 · 점선 · 위쪽만).</summary>
    public EBorderStyle BorderStyle
    {
        get
        {
            return _borderStyle;
        }
        set
        {
            if (_borderStyle != value)
            {
                _borderStyle = value;
                SetVerticesDirty();
            }
        }
    }

    /// <summary>훑는 빛 띠의 가로 위치(폭 배수, CSS translateX %/100).</summary>
    public float SheenOffset
    {
        get
        {
            return _sheenOffset;
        }
        set
        {
            if (!Mathf.Approximately(_sheenOffset, value))
            {
                _sheenOffset = value;
                SetVerticesDirty();
            }
        }
    }

    /// <summary>
    /// 바깥 box-shadow (0 offsetY blur color) 를 바꿉니다.
    /// </summary>
    public void SetShadow(float blur, Color color, float offsetY = 0f)
    {
        if (Mathf.Approximately(_shadowBlur, blur) && _shadowColor == color
            && Mathf.Approximately(_shadowOffsetY, offsetY)) return;

        _shadowBlur = blur;
        _shadowColor = color;
        _shadowOffsetY = offsetY;
        SetVerticesDirty();
    }

    /// <summary>
    /// inset box-shadow (inset 0 0 blur color) 를 바꿉니다.
    /// </summary>
    public void SetInset(float blur, Color color)
    {
        if (Mathf.Approximately(_insetBlur, blur) && _insetColor == color) return;

        _insetBlur = blur;
        _insetColor = color;
        SetVerticesDirty();
    }

    /// <summary>
    /// 그라데이션 배경(linear-gradient(angle, from, to))을 바꿉니다.
    /// </summary>
    public void SetGradient(Color from, Color to, float angle)
    {
        _fill = from;
        _fillTo = to;
        _gradientAngle = angle;
        _hasGradient = true;
        SetVerticesDirty();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        CHudVertexPack.EnableChannels(canvas);
    }

    protected override void OnCanvasHierarchyChanged()
    {
        base.OnCanvasHierarchyChanged();
        CHudVertexPack.EnableChannels(canvas);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        Vector2 center = r.center;
        Vector2 half = r.size * 0.5f;
        float radius = _isCircle ? Mathf.Min(half.x, half.y) : _radius;
        float pad = Mathf.Max(_shadowBlur * 1.5f + Mathf.Abs(_shadowOffsetY), 0f) + 2f;

        Vector4 uv1 = new Vector4(radius, _borderWidth, _shadowBlur, _insetBlur);
        Vector4 uv2 = new Vector4(CHudVertexPack.Rgb(_fill), _fill.a,
            CHudVertexPack.Rgb(_hasGradient ? _fillTo : _fill),
            _hasGradient ? _fillTo.a : _fill.a);
        Vector4 uv3 = new Vector4(CHudVertexPack.Rgb(_borderColor), _borderColor.a, CHudVertexPack.Rgb(_shadowColor),
            _shadowColor.a);
        Vector4 tangent = new Vector4(CHudVertexPack.Rgb(_insetColor), _insetColor.a, _shadowOffsetY, _backdropBlur);
        int flags = (_borderStyle == EBorderStyle.Dashed ? 1 : 0) | (_borderStyle == EBorderStyle.TopOnly ? 2 : 0) |
            (_isSheen ? 4 : 0);
        Vector3 normal = new Vector3(_hasGradient ? _gradientAngle : -1f, flags, _sheenOffset);
        Color32 c = color;

        CHudVertexPack.AddQuad(vh, center, half + new Vector2(pad, pad), half, c, uv1, uv2, uv3, tangent, normal);
    }
}
