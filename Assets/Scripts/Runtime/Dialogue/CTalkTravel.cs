using System;

/// <summary>
/// 대화(길잡이 선택지)에서 맵 이동을 요청하는 연결점.
/// 등록된 Handler에 요청을 전달하며 실제 씬 전환은 CSceneFlowManager가 담당합니다.
/// 맵 키는 "m51" 사막, "c2a" 달빛 장미 정원, "c3a" 천공의 탑입니다.
/// </summary>
public static class CTalkTravel
{
    /// <summary>
    /// 맵 키를 받아 이동을 시작하고, 받아들였으면 true 를 반환하는 처리기. 없으면 이동하지 않는다.
    /// </summary>
    public static Func<string, bool> Handler;

    /// <summary>
    /// 마지막으로 요청한 맵 키입니다.
    /// </summary>
    public static string LastRequest { get; private set; }

    /// <summary>
    /// 맵 이동을 요청합니다. 처리기가 없거나 거절하면 false 를 반환합니다.
    /// </summary>
    public static bool Request(string mapKey)
    {
        LastRequest = mapKey;

        if (Handler == null)
        {
            CLogger.Warn($"CTalkTravel: '{mapKey}' 이동 요청을 받을 처리기가 없습니다.");
            return false;
        }

        return Handler(mapKey);
    }
}
