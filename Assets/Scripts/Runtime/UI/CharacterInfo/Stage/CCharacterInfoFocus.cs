using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// 캐릭터 정보창의 배경 흐림과 색상 효과 설정입니다.
/// </summary>
[Serializable]
[PostProcess(typeof(CCharacterInfoFocusRenderer), PostProcessEvent.AfterStack, "Espoir/Character Info Focus", false)]
public sealed class CCharacterInfoFocus : PostProcessEffectSettings
{
    [Tooltip("초점 거리, 초점 범위 (m)")]
    public Vector2Parameter focus = new Vector2Parameter { value = new Vector2(4f, 1f) };
    [Tooltip("배경 흐림 반지름 (px)")]
    public FloatParameter blur = new FloatParameter { value = 0f };
    [Range(0f, 1f)] public FloatParameter dim = new FloatParameter { value = 0f };
    [Range(0f, 1f)] public FloatParameter saturation = new FloatParameter { value = 1f };
    [Range(0f, 1f)] public FloatParameter vignette = new FloatParameter { value = 0f };
    public Vector4Parameter tint = new Vector4Parameter { value = Vector4.one };
    [Tooltip("화면 중심 (x, y), 캐릭터와 먼 곳도 배경으로 다루는 정도, 초점 거리에서 1m 가 화면 높이의 몇 배인지")]
    public Vector4Parameter center = new Vector4Parameter { value = new Vector4(0.5f, 0.5f, 0f, 0.4f) };
    [Tooltip("효과에서 빼는 원 (x, y, 반지름, 세기)")]
    public Vector4Parameter protect = new Vector4Parameter { value = new Vector4(0.5f, 0.5f, 0.1f, 0f) };
    [Tooltip("섬광, 줌 블러, 흑백, 흑백 반전")]
    public Vector4Parameter impact = new Vector4Parameter { value = Vector4.zero };
    public ColorParameter themeLight = new ColorParameter { value = Color.white };
    public Vector4Parameter themeTint = new Vector4Parameter { value = Vector4.one };
    [Tooltip("배경 색상 강도, 가장자리 색상 강도")]
    public Vector2Parameter themeAmount = new Vector2Parameter { value = Vector2.zero };
    [Tooltip("정보 패널 흐림 반지름 (px), 오른쪽 경계 (0~1), 경계 폭")]
    public Vector4Parameter panel = new Vector4Parameter { value = new Vector4(0f, 0.5f, 0.1f, 0f) };

    /// <summary>
    /// 효과를 적용할 정보창 카메라입니다.
    /// </summary>
    public static Camera TargetCamera { get; set; }

    /// <summary>
    /// 배경 효과에서 제외할 캐릭터 렌더러입니다.
    /// </summary>
    public static List<Renderer> Characters { get; } = new List<Renderer>();

    /// <summary>
    /// 정보창 카메라의 효과 활성 여부를 반환합니다.
    /// </summary>
    public override bool IsEnabledAndSupported(PostProcessRenderContext context)
    {
        return enabled.value && TargetCamera != null && context.camera == TargetCamera;
    }
}
