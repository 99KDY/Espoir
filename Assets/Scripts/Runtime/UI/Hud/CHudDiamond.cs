using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 테두리와 그림자가 있는 마름모를 그립니다.
/// </summary>
public class CHudDiamond : MaskableGraphic
{
    [SerializeField] private Color _fill = new Color(1f, 192f / 255f, 112f / 255f);
    [SerializeField] private Color _borderColor = Color.clear;
    [SerializeField, Min(0f)] private float _borderWidth;
    [SerializeField, Min(0f)] private float _shadowBlur;
    [SerializeField] private Color _shadowColor = Color.clear;

    private const float _SQRT_TWO = 1.41421356f;
    private const float _FEATHER = 0.75f;

    private static readonly float[] _SHADOW_STEP = { 0f, 0.25f, 0.5f, 0.75f, 1f };
    private static readonly float[] _SHADOW_ALPHA = { 0.5f, 0.31f, 0.16f, 0.067f, 0f };

    public Color Fill
    {
        get => _fill;
        set
        {
            if (_fill == value) return;
            _fill = value;
            SetVerticesDirty();
        }
    }

    public Color BorderColor
    {
        get => _borderColor;
        set
        {
            if (_borderColor == value) return;
            _borderColor = value;
            SetVerticesDirty();
        }
    }

    public float BorderWidth
    {
        get => _borderWidth;
        set
        {
            if (Mathf.Approximately(_borderWidth, value)) return;
            _borderWidth = value;
            SetVerticesDirty();
        }
    }

    /// <summary>
    /// 그림자의 너비와 색상을 설정합니다.
    /// </summary>
    public void SetShadow(float blur, Color color)
    {
        if (Mathf.Approximately(_shadowBlur, blur) && _shadowColor == color) return;

        _shadowBlur = blur;
        _shadowColor = color;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;

        float side = Mathf.Min(rect.width, rect.height);
        float outer = side * (_SQRT_TWO * 0.5f);
        float inner = Mathf.Max(0f, outer - _borderWidth * _SQRT_TWO);
        Color32 tint = color;

        if (_shadowBlur > 0f && _shadowColor.a > 0f) Shadow(vh, center, outer, tint);

        if (_fill.a > 0f) Solid(vh, center, inner, Tint(_fill, tint));

        Color edge = _borderWidth > 0f && _borderColor.a > 0f ? _borderColor : _fill;
        if (_borderWidth > 0f && _borderColor.a > 0f)
        {
            Color32 border = Tint(_borderColor, tint);
            Ring(vh, center, inner, outer, border, border);
        }
        Ring(vh, center, outer, outer + _FEATHER, Tint(edge, tint), Tint(Clear(edge), tint));
    }

    private void Shadow(VertexHelper vh, Vector2 center, float outer, Color32 tint)
    {
        for (int i = 0; i < _SHADOW_STEP.Length - 1; i++)
        {
            Color from = _shadowColor;
            Color to = _shadowColor;
            from.a *= _SHADOW_ALPHA[i];
            to.a *= _SHADOW_ALPHA[i + 1];
            float r0 = outer + _shadowBlur * _SHADOW_STEP[i] * _SQRT_TWO;
            float r1 = outer + _shadowBlur * _SHADOW_STEP[i + 1] * _SQRT_TWO;
            Ring(vh, center, r0, r1, Tint(from, tint), Tint(to, tint));
        }
    }

    private static Color32 Tint(Color c, Color32 tint)
    {
        return new Color(c.r * tint.r / 255f, c.g * tint.g / 255f, c.b * tint.b / 255f, c.a * tint.a / 255f);
    }

    private static Color Clear(Color c)
    {
        return new Color(c.r, c.g, c.b, 0f);
    }

    private static Vector2 Corner(Vector2 center, float radius, int index)
    {
        switch (index & 3)
        {
            case 0: return center + new Vector2(radius, 0f);
            case 1: return center + new Vector2(0f, radius);
            case 2: return center + new Vector2(-radius, 0f);
            default: return center + new Vector2(0f, -radius);
        }
    }

    private static void Solid(VertexHelper vh, Vector2 center, float radius, Color32 color)
    {
        if (radius <= 0f) return;

        int start = vh.currentVertCount;
        for (int i = 0; i < 4; i++) vh.AddVert(Corner(center, radius, i), color, Vector4.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    private static void Ring(VertexHelper vh, Vector2 center, float innerRadius, float outerRadius, Color32 innerColor,
        Color32 outerColor)
    {
        if (outerRadius <= innerRadius) return;

        int start = vh.currentVertCount;
        for (int i = 0; i < 4; i++)
        {
            vh.AddVert(Corner(center, innerRadius, i), innerColor, Vector4.zero);
            vh.AddVert(Corner(center, outerRadius, i), outerColor, Vector4.zero);
        }

        for (int i = 0; i < 4; i++)
        {
            int a = start + i * 2;
            int b = start + ((i + 1) & 3) * 2;
            vh.AddTriangle(a, a + 1, b + 1);
            vh.AddTriangle(a, b + 1, b);
        }
    }
}
