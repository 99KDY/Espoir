using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStatus", menuName = "Espoir/StatusSO")]
public class CStatusSO : ScriptableObject
{
    [Tooltip("상태이상 ID")]
    [SerializeField] private int _id;
    [Tooltip("상태이상 이름")]
    [SerializeField] private string _name;
    [Tooltip("상태이상 아이콘")]
    [SerializeField] private Sprite _icon;

    [Tooltip("상태이상 설명")]
    [SerializeField][TextArea(1, 5)] private string _description;
    [Tooltip("상태이상 툴팁")]
    [SerializeField][TextArea(1, 5)] private string _tooltip;

    [Tooltip("상태이상 기본 지속시간")]
    [SerializeField] private float _duration;
    [Tooltip("상태이상 효과")]
    [SerializeField] private List<SStatusAction> _actionList;

    private static Dictionary<int, CStatusSO> _idToStatus = new Dictionary<int, CStatusSO>();
    private static Dictionary<string, CStatusSO> _stringToStatus = new Dictionary<string, CStatusSO>();
    private static bool _isBuilded = false;

    public static void Build(bool force)
    {
        if (force || !_isBuilded) BuildInternal();
    }

    private static void BuildInternal()
    {
        _isBuilded = true;
        _idToStatus.Clear();
        _stringToStatus.Clear();
        CStatusSO[] statuses = Resources.LoadAll<CStatusSO>("StatusSO");
        if (statuses == null || statuses.Length == 0)
        {
            CLogger.Warn($"CStatusSO : 상태이상이 없음");
            return;
        }
        for (int i = 0; i < statuses.Length; i++)
        {
            if (statuses[i] == null)
            {
                CLogger.Error($"CStatusSO : 딕셔너리 생성 중 에러 발생 - {i}");
                continue;
            }
            _idToStatus.Add(statuses[i]._id, statuses[i]);
            _stringToStatus.Add(statuses[i]._name, statuses[i]);
        }

        return;
    }

    public static CStatus Setup(int id, float duration)
    {
        return null;
    }

    public static CStatus Setup(SStatusApply target)
    {
        return Setup(target.statusId, target.duration);
    }
}
