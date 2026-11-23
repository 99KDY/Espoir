using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

public partial class CCharacterInfoStage
{
    private enum EQueued
    {
        Sweep,
        Shake,
        Moment,
    }

    private struct SQueued
    {
        public float time;
        public EQueued kind;
        public float value;
    }

    private const float _MAX_ZOOM = 0.3f;
    private const float _ZOOM_SCALE = 0.16f;
    private const float _INTRO_ZOOM = 0.22f;
    private const float _IMPACT_HOLD = 0.13f;
    private const float _IMPACT_FADE = 0.07f;
    private const float _IMPACT_INVERT = 0.06f;
    private const float _VISIBLE_MIN = 0.01f;
    private const float _WORD_ASPECT = 4f;
    private const int _GLINT_DOTS = 8;
    private const float _GLINT_SWORD_LENGTH = 1.54f;
    private const float _GLINT_HEAD_FROM = 0.5f;
    private const float _GLINT_HEAD_TRAVEL = 1.4f;
    private const float _GLINT_BRIGHTNESS = 4.2f;

    private static readonly int _MAIN_TEX = Shader.PropertyToID("_MainTex");
    private static readonly int _COLOR = Shader.PropertyToID("_Color");
    private static readonly int _OPACITY = Shader.PropertyToID("_Opacity");
    private static readonly int _WIPE = Shader.PropertyToID("_Wipe");
    private static readonly int _SWEEP = Shader.PropertyToID("_Sweep");
    private static readonly int _COLOR_TOP = Shader.PropertyToID("_ColorTop");
    private static readonly int _COLOR_BOTTOM = Shader.PropertyToID("_ColorBottom");
    private static readonly int _AMOUNT = Shader.PropertyToID("_Amount");
    private static readonly int _PHASE = Shader.PropertyToID("_Phase");
    private static readonly int _GLINT_START = Shader.PropertyToID("_GlintStart");
    private static readonly int _GLINT_AXIS = Shader.PropertyToID("_GlintAxis");
    private static readonly int _GLINT_VALUES = Shader.PropertyToID("_GlintValues");
    private static readonly int _GLINT_COLOR_A = Shader.PropertyToID("_GlintColorA");
    private static readonly int _GLINT_COLOR_B = Shader.PropertyToID("_GlintColorB");

    private readonly List<SQueued> _QUEUE = new List<SQueued>();
    private CCharacterInfoFocus _focus;
    private PostProcessVolume _volume;
    private MeshRenderer _wordRenderer;
    private MeshRenderer _haloRenderer;
    private Material _wordMaterial;
    private Material _haloMaterial;
    private Mesh _quadMesh;
    private MeshRenderer _glintRenderer;
    private Material _glintMaterial;
    private Mesh _glintMesh;
    private Vector4 _glintStart;
    private Vector3 _glintAxis;
    private float _glintLength;
    private MaterialPropertyBlock _block;

    private float _blur;
    private float _dim;
    private float _saturation = 1f;
    private float _vignette;
    private float _side;
    private float _wordAmount;
    private float _haloAmount;
    private Vector3 _tint = Vector3.one;
    private float _themeTint;
    private float _themeVignette;
    private float _themeMatch;
    private float _themeHelper;

    private float _flash;
    private float _zoom;
    private float _impactTime = -1f;
    private float _haloPulse;
    private float _sweep = -1f;
    private float _glintAt = -1f;
    private float _glintTime = -1f;
    private float _wipe = 1f;
    private int _word;
    private int _nextWord = -1;

    private bool PrepareEffects()
    {
        if (_block == null) _block = new MaterialPropertyBlock();
        CCharacterInfoStageSettingsSO.SAssets assets = _settings.Assets;

        if (_quadMesh == null)
        {
            _quadMesh = new Mesh { name = "CharacterInfoStageQuad" };
            _quadMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
            };
            _quadMesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            _quadMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            // 카메라 근처에서 잘리지 않도록 컬링 범위를 확장한다.
            _quadMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 4f);
        }

        if (_wordRenderer == null && assets.wordShader != null)
        {
            _wordMaterial = new Material(assets.wordShader);
            _wordRenderer = CreateQuad("CharacterInfoStageWord", _wordMaterial);
        }

        if (_haloRenderer == null && assets.haloShader != null)
        {
            _haloMaterial = new Material(assets.haloShader);
            _haloRenderer = CreateQuad("CharacterInfoStageHalo", _haloMaterial);
        }

        if (_glintRenderer == null && assets.glintShader != null && _swordCopy != null) CreateGlint(assets);

        if (_volume == null && _postLayer != null)
        {
            _focus = ScriptableObject.CreateInstance<CCharacterInfoFocus>();
            _focus.enabled.Override(true);
            _volume = PostProcessManager.instance.QuickVolume(_settings.Camera.volumeLayer, 100f, _focus);
            _volume.gameObject.name = "CharacterInfoStageVolume";
        }

        return true;
    }

    private void CreateGlint(CCharacterInfoStageSettingsSO.SAssets assets)
    {
        Transform hand = _swordCopy.parent;
        Vector3 tip = assets.swordEndA;
        Vector3 pommel = assets.swordEndB;
        if (hand.InverseTransformPoint(_swordCopy.TransformPoint(tip)).sqrMagnitude
            < hand.InverseTransformPoint(_swordCopy.TransformPoint(pommel)).sqrMagnitude)
        {
            (tip, pommel) = (pommel, tip);
        }

        float length = Vector3.Distance(tip, pommel);
        if (length < 1e-5f)
        {
            CLogger.Warn($"{name}: 검광의 시작점과 끝점이 너무 가까워 검광을 표시하지 않습니다.");
            return;
        }

        _glintLength = length;
        _glintStart = new Vector4(pommel.x, pommel.y, pommel.z, length / _GLINT_SWORD_LENGTH);
        _glintAxis = (tip - pommel) / length;

        Vector3[] vertices = new Vector3[_GLINT_DOTS * 4];
        Vector2[] corners = new Vector2[_GLINT_DOTS * 4];
        Vector2[] orders = new Vector2[_GLINT_DOTS * 4];
        int[] triangles = new int[_GLINT_DOTS * 6];

        for (int i = 0; i < _GLINT_DOTS; i++)
        {
            int v = i * 4;
            corners[v] = new Vector2(-1f, -1f);
            corners[v + 1] = new Vector2(1f, -1f);
            corners[v + 2] = new Vector2(1f, 1f);
            corners[v + 3] = new Vector2(-1f, 1f);
            Vector2 order = new Vector2(i / (float)(_GLINT_DOTS - 1), 0f);
            for (int k = 0; k < 4; k++) orders[v + k] = order;

            int t = i * 6;
            triangles[t] = v;
            triangles[t + 1] = v + 2;
            triangles[t + 2] = v + 1;
            triangles[t + 3] = v;
            triangles[t + 4] = v + 3;
            triangles[t + 5] = v + 2;
        }

        _glintMesh = new Mesh { name = "CharacterInfoStageGlint" };
        _glintMesh.vertices = vertices;
        _glintMesh.uv = corners;
        _glintMesh.uv2 = orders;
        _glintMesh.triangles = triangles;
        // 정점 위치를 셰이더에서 계산하므로 검 전체를 포함하는 컬링 범위를 지정한다.
        _glintMesh.bounds = new Bounds((tip + pommel) * 0.5f, Vector3.one * (length * 1.5f));

        GameObject glint = new GameObject("CharacterInfoStageGlint");
        glint.layer = _swordCopy.gameObject.layer;
        glint.transform.SetParent(_swordCopy, false);
        glint.AddComponent<MeshFilter>().sharedMesh = _glintMesh;
        _glintMaterial = new Material(assets.glintShader);
        _glintRenderer = glint.AddComponent<MeshRenderer>();
        _glintRenderer.sharedMaterial = _glintMaterial;
        _glintRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _glintRenderer.receiveShadows = false;
        _glintRenderer.lightProbeUsage = LightProbeUsage.Off;
        _glintRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        _glintRenderer.enabled = false;
    }

    private MeshRenderer CreateQuad(string quadName, Material material)
    {
        GameObject quad = new GameObject(quadName);
        quad.transform.SetParent(_rig, false);
        quad.AddComponent<MeshFilter>().sharedMesh = _quadMesh;
        MeshRenderer renderer = quad.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.enabled = false;
        return renderer;
    }

    private void ReleaseEffects()
    {
        CCharacterInfoFocus.TargetCamera = null;
        if (_volume != null) RuntimeUtilities.DestroyVolume(_volume, true, true);
        if (_wordMaterial != null) Destroy(_wordMaterial);
        if (_haloMaterial != null) Destroy(_haloMaterial);
        if (_quadMesh != null) Destroy(_quadMesh);
        if (_glintMaterial != null) Destroy(_glintMaterial);
        if (_glintMesh != null) Destroy(_glintMesh);
    }

    private void BeginEffects(int tab)
    {
        ClearQueue();
        _blur = 0f;
        _dim = 0f;
        _saturation = 1f;
        _vignette = 0f;
        _side = 0f;
        _wordAmount = 0f;
        _haloAmount = 0f;
        _tint = Vector3.one;
        _themeTint = 0f;
        _themeVignette = 0f;
        _themeMatch = 0f;
        _themeHelper = 0f;
        _flash = 0f;
        _zoom = 0f;
        _impactTime = -1f;
        _haloPulse = 0f;
        _sweep = -1f;
        _wipe = 0f;
        _word = tab;
        _nextWord = -1;
        _protect = Vector4.zero;
        _glintTime = -1f;
        _glintAt = _time + _settings.Look.glintOpenDelay;
        CCharacterInfoFocus.TargetCamera = _camera;
        if (_volume != null) _volume.enabled = true;
    }

    private void EndEffects()
    {
        ClearQueue();
        CCharacterInfoFocus.TargetCamera = null;
        if (_volume != null)
        {
            _volume.enabled = false;
            _volume.weight = 1f;
        }

        HideBackdrop();
    }

    private void HideBackdrop()
    {
        _glintAt = -1f;
        _glintTime = -1f;
        if (_glintRenderer != null) _glintRenderer.enabled = false;
        if (_wordRenderer != null) _wordRenderer.enabled = false;
        if (_haloRenderer != null) _haloRenderer.enabled = false;
    }

    private void FadeEffects(float amount)
    {
        if (_volume != null) _volume.weight = amount;
    }

    private void ClearQueue()
    {
        _QUEUE.Clear();
    }

    private void After(float delay, EQueued kind, float value)
    {
        _QUEUE.Add(new SQueued { time = _time + delay, kind = kind, value = value });
    }

    private void RunQueue()
    {
        for (int i = _QUEUE.Count - 1; i >= 0; i--)
        {
            SQueued item = _QUEUE[i];
            if (_time < item.time) continue;

            _QUEUE.RemoveAt(i);
            if (item.kind == EQueued.Sweep) _sweep = 0f;
            else if (item.kind == EQueued.Shake) _shake = item.value;
            else PlayMoment((int)item.value);
        }
    }

    private void BeginCut(int previous, int tab)
    {
        CCharacterInfoStageSettingsSO.SLook look = _settings.Look;
        ClearQueue();
        _flash = look.flash;
        _zoom = look.zoom;
        _nextWord = tab;
        _haloPulse = 1f;
        After(look.sweepDelay, EQueued.Sweep, 0f);
        float arrive = _settings.Motion.arriveTimes[TransitionIndex(previous, tab) - _POSE_COUNT];
        After(arrive, EQueued.Moment, tab);
        _glintAt = _time + arrive;
    }

    private void PlayMoment(int tab)
    {
        float moment = _settings.Look.moment;

        if (tab == 1)
        {
            _flash = Mathf.Max(_flash, 0.16f * moment);
            _haloPulse = 1f;
        }
        else if (tab == 2)
        {
            _shake = Mathf.Max(_shake, 0.7f * moment);
            _flash = Mathf.Max(_flash, 0.1f * moment);
            _haloPulse = 1f;
        }
        else
        {
            _haloPulse = Mathf.Max(_haloPulse, 0.6f);
        }
    }

    private void OnIntroLanded()
    {
        _flash = Mathf.Max(_flash, _settings.Look.landFlash * _settings.Look.flash);
        _haloPulse = 1f;
        _sweep = 0f;
    }

    private static Vector3 Linear(Color color)
    {
        Color linear = color.linear;
        return new Vector3(linear.r, linear.g, linear.b);
    }

    private void StepEffects(float deltaTime, float blend)
    {
        CCharacterInfoStageSettingsSO.SLook look = _settings.Look;
        CCharacterInfoStageSettingsSO.SThemeLook theme = _settings.Theme;
        CCharacterInfoStageSettingsSO.SSwap swap = _settings.Swap;

        float follow = 1f - Mathf.Exp(-deltaTime * look.follow);
        _blur += (look.blur - _blur) * follow;
        _dim += (look.dim - _dim) * follow;
        _saturation += (look.saturation - _saturation) * follow;
        _vignette += (look.vignette - _vignette) * follow;
        _side += (look.side - _side) * follow;
        _wordAmount += (1f - _wordAmount) * follow;
        _haloAmount += (look.halo - _haloAmount) * follow;
        _tint += (look.tint - _tint) * follow;
        _flash *= Mathf.Exp(-deltaTime * look.flashDecay);
        _zoom *= Mathf.Exp(-deltaTime * look.zoomDecay);
        _haloPulse *= Mathf.Exp(-deltaTime * look.haloDecay);
        RunQueue();

        if (_impactTime >= 0f)
        {
            _impactTime += deltaTime;
            if (_impactTime > _IMPACT_HOLD + _IMPACT_FADE) _impactTime = -1f;
        }

        bool isHelperTheme = _tab == _HELPER_TAB && (!_isSwapOn || _swapTime >= swap.hitTime);
        float themeFollow = 1f - Mathf.Exp(-deltaTime * theme.follow);
        _themeHelper += ((isHelperTheme ? 1f : 0f) - _themeHelper) * (1f - Mathf.Exp(-deltaTime * theme.colorFollow));
        _themeTint += (theme.tint - _themeTint) * themeFollow;
        _themeVignette += (theme.vignette - _themeVignette) * themeFollow;
        _themeMatch += (theme.match - _themeMatch) * themeFollow;

        Transform cameraTransform = _camera.transform;
        float tangent = Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad * 0.5f);

        if (_focus != null)
        {
            Vector3 focusPoint = _isSwapOn ? _swapFocus : _feet + Vector3.up * look.focusHeight;
            float focusDistance = Mathf.Max(1f, Vector3.Dot(focusPoint - cameraTransform.position,
                cameraTransform.forward));
            Vector3 centerPoint = _isSwapOn ? _swapFocus : _feet + Vector3.up * look.centerHeight;
            Vector3 center = _camera.WorldToViewportPoint(centerPoint);
            float pixelScale = _camera.pixelHeight / 1080f;
            float impact = _impactTime < 0f ? 0f : _impactTime < _IMPACT_HOLD ? 1f
                : Mathf.Max(0f, 1f - (_impactTime - _IMPACT_HOLD) / _IMPACT_FADE);
            float invert = _impactTime >= 0f && _impactTime < _IMPACT_INVERT ? 1f : 0f;
            float zoom = Mathf.Min(_MAX_ZOOM, _zoom * _ZOOM_SCALE + _introRatio * _INTRO_ZOOM * look.zoom);

            _focus.focus.Override(new Vector2(focusDistance, look.focusRange));
            _focus.blur.Override(_blur * blend * pixelScale * (_isSwapOn ? _swapKey.blur : 1f));
            _focus.dim.Override(_dim * blend);
            _focus.saturation.Override(1f + (_saturation - 1f) * blend);
            _focus.vignette.Override(_vignette * blend);
            _focus.tint.Override(Vector3.Lerp(Vector3.one, _tint, blend));
            _focus.center.Override(new Vector4(center.x, center.y, _side * blend,
                1f / (2f * focusDistance * tangent)));
            _focus.protect.Override(new Vector4(_protect.x, _protect.y, _protect.z, _protect.w * blend));
            _focus.impact.Override(new Vector4(_flash, zoom, impact, invert));
            _focus.themeLight.Override(Color.Lerp(theme.player.light.linear, theme.helper.light.linear, _themeHelper)
                .gamma);
            _focus.themeTint.Override(Vector3.Lerp(theme.player.backgroundTint, theme.helper.backgroundTint,
                _themeHelper));
            _focus.themeAmount.Override(new Vector2(_themeTint, _themeVignette) * blend);
            _focus.panel.Override(new Vector4(look.panelBlur * blend * pixelScale, look.panelEdge, look.panelFeather,
                0f));
        }

        StepWord(deltaTime, blend, tangent * _camera.aspect);
        StepHalo(blend);
        StepGlint(deltaTime, blend);
    }

    private void StepGlint(float deltaTime, float blend)
    {
        if (_glintRenderer == null) return;

        CCharacterInfoStageSettingsSO.SLook look = _settings.Look;

        if (_glintAt >= 0f && _time >= _glintAt)
        {
            _glintAt = -1f;
            _glintTime = 0f;
        }

        if (_glintTime >= 0f) _glintTime += deltaTime / Mathf.Max(0.01f, look.glintTime);
        if (_glintTime >= 1f) _glintTime = -1f;

        bool isVisible = _glintTime >= 0f && look.glint > 0f;
        _glintRenderer.enabled = isVisible;
        if (!isVisible) return;

        float eased = _glintTime * _glintTime * (3f - 2f * _glintTime);
        float amount = Mathf.Pow(Mathf.Sin(Mathf.PI * _glintTime), 0.6f) * blend;
        float worldUnit = _glintLength * _swordCopy.lossyScale.x / _GLINT_SWORD_LENGTH;
        _glintRenderer.GetPropertyBlock(_block);
        _block.SetVector(_GLINT_START, _glintStart);
        _block.SetVector(_GLINT_AXIS, new Vector4(_glintAxis.x, _glintAxis.y, _glintAxis.z, worldUnit));
        _block.SetVector(_GLINT_VALUES, new Vector4(_GLINT_HEAD_FROM + eased * _GLINT_HEAD_TRAVEL, amount,
            _GLINT_BRIGHTNESS * look.glint, 0f));
        _block.SetVector(_GLINT_COLOR_A, Linear(look.glintColorA));
        _block.SetVector(_GLINT_COLOR_B, Linear(look.glintColorB));
        _glintRenderer.SetPropertyBlock(_block);
    }

    private void StepWord(float deltaTime, float blend, float halfWidth)
    {
        if (_wordRenderer == null) return;

        CCharacterInfoStageSettingsSO.SLook look = _settings.Look;
        CCharacterInfoStageSettingsSO.SThemeLook theme = _settings.Theme;
        CCharacterInfoStageSettingsSO.SSwap swap = _settings.Swap;
        bool isVisible = _wordAmount * blend > _VISIBLE_MIN;
        _wordRenderer.enabled = isVisible;
        if (!isVisible) return;

        if (_nextWord >= 0 && _nextWord != _word)
        {
            _wipe -= deltaTime / look.wipeOutTime;
            if (_wipe <= 0f)
            {
                _wipe = 0f;
                _word = _nextWord;
                _nextWord = -1;
            }
        }
        else
        {
            _nextWord = -1;
            if (_wipe < 1f) _wipe = Mathf.Min(1f, _wipe + deltaTime / look.wipeInTime);
        }

        if (_sweep >= 0f)
        {
            _sweep += deltaTime / look.sweepTime;
            if (_sweep > 1f) _sweep = -1f;
        }

        Transform cameraTransform = _camera.transform;
        Transform word = _wordRenderer.transform;
        float width;
        float appear = 1f;

        if (_isSwapOn)
        {
            Vector3 forward = cameraTransform.forward;
            float depth = Mathf.Max(swap.wordMinDepth,
                Vector3.Dot(_swapFocus - cameraTransform.position, forward) + swap.wordDepth);
            Vector3 viewport = new Vector3((swap.wordPosition.x + 1f) * 0.5f, (swap.wordPosition.y + 1f) * 0.5f, 0f);
            Vector3 ray = _camera.ViewportPointToRay(viewport).direction;
            word.position = cameraTransform.position + ray * (depth / Mathf.Max(0.2f, Vector3.Dot(ray, forward)));
            width = 2f * depth * halfWidth * swap.wordWidth;
            Vector3 flat = new Vector3(forward.x, 0f, forward.z);
            if (flat.sqrMagnitude > 1e-6f) word.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
            appear = Mathf.Clamp01((_swapTime - swap.wordAppearTime) / swap.wordAppearLength);
            appear = appear * appear * (3f - 2f * appear);
        }
        else
        {
            Vector3 toCamera = cameraTransform.position - _feet;
            toCamera.y = 0f;
            float distance = toCamera.magnitude;
            if (distance < 1e-4f) return;

            toCamera /= distance;
            float reach = distance + look.wordDistance;
            width = 2f * reach * halfWidth * look.wordWidth;
            Vector3 position = _feet - toCamera * look.wordDistance;
            position.y = _feet.y + _now.lookHeight + 0.52f * (width / 4.2f);
            Vector3 right = cameraTransform.right;
            right.y = 0f;
            position += right.normalized * ((look.wordX - _now.screenX) * reach * halfWidth);
            word.SetPositionAndRotation(position, Quaternion.LookRotation(-toCamera, Vector3.up));
        }

        word.localScale = new Vector3(width, width / _WORD_ASPECT, 1f);

        Texture2D[] textures = _settings.Assets.wordTextures;
        Texture2D texture = textures != null && _word < textures.Length ? textures[_word] : null;
        Vector3 themeWord = Vector3.Lerp(Linear(theme.player.word), Linear(theme.helper.word), _themeHelper);
        Vector3 color = Vector3.Lerp(Linear(look.wordColor), themeWord, Mathf.Max(_themeMatch, _themeHelper));
        _wordRenderer.GetPropertyBlock(_block);
        if (texture != null) _block.SetTexture(_MAIN_TEX, texture);
        _block.SetVector(_COLOR, color);
        _block.SetFloat(_OPACITY, look.wordOpacity * _wordAmount * blend * appear);
        _block.SetFloat(_WIPE, _wipe);
        _block.SetFloat(_SWEEP, _sweep >= 0f ? _sweep : -1f);
        _wordRenderer.SetPropertyBlock(_block);
    }

    private void StepHalo(float blend)
    {
        if (_haloRenderer == null) return;

        CCharacterInfoStageSettingsSO.SLook look = _settings.Look;
        CCharacterInfoStageSettingsSO.SThemeLook theme = _settings.Theme;
        bool isVisible = _haloAmount * blend > _VISIBLE_MIN;
        _haloRenderer.enabled = isVisible;
        if (!isVisible) return;

        Transform cameraTransform = _camera.transform;
        Vector3 toCamera = cameraTransform.position - _feet;
        toCamera.y = 0f;
        Vector3 position = _feet - toCamera.normalized * look.haloDistance;
        position.y = _feet.y + look.haloHeight;
        Transform halo = _haloRenderer.transform;
        halo.SetPositionAndRotation(position, cameraTransform.rotation);
        halo.localScale = new Vector3(look.haloSize.x, look.haloSize.y, 1f);

        Vector3 top = Vector3.Lerp(Linear(look.haloTop),
            Vector3.Lerp(Linear(theme.player.haloTop), Linear(theme.helper.haloTop), _themeHelper), _themeMatch);
        Vector3 bottom = Vector3.Lerp(Linear(look.haloBottom),
            Vector3.Lerp(Linear(theme.player.haloBottom), Linear(theme.helper.haloBottom), _themeHelper), _themeMatch);
        _haloRenderer.GetPropertyBlock(_block);
        _block.SetVector(_COLOR_TOP, top);
        _block.SetVector(_COLOR_BOTTOM, bottom);
        _block.SetFloat(_AMOUNT, _haloAmount * blend * (0.6f + 0.6f * _haloPulse + 0.05f * Mathf.Sin(_time * 0.8f)));
        _block.SetFloat(_PHASE, _time * 0.45f);
        _haloRenderer.SetPropertyBlock(_block);
    }
}
