using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하단 HUD 묶음의 시작 · 끝 표시용 사각형. 자기 영역을 그리며 uv0 에 로컬 좌표(px)를 넣는다(Espoir/UI/HudGroupBegin · HudGroupEnd 용).
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class CHudGroupQuad : MaskableGraphic
{
    private Material _runtime;

    /// <summary>끝 사각형이 쓰는 재질 사본(속성을 매 프레임 넣음). 시작 사각형은 공용 재질 그대로.</summary>
    public Material RuntimeMaterial
    {
        get
        {
            if (_runtime == null && material != null && material != defaultMaterial)
            {
                _runtime = new Material(material) { name = material.name + " (" + name + ")",
                    hideFlags = HideFlags.DontSave };
            }

            return _runtime;
        }
    }

    public override Material materialForRendering
    {
        get
        {
            Material m = RuntimeMaterial;
            return m != null ? m : base.materialForRendering;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_runtime != null)
        {
            Destroy(_runtime);
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        Color32 c = color;
        vh.AddVert(new Vector3(r.xMin, r.yMin), c, new Vector2(r.xMin, r.yMin));
        vh.AddVert(new Vector3(r.xMin, r.yMax), c, new Vector2(r.xMin, r.yMax));
        vh.AddVert(new Vector3(r.xMax, r.yMax), c, new Vector2(r.xMax, r.yMax));
        vh.AddVert(new Vector3(r.xMax, r.yMin), c, new Vector2(r.xMax, r.yMin));
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }
}
