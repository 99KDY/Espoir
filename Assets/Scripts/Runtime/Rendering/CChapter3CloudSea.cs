using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// 화면 깊이를 따라 bottom~top 높이에 있는 구름 부피의 안개를 누적합니다.
/// 해와 바람 방향은 원본 오른손 좌표계 기준으로 저장됩니다.
/// </summary>
[Serializable]
[PostProcess(typeof(CChapter3CloudSeaRenderer), PostProcessEvent.BeforeStack, "Espoir/Chapter3 Cloud Sea")]
public sealed class CChapter3CloudSea : PostProcessEffectSettings
{
    [Tooltip("구름 층 윗면 높이 (m)")]
    public FloatParameter top = new FloatParameter { value = -0.6f };

    [Tooltip("구름 층 아랫면 높이 (m)")]
    public FloatParameter bottom = new FloatParameter { value = -20f };

    [Tooltip("밀도 배율")]
    public FloatParameter density = new FloatParameter { value = 1f };

    [Tooltip("노이즈 크기 배율")]
    public FloatParameter scale = new FloatParameter { value = 1f };

    [Tooltip("먼 구름이 지평선 색으로 바뀌기 시작하는 거리 (m)")]
    public FloatParameter fogNear = new FloatParameter { value = 170f };

    [Tooltip("먼 구름이 지평선 색이 되는 거리 (m)")]
    public FloatParameter fogFar = new FloatParameter { value = 560f };

    [Tooltip("x 높이 · y 큰 덩이 · z 잔 결 · w 깎는 값")]
    public Vector4Parameter shape = new Vector4Parameter { value = new Vector4(1.9f, 2.3f, 1.15f, 0.95f) };

    [Tooltip("짙기")]
    public FloatParameter extinction = new FloatParameter { value = 0.42f };

    [Tooltip("빛 받은 구름 색")]
    public ColorParameter litColor = new ColorParameter { value = new Color(0.949f, 0.961f, 0.988f, 1f) };

    [Tooltip("그늘진 구름 색")]
    public ColorParameter shadeColor = new ColorParameter { value = new Color(0.616f, 0.69f, 0.894f, 1f) };

    [Tooltip("먼 구름 색 (지평선 안개)")]
    public ColorParameter farColor = new ColorParameter { value = new Color(0.875f, 0.91f, 0.965f, 1f) };

    [Tooltip("해 방향 (웹 좌표)")]
    public Vector3Parameter sunDirection = new Vector3Parameter { value = new Vector3(-0.447f, 0.602f, 0.662f) };

    [Tooltip("바람 방향 (웹 좌표 x, z)")]
    public Vector2Parameter wind = new Vector2Parameter { value = new Vector2(0.503f, 0.865f) };
}
