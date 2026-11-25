using System.Collections.Generic;

/// <summary>
/// 맵 이동 동안 플레이어 상태를 들고 가는 데이터입니다. 떠나는 씬의 플레이어에서 읽고(Capture) 새 씬의 플레이어에 넣습니다(Apply).
/// 값은 모두 실제 컴포넌트에서 그대로 읽고, 최대 체력 · 최대 MP 같은 능력치는 새 플레이어 자신의 값을 씁니다.
/// 시각 값(스킬 · 소모품 쿨타임, W 사용 기회)은 Time.time 기준이라 씬이 바뀌어도 그대로 이어집니다.
/// </summary>
public class CTravelPlayerState
{
    private struct SStatusState
    {
        public int id;
        public float duration;
        public float baseTimer;
        public int stack;
        public bool isPermanent;
        public bool isStackRemoving;
    }

    private bool _hasData;
    private float _hp;
    private int _mp;
    private int _gold;
    private float _crownWindowEnd;
    private float _dashCooldownDuration;
    private EPradonaPower _pradonaPower;
    private readonly List<CItem> _ITEMS = new List<CItem>();
    private int[] _quickSlotIds = new int[0];
    private readonly Dictionary<(EConsumableCooldownGroup group, int itemId), float> _ITEM_COOLDOWN_ENDS = new();
    private readonly Dictionary<EEquipmentSlot, CItemEquipment> _EQUIPMENT =
        new Dictionary<EEquipmentSlot, CItemEquipment>();
    private readonly List<SStatusState> _STATUSES = new List<SStatusState>();
    private readonly List<int> _ENDED_STATUSES = new List<int>();
    private readonly Dictionary<CSkillSO, float> _SKILL_READY_TIMES = new Dictionary<CSkillSO, float>();

    /// <summary>마지막으로 읽은 체력 (확인용).</summary>
    public float CapturedHp => _hp;

    /// <summary>마지막으로 읽은 MP (확인용).</summary>
    public int CapturedMp => _mp;

    /// <summary>들고 있는 상태가 있으면 true.</summary>
    public bool HasData => _hasData;

    /// <summary>마지막 Capture 에서 이동 때문에 끝난 상태이상 id (기록 · 확인용).</summary>
    public IReadOnlyList<int> EndedStatuses => _ENDED_STATUSES;

    /// <summary>
    /// 떠나는 플레이어의 체력 · MP · 가방 · 장비 · 퀵슬롯 · 쿨타임 · 상태이상을 읽습니다. endOnTravel 에 있는 상태이상은 이동하면서 끝납니다.
    /// </summary>
    public void Capture(CPlayer player, ICollection<int> endOnTravel)
    {
        _hp = player.HpCurrent;
        _mp = player.CurrentMp;
        CInventory inventory = player.Inventory;
        if (inventory != null)
        {
            if (_quickSlotIds.Length != inventory.QuickSlotLength) _quickSlotIds = new int[inventory.QuickSlotLength];
            inventory.ExportState(_ITEMS, _quickSlotIds, _ITEM_COOLDOWN_ENDS, out _gold);
        }

        _EQUIPMENT.Clear();
        foreach (KeyValuePair<EEquipmentSlot, CItemEquipment> pair in player.Equipment)
        {
            _EQUIPMENT[pair.Key] = pair.Value;
        }

        _STATUSES.Clear();
        _ENDED_STATUSES.Clear();
        foreach (CStatus status in player.Status)
        {
            if (endOnTravel.Contains(status.Id))
            {
                _ENDED_STATUSES.Add(status.Id);
                continue;
            }

            _STATUSES.Add(new SStatusState
            {
                id = status.Id,
                duration = status.Duration,
                baseTimer = status.BaseTimer,
                stack = status.Stack,
                isPermanent = status.IsPermanent,
                isStackRemoving = status.IsStackRemoving
            });
        }

        CPlayerSkill skill = player.GetComponent<CPlayerSkill>();
        _crownWindowEnd = skill != null ? skill.ExportTimers(_SKILL_READY_TIMES) : float.NegativeInfinity;
        _dashCooldownDuration = skill != null ? skill.DashCooldownDuration : 0f;
        _pradonaPower = player.PradonaPower;
        _hasData = true;
    }

    /// <summary>
    /// 새 씬의 플레이어에 들고 온 상태를 넣고 비웁니다. 넣은 스킬 쿨타임 수를 돌려줍니다 (스킬 등록 전이면 0).
    /// </summary>
    public int Apply(CPlayer player)
    {
        if (!_hasData) return 0;

        // 장비 → 능력치 다시 계산 (최대 체력은 새 플레이어 능력치에서)
        foreach (KeyValuePair<EEquipmentSlot, CItemEquipment> pair in _EQUIPMENT)
        {
            player.Equipment[pair.Key] = pair.Value;
        }

        player.EquipmentResync();

        // SO의 감소 주기를 유지하고, 남은 시간과 표시 기준은 이동 전 값으로 복원한다.
        foreach (SStatusState s in _STATUSES)
        {
            int before = player.Status.Count;
            player.AddStatus(s.id, s.isPermanent ? 0f : -1f, s.stack);
            CStatus added = player.Status.Count > before ? player.Status[player.Status.Count - 1] :
                player.FindStatus(s.id);
            if (added == null) continue;
            added.Stack = s.stack;
            added.IsStackRemoving = s.isStackRemoving;
            added.RestoreTimer(s.duration, s.baseTimer);
        }

        player.ResyncStatus();

        if (player.Inventory != null) player.Inventory.ImportState(_ITEMS, _quickSlotIds, _ITEM_COOLDOWN_ENDS, _gold);
        player.RestoreHp(_hp);
        player.RestoreMp(_mp);
        player.PradonaPower = _pradonaPower;

        CPlayerSkill skill = player.GetComponent<CPlayerSkill>();
        int restored = skill != null ? skill.ImportTimers(_SKILL_READY_TIMES, _crownWindowEnd) : 0;
        if (skill != null)
        {
            skill.DashCooldownDuration = _dashCooldownDuration;
        }
        Clear();
        return restored;
    }

    /// <summary>들고 있는 상태를 버립니다.</summary>
    public void Clear()
    {
        _hasData = false;
        _ITEMS.Clear();
        _ITEM_COOLDOWN_ENDS.Clear();
        _EQUIPMENT.Clear();
        _STATUSES.Clear();
        _SKILL_READY_TIMES.Clear();
    }
}
