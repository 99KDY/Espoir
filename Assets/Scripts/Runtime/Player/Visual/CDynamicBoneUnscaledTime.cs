using UnityEngine;

/// <summary>
/// 일시정지 중 메뉴 모델의 DynamicBone과 이동 바람을 갱신합니다.
/// </summary>
public class CDynamicBoneUnscaledTime : MonoBehaviour
{
    // DynamicBone 기본 모드의 60Hz 기준과 프레임 지연 보정 한계를 유지한다.
    private const float _BASE_RATE = 60f;
    private const float _MAX_STEP = 2f;

    private DynamicBone[] _bones;
    private DynamicBone.UpdateMode[] _modes;
    private float[] _rates;
    private Vector3[] _gravities;
    private Vector3[] _forces;
    private float[] _elasticities;
    private CDynamicBoneHairWind _wind;
    private bool _wasManual;

    private void OnEnable()
    {
        _bones = GetComponentsInChildren<DynamicBone>(true);
        _modes = new DynamicBone.UpdateMode[_bones.Length];
        _rates = new float[_bones.Length];
        _gravities = new Vector3[_bones.Length];
        _forces = new Vector3[_bones.Length];
        _elasticities = new float[_bones.Length];

        for (int i = 0; i < _bones.Length; i++)
        {
            DynamicBone bone = _bones[i];
            _modes[i] = bone.m_UpdateMode;
            _rates[i] = bone.m_UpdateRate;
            _gravities[i] = bone.m_Gravity;
            _forces[i] = bone.m_Force;
            _elasticities[i] = bone.m_Elasticity;

            // 고정 주기와 화면 주기의 차이로 생기는 떨림을 막기 위해 프레임당 한 번 갱신한다.
            bone.m_UpdateMode = DynamicBone.UpdateMode.UnscaledTime;
            bone.m_UpdateRate = 0f;
        }

        _wind = GetComponentInChildren<CDynamicBoneHairWind>(true);
        if (_wind != null)
        {
            _wasManual = _wind.IsManual;
            _wind.IsManual = true;
        }
    }

    private void OnDisable()
    {
        if (_wind != null) _wind.IsManual = _wasManual;
        if (_bones == null) return;

        for (int i = 0; i < _bones.Length; i++)
        {
            DynamicBone bone = _bones[i];
            if (bone == null) continue;

            bone.m_UpdateMode = _modes[i];
            bone.m_UpdateRate = _rates[i];
            bone.m_Gravity = _gravities[i];
            bone.m_Force = _forces[i];
            bone.m_Elasticity = _elasticities[i];
            bone.UpdateParameters();
        }
    }

    /// <summary>
    /// 모델 자세를 적용한 뒤, LateUpdate 전에 전달받은 시간으로 흔들림을 갱신합니다.
    /// </summary>
    public void Step(float deltaTime)
    {
        if (_bones == null) return;

        // 시간 배율이 누적되지 않도록 원래 힘으로 복원한 뒤 바람을 더한다.
        for (int i = 0; i < _bones.Length; i++)
        {
            if (_bones[i] != null) _bones[i].m_Force = _forces[i];
        }

        if (_wind != null && _wind.isActiveAndEnabled) _wind.Step(deltaTime);

        float scale = Mathf.Min(Mathf.Max(deltaTime, 0f) * _BASE_RATE, _MAX_STEP);

        for (int i = 0; i < _bones.Length; i++)
        {
            DynamicBone bone = _bones[i];
            if (bone == null) continue;

            bone.m_Gravity = _gravities[i] * scale;
            bone.m_Force *= scale;
            bone.m_Elasticity = Mathf.Min(1f, _elasticities[i] * scale);
            bone.UpdateParameters();
        }
    }
}
