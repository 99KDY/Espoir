using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대사 묶음이 끝났을 때 할 일.
/// </summary>
public enum ETalkEnd
{
    None = 0,
    Close = 1,
    Travel = 2,
    Summon = 3,
    Unsummon = 4,
    Shop = 5,
}

public enum ETalkQuestAction
{
    [InspectorName("없음")] None = 0,
    [InspectorName("수락")] Accept = 1,
    [InspectorName("보고")] Report = 2,
}

/// <summary>
/// 선택지 한 줄. to 는 고르면 이어질 대사 묶음 id.
/// </summary>
[Serializable]
public class CTalkChoice
{
    public string text;
    public string to;
    [InspectorName("퀘스트")] public ETalkQuestAction questAction;
}

/// <summary>
/// 대사 묶음 하나. lines 를 차례로 보여 주거나, ask 를 보여 주고 choices 에서 고르게 한다.
/// 끝나면 next 로 이어지거나 end 를 실행한다(dest 는 이동할 맵 키).
/// </summary>
[Serializable]
public class CTalkNode
{
    public string id;
    public string[] lines;
    public string ask;
    public CTalkChoice[] choices;
    public string next;
    public ETalkEnd end;
    public string dest;

    public bool HasChoices => choices != null && choices.Length > 0;

    public int LineCount => lines != null && lines.Length > 0 ? lines.Length : 1;

    /// <summary>
    /// index 번째로 보여 줄 문장을 반환합니다. 줄이 없으면 질문(ask)을 반환합니다.
    /// </summary>
    public string GetLine(int index)
    {
        if (lines != null && lines.Length > 0)
        {
            return lines[Mathf.Clamp(index, 0, lines.Length - 1)];
        }

        return ask ?? string.Empty;
    }
}

/// <summary>
/// 퀘스트 진행 상태에 따라 대화의 시작 지점을 정합니다.
/// </summary>
[Serializable]
public class CTalkQuestLink
{
    public CQuestSO quest;
    public string offerNode;
    public string progressNode;
    public string reportNode;
    public string doneNode;

    /// <summary>
    /// 지금 진행 단계에 맞는 시작 묶음 id. 정하지 않았으면 null.
    /// </summary>
    public string GetStartNode()
    {
        if (quest == null) return null;

        string node = CQuestManager.GetState(quest) switch
        {
            EQuestState.Active => progressNode,
            EQuestState.Reportable => reportNode,
            EQuestState.Completed => doneNode,
            _ => CQuestManager.CanAccept(quest) ? offerNode : null,
        };
        return string.IsNullOrEmpty(node) ? null : node;
    }

    /// <summary>
    /// 선택지에 지정된 퀘스트 동작을 처리합니다.
    /// </summary>
    public void OnChoiceConfirmed(ETalkQuestAction action)
    {
        if (quest == null) return;

        if (action == ETalkQuestAction.Accept) CQuestManager.Accept(quest);
        else if (action == ETalkQuestAction.Report) CQuestManager.Report(quest);
    }
}

/// <summary>
/// NPC 한 명의 대화 데이터. 시작 묶음과 보스가 깨어 있을 때의 시작 묶음을 따로 둘 수 있다.
/// 퀘스트를 연결하면 진행 단계에 맞는 묶음으로 시작한다.
/// </summary>
[CreateAssetMenu(fileName = "TalkScript", menuName = "Espoir/Dialogue/Talk Script")]
public class CTalkScriptSO : ScriptableObject
{
    [SerializeField] private string _startNode = "hello";
    [SerializeField] private string _summonedStartNode;
    [SerializeField] private List<CTalkNode> _nodes = new List<CTalkNode>();
    [SerializeField] private CTalkQuestLink _questLink = new CTalkQuestLink();

    public string StartNode => _startNode;
    public IReadOnlyList<CTalkNode> Nodes => _nodes;

    public CTalkQuestLink QuestLink => _questLink;

    /// <summary>
    /// 대화를 시작할 묶음 id 를 반환합니다. 보스가 소환돼 있고 따로 정한 묶음이 있으면 그것을,
    /// 퀘스트가 연결돼 있으면 진행 단계에 맞는 묶음을 씁니다.
    /// </summary>
    public string GetStartNode(bool isBossSummoned)
    {
        if (isBossSummoned && !string.IsNullOrEmpty(_summonedStartNode)) return _summonedStartNode;

        string questNode = _questLink != null ? _questLink.GetStartNode() : null;
        if (questNode != null) return questNode;
        return _startNode;
    }

    /// <summary>
    /// id 로 대사 묶음을 찾습니다. 없으면 null.
    /// </summary>
    public CTalkNode Find(string id)
    {
        foreach (CTalkNode node in _nodes)
        {
            if (node.id == id) return node;
        }

        return null;
    }

    /// <summary>
    /// 시작 대사와 보스 소환 후 대사, 대사 목록을 설정합니다.
    /// </summary>
    public void SetData(string startNode, string summonedStartNode, List<CTalkNode> nodes)
    {
        _startNode = startNode;
        _summonedStartNode = summonedStartNode;
        _nodes = nodes;
    }
}
