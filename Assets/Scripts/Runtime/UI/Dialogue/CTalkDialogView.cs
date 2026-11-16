using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 화면 하단에 NPC 이름, 순차 출력되는 대사와 선택지를 배치합니다.
/// 띠 위 지난 대사 두 줄, 오른쪽 선택지 칸, 왼쪽 아래 조작 안내(F · 마우스 다음 / 마우스 선택 / Esc 닫기).
/// 화면을 누르면 다음 대사로 진행합니다.
/// </summary>
public class CTalkDialogView : MonoBehaviour, IPointerClickHandler
{
    private static readonly Vector4 _LOG_EASE = new Vector4(0.2f, 0.8f, 0.2f, 1f);
    private static readonly Color _KEY_BG = new Color(8f / 255f, 10f / 255f, 18f / 255f, 0.4f);
    private static readonly Color _KEY_BLINK_TEXT = new Color(20f / 255f, 21f / 255f, 27f / 255f, 1f);
    private static readonly Color _GOLD = new Color(234f / 255f, 223f / 255f, 180f / 255f, 1f);

    [Header("띠")]
    [SerializeField] private RectTransform _bar;
    [SerializeField] private CanvasGroup _barGroup;
    [SerializeField] private RectTransform _lin;

    [Header("이름 칸")]
    [SerializeField] private TMP_Text _name;
    [SerializeField] private TMP_Text _title;

    [Header("대사")]
    [SerializeField] private TMP_Text _text;

    [Header("지난 대사 (띠 위 두 줄)")]
    [SerializeField] private RectTransform _log;
    [SerializeField] private RectTransform _logBorder;
    [SerializeField] private TMP_Text[] _logLines;

    [Header("조작 안내")]
    [SerializeField] private RectTransform _keyNext;
    [SerializeField] private CanvasGroup _keyNextGroup;
    [SerializeField] private Image _keyNextFill;
    [SerializeField] private TMP_Text _keyNextLetter;
    [SerializeField] private RectTransform _keySelect;
    [SerializeField] private RectTransform _keyClose;

    [Header("선택지")]
    [SerializeField] private RectTransform _choices;
    [SerializeField] private CTalkChoiceView _choiceTemplate;

    [Header("웹 값 (px, 1080 기준)")]
    [SerializeField] private float _barHeight = 252f;
    [SerializeField] private float _maxLineWidth = 1500f;
    [SerializeField] private float _sideMargin = 120f;
    [SerializeField] private float _textLeft = 264f;
    [SerializeField] private float _choicesWidth = 450f;
    [SerializeField] private float _choicesGap = 44f;
    [SerializeField] private float _textMinHeight = 126f;
    [SerializeField] private float _textLineHeight = 44.2f;
    [SerializeField] private float _logRight = 500f;
    [SerializeField] private float _logLineHeight = 28.5f;
    [SerializeField] private float _logLineGap = 6f;
    [SerializeField] private float _logPaddingLeft = 18f;
    [SerializeField] private float _choiceRowHeight = 62f;
    [SerializeField] private float _choicesMargin = 18f;

    private readonly List<CTalkChoiceView> _ROWS = new List<CTalkChoiceView>();
    private readonly List<string> _LOG_TEXTS = new List<string>();
    private readonly List<bool> _LOG_IS_MINE = new List<bool>();
    private float[] _logTops = new float[0];
    private float _openTime;
    private float _logTime;
    private float _waitStart;
    private bool _isWaiting;
    private bool _isPicking;
    private int _shown;
    private float _lineWidth;

    /// <summary>
    /// 대화창(빈 곳 포함)을 눌렀을 때.
    /// </summary>
    public event Action AdvanceRequested;

    /// <summary>
    /// 선택지를 눌렀을 때 (0부터).
    /// </summary>
    public event Action<int> ChoiceRequested;

    public bool IsOpen => gameObject.activeSelf;

    public IReadOnlyList<CTalkChoiceView> Rows => _ROWS;

    private void Awake()
    {
        if (_choiceTemplate != null) _choiceTemplate.gameObject.SetActive(false);
    }

    /// <summary>
    /// 대화창을 엽니다 (dlg-in .45s: 아래 18px 에서 떠오르며 나타남).
    /// </summary>
    public void Open(string npcName, string npcTitle, float now)
    {
        gameObject.SetActive(true);
        _name.text = npcName;
        _title.text = npcTitle;
        _openTime = now;
        _LOG_TEXTS.Clear();
        _LOG_IS_MINE.Clear();
        _text.text = string.Empty;
        _isWaiting = false;
        _isPicking = false;
        HideChoices();
        Layout();
        RebuildLog(now);
        Tick(now);
    }

    /// <summary>
    /// 대화창을 즉시 닫습니다.
    /// </summary>
    public void Close()
    {
        HideChoices();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 새 대사를 시작합니다. 지난 대사 목록(최근 두 개)은 다시 그려지며 위로 밀려 올라갑니다.
    /// </summary>
    public void ShowLine(string text, IReadOnlyList<string> history, IReadOnlyList<bool> historyIsMine, float now)
    {
        _text.text = text;
        _shown = 0;
        _text.maxVisibleCharacters = 0;
        _LOG_TEXTS.Clear();
        _LOG_IS_MINE.Clear();

        int start = Mathf.Max(0, history.Count - _logLines.Length);

        for (int i = start; i < history.Count; i++)
        {
            _LOG_TEXTS.Add(history[i]);
            _LOG_IS_MINE.Add(historyIsMine[i]);
        }

        Layout();
        RebuildLog(now);
    }

    /// <summary>
    /// 지금까지 나온 글자 수와 상태(기다림 = 다 나오고 선택지 없음, 고름 = 선택지 표시)를 넣습니다.
    /// </summary>
    public void SetProgress(int shown, bool isWaiting, bool isPicking, IReadOnlyList<CTalkChoice> choices, float now)
    {
        _shown = shown;
        _text.maxVisibleCharacters = shown;

        if (isWaiting && !_isWaiting) _waitStart = now;

        _isWaiting = isWaiting;

        if (isPicking != _isPicking)
        {
            _isPicking = isPicking;

            if (isPicking) ShowChoices(choices, now);
            else HideChoices();

            Layout();
        }
    }

    /// <summary>
    /// 매 프레임 애니메이션 값을 반영합니다.
    /// </summary>
    public void Tick(float now)
    {
        if (!gameObject.activeSelf) return;

        // dlg-in .45s ease: opacity 0 → 1, translateY 18 → 0
        float k = CCssEase.Evaluate(CCssEase.EASE, Mathf.Clamp01((now - _openTime) / 0.45f));
        _barGroup.alpha = k;
        _bar.anchoredPosition = new Vector2(0f, -(1f - k) * 18f);

        // 지난 대사: 마지막 줄 g4-rise(.5s: opacity 0, 16px 아래에서), 그 앞 줄 g4-push(.5s: 30px 아래에서)
        float logK = CCssEase.Evaluate(_LOG_EASE, Mathf.Clamp01((now - _logTime) / 0.5f));
        int count = _LOG_TEXTS.Count;

        for (int i = 0; i < _logLines.Length; i++)
        {
            if (i >= count || i >= _logTops.Length) continue;

            bool isLast = i == count - 1;
            RectTransform line = _logLines[i].rectTransform;
            float baseY = _logTops[i];
            float offset = (1f - logK) * (isLast ? 16f : 30f);
            line.anchoredPosition = new Vector2(_logPaddingLeft + 1f, -baseY - offset);
            float opacity = isLast ? 0.72f * logK : (count >= 2 && i == count - 2 ? 0.36f : 0.72f);
            _logLines[i].alpha = opacity;
        }

        // F 키 깜빡임 (g4-key 1s ease-in-out 무한, 50% = 흰 바탕 · 짙은 글자) — 기다림 상태에서만
        _keyNextGroup.alpha = _isWaiting ? 1f : 0.78f;

        if (_isWaiting)
        {
            float phase = Mathf.Repeat(now - _waitStart, 1f);
            float blink = phase < 0.5f
                ? CCssEase.Evaluate(CCssEase.EASE_IN_OUT, phase / 0.5f)
                : 1f - CCssEase.Evaluate(CCssEase.EASE_IN_OUT, (phase - 0.5f) / 0.5f);
            _keyNextFill.color = LerpPremultiplied(_KEY_BG, Color.white, blink);
            _keyNextLetter.color = Color.Lerp(Color.white, _KEY_BLINK_TEXT, blink);
        }

        else
        {
            _keyNextFill.color = _KEY_BG;
            _keyNextLetter.color = Color.white;
        }

        foreach (CTalkChoiceView row in _ROWS)
        {
            if (row.gameObject.activeSelf) row.Tick(now);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        AdvanceRequested?.Invoke();
    }

    // 띠 안 배치: .lin = 가운데 min(1500, 화면 - 120), 대사 = 왼쪽 264 부터 (고를 때는 오른쪽 450 + 사이 44 를 비움)
    private void Layout()
    {
        RectTransform root = (RectTransform)transform;
        float screenWidth = root.rect.width;
        _lineWidth = Mathf.Min(_maxLineWidth, screenWidth - _sideMargin);
        _lin.sizeDelta = new Vector2(_lineWidth, _barHeight - 1f);

        float textWidth = _lineWidth - _textLeft - (_isPicking ? _choicesWidth + _choicesGap : 0f);
        RectTransform text = _text.rectTransform;
        text.sizeDelta = new Vector2(textWidth, _textMinHeight);
        _text.ForceMeshUpdate();
        int lines = Mathf.Max(1, _text.textInfo.lineCount);
        float textHeight = Mathf.Max(_textMinHeight, lines * _textLineHeight);
        text.sizeDelta = new Vector2(textWidth, textHeight);
        text.anchoredPosition = new Vector2(_textLeft, -((_barHeight - 1f) - textHeight) * 0.5f);

        _choices.gameObject.SetActive(_isPicking);
        _choices.anchoredPosition = new Vector2(_lineWidth - _choicesWidth, -_choicesMargin);
        _choices.sizeDelta = new Vector2(_choicesWidth, _barHeight - 1f - _choicesMargin * 2f);

        _log.sizeDelta = new Vector2(_lineWidth - _textLeft - _logRight, _log.sizeDelta.y);

        // 조작 안내: 기다림/글자 중 = [F · 마우스 다음] [Esc 닫기], 고름 = [마우스 선택] [Esc 닫기] (사이 24)
        _keyNext.gameObject.SetActive(!_isPicking);
        _keySelect.gameObject.SetActive(_isPicking);
        RectTransform first = _isPicking ? _keySelect : _keyNext;
        first.anchoredPosition = new Vector2(0f, 0f);
        _keyClose.anchoredPosition = new Vector2(first.sizeDelta.x + 24f, 0f);
    }

    private void RebuildLog(float now)
    {
        _logTime = now;
        if (_logTops.Length != _logLines.Length) _logTops = new float[_logLines.Length];

        float y = 0f;
        float lineWidth = _log.sizeDelta.x - _logPaddingLeft - 1f;

        for (int i = 0; i < _logLines.Length; i++)
        {
            TMP_Text line = _logLines[i];

            if (i >= _LOG_TEXTS.Count)
            {
                line.gameObject.SetActive(false);
                continue;
            }

            line.gameObject.SetActive(true);
            line.text = _LOG_IS_MINE[i] ? "— " + _LOG_TEXTS[i] : _LOG_TEXTS[i];
            line.color = _LOG_IS_MINE[i] ? _GOLD : Color.white;
            line.rectTransform.sizeDelta = new Vector2(lineWidth, _logLineHeight);
            line.ForceMeshUpdate();
            float height = Mathf.Max(1, line.textInfo.lineCount) * _logLineHeight;
            line.rectTransform.sizeDelta = new Vector2(lineWidth, height);
            y += _logLineGap;
            _logTops[i] = y;
            y += height;
        }

        // .lg 의 아랫변은 띠 윗변 16px 위에 고정, 내용이 많으면 위로 늘어남
        _log.sizeDelta = new Vector2(_log.sizeDelta.x, y);
        _logBorder.gameObject.SetActive(y > 0f);
        Tick(now);
    }

    private void ShowChoices(IReadOnlyList<CTalkChoice> choices, float now)
    {
        HideChoices();

        if (choices == null) return;

        int count = choices.Count;
        float available = _barHeight - 1f - _choicesMargin * 2f;
        // 줄 높이 62, 넘치면 같은 비율로 줄어듦 (flex-shrink), 칸 가운데 정렬
        float height = Mathf.Min(_choiceRowHeight, available / Mathf.Max(1, count));
        float top = (available - height * count) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            CTalkChoiceView row = GetRow(i);
            row.Show(i, choices[i].text, top + height * i, _choicesWidth - 1f, height, i == count - 1, now);
        }
    }

    private void HideChoices()
    {
        foreach (CTalkChoiceView row in _ROWS)
        {
            row.Hide();
        }
    }

    private CTalkChoiceView GetRow(int index)
    {
        while (_ROWS.Count <= index)
        {
            CTalkChoiceView row = Instantiate(_choiceTemplate, _choiceTemplate.transform.parent);
            row.name = "Choice" + (_ROWS.Count + 1);
            row.Chosen += OnChosen;
            _ROWS.Add(row);
        }

        return _ROWS[index];
    }

    private void OnChosen(int index)
    {
        ChoiceRequested?.Invoke(index);
    }

    private static Color LerpPremultiplied(Color a, Color b, float t)
    {
        float alpha = Mathf.Lerp(a.a, b.a, t);
        if (alpha <= 1e-5f) return new Color(0f, 0f, 0f, 0f);

        Color premultiplied = Color.Lerp(new Color(a.r * a.a, a.g * a.a, a.b * a.a),
            new Color(b.r * b.a, b.g * b.a, b.b * b.a), t);
        return new Color(premultiplied.r / alpha, premultiplied.g / alpha, premultiplied.b / alpha, alpha);
    }
}
