using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public sealed class CEnvironmentGradeRenderer : PostProcessEffectRenderer<CEnvironmentGrade>
{
    private static readonly int _GRADE = Shader.PropertyToID("_Grade");
    private static readonly int _SHADOW_TONE = Shader.PropertyToID("_ShadowTone");
    private static readonly int _HIGHLIGHT_TONE = Shader.PropertyToID("_HighlightTone");
    private static readonly int _VIGNETTE_COLOR = Shader.PropertyToID("_VignetteColor");
    private static readonly int _DETAIL = Shader.PropertyToID("_Detail");

    private Shader _shader;

    /// <summary>
    /// 색 보정에 사용할 셰이더를 저장합니다.
    /// </summary>

    public override void Init()
    {
        _shader = Shader.Find("Hidden/Espoir/EnvironmentGrade");
    }

    /// <summary>
    /// 빛 번짐 이후 선형 공간에서 맵의 색감과 밝기를 보정합니다.
    /// </summary>

    public override void Render(PostProcessRenderContext context)
    {
        PropertySheet sheet = context.propertySheets.Get(_shader);
        sheet.properties.SetVector(
            _GRADE,
            new Vector4(settings.exposure, settings.saturation, settings.contrast, settings.toneAmount)
        );
        sheet.properties.SetColor(_SHADOW_TONE, settings.shadowTone.value.linear);
        sheet.properties.SetColor(_HIGHLIGHT_TONE, settings.highlightTone.value.linear);
        sheet.properties.SetColor(_VIGNETTE_COLOR, settings.vignetteColor.value.linear);
        sheet.properties.SetVector(
            _DETAIL,
            new Vector4(settings.vignette, settings.grain, Time.time, settings.shadowProtection)
        );
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}
