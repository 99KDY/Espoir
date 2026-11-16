using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// CUnit.OnDamageResolved의 확정 피해 결과를 받아 대상 머리 위에 숫자를 표시합니다.
/// 숫자별 글자 마스크를 작은 렌더 텍스처에 한 번 굽고 재사용합니다.
/// r은 글자, g와 b는 서로 다른 크기로 흐린 마스크이며 색상·그림자·등장 섬광 합성에 사용합니다.
/// 숫자 오브젝트 · 재질은 시작할 때 미리 만들어 두고 돌려 쓴다. 맞을 때마다 문자열을 만들지 않는다.
/// </summary>
[DefaultExecutionOrder(100)]
public class CDamageNumberSpawner : MonoBehaviour
{
    private enum ETier
    {
        Normal,
        Critical,
        Taken
    }

    [Serializable]
    private struct STierStyle
    {
        [Tooltip("글자 크기 (1080 기준 px)")]
        public float fontSize;
        [Tooltip("글자 위쪽 색상 (sRGB)")]
        public Vector4 faceTop;
        [Tooltip("글자 아래쪽 색상")]
        public Vector4 faceBottom;
        [Tooltip("음영 색상 (RGB)과 세기 (W)")]
        public Vector4 shade;
        [Tooltip("훑는 빛 색상 (HDR)")]
        public Vector4 sweep;
    }

    [Serializable]
    private struct STargetPlacement
    {
        [Tooltip("콜라이더 꼭대기에서 위로 (m)")]
        public float heightAboveTop;
        [Tooltip("좌우로 벌어지는 거리 범위 (1080 기준 px). 숫자마다 왼쪽 · 오른쪽을 번갈아 씀")]
        public Vector2 sideRange;
        [Tooltip("기준점 아래로 내려오는 거리 범위 (1080 기준 px, 음수면 위)")]
        public Vector2 downRange;
    }

    private class CItem
    {
        public RectTransform rect;
        public RawImage image;
        public Material material;
        public RectTransform moteRect;
        public Image mote;
        public RenderTexture glyph;
        public readonly char[] CHARS = new char[_MAX_CHARS];
        public Transform follow;
        public Vector3 anchor;
        public float height;
        public Vector2 offset;
        public Vector2 moteOffset;
        public float moteLife;
        public float moteSize;
        public float age;
        public ETier tier;
        public bool hasMote;
        public bool isCritical;
        public bool isActive;
    }

    private const int _MAX_CHARS = 16;
    private const char _MINUS = '−';
    private static readonly int _C0 = Shader.PropertyToID("_C0");
    private static readonly int _C1 = Shader.PropertyToID("_C1");
    private static readonly int _SHADE = Shader.PropertyToID("_Shade");
    private static readonly int _C2 = Shader.PropertyToID("_C2");
    private static readonly int _FX = Shader.PropertyToID("_Fx");
    private static readonly int _SIGMA = Shader.PropertyToID("_Sigma");
    private static readonly int _MAIN_TEX = Shader.PropertyToID("_MainTex");

    [SerializeField] private Camera _camera;
    [Tooltip("글자 메쉬만 만드는 TMP (화면에는 그리지 않음). 글꼴 Saira Condensed 800 · 가운데 정렬")]
    [SerializeField] private TextMeshProUGUI _template;
    [SerializeField] private int _poolSize = 48;
    [SerializeField] private Shader _glyphShader;
    [SerializeField] private Material _flatMaterial;
    [SerializeField] private Material _moteMaterial;

    [Header("피해 종류별 모양")]
    [SerializeField] private STierStyle _normal;
    [SerializeField] private STierStyle _critical;
    [SerializeField] private STierStyle _taken;
    [SerializeField] private float _takenCriticalSizeBonus = 4f;

    [Header("글자 마스크")]
    [Tooltip("글자 둘레 여백 (글자 크기 배수)")]
    [SerializeField] private float _padding = 0.55f;
    [Tooltip("글자 줄 높이 (글자 크기 배수)")]
    [SerializeField] private float _lineHeight = 1.35f;
    [Tooltip("작은 흐림 반경: X는 최소 픽셀, Y는 글자 크기 배수. 둘 중 큰 값을 사용")]
    [SerializeField] private Vector2 _blurSmall = new Vector2(1.2f, 0.06f);
    [Tooltip("큰 흐림 반경: X는 최소 픽셀, Y는 글자 크기 배수. 둘 중 큰 값을 사용")]
    [SerializeField] private Vector2 _blurLarge = new Vector2(3f, 0.22f);
    [Tooltip("글자 마스크 해상도 배수")]
    [SerializeField] private float _bakeScale = 2f;
    [Tooltip("글자를 줄 가운데에서 아래로 옮기는 거리 (글자 크기 배수)")]
    [SerializeField] private float _baselineShift = 0.05f;

    [Header("숫자 움직임")]
    [SerializeField] private float _life = 1.25f;
    [SerializeField] private float _riseFast = 14f;
    [SerializeField] private float _riseFastTime = 0.25f;
    [SerializeField] private float _riseSlow = 10f;
    [SerializeField] private float _popFrom = 0.9f;
    [SerializeField] private float _popTime = 0.22f;
    [SerializeField] private float _fadeInTime = 0.05f;
    [SerializeField, Range(0f, 1f)] private float _fadeOutFrom = 0.74f;
    [SerializeField] private float _sweepTime = 0.18f;

    [Header("치명타 섬광")]
    [SerializeField] private float _moteDelay = 0.12f;
    [SerializeField] private float _moteLife = 0.42f;
    [SerializeField] private float _moteSize = 4.5f;
    [SerializeField] private float _moteRise = 40f;
    [Tooltip("숫자 가운데 기준 (x = 글자 폭 배수, y = 글자 크기 배수, 위 +)")]
    [SerializeField] private Vector2 _moteAnchor = new Vector2(0.44f, 0.34f);

    [Header("대상별 자리")]
    [SerializeField] private STargetPlacement _mob;
    [SerializeField] private STargetPlacement _boss;
    [SerializeField] private STargetPlacement _hero;
    [SerializeField] private float _defaultHeight = 2f;

    private Canvas _canvas;
    private CItem[] _items;
    private float _side = 1f;
    private Material _glyphMaterial;
    private CommandBuffer _bake;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        _glyphMaterial = new Material(_glyphShader) { hideFlags = HideFlags.HideAndDontSave };
        _bake = new CommandBuffer { name = "Damage Number Glyph" };
        _items = new CItem[Mathf.Max(1, _poolSize)];
        for (int i = 0; i < _items.Length; i++)
        {
            _items[i] = CreateItem(i);
        }

        // 글자 메쉬만 만들고 화면에는 그리지 않음: 템플릿에 꺼진 캔버스를 붙여 둔다(오브젝트는 켜 둬야 TMP 가 새 글자로 메쉬를 다시 만듦)
        _template.gameObject.SetActive(true);
        Canvas hidden = _template.GetComponent<Canvas>();
        if (hidden == null) hidden = _template.gameObject.AddComponent<Canvas>();
        hidden.enabled = false;
        // 가장 긴 숫자로 한 번 만들어 TMP 내부 버퍼를 미리 키워 둔다.
        int length = FormatNumber(_items[0].CHARS, int.MaxValue, true);
        _template.SetCharArray(_items[0].CHARS, 0, length);
        _template.ForceMeshUpdate(false, true);
    }

    private void OnEnable()
    {
        CUnit.OnDamageResolved += HandleDamageResolved;
    }

    private void OnDisable()
    {
        CUnit.OnDamageResolved -= HandleDamageResolved;
        if (_items == null) return;

        for (int i = 0; i < _items.Length; i++)
        {
            Release(_items[i]);
        }
    }

    private void OnDestroy()
    {
        _bake?.Release();
        if (_glyphMaterial != null) Destroy(_glyphMaterial);
        if (_items == null) return;

        foreach (CItem item in _items)
        {
            if (item.material != null) Destroy(item.material);
        }
    }

    private CItem CreateItem(int index)
    {
        GameObject go = new GameObject("DamageNumber" + index, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(RawImage));
        go.transform.SetParent(_template.transform.parent, false);
        CItem item = new CItem();
        item.rect = (RectTransform)go.transform;
        item.rect.anchorMin = item.rect.anchorMax = Vector2.zero;
        item.rect.pivot = new Vector2(0.5f, 0.5f);
        item.image = go.GetComponent<RawImage>();
        item.image.raycastTarget = false;
        item.material = new Material(_flatMaterial) { hideFlags = HideFlags.HideAndDontSave };
        item.image.material = item.material;

        GameObject mote = new GameObject("Mote", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        mote.transform.SetParent(_template.transform.parent, false);
        item.moteRect = (RectTransform)mote.transform;
        item.moteRect.anchorMin = item.moteRect.anchorMax = Vector2.zero;
        item.mote = mote.GetComponent<Image>();
        item.mote.material = _moteMaterial;
        item.mote.raycastTarget = false;
        mote.SetActive(false);
        go.SetActive(false);
        return item;
    }

    private void HandleDamageResolved(SDamageResult result)
    {
        if (result.target == null || result.hpLost <= 0f) return;

        int value = Mathf.RoundToInt(result.damage);
        if (value <= 0) return;

        bool isTaken = result.target is CPlayer;
        ETier tier = isTaken ? ETier.Taken : result.isCritical ? ETier.Critical : ETier.Normal;
        STargetPlacement placement = isTaken ? _hero : result.target is CSlothBoss ? _boss : _mob;

        Transform follow = result.target.transform;
        float height = _defaultHeight;
        if (result.target.TryGetComponent(out Collider bodyCollider) && bodyCollider.enabled)
        {
            height = bodyCollider.bounds.max.y - follow.position.y;
        }

        CItem item = TakeItem();
        _side = -_side;
        item.follow = follow;
        item.height = height + placement.heightAboveTop;
        item.anchor = result.worldPosition + Vector3.up * item.height;
        item.offset = new Vector2(_side * UnityEngine.Random.Range(placement.sideRange.x, placement.sideRange.y),
            -UnityEngine.Random.Range(placement.downRange.x, placement.downRange.y));
        item.age = 0f;
        item.tier = tier;
        item.isCritical = result.isCritical;
        item.isActive = true;

        Show(item, value, isTaken, result.isCritical);
        UpdateItem(item, _canvas.scaleFactor);
    }

    private CItem TakeItem()
    {
        CItem oldest = _items[0];
        for (int i = 0; i < _items.Length; i++)
        {
            if (!_items[i].isActive) return _items[i];
            if (_items[i].age > oldest.age) oldest = _items[i];
        }

        // 풀이 모두 쓰이는 중이면 가장 오래된 숫자를 다시 쓴다.
        Release(oldest);
        return oldest;
    }

    private void Show(CItem item, int value, bool isTaken, bool isCritical)
    {
        STierStyle style = GetStyle(item.tier);
        float size = style.fontSize + (isTaken && isCritical ? _takenCriticalSizeBonus : 0f);
        int length = FormatNumber(item.CHARS, value, isTaken);

        // 글자 메쉬 (TMP, 화면에는 안 그림)
        _template.fontSize = size;
        _template.SetCharArray(item.CHARS, 0, length);
        _template.ForceMeshUpdate(false, true);
        Mesh mesh = _template.mesh;
        float textWidth = _template.preferredWidth;

        // 글자 폭과 높이 양쪽에 흐림 효과용 여백을 확보합니다.
        float pad = Mathf.Round(size * _padding);
        float w = Mathf.Ceil(textWidth) + pad * 2f;
        float h = Mathf.Ceil(size * _lineHeight) + pad * 2f;
        float scale = _bakeScale * Mathf.Max(1f, _canvas.scaleFactor);
        int tw = Mathf.CeilToInt(w * scale);
        int th = Mathf.CeilToInt(h * scale);
        item.glyph = RenderTexture.GetTemporary(tw, th, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        item.glyph.filterMode = FilterMode.Bilinear;
        RenderTexture mask = RenderTexture.GetTemporary(tw, th, 0, RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear);
        RenderTexture tmp = RenderTexture.GetTemporary(tw, th, 0, RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear);

        float shift = size * _baselineShift;
        Material fontMaterial = _template.fontSharedMaterial;
        _glyphMaterial.SetTexture(_MAIN_TEX, fontMaterial.GetTexture(_MAIN_TEX));
        float sigmaSmall = Mathf.Max(_blurSmall.x, size * _blurSmall.y) * scale;
        float sigmaLarge = Mathf.Max(_blurLarge.x, size * _blurLarge.y) * scale;
        _glyphMaterial.SetVector(_SIGMA, new Vector4(sigmaSmall, sigmaLarge, 0f, 0f));

        _bake.Clear();
        _bake.SetRenderTarget(mask);
        _bake.ClearRenderTarget(false, true, Color.clear);
        Matrix4x4 proj = Matrix4x4.Ortho(-w * 0.5f, w * 0.5f, -h * 0.5f - shift, h * 0.5f - shift, -10f, 10f);
        _bake.SetViewProjectionMatrices(Matrix4x4.identity, proj);
        _bake.DrawMesh(mesh, Matrix4x4.identity, _glyphMaterial, 0, 0);
        _bake.Blit(mask, tmp, _glyphMaterial, 1);
        _bake.Blit(tmp, item.glyph, _glyphMaterial, 2);
        Graphics.ExecuteCommandBuffer(_bake);
        RenderTexture.ReleaseTemporary(mask);
        RenderTexture.ReleaseTemporary(tmp);

        item.image.texture = item.glyph;
        item.rect.sizeDelta = new Vector2(w, h);
        item.material.SetVector(_C0, style.faceTop);
        item.material.SetVector(_C1, style.faceBottom);
        item.material.SetVector(_SHADE, style.shade);
        item.material.SetVector(_C2, style.sweep);
        item.rect.gameObject.SetActive(true);
        item.rect.SetAsLastSibling();

        // 플레이어가 준 치명타에는 숫자 오른쪽 위에 지연 섬광을 추가합니다.
        item.hasMote = isCritical && !isTaken;
        if (item.hasMote)
        {
            item.moteOffset = new Vector2(textWidth * _moteAnchor.x, size * _moteAnchor.y);
            item.moteLife = _moteLife * UnityEngine.Random.Range(0.7f, 1.3f);
            item.moteSize = _moteSize * UnityEngine.Random.Range(0.6f, 1.4f);
            item.moteRect.sizeDelta = Vector2.one * item.moteSize * 6f;      // 그림 범위 = 반지름 3배(블룸 포함)
            item.moteRect.SetAsLastSibling();
        }
    }

    private void LateUpdate()
    {
        float scaleFactor = _canvas.scaleFactor;
        for (int i = 0; i < _items.Length; i++)
        {
            CItem item = _items[i];
            if (!item.isActive) continue;

            item.age += Time.deltaTime;
            if (item.age >= _life)
            {
                Release(item);
                continue;
            }

            UpdateItem(item, scaleFactor);
        }
    }

    private void UpdateItem(CItem item, float scaleFactor)
    {
        if (item.follow != null && item.follow.gameObject.activeInHierarchy)
        {
            item.anchor = item.follow.position + Vector3.up * item.height;
        }

        float t = item.age;
        float k = t / _life;
        Vector3 screen = _camera != null ? _camera.WorldToScreenPoint(item.anchor) : Vector3.zero;
        bool isVisible = _camera != null && screen.z > 0f;
        Vector2 basePos = new Vector2(screen.x / scaleFactor + item.offset.x, screen.y / scaleFactor + item.offset.y);

        // 초반 상승과 남은 수명의 이동량을 합친다. 주는 피해는 위로, 받는 피해는 아래로 움직인다.
        float direction = item.tier == ETier.Taken ? -1f : 1f;
        float rise = _riseFast * EaseOut(t / _riseFastTime) + _riseSlow * k;
        item.rect.anchoredPosition = basePos + new Vector2(0f, direction * rise);
        item.rect.localScale = Vector3.one * (_popFrom + (1f - _popFrom) * EaseOutBack(t / _popTime));

        float alpha = isVisible ? Mathf.Clamp01(t / _fadeInTime) * FadeOut(k, _fadeOutFrom) : 0f;
        // 받는 피해도 치명타이면 섬광 강도를 높입니다.
        item.material.SetVector(_FX, new Vector4(Mathf.Clamp01(t / _sweepTime), alpha, item.isCritical ? 1f : 0f, 1f));

        if (item.hasMote)
        {
            UpdateMote(item, basePos, t, isVisible);
        }
    }

    // 수명 초반에 나타나고 후반에 사라지며 위쪽으로 가속합니다.
    private void UpdateMote(CItem item, Vector2 basePos, float t, bool isVisible)
    {
        float age = t - _moteDelay;
        float k = age / item.moteLife;
        bool isOn = isVisible && k >= 0f && k < 1f;
        if (item.mote.gameObject.activeSelf != isOn) item.mote.gameObject.SetActive(isOn);
        if (!isOn) return;

        float va = (1f - Mathf.SmoothStep(0f, 1f, (k - 0.55f) / 0.45f)) * Mathf.SmoothStep(0f, 1f, k / 0.06f);
        item.mote.color = new Color(1f, 1f, 1f, va);
        item.moteRect.anchoredPosition = basePos + item.moteOffset + new Vector2(0f, 0.5f * _moteRise * age * age);
    }

    private void Release(CItem item)
    {
        item.isActive = false;
        item.follow = null;
        if (item.rect == null) return; // 씬을 닫는 중이면 이미 지워졌을 수 있음

        if (item.glyph != null)
        {
            item.image.texture = null;
            RenderTexture.ReleaseTemporary(item.glyph);
            item.glyph = null;
        }

        if (item.mote.gameObject.activeSelf) item.mote.gameObject.SetActive(false);
        if (item.rect.gameObject.activeSelf) item.rect.gameObject.SetActive(false);
    }

    private STierStyle GetStyle(ETier tier)
    {
        if (tier == ETier.Critical) return _critical;
        if (tier == ETier.Taken) return _taken;
        return _normal;
    }

    // 정수를 천 단위 쉼표와 함께 버퍼 앞쪽에 쓴다(string 을 만들지 않음). 반환값 = 글자 수
    private static int FormatNumber(char[] buffer, int value, bool withMinus)
    {
        int position = buffer.Length;
        int digits = 0;
        do
        {
            if (digits > 0 && digits % 3 == 0)
            {
                buffer[--position] = ',';
            }

            buffer[--position] = (char)('0' + value % 10);
            value /= 10;
            digits++;
        }
        while (value > 0);

        if (withMinus)
        {
            buffer[--position] = _MINUS;
        }

        int length = buffer.Length - position;
        Array.Copy(buffer, position, buffer, 0, length);
        return length;
    }

    private static float EaseOut(float t)
    {
        float u = 1f - Mathf.Clamp01(t);
        return 1f - u * u * u;
    }

    // 목표를 살짝 넘었다 돌아오는 오버슈트 곡선입니다.
    private static float EaseOutBack(float t)
    {
        float u = Mathf.Clamp01(t) - 1f;
        return 1f + 2.7f * u * u * u + 1.7f * u * u;
    }

    private static float FadeOut(float k, float from)
    {
        return k < from ? 1f : Mathf.Max(0f, 1f - (k - from) / (1f - from));
    }
}
