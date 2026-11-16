using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 지정한 정렬 순서 구간의 UI를 sRGB 공간에서 합성합니다.
/// 시작 화면 사각형에서 장면 색을 sRGB로 바꾸고, UI 합성이 끝나면 Linear로 되돌립니다.
/// 기본 UI와 TMP 재질은 감마 출력 사본으로 교체하며 비활성화할 때 원본을 복원합니다.
/// HUD 전용 셰이더는 _UiGammaOut 전역 값으로 합성 여부를 확인합니다.
/// </summary>
public class CUiSrgbComposite : MonoBehaviour
{
    [System.Serializable]
    private struct SRange
    {
        [Tooltip("시작 캔버스 정렬 순서 (구간 안 캔버스들보다 작게)")]
        public int beginOrder;
        [Tooltip("끝 캔버스 정렬 순서 (구간 안 캔버스들보다 크게)")]
        public int endOrder;
        public Material begin;
        public Material end;
    }

    private const string _TMP_SDF = "TextMeshPro/Distance Field";
    private const string _TMP_SDF_MOBILE = "TextMeshPro/Mobile/Distance Field";
    private static readonly int _GAMMA_OUT = Shader.PropertyToID("_UiGammaOut");
    private static CUiSrgbComposite _owner;

    [SerializeField] private SRange[] _ranges = new SRange[0];
    [Tooltip("구간 안 기본 이미지 · 글자에 쓸 재질 (Espoir/UI/DefaultGamma)")]
    [SerializeField] private Material _defaultGamma;
    [SerializeField] private Shader _tmpGamma;
    [SerializeField] private Shader _tmpMobileGamma;
    [Tooltip("구간 안 캔버스 목록을 다시 찾는 간격 (초)")]
    [SerializeField] private float _rescanInterval = 0.5f;

    private GameObject _root;
    private readonly List<Canvas> _RANGE_CANVASES = new List<Canvas>();
    private readonly HashSet<Canvas> _OWN = new HashSet<Canvas>();
    private readonly Dictionary<Material, Material> _GAMMA_COPIES = new Dictionary<Material, Material>();
    private readonly Dictionary<Graphic, Material> _ORIGINAL_MATERIALS = new Dictionary<Graphic, Material>();
    private float _nextScan;
    private int _materialFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        _owner = null;
    }

    private void OnEnable()
    {
        if (!Application.isPlaying || _owner != null) return;

        Activate();
    }

    private void OnDisable()
    {
        if (_owner != this) return;

        Deactivate();
        // 씬을 옮길 때 새 씬의 것이 먼저 켜져 있었으면 그것이 이어받음
        foreach (CUiSrgbComposite other in FindObjectsByType<CUiSrgbComposite>(FindObjectsSortMode.None))
        {
            if (other != this && other.isActiveAndEnabled)
            {
                other.Activate();
                break;
            }
        }
    }

    private void OnDestroy()
    {
        foreach (Material m in _GAMMA_COPIES.Values)
        {
            if (m != null && m != _defaultGamma && m.hideFlags == HideFlags.DontSave) Destroy(m);
        }

        _GAMMA_COPIES.Clear();
    }

    private void Activate()
    {
        _owner = this;
        _root = new GameObject("UiSrgbComposite");
        SceneManager.MoveGameObjectToScene(_root, gameObject.scene);
        _OWN.Clear();
        for (int i = 0; i < _ranges.Length; i++)
        {
            SRange r = _ranges[i];
            if (r.begin == null || r.end == null) continue;

            MakeQuad("Begin" + i, r.beginOrder, r.begin);
            MakeQuad("End" + i, r.endOrder, r.end);
        }

        Shader.SetGlobalFloat(_GAMMA_OUT, 1f);
        _nextScan = 0f;
        Canvas.willRenderCanvases += FixMaterials;
    }

    private void Deactivate()
    {
        _owner = null;
        Canvas.willRenderCanvases -= FixMaterials;
        RestoreMaterials();
        Shader.SetGlobalFloat(_GAMMA_OUT, 0f);
        if (_root != null) Destroy(_root);
        _root = null;
        _RANGE_CANVASES.Clear();
        _materialFrame = -1;
    }

    private void MakeQuad(string name, int order, Material material)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
        go.transform.SetParent(_root.transform, false);
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        _OWN.Add(canvas);
        GameObject quad = new GameObject("Quad", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        quad.transform.SetParent(go.transform, false);
        RectTransform rt = (RectTransform)quad.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image image = quad.GetComponent<Image>();
        image.material = material;
        image.raycastTarget = false;
    }

    // 그리기 직전 매 프레임: 구간 안 캔버스의 그래픽 중 기본 · TMP 재질을 감마 출력 사본으로 (이미 바꾼 것은 참조 비교만)
    private void FixMaterials()
    {
        if (_materialFrame == Time.frameCount) return;

        _materialFrame = Time.frameCount;
        if (Time.unscaledTime >= _nextScan) Rescan();

        for (int c = 0; c < _RANGE_CANVASES.Count; c++)
        {
            Canvas canvas = _RANGE_CANVASES[c];
            if (canvas == null) continue;

            IList<Graphic> graphics = GraphicRegistry.GetGraphicsForCanvas(canvas);
            for (int i = 0; i < graphics.Count; i++)
            {
                Fix(graphics[i]);
            }
        }
    }

    private void Rescan()
    {
        _nextScan = Time.unscaledTime + _rescanInterval;
        _RANGE_CANVASES.Clear();
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int c = 0; c < canvases.Length; c++)
        {
            Canvas canvas = canvases[c];
            Canvas root = canvas.rootCanvas;
            if (root == null || root.renderMode != RenderMode.ScreenSpaceOverlay || _OWN.Contains(root)) continue;

            for (int i = 0; i < _ranges.Length; i++)
            {
                if (root.sortingOrder > _ranges[i].beginOrder && root.sortingOrder < _ranges[i].endOrder)
                {
                    _RANGE_CANVASES.Add(canvas);
                    break;
                }
            }
        }
    }

    private void Fix(Graphic g)
    {
        if (g == null) return;

        if (g is TMP_Text text)
        {
            Material m = text.fontSharedMaterial;
            Material gm = GammaCopy(m);
            if (gm != m)
            {
                _ORIGINAL_MATERIALS[g] = m;
                text.fontSharedMaterial = gm;
            }
            return;
        }

        if (g is TMP_SubMeshUI sub)
        {
            Material m = sub.sharedMaterial;
            Material gm = GammaCopy(m);
            if (gm != m)
            {
                _ORIGINAL_MATERIALS[g] = m;
                sub.sharedMaterial = gm;
            }
            return;
        }

        Material mat = g.material;
        if (mat == null) return;

        if (mat == g.defaultMaterial)
        {
            if (_defaultGamma != null)
            {
                _ORIGINAL_MATERIALS[g] = mat;
                g.material = _defaultGamma;
            }
            return;
        }

        Material copy = GammaCopy(mat);
        if (copy != mat)
        {
            _ORIGINAL_MATERIALS[g] = mat;
            g.material = copy;
        }
    }

    private void RestoreMaterials()
    {
        foreach (KeyValuePair<Graphic, Material> pair in _ORIGINAL_MATERIALS)
        {
            Graphic graphic = pair.Key;
            if (graphic == null) continue;

            if (graphic is TMP_Text text)
            {
                text.fontSharedMaterial = pair.Value;
            }

            else if (graphic is TMP_SubMeshUI sub)
            {
                sub.sharedMaterial = pair.Value;
            }

            else
            {
                graphic.material = pair.Value;
            }
        }

        _ORIGINAL_MATERIALS.Clear();
    }

    // 기본 UI와 TMP 재질의 감마 출력 사본을 재사용합니다. 다른 셰이더는 변경하지 않습니다.
    private Material GammaCopy(Material m)
    {
        if (m == null) return null;
        if (_GAMMA_COPIES.TryGetValue(m, out Material cached)) return cached;

        Shader target = null;
        string name = m.shader != null ? m.shader.name : "";
        if (name == _TMP_SDF) target = _tmpGamma;
        else if (name == _TMP_SDF_MOBILE) target = _tmpMobileGamma;
        else if (name == "UI/Default" && _defaultGamma != null) target = _defaultGamma.shader;

        if (target == null)
        {
            _GAMMA_COPIES[m] = m;
            return m;
        }

        Material copy = new Material(m) { name = m.name + " (sRGB)", hideFlags = HideFlags.DontSave };
        copy.shader = target;
        copy.shaderKeywords = m.shaderKeywords;
        _GAMMA_COPIES[m] = copy;
        _GAMMA_COPIES[copy] = copy;
        return copy;
    }
}
