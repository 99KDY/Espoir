using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public enum ESceneId
{
    // 0 ~ 9 : 코어
    Title = 0,
    Prologue = 1,
    Epilogue = 9,
    // 10 ~ 19 : 마을
    Lobby = 10,
    // 20 ~ 29 : 전투
    BattlePigritia = 20
}

[Serializable]
public class SceneEntry
{
    public ESceneId Id;
    public string SceneName;
}

public partial class SceneFlowManager : MonoBehaviour
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
    private static SceneFlowManager _instance;
    private int _targetSaveData = -1;
    private bool _loadSavedData = false;
    //private float _bgmLevel = 1f;
    //private float _seLevel = 1f;
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

    //public (float bgmLevel, float seLevel) KeepVolume
    //{
    //    get { return (_bgmLevel, _seLevel); }
    //    set
    //    {
    //        _bgmLevel = value.bgmLevel;
    //        _seLevel = value.seLevel;
    //    }
    //}

    public static SceneFlowManager Instance
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
        op.allowSceneActivation = false;
        while (op.progress < 0.9f)
        {
            yield return null;
        }
        op.allowSceneActivation = true;

        yield return null;
        // StartCoroutine(Co_FadeTo(0f, _fadeDuration));
        SyncCursorToCurrentScene();

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
