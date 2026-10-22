using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(CEnvironmentGradeRenderer), PostProcessEvent.AfterStack, "Espoir/Environment Grade")]
public sealed class CEnvironmentGrade : PostProcessEffectSettings
{
    [Range(0.4f, 2f)] public FloatParameter exposure = new FloatParameter { value = 1f };
    [Range(0f, 2f)] public FloatParameter saturation = new FloatParameter { value = 0.97f };
    [Range(0.6f, 1.6f)] public FloatParameter contrast = new FloatParameter { value = 1.02f };
    [Range(0f, 1f)] public FloatParameter toneAmount = new FloatParameter { value = 0.12f };
    [Range(0f, 0.2f)] public FloatParameter shadowProtection = new FloatParameter { value = 0f };
    public ColorParameter shadowTone = new ColorParameter { value = new Color(0.416f, 0.478f, 0.69f) };
    public ColorParameter highlightTone = new ColorParameter { value = new Color(1f, 0.91f, 0.784f) };
    [Range(0f, 0.8f)] public FloatParameter vignette = new FloatParameter { value = 0.2f };
    public ColorParameter vignetteColor = new ColorParameter { value = new Color(0.353f, 0.29f, 0.353f) };
    [Range(0f, 0.1f)] public FloatParameter grain = new FloatParameter { value = 0.01f };
}
