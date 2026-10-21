using System.Collections;
using UnityEngine;

public partial class SceneFlowManager
{
    private void TransitionInitialize()
    {
        if (_fadeGroup == null)
        {
            return;
        }

        _fadeGroup.alpha = 0.0f;
        _fadeGroup.blocksRaycasts = false;
        _fadeGroup.interactable = false;

        CLogger.Log("초기화 완료");
    }

    private IEnumerator Co_FadeTo(float targetAlpha, float duration = -1f, bool blockRaycastWhileFading = true)
    {
        if (_fadeGroup == null)
        {
            yield break;
        }
        if (duration < 0f)
        { 
            duration = _fadeDuration;
        }

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        _fadeRoutine = StartCoroutine(Co_Fade_Internal(targetAlpha, duration, blockRaycastWhileFading));
        yield return _fadeRoutine;
        _fadeRoutine = null;
    }

    private IEnumerator Co_Fade_Internal(float targetAlpha, float duration, bool blockRaycastWhileFading)
    {
        float startAlpha = _fadeGroup.alpha;
        _fadeGroup.blocksRaycasts = blockRaycastWhileFading;
        _fadeGroup.interactable = false;

        if (duration <= 0f)
        {
            _fadeGroup.alpha = targetAlpha;
            _fadeGroup.blocksRaycasts = (targetAlpha >= 0.99f);
            yield break;
        }

        float t = 0f;

        while (t < duration)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            float lerp = Mathf.Clamp01(t / duration);
            _fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, lerp);
            yield return null;
        }
        _fadeGroup.alpha = targetAlpha;
        _fadeGroup.blocksRaycasts = (targetAlpha >= 0.99f);
    }
}