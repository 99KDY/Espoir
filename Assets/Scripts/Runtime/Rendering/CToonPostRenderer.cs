using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public sealed class CToonPostRenderer : PostProcessEffectRenderer<CToonPost>
{
    private static readonly int _EDGE = Shader.PropertyToID("_Edge");
    private static readonly int _EDGE_COLOR = Shader.PropertyToID("_EdgeColor");
    private static readonly int _EDGE_DARK = Shader.PropertyToID("_EdgeDark");
    private static readonly int _SEPARATE_GRADING = Shader.PropertyToID("_SeparateGrading");
    private static readonly int _EDGE_FADE_DISTANCE = Shader.PropertyToID("_EdgeFadeDistance");
    private static readonly int _GRADE = Shader.PropertyToID("_Grade");

    private static readonly int _HEAT_HAZE = Shader.PropertyToID("_HeatHaze");
    private static readonly int _HEAT_HAZE_TIME = Shader.PropertyToID("_HeatHazeTime");
    private static readonly int _TOON_COLOR = Shader.PropertyToID("_ToonColor");

    private Shader _toonShader;
    private Shader _heatHazeShader;

    /// <summary>
    /// 후처리에 사용할 셰이더를 저장합니다.
    /// </summary>

    public override void Init()
    {
        _toonShader = Shader.Find("Hidden/Espoir/PlayerToonPost");
        _heatHazeShader = Shader.Find("Hidden/Espoir/EnvironmentHeatHaze");
    }

    /// <summary>
    /// 화면 외곽선 계산에 필요한 깊이 텍스처를 요청합니다.
    /// </summary>
    
    public override DepthTextureMode GetCameraFlags()
    {
        return DepthTextureMode.Depth;
    }
    
    /// <summary>
    /// 깊이 외곽선과 색 보정을 화면에 적용합니다. 
    /// </summary>
    
    public override void Render(PostProcessRenderContext context)
    {
        PropertySheet sheet = context.propertySheets.Get(_toonShader);
        float pixelAngle = 2f * Mathf.Tan(context.camera.fieldOfView * 0.5f * Mathf.Deg2Rad) / context.height;
        Color edgeColor = settings.edgeColor.value.linear;

        sheet.properties.SetVector(
            _EDGE,
            new Vector4(settings.edgeStrength, settings.edgeWidth, settings.edgeThreshold, pixelAngle)
        );
        sheet.properties.SetVector(
            _EDGE_COLOR,
            new Vector4(edgeColor.r, edgeColor.g, edgeColor.b, settings.edgeUseSceneColor)
        );
        sheet.properties.SetVector(
            _GRADE,
            new Vector4(settings.exposure, settings.saturation, settings.contrast, settings.vignette)
        );
        sheet.properties.SetFloat(_EDGE_DARK, settings.edgeDarkness);
        sheet.properties.SetFloat(_SEPARATE_GRADING, settings.separateGrading.value ? 1f : 0f);
        sheet.properties.SetFloat(_EDGE_FADE_DISTANCE, settings.edgeFadeDistance);
        if (settings.heatHazeStrength.value <= 0f)
        {
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
            return;
        }

        PropertySheet hazeSheet = context.propertySheets.Get(_heatHazeShader);
        Vector2 distance = settings.heatHazeDistance.value;
        float startDistance = Mathf.Max(0f, distance.x);
        float endDistance = Mathf.Max(startDistance + 0.01f, distance.y);

        hazeSheet.properties.SetVector(
            _HEAT_HAZE,
            new Vector4(settings.heatHazeStrength, startDistance, endDistance, context.camera.aspect)
        );
        hazeSheet.properties.SetFloat(_HEAT_HAZE_TIME, Time.time);

        context.GetScreenSpaceTemporaryRT(context.command, _TOON_COLOR, 0, context.sourceFormat);
        context.command.BlitFullscreenTriangle(context.source, _TOON_COLOR, sheet, 0);
        context.command.BlitFullscreenTriangle(_TOON_COLOR, context.destination, hazeSheet, 0);
        context.command.ReleaseTemporaryRT(_TOON_COLOR);
    }
}
