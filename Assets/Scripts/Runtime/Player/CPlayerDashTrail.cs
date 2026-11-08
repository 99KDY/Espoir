using System;
using UnityEngine;

[Serializable]
public struct SDashGhostStyle
{
    [Min(0f)] public float lifetime;
    [Range(0f, 1f)] public float alpha;
    [Min(0f)] public float smear;
    [Range(0f, 1f)] public float hold;
}

[Serializable]
public struct SDashTrailSettings
{
    public GameObject startShock;
    public GameObject endShock;
    public float startHeight;
    public float endHeight;
    public Material ghostMaterial;
    [Min(0.01f)] public float firstDistance;
    [Min(0.01f)] public float spacing;
    [Range(0f, 1f)] public float lateProgress;
    [Min(0f)] public float earlyLifetimeStep;
    [Min(0f)] public float alphaStep;
    [Range(0f, 1f)] public float minAlpha;
    public Vector2 ghostWindow;
    public SDashGhostStyle departure;
    public SDashGhostStyle early;
    public SDashGhostStyle late;
}

internal class CPlayerDashTrail
{
    private CPlayerDashGhost.CSource _source;
    private Vector3 _origin;
    private float _nextDistance;
    private int _ghostCount;
    private bool _spawnedRushGhost;

    internal void Begin(SDashTrailSettings settings, Transform player, Animator animator, Vector3 direction,
        CPlayerSkillEffects effects)
    {
        Reset(player.position, settings.firstDistance);
        SpawnShock(settings.startShock, player.position, settings.startHeight, effects);

        if (settings.ghostMaterial != null)
        {
            SpawnGhost(animator, settings.ghostMaterial, settings.departure, direction, false, effects);
        }
    }

    internal void Update(SDashTrailSettings settings, Transform player, Animator animator, Vector3 direction,
        float progress, CPlayerSkillEffects effects)
    {
        if (progress >= 1f)
        {
            SpawnShock(settings.endShock, player.position, settings.endHeight, effects);
            return;
        }

        UpdateGhosts(settings, player, animator, direction, progress, effects);
    }

    internal void Reset(Vector3 origin, float firstDistance)
    {
        _origin = origin;
        _ghostCount = 0;
        _nextDistance = firstDistance;
        _spawnedRushGhost = false;
    }

    internal void UpdateRush(SDashTrailSettings settings, Transform player, Animator animator, Vector3 direction,
        float time, CPlayerSkillEffects effects)
    {
        if (settings.ghostMaterial == null || time < settings.ghostWindow.x || time >= settings.ghostWindow.y)
        {
            return;
        }

        if (!_spawnedRushGhost)
        {
            SpawnGhost(animator, settings.ghostMaterial, settings.departure, direction, true, effects);
            _spawnedRushGhost = true;
        }

        UpdateGhosts(settings, player, animator, direction, time, effects);
    }

    private void UpdateGhosts(SDashTrailSettings settings, Transform player, Animator animator, Vector3 direction,
        float progress, CPlayerSkillEffects effects)
    {
        float distance = Vector3.Dot(player.position - _origin, direction);

        if (settings.ghostMaterial == null || progress <= 0f || distance < _nextDistance)
        {
            return;
        }

        _nextDistance = distance + settings.spacing;
        _ghostCount++;
        bool isLate = progress > settings.lateProgress;
        SDashGhostStyle style = isLate ? settings.late : settings.early;

        if (!isLate)
        {
            style.lifetime += _ghostCount * settings.earlyLifetimeStep;
        }

        style.alpha = Mathf.Clamp(style.alpha - _ghostCount * settings.alphaStep, settings.minAlpha, 1f);
        SpawnGhost(animator, settings.ghostMaterial, style, direction, true, effects);
    }

    private void SpawnGhost(Animator animator, Material material, SDashGhostStyle style, Vector3 direction,
        bool flare, CPlayerSkillEffects effects)
    {
        if (_source == null || _source._CHARACTER != animator.gameObject)
        {
            _source = new CPlayerDashGhost.CSource(animator.gameObject);
        }

        CPlayerDashGhost ghost = CPlayerDashGhost.Spawn(_source, material, style.lifetime,
            style.alpha, style.smear, style.hold, direction, flare);
        if (ghost != null)
        {
            effects.Add(ghost.gameObject);
        }
    }

    private void SpawnShock(GameObject prefab, Vector3 position, float height, CPlayerSkillEffects effects)
    {
        if (prefab != null)
        {
            effects.Spawn(prefab, position + Vector3.up * height, Quaternion.identity);
        }
    }
}
