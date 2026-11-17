using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// 구름 바다 효과를 화면에 그립니다.
/// </summary>
public sealed class CChapter3CloudSeaRenderer : PostProcessEffectRenderer<CChapter3CloudSea>
{
    private static readonly int _CAMERA_TO_WORLD = Shader.PropertyToID("_CloudCameraToWorld");
    private static readonly int _PROJECTION = Shader.PropertyToID("_CloudProjection");
    private static readonly int _LAYER = Shader.PropertyToID("_CloudLayer");
    private static readonly int _FOG = Shader.PropertyToID("_CloudFog");
    private static readonly int _SHAPE = Shader.PropertyToID("_CloudShape");
    private static readonly int _LIT = Shader.PropertyToID("_CloudLit");
    private static readonly int _SHADE = Shader.PropertyToID("_CloudShade");
    private static readonly int _FAR = Shader.PropertyToID("_CloudFar");
    private static readonly int _SUN = Shader.PropertyToID("_CloudSun");
    private static readonly int _WIND = Shader.PropertyToID("_CloudWind");

    private Shader _shader;

    /// <summary>
    /// 구름 바다 셰이더를 찾습니다.
    /// </summary>
    public override void Init()
    {
        _shader = Shader.Find("Hidden/Espoir/Chapter3CloudSea");
    }

    /// <summary>
    /// 깊이 텍스처를 요청합니다.
    /// </summary>
    public override DepthTextureMode GetCameraFlags()
    {
        return DepthTextureMode.Depth;
    }

    /// <summary>
    /// 화면 깊이를 따라 구름 층을 쌓아 그립니다.
    /// </summary>
    public override void Render(PostProcessRenderContext context)
    {
        PropertySheet sheet = context.propertySheets.Get(_shader);
        Matrix4x4 projection = context.camera.projectionMatrix;
        Vector2 wind = settings.wind.value.normalized;

        sheet.properties.SetMatrix(_CAMERA_TO_WORLD, context.camera.cameraToWorldMatrix);
        sheet.properties.SetVector(_PROJECTION,
            new Vector4(projection.m00, Mathf.Abs(projection.m11), projection.m02, projection.m12));
        sheet.properties.SetVector(_LAYER,
            new Vector4(settings.top, settings.bottom, settings.density, settings.scale));
        sheet.properties.SetVector(_FOG,
            new Vector4(settings.fogNear, settings.fogFar, settings.extinction, Time.time));
        sheet.properties.SetVector(_SHAPE, settings.shape.value);
        sheet.properties.SetVector(_LIT, Linear(settings.litColor.value));
        sheet.properties.SetVector(_SHADE, Linear(settings.shadeColor.value));
        sheet.properties.SetVector(_FAR, Linear(settings.farColor.value));
        Vector3 sun = settings.sunDirection.value.normalized;
        sheet.properties.SetVector(_SUN, new Vector4(sun.x, sun.y, sun.z, 0f));
        sheet.properties.SetVector(_WIND, new Vector4(wind.x, wind.y, 0f, 0f));
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }

    // 프로필의 화면 기준 색상을 셰이더에서 사용할 Linear 값으로 변환합니다.
    private static Vector4 Linear(Color color)
    {
        Color c = color.linear;
        return new Vector4(c.r, c.g, c.b, 1f);
    }
}
