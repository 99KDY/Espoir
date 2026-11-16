using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// 글자에 최대 세 겹의 가우스 그림자를 표시합니다.
/// 글자 메쉬가 바뀔 때만 글자 그림을 작은 렌더 텍스처에 굽고(Hidden/Espoir/UI/DamageGlyph — 데미지 숫자와 같은 방식),
/// Espoir/UI/TextShadowCss 로 그림자만 그린다. 이 오브젝트는 원본 글자 바로 앞 형제(아래 층)에 두고, 원본의 위치 · 크기 · 투명도를 매 프레임 따른다.
/// 글자 경계 밖으로 번지는 그림자가 잘리지 않도록 별도 텍스처를 사용합니다.
/// </summary>
[DefaultExecutionOrder(240)]
[RequireComponent(typeof(RawImage))]
public class CHudTextGlow : MonoBehaviour
{
    /// <summary>CSS text-shadow 한 겹: 색(sRGB rgba) · 위치(CSS px, 아래 +) · 흐림 반지름(px).</summary>
    [Serializable]
    public struct SShadow
    {
        public Color color;
        public Vector2 offset;
        public float blur;
    }

    private static readonly int _MAIN_TEX = Shader.PropertyToID("_MainTex");
    private static readonly int _SHADOW_TEX = Shader.PropertyToID("_ShadowTex");
    private static readonly int _SIGMA = Shader.PropertyToID("_Sigma");
    private static readonly int _S1 = Shader.PropertyToID("_S1");
    private static readonly int _S2 = Shader.PropertyToID("_S2");
    private static readonly int _S3 = Shader.PropertyToID("_S3");
    private static readonly int _OFF12 = Shader.PropertyToID("_Off12");
    private static readonly int _OFF3 = Shader.PropertyToID("_Off3");

    [SerializeField] private TMP_Text _source;
    [SerializeField] private Shader _glyphShader;
    [SerializeField] private Material _material;
    [Tooltip("평소 그림자 3겹 (CSS 순서: 첫 겹이 맨 위)")]
    [SerializeField] private SShadow[] _normal = new SShadow[3];
    [Tooltip("글자 색이 _altFaceColor 일 때 그림자 3겹 (.ux-count.max)")]
    [SerializeField] private SShadow[] _alt = new SShadow[3];
    [SerializeField] private Color _altFaceColor = new Color(1f, 210f / 255f, 122f / 255f);
    [SerializeField] private float _bakeScale = 1f;

    private RawImage _image;
    private RectTransform _rect;
    private Material _runtimeMaterial;
    private Material _glyphMaterial;
    private CommandBuffer _bake;
    private RenderTexture _texA;
    private RenderTexture _texB;
    private string _bakedText;
    private float _bakedSize = -1f;
    private bool _bakedAlt;
    private bool _isDirty = true;

    private void Awake()
    {
        _image = GetComponent<RawImage>();
        _rect = (RectTransform)transform;
        _image.raycastTarget = false;
        if (_material != null)
        {
            _runtimeMaterial = new Material(_material) { hideFlags = HideFlags.HideAndDontSave };
            _image.material = _runtimeMaterial;
        }

        if (_glyphShader != null) _glyphMaterial = new Material(_glyphShader) { hideFlags = HideFlags.HideAndDontSave };
        _bake = new CommandBuffer { name = "Hud Text Glow" };
        _image.enabled = false;
    }

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        _isDirty = true;
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    private void OnDestroy()
    {
        Release();
        if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
        if (_glyphMaterial != null) Destroy(_glyphMaterial);
        _bake?.Release();
    }

    private void LateUpdate()
    {
        if (_source == null || !_source.gameObject.activeInHierarchy || string.IsNullOrEmpty(_source.text))
        {
            _image.enabled = false;
            return;
        }

        RectTransform src = _source.rectTransform;
        _rect.localPosition = src.localPosition;
        _rect.localRotation = src.localRotation;
        _rect.localScale = src.localScale;
        Color c = _image.color;
        c.a = _source.alpha;
        _image.color = c;
        if (NeedsBake())
        {
            // 글자 메쉬는 보통 그리기 직전에 만들어짐 → 지금 만들어(기울임 OnPreRenderText 포함) 그 메쉬로 굽는다
            _source.ForceMeshUpdate();
            if (NeedsBake()) Bake();
        }

        _image.enabled = _texA != null;
    }

    // 글자 메쉬가 새로 만들어진 순간(그리기 전) — 같은 프레임에 새 숫자의 그림자를 굽는다
    private void OnTextChanged(UnityEngine.Object obj)
    {
        if (obj != _source || !isActiveAndEnabled) return;

        if (NeedsBake()) Bake();
    }

    private bool NeedsBake()
    {
        return _isDirty || _source.text != _bakedText || !Mathf.Approximately(_source.fontSize, _bakedSize)
            || IsAlt() != _bakedAlt;
    }

    private bool IsAlt()
    {
        Color f = _source.color;
        return Mathf.Abs(f.r - _altFaceColor.r) + Mathf.Abs(f.g - _altFaceColor.g) +
            Mathf.Abs(f.b - _altFaceColor.b) < 0.02f;
    }

    private void Bake()
    {
        if (_glyphMaterial == null || _runtimeMaterial == null) return;

        Mesh mesh = _source.mesh;
        if (mesh == null || mesh.vertexCount == 0) return;

        bool alt = IsAlt();
        SShadow[] set = alt ? _alt : _normal;
        if (set == null || set.Length < 3) return;

        // 여백 = 가장 넓은 그림자(3σ + 위치)
        float pad = 2f;
        for (int i = 0; i < 3; i++) pad = Mathf.Max(pad, 1.5f * set[i].blur + set[i].offset.magnitude + 2f);
        Bounds b = mesh.bounds;
        float w = Mathf.Ceil(b.size.x + pad * 2f);
        float h = Mathf.Ceil(b.size.y + pad * 2f);
        Canvas canvas = _source.canvas;
        float scale = _bakeScale * Mathf.Max(1f, canvas != null ? canvas.scaleFactor : 1f);
        int tw = Mathf.Max(4, Mathf.CeilToInt(w * scale));
        int th = Mathf.Max(4, Mathf.CeilToInt(h * scale));

        Release();
        _texA = RenderTexture.GetTemporary(tw, th, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        _texB = RenderTexture.GetTemporary(tw, th, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        _texA.filterMode = FilterMode.Bilinear;
        _texB.filterMode = FilterMode.Bilinear;
        RenderTexture mask = RenderTexture.GetTemporary(tw, th, 0, RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear);
        RenderTexture tmp = RenderTexture.GetTemporary(tw, th, 0, RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear);

        _glyphMaterial.SetTexture(_MAIN_TEX, _source.fontSharedMaterial.GetTexture(_MAIN_TEX));
        _bake.Clear();
        _bake.SetRenderTarget(mask);
        _bake.ClearRenderTarget(false, true, Color.clear);
        Vector3 cen = b.center;
        Matrix4x4 proj = Matrix4x4.Ortho(cen.x - w * 0.5f, cen.x + w * 0.5f, cen.y - h * 0.5f, cen.y + h * 0.5f,
            -10f, 10f);
        _bake.SetViewProjectionMatrices(Matrix4x4.identity, proj);
        _bake.DrawMesh(mesh, Matrix4x4.identity, _glyphMaterial, 0, 0);
        // CSS blur 반지름 b → σ = b / 2
        Graphics.ExecuteCommandBuffer(_bake);
        BlurInto(mask, tmp, _texA, set[0].blur * 0.5f * scale, set[1].blur * 0.5f * scale);
        BlurInto(mask, tmp, _texB, set[2].blur * 0.5f * scale, set[2].blur * 0.5f * scale);
        RenderTexture.ReleaseTemporary(mask);
        RenderTexture.ReleaseTemporary(tmp);

        _image.texture = _texA;
        _runtimeMaterial.SetTexture(_SHADOW_TEX, _texB);
        _runtimeMaterial.SetVector(_S1, Vec(set[0].color));
        _runtimeMaterial.SetVector(_S2, Vec(set[1].color));
        _runtimeMaterial.SetVector(_S3, Vec(set[2].color));
        // CSS 위치(아래 +) → 그림자는 글자보다 (x, −y) 만큼 옮겨 그림 → 표본 uv 에서 뺄 값
        _runtimeMaterial.SetVector(_OFF12,
            new Vector4(set[0].offset.x / w, -set[0].offset.y / h, set[1].offset.x / w, -set[1].offset.y / h));
        _runtimeMaterial.SetVector(_OFF3, new Vector4(set[2].offset.x / w, -set[2].offset.y / h, 0f, 0f));

        // 글자 그림 사각형 = 메쉬 경계 + 여백 (원본과 같은 국소 좌표: 피벗 위치를 원점으로)
        _rect.anchorMin = new Vector2(0.5f, 0.5f);
        _rect.anchorMax = new Vector2(0.5f, 0.5f);
        _rect.sizeDelta = new Vector2(w, h);
        _rect.pivot = new Vector2(0.5f - cen.x / w, 0.5f - cen.y / h);
        _rect.localPosition = _source.rectTransform.localPosition;

        _bakedText = _source.text;
        _bakedSize = _source.fontSize;
        _bakedAlt = alt;
        _isDirty = false;
    }

    private void BlurInto(RenderTexture mask, RenderTexture tmp, RenderTexture dst, float sigmaG, float sigmaB)
    {
        _glyphMaterial.SetVector(_SIGMA, new Vector4(Mathf.Max(0.5f, sigmaG), Mathf.Max(0.5f, sigmaB), 0f, 0f));
        Graphics.Blit(mask, tmp, _glyphMaterial, 1);
        Graphics.Blit(tmp, dst, _glyphMaterial, 2);
    }

    private static Vector4 Vec(Color c)
    {
        return new Vector4(c.r, c.g, c.b, c.a);
    }

    private void Release()
    {
        if (_texA != null) RenderTexture.ReleaseTemporary(_texA);
        if (_texB != null) RenderTexture.ReleaseTemporary(_texB);
        _texA = null;
        _texB = null;
    }
}
