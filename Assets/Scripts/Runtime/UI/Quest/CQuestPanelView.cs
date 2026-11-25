using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니맵 아래에 현재 퀘스트 목표와 진행 수량을 표시합니다.
/// </summary>
public class CQuestPanelView : MonoBehaviour
{
    private class CBlock
    {
        public GameObject root;
        public CHudDiamond icon;
        public TMP_Text title;
        public readonly List<CRow> rows = new List<CRow>();
    }

    private class CRow
    {
        public GameObject root;
        public CHudDiamond diamond;
        public Image flash;
        public RectTransform sweep;
        public Image sweepGlow;
        public TMP_Text text;
        public TMP_Text progress;
        public TMP_Text hint;
        public bool isCurrent;
        public float doneTime = -1f;
        public float tickTime = -1f;
    }

    private struct SShown
    {
        public string questId;
        public int index;
        public int value;
        public EQuestState state;
    }

    [Header("연결")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private RectTransform _blockRoot;
    [SerializeField] private GameObject _blockTemplate;
    [SerializeField] private GameObject _rowTemplate;

    [Header("표시")]
    [SerializeField, Min(1)] private int _maxQuests = 2;
    [Tooltip("완료한 목표의 취소선 표시 시간(초)")]
    [SerializeField, Min(0f)] private float _doneHoldTime = 1.2f;

    private static readonly Color _AMBER = new Color(1f, 192f / 255f, 112f / 255f);
    private static readonly Color _AMBER_LIGHT = new Color(1f, 217f / 255f, 160f / 255f);
    private static readonly Color _TEXT = new Color(1f, 240f / 255f, 220f / 255f);
    private static readonly Color _TEXT_CURRENT = new Color(1f, 246f / 255f, 232f / 255f);
    private static readonly Color _TEXT_DONE = new Color(1f, 240f / 255f, 220f / 255f, 0.45f);
    private static readonly Color _HINT = new Color(1f, 228f / 255f, 196f / 255f, 0.62f);
    private static readonly Color _HINT_CURRENT = new Color(1f, 228f / 255f, 196f / 255f, 0.85f);
    private static readonly Color _PROGRESS = new Color(1f, 240f / 255f, 220f / 255f, 0.75f);
    private static readonly Color _DIAMOND_BORDER = new Color(1f, 240f / 255f, 220f / 255f, 0.7f);
    private const float _BREATH_PERIOD = 1.6f;
    private const float _SWEEP_PERIOD = 3.2f;
    private const float _SWEEP_ALPHA = 0.22f;
    private const float _FLASH_ALPHA = 0.32f;
    private const float _DONE_TIME = 1f;
    private const float _TICK_TIME = 0.5f;
    private const float _IN_TIME = 0.45f;
    private const float _OK_TIME = 0.9f;

    private readonly List<CBlock> _BLOCKS = new List<CBlock>();
    private readonly List<CQuestSO> _TRACKED = new List<CQuestSO>();
    private readonly List<SShown> _SHOWN = new List<SShown>();
    private int _version = -1;
    private float _inTime = -1f;
    private float _okTime = -1f;
    private RectTransform _panelRect;
    private float _panelX;
    private CQuestSO _offer;
    private string _offerText;
    private string _offerHint;

    /// <summary>
    /// 수락 전까지 패널에 표시할 안내를 설정합니다.
    /// </summary>
    public void SetOffer(CQuestSO quest, string text, string hint)
    {
        if (_offer == quest && _offerText == text && _offerHint == hint) return;

        _offer = quest;
        _offerText = text;
        _offerHint = hint;
        _version = -1;
    }

    private void Awake()
    {
        _panelRect = _panel.GetComponent<RectTransform>();
        _panelX = _panelRect.anchoredPosition.x;
        _blockTemplate.SetActive(false);
        _rowTemplate.SetActive(false);
        _panel.SetActive(false);
    }

    private void LateUpdate()
    {
        float now = Time.unscaledTime;
        if (_version != CQuestManager.Version || HasLiveChange()) Rebuild(now);
        if (!_panel.activeSelf) return;

        Animate(now);
    }

    private bool HasLiveChange()
    {
        for (int i = 0; i < _SHOWN.Count; i++)
        {
            SShown shown = _SHOWN[i];
            if (shown.state != EQuestState.Active || i >= _TRACKED.Count) continue;
            if (CQuestManager.GetLiveProgress(_TRACKED[i]) != shown.value) return true;
        }

        return false;
    }

    private void Rebuild(float now)
    {
        _version = CQuestManager.Version;
        CQuestManager.GetTracked(_TRACKED);
        if (_offer != null && CQuestManager.IsReady && CQuestManager.GetState(_offer) == EQuestState.None)
        {
            _TRACKED.Insert(0, _offer);
        }
        if (_TRACKED.Count > _maxQuests) _TRACKED.RemoveRange(_maxQuests, _TRACKED.Count - _maxQuests);

        bool wasVisible = _panel.activeSelf;
        bool hasNewContent = false;
        _panel.SetActive(_TRACKED.Count > 0);

        for (int i = 0; i < _TRACKED.Count; i++)
        {
            CQuestSO quest = _TRACKED[i];
            CBlock block = GetBlock(i);
            block.root.SetActive(true);
            block.title.text = quest.Title;

            EQuestState state = CQuestManager.GetState(quest);
            CQuestManager.TryGetCurrent(quest, out int index, out _);
            int value = CQuestManager.GetLiveProgress(quest);
            SShown previous = FindShown(quest.QuestId);
            bool isSameQuest = previous.questId == quest.QuestId;
            bool hasAdvanced = isSameQuest && (previous.index != index || previous.state != state);

            if (!isSameQuest) hasNewContent = true;
            if (hasAdvanced) _okTime = now;

            int rowCount = 0;

            if (hasAdvanced && previous.state == EQuestState.Active)
            {
                CQuestObjective done = quest.GetObjective(previous.index);
                if (done != null) SetRow(GetRow(block, rowCount++), done.text, null, string.Empty, false, true, now);
            }
            else if (isSameQuest && block.rows.Count > 0 && block.rows[0].doneTime >= 0f &&
                now - block.rows[0].doneTime < _doneHoldTime)
            {
                rowCount++;
            }

            if (state == EQuestState.Active)
            {
                CQuestObjective objective = quest.GetObjective(index);
                if (objective != null)
                {
                    int target = GetProgressTarget(objective);
                    string progress = target > 0
                        ? $"<color=#FFC070><b>{Mathf.Min(value, target)}</b></color>/{target}" : string.Empty;
                    CRow row = GetRow(block, rowCount++);
                    bool isTick = isSameQuest && !hasAdvanced && value > previous.value;
                    SetRow(row, objective.text, objective.hint, progress, true, false, now);
                    if (isTick) row.tickTime = now;
                }
            }
            else if (state == EQuestState.Reportable)
            {
                SetRow(GetRow(block, rowCount++), quest.ReportText, null, string.Empty, true, false, now);
            }
            else if (state == EQuestState.None)
            {
                SetRow(GetRow(block, rowCount++), _offerText, _offerHint, string.Empty, true, false, now);
            }

            for (int r = rowCount; r < block.rows.Count; r++)
            {
                block.rows[r].root.SetActive(false);
            }

            SetShown(i, new SShown { questId = quest.QuestId, index = index, value = value, state = state });
        }

        for (int i = _TRACKED.Count; i < _BLOCKS.Count; i++)
        {
            _BLOCKS[i].root.SetActive(false);
        }

        if (_SHOWN.Count > _TRACKED.Count) _SHOWN.RemoveRange(_TRACKED.Count, _SHOWN.Count - _TRACKED.Count);
        if (_panel.activeSelf && (!wasVisible || hasNewContent)) _inTime = now;
    }

    private static int GetProgressTarget(CQuestObjective objective)
    {
        switch (objective.kind)
        {
            case EQuestObjectiveKind.Kill:
                return objective.RequiredCount;
            case EQuestObjectiveKind.StatusStack:
                return objective.stack;
            case EQuestObjectiveKind.LeviathanFinisher:
                return objective.requireFullStack ? CQuestManager.MaxFinisherStack : 0;
            default:
                return objective.RequiredCount > 1 ? objective.RequiredCount : 0;
        }
    }

    private void SetRow(CRow row, string text, string hint, string progress, bool isCurrent, bool isDone, float now)
    {
        row.root.SetActive(true);
        row.text.text = isDone ? $"<s>{text}</s>" : text;
        row.text.color = isDone ? _TEXT_DONE : isCurrent ? _TEXT_CURRENT : _TEXT;
        row.progress.text = progress;
        row.progress.gameObject.SetActive(!string.IsNullOrEmpty(progress));
        row.hint.text = hint ?? string.Empty;
        row.hint.color = isCurrent ? _HINT_CURRENT : _HINT;
        row.hint.gameObject.SetActive(!string.IsNullOrEmpty(hint));
        row.isCurrent = isCurrent && !isDone;
        row.doneTime = isDone ? now : -1f;
        row.tickTime = -1f;
        row.sweep.gameObject.SetActive(row.isCurrent);

        row.diamond.Fill = isDone || isCurrent ? _AMBER : Color.clear;
        row.diamond.BorderColor = isDone ? _AMBER : isCurrent ? _AMBER_LIGHT : _DIAMOND_BORDER;
    }

    private void Animate(float now)
    {
        float inK = _inTime >= 0f ? Mathf.Clamp01((now - _inTime) / _IN_TIME) : 1f;
        float inE = CHudEase.Bezier(0.2f, 0.8f, 0.2f, 1f, inK);
        _panelRect.anchoredPosition = new Vector2(_panelX + 14f * (1f - inE), _panelRect.anchoredPosition.y);

        float breath = 0.5f - 0.5f * Mathf.Cos(now / _BREATH_PERIOD * Mathf.PI * 2f);
        float sweepT = Mathf.Repeat(now, _SWEEP_PERIOD) / _SWEEP_PERIOD;
        float sweepK = Mathf.Clamp01(sweepT / 0.6f);
        float sweepE = CHudEase.EaseInOut(sweepK);
        float okK = _okTime >= 0f ? Mathf.Clamp01((now - _okTime) / _OK_TIME) : 1f;

        foreach (CBlock block in _BLOCKS)
        {
            if (!block.root.activeSelf) continue;

            float okE = 1f - okK;
            block.icon.rectTransform.localScale = Vector3.one * (1f + 0.8f * okE * okE);
            block.icon.SetShadow(8f + 14f * (1f - okE), new Color(_AMBER.r, _AMBER.g, _AMBER.b, 0.7f + 0.2f * okE));

            foreach (CRow row in block.rows)
            {
                if (!row.root.activeSelf) continue;

                if (row.isCurrent)
                {
                    Color borderGlow = _AMBER;
                    borderGlow.a = 0.45f + 0.55f * breath;
                    row.diamond.SetShadow(3f + 15f * breath, borderGlow);
                    row.diamond.color = new Color(1f, 1f, 1f, 0.75f + 0.25f * breath);

                    RectTransform rect = (RectTransform)row.root.transform;
                    float width = rect.rect.width;
                    row.sweep.anchoredPosition = new Vector2(sweepE * width * 0.55f, 0f);
                    Color glow = _AMBER;
                    glow.a = _SWEEP_ALPHA * Mathf.Sin(sweepK * Mathf.PI);
                    row.sweepGlow.color = glow;
                }

                float doneK = row.doneTime >= 0f ? Mathf.Clamp01((now - row.doneTime) / _DONE_TIME) : 1f;
                Color flash = _AMBER;
                flash.a = _FLASH_ALPHA * (1f - doneK);
                row.flash.color = flash;
                if (row.doneTime >= 0f && !row.isCurrent)
                {
                    float popE = 1f - Mathf.Clamp01((now - row.doneTime) / 0.7f);
                    row.diamond.SetShadow(16f * popE, new Color(_AMBER.r, _AMBER.g, _AMBER.b, popE));
                    if (now - row.doneTime > _doneHoldTime) row.root.SetActive(false);
                }

                float tickK = row.tickTime >= 0f ? Mathf.Clamp01((now - row.tickTime) / _TICK_TIME) : 1f;
                float tickE = CHudEase.Bezier(0.2f, 0.9f, 0.3f, 1.4f, tickK);
                row.progress.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(1.6f, 1f, tickE);
            }
        }
    }

    private SShown FindShown(string questId)
    {
        foreach (SShown shown in _SHOWN)
        {
            if (shown.questId == questId) return shown;
        }

        return new SShown { index = -1 };
    }

    private void SetShown(int index, SShown shown)
    {
        while (_SHOWN.Count <= index) _SHOWN.Add(default);
        _SHOWN[index] = shown;
    }

    private CBlock GetBlock(int index)
    {
        while (_BLOCKS.Count <= index)
        {
            GameObject root = Instantiate(_blockTemplate, _blockRoot);
            root.name = "Quest" + _BLOCKS.Count;
            _BLOCKS.Add(new CBlock
            {
                root = root,
                icon = root.transform.Find("Header/Icon").GetComponent<CHudDiamond>(),
                title = root.transform.Find("Header/Title").GetComponent<TMP_Text>(),
            });
        }

        return _BLOCKS[index];
    }

    private CRow GetRow(CBlock block, int index)
    {
        while (block.rows.Count <= index)
        {
            Transform rows = block.root.transform.Find("Rows");
            GameObject root = Instantiate(_rowTemplate, rows);
            root.name = "Row" + block.rows.Count;
            block.rows.Add(new CRow
            {
                root = root,
                diamond = root.transform.Find("Diamond").GetComponent<CHudDiamond>(),
                flash = root.transform.Find("Flash").GetComponent<Image>(),
                sweep = (RectTransform)root.transform.Find("Sweep"),
                sweepGlow = root.transform.Find("Sweep/Glow").GetComponent<Image>(),
                text = root.transform.Find("Line/Text").GetComponent<TMP_Text>(),
                progress = root.transform.Find("Line/Progress").GetComponent<TMP_Text>(),
                hint = root.transform.Find("Hint").GetComponent<TMP_Text>(),
            });
        }

        return block.rows[index];
    }
}
