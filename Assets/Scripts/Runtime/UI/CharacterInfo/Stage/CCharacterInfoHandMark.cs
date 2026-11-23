using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 프라도나의 손등 문양과 발광 효과를 재생합니다.
/// </summary>
public class CCharacterInfoHandMark : MonoBehaviour
{
    private const float _MAX_STEP = 0.1f;
    private const float _VISIBLE_POWER = 0.003f;
    private const float _OFF_GLOW = 0.02f;
    private const float _TWO_PI = Mathf.PI * 2f;

    private static readonly int _MARK_TEX = Shader.PropertyToID("_MarkTex");
    private static readonly int _MARK_GLOW = Shader.PropertyToID("_MarkGlow");
    private static readonly int _MARK_REVEAL = Shader.PropertyToID("_MarkReveal");
    private static readonly int _MARK_TIME = Shader.PropertyToID("_MarkTime");
    private static readonly int _MARK_INK = Shader.PropertyToID("_MarkInk");
    private static readonly int _MARK_INK_AMOUNT = Shader.PropertyToID("_MarkInkAmount");
    private static readonly int _MARK_GLOW_COLOR = Shader.PropertyToID("_MarkGlowColor");
    private static readonly int _MARK_GLOW_TINT = Shader.PropertyToID("_MarkGlowTint");
    private static readonly int _MARK_LINE = Shader.PropertyToID("_MarkLine");
    private static readonly int _MARK_SPREAD = Shader.PropertyToID("_MarkSpread");
    private static readonly int _MARK_KEEP = Shader.PropertyToID("_MarkKeep");
    private static readonly int _RIM_COLOR_A = Shader.PropertyToID("_RimColorA");
    private static readonly int _RIM_COLOR_B = Shader.PropertyToID("_RimColorB");
    private static readonly int _RIM_POWER = Shader.PropertyToID("_RimPower");
    private static readonly int _RING_COLOR_A = Shader.PropertyToID("_RingColorA");
    private static readonly int _RING_COLOR_B = Shader.PropertyToID("_RingColorB");
    private static readonly int _RING_VALUES = Shader.PropertyToID("_RingValues");

    private class CRing
    {
        public CCharacterInfoHandMarkSO.SRing settings;
        public MeshRenderer renderer;
        public Mesh mesh;
        public Texture texture;
        public float angle;
    }

    private CCharacterInfoHandMarkSO _data;
    private Transform _hand;
    private Transform _middle;
    private SkinnedMeshRenderer _markRenderer;
    private SkinnedMeshRenderer _rimRenderer;
    private CRing[] _rings = new CRing[0];
    private Material _inkMaterial;
    private Material _glowMaterial;
    private Material _rimMaterial;
    private Material _ringMaterial;
    private MaterialPropertyBlock _block;
    private bool _isReady;
    private bool _isGlowOn;
    private bool _wasGlowOn;
    private float _current;
    private float _reveal = 1f;
    private float _time;
    private float _turnOnTime = -1000f;
    private float _glow;

    /// <summary>
    /// 맥동을 포함한 현재 발광 세기입니다.
    /// </summary>
    public float Glow => _glow;

    /// <summary>
    /// 배경 흐림에서 제외할 손 이펙트의 월드 위치입니다.
    /// </summary>
    public Vector3 ProtectPoint
    {
        get
        {
            if (!_isReady) return transform.position;

            return Vector3.Lerp(_hand.position, _middle.position, _data.ProtectAlong);
        }
    }

    private void OnDestroy()
    {
        DestroyOwned(_inkMaterial);
        DestroyOwned(_glowMaterial);
        DestroyOwned(_rimMaterial);
        DestroyOwned(_ringMaterial);

        for (int i = 0; i < _rings.Length; i++)
        {
            DestroyOwned(_rings[i].mesh);
        }
    }

    /// <summary>
    /// 모델의 손 뼈에 문양 효과를 구성하고 성공 여부를 반환합니다.
    /// </summary>
    public bool Setup(CCharacterInfoHandMarkSO data)
    {
        if (_isReady) return true;

        if (data == null || data.MarkMesh == null || data.CrestTexture == null || data.InkShader == null
            || data.GlowShader == null)
        {
            CLogger.Error($"{name}: 손등 문양 메시, 텍스처 또는 셰이더가 지정되지 않았습니다.");
            return false;
        }

        SkinnedMeshRenderer body = FindBody(data.SourceRendererName);
        if (body == null)
        {
            CLogger.Error($"{name}: 손등 문양 대상 렌더러 '{data.SourceRendererName}'를 찾을 수 없습니다.");
            return false;
        }

        Transform[] bones = body.bones;
        Transform hand = FindBone(bones, data.HandBoneName);
        Transform middle = FindBone(bones, data.MiddleBoneName);
        if (hand == null || middle == null)
        {
            CLogger.Error($"{name}: 손등 문양에 필요한 뼈를 찾을 수 없습니다: '{data.HandBoneName}', '{data.MiddleBoneName}'.");
            return false;
        }

        if (data.MarkMesh.bindposes.Length != bones.Length)
        {
            CLogger.Error($"{name}: 손등 문양 메시의 뼈 수({data.MarkMesh.bindposes.Length})가 대상 렌더러"
                + $"({bones.Length})와 다릅니다. 모델과 문양 메시의 본 구성을 확인하세요.");
            return false;
        }

        _data = data;
        _hand = hand;
        _middle = middle;
        _block = new MaterialPropertyBlock();

        _inkMaterial = CreateMaterial(data.InkShader);
        _glowMaterial = CreateMaterial(data.GlowShader);
        _markRenderer = CreateSkinned("HandMark", body, data.MarkMesh, new[] { _inkMaterial, _glowMaterial });

        if (data.RimMesh != null && data.RimShader != null && data.RimMesh.bindposes.Length == bones.Length)
        {
            _rimMaterial = CreateMaterial(data.RimShader);
            _rimRenderer = CreateSkinned("HandMarkRim", body, data.RimMesh, new[] { _rimMaterial });
        }
        else
        {
            CLogger.Warn($"{name}: 손 테두리 발광 설정이 올바르지 않아 해당 효과를 생략합니다. 메시, 셰이더와 본 구성을 확인하세요.");
        }

        BuildRings(body.gameObject.layer);

        _isReady = true;
        Clear();
        return true;
    }

    /// <summary>
    /// 문양 발광의 활성 상태를 설정합니다.
    /// </summary>
    public void SetGlow(bool isOn)
    {
        _isGlowOn = isOn;
    }

    /// <summary>
    /// 모델 자세 갱신 후 지정한 시간만큼 문양 효과를 진행합니다.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (!_isReady) return;

        float dt = Mathf.Clamp(deltaTime, 0f, _MAX_STEP);
        _time += dt;

        if (_isGlowOn && !_wasGlowOn)
        {
            _reveal = 0f;
            _turnOnTime = _time;
        }

        _wasGlowOn = _isGlowOn;

        if (_isGlowOn)
        {
            _reveal = Mathf.Min(1f, _reveal + dt / _data.RevealTime);
        }
        else if (_current < _OFF_GLOW)
        {
            _reveal = 1f;
        }

        float target = _isGlowOn ? 1f : 0f;
        float follow = target > _current ? _data.GlowRise : _data.GlowFall;
        _current += (target - _current) * Mathf.Min(1f, dt * follow);

        float beat = 1f + _data.Pulse * Mathf.Sin(_time * _data.PulseSpeed);
        _glow = _current * beat * _data.GlowScale;
        float appear = _reveal * _reveal * (3f - 2f * _reveal);

        ApplyMark();
        ApplyRim(appear);

        for (int i = 0; i < _rings.Length; i++)
        {
            ApplyRing(_rings[i], dt, appear, beat);
        }
    }

    /// <summary>
    /// 발광과 고리 효과를 초기화합니다.
    /// </summary>
    public void Clear()
    {
        _isGlowOn = false;
        _wasGlowOn = false;
        _current = 0f;
        _reveal = 1f;
        _glow = 0f;
        if (!_isReady) return;

        ApplyMark();
        if (_rimRenderer != null) _rimRenderer.enabled = false;

        for (int i = 0; i < _rings.Length; i++)
        {
            _rings[i].renderer.enabled = false;
        }
    }

    private void ApplyMark()
    {
        _block.Clear();
        _block.SetTexture(_MARK_TEX, _data.CrestTexture);
        _block.SetFloat(_MARK_GLOW, _glow);
        _block.SetFloat(_MARK_REVEAL, _reveal);
        _block.SetFloat(_MARK_TIME, _time);
        _block.SetVector(_MARK_INK, _data.InkLinear);
        _block.SetFloat(_MARK_INK_AMOUNT, _data.InkAmount);
        _block.SetVector(_MARK_GLOW_COLOR, _data.GlowLinear);
        _block.SetVector(_MARK_GLOW_TINT, _data.GlowTintLinear);
        _block.SetVector(_MARK_LINE, new Vector4(_data.Core, _data.CoreWhite, _data.Sheen, _data.Aura));
        _block.SetVector(_MARK_SPREAD, new Vector4(_data.Halo, _data.Wide, _data.Flow, _data.Idle));
        _block.SetVector(_MARK_KEEP, new Vector4(_data.KeepHue, _data.SkinPeak, 0f, 0f));
        _markRenderer.SetPropertyBlock(_block);
    }

    private void ApplyRim(float appear)
    {
        if (_rimRenderer == null) return;

        float power = _data.RimPower * _data.RimScale;
        bool isVisible = power > 0f && _current > _VISIBLE_POWER;
        _rimRenderer.enabled = isVisible;
        if (!isVisible) return;

        _block.Clear();
        _block.SetVector(_RIM_COLOR_A, _data.RimLinearA);
        _block.SetVector(_RIM_COLOR_B, _data.RimLinearB);
        _block.SetFloat(_RIM_POWER, power * _glow * appear);
        _rimRenderer.SetPropertyBlock(_block);
    }

    private void ApplyRing(CRing ring, float dt, float appear, float beat)
    {
        CCharacterInfoHandMarkSO.SRing settings = ring.settings;
        ring.angle = Mathf.Repeat(ring.angle + dt * settings.spin * _TWO_PI, _TWO_PI);

        float scale;
        float power;

        if (settings.isBurst)
        {
            float progress = (_time - _turnOnTime) / Mathf.Max(0.01f, settings.burstDuration);
            float clamped = Mathf.Min(progress, 1f);
            float ease = 1f - Mathf.Pow(1f - clamped, 3f);
            scale = settings.burstFrom + (1f - settings.burstFrom) * ease;
            power = progress < 1f ? settings.power * _current * (1f - progress) * (1f - progress) : 0f;
        }
        else
        {
            scale = 0.5f + 0.5f * appear;
            power = settings.power * _current * appear * beat;
        }

        power *= _data.RingScale;
        bool isVisible = power > _VISIBLE_POWER;
        ring.renderer.enabled = isVisible;
        if (!isVisible) return;

        _block.Clear();
        _block.SetTexture(_MARK_TEX, ring.texture);
        _block.SetVector(_RING_COLOR_A, _data.RingLinearA);
        _block.SetVector(_RING_COLOR_B, _data.RingLinearB);
        _block.SetVector(_RING_VALUES, new Vector4(ring.angle, scale, power, settings.hot));
        ring.renderer.SetPropertyBlock(_block);
    }

    private SkinnedMeshRenderer FindBody(string rendererName)
    {
        foreach (SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.name == rendererName) return renderer;
        }

        return null;
    }

    private static Transform FindBone(Transform[] bones, string boneName)
    {
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] != null && bones[i].name == boneName) return bones[i];
        }

        return null;
    }

    private static Material CreateMaterial(Shader shader)
    {
        return new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
    }

    private static SkinnedMeshRenderer CreateSkinned(string objectName, SkinnedMeshRenderer body, Mesh mesh,
        Material[] materials)
    {
        GameObject child = new GameObject(objectName);
        child.layer = body.gameObject.layer;
        child.transform.SetParent(body.transform.parent, false);
        child.transform.localPosition = body.transform.localPosition;
        child.transform.localRotation = body.transform.localRotation;
        child.transform.localScale = body.transform.localScale;

        SkinnedMeshRenderer renderer = child.AddComponent<SkinnedMeshRenderer>();
        renderer.sharedMesh = mesh;
        renderer.bones = body.bones;
        renderer.rootBone = body.rootBone;
        renderer.localBounds = body.localBounds;
        renderer.updateWhenOffscreen = false;
        renderer.quality = body.quality;
        renderer.sharedMaterials = materials;
        SetPlain(renderer);
        return renderer;
    }

    private void BuildRings(int layer)
    {
        CCharacterInfoHandMarkSO.SRing[] list = _data.Rings;
        bool canDraw = list != null && _data.RingShader != null;
        _rings = new CRing[canDraw ? list.Length : 0];
        if (!canDraw) return;

        _ringMaterial = CreateMaterial(_data.RingShader);

        for (int i = 0; i < list.Length; i++)
        {
            CCharacterInfoHandMarkSO.SRing settings = list[i];
            Mesh mesh = BuildRingMesh(settings);

            GameObject child = new GameObject("HandMarkRing");
            child.layer = layer;
            child.transform.SetParent(_hand, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;

            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _ringMaterial;
            SetPlain(renderer);
            renderer.enabled = false;

            Texture texture = settings.usesCrescent ? _data.CrescentTexture : _data.CircleTexture;
            _rings[i] = new CRing
            {
                settings = settings,
                renderer = renderer,
                mesh = mesh,
                texture = texture != null ? texture : Texture2D.blackTexture,
                angle = settings.startAngle,
            };
        }
    }

    private Mesh BuildRingMesh(CCharacterInfoHandMarkSO.SRing settings)
    {
        float length = _data.HandLength;
        float tilt = settings.tilt * Mathf.Deg2Rad;
        Vector3 right = _data.FrameRight;
        Vector3 up = _data.FrameUp;
        Vector3 normal = _data.FrameNormal;

        Vector3 center = _data.FrameCenter + up * ((settings.along - 0.5f) * _data.MarkSize)
            + normal * (settings.lift * length);
        Vector3 axisA = right * (settings.radius * length);
        Vector3 axisB = settings.isBracelet
            ? normal * Mathf.Cos(tilt) + up * Mathf.Sin(tilt)
            : up * Mathf.Cos(tilt) + normal * Mathf.Sin(tilt);
        axisB *= settings.radius * length;

        Mesh mesh = new Mesh { name = "HandMarkRing", hideFlags = HideFlags.HideAndDontSave };
        mesh.vertices = new[]
        {
            center - axisA - axisB, center + axisA - axisB, center + axisA + axisB, center - axisA + axisB,
        };
        mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void SetPlain(Renderer renderer)
    {
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
    }

    private static void DestroyOwned(Object target)
    {
        if (target != null) Destroy(target);
    }
}
