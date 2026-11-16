using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아이콘 Image 에 CSS filter: drop-shadow(color 0 0 blur) 를 붙인다(재질 Espoir/UI/HudIcon).
/// 그림자가 아이콘 밖으로 번지도록 사각형을 넓히고, 그림자 색 · 흐림 · 아이콘 글자색(svg color, 흰 그림에 곱함)을 정점 데이터로 넘긴다.
/// </summary>
[RequireComponent(typeof(Graphic))]
public class CHudIconShadow : BaseMeshEffect
{
    [SerializeField] private Color _shadowColor = new Color(1f, 192f / 255f, 112f / 255f, 0.4f);
    [SerializeField, Min(0f)] private float _blur = 4f;
    [Tooltip("아이콘 그림(흰색)에 곱하는 색 — 웹 svg 아이콘의 color. img 아이콘은 흰색 그대로.")]
    [SerializeField] private Color _tint = Color.white;

    private readonly List<UIVertex> _VERTS = new List<UIVertex>();

    /// <summary>
    /// drop-shadow 색과 흐림(px)을 바꿉니다.
    /// </summary>
    public void SetShadow(Color shadowColor, float blur)
    {
        if (_shadowColor == shadowColor && Mathf.Approximately(_blur, blur)) return;

        _shadowColor = shadowColor;
        _blur = blur;
        if (graphic != null) graphic.SetVerticesDirty();
    }

    /// <summary>
    /// 아이콘에 곱할 색상을 변경합니다. 알파는 사용하지 않습니다.
    /// </summary>
    public void SetTint(Color tint)
    {
        if (CHudVertexPack.Rgb(_tint) == CHudVertexPack.Rgb(tint)) return;

        _tint = tint;
        if (graphic != null) graphic.SetVerticesDirty();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (graphic != null) CHudVertexPack.EnableChannels(graphic.canvas);
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount != 4) return;

        _VERTS.Clear();
        vh.GetUIVertexStream(_VERTS);
        Vector2 pMin = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 pMax = new Vector2(float.MinValue, float.MinValue);
        Vector2 uMin = pMin;
        Vector2 uMax = pMax;
        for (int i = 0; i < _VERTS.Count; i++)
        {
            Vector2 p = _VERTS[i].position;
            Vector2 u = _VERTS[i].uv0;
            pMin = Vector2.Min(pMin, p);
            pMax = Vector2.Max(pMax, p);
            uMin = Vector2.Min(uMin, u);
            uMax = Vector2.Max(uMax, u);
        }

        Vector2 size = pMax - pMin;
        if (size.x <= 0f || size.y <= 0f) return;

        float pad = _blur * 1.5f + 1f;
        Vector2 duv = new Vector2((uMax.x - uMin.x) / size.x, (uMax.y - uMin.y) / size.y) * pad;
        Vector4 uv1 = new Vector4(CHudVertexPack.Rgb(_shadowColor), _shadowColor.a, _blur, 0f);
        Vector4 uv2 = new Vector4(size.x, size.y, CHudVertexPack.Rgb(_tint), 0f);
        Vector4 uv3 = new Vector4(uMin.x, uMin.y, uMax.x, uMax.y);
        for (int i = 0; i < _VERTS.Count; i++)
        {
            UIVertex v = _VERTS[i];
            Vector2 p = v.position;
            float sx = p.x <= (pMin.x + pMax.x) * 0.5f ? -1f : 1f;
            float sy = p.y <= (pMin.y + pMax.y) * 0.5f ? -1f : 1f;
            v.position = new Vector3(p.x + sx * pad, p.y + sy * pad, v.position.z);
            Vector4 uv0 = v.uv0;
            v.uv0 = new Vector4(uv0.x + sx * duv.x, uv0.y + sy * duv.y, uv0.z, uv0.w);
            v.uv1 = uv1;
            v.uv2 = uv2;
            v.uv3 = uv3;
            _VERTS[i] = v;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(_VERTS);
    }
}
