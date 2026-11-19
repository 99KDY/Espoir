using UnityEngine;

[DisallowMultipleComponent]
public class CPlayerSkillAudio : MonoBehaviour
{
    [System.Serializable]
    private class CSound
    {
        public AudioSource source;
        [Min(0f)] public float impactTime;
        public Vector2 pitchRange = Vector2.one;
    }

    [System.Serializable]
    private class CAttackSound
    {
        [Min(0)] public int stepIndex;
        [Min(0)] public int hitIndex;
        public bool useEffectTime;
        public CSound sound = new CSound();
    }

    [System.Serializable]
    private class CAttackSoundSet
    {
        public CSkillSO[] skills;
        public CAttackSound[] attacks;
    }

    [Header("평타")]
    [SerializeField] private CAttackSoundSet[] _attacks;

    [Header("Q")]
    [SerializeField] private CSound _strike = new CSound();

    [Header("W")]
    [SerializeField] private AudioSource _crown;
    [SerializeField] private AudioSource _crownFull;
    [SerializeField, Min(1)] private int _crownFullStack = 3;
    [SerializeField, Min(0f)] private float _crownFullDelay;

    [Header("E")]
    [SerializeField] private CSound _rush = new CSound();

    [Header("R")]
    [SerializeField] private AudioSource _leviathanBuff;
    [SerializeField] private AudioSource _leviathanFinisher;
    [SerializeField] private CSound _leviathanImpact = new CSound();

    [Header("대쉬")]
    [SerializeField] private AudioSource _dash;

    private readonly System.Random _PITCH_RANDOM = new System.Random();
    private CAttackSound[] _stepAttacks;
    private int _stepIndex;
    private int _nextAttack;
    private bool _hasPlayedSkill;
    private AudioSource _preparingSound;
    private float _impactProgress;
    private float _progress;
    private bool _isCrownPending;
    private float _crownPlayTime;

    private void Update()
    {
        if (!_isCrownPending || Time.time < _crownPlayTime) return;

        _isCrownPending = false;
        Play(_crownFull);
    }

    private void OnDisable()
    {
        _isCrownPending = false;
        EndStep(true);
        if (_attacks != null)
        {
            foreach (CAttackSoundSet set in _attacks)
            {
                if (set == null || set.attacks == null) continue;
                foreach (CAttackSound attack in set.attacks)
                {
                    if (attack != null) Stop(attack.sound);
                }
            }
        }
        Stop(_strike);
        Stop(_crown);
        Stop(_crownFull);
        Stop(_rush);
        Stop(_leviathanBuff);
        Stop(_leviathanFinisher);
        Stop(_leviathanImpact);
        Stop(_dash);
    }

    internal void BeginStep(CSkillSO skill, int stepIndex)
    {
        EndStep(false);
        _stepIndex = stepIndex;
        if (_attacks == null || skill.Type != ESkillType.Melee) return;

        foreach (CAttackSoundSet set in _attacks)
        {
            if (set != null && set.skills != null && System.Array.IndexOf(set.skills, skill) >= 0)
            {
                _stepAttacks = set.attacks;
                return;
            }
        }
    }

    internal void UpdateStep(CSkillSO skill, AnimatorStateInfo state, float animatorSpeed, int nextHitIndex)
    {
        if (!isActiveAndEnabled) return;

        _progress = state.normalizedTime;
        float speed = state.speed * state.speedMultiplier * animatorSpeed;
        if (speed <= 0f) return;
        if (_preparingSound != null && _progress >= _impactProgress) _preparingSound = null;

        SSkillStep step = skill.Steps[_stepIndex];
        if (skill.Type == ESkillType.Melee)
        {
            if (_stepAttacks == null) return;
            while (_nextAttack < _stepAttacks.Length)
            {
                CAttackSound attack = _stepAttacks[_nextAttack];
                if (attack == null || attack.stepIndex != _stepIndex)
                {
                    _nextAttack++;
                    continue;
                }
                if (!attack.useEffectTime && (step.hits == null || attack.hitIndex < 0 ||
                    attack.hitIndex >= step.hits.Length))
                {
                    _nextAttack++;
                    continue;
                }
                if (!attack.useEffectTime && nextHitIndex < attack.hitIndex) break;
                float time = attack.useEffectTime ? step.effectTime : step.hits[attack.hitIndex].time;
                if (!TryPlayTimed(attack.sound, time, state.length, speed)) break;
                _nextAttack++;
            }
            return;
        }

        if (_hasPlayedSkill) return;
        if (skill.Type == ESkillType.Strike && step.hits != null && step.hits.Length > 0)
        {
            _hasPlayedSkill = TryPlayTimed(_strike, step.hits[0].time, state.length, speed);
        }
        else if (skill.Type == ESkillType.Rush)
        {
            _hasPlayedSkill = TryPlayTimed(_rush, step.moveStart, state.length, speed);
        }
        else if (skill is CLeviathanSkillSO leviathan)
        {
            _hasPlayedSkill = TryPlayTimed(_leviathanImpact, leviathan.FirstHitTime, state.length, speed);
        }
    }

    internal void EndStep(bool canceled)
    {
        // 타격 전에 취소하면 준비음만 끊고, 이미 발생한 타격음의 잔향은 유지한다.
        if (canceled && _preparingSound != null && _progress < _impactProgress) _preparingSound.Stop();
        _preparingSound = null;
        _stepAttacks = null;
        _nextAttack = 0;
        _hasPlayedSkill = false;
        _progress = 0f;
    }

    internal void PlayCrown(int stack)
    {
        if (!isActiveAndEnabled) return;
        _isCrownPending = false;
        if (stack >= _crownFullStack && _crownFullDelay > 0f)
        {
            _isCrownPending = true;
            _crownPlayTime = Time.time + _crownFullDelay;
        }
        else
        {
            Play(stack >= _crownFullStack ? _crownFull : _crown);
        }
    }

    internal void PlayLeviathanBuff()
    {
        Play(_leviathanBuff);
    }

    internal void PlayLeviathanFinisher()
    {
        Play(_leviathanFinisher);
    }

    internal void PlayDash()
    {
        Play(_dash);
    }

    private bool TryPlayTimed(CSound sound, float time, float length, float speed)
    {
        if (sound == null || sound.source == null || sound.source.clip == null) return true;

        float remaining = Mathf.Max(0f, time - _progress) * length / speed;
        float minPitch = Mathf.Clamp(Mathf.Min(sound.pitchRange.x, sound.pitchRange.y), 0.01f, 3f);
        float maxPitch = Mathf.Clamp(Mathf.Max(sound.pitchRange.x, sound.pitchRange.y), minPitch, 3f);
        if (remaining * maxPitch > sound.impactTime) return false;

        AudioSource source = sound.source;
        source.pitch = Mathf.Lerp(minPitch, maxPitch, (float)_PITCH_RANDOM.NextDouble());
        // 준비 동작이 짧으면 음원 앞부분을 건너뛰어 타격음을 맞춘다.
        float start = Mathf.Max(0f, sound.impactTime - remaining * source.pitch);
        source.timeSamples = Mathf.Clamp(Mathf.RoundToInt(start * source.clip.frequency), 0, source.clip.samples - 1);
        source.Play();
        _preparingSound = _progress < time ? source : null;
        _impactProgress = time;
        return true;
    }

    private void Play(AudioSource source)
    {
        if (!isActiveAndEnabled || source == null || source.clip == null) return;
        source.Play();
    }

    private static void Stop(CSound sound)
    {
        if (sound != null) Stop(sound.source);
    }

    private static void Stop(AudioSource source)
    {
        if (source != null) source.Stop();
    }
}
