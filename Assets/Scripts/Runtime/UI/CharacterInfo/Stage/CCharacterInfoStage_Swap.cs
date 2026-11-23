using UnityEngine;

public partial class CCharacterInfoStage
{
    private const int _HELPER_SWAP = 0;
    private const int _HELPER_HOLD = 1;
    private const float _MIN_SPRING_RATE = 0.05f;
    private const float _SETTLE_STEP = 1f / 60f;
    private const int _SETTLE_STEPS = 60;
    private const float _PROTECT_MIN_GLOW = 0.02f;

    private static readonly int _TOON_LIGHT_DIR = Shader.PropertyToID("_ToonLightDir");

    private bool _isSwapOn;
    private bool _hasSwapHit;
    private bool _isPlayerCopyHidden;
    private float _swapTime;
    private CCharacterInfoStageSettingsSO.SSwapKey[] _swapKeys;
    private CCharacterInfoStageSettingsSO.SSwapKey _swapKey;
    private Vector3 _swapFocus;
    private Vector4 _protect;

    private void StartSwap()
    {
        if (_helperCopy == null)
        {
            CLogger.Warn($"{name}: 프라도나 사본을 만들 수 없어 교체 연출을 건너뜁니다.");
            return;
        }

        CCharacterInfoStageSettingsSO.SSwap swap = _settings.Swap;
        _isSwapOn = true;
        _hasSwapHit = false;
        _isPlayerCopyHidden = false;
        _swapTime = 0f;
        _playRate = 1f;
        _protect = Vector4.zero;
        _helperWarmFrames = 0;

        SnapFace();
        FitSwapCameras();
        _swapKey = _swapKeys[0];
        FadeTo(_SWAP_OUT_INDEX, _settings.Motion.swapBlend);
        _pose = _HELPER_TAB;
        _nextHold = -1;

        _helperCopy.SetActive(true);
        _helperCopy.transform.SetPositionAndRotation(_feet, Quaternion.LookRotation(_baseForward, Vector3.up));
        if (_helperMark != null)
        {
            _helperMark.Clear();
            _helperMark.SetGlow(false);
        }

        SetHelperMotion(_HELPER_SWAP, 0f);

        if (_helperSprings != null)
        {
            _helperSprings.ExternalFlow = Vector3.zero;
            _helperSprings.ResetPose();
            for (int i = 0; i < _SETTLE_STEPS; i++) _helperSprings.Simulate(_SETTLE_STEP);
        }

        _flash = Mathf.Max(_flash, swap.startFlash);
    }

    private void StopSwap(int hold)
    {
        if (!_isSwapOn) return;

        _isSwapOn = false;
        _isPlayerCopyHidden = false;
        _playRate = 1f;
        _protect = Vector4.zero;

        if (_helperMark != null)
        {
            _helperMark.SetGlow(false);
            _helperMark.Clear();
        }

        if (_helperSprings != null) _helperSprings.ExternalFlow = Vector3.zero;
        if (_helperCopy != null) _helperCopy.SetActive(false);
        if (hold >= 0) SnapHold(hold);
    }

    private void LeaveSwap(int tab)
    {
        CCharacterInfoStageSettingsSO.SSwap swap = _settings.Swap;
        StopSwap(tab);
        SetShotTarget(tab, true);
        ClearQueue();
        _flash = swap.leaveFlash;
        _zoom = swap.leaveZoom;
        _impactTime = -1f;
        _nextWord = tab;
        _haloPulse = 1f;
        After(_settings.Look.sweepDelay, EQueued.Sweep, 0f);
    }

    private float RateAt(float time)
    {
        Vector2[] rate = _settings.Swap.rate;
        if (rate == null || rate.Length == 0) return 1f;
        if (rate.Length == 1) return rate[0].y;

        int i = 0;
        while (i < rate.Length - 2 && time >= rate[i + 1].x) i++;
        Vector2 a = rate[i];
        Vector2 b = rate[i + 1];
        return Mathf.Lerp(a.y, b.y, Mathf.Clamp01((time - a.x) / Mathf.Max(1e-6f, b.x - a.x)));
    }

    private void TickSwap(float deltaTime)
    {
        CCharacterInfoStageSettingsSO.SSwap swap = _settings.Swap;

        // 짧은 감속 구간을 건너뛰지 않도록 클립 시간을 나누어 진행한다.
        float remain = deltaTime;
        float time = _swapTime;
        float timeStep = Mathf.Max(0.0001f, swap.rateStep);
        while (remain > 1e-6f)
        {
            float rate = Mathf.Max(0.001f, RateAt(time));
            float step = Mathf.Min(remain, timeStep / rate);
            time += step * rate;
            remain -= step;
        }

        _playRate = deltaTime > 1e-6f ? (time - _swapTime) / deltaTime : RateAt(time);
        _swapTime = time;

        if (!_hasSwapHit && _swapTime >= swap.hitTime)
        {
            _hasSwapHit = true;
            OnSwapHit();
        }

        if (_helperMark != null) _helperMark.SetGlow(_swapTime >= swap.glowTime);
        _isPlayerCopyHidden = _swapTime >= swap.hideTime;

        int i = 0;
        while (i < _swapKeys.Length - 1 && _swapTime >= _swapKeys[i + 1].time) i++;
        CCharacterInfoStageSettingsSO.SSwapKey a = _swapKeys[i];
        _swapKey = a;

        if (i + 1 < _swapKeys.Length && !_swapKeys[i + 1].isCut)
        {
            CCharacterInfoStageSettingsSO.SSwapKey b = _swapKeys[i + 1];
            float t = Mathf.Clamp01((_swapTime - a.time) / Mathf.Max(1e-4f, b.time - a.time));
            t = t * t * (3f - 2f * t);
            _swapKey.lookAt = Vector3.Lerp(a.lookAt, b.lookAt, t);
            _swapKey.azimuth = Mathf.Lerp(a.azimuth, b.azimuth, t);
            _swapKey.distance = Mathf.Lerp(a.distance, b.distance, t);
            _swapKey.heightOffset = Mathf.Lerp(a.heightOffset, b.heightOffset, t);
            _swapKey.fieldOfView = Mathf.Lerp(a.fieldOfView, b.fieldOfView, t);
            _swapKey.roll = Mathf.Lerp(a.roll, b.roll, t);
            _swapKey.blur = Mathf.Lerp(a.blur, b.blur, t);
        }

        _swapFocus = StageToWorld(_swapKey.lookAt);
    }

    private void OnSwapHit()
    {
        CCharacterInfoStageSettingsSO.SSwap swap = _settings.Swap;
        _flash = swap.hitFlash;
        _zoom = swap.hitZoom;
        _impactTime = 0f;
        _haloPulse = 1f;
        _sweep = 0f;
        _nextWord = _HELPER_TAB;
        After(swap.hitShakeDelay, EQueued.Shake, swap.hitShake);
        HelperRevealed?.Invoke();
    }

    private void GetSwapCamera(float shake, out Vector3 position, out Vector3 lookAt)
    {
        CCharacterInfoStageSettingsSO.SSwap swap = _settings.Swap;
        float angle = (_swapKey.azimuth + _mouse.x * swap.mouseAzimuth) * Mathf.Deg2Rad;
        Vector3 stage = _swapKey.lookAt + new Vector3(Mathf.Sin(angle) * _swapKey.distance,
            _swapKey.heightOffset + _mouse.y * swap.mouseHeight + Mathf.Sin(_time * 61f) * shake * 0.012f,
            Mathf.Cos(angle) * _swapKey.distance);
        position = StageToWorld(stage);
        lookAt = _swapFocus;
    }

    private void FitSwapCameras()
    {
        CCharacterInfoStageSettingsSO.SSwap swap = _settings.Swap;
        _swapKeys = (CCharacterInfoStageSettingsSO.SSwapKey[])swap.keys.Clone();

        for (int start = 0; start < _swapKeys.Length;)
        {
            int end = start + 1;
            while (end < _swapKeys.Length && !_swapKeys[end].isCut) end++;

            foreach (Vector2 fit in swap.fitTries)
            {
                bool isClear = true;
                for (int i = start; i < end && isClear; i++) isClear = IsSwapKeyClear(_swapKeys[i], fit.x, fit.y);
                if (!isClear) continue;

                for (int i = start; i < end; i++)
                {
                    _swapKeys[i].azimuth += fit.x;
                    _swapKeys[i].distance *= fit.y;
                }

                break;
            }

            start = end;
        }
    }

    private bool IsSwapKeyClear(CCharacterInfoStageSettingsSO.SSwapKey key, float addAzimuth, float scaleDistance)
    {
        CCharacterInfoStageSettingsSO.SOcclusion occlusion = _settings.Occlusion;
        float angle = (key.azimuth + addAzimuth) * Mathf.Deg2Rad;
        float distance = key.distance * scaleDistance;
        Vector3 lookAt = StageToWorld(key.lookAt);
        Vector3 position = StageToWorld(key.lookAt + new Vector3(Mathf.Sin(angle) * distance, key.heightOffset,
            Mathf.Cos(angle) * distance));

        if (Physics.CheckSphere(position, occlusion.cameraRadius, occlusion.mask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (IsOccluded(position, lookAt)) return false;

        if (distance <= 2.4f) return true;

        Vector3 waist = new Vector3(lookAt.x, _feet.y + 0.9f, lookAt.z);
        Vector3 shin = new Vector3(lookAt.x, _feet.y + 0.45f, lookAt.z);
        return !IsOccluded(position, waist) && !IsOccluded(position, shin);
    }

    private void HoldSwapMotion()
    {
        CMotion motion = _playerMotions[_SWAP_OUT_INDEX];
        motion.time = Mathf.Min(_swapTime, motion.length);
    }

    private void SetHelperMotion(int index, float time)
    {
        for (int i = 0; i < _helperMotions.Length; i++) _helperMotions[i].weight = i == index ? 1f : 0f;
        _helperMotions[index].time = time;
        ApplyMotions(_helperMotions, _helperMixer, _helperGraph);
    }

    private void ApplyHelperLight()
    {
        if (_helperRenderers == null) return;

        float azimuth = _settings.Swap.helperLightAngles.x * Mathf.Deg2Rad;
        float elevation = _settings.Swap.helperLightAngles.y * Mathf.Deg2Rad;
        Vector3 local = new Vector3(-Mathf.Sin(azimuth) * Mathf.Cos(elevation), Mathf.Sin(elevation),
            Mathf.Cos(azimuth) * Mathf.Cos(elevation));
        Vector3 direction = _helperCopy.transform.TransformDirection(local).normalized;
        Vector4 value = new Vector4(direction.x, direction.y, direction.z, 1f);

        for (int i = 0; i < _helperRenderers.Length; i++)
        {
            Renderer target = _helperRenderers[i];
            if (target == null) continue;

            target.GetPropertyBlock(_block);
            _block.SetVector(_TOON_LIGHT_DIR, value);
            target.SetPropertyBlock(_block);
        }
    }

    private void StepHelper(float deltaTime)
    {
        CCharacterInfoStageSettingsSO.SSwap swap = _settings.Swap;

        if (_swapTime < swap.holdTime)
        {
            SetHelperMotion(_HELPER_SWAP, _swapTime);
        }
        else
        {
            SetHelperMotion(_HELPER_HOLD, _swapTime - swap.holdTime);
        }

        ApplyHelperLight();

        float step = deltaTime * Mathf.Max(_MIN_SPRING_RATE, _playRate);
        if (_helperMark != null) _helperMark.Tick(step);

        if (_helperSprings != null)
        {
            float since = _swapTime - swap.glowTime;
            Vector3 flow = Vector3.zero;

            if (since >= 0f && since < swap.gustLength && _helperMark != null && _helperHips != null)
            {
                float power = swap.gustStrength * Mathf.Exp(-since / swap.gustDecay);
                Vector3 back = _helperHips.position - _helperMark.ProtectPoint;
                back.y = 0f;
                flow = back.normalized * (swap.gustBack * power) + Vector3.up * (swap.gustUp * power);
            }

            _helperSprings.ExternalFlow = flow;
            _helperSprings.Simulate(step);
        }

        // 깊이를 기록하지 않는 손 이펙트를 배경 흐림에서 제외한다.
        _protect = Vector4.zero;
        if (_helperMark == null || _helperMark.Glow <= _PROTECT_MIN_GLOW) return;

        Vector3 point = _helperMark.ProtectPoint;
        Vector3 view = _camera.WorldToViewportPoint(point);
        float distance = Vector3.Distance(point, _camera.transform.position);
        float radius = swap.protectRadius / (2f * distance * Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad * 0.5f));
        _protect = new Vector4(view.x, view.y, radius, Mathf.Min(1f, _helperMark.Glow));
    }
}
