using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(CToonPostRenderer), PostProcessEvent.BeforeStack, "Espoir/Player Toon Post")]

public sealed class CToonPost : PostProcessEffectSettings
{
    [Range(0f, 1f)] public FloatParameter edgeStrength = new FloatParameter { value = 1f };
    [Range(0.5f, 4f)] public FloatParameter edgeWidth = new FloatParameter { value = 1.2f };
    [Tooltip("외곽선으로 표시할 깊이 차이")]
    [Range(0.001f, 0.05f)] public FloatParameter edgeThreshold = new FloatParameter { value = 0.008f };

    public ColorParameter edgeColor = new ColorParameter { value = new Color(0.078f, 0.047f, 0.094f, 1f) };
    [Range(0f, 1f)] public FloatParameter edgeUseSceneColor = new FloatParameter { value = 1f };
    [Range(0f, 1f)] public FloatParameter edgeDarkness = new FloatParameter { value = 0.32f };

    [Range(0.4f, 2f)] public FloatParameter exposure = new FloatParameter { value = 1f };
    [Range(0f, 2f)] public FloatParameter saturation = new FloatParameter { value = 1f };
    [Range(0.6f, 1.6f)] public FloatParameter contrast = new FloatParameter { value = 1f };
    [Range(0f, 0.8f)] public FloatParameter vignette = new FloatParameter { value = 0.12f };
}
