using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Espoir/UI/HudRing 재질로 궁극기와 흐름 고리를 그립니다.
/// 호 길이(전투의 흐름 비율)는 다른 뷰가 Flow 로 넣고, conic 그라데이션 회전(ugr-spin)은 여기서 돌린다.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class CHudRing : MaskableGraphic
{
    private static readonly int _RING = Shader.PropertyToID("_Ring");
    private static readonly int _TRACK = Shader.PropertyToID("_Track");
    private static readonly int _TRACK_SHADOW = Shader.PropertyToID("_TrackShadow");
    private static readonly int _ARC = Shader.PropertyToID("_Arc");
    private static readonly int _ARC_SHADOW = Shader.PropertyToID("_ArcShadow");
    private static readonly int _BLUR = Shader.PropertyToID("_Blur");
    private static readonly int[] _STOPS =
    {
        Shader.PropertyToID("_Stop0"), Shader.PropertyToID("_Stop1"), Shader.PropertyToID("_Stop2"),
        Shader.PropertyToID("_Stop3"), Shader.PropertyToID("_Stop4"),
    };

    [Header("마스크 반지름 (px, closest-side 기준)")]
    [SerializeField] private float _maskFrom = 46.8f;
    [SerializeField] private float _maskTo = 47.84f;
    [SerializeField] private bool _isUnionShadow;

    [Header("색 (CSS 값, sRGB)")]
    [SerializeField] private Color _track = new Color(1f, 1f, 1f, 0.16f);
    [SerializeField] private Color _trackShadow = new Color(1f, 192f / 255f, 112f / 255f, 0.7f);
    [SerializeField] private float _trackShadowBlur = 5f;
    [SerializeField] private Color _arcShadow = new Color(1f, 170f / 255f, 90f / 255f, 0.75f);
    [SerializeField] private float _arcShadowBlur = 6f;
    [SerializeField] private Color[] _stops = new Color[5];
    [SerializeField] private float _spinTime = 6f;

    private Material _runtime;
    private float _flow;

    /// <summary>흐름 호의 비율 0~1 (CSS --flow).</summary>
    public float Flow
    {
        get { return _flow; }
        set { _flow = Mathf.Clamp01(value); }
    }

    public override Material materialForRendering
    {
        get
        {
            if (_runtime == null && material != null && material.shader != null
                && material.shader.name == "Espoir/UI/HudRing")
            {
                _runtime = new Material(material) { name = material.name + " (" + name + ")" };
                _runtime.hideFlags = HideFlags.DontSave;
            }

            return _runtime != null ? _runtime : base.materialForRendering;
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
        float pad = Mathf.Max(_trackShadowBlur, _arcShadowBlur) * 1.5f + 2f;
        Vector2 c = r.center;
        Vector2 h = r.size * 0.5f + new Vector2(pad, pad);
        Color32 col = color;
        vh.AddVert(new Vector3(c.x - h.x, c.y - h.y), col, new Vector2(-h.x, -h.y));
        vh.AddVert(new Vector3(c.x - h.x, c.y + h.y), col, new Vector2(-h.x, h.y));
        vh.AddVert(new Vector3(c.x + h.x, c.y + h.y), col, new Vector2(h.x, h.y));
        vh.AddVert(new Vector3(c.x + h.x, c.y - h.y), col, new Vector2(h.x, -h.y));
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

    private void LateUpdate()
    {
        if (materialForRendering == null || _runtime == null) return;

        Rect r = rectTransform.rect;
        float outer = Mathf.Min(r.width, r.height) * 0.5f;
        float spin = _spinTime > 0f ? Mathf.Repeat(Time.unscaledTime / _spinTime, 1f) * 360f : 0f;
        _runtime.SetVector(_RING, new Vector4(_maskFrom, _maskTo, outer, _isUnionShadow ? 1f : 0f));
        _runtime.SetVector(_TRACK, ToVector(_track));
        _runtime.SetVector(_TRACK_SHADOW, ToVector(_trackShadow));
        _runtime.SetVector(_ARC, new Vector4(_flow, spin, 0f, 0f));
        _runtime.SetVector(_ARC_SHADOW, ToVector(_arcShadow));
        _runtime.SetVector(_BLUR, new Vector4(_trackShadowBlur, _arcShadowBlur, 0f, 0f));
        for (int i = 0; i < _STOPS.Length && i < _stops.Length; i++)
        {
            _runtime.SetVector(_STOPS[i], ToVector(_stops[i]));
        }
    }

    private static Vector4 ToVector(Color c)
    {
        return new Vector4(c.r, c.g, c.b, c.a);
    }
}
