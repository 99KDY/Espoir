using UnityEngine;

public class CPlayerCrown
{
    private CSkillSO _settings;
    private float _availableUntil = float.NegativeInfinity;

    internal float RemainingTime => Mathf.Max(0f, _availableUntil - Time.time);

    internal void Initialize(CSkillSO settings)
    {
        _settings = settings;
    }

    internal void OpenWindow()
    {
        if (_settings != null)
        {
            _availableUntil = Time.time + _settings.CrownWindow;
        }
    }

    internal bool CanUse()
    {
        return _settings != null && Time.time <= _availableUntil;
    }

    internal void Use(CPlayer player)
    {
        player.AddStatus(_settings.CrownStatus.Id);
        int index = player.FindStatusStack(_settings.CrownStatus.Id) - 1;
        _availableUntil = float.NegativeInfinity;

        GameObject effect = _settings.CrownEffects[index];
        Camera camera = Camera.main;
        float yaw = camera != null ? camera.transform.eulerAngles.y : player.transform.eulerAngles.y;
        Quaternion rotation = Quaternion.Euler(0f, yaw - _settings.CrownEffectYaw, 0f);
        Object.Instantiate(effect, player.transform.position, rotation);
    }

    internal CSkillSO GetAttack(CPlayer player, CSkillSO basicAttack)
    {
        if (_settings != null)
        {
            int index = player.FindStatusStack(_settings.CrownStatus.Id) - 1;
            
            if (index >= 0)
            {
                return _settings.EmpoweredAttacks[index];
            }
        }

        return basicAttack;
    }

    internal float GetDamageMultiplier(CPlayer player)
    {
        return _settings != null && player.FindStatusStack(_settings.CrownStatus.Id) > 0
            ? _settings.EmpoweredDamageMultiplier : 1f;
    }

    internal void Clear()
    {
        _availableUntil = float.NegativeInfinity;
    }
}
