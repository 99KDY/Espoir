using UnityEngine;

/// <summary>
/// 챕터3 맵은 챕터2 셰이더(툰 · 창)를 같이 쓰므로, 챕터2 맵에서 넘어왔을 때 남아 있는 등불 · 풀 전역값을 비웁니다.
/// 챕터3 낮 조명에서는 등불의 바닥 조명을 끕니다.
/// </summary>
public class CChapter3Environment : MonoBehaviour
{
    private static readonly int _LAMP_COUNT = Shader.PropertyToID("_Chapter2LampCount");
    private static readonly int _GRASS_CENTER = Shader.PropertyToID("_Chapter2GrassCenter");

    private void OnEnable()
    {
        Shader.SetGlobalFloat(_LAMP_COUNT, 0f);
        Shader.SetGlobalVector(_GRASS_CENTER, Vector4.zero);
    }
}
