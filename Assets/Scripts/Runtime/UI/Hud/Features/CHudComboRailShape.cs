using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 평타 칸과 연계 막대의 배경, 테두리 및 그림자를 그립니다.
/// 좌표는 왼쪽 위 원점에서 아래 방향이 양수이며, 변형 전 좌표로 모양을 계산합니다.
/// 색은 sRGB 공간에서 지정합니다.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class CHudComboRailShape : MaskableGraphic
{
    /// <summary>배경 칠 방식.</summary>
    public enum EFill
    {
        Solid = 0,
        Horizontal = 1,
        Vertical = 2,
    }

    [Header("크기")]
    [SerializeField] private Vector2 _size = new Vector2(7f, 24f);

    [SerializeField] private float[] _matrix = { 1f, 0f, 0f, 1f, 0f, 0f };

    [Header("배경")]
    [SerializeField] private EFill _fill = EFill.Solid;
    [SerializeField] private Color[] _stops = { Color.clear, Color.clear, Color.clear, Color.clear };
    [SerializeField, Range(2, 4)] private int _stopCount = 2;

    [SerializeField, Range(0, 7)] private int _segment;
    [SerializeField, Range(1, 8)] private int _segmentCount = 1;

    [Header("테두리")]
    [SerializeField, Min(0f)] private float _borderWidth;
    [SerializeField] private Color _borderColor = Color.clear;

    [Header("그림자")]
    [SerializeField, Min(0f)] private float _shadowBlur1;
    [SerializeField] private Color _shadowColor1 = Color.clear;
    [SerializeField, Min(0f)] private float _shadowBlur2;
    [SerializeField] private Color _shadowColor2 = Color.clear;

    [Header("밝기 및 투명도")]
    [SerializeField, Min(0f)] private float _brightness = 1f;
    [SerializeField, Range(0f, 1f)] private float _opacity = 1f;

    /// <summary>
    /// 상자 크기와 변환(상자 좌표 → 줄 좌표, 2차원 아핀 변환)을 정합니다.
    /// </summary>
    public void SetGeometry(Vector2 size, float a, float b, float c, float d, float e, float f)
    {
        if (_matrix == null || _matrix.Length != 6) _matrix = new float[6];
        bool same = _size == size && _matrix[0] == a && _matrix[1] == b && _matrix[2] == c && _matrix[3] == d &&
            _matrix[4] == e && _matrix[5] == f;
        if (same) return;

        _size = size;
        _matrix[0] = a;
        _matrix[1] = b;
        _matrix[2] = c;
        _matrix[3] = d;
        _matrix[4] = e;
        _matrix[5] = f;
        SetVerticesDirty();
    }

    /// <summary>
    /// 단색 배경을 정합니다.
    /// </summary>
    public void SetSolid(Color c)
    {
        SetFill(EFill.Solid, 2, c, c, c, c, 0, 1);
    }

    /// <summary>
    /// 배경색을 설정합니다. segment와 segmentCount는 가로 그라데이션의 분할 위치와 개수입니다.
    /// </summary>
    public void SetFill(EFill fill, int stopCount, Color s0, Color s1, Color s2, Color s3,
        int segment, int segmentCount)
    {
        if (_stops == null || _stops.Length != 4) _stops = new Color[4];
        if (_fill == fill && _stopCount == stopCount && _stops[0] == s0 && _stops[1] == s1 && _stops[2] == s2 &&
            _stops[3] == s3 && _segment == segment && _segmentCount == segmentCount) return;

        _fill = fill;
        _stopCount = Mathf.Clamp(stopCount, 2, 4);
        _stops[0] = s0;
        _stops[1] = s1;
        _stops[2] = s2;
        _stops[3] = s3;
        _segment = segment;
        _segmentCount = Mathf.Max(1, segmentCount);
        SetVerticesDirty();
    }

    /// <summary>
    /// 테두리 두께(px)와 색을 정합니다.
    /// </summary>
    public void SetBorder(float width, Color c)
    {
        if (Mathf.Approximately(_borderWidth, width) && _borderColor == c) return;

        _borderWidth = Mathf.Max(0f, width);
        _borderColor = c;
        SetVerticesDirty();
    }

    /// <summary>
    /// 바깥 그림자 두 겹을 정합니다. 첫 겹이 위입니다.
    /// </summary>
    public void SetShadows(float blur1, Color c1, float blur2, Color c2)
    {
        if (Mathf.Approximately(_shadowBlur1, blur1) && _shadowColor1 == c1 &&
            Mathf.Approximately(_shadowBlur2, blur2) && _shadowColor2 == c2) return;

        _shadowBlur1 = blur1;
        _shadowColor1 = c1;
        _shadowBlur2 = blur2;
        _shadowColor2 = c2;
        SetVerticesDirty();
    }

    /// <summary>
    /// 밝기와 투명도를 정합니다.
    /// </summary>
    public void SetFilter(float brightness, float opacity)
    {
        if (Mathf.Approximately(_brightness, brightness) && Mathf.Approximately(_opacity, opacity)) return;

        _brightness = Mathf.Max(0f, brightness);
        _opacity = Mathf.Clamp01(opacity);
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
        if (_opacity <= 0f || _matrix == null || _matrix.Length != 6) return;

        Vector2 half = _size * 0.5f;
        float sigma1 = _shadowColor1.a > 0f ? _shadowBlur1 * 0.5f : 0f;
        float sigma2 = _shadowColor2.a > 0f ? _shadowBlur2 * 0.5f : 0f;
        float pad = 3f * Mathf.Max(sigma1, sigma2) + 2f;

        if (_stops == null || _stops.Length != 4) _stops = new Color[4];
        Color[] s = _stops;
        Vector4 uv1 = new Vector4(CHudVertexPack.Rgb(s[0]), s[0].a, CHudVertexPack.Rgb(s[1]), s[1].a);
        Vector4 uv2 = new Vector4(CHudVertexPack.Rgb(s[2]), s[2].a, CHudVertexPack.Rgb(s[3]), s[3].a);
        Vector4 uv3 = new Vector4(CHudVertexPack.Rgb(_borderColor), _borderColor.a, CHudVertexPack.Rgb(_shadowColor1),
            _shadowColor1.a);
        Vector4 tangent = new Vector4(CHudVertexPack.Rgb(_shadowColor2), _shadowColor2.a, sigma1, sigma2);
        int packed = (int)_fill + 4 * _stopCount + 32 * _segment + 256 * _segmentCount;
        Vector3 normal = new Vector3(_borderWidth, _brightness, packed);
        Color32 c = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(_opacity * 255f));

        int start = vh.currentVertCount;
        for (int i = 0; i < 4; i++)
        {
            float sx = i == 0 || i == 1 ? -1f : 1f;
            float sy = i == 0 || i == 3 ? 1f : -1f;
            float lx = sx * (half.x + pad);
            float ly = sy * (half.y + pad);
            float bx = lx + half.x;
            float by = ly + half.y;
            float x = _matrix[0] * bx + _matrix[2] * by + _matrix[4];
            float y = _matrix[1] * bx + _matrix[3] * by + _matrix[5];
            UIVertex v = UIVertex.simpleVert;
            v.position = new Vector3(x, -y, 0f);
            v.color = c;
            v.uv0 = new Vector4(lx, ly, half.x, half.y);
            v.uv1 = uv1;
            v.uv2 = uv2;
            v.uv3 = uv3;
            v.tangent = tangent;
            v.normal = normal;
            vh.AddVert(v);
        }

        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start + 2, start + 3, start);
    }
}
