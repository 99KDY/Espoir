using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// 깊이와 노멀 차이를 이용해 환경 외곽선을 그립니다.
/// </summary>
public sealed class CChapter2EdgeRenderer : PostProcessEffectRenderer<CChapter2Edge>
{
    private static readonly int _EDGE = Shader.PropertyToID("_Edge");
    private static readonly int _EDGE2 = Shader.PropertyToID("_Edge2");
    private static readonly int _EDGE_COLOR = Shader.PropertyToID("_EdgeColor");
    private static readonly int _PROJECTION = Shader.PropertyToID("_ViewProjection");
    private static readonly int _MASK = Shader.PropertyToID("_Chapter2EdgeMask");

    private Shader _shader;
    private Material _maskMaterial;

    /// <summary>
    /// 외곽선 셰이더와 플레이어 마스크 재질을 준비합니다.
    /// </summary>
    public override void Init()
    {
        _shader = Shader.Find("Hidden/Espoir/Chapter2Edge");
        _maskMaterial = new Material(Shader.Find("Hidden/Espoir/PlayerPostMask"))
        {
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    /// <summary>
    /// 깊이 텍스처를 요청합니다.
    /// </summary>
    public override DepthTextureMode GetCameraFlags()
    {
        return DepthTextureMode.Depth;
    }

    /// <summary>
    /// 마스크 재질을 해제합니다.
    /// </summary>
    public override void Release()
    {
        RuntimeUtilities.Destroy(_maskMaterial);
    }

    /// <summary>
    /// 외곽선을 화면에 그립니다.
    /// </summary>
    public override void Render(PostProcessRenderContext context)
    {
        CommandBuffer cmd = context.command;
        PropertySheet sheet = context.propertySheets.Get(_shader);
        Matrix4x4 projection = context.camera.projectionMatrix;
        Color color = settings.color.value.linear;

        sheet.properties.SetVector(
            _EDGE,
            new Vector4(settings.width, settings.depthStrength, settings.normalStrength, settings.depthThreshold)
        );
        sheet.properties.SetVector(
            _EDGE2,
            new Vector4(settings.normalThreshold, settings.darkness, settings.useSceneColor, settings.fadeDistance)
        );
        sheet.properties.SetVector(_EDGE_COLOR, new Vector4(color.r, color.g, color.b, 1f));
        sheet.properties.SetVector(
            _PROJECTION,
            new Vector4(projection.m00, Mathf.Abs(projection.m11), projection.m02, projection.m12)
        );

        bool hasMask = CEnvironmentGradeMask.HasTargets;

        if (hasMask)
        {
            context.GetScreenSpaceTemporaryRT(cmd, _MASK, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            cmd.SetRenderTarget(_MASK);
            cmd.ClearRenderTarget(false, true, Color.clear);
            CEnvironmentGradeMask.DrawMasks(cmd, context.camera, _maskMaterial);
            cmd.SetGlobalTexture(_MASK, _MASK);
        }

        else
        {
            cmd.SetGlobalTexture(_MASK, Texture2D.blackTexture);
        }

        cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);

        if (hasMask)
        {
            cmd.ReleaseTemporaryRT(_MASK);
        }
    }
}
