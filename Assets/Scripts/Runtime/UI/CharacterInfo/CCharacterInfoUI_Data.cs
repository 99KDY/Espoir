using System;
using TMPro;
using UnityEngine;

public partial class CCharacterInfoUI
{
    private enum EStatKind
    {
        HpMax = 0,
        MpMax = 1,
        Attack = 2,
        Defence = 3,
        CriticalRate = 4,
        CriticalDamage = 5,
        AttackSpeed = 6,
        MovementSpeed = 7,
    }

    private enum EBossStatus
    {
        None = 0,
        Unknown = 1,
        Challenging = 2,
        Cleared = 3,
    }

    [Serializable]
    private struct SBar
    {
        public TMP_Text value;
        public TMP_Text max;
        [Tooltip("막대 전체 길이의 바탕")]
        public RectTransform track;
        [Tooltip("채워진 부분 (폭을 비율만큼 줄임)")]
        public RectTransform fill;
        [Tooltip("채워진 부분 위를 지나가는 빛")]
        public RectTransform shine;
        [NonSerialized] public float ratio;
        [NonSerialized] public int shownValue;
        [NonSerialized] public int shownMax;
    }

    [Serializable]
    private struct SStat
    {
        public EStatKind kind;
        public TMP_Text value;
        [Tooltip("기본 능력치 대비 증감량")]
        public TMP_Text bonus;
        [NonSerialized] public int shownValue;
        [NonSerialized] public int shownBonus;
    }

    [Serializable]
    private struct SBossRow
    {
        public EBossClear boss;
        public TMP_Text place;
        public TMP_Text title;
        public TMP_Text name;
        public CHudBox tag;
        public TMP_Text tagText;
        public TMP_Text time;
        [Tooltip("초상 그림 (없으면 빈 칸)")]
        public CCssBoxGraphic portrait;
        [NonSerialized] public EBossStatus shownStatus;
        [NonSerialized] public int shownSeconds;
    }

    private const int _UNSET = int.MinValue;
    private const int _SECONDS_PER_MINUTE = 60;
    private const int _MINUTES_PER_HOUR = 60;

    [Header("정보")]
    [Tooltip("HP, MP 순서")]
    [SerializeField] private SBar[] _bars = new SBar[0];
    [Tooltip("현재값과 최대값 글자 사이 (px)")]
    [SerializeField] private float _barValueGap = 10f;

    [Header("능력치")]
    [SerializeField] private SStat[] _stats = new SStat[0];
    [Tooltip("값과 차이 글자 사이 (px)")]
    [SerializeField] private float _bonusGap = 8f;

    [Header("기록")]
    [SerializeField] private TMP_Text _hoursText;
    [SerializeField] private TMP_Text _hoursUnit;
    [SerializeField] private TMP_Text _minutesText;
    [SerializeField] private TMP_Text _minutesUnit;
    [Tooltip("숫자와 단위 글자 사이, 단위 글자와 다음 숫자 사이 (px)")]
    [SerializeField] private Vector2 _playTimeGaps = new Vector2(4f, 12f);
    [SerializeField] private TMP_Text _killCountText;
    [SerializeField] private TMP_Text _killCaption;
    [SerializeField] private float _killCaptionGap = 8f;
    [SerializeField] private SBossRow[] _bosses = new SBossRow[0];
    [Tooltip("상태 표시의 좌우 안쪽 여백 (px)")]
    [SerializeField] private float _tagPadding = 10f;
    [Tooltip("상태 표시의 세로 위치: 최단 시간이 같이 보일 때 / 상태만 보일 때")]
    [SerializeField] private Vector2 _tagHeights = new Vector2(-12.8f, -24.2f);

    private readonly char[] _BUFFER = new char[24];
    private int _shownHours = _UNSET;
    private int _shownMinutes = _UNSET;
    private int _shownKills = _UNSET;

    private void ResetShownValues()
    {
        for (int i = 0; i < _bars.Length; i++)
        {
            _bars[i].shownValue = _UNSET;
            _bars[i].shownMax = _UNSET;
        }

        for (int i = 0; i < _stats.Length; i++)
        {
            _stats[i].shownValue = _UNSET;
            _stats[i].shownBonus = _UNSET;
        }

        for (int i = 0; i < _bosses.Length; i++)
        {
            _bosses[i].shownStatus = EBossStatus.None;
            _bosses[i].shownSeconds = _UNSET;
            if (_bosses[i].portrait != null)
            {
                _bosses[i].portrait.SetDynamic(new Vector4(1f, 0f, 0f, 0f), Vector4.zero, Vector3.zero,
                    Vector4.zero);
            }
        }

        _shownHours = _UNSET;
        _shownMinutes = _UNSET;
        _shownKills = _UNSET;
    }

    private void RefreshValues(float now)
    {
        switch (_shownPage)
        {
            case 0:
                RefreshInfo(now);
                break;
            case 1:
                RefreshStats(now);
                break;
            case 2:
                RefreshRecords(now);
                break;
        }
    }

    private void RefreshInfo(float now)
    {
        if (_bars.Length < 2) return;

        RefreshBar(ref _bars[0], Mathf.Max(0f, _player.HpCurrent), _player.HpMax, CountUp(now, 0));
        RefreshBar(ref _bars[1], Mathf.Max(0, _player.CurrentMp), _player.MaxMp, CountUp(now, 1));
    }

    private void RefreshBar(ref SBar bar, float current, float max, float countUp)
    {
        bar.ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        int shownMax = Mathf.RoundToInt(max);
        if (shownMax != bar.shownMax)
        {
            bar.shownMax = shownMax;
            int length = 0;
            _BUFFER[length++] = '/';
            _BUFFER[length++] = ' ';
            length = CHudNumberFormat.Write(_BUFFER, shownMax, length);
            bar.max.SetCharArray(_BUFFER, 0, length);
        }

        if (WriteNumber(bar.value, ref bar.shownValue, current * countUp, 0, '\0', null))
        {
            PlaceAfter(bar.max, bar.value, _barValueGap);
        }
    }

    private void RefreshStats(float now)
    {
        for (int i = 0; i < _stats.Length; i++)
        {
            float countUp = CountUp(now, i);
            bool hasChanged;
            switch (_stats[i].kind)
            {
                case EStatKind.HpMax:
                    hasChanged = WriteNumber(_stats[i].value, ref _stats[i].shownValue,
                        _player.HpMax * countUp, 0, '\0', null);
                    hasChanged |= WriteBonus(ref _stats[i], _player.HpMax - _player.BaseHpMax);
                    break;
                case EStatKind.MpMax:
                    hasChanged = WriteNumber(_stats[i].value, ref _stats[i].shownValue,
                        _player.MaxMp * countUp, 0, '\0', null);
                    break;
                case EStatKind.Attack:
                    hasChanged = WriteNumber(_stats[i].value, ref _stats[i].shownValue,
                        _player.Attack * countUp, 0, '\0', null);
                    hasChanged |= WriteBonus(ref _stats[i], _player.Attack - _player.BaseAttack);
                    break;
                case EStatKind.Defence:
                    hasChanged = WriteNumber(_stats[i].value, ref _stats[i].shownValue,
                        _player.Defence * countUp, 0, '\0', null);
                    hasChanged |= WriteBonus(ref _stats[i], _player.Defence - _player.BaseDefence);
                    break;
                case EStatKind.CriticalRate:
                    hasChanged = WriteNumber(_stats[i].value, ref _stats[i].shownValue,
                        _player.CriticalRate * 100f * countUp, 1, '\0', "%");
                    hasChanged |= WriteBonus(ref _stats[i],
                        (_player.CriticalRate - _player.BaseCriticalRate) * 100f, 1, "%p");
                    break;
                case EStatKind.CriticalDamage:
                    hasChanged = WriteNumber(_stats[i].value, ref _stats[i].shownValue,
                        _player.CriticalDamage * 100f * countUp, 1, '\0', "%");
                    hasChanged |= WriteBonus(ref _stats[i],
                        (_player.CriticalDamage - _player.BaseCriticalDamage) * 100f, 1, "%p");
                    break;
                case EStatKind.AttackSpeed:
                    hasChanged = WriteNumber(_stats[i].value, ref _stats[i].shownValue,
                        _player.AttackSpeedMultiplier * countUp, 2, '×', null);
                    hasChanged |= WriteBonus(ref _stats[i],
                        (_player.AttackSpeedMultiplier - 1f) * 100f, 1, "%");
                    break;
                default:
                    hasChanged = WriteNumber(_stats[i].value, ref _stats[i].shownValue,
                        _player.MovementSpeedMultiplier * countUp, 2, '×', null);
                    hasChanged |= WriteBonus(ref _stats[i],
                        (_player.MovementSpeedMultiplier - 1f) * 100f, 1, "%");
                    break;
            }

            if (hasChanged && _stats[i].bonus != null && _stats[i].bonus.gameObject.activeSelf)
            {
                PlaceAfter(_stats[i].bonus, _stats[i].value, _bonusGap);
            }
        }
    }

    private bool WriteBonus(ref SStat stat, float difference, int decimals = 0, string suffix = null)
    {
        if (stat.bonus == null) return false;

        int scale = decimals == 0 ? 1 : decimals == 1 ? 10 : 100;
        int bonus = Mathf.RoundToInt(difference * scale);
        if (bonus == stat.shownBonus) return false;

        stat.shownBonus = bonus;
        stat.bonus.gameObject.SetActive(bonus != 0);
        if (bonus == 0) return false;

        int shown = _UNSET;
        WriteNumber(stat.bonus, ref shown, Mathf.Abs(difference), decimals, bonus > 0 ? '+' : '−', suffix);
        return true;
    }

    private void RefreshRecords(float now)
    {
        CGameManager manager = CGameManager.Instance;
        int totalMinutes = manager != null ? Mathf.FloorToInt(manager.PlayTime / _SECONDS_PER_MINUTE) : 0;
        bool hasMoved = WriteNumber(_hoursText, ref _shownHours,
            totalMinutes / _MINUTES_PER_HOUR * CountUp(now, 0), 0, '\0', null);
        hasMoved |= WriteNumber(_minutesText, ref _shownMinutes,
            totalMinutes % _MINUTES_PER_HOUR * CountUp(now, 1), 0, '\0', null);
        if (hasMoved)
        {
            PlaceAfter(_hoursUnit, _hoursText, _playTimeGaps.x);
            PlaceAfter(_minutesText, _hoursUnit, _playTimeGaps.y);
            PlaceAfter(_minutesUnit, _minutesText, _playTimeGaps.x);
        }

        int kills = 0;
        for (int i = 0; i < _bosses.Length; i++)
        {
            if (RefreshBoss(ref _bosses[i], manager) == EBossStatus.Cleared) kills++;
        }

        if (WriteNumber(_killCountText, ref _shownKills, kills * CountUp(now, 2), 0, '\0', null))
        {
            PlaceAfter(_killCaption, _killCountText, _killCaptionGap);
        }
    }

    private EBossStatus RefreshBoss(ref SBossRow row, CGameManager manager)
    {
        EBossStatus status = EBossStatus.Unknown;
        int seconds = _UNSET;
        bool isSupported = manager != null && (row.boss & EBossClear.AvailableBoss) != 0;

        if (isSupported && (manager.BossClearState & row.boss) != 0)
        {
            status = EBossStatus.Cleared;
            if (manager.TryGetBossClearTime(row.boss, out float best) && best > 0f)
            {
                seconds = Mathf.FloorToInt(best);
            }
        }
        else if (isSupported && ((manager.BossFoundState & row.boss) != 0 ||
            manager.TryGetBossTimer(row.boss, out _, out _)))
        {
            status = EBossStatus.Challenging;
        }

        if (status == row.shownStatus && seconds == row.shownSeconds) return status;

        row.shownStatus = status;
        row.shownSeconds = seconds;
        bool hasBestTime = seconds != _UNSET;
        CCharacterInfoSettingsSO.SBossStatusStyle style = status == EBossStatus.Cleared
            ? _settings.ClearedStyle
            : status == EBossStatus.Challenging ? _settings.ChallengingStyle : _settings.UnknownStyle;

        row.tagText.text = style.label;
        row.tagText.color = style.tagText;
        row.tag.Fill = style.tagFill;
        RectTransform tag = row.tag.rectTransform;
        tag.sizeDelta = new Vector2(row.tagText.preferredWidth + _tagPadding * 2f, tag.sizeDelta.y);
        tag.anchoredPosition = new Vector2(tag.anchoredPosition.x, hasBestTime ? _tagHeights.x : _tagHeights.y);
        row.title.alpha = style.textAlpha;
        row.name.alpha = style.textAlpha;
        row.place.alpha = _settings.PlayerTheme.soft.a * style.placeAlpha;

        row.time.gameObject.SetActive(hasBestTime);
        if (hasBestTime)
        {
            row.time.text = $"{_settings.BestTimePrefix}{seconds / _SECONDS_PER_MINUTE:00}:" +
                $"{seconds % _SECONDS_PER_MINUTE:00}";
        }

        return status;
    }

    private float CountUp(float now, int index)
    {
        float start = _fillTime + _fillOffset + _settings.CountUpDelay + index * _settings.CountUpStagger;
        float k = 1f - Progress(now, start, _settings.CountUpTime);
        return 1f - k * k * k;
    }

    private bool WriteNumber(TMP_Text text, ref int shown, float value, int decimals, char prefix, string suffix)
    {
        int scale = decimals == 0 ? 1 : decimals == 1 ? 10 : 100;
        int scaled = Mathf.Max(0, Mathf.RoundToInt(value * scale));
        if (scaled == shown) return false;

        shown = scaled;
        int length = 0;
        if (prefix != '\0') _BUFFER[length++] = prefix;
        length = CHudNumberFormat.Write(_BUFFER, scaled / scale, length);
        if (decimals > 0)
        {
            int fraction = scaled % scale;
            _BUFFER[length++] = '.';
            if (decimals > 1) _BUFFER[length++] = (char)('0' + fraction / 10);
            _BUFFER[length++] = (char)('0' + fraction % 10);
        }

        if (suffix != null)
        {
            for (int i = 0; i < suffix.Length; i++) _BUFFER[length++] = suffix[i];
        }
        text.SetCharArray(_BUFFER, 0, length);
        return true;
    }

    // 두 텍스트의 앵커와 피벗은 왼쪽 기준이어야 한다.
    private static void PlaceAfter(TMP_Text target, TMP_Text anchor, float gap)
    {
        RectTransform rect = target.rectTransform;
        rect.anchoredPosition = new Vector2(
            anchor.rectTransform.anchoredPosition.x + anchor.preferredWidth + gap, rect.anchoredPosition.y);
    }
}
