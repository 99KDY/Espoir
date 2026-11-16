using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Espoir/UI/HudBar 재질로 HP와 MP 막대를 그립니다.
/// 채움 비율은 팀 HUD(CPlayerUI_Build)가 쓰는 채움 이미지의 fillAmount 를 읽기만 한다.
/// 피격 전 체력 잔상은 0.25초 유지한 뒤 현재 체력을 따라갑니다.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class CHudBar : MaskableGraphic
{
    private static readonly int _BAR_SIZE = Shader.PropertyToID("_BarSize");
    private static readonly int _RATIO = Shader.PropertyToID("_Ratio");
    private static readonly int _TRACK = Shader.PropertyToID("_Track");
    private static readonly int[] _STOPS =
    {
        Shader.PropertyToID("_Stop0"), Shader.PropertyToID("_Stop1"), Shader.PropertyToID("_Stop2"),
        Shader.PropertyToID("_Stop3"), Shader.PropertyToID("_Stop4"),
    };
    private static readonly int _ANIM = Shader.PropertyToID("_Anim");
    private static readonly int _LAG = Shader.PropertyToID("_Lag");
    private static readonly int _LAG_FX = Shader.PropertyToID("_LagFx");
    private static readonly int _SHADOW1 = Shader.PropertyToID("_Shadow1");
    private static readonly int _SHADOW2 = Shader.PropertyToID("_Shadow2");
    private static readonly int _SHADOW_BLUR = Shader.PropertyToID("_ShadowBlur");
    private static readonly int _CRIMSON = Shader.PropertyToID("_Crimson");
    private static readonly int _CRIMSON_W = Shader.PropertyToID("_CrimsonW");

    [Header("데이터 (읽기만)")]
    [SerializeField] private Image _source;

    [Header("모양 · 색 (CSS 값, sRGB)")]
    [SerializeField] private float _slant = 11f;
    [SerializeField] private Color _track = new Color(78f / 255f, 29f / 255f, 33f / 255f, 0.59f);
    [SerializeField] private Color[] _stops = new Color[5];
    [SerializeField] private float _glossAlpha = 0.22f;
    [SerializeField] private float _sheenAlpha = 0.3f;
    [SerializeField] private Color _shadow1 = new Color(1f, 106f / 255f, 80f / 255f, 0.5f);
    [SerializeField] private float _shadow1Blur = 3f;
    [SerializeField] private Color _shadow2 = new Color(1f, 138f / 255f, 74f / 255f, 0.38f);
    [SerializeField] private float _shadow2Blur = 11f;
    [SerializeField] private Color _crimson = new Color(1f, 61f / 255f, 82f / 255f);

    [Header("움직임 (웹 keyframes)")]
    [SerializeField] private float _auroraTime = 12f;
    [SerializeField] private float _sheenTime = 7f;
    [SerializeField] private float _sheenDelay;

    [Header("HP 감소 연출 (d3)")]
    [SerializeField] private bool _hasDamageLag;
    [SerializeField] private Color _lagColor = new Color(233f / 255f, 163f / 255f, 173f / 255f);
    [SerializeField] private float _lagDelay = 0.25f;
    [SerializeField] private float _lagK = 8f;
    [SerializeField] private float _lagMinSpeed = 0.009f;
    [SerializeField] private float _lagMinGap = 0.0002f;
    [SerializeField] private float _hitShakeTime = 0.28f;
    [SerializeField] private float _hitFlashTime = 0.25f;

    private Material _runtime;
    private float _ratio = -1f;
    private float _lag = -1f;
    private float _lagWait;
    private float _hitAge = -1f;
    private float _crimsonWeight;
    private Vector2 _basePos;
    private bool _hasBase;

    /// <summary>궁 상태 채움 filter(saturate · brightness · 진홍 빛) 가중치 0~1.</summary>
    public float CrimsonWeight
    {
        get
        {
            return _crimsonWeight;
        }
        set
        {
            _crimsonWeight = Mathf.Clamp01(value);
        }
    }

    public override Material materialForRendering
    {
        get
        {
            if (_runtime == null && material != null && material.shader != null
                && material.shader.name == "Espoir/UI/HudBar")
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
        float pad = _shadow2Blur * 1.5f + 2f;
        Vector2 min = r.min - new Vector2(pad, pad);
        Vector2 max = r.max + new Vector2(pad, pad);
        Color32 c = color;
        vh.AddVert(new Vector3(min.x, min.y), c, new Vector2(-pad, -pad));
        vh.AddVert(new Vector3(min.x, max.y), c, new Vector2(-pad, r.height + pad));
        vh.AddVert(new Vector3(max.x, max.y), c, new Vector2(r.width + pad, r.height + pad));
        vh.AddVert(new Vector3(max.x, min.y), c, new Vector2(r.width + pad, -pad));
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

    private void LateUpdate()
    {
        if (materialForRendering == null || _runtime == null) return;

        Material m = _runtime;
        float dt = Time.unscaledDeltaTime;
        float ratio = _source != null ? Mathf.Clamp01(_source.fillAmount) : 1f;
        UpdateLag(ratio, dt);

        RectTransform rt = rectTransform;
        if (!_hasBase)
        {
            _basePos = rt.anchoredPosition;
            _hasBase = true;
        }

        rt.anchoredPosition = _basePos + HitShake();

        float t = Time.unscaledTime;
        Rect r = rt.rect;
        m.SetVector(_BAR_SIZE, new Vector4(r.width, r.height, _slant, 0f));
        m.SetVector(_RATIO, new Vector4(_ratio, Mathf.Max(_lag, _ratio), 0f, 0f));
        m.SetVector(_TRACK, ToVector(_track));
        for (int i = 0; i < _STOPS.Length && i < _stops.Length; i++)
        {
            m.SetVector(_STOPS[i], ToVector(_stops[i]));
        }

        m.SetVector(_ANIM, new Vector4(Aurora(t), Sheen(t), _glossAlpha, _sheenAlpha));
        float flash = _hitAge >= 0f && _hitAge < _hitFlashTime ? Mathf.Lerp(2.6f, 1f, _hitAge / _hitFlashTime) : 1f;
        bool isLagShown = _hasDamageLag && _lag - _ratio >= _lagMinGap;
        m.SetVector(_LAG, ToVector(_lagColor));
        m.SetVector(_LAG_FX, new Vector4(isLagShown ? 1f : 0f, flash, 0f, 0f));
        m.SetVector(_SHADOW1, ToVector(_shadow1));
        m.SetVector(_SHADOW2, ToVector(_shadow2));
        m.SetVector(_SHADOW_BLUR, new Vector4(_shadow1Blur, _shadow2Blur, 0f, 0f));
        m.SetVector(_CRIMSON, ToVector(_crimson));
        m.SetFloat(_CRIMSON_W, _crimsonWeight);
    }

    private void UpdateLag(float ratio, float dt)
    {
        if (_ratio < 0f)
        {
            _ratio = ratio;
            _lag = ratio;
            return;
        }

        // vmain-hp.js: 맞으면(값이 줄면) 잔상은 그대로 두고 머무는 시간을 다시 잼, 그 뒤 lag -= max(최소 속도, gap·k)·dt
        if (_hasDamageLag && ratio < _ratio - 0.0001f)
        {
            _lagWait = _lagDelay;
            _hitAge = 0f;
        }

        _ratio = ratio;
        if (_lag < ratio || !_hasDamageLag)
        {
            _lag = ratio;
        }

        if (_lagWait > 0f)
        {
            _lagWait -= dt;
        }

        else
        {
            _lag = Mathf.Max(ratio, _lag - Mathf.Max(_lagMinSpeed, (_lag - ratio) * _lagK) * dt);
        }

        if (_hitAge >= 0f)
        {
            _hitAge += dt;
            if (_hitAge > Mathf.Max(_hitShakeTime, _hitFlashTime)) _hitAge = -1f;
        }
    }

    private Vector2 HitShake()
    {
        if (_hitAge < 0f || _hitAge >= _hitShakeTime) return Vector2.zero;

        // 피격 흔들림의 y 방향을 Unity 캔버스 좌표로 반전합니다.
        float k = _hitAge / _hitShakeTime;
        Vector2 a;
        Vector2 b;
        float u;
        if (k < 0.2f)
        {
            a = Vector2.zero;
            b = new Vector2(-4f, 1f);
            u = k / 0.2f;
        }

        else if (k < 0.5f)
        {
            a = new Vector2(-4f, 1f);
            b = new Vector2(3f, -1f);
            u = (k - 0.2f) / 0.3f;
        }

        else if (k < 0.8f)
        {
            a = new Vector2(3f, -1f);
            b = new Vector2(-2f, 0f);
            u = (k - 0.5f) / 0.3f;
        }

        else
        {
            a = new Vector2(-2f, 0f);
            b = Vector2.zero;
            u = (k - 0.8f) / 0.2f;
        }

        Vector2 v = Vector2.Lerp(a, b, CHudEase.Ease(u));
        return new Vector2(v.x, -v.y);
    }

    private float Aurora(float t)
    {
        // 배경 그라데이션을 왕복 이동시킵니다.
        float cycle = Mathf.Repeat(t / _auroraTime, 2f);
        float k = cycle <= 1f ? cycle : 2f - cycle;
        return CHudEase.EaseInOut(k);
    }

    private float Sheen(float t)
    {
        // kh-sweep 7s: 0~55% 130% → 92~100% -30% (구간 ease-in-out)
        if (t < _sheenDelay) return 0f;

        float k = Mathf.Repeat((t - _sheenDelay) / _sheenTime, 1f);
        if (k < 0.55f) return 1.3f;
        if (k > 0.92f) return -0.3f;
        return Mathf.Lerp(1.3f, -0.3f, CHudEase.EaseInOut((k - 0.55f) / 0.37f));
    }

    private static Vector4 ToVector(Color c)
    {
        return new Vector4(c.r, c.g, c.b, c.a);
    }
}
