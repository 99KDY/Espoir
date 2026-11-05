using System;
using System.Collections.Generic;
using UnityEngine;

public class CPlayerEffectStart : MonoBehaviour
{
    private const float _FIRST_STEP = 1f / 120f;
    private const float _SORT_SCALE = 1000f;

    private static readonly List<CPlayerEffectStart> _PLAYING = new List<CPlayerEffectStart>();

    private enum EScreenMode
    {
        Fade,
        Hold,
        Invert,
        Flash
    }

    [Serializable]
    private struct SCameraPunch
    {
        public float time;
        public float zoom;
        public float shake;
    }

    [Serializable]
    private struct SScreenCue
    {
        public float time;
        public float duration;
        public float strength;
        public EScreenMode mode;
    }

    [Serializable]
    private struct SStopCue
    {
        public float time;
        public float duration;
    }

    [Serializable]
    private struct SChromaCue
    {
        public float time;
        public float amount;
        public float height;
    }

    [SerializeField] private SStopCue[] _stops = Array.Empty<SStopCue>();
    [SerializeField] private SChromaCue[] _chroma = Array.Empty<SChromaCue>();
    [SerializeField] private SCameraPunch[] _punch = Array.Empty<SCameraPunch>();
    [SerializeField] private SScreenCue[] _screens = Array.Empty<SScreenCue>();

    private ParticleSystem _particles;
    private ParticleSystem[] _systems;
    private AnimationState[] _lightAnimations;
    private float _speed = 1f;
    private float _pauseRemaining;
    private int _nextStop;
    private ParticleSystemRenderer[] _ribbons;
    private Vector3[] _centers;
    private bool _distorts;

    internal static int ActiveCount => _PLAYING.Count;
    internal static int DistortionCount { get; private set; }

    private void Awake()
    {
        ParticleSystemRenderer[] renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
        List<ParticleSystemRenderer> ribbons = new List<ParticleSystemRenderer>();

        foreach (ParticleSystemRenderer particle in renderers)
        {
            particle.gameObject.layer = CPlayerEffectRenderer.EFFECT_LAYER;

            if (!particle.enabled || particle.sharedMaterial == null)
            {
                continue;
            }

            string distortion = particle.sharedMaterial.GetTag("EspoirDistort", false);

            if (distortion == "On" || distortion == "Ground")
            {
                _distorts = true;
            }

            if (particle.renderMode == ParticleSystemRenderMode.Mesh && particle.mesh != null)
            {
                ribbons.Add(particle);
            }
        }

        _ribbons = ribbons.ToArray();
        _centers = new Vector3[_ribbons.Length];

        for (int i = 0; i < _ribbons.Length; i++)
        {
            _centers[i] = _ribbons[i].mesh.bounds.center;
        }

        _systems = GetComponentsInChildren<ParticleSystem>(true);
        List<AnimationState> lightAnimations = new List<AnimationState>();

        foreach (Animation animation in GetComponentsInChildren<Animation>(true))
        {
            foreach (AnimationState state in animation)
            {
                lightAnimations.Add(state);
            }
        }

        _lightAnimations = lightAnimations.ToArray();
        _particles = GetComponent<ParticleSystem>();
        _particles.Simulate(_FIRST_STEP, true, true, false);
        _particles.Play(true);
    }

    private void Update()
    {
        if (_pauseRemaining > 0f)
        {
            _pauseRemaining -= Time.deltaTime;

            if (_pauseRemaining <= 0f)
            {
                ApplySpeed();
            }

            return;
        }

        if (_nextStop < _stops.Length && _particles.time >= _stops[_nextStop].time)
        {
            _pauseRemaining = _stops[_nextStop].duration;
            _nextStop++;
            ApplySpeed();
        }
    }

    /// <summary>
    /// 파티클과 이펙트 조명의 재생 속도를 맞춥니다.
    /// </summary>

    public void SetSpeed(float speed)
    {
        speed = Mathf.Max(0f, speed);

        if (_speed == speed)
        {
            return;
        }

        _speed = speed;
        ApplySpeed();
    }

    internal void Play(float time, float speed)
    {
        SetSpeed(1f);
        _particles.Simulate(time, true, true, false);
        _particles.Play(true);

        foreach (AnimationState animation in _lightAnimations)
        {
            animation.time = time;
        }

        SetSpeed(speed);
    }

    private void ApplySpeed()
    {
        float speed = _pauseRemaining > 0f ? 0f : _speed;

        foreach (ParticleSystem system in _systems)
        {
            ParticleSystem.MainModule main = system.main;
            main.simulationSpeed = speed;
        }

        foreach (AnimationState animation in _lightAnimations)
        {
            animation.speed = speed;
        }
    }

    private void OnEnable()
    {
        _PLAYING.Add(this);

        if (_distorts)
        {
            DistortionCount++;
        }
    }

    private void OnDisable()
    {
        _PLAYING.Remove(this);

        if (_distorts)
        {
            DistortionCount--;
        }
    }

    internal static void GetCameraCues(out Vector2 punch, out Vector3 screen)
    {
        punch = Vector2.zero;
        screen = Vector3.zero;

        foreach (CPlayerEffectStart effect in _PLAYING)
        {
            if (effect._punch.Length == 0 && effect._screens.Length == 0)
            {
                continue;
            }

            float age = effect._particles.time;

            foreach (SCameraPunch cue in effect._punch)
            {
                float elapsed = age - cue.time;
                if (elapsed < 0f) break;

                punch.x = Mathf.Max(punch.x, cue.zoom * Mathf.Pow(0.002f, elapsed));
                punch.y = Mathf.Max(punch.y, cue.shake * Mathf.Pow(0.0008f, elapsed));
            }

            foreach (SScreenCue cue in effect._screens)
            {
                float elapsed = age - cue.time;
                if (elapsed < 0f) break;
                if (elapsed >= cue.duration) continue;

                float fade = 1f - elapsed / cue.duration;

                if (cue.mode == EScreenMode.Flash)
                {
                    screen.z = Mathf.Max(screen.z, cue.strength * fade);
                    continue;
                }

                float strength = cue.mode == EScreenMode.Fade ? cue.strength * fade * fade : cue.strength;
                if (strength <= screen.x) continue;

                screen.x = strength;
                screen.y = cue.mode == EScreenMode.Invert ? 1f : 0f;
            }
        }
    }

    internal static void GetChroma(out float amount, out Vector3 center)
    {
        amount = 0f;
        center = Vector3.zero;

        foreach (CPlayerEffectStart effect in _PLAYING)
        {
            float age = effect._particles.time;

            foreach (SChromaCue cue in effect._chroma)
            {
                float elapsed = age - cue.time;

                if (elapsed < 0f)
                {
                    break;
                }

                float strength = cue.amount * Mathf.Pow(0.004f, elapsed);

                if (strength <= amount)
                {
                    continue;
                }

                amount = strength;
                center = effect.transform.position + Vector3.up * cue.height;
            }
        }
    }

    internal static void Sort(Camera camera)
    {
        Vector3 eye = camera.transform.position;
        Vector3 forward = camera.transform.forward;

        foreach (CPlayerEffectStart effect in _PLAYING)
        {
            for (int i = 0; i < effect._ribbons.Length; i++)
            {
                ParticleSystemRenderer ribbon = effect._ribbons[i];
                Vector3 center = ribbon.transform.TransformPoint(effect._centers[i]);
                float depth = Vector3.Dot(center - eye, forward);
                ribbon.sortingFudge = depth * _SORT_SCALE - i * 0.01f;
            }
        }
    }
}
