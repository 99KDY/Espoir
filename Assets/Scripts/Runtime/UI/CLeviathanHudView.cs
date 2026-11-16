using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// R의 강화 상태, 피니시 중첩과 남은 타격을 표시하고 HUD 연출을 갱신합니다.
/// 게임 수치는 CPlayerLeviathan과 CStatus에서 읽고, 화면 효과는 CHudGroupFx에 전달합니다.
/// </summary>
[DefaultExecutionOrder(200)]
public class CLeviathanHudView : MonoBehaviour
{
    private sealed class CTrack
    {
        public float start;
        public float duration;
        public readonly List<float> hits = new List<float>();
        public float caAmp;
        public bool isCaHit;
        public bool isCaRed;
        public float shake;
        public float white;
        public float ghost;
        public float glitch;
        public bool isGrow;
        public int growTotal;
    }

    [SerializeField] private CPlayerHudView _hudView;
    [SerializeField] private CHudGroupFx _groupFx;

    [Header("R 칸")]
    [SerializeField] private CHudRing _ring;
    [SerializeField] private CanvasGroup _iconGroup;
    [SerializeField] private CanvasGroup _cooldownGroup;
    [SerializeField] private CHudCooldownRise _cooldownRise;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private Material _countMaterial;
    [SerializeField] private Material _countMaxMaterial;
    [SerializeField] private TMP_Text _bonusText;
    [SerializeField] private CanvasGroup _ultTag;

    [Header("궁 상태 (HUD 전체)")]
    [SerializeField] private Image _overlay;
    [SerializeField] private CHudSlotStyle[] _slots;
    [SerializeField] private CHudBar[] _bars;
    [SerializeField] private RectTransform[] _waveSlots;

    [Header("색상")]
    [SerializeField] private Color _crimson = new Color(1f, 61f / 255f, 82f / 255f);
    [SerializeField] private Color _gold = new Color(1f, 210f / 255f, 122f / 255f);
    [SerializeField, Range(0f, 1f)] private float _overlayAlpha = 0.12f;
    [SerializeField, Range(0f, 1f)] private float _overlayOpacity = 0.4f;

    [Header("궁 상태 연출")]
    [SerializeField] private float _stateOnTime = 0.6f;
    [SerializeField] private float _stateOffTime = 0.05f;
    [SerializeField] private float _iconAlphaStack = 0.18f;
    [SerializeField] private float _iconAlphaFinisher = 0.15f;
    [SerializeField] private float _countSize = 64f;
    [SerializeField] private float _finisherCountSize = 52f;
    [SerializeField] private float _bonusDelay = 0.6f;
    [SerializeField] private float _entryTime = 1.8f;
    [Tooltip("R 슬롯 위 보너스 타격 문구의 기본 Y 위치")]
    [SerializeField] private float _bonusRestY = 15f;

    private const float _HIT_DECAY = 13f;
    private const float _WAVE_STEP = 0.055f;
    private const float _WAVE_TIME = 0.45f;
    private const float _WAVE_HEIGHT = 12f;
    private const float _SLAM_TIME = 0.28f;
    private const float _TICK_TIME = 0.12f;
    private const float _RISE_TIME = 1.1f;
    private const float _TAG_TIME = 0.2f;
    private const float _TAG_SHOW_TIME = 1.4f;

    private readonly List<CTrack> _TRACKS = new List<CTrack>();
    private CPlayer _player;
    private CPlayerLeviathan _leviathan;
    private ELeviathanState _shownState;
    private int _shownStack = -1;
    private int _shownHits;
    private bool _wasReady;
    private bool _hasReadyBaseline;

    private Vector2[] _waveBase;
    private float _tagBaseY;
    private float _tagFrom;
    private float _tagTo;
    private float _tagStart = -10f;
    private float _tagUntil = -10f;
    private bool _isTagShown;
    private CSkillSO _ultSkill;
    private float _waveStart = -1f;
    private float _stateFrom;
    private float _stateTo;
    private float _stateStart = -10f;
    private float _stateDuration = 0.6f;
    private float _stateOffAt = -1f;
    private float _buffStart = -10f;
    private float _bonusAt = -1f;
    private float _bonusTextStart = -1f;
    private float _bloomStart = -1f;
    private float _thirtyAt = -1f;
    private float _countAnimStart = -1f;
    private bool _isCountSlam;
    private Image _iconImage;
    private CTrack _growTrack;

    private void Awake()
    {
        _waveBase = new Vector2[_waveSlots.Length];
        for (int i = 0; i < _waveSlots.Length; i++)
        {
            _waveBase[i] = _waveSlots[i] != null ? _waveSlots[i].anchoredPosition : Vector2.zero;
        }

        if (_ultTag != null)
        {
            _tagBaseY = ((RectTransform)_ultTag.transform).anchoredPosition.y;
        }

        HideEffects();
    }

    private void OnDisable()
    {
        if (_cooldownRise != null)
        {
            _cooldownRise.IsSuppressed = false;
        }
    }

    private void LateUpdate()
    {
        if (_leviathan == null && !TryConnect()) return;

        float now = Time.unscaledTime;
        ELeviathanState state = _leviathan.State;
        if (state != _shownState)
        {
            OnStateChanged(_shownState, state, now);
            _shownState = state;
        }

        UpdateFlow();
        UpdateReadyWave(now);
        UpdateSlot(state, now);
        UpdateStateWeight(now);
        UpdateTracks(now);
        UpdateCount(now);
        UpdateBonus(now);
    }

    private bool TryConnect()
    {
        if (_hudView == null || _hudView.Player == null) return false;

        CPlayerSkill skill = _hudView.Player.GetComponent<CPlayerSkill>();
        if (skill == null || skill.Leviathan.Settings == null) return false;

        _player = _hudView.Player;
        _leviathan = skill.Leviathan;
        _shownState = _leviathan.State;
        return true;
    }

    private void OnStateChanged(ELeviathanState from, ELeviathanState to, float now)
    {
        if (to == ELeviathanState.Buff)
        {
            // 강화 상태가 시작될 때 섬광과 색 변화를 재생한다.
            CTrack cast = AddTrack(now, 1.4f);
            cast.hits.Add(0.05f);
            cast.glitch = 0.6f;
            cast.caAmp = 10f;
            cast.isCaHit = true;
            SetState(1f, _stateOnTime, now);
            _stateOffAt = -1f;
            _buffStart = now;
            _shownStack = 0;
            _bonusAt = -1f;
            _thirtyAt = -1f;
            HideCount();
        }

        else if (to == ELeviathanState.Finisher)
        {
            // 피니시 타격이 진행될수록 흔들림과 색수차를 키운다.
            _shownHits = 0;
            _bonusAt = -1f;
            _thirtyAt = -1f;
            HideCount();
            EndGrow();
            _growTrack = AddTrack(now, float.MaxValue);
            _growTrack.isGrow = true;
            _growTrack.growTotal = Mathf.Max(1, _leviathan.FinisherHitCount);
            if (from == ELeviathanState.None)
            {
                SetState(1f, _stateOnTime, now);
            }
        }

        else if (from == ELeviathanState.Finisher)
        {
            // 피니시 종료 연출이 끝난 뒤 기본 색으로 돌린다.
            EndGrow();
            HideCount();
            CTrack back = AddTrack(now, 0.5f);
            back.hits.Add(0.05f);
            back.glitch = 1f;
            back.caAmp = 10f;
            _stateOffAt = now + 0.35f;
        }

        else
        {
            EndGrow();
            HideCount();
            SetState(0f, _stateOffTime, now);
        }
    }

    private void UpdateFlow()
    {
        CStatus flow = _player.FindStatus(_leviathan.Settings.FlowStatus.Id);
        float ratio = flow != null && flow.MaxStack > 0 ? (float)flow.Stack / flow.MaxStack : 0f;
        if (_ring != null) _ring.Flow = ratio;
    }

    private void UpdateReadyWave(float now)
    {
        // 쿨타임과 MP 조건을 만족하는 순간 슬롯을 차례로 들어 올린다.
        // 동작 종료마다 반복되지 않도록 현재 행동의 사용 제한은 보지 않는다.
        bool isReady = _leviathan.State == ELeviathanState.None && IsUltReady();
        if (_hasReadyBaseline && isReady && !_wasReady)
        {
            _waveStart = now;

            _tagUntil = now + _TAG_SHOW_TIME;
        }

        _wasReady = isReady;
        _hasReadyBaseline = true;
        if (_waveStart < 0f) return;

        bool isDone = true;
        for (int i = 0; i < _waveSlots.Length; i++)
        {
            if (_waveSlots[i] == null) continue;

            float x = (now - _waveStart - i * _WAVE_STEP) / _WAVE_TIME;
            float lift = 0f;
            if (x < 1f) isDone = false;
            if (x > 0f && x < 1f)
            {
                lift = x < 0.4f ? WaveEase(x / 0.4f) : 1f - WaveEase((x - 0.4f) / 0.6f);
            }

            _waveSlots[i].anchoredPosition = _waveBase[i] + new Vector2(0f, lift * _WAVE_HEIGHT);
        }

        if (isDone) _waveStart = -1f;
    }

    private bool IsUltReady()
    {
        if (_ultSkill == null)
        {
            foreach (CSkillSO s in _player.Skills)
            {
                if (s is CLeviathanSkillSO) _ultSkill = s;
            }
        }

        _player.GetSkillCooldown(ESkillType.Slam, out _, out float remaining);
        return remaining <= 0f && (_ultSkill == null || _player.CurrentMp >= _ultSkill.MpCost);
    }

    private static float WaveEase(float t)
    {
        return CHudEase.Bezier(0.2f, 0.9f, 0.3f, 1.4f, t);
    }

    private void UpdateSlot(ELeviathanState state, float now)
    {
        bool inState = state != ELeviathanState.None;
        if (_cooldownRise != null) _cooldownRise.IsSuppressed = inState;
        if (_cooldownGroup != null) _cooldownGroup.alpha = inState ? 0f : 1f;

        if (_iconGroup != null)
        {
            // 강화와 피니시 중에는 기본 쿨타임 표시보다 궁 상태의 아이콘 투명도를 우선한다.
            if (state == ELeviathanState.Buff && (now - _buffStart >= _entryTime || _leviathan.Stack > 0))
            {
                SetIconAlpha(_iconAlphaStack);
            }

            else if (state == ELeviathanState.Finisher) SetIconAlpha(_iconAlphaFinisher);
        }

        // 준비 알림은 잠시 표시하고, 강화와 피니시 중에는 숨긴다.

        bool showTag = !inState && now < _tagUntil;
        if (showTag != _isTagShown)
        {
            _tagFrom = TagValue(now);
            _tagTo = showTag ? 1f : 0f;
            _tagStart = now;
            _isTagShown = showTag;
        }

        if (inState) _tagUntil = -10f;
        if (_ultTag != null)
        {
            float e = TagValue(now);
            _ultTag.alpha = e;
            RectTransform rt = (RectTransform)_ultTag.transform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, _tagBaseY - 6f * (1f - e));
        }

        if (state == ELeviathanState.Buff)
        {
            UpdateBuffSlot(now);
        }

        else if (state == ELeviathanState.Finisher)
        {
            UpdateFinisherSlot(now);
        }
    }

    private float TagValue(float now)
    {
        float k = Mathf.Clamp01((now - _tagStart) / _TAG_TIME);
        return Mathf.Lerp(_tagFrom, _tagTo, CHudEase.Ease(k));
    }

    private void UpdateBuffSlot(float now)
    {
        int stack = _leviathan.Stack;
        if (stack > _shownStack)
        {
            _shownStack = stack;
            // 중첩이 늘면 숫자 확대와 섬광을 함께 재생한다.
            SetCount(stack.ToString(CultureInfo.InvariantCulture), stack >= _leviathan.MaxStack, _countSize, true, now);
            CTrack hit = AddTrack(now, 0.34f);
            hit.hits.Add(0.01f);
            hit.caAmp = 6f;
            hit.isCaHit = true;
            hit.isCaRed = true;
            hit.shake = 4f;
            CTrack flash = AddTrack(now, 0.25f);
            flash.hits.Add(0f);
            flash.white = 1f;
            CTrack ghost = AddTrack(now, 0.45f);
            ghost.hits.Add(0f);
            ghost.ghost = 1f;

            if (_leviathan.IsEnhanced)
            {
                // 최대 중첩에 도달한 뒤 보너스 타격 알림을 표시한다.
                _bonusAt = now + _bonusDelay;
            }
        }

        else if (stack < _shownStack)
        {
            _shownStack = stack;
        }

        if (_bonusAt >= 0f && now >= _bonusAt)
        {
            // 보너스 문구, 빛 번짐, 총 타격 수를 차례로 표시한다.
            _bonusTextStart = _bonusAt + 0.1f;
            _bloomStart = _bonusAt + 0.1f;
            _thirtyAt = _bonusAt + 0.5f;
            _bonusAt = -1f;
        }

        if (_thirtyAt >= 0f && now >= _thirtyAt)
        {
            _thirtyAt = -1f;
            SetCount(_leviathan.FinisherHitCount.ToString(CultureInfo.InvariantCulture), true, _countSize, true, now);
        }
    }

    private void UpdateFinisherSlot(float now)
    {
        int done = _leviathan.FinisherHitsDone;
        int total = _leviathan.FinisherHitCount;
        if (done == _shownHits) return;

        for (int i = _shownHits; i < done; i++)
        {
            if (_growTrack != null) _growTrack.hits.Add(now - _growTrack.start);
        }

        _shownHits = done;
        bool isLast = done >= total;
        // 남은 타격 수를 표시하고 마지막 타격은 큰 섬광으로 구분한다.
        SetCount((total - done + 1).ToString(CultureInfo.InvariantCulture), isLast, _finisherCountSize, false, now);
        if (isLast)
        {
            CTrack z8 = AddTrack(now, 0.5f);
            z8.hits.Add(0f);
            z8.white = 2.5f;
        }
    }

    // 아이콘 자체의 알파는 복구하고 그룹 알파만 적용해 투명도가 중복되지 않게 한다.
    // CSkillUI가 쓴 기본 알파를 이 뷰의 LateUpdate에서 덮는다.
    private void SetIconAlpha(float alpha)
    {
        _iconGroup.alpha = alpha;
        if (_iconImage == null) _iconImage = _iconGroup.GetComponent<Image>();
        if (_iconImage != null && _iconImage.color.a != 1f)
        {
            Color c = _iconImage.color;
            c.a = 1f;
            _iconImage.color = c;
        }
    }

    // 글자 크기에 비례한 여백을 적용하고 확대 기준점은 R 슬롯 중앙에 맞춘다.
    // 브라우저와 TMP의 글꼴 기준선 반올림 차이를 보정한다.

    private void ApplyCountBox(float size)
    {
        RectTransform rt = _countText.rectTransform;
        float padRight = 0.1f * size;
        float padTop = 0.07f * size;
        float dy = BaselineRound(size);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(0f, -dy);
        rt.offsetMax = new Vector2(-padRight, -padTop - dy);
        Vector2 slot = ((RectTransform)rt.parent).rect.size;
        Vector2 pivot = new Vector2(slot.x * 0.5f / (slot.x - padRight), (slot.y * 0.5f + dy) / (slot.y - padTop));
        Vector2 shift = Vector2.Scale(pivot - rt.pivot, rt.rect.size);
        rt.pivot = pivot;
        rt.anchoredPosition += shift;                                       // pivot 만 옮기고 상자는 그대로
    }

    /// <summary>
    /// Chrome 기준선(ascent · descent 정수 반올림)과 TMP 기준선의 차이(px, 화면 아래 +). Rajdhani ascent .93em · descent .346em.
    /// </summary>
    public static float BaselineRound(float size)
    {
        float a = 0.93f * size;
        float d = 0.346f * size;
        return (Mathf.Round(a) - Mathf.Round(d)) * 0.5f - (a - d) * 0.5f;
    }

    private void SetCount(string value, bool isMax, float size, bool isSlam, float now)
    {
        if (_countText == null) return;

        _countText.text = value;
        _countText.fontSize = size;
        ApplyCountBox(size);
        _countText.color = isMax ? _gold : Color.white;
        Material m = isMax ? _countMaxMaterial : _countMaterial;
        if (m != null && _countText.fontSharedMaterial != m) _countText.fontSharedMaterial = m;
        if (!_countText.gameObject.activeSelf) _countText.gameObject.SetActive(true);
        _countAnimStart = now;
        _isCountSlam = isSlam;
    }

    private void HideCount()
    {
        if (_countText != null) _countText.gameObject.SetActive(false);
        _countAnimStart = -1f;
    }

    private void UpdateCount(float now)
    {
        if (_countText == null || _countAnimStart < 0f) return;

        float age = now - _countAnimStart;
        RectTransform rt = _countText.rectTransform;
        if (_isCountSlam)
        {

            float k = CHudEase.Bezier(0.2f, 0.9f, 0.3f, 1.4f, Mathf.Clamp01(age / _SLAM_TIME));
            rt.localScale = Vector3.one * Mathf.LerpUnclamped(2.6f, 1f, k);
            _countText.alpha = Mathf.Clamp01(Mathf.LerpUnclamped(0.2f, 1f, k));
            if (age >= _SLAM_TIME) _countAnimStart = -1f;
        }

        else
        {

            float k = CHudEase.EaseOut(Mathf.Clamp01(age / _TICK_TIME));
            rt.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, k);
            _countText.alpha = 1f;
            if (age >= _TICK_TIME) _countAnimStart = -1f;
        }

        if (_countAnimStart < 0f)
        {
            rt.localScale = Vector3.one;
            _countText.alpha = 1f;
        }
    }

    private void UpdateBonus(float now)
    {
        if (_bonusText == null) return;

        float age = _bonusTextStart >= 0f ? now - _bonusTextStart : -1f;
        bool isBuff = _leviathan.State == ELeviathanState.Buff;
        if (age < 0f || age >= _RISE_TIME || !isBuff)
        {
            if (_bonusText.gameObject.activeSelf) _bonusText.gameObject.SetActive(false);
            if (age >= _RISE_TIME || !isBuff) _bonusTextStart = -1f;
            return;
        }

        // 보너스 문구는 아래에서 나타나 위로 올라가며 사라진다.
        float p = age / _RISE_TIME;
        float y;
        float scale;
        float alpha;
        if (p < 0.2f)
        {
            float k = CHudEase.EaseOut(p / 0.2f);
            y = Mathf.Lerp(16f, 0f, k);
            scale = Mathf.Lerp(0.7f, 1.08f, k);
            alpha = k;
        }

        else if (p < 0.7f)
        {
            float k = CHudEase.EaseOut((p - 0.2f) / 0.5f);
            y = Mathf.Lerp(0f, -14f, k);
            scale = Mathf.Lerp(1.08f, 1f, k);
            alpha = 1f;
        }

        else
        {
            float k = CHudEase.EaseOut((p - 0.7f) / 0.3f);
            y = Mathf.Lerp(-14f, -30f, k);
            scale = 1f;
            alpha = 1f - k;
        }

        if (!_bonusText.gameObject.activeSelf) _bonusText.gameObject.SetActive(true);
        _bonusText.rectTransform.anchoredPosition = new Vector2(0f, _bonusRestY - y);
        _bonusText.rectTransform.localScale = Vector3.one * scale;
        _bonusText.alpha = alpha;
    }

    private void SetState(float to, float duration, float now)
    {
        _stateFrom = CurrentStateWeight(now);
        _stateTo = to;
        _stateStart = now;
        _stateDuration = Mathf.Max(0.001f, duration);
    }

    private float CurrentStateWeight(float now)
    {
        float k = Mathf.Clamp01((now - _stateStart) / _stateDuration);
        return Mathf.Lerp(_stateFrom, _stateTo, CHudEase.Ease(k));
    }

    private void UpdateStateWeight(float now)
    {
        if (_stateOffAt >= 0f && now >= _stateOffAt)
        {
            _stateOffAt = -1f;
            SetState(0f, _stateOffTime, now);
        }

        // 슬롯, 아이콘, 키, 막대 색을 같은 속도로 전환한다.
        float w = CurrentStateWeight(now);
        if (_overlay != null)
        {
            _overlay.color = new Color(_crimson.r, _crimson.g, _crimson.b, _overlayAlpha * _overlayOpacity * w);
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null) _slots[i].CrimsonWeight = w;
        }

        for (int i = 0; i < _bars.Length; i++)
        {
            if (_bars[i] != null) _bars[i].CrimsonWeight = w;
        }
    }

    private CTrack AddTrack(float now, float duration)
    {
        CTrack t = new CTrack { start = now, duration = duration };
        _TRACKS.Add(t);
        return t;
    }

    private void EndGrow()
    {
        if (_growTrack == null) return;

        _TRACKS.Remove(_growTrack);
        _growTrack = null;
    }

    private static float Env(float t, float duration, float attack = 0.12f, float release = 0.5f)
    {
        return Mathf.Min(1f, t / attack) * (1f - Mathf.Clamp01((t - (duration - release)) / release));
    }

    private void UpdateTracks(float now)
    {
        // 각 연출의 시간 감쇠와 타격 반응을 합산한다.
        float ca = 0f;
        bool isRed = false;
        float shake = 0f;
        float white = 0f;
        float ghostAmount = 0f;
        float ghostEnv = 0f;
        bool hasGhost = false;
        float glitchOn = 0f;
        bool hasGlitch = false;

        for (int i = _TRACKS.Count - 1; i >= 0; i--)
        {
            CTrack tr = _TRACKS[i];
            float tt = now - tr.start;
            if (tt > tr.duration)
            {
                _TRACKS.RemoveAt(i);
                continue;
            }

            float e = Env(tt, tr.duration);
            float h = 0f;
            int k = 0;
            for (int j = 0; j < tr.hits.Count; j++)
            {
                if (tt < tr.hits[j]) continue;

                h += Mathf.Exp(-(tt - tr.hits[j]) * _HIT_DECAY);
                k++;
            }

            h = Mathf.Min(1.4f, h);
            if (tr.isGrow)
            {
                float g = (float)k / tr.growTotal;
                ca += (3f + 14f * g) * h;
                isRed = true;
                shake += (1f + 10f * g) * h;
            }

            if (tr.caAmp > 0f)
            {
                ca += tr.caAmp * (tr.isCaHit ? h : 0.3f * e + 0.9f * h);
                isRed |= tr.isCaRed;
            }

            if (tr.shake > 0f) shake += tr.shake * (0.25f * e + h);
            if (tr.white > 0f) white += tr.white * (1.4f * Mathf.Max(0f, 1f - tt / 0.18f) + 0.45f * h);
            if (tr.ghost > 0f)
            {
                hasGhost = true;
                ghostAmount = Mathf.Max(ghostAmount, h * tr.ghost);
                ghostEnv = Mathf.Max(ghostEnv, e);
            }

            if (tr.glitch > 0f)
            {
                hasGlitch = true;
                glitchOn = Mathf.Max(glitchOn, tr.glitch * (0.3f * e + h));
            }
        }

        if (_groupFx == null) return;

        SHudGroupState s = SHudGroupState.Idle;
        s.brightness = 1f + white;
        s.ca = ca;
        s.isRed = isRed;
            s.shake = shake > 0.05f
                ? new Vector2(Random.Range(-1f, 1f) * shake, Random.Range(-1f, 1f) * shake * 0.7f)
                : Vector2.zero;
        if (hasGhost)
        {
            // R 슬롯을 중심으로 잔상 세 장의 크기와 투명도를 달리한다.
            s.hasGhost = true;
            float step = (0.04f + 0.05f * ghostAmount) * ghostEnv;
            s.ghostScale = new Vector3(1f + step, 1f + 2f * step, 1f + 3f * step);
            s.ghostAlpha = new Vector3(0.315f, 0.21f, 0.105f);
        }

        if (hasGlitch)
        {
            s.glitch0 = GlitchBand(glitchOn, 1f);
            s.glitch1 = GlitchBand(glitchOn, -1f);
        }

        if (_bloomStart >= 0f)
        {
            float lt = now - _bloomStart;
            if (lt > 0.7f)
            {
                _bloomStart = -1f;
            }

            else if (lt >= 0f)
            {
                s.bloomAmp = 0.9f * Env(lt, 0.7f, 0.1f, 0.28f);
                s.bloomTime = lt;
            }
        }

        _groupFx.SetState(s);
    }

    private static Vector4 GlitchBand(float on, float side)
    {
        // 수평 띠의 위치와 밝기를 무작위로 바꿔 화면 흔들림을 보완한다.
        if (Random.value >= 0.55f * on + 0.08f) return Vector4.zero;

        float y0 = Random.value * 90f;
        float y1 = y0 + 4f + Random.value * 22f;
        float dx = side * (4f + Random.value * 18f) * on;
        return new Vector4(y0 / 100f, Mathf.Min(1f, y1 / 100f), dx, Mathf.Min(0.85f, 0.3f + on));
    }

    private void HideEffects()
    {
        HideCount();
        if (_bonusText != null) _bonusText.gameObject.SetActive(false);
        if (_ultTag != null) _ultTag.alpha = 0f;
        if (_overlay != null) _overlay.color = Color.clear;
    }
}
