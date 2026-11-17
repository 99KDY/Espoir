using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// 플레이어(CEnvironmentGradeMask 대상) 위에는 노멀 선을 그리지 않고 깊이 선만 그립니다.
/// </summary>
[Serializable]
[PostProcess(typeof(CChapter2EdgeRenderer), PostProcessEvent.BeforeStack, "Espoir/Chapter2 Edge")]
public sealed class CChapter2Edge : PostProcessEffectSettings
{
    [Tooltip("선 두께 (화면 픽셀)")]
    [Range(0.5f, 4f)] public FloatParameter width = new FloatParameter { value = 1.2f };

    [Tooltip("깊이 차이 선 세기")]
    [Range(0f, 1f)] public FloatParameter depthStrength = new FloatParameter { value = 1f };

    [Tooltip("면 꺾임(노멀) 선 세기")]
    [Range(0f, 1f)] public FloatParameter normalStrength = new FloatParameter { value = 0.5f };

    [Tooltip("깊이 선 판정 기준 (거리 비례)")]
    [Range(0.001f, 0.05f)] public FloatParameter depthThreshold = new FloatParameter { value = 0.012f };

    [Tooltip("노멀 선 판정 기준 (1 - 두 면 노멀 내적)")]
    [Range(0f, 1f)] public FloatParameter normalThreshold = new FloatParameter { value = 0.55f };

    [Tooltip("선 색에 장면 색을 섞을 때 어둡게 하는 배율")]
    [Range(0f, 1f)] public FloatParameter darkness = new FloatParameter { value = 0.42f };

    [Tooltip("선 색 (장면 색을 섞지 않는 부분)")]
    public ColorParameter color = new ColorParameter { value = new Color(0.078f, 0.047f, 0.133f, 1f) };

    [Tooltip("선 색에 장면 색을 섞는 비율")]
    [Range(0f, 1f)] public FloatParameter useSceneColor = new FloatParameter { value = 0.8f };

    [Tooltip("이 거리(m) 쯤에서 선이 사라짐")]
    [Range(10f, 500f)] public FloatParameter fadeDistance = new FloatParameter { value = 160f };
}
