using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 맵에 맞는 인사와 다른 맵으로 이동할 선택지를 구성하는 길잡이 대화 데이터입니다.
/// </summary>
[CreateAssetMenu(fileName = "TravelGuide", menuName = "Espoir/Dialogue/Travel Guide")]
public class CTravelGuideSO : ScriptableObject
{
    /// <summary>
    /// 맵 하나의 이름과 소개, 그 맵에서의 인사말.
    /// </summary>
    [Serializable]
    public class CPlace
    {
        public string mapKey;
        public string areaName;
        public string placeName;
        public string[] about;
        public string[] hello;
    }

    [SerializeField] private List<CPlace> _places = new List<CPlace>();

    [Header("문구 틀 ({0} 영역 이름, {1} 장소 이름)")]
    [SerializeField] private string _menuAskFormat = "{1}이라… 자, 어디로 가겠나?";
    [SerializeField] private string _goChoiceFormat = "{0}으로 보내 줘";
    [SerializeField] private string _aboutChoice = "그곳들은 어떤 곳이지?";
    [SerializeField] private string _byeChoice = "아직은 됐어";
    [SerializeField] private string _aboutHeaderFormat = "{0} — {1}.";
    [SerializeField] private string _goLineFormat = "좋아, {0}이다. 눈을 감게. 금방 도착할 걸세.";
    [SerializeField] private string[] _byeLines = { "그러게. 마음이 정해지면 다시 오게나." };

    public IReadOnlyList<CPlace> Places => _places;

    /// <summary>
    /// 맵 키로 장소 정보를 찾습니다. 없으면 null.
    /// </summary>
    public CPlace FindPlace(string mapKey)
    {
        foreach (CPlace place in _places)
        {
            if (place.mapKey == mapKey) return place;
        }

        return null;
    }

    /// <summary>
    /// 지금 맵 기준의 대사 묶음 목록을 만듭니다. 모르는 맵이면 첫 장소를 기준으로 합니다.
    /// </summary>
    public List<CTalkNode> BuildNodes(string mapKey)
    {
        List<CTalkNode> nodes = new List<CTalkNode>();

        if (_places.Count == 0) return nodes;

        CPlace here = FindPlace(mapKey) ?? _places[0];
        List<CPlace> others = new List<CPlace>();

        foreach (CPlace place in _places)
        {
            if (place != here) others.Add(place);
        }

        nodes.Add(new CTalkNode { id = "hello", lines = here.hello, next = "menu" });

        List<CTalkChoice> choices = new List<CTalkChoice>();

        foreach (CPlace place in others)
        {
            choices.Add(new CTalkChoice
            {
                text = string.Format(_goChoiceFormat, place.areaName, place.placeName),
                to = "go_" + place.mapKey
            });
        }

        choices.Add(new CTalkChoice { text = _aboutChoice, to = "about" });
        choices.Add(new CTalkChoice { text = _byeChoice, to = "bye" });
        nodes.Add(new CTalkNode
        {
            id = "menu",
            ask = string.Format(_menuAskFormat, here.areaName, here.placeName),
            choices = choices.ToArray()
        });

        List<string> aboutLines = new List<string>();

        foreach (CPlace place in others)
        {
            aboutLines.Add(string.Format(_aboutHeaderFormat, place.areaName, place.placeName));
            aboutLines.AddRange(place.about);
        }

        nodes.Add(new CTalkNode { id = "about", lines = aboutLines.ToArray(), next = "menu" });
        nodes.Add(new CTalkNode { id = "bye", lines = _byeLines, end = ETalkEnd.Close });

        foreach (CPlace place in others)
        {
            string line = string.Format(_goLineFormat, place.areaName, place.placeName);
            nodes.Add(new CTalkNode
            {
                id = "go_" + place.mapKey,
                lines = new[] { line },
                end = ETalkEnd.Travel,
                dest = place.mapKey
            });
        }

        return nodes;
    }

    /// <summary>
    /// 길잡이가 안내할 장소 목록을 설정합니다.
    /// </summary>
    public void SetPlaces(List<CPlace> places)
    {
        _places = places;
    }
}
