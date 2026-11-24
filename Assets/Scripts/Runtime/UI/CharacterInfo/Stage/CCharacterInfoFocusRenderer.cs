using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public sealed class CCharacterInfoFocusRenderer : PostProcessEffectRenderer<CCharacterInfoFocus>
{
    private static readonly int _FOCUS = Shader.PropertyToID("_Focus");
    private static readonly int _GRADE = Shader.PropertyToID("_Grade");
    private static readonly int _TINT = Shader.PropertyToID("_Tint");
    private static readonly int _CENTER = Shader.PropertyToID("_Center");
    private static readonly int _PROTECT = Shader.PropertyToID("_Protect");
    private static readonly int _IMPACT = Shader.PropertyToID("_Impact");
    private static readonly int _THEME_LIGHT = Shader.PropertyToID("_ThemeLight");
    private static readonly int _THEME_TINT = Shader.PropertyToID("_ThemeTint");
    private static readonly int _THEME_AMOUNT = Shader.PropertyToID("_ThemeAmount");
    private static readonly int _PANEL = Shader.PropertyToID("_Panel");

    private static readonly int _CHARACTER_DEPTH = Shader.PropertyToID("_CharacterInfoDepth");
    private const float _FAR_DEPTH = 100000f;

    private readonly List<Material> _MATERIALS = new List<Material>();
    private Shader _shader;
    private Material _depthMaterial;

    /// <summary>
    /// 화면 효과에 사용할 셰이더를 저장합니다.
    /// </summary>
    public override void Init()
    {
        _shader = Shader.Find("Hidden/Espoir/CharacterInfoFocus");
        _depthMaterial = new Material(Shader.Find("Hidden/Espoir/CharacterInfoDepth"))
        {
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    /// <summary>
    /// 캐릭터 깊이 렌더링에 사용한 재질을 해제합니다.
    /// </summary>
    public override void Release()
    {
        RuntimeUtilities.Destroy(_depthMaterial);
    }

    /// <summary>
    /// 배경을 가려내는 데 쓸 깊이 텍스처를 요청합니다.
    /// </summary>
    public override DepthTextureMode GetCameraFlags()
    {
        return DepthTextureMode.Depth;
    }

    /// <summary>
    /// 색 보정이 끝난 화면에 정보창 효과를 얹습니다.
    /// </summary>
    public override void Render(PostProcessRenderContext context)
    {
        PropertySheet sheet = context.propertySheets.Get(_shader);
        Vector2 focus = settings.focus.value;
        sheet.properties.SetVector(_FOCUS, new Vector4(focus.x, focus.y, settings.blur, context.camera.aspect));
        sheet.properties.SetVector(_GRADE, new Vector4(settings.dim, settings.saturation, settings.vignette, 0f));
        sheet.properties.SetVector(_TINT, settings.tint.value);
        sheet.properties.SetVector(_CENTER, settings.center.value);
        sheet.properties.SetVector(_PROTECT, settings.protect.value);
        sheet.properties.SetVector(_IMPACT, settings.impact.value);
        sheet.properties.SetColor(_THEME_LIGHT, settings.themeLight.value.linear);
        sheet.properties.SetVector(_THEME_TINT, settings.themeTint.value);
        sheet.properties.SetVector(_THEME_AMOUNT, settings.themeAmount.value);
        sheet.properties.SetVector(_PANEL, settings.panel.value);

        // 양면 재질도 배경으로 분류되지 않도록 캐릭터 깊이를 별도로 렌더링한다.
        context.GetScreenSpaceTemporaryRT(context.command, _CHARACTER_DEPTH, 24, RenderTextureFormat.RFloat,
            RenderTextureReadWrite.Linear, FilterMode.Point);
        context.command.SetRenderTarget(_CHARACTER_DEPTH);
        context.command.ClearRenderTarget(true, true, new Color(_FAR_DEPTH, 0f, 0f, 0f));
        List<Renderer> characters = CCharacterInfoFocus.Characters;

        for (int i = 0; i < characters.Count; i++)
        {
            Renderer renderer = characters[i];
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;

            renderer.GetSharedMaterials(_MATERIALS);
            int subMeshCount = _MATERIALS.Count;
            for (int k = 0; k < subMeshCount; k++) context.command.DrawRenderer(renderer, _depthMaterial, k, 0);
        }

        context.command.SetGlobalTexture(_CHARACTER_DEPTH, _CHARACTER_DEPTH);
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        context.command.ReleaseTemporaryRT(_CHARACTER_DEPTH);
    }
}
