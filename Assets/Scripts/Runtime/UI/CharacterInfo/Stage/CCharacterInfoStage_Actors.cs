using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public partial class CCharacterInfoStage
{
    private class CMotion
    {
        public AnimationClipPlayable playable;
        public float length;
        public bool isLoop;
        public float time;
        public float weight;
        public float targetWeight;
        public float fadeSpeed;
    }

    private const string _SWORD_NAME = "Sword";
    private const int _SWAP_OUT_INDEX = _POSE_COUNT + _POSE_COUNT * (_POSE_COUNT - 1);
    private const int _HELPER_WARM_FRAMES = 3;
    private const float _HELPER_WARM_DEPTH = 60f;
    private const float _CLIP_END_MARGIN = 0.001f;

    private GameObject _playerCopy;
    private Renderer[] _playerCopyRenderers;
    private Transform _swordCopy;
    private Transform _playerHips;
    private Vector3 _playerHipsRest;
    private CDynamicBoneUnscaledTime _playerBoneTime;
    private PlayableGraph _playerGraph;
    private AnimationMixerPlayable _playerMixer;
    private CMotion[] _playerMotions;
    private int _currentMotion = -1;
    private int _nextHold = -1;
    private int _pose;
    private float _playRate = 1f;

    private GameObject _helperCopy;
    private Renderer[] _helperRenderers;
    private CPradonaSpringBones _helperSprings;
    private CCharacterInfoHandMark _helperMark;
    private Transform _helperHips;
    private PlayableGraph _helperGraph;
    private AnimationMixerPlayable _helperMixer;
    private CMotion[] _helperMotions;
    private int _helperWarmFrames;

    private bool PrepareActors()
    {
        CCharacterInfoStageSettingsSO.SAssets assets = _settings.Assets;

        if (_playerCopy == null)
        {
            if (assets.playerVisualPrefab == null || assets.holdClips == null || assets.holdClips.Length < _POSE_COUNT
                || assets.transitionClips == null || assets.transitionClips.Length < _SWAP_OUT_INDEX - _POSE_COUNT)
            {
                CLogger.Warn($"{name}: 무대 설정에 플레이어 프리팹이나 동작 클립이 비어 있습니다.");
                return false;
            }

            CreatePlayerCopy(assets);
        }

        if (_helperCopy == null && assets.helperPrefab != null && assets.helperSwapClip != null
            && assets.helperHoldClip != null)
        {
            CreateHelperCopy(assets);
        }

        return true;
    }

    private void CreatePlayerCopy(CCharacterInfoStageSettingsSO.SAssets assets)
    {
        _playerCopy = Instantiate(assets.playerVisualPrefab, _rig);
        _playerCopy.name = "CharacterInfoPlayerCopy";
        StripGameplay(_playerCopy);

        Transform sword = FindChild(_playerVisual, _SWORD_NAME);
        Transform hand = sword != null ? FindChild(_playerCopy.transform, sword.parent.name) : null;
        if (hand != null)
        {
            GameObject swordCopy = Instantiate(sword.gameObject, hand);
            swordCopy.name = _SWORD_NAME;
            swordCopy.transform.localPosition = sword.localPosition;
            swordCopy.transform.localRotation = sword.localRotation;
            swordCopy.transform.localScale = sword.localScale;
            StripGameplay(swordCopy);
            _swordCopy = swordCopy.transform;
            foreach (Renderer renderer in swordCopy.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
        }

        _playerHips = FindChild(_playerCopy.transform, "hips");
        if (_playerHips != null) _playerHipsRest = _playerCopy.transform.InverseTransformPoint(_playerHips.position);

        _playerBoneTime = _playerCopy.AddComponent<CDynamicBoneUnscaledTime>();

        // 뼈 경로를 사용하는 클립이므로 휴머노이드 리타게팅을 끈다.
        Animator animator = _playerCopy.GetComponentInChildren<Animator>();
        animator.runtimeAnimatorController = null;
        animator.avatar = null;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        AnimationClip[] clips = new AnimationClip[_SWAP_OUT_INDEX + 1];
        for (int i = 0; i < _POSE_COUNT; i++) clips[i] = assets.holdClips[i];
        for (int i = _POSE_COUNT; i < _SWAP_OUT_INDEX; i++) clips[i] = assets.transitionClips[i - _POSE_COUNT];
        clips[_SWAP_OUT_INDEX] = assets.swapOutClip;
        _playerMotions = BuildGraph("CharacterInfoPlayer", animator, clips, out _playerGraph, out _playerMixer);
        _playerCopyRenderers = _playerCopy.GetComponentsInChildren<Renderer>(true);
        RegisterCharacters(_playerCopy);
        _playerCopy.SetActive(false);
    }

    private void CreateHelperCopy(CCharacterInfoStageSettingsSO.SAssets assets)
    {
        _helperCopy = Instantiate(assets.helperPrefab, _rig);
        _helperCopy.name = "CharacterInfoHelperCopy";
        StripGameplay(_helperCopy);

        Animator animator = _helperCopy.GetComponentInChildren<Animator>();
        animator.runtimeAnimatorController = null;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        _helperMotions = BuildGraph("CharacterInfoHelper", animator,
            new[] { assets.helperSwapClip, assets.helperHoldClip }, out _helperGraph, out _helperMixer);

        // 가산 합성 이펙트는 캐릭터 깊이 렌더링에서 제외한다.
        RegisterCharacters(_helperCopy);
        _helperRenderers = _helperCopy.GetComponentsInChildren<Renderer>(true);
        _helperHips = FindChild(_helperCopy.transform, "hips");
        _helperSprings = _helperCopy.GetComponentInChildren<CPradonaSpringBones>();
        if (_helperSprings != null) _helperSprings.IsManual = true;

        if (assets.handMark != null)
        {
            _helperMark = _helperCopy.AddComponent<CCharacterInfoHandMark>();
            if (!_helperMark.Setup(assets.handMark)) _helperMark = null;
        }

        // 첫 표시 전 화면 밖에서 몇 프레임 활성화해 초기화를 마친다.
        _helperCopy.transform.position = _playerVisual.position + Vector3.down * _HELPER_WARM_DEPTH;
        _helperWarmFrames = _HELPER_WARM_FRAMES;
    }

    private static void StripGameplay(GameObject target)
    {
        foreach (Collider collider in target.GetComponentsInChildren<Collider>(true)) Destroy(collider);
        foreach (AudioSource source in target.GetComponentsInChildren<AudioSource>(true)) Destroy(source);
        foreach (AudioListener listener in target.GetComponentsInChildren<AudioListener>(true)) Destroy(listener);
        foreach (Camera camera in target.GetComponentsInChildren<Camera>(true)) Destroy(camera);
    }

    private static void RegisterCharacters(GameObject copy)
    {
        foreach (Renderer renderer in copy.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is SkinnedMeshRenderer || renderer is MeshRenderer)
            {
                CCharacterInfoFocus.Characters.Add(renderer);
            }
        }
    }

    private static Transform FindChild(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName) return child;
        }

        return null;
    }

    private static CMotion[] BuildGraph(string graphName, Animator animator, AnimationClip[] clips,
        out PlayableGraph graph, out AnimationMixerPlayable mixer)
    {
        graph = PlayableGraph.Create(graphName);
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        mixer = AnimationMixerPlayable.Create(graph, clips.Length);
        CMotion[] motions = new CMotion[clips.Length];

        for (int i = 0; i < clips.Length; i++)
        {
            motions[i] = new CMotion();
            if (clips[i] == null) continue;

            AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clips[i]);
            playable.SetApplyFootIK(false);
            graph.Connect(playable, 0, mixer, i);
            motions[i].playable = playable;
            motions[i].length = clips[i].length;
            motions[i].isLoop = clips[i].isLooping;
        }

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, graphName, animator);
        output.SetSourcePlayable(mixer);
        graph.Play();
        return motions;
    }

    private void ReleaseActors()
    {
        CCharacterInfoFocus.Characters.Clear();
        if (_playerGraph.IsValid()) _playerGraph.Destroy();
        if (_helperGraph.IsValid()) _helperGraph.Destroy();
    }

    private void ShowPlayerCopy(int tab)
    {
        _playRate = 1f;
        _playerCopy.SetActive(true);
        SetPlayerCopyVisible(true);
        PlacePlayerCopy(_startYaw);
        SnapHold(tab);
        ApplyMotions(_playerMotions, _playerMixer, _playerGraph);

        if (_helperCopy != null && _helperWarmFrames > 0)
        {
            _helperCopy.transform.position = _feet + Vector3.down * _HELPER_WARM_DEPTH;
            _helperCopy.SetActive(true);
        }
    }

    private void HideCopies()
    {
        if (_playerCopy != null) _playerCopy.SetActive(false);
        if (_helperCopy != null) _helperCopy.SetActive(false);
    }

    private void PlacePlayerCopy(float yaw)
    {
        _playerCopy.transform.SetPositionAndRotation(_feet, Quaternion.LookRotation(StageDirection(yaw), Vector3.up));
    }

    private void SetPlayerCopyVisible(bool isVisible)
    {
        for (int i = 0; i < _playerCopyRenderers.Length; i++)
        {
            if (_playerCopyRenderers[i] != null) _playerCopyRenderers[i].enabled = isVisible;
        }
    }

    private static int TransitionIndex(int from, int to)
    {
        return _POSE_COUNT + from * (_POSE_COUNT - 1) + (to > from ? to - 1 : to);
    }

    private void SnapHold(int pose)
    {
        for (int i = 0; i < _playerMotions.Length; i++)
        {
            _playerMotions[i].weight = 0f;
            _playerMotions[i].targetWeight = 0f;
        }

        _playerMotions[pose].weight = 1f;
        _playerMotions[pose].targetWeight = 1f;
        _playerMotions[pose].time = 0f;
        _currentMotion = pose;
        _nextHold = -1;
        _pose = pose;
    }

    private void FadeTo(int next, float duration)
    {
        float speed = duration > 1e-4f ? 1f / duration : float.MaxValue;

        for (int i = 0; i < _playerMotions.Length; i++)
        {
            _playerMotions[i].targetWeight = i == next ? 1f : 0f;
            _playerMotions[i].fadeSpeed = speed;
        }

        _playerMotions[next].time = 0f;
        _currentMotion = next;
    }

    private void PlayTransition(int pose)
    {
        if (_pose == pose || _pose >= _POSE_COUNT)
        {
            FadeTo(pose, _settings.Motion.holdBlend);
            _nextHold = -1;
        }
        else
        {
            FadeTo(TransitionIndex(_pose, pose), _settings.Motion.transitionBlend);
            _nextHold = pose;
        }

        _pose = pose;
    }

    // 골반 이동을 루트로 전달해 머리카락 물리가 실제 이동량을 받도록 한다.
    private void CarryPlayerRoot()
    {
        if (_playerHips == null) return;

        Transform root = _playerCopy.transform;
        Vector3 hips = _playerHips.position;
        Vector3 local = root.InverseTransformPoint(hips) - _playerHipsRest;
        local.y = 0f;
        root.position += root.TransformVector(local);
        _playerHips.position = hips;
    }

    private void StepActors(float deltaTime)
    {
        float step = deltaTime * _playRate;

        for (int i = 0; i < _playerMotions.Length; i++)
        {
            CMotion motion = _playerMotions[i];
            motion.weight = Mathf.MoveTowards(motion.weight, motion.targetWeight, motion.fadeSpeed * step);
            if (motion.weight > 0f || motion.targetWeight > 0f) motion.time += step;
        }

        if (_nextHold >= 0 && _playerMotions[_currentMotion].time >= _playerMotions[_currentMotion].length
            - _CLIP_END_MARGIN)
        {
            int hold = _nextHold;
            _nextHold = -1;
            FadeTo(hold, _settings.Motion.settleBlend);
        }

        if (_isSwapOn) HoldSwapMotion();
        ApplyMotions(_playerMotions, _playerMixer, _playerGraph);
        CarryPlayerRoot();
        if (_playerBoneTime != null) _playerBoneTime.Step(deltaTime);
        SetPlayerCopyVisible(!_isPlayerCopyHidden);

        if (_isSwapOn)
        {
            StepHelper(deltaTime);
        }
        else if (_helperWarmFrames > 0 && _helperCopy != null && _helperCopy.activeSelf)
        {
            _helperWarmFrames--;
            if (_helperWarmFrames == 0) _helperCopy.SetActive(false);
        }
    }

    private static void ApplyMotions(CMotion[] motions, AnimationMixerPlayable mixer, PlayableGraph graph)
    {
        float sum = 0f;
        for (int i = 0; i < motions.Length; i++) sum += motions[i].weight;
        if (sum <= 0f) return;

        for (int i = 0; i < motions.Length; i++)
        {
            CMotion motion = motions[i];
            if (!motion.playable.IsValid()) continue;

            mixer.SetInputWeight(i, motion.weight / sum);
            if (motion.weight <= 0f) continue;

            float time = motion.isLoop ? Mathf.Repeat(motion.time, motion.length)
                : Mathf.Min(motion.time, motion.length);
            motion.playable.SetTime(time);
        }

        graph.Evaluate(0f);
    }
}
