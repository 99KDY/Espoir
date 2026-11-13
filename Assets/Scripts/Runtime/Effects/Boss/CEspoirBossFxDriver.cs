using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CEspoirBossFxDriver : MonoBehaviour
{
    [Serializable]
    public struct SAttackEffect
    {
        public string state;
        public CEspoirBossFxPlayer prefab;

        [Min(0.01f)]
        public float scale;
        public Vector3 offset;

        [HideInInspector] public float holdTime;
        [HideInInspector] public float holdDuration;
        public CEspoirBossFxPlayer.CElementSetting[] elements;

        [NonSerialized]
        public int hash;

        [NonSerialized]
        public float clipLength;
    }

    [Header("연결")]
    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private Transform _origin;

    [Header("전체 이펙트")]
    [SerializeField, Min(0.01f)]
    private float _scale = 1f;

    [SerializeField]
    private Vector3 _offset;

    [SerializeField]
    private bool _screenEffects = true;

    [SerializeField]
    private bool _effectLights;

    [SerializeField]
    private bool _clearOnNextAttack = true;

    [Header("공격별 설정")]
    [SerializeField]
    private SAttackEffect[] _attacks;

    private int _previewGroup = -1;

    /// <summary>재생 중 선택한 묶음만 표시합니다. 테스트 상태는 저장하지 않습니다.</summary>
    public void PreviewOnly(int group)
    {
        _previewGroup = group;
        foreach (CEspoirBossFxPlayer effect in _effects)
            if (effect != null) effect.PreviewGroup = group;
    }

    private readonly List<CEspoirBossFxPlayer> _effects = new List<CEspoirBossFxPlayer>();
    private Renderer[] _renderers;
    private int _currentHash;
    private int _currentLayer = -1;
    private float _previousTime;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponentInParent<Animator>();
        }

        if (_animator == null || _animator.runtimeAnimatorController == null || _attacks == null)
        {
            enabled = false;
            return;
        }

        if (_origin == null)
        {
            _origin = _animator.transform;
        }

        _renderers = _animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        AnimationClip[] clips = _animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < _attacks.Length; i++)
        {
            _attacks[i].hash = Animator.StringToHash(_attacks[i].state);
            foreach (AnimationClip clip in clips)
            {
                if (clip.name == _attacks[i].state)
                {
                    _attacks[i].clipLength = clip.length;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        if (_animator == null || !_animator.isActiveAndEnabled)
        {
            StopAll();
            return;
        }

        for (int i = _effects.Count - 1; i >= 0; i--)
        {
            if (_effects[i] == null)
            {
                _effects.RemoveAt(i);
            }
        }

        for (int layer = _animator.layerCount - 1; layer >= 0; layer--)
        {
            if (layer > 0 && _animator.GetLayerWeight(layer) <= 0f)
            {
                continue;
            }

            AnimatorStateInfo state = _animator.IsInTransition(layer)
                ? _animator.GetNextAnimatorStateInfo(layer)
                : _animator.GetCurrentAnimatorStateInfo(layer);
            int index = FindAttack(state.shortNameHash);
            if (index < 0)
            {
                continue;
            }

            if (
                _currentHash != state.shortNameHash
                || _currentLayer != layer
                || state.normalizedTime + 0.05f < _previousTime
            )
            {
                Play(index, layer);
            }

            _currentHash = state.shortNameHash;
            _currentLayer = layer;
            _previousTime = state.normalizedTime;
            return;
        }

        _currentHash = 0;
        _currentLayer = -1;
    }

    private int FindAttack(int hash)
    {
        for (int i = 0; i < _attacks.Length; i++)
        {
            if (_attacks[i].hash == hash && _attacks[i].prefab != null)
            {
                return i;
            }
        }

        return -1;
    }

    private float GetTime(int layer, SAttackEffect attack)
    {
        if (_animator == null || !_animator.isActiveAndEnabled)
        {
            return float.NaN;
        }

        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(layer);
        if (_animator.IsInTransition(layer))
        {
            AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(layer);
            if (next.shortNameHash == attack.hash)
            {
                state = next;
            }
        }

        if (state.shortNameHash != attack.hash)
        {
            return float.NaN;
        }

        float length = attack.clipLength > 0f ? attack.clipLength : state.length;
        return state.normalizedTime * length;
    }

    private void Play(int index, int layer)
    {
        if (_clearOnNextAttack)
        {
            foreach (CEspoirBossFxPlayer previous in _effects)
                if (previous != null) previous.FinishAttack();
        }

        SAttackEffect attack = _attacks[index];
        Quaternion rotation = Quaternion.Euler(0f, _origin.eulerAngles.y, 0f);
        Vector3 position = _origin.position + rotation * (_offset + attack.offset);
        CEspoirBossFxPlayer effect = Instantiate(attack.prefab, position, rotation);
        effect.SetElements(attack.elements);
        effect.holdTime = attack.holdTime;
        effect.holdDuration = attack.holdDuration;
        effect.PreviewGroup = _previewGroup;
        effect.stageScale *= _scale * attack.scale;
        effect.ApplyStageScale();
        effect.screenEffects = _screenEffects;
        effect.effectLights = _effectLights;
        effect.inkRenderers = _renderers;
        effect.clock = () => GetTime(layer, attack);
        _effects.Add(effect);
    }

    /// <summary>
    /// 재생 중인 이펙트를 모두 종료합니다. 보스의 사망이나 패턴 취소에 연결합니다.
    /// </summary>
    public void StopAll()
    {
        foreach (CEspoirBossFxPlayer effect in _effects)
        {
            if (effect != null)
            {
                effect.Stop();
            }
        }

        _effects.Clear();
        _currentHash = 0;
        _currentLayer = -1;
    }

    private void OnDisable()
    {
        StopAll();
    }
}
