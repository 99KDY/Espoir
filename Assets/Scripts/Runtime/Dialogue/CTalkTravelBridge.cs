using System;
using UnityEngine;

/// <summary>
/// 대화의 맵 키를 씬 ID로 바꾸어 CSceneFlowManager.Travel에 전달합니다.
/// 씬 전환 후에도 유지되는 SceneFlowManager 오브젝트에 연결합니다.
/// </summary>
public class CTalkTravelBridge : MonoBehaviour
{
    [Serializable]
    private struct SDestination
    {
        public string mapKey;
        public ESceneId sceneId;
    }

    [SerializeField] private SDestination[] _destinations = new SDestination[0];
    private bool _isRegistered;

    // CSceneFlowManager.Awake에서 싱글톤이 정해진 뒤 등록한다.
    private void Start()
    {
        // 새 씬의 중복 매니저는 제거되므로 살아남은 인스턴스만 등록한다.
        if (CSceneFlowManager.Instance == null || CSceneFlowManager.Instance.gameObject != gameObject) return;

        CTalkTravel.Handler = Travel;
        _isRegistered = true;
    }

    private void OnDestroy()
    {
        if (!_isRegistered) return;

        _isRegistered = false;
        if (CTalkTravel.Handler == (Func<string, bool>)Travel) CTalkTravel.Handler = null;
    }

    private bool Travel(string mapKey)
    {
        if (CSceneFlowManager.Instance == null) return false;

        foreach (SDestination destination in _destinations)
        {
            if (destination.mapKey == mapKey)
            {
                return CSceneFlowManager.Instance.Travel(destination.sceneId);
            }
        }

        CLogger.Warn($"맵 이동: 등록되지 않은 목적지 '{mapKey}'");
        return false;
    }
}
