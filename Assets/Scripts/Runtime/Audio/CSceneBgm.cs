using UnityEngine;

public class CSceneBgm : MonoBehaviour
{
    [SerializeField] private EBackgroundSound _backgroundSound;

    private void Start()
    {
        if (CSoundManager.Instance == null)
        {
            CLogger.Error($"{name}: SoundManager가 연결되지 않았습니다.");
            return;
        }

        CSoundManager.Instance.PlayBGM(_backgroundSound);
    }
}
