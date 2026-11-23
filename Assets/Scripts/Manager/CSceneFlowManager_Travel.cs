using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 기존 씬 전환 흐름에 플레이어 상태 보존과 도착 위치 적용을 연결한다.
public partial class CSceneFlowManager
{
    [Header("맵 이동 — 이동하면 끝나는 상태이상 (나머지 상태 · 체력 · MP · 가방 · 장비 · 퀵슬롯 · 쿨타임은 이어짐)")]
    [SerializeField] private List<CStatusSO> _endOnTravel = new List<CStatusSO>();

    private bool _hasBossRecords;
    private EBossClear _carriedBossClear;
    private readonly Dictionary<EBossClear, float> _BOSS_RECORDS = new Dictionary<EBossClear, float>();
    private bool _isTraveling;
    private CPlayer _arrivingPlayer;
    private bool _wasArrivalInputLocked;
    private readonly CTravelPlayerState _CARRY = new CTravelPlayerState();
    private readonly HashSet<int> _END_ON_TRAVEL_IDS = new HashSet<int>();

    /// <summary>맵 이동이 시작될 때 (목적지 ID). 대화 · UI 정리용.</summary>
    public static event Action<ESceneId> OnTravelStarted;

    /// <summary>새 맵이 열리고 화면이 다시 보이기 시작할 때 (도착한 맵 ID).</summary>
    public static event Action<ESceneId> OnTravelArrived;

    /// <summary>맵 이동 중이면 true (페이드 · 로드 · 페이드).</summary>
    public bool IsTraveling => _isTraveling;

    /// <summary>마지막 이동에서 끝난 상태이상 id.</summary>
    public IReadOnlyList<int> LastEndedStatuses => _CARRY.EndedStatuses;

    /// <summary>마지막 이동에서 이어 준 상태 (확인용 읽기).</summary>
    public CTravelPlayerState LastCarry => _CARRY;

    /// <summary>검은 화면이 조금이라도 덮여 있으면 true.</summary>
    public bool IsScreenCovered => _fadeGroup != null && _fadeGroup.alpha > 0.001f;

    /// <summary>
    /// 다른 맵으로 이동합니다. 이미 이동 중이거나, 카탈로그에 없거나, 지금 그 맵이면 false 를 돌려줍니다.
    /// 화면을 덮는 동안 현재 플레이어 입력을 잠그고, 새 맵의 플레이어는 그 맵 시작 자리(CMapInfo)에 서며 떠날 때의 상태를 그대로 이어 받습니다.
    /// </summary>
    public bool Travel(ESceneId id)
    {
        if (_isTraveling) return false;
        if (TryGetSceneName(id, out string sceneName) == false || string.IsNullOrEmpty(sceneName))
        {
            CLogger.Error($"맵 이동 실패 : 카탈로그에 없음 {id}");
            return false;
        }

        if (SceneManager.GetActiveScene().name == sceneName) return false;
        StartCoroutine(Co_Travel(id, sceneName));
        return true;
    }

    /// <summary>
    /// 지금 열린 씬의 ID 를 찾습니다.
    /// </summary>
    public bool TryGetCurrentSceneId(out ESceneId id)
    {
        return TryGetSceneId(SceneManager.GetActiveScene().name, out id);
    }

    private IEnumerator Co_Travel(ESceneId id, string sceneName)
    {
        _isTraveling = true;
        OnTravelStarted?.Invoke(id);
        CPlayer leaving = FindFirstObjectByType<CPlayer>();
        bool wasLeavingInputLocked = leaving != null && leaving.IsInputLocked;
        if (leaving != null) leaving.SetInputLocked(true);

        try
        {
            yield return StartCoroutine(Co_LoadSceneWithTransition(id, sceneName));
        }
        finally
        {
            if (_arrivingPlayer != null) _arrivingPlayer.SetInputLocked(_wasArrivalInputLocked);
            if (leaving != null) leaving.SetInputLocked(wasLeavingInputLocked);
            _arrivingPlayer = null;
            _isTraveling = false;
            if (_firstLoadFromTitle) _firstLoadFromTitle = false;
        }
    }

    // Co_LoadSceneWithTransition 이 새 씬을 켜기 직전에 부름: 떠나는 플레이어 상태를 읽어 둠 (맵 이동일 때만)
    private void CaptureLeavingPlayer()
    {
        _CARRY.Clear();
        _hasBossRecords = false;
        _BOSS_RECORDS.Clear();
        if (!_isTraveling) return;

        CGameManager game = CGameManager.Instance;
        if (game != null)
        {
            _carriedBossClear = game.BossClearState;
            foreach (KeyValuePair<EBossClear, float> pair in game.BossTime)
            {
                _BOSS_RECORDS.Add(pair.Key, pair.Value);
            }
            _hasBossRecords = true;
        }

        CPlayer player = FindFirstObjectByType<CPlayer>();
        if (player == null) return;
        _END_ON_TRAVEL_IDS.Clear();
        foreach (CStatusSO status in _endOnTravel)
        {
            if (status != null) _END_ON_TRAVEL_IDS.Add(status.Id);
        }

        _CARRY.Capture(player, _END_ON_TRAVEL_IDS);
    }

    // Co_LoadSceneWithTransition 이 새 씬을 연 뒤, 화면을 다시 밝히기 전에 부름
    private void PlaceArrivedPlayer(ESceneId id)
    {
        CMapInfo map = CMapInfo.Current;
        CPlayer player = FindFirstObjectByType<CPlayer>();
        if (_isTraveling && player != null)
        {
            _arrivingPlayer = player;
            _wasArrivalInputLocked = player.IsInputLocked;
            player.SetInputLocked(true);
        }

        if (player != null && _CARRY.HasData)
        {
            int skills = _CARRY.Apply(player);
            if (skills == 0) CLogger.Warn("맵 이동 : 새 플레이어 스킬이 아직 등록되지 않아 쿨타임을 넣지 못함");
        }

        if (map != null && map.StartPoint != null && player != null)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (wasEnabled) controller.enabled = false;
            player.transform.SetPositionAndRotation(map.StartPoint.position, map.StartPoint.rotation);
            if (wasEnabled) controller.enabled = true;
        }

        if (_hasBossRecords && !_firstLoadFromTitle && CGameManager.Instance != null)
        {
            CGameManager.Instance.RestoreBossRecords(_carriedBossClear, _BOSS_RECORDS);
        }
        _hasBossRecords = false;
        _BOSS_RECORDS.Clear();

        OnTravelArrived?.Invoke(id);

        if (_firstLoadFromTitle)
        {
            _firstLoadFromTitle = false;

            int slot = _targetSaveData;
            bool useSaveData = _loadSavedData;
            _loadSavedData = false;

            CGameManager game = CGameManager.Instance;
            if (game == null || !game.InitializeSaveslot(slot, useSaveData))
            {
                _targetSaveData = -1;
                CLogger.Error($"{slot + 1}번 슬롯의 첫 저장 또는 불러오기에 실패했습니다.");
            }
        }
    }
}
