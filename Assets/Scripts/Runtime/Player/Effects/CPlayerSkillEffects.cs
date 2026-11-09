using System.Collections.Generic;
using UnityEngine;

internal class CPlayerSkillEffects
{
    private readonly List<GameObject> _INSTANCES = new List<GameObject>();
    private readonly List<CPlayerEffectStart> _SYNCED_EFFECTS = new List<CPlayerEffectStart>();
    private readonly List<ParticleSystem> _PARTICLES = new List<ParticleSystem>();
    private readonly List<ParticleSystem> _PAUSED_PARTICLES = new List<ParticleSystem>();
    private Animator _animator;
    private float _animatorSpeed;
    private float _hitStopRemaining;

    internal GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        GameObject effect = Object.Instantiate(prefab, position, rotation, parent);
        Add(effect);
        return effect;
    }

    internal void Add(GameObject effect)
    {
        _INSTANCES.Add(effect);
    }

    internal void PlaySynced(GameObject effect, float elapsed, float speed)
    {
        if (effect.TryGetComponent(out CPlayerEffectStart effectStart))
        {
            _SYNCED_EFFECTS.Add(effectStart);
            effectStart.Play(elapsed, speed);
        }
    }

    internal void SetSpeed(float speed)
    {
        foreach (CPlayerEffectStart effect in _SYNCED_EFFECTS)
        {
            if (effect != null)
            {
                effect.SetSpeed(speed);
            }
        }
    }

    internal void HitStop(float duration, Animator animator)
    {
        _hitStopRemaining = duration;
        _animator = animator;
        _animatorSpeed = animator.speed;
        animator.speed = 0f;
        SetSpeed(0f);

        foreach (GameObject effect in _INSTANCES)
        {
            if (effect == null)
            {
                continue;
            }

            effect.GetComponentsInChildren(false, _PARTICLES);
            foreach (ParticleSystem particle in _PARTICLES)
            {
                if (particle.isPlaying && !particle.main.useUnscaledTime)
                {
                    particle.Pause(false);
                    _PAUSED_PARTICLES.Add(particle);
                }
            }
        }

        _PARTICLES.Clear();
    }

    internal bool UpdateHitStop()
    {
        if (_hitStopRemaining <= 0f)
        {
            return false;
        }

        _hitStopRemaining = Mathf.Max(_hitStopRemaining - Time.unscaledDeltaTime, 0f);
        if (_hitStopRemaining > 0f)
        {
            return true;
        }

        EndHitStop();
        return false;
    }

    internal void End(bool canceled, bool keepEffects)
    {
        if (_hitStopRemaining > 0f)
        {
            EndHitStop();
        }

        if (canceled)
        {
            foreach (GameObject effect in _INSTANCES)
            {
                if (effect == null)
                {
                    continue;
                }

                if (keepEffects)
                {
                    effect.transform.SetParent(null, true);
                }

                else
                {
                    effect.SetActive(false);
                    Object.Destroy(effect);
                }
            }
        }

        _SYNCED_EFFECTS.Clear();
        _INSTANCES.Clear();
    }

    private void EndHitStop()
    {
        _hitStopRemaining = 0f;
        _animator.speed = _animatorSpeed;
        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        SetSpeed(state.speed * state.speedMultiplier * _animatorSpeed);

        foreach (ParticleSystem particle in _PAUSED_PARTICLES)
        {
            if (particle != null)
            {
                particle.Play(false);
            }
        }

        _PAUSED_PARTICLES.Clear();
    }
}
