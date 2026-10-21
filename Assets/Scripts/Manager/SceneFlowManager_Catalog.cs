using UnityEngine;

public partial class SceneFlowManager
{
    [ContextMenu("BuildMaps (Rebuild Catalog)")]
    public void BuildMaps()
    {
        // 이번에 빌드된 데이터 지우기
        _idToName.Clear();
        _nameToId.Clear();

        for (int i = 0; i < _scenes.Count; i++)
        {
            SceneEntry e = _scenes[i];

            if (e == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(e.SceneName))
            {
                CLogger.Warn($"엔트리가 비어있음 - 확인할 것");
                continue;
            }

            if (_idToName.ContainsKey(e.Id))
            {
                CLogger.Warn($"ID 중복 {e.Id} / 기존 = {_idToName[e.Id]} / 신규 = {e.SceneName}");
                continue;
            }

            if (_nameToId.ContainsKey(e.SceneName))
            {
                CLogger.Warn($"씬 이름 중복 {e.SceneName} / 기존 = {_nameToId[e.SceneName]} / 신규 = {e.Id}");
                continue;
            }

            _idToName.Add(e.Id, e.SceneName);
            _nameToId.Add(e.SceneName, e.Id);
        }

        CLogger.Log("씬 카탈로그 빌드");
        CLogger.Log($"리스트 카운트 {_scenes.Count}");
        CLogger.Log($"맵 카운트 (ID -> Name) {_idToName.Count}");
        CLogger.Log($"맵 카운트 (Name -> ID) {_nameToId.Count}");
    }

    public bool TryGetSceneName(ESceneId id, out string sceneName)
    {
        return _idToName.TryGetValue(id, out sceneName);
    }

    public string GetSceneName(ESceneId id)
    {
        if (_idToName.TryGetValue(id, out string name))
        {
            return name;
        }

        return string.Empty;
    }
    public bool TryGetSceneId(string sceneName, out ESceneId id)
    {
        return _nameToId.TryGetValue(sceneName, out id);
    }
}
