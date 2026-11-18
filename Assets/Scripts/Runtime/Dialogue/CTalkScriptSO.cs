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

/// <summary>
/// 선택지 한 줄. to 는 고르면 이어질 대사 묶음 id.
/// </summary>
[Serializable]
public class CTalkChoice
{
    public string text;
    public string to;
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
/// NPC 한 명의 대화 데이터. 시작 묶음과 보스가 깨어 있을 때의 시작 묶음을 따로 둘 수 있다.
/// </summary>
[CreateAssetMenu(fileName = "TalkScript", menuName = "Espoir/Dialogue/Talk Script")]
public class CTalkScriptSO : ScriptableObject
{
    [SerializeField] private string _startNode = "hello";
    [SerializeField] private string _summonedStartNode;
    [SerializeField] private List<CTalkNode> _nodes = new List<CTalkNode>();

    public IReadOnlyList<CTalkNode> Nodes => _nodes;

    /// <summary>
    /// 대화를 시작할 묶음 id 를 반환합니다. 보스가 소환돼 있고 따로 정한 묶음이 있으면 그것을 씁니다.
    /// </summary>
    public string GetStartNode(bool isBossSummoned)
    {
        if (isBossSummoned && !string.IsNullOrEmpty(_summonedStartNode)) return _summonedStartNode;
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
