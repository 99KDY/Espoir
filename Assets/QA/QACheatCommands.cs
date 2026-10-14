using UnityEngine;
using IngameDebugConsole;

public class QACheatCommands : MonoBehaviour
{
    private void Awake()
    {
        // 실행 명령어
        DebugLogConsole.AddCommand("god", "플레이어를 무적 상태로 만듭니다.", GodMode);
        DebugLogConsole.AddCommand("killall", "화면의 모든 적을 처치합니다.", KillAllEnemies);
        DebugLogConsole.AddCommand("heal", "플레이어의 체력을 즉시 100% 회복합니다.", Cheat_HealPlayer);
        DebugLogConsole.AddCommand("die", "플레이어를 즉시 사망 상태로 만듭니다.", Cheat_KillPlayer);

        // 숫자 입력받는 명령어
        DebugLogConsole.AddCommand<int>("gold", "입력한 만큼 골드를 추가합니다. (예: gold 5000)", AddGold);
    }

    public void GodMode()
    {
        CLogger.Log("[QA] 무적 모드가 활성화되었습니다. (기능 연결 필요)");
        // TODO : 플레이어 코드를 완성하면 여기에 체력 감소 안되게 하는거 추가예정
    }

    public void KillAllEnemies()
    {
        CLogger.Log("[QA] 모든 적에게 즉사 명령을 내렸습니다. (기능 연결 필요)");
        // TODO : 적 시스템 완성하면 여기에 모든 적 파괴 코드 추가예정
    }

    public void AddGold(int amount)
    {
        CLogger.Log($"[QA] 골드가 {amount}만큼 추가되었습니다. (기능 연결 필요)");
        // TODO : 재화 매니저 완성하면 데이터 연동예정
    }

    /// <summary>
    /// 플레이어 체력 회복 치트 기능
    /// </summary>
    public void Cheat_HealPlayer()
    {
        CLogger.Log("[QA] 플레이어 체력 회복 명령을 실행합니다. (기능 연결 필요)");

        // TODO : 플레이어 체력코드 연동예정

        // FindObjectOfType<CPlayer>().currentHP = 100;
    }

    /// <summary>
    /// 플레이어 강제 사망 치트 기능
    /// </summary>
    public void Cheat_KillPlayer()
    {
        CLogger.Log("[QA] 플레이어 강제 사망 명령을 실행합니다. (기능 연결 필요)");

        // TODO : 플레이어 사망 코드 연동예정

        // FindObjectOfType<CPlayer>().Die();
    }

}
