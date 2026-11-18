using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public enum ESceneId
{
    Title = 0,
    PradonaCamp = 1,
    Desert = 30,
    Garden = 31,
    Tower = 32
}

[Serializable]
public class SceneEntry
{
    public ESceneId Id;
    public string SceneName;
}

public partial class CSceneFlowManager : MonoBehaviour
{
    #region Inspector
    [Header("씬 카탈로그")]
    [SerializeField] private List<SceneEntry> _scenes = new List<SceneEntry>();
    [Header("옵션 - 전환")]
    [SerializeField] private float _fadeDuration = 1f;
    [Header("페이드")]
    [SerializeField] private CanvasGroup _fadeGroup;
    [Header("로딩 텍스트")]
    [SerializeField] private TMP_Text _loadingText;
    #endregion

    #region Member Variable
    private static CSceneFlowManager _instance;
    private int _targetSaveData = -1;
    private bool _loadSavedData = false;
    private bool _firstLoadFromTitle = false;
    private int _cursorIndex = 0;
    private readonly Dictionary<ESceneId, string> _idToName = new Dictionary<ESceneId, string>();
    private readonly Dictionary<string, ESceneId> _nameToId = new Dictionary<string, ESceneId>();
    public IReadOnlyList<SceneEntry> Entries => _scenes;
    private Coroutine _fadeRoutine;
    #endregion

    public int TargetSaveData
    {
        get { return _targetSaveData; }
        set { _targetSaveData = value; }
    }

    public bool LoadSavedData
    {
        get { return _loadSavedData; }
        set { _loadSavedData = value; }
    }

    public bool FirstLoadFromTitle
    {
        get { return _firstLoadFromTitle; }
        set { _firstLoadFromTitle = value; }
    }

    public static CSceneFlowManager Instance
    {
        get { return _instance; }
        protected set { _instance = value; }
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this.gameObject);
        BuildMaps();
        SyncCursorToCurrentScene();
    }

    private void Start()
    {
        TransitionInitialize();
    }

    private void SyncCursorToCurrentScene()
    {
        List<SceneEntry> entries = new List<SceneEntry>(Entries);

        if (entries == null || entries.Count == 0)
        {
            return;
        }

        string currentName = SceneManager.GetActiveScene().name;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].SceneName == currentName)
            {
                _cursorIndex = i;
                return;
            }
        }
        _cursorIndex = 0;
    }

    public void LoadScene(ESceneId id)
    {
        if (TryGetSceneName(id, out string sceneName) == false)
        {
            CLogger.Error($"씬 불러오기 실패 : {id}");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }
        StartCoroutine(Co_LoadSceneWithTransition(id, sceneName));
    }

    public IEnumerator LoadingScreenOn(float ratio = 1f, bool showLoading = true)
    {
        if (showLoading) _loadingText.gameObject.SetActive(true);
        else _loadingText.gameObject.SetActive(false);
        _fadeGroup.blocksRaycasts = true;
        yield return StartCoroutine(Co_FadeTo(1f, _fadeDuration * ratio, true));
    }

    public IEnumerator LoadingScreenOff(float ratio = 1f, float delay = 0f)
    {
        _loadingText.gameObject.SetActive(false);
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(Co_FadeTo(0f, _fadeDuration * ratio, true));
        _fadeGroup.blocksRaycasts = false;
    }

    private IEnumerator Co_LoadSceneWithTransition(ESceneId id, string sceneName)
    {
        CLogger.Log($"Transition : id = {id} / sceneName = {sceneName}");
        yield return StartCoroutine(Co_FadeTo(1f, _fadeDuration));

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        _loadingText.gameObject.SetActive(true);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f)
        {
            int percent = Mathf.FloorToInt(op.progress / 0.9f * 100f);
            _loadingText.text = $"맵 불러오는 중… {percent}%";
            yield return null;
        }
        _loadingText.text = "씬 준비 중…";
        CaptureLeavingPlayer();
        op.allowSceneActivation = true;
        while (op.isDone == false)
        {
            yield return null;
        }

        yield return null;
        SyncCursorToCurrentScene();
        _loadingText.gameObject.SetActive(false);
        _loadingText.text = "";
        if (_isTraveling)
        {
            PlaceArrivedPlayer(id);
        }
        if (id == ESceneId.Title)
        {
            Time.timeScale = 1f;
        }

        yield return StartCoroutine(Co_FadeTo(0f, _fadeDuration));

        CLogger.Success($"씬 로드 -> {sceneName}");
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
