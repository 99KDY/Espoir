using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public sealed class CToonPostRenderer : PostProcessEffectRenderer<CToonPost>
{
    private static readonly int _EDGE = Shader.PropertyToID("_Edge");
    private static readonly int _EDGE_COLOR = Shader.PropertyToID("_EdgeColor");
    private static readonly int _EDGE_DARK = Shader.PropertyToID("_EdgeDark");
    private static readonly int _GRADE = Shader.PropertyToID("_Grade");

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
        PropertySheet sheet = context.propertySheets.Get(Shader.Find("Hidden/Espoir/PlayerToonPost"));
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
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}
