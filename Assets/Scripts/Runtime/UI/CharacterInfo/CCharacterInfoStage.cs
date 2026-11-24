using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 정보창의 모델, 카메라와 연출을 관리합니다.
/// </summary>
public partial class CCharacterInfoStage : MonoBehaviour
{
    private const int _HELPER_TAB = 3;
    private const int _POSE_COUNT = 3;
    private const float _MAX_DELTA_TIME = 0.1f;
    private const string _SETTINGS_PATH = "Prefabs/UI/CharacterInfo/CharacterInfoStageSettings";

    [Tooltip("미지정 시 기본 설정을 불러옵니다.")]
    [SerializeField] private CCharacterInfoStageSettingsSO _settings;

    private readonly List<Renderer> _HIDDEN_RENDERERS = new List<Renderer>();
    private bool _isOpen;
    private bool _isClosing;
    private int _tab;
    private float _time;
    private float _blend;
    private CPlayer _player;
    private Transform _playerVisual;
    private CCompanionFollow _companion;
    private Camera _gameCamera;
    private bool _wasGameCameraEnabled;
    private bool _isGameCameraSuspended;
    private Transform _rig;
    private Vector3 _closePosition;
    private Quaternion _closeRotation;
    private float _closeFieldOfView;

    /// <summary>
    /// 하이파이브 타격 시점을 알립니다.
    /// </summary>
    public event Action HelperRevealed;

    public bool IsClosing => _isClosing;

    public bool IsHelperReady => LoadSettings() && _settings.Assets.helperPrefab != null;

    private void OnDisable()
    {
        Close();
        FinishClose();
    }

    private void OnDestroy()
    {
        Close();
        FinishClose();
        ReleaseActors();
        ReleaseEffects();
        if (_rig != null) Destroy(_rig.gameObject);
    }

    private void Update()
    {
        if (_isClosing)
        {
            StepClose();
            return;
        }

        if (!_isOpen) return;

        if (_player == null || _gameCamera == null || _playerVisual == null)
        {
            Close();
            FinishClose();
            return;
        }

        float deltaTime = Mathf.Min(Time.unscaledDeltaTime, _MAX_DELTA_TIME);
        _time += deltaTime;
        _blend = Mathf.Min(1f, _blend + deltaTime / _settings.Camera.openBlendTime);
        float blend = _blend * _blend * (3f - 2f * _blend);

        ReadPointer(deltaTime);
        StepCamera(deltaTime, blend);
        StepActors(deltaTime);
        StepEffects(deltaTime, blend);
    }

    /// <summary>
    /// 지정한 탭으로 정보창 연출을 시작합니다.
    /// </summary>
    public void Open(int tab)
    {
        if (_isOpen)
        {
            ChangeTab(tab);
            return;
        }

        FinishClose();
        if (!Prepare()) return;

        _isOpen = true;
        _tab = Mathf.Clamp(tab, 0, _POSE_COUNT - 1);
        _time = 0f;
        _blend = 0f;
        _feet = _playerVisual.position;

        PickBase();
        ResetCamera(_tab);
        HideRealCharacters();
        _rig.gameObject.SetActive(true);
        ShowPlayerCopy(_tab);
        BeginEffects(_tab);
        _camera.gameObject.SetActive(true);
        SortCustomEffects();
        _wasGameCameraEnabled = _gameCamera.enabled;
        _gameCamera.enabled = false;
        _isGameCameraSuspended = true;
    }

    /// <summary>
    /// 실제 캐릭터를 표시하고 카메라 복귀 연출을 시작합니다.
    /// </summary>
    public void Close()
    {
        if (!_isOpen) return;

        // 게임 입력이 재개되기 전에 Camera.main 조회를 복원한다.
        RestoreGameCamera();
        _isOpen = false;
        StopSwap(-1);
        HideCopies();
        ShowRealCharacters();

        bool canBlend = isActiveAndEnabled && _camera != null && _gameCamera != null && _blend > 0f
            && _settings.Camera.closeBlendTime > 0f;
        if (!canBlend)
        {
            _isClosing = true;
            FinishClose();
            return;
        }

        HideBackdrop();
        Transform cameraTransform = _camera.transform;
        _closePosition = cameraTransform.position;
        _closeRotation = cameraTransform.rotation;
        _closeFieldOfView = _camera.fieldOfView;
        _isClosing = true;
    }

    private void StepClose()
    {
        if (_camera == null || _gameCamera == null)
        {
            FinishClose();
            return;
        }

        float deltaTime = Mathf.Min(Time.unscaledDeltaTime, _MAX_DELTA_TIME);
        _blend = Mathf.Max(0f, _blend - deltaTime / _settings.Camera.closeBlendTime);
        if (_blend <= 0f)
        {
            FinishClose();
            return;
        }

        float blend = _blend * _blend * (3f - 2f * _blend);
        Transform game = _gameCamera.transform;
        _camera.transform.SetPositionAndRotation(Vector3.Lerp(game.position, _closePosition, blend),
            Quaternion.Slerp(game.rotation, _closeRotation, blend));
        _camera.fieldOfView = Mathf.Lerp(_gameCamera.fieldOfView, _closeFieldOfView, blend);
        FadeEffects(blend);
    }

    private void FinishClose()
    {
        RestoreGameCamera();
        if (!_isClosing) return;

        _isClosing = false;
        EndEffects();
        if (_camera != null) _camera.gameObject.SetActive(false);
    }

    private void RestoreGameCamera()
    {
        if (!_isGameCameraSuspended) return;

        if (_gameCamera != null) _gameCamera.enabled = _wasGameCameraEnabled;
        _isGameCameraSuspended = false;
    }

    /// <summary>
    /// 탭에 맞는 모델 동작과 카메라 연출로 전환합니다.
    /// </summary>
    public void ChangeTab(int current)
    {
        if (!_isOpen) return;

        current = Mathf.Clamp(current, 0, _HELPER_TAB);
        if (current == _tab) return;

        int previous = _tab;
        _tab = current;

        if (current == _HELPER_TAB)
        {
            ClearQueue();
            StartSwap();
            return;
        }

        if (previous == _HELPER_TAB)
        {
            LeaveSwap(current);
            return;
        }

        SetShotTarget(current, false);
        PlayTransition(current);
        BeginCut(previous, current);
    }

    private bool LoadSettings()
    {
        if (_settings == null) _settings = Resources.Load<CCharacterInfoStageSettingsSO>(_SETTINGS_PATH);
        return _settings != null;
    }

    private bool Prepare()
    {
        if (!LoadSettings())
        {
            CLogger.Warn($"{name}: 무대 설정(Resources/{_SETTINGS_PATH})이 없어 3D 무대를 열 수 없습니다.");
            return false;
        }

        if (_player == null) _player = FindFirstObjectByType<CPlayer>();
        if (_player == null) return false;

        if (_playerVisual == null)
        {
            Animator animator = _player.GetComponentInChildren<Animator>();
            if (animator != null) _playerVisual = animator.transform;
        }

        if (_companion == null) _companion = FindFirstObjectByType<CCompanionFollow>();
        _gameCamera = Camera.main;
        if (_playerVisual == null || _gameCamera == null) return false;

        if (_rig == null)
        {
            _rig = new GameObject("CharacterInfoStageRig").transform;
            _rig.gameObject.SetActive(false);
        }

        return PrepareCamera() && PrepareActors() && PrepareEffects();
    }

    private void HideRealCharacters()
    {
        HideRenderers(_player.transform);
        if (_companion != null) HideRenderers(_companion.transform);
    }

    private void HideRenderers(Transform root)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.enabled) continue;

            renderer.enabled = false;
            _HIDDEN_RENDERERS.Add(renderer);
        }
    }

    private void ShowRealCharacters()
    {
        for (int i = 0; i < _HIDDEN_RENDERERS.Count; i++)
        {
            if (_HIDDEN_RENDERERS[i] != null) _HIDDEN_RENDERERS[i].enabled = true;
        }

        _HIDDEN_RENDERERS.Clear();
    }
}
