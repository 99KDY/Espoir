using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Espoir/UI/CssBox 셰이더로 그리는 사각형 하나. 상자 크기와 요소마다 바뀌는 값(막대 비율 · 번쩍 · 빛 세기 등)을 정점 자료로 넘긴다.
/// 재질에는 CSS 모양 값(색 · 그라데이션 · 그림자)만 두고, 같은 모양의 요소들은 한 재질을 함께 써서 묶어 그린다.
/// 캔버스에 TexCoord1~3 · Normal · Tangent 채널이 켜져 있어야 한다.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class CCssBoxGraphic : MaskableGraphic
{
    [Tooltip("빛 번짐 · 그림자가 상자 밖으로 퍼질 여백 (px)")]
    [SerializeField] private float _outset = 0f;
    [SerializeField] private Texture _texture;

    private Vector4 _dynamicA;
    private Vector4 _dynamicB;
    private Vector3 _dynamicC;
    private Vector4 _dynamicD;

    /// <summary>
    /// 모드 0 에서 쓰는 그림(초상 등).
    /// </summary>
    public override Texture mainTexture => _texture != null ? _texture : s_WhiteTexture;

    /// <summary>
    /// 상자 밖 여백(px). 빛 번짐이 잘리지 않을 만큼 둔다.
    /// </summary>
    public float Outset
    {
        get => _outset;
        set
        {
            if (Mathf.Approximately(_outset, value)) return;
            _outset = value;
            SetVerticesDirty();
        }
    }

    /// <summary>
    /// 요소마다 바뀌는 셰이더 값. 값이 바뀔 때만 정점을 다시 만든다.
    /// </summary>
    public void SetDynamic(Vector4 a, Vector4 b, Vector3 c, Vector4 d)
    {
        if (a == _dynamicA && b == _dynamicB && c == _dynamicC && d == _dynamicD) return;

        _dynamicA = a;
        _dynamicB = b;
        _dynamicC = c;
        _dynamicD = d;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        if (r.width <= 0f || r.height <= 0f) return;

        float o = _outset;
        Vector4 size = new Vector4(r.width, r.height, 0f, 0f);
        Vector4 tangent = _dynamicD;
        Color32 c = color;

        // uv0: 상자 왼쪽 위 = (0, 0), 오른쪽 아래 = (1, 1). 여백 쪽은 0~1 밖.
        AddVertex(vh, r.xMin - o, r.yMin - o, -o / r.width, 1f + o / r.height, size, c, tangent);
        AddVertex(vh, r.xMin - o, r.yMax + o, -o / r.width, -o / r.height, size, c, tangent);
        AddVertex(vh, r.xMax + o, r.yMax + o, 1f + o / r.width, -o / r.height, size, c, tangent);
        AddVertex(vh, r.xMax + o, r.yMin - o, 1f + o / r.width, 1f + o / r.height, size, c, tangent);
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

    private void AddVertex(VertexHelper vh, float x, float y, float u, float v, Vector4 size, Color32 c,
        Vector4 tangent)
    {
        UIVertex vert = UIVertex.simpleVert;
        vert.position = new Vector3(x, y, 0f);
        vert.color = c;
        vert.uv0 = new Vector4(u, v, 0f, 0f);
        vert.uv1 = size;
        vert.uv2 = _dynamicA;
        vert.uv3 = _dynamicB;
        vert.normal = _dynamicC;
        vert.tangent = tangent;
        vh.AddVert(vert);
    }
}
