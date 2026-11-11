using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CEspoirBossFxDriver))]
public class CEspoirBossFxEditor : Editor
{
    private int _selectedAttack = 1;
    private bool _connections;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField("보스 이펙트 조절", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("공격을 선택한 뒤 장판이나 궤적을 조절하세요. 피해 범위와 보스 모션은 바뀌지 않습니다.", MessageType.Info);
        if (Application.isPlaying)
            EditorGUILayout.HelpBox("재생 중 수정한 값은 종료하면 돌아갑니다. 조절값은 다음 공격부터 확인해주세요.", MessageType.Warning);

        SerializedProperty attacks = serializedObject.FindProperty("_attacks");
        string[] names = new string[attacks.arraySize];
        for (int i = 0; i < attacks.arraySize; i++)
            names[i] = (i + 1) + ". " + AttackName(
                attacks.GetArrayElementAtIndex(i).FindPropertyRelative("state").stringValue);
        if (attacks.arraySize > 0)
        {
            _selectedAttack = Mathf.Clamp(_selectedAttack, 0, attacks.arraySize - 1);
            _selectedAttack = EditorGUILayout.Popup("조절할 공격", _selectedAttack, names);
            SerializedProperty entry = attacks.GetArrayElementAtIndex(_selectedAttack);
            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("공격 전체", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("offset"), new GUIContent("위치 (m)"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("scale"), new GUIContent("크기 배율"));
            EditorGUILayout.EndVertical();
            if (entry.FindPropertyRelative("holdDuration").floatValue > 0f)
            {
                EditorGUILayout.HelpBox(
                    "이전 버전의 전체 이펙트 정지 설정이 남아 있습니다. 장판만 유지하려면 이 설정을 해제하고 아래 장판 표시 시간을 사용하세요.",
                    MessageType.Warning);
                if (GUILayout.Button("이전 전체 정지 설정 해제"))
                {
                    entry.FindPropertyRelative("holdTime").floatValue = 0f;
                    entry.FindPropertyRelative("holdDuration").floatValue = 0f;
                }
            }
            EditorGUILayout.Space(8f);
            CEspoirBossFxElementInspector.Draw(entry.FindPropertyRelative("elements"),
                (entry.FindPropertyRelative("prefab").objectReferenceValue as CEspoirBossFxPlayer)?.data,
                (CEspoirBossFxDriver)target);
        }
        EditorGUILayout.Space(10f);
        _connections = EditorGUILayout.Foldout(_connections, "공통 설정 및 연결", true);
        if (_connections)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_animator"), new GUIContent("보스 Animator"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_origin"), new GUIContent("생성 기준"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_scale"), new GUIContent("모든 공격 크기"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_offset"), new GUIContent("모든 공격 위치"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_screenEffects"), new GUIContent("화면 연출"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_effectLights"), new GUIContent("주변 조명"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_clearOnNextAttack"),
                new GUIContent("다음 공격 시 이전 효과 종료"));
            if (attacks.arraySize > 0)
                EditorGUILayout.PropertyField(
                    attacks.GetArrayElementAtIndex(_selectedAttack).FindPropertyRelative("prefab"),
                    new GUIContent("선택한 공격 프리팹"));
        }
        serializedObject.ApplyModifiedProperties();
    }

    private static string AttackName(string state)
    {
        switch (state)
        {
            case "Boss_Attack01_DoubleHook": return "더블훅";
            case "Boss_Attack02_Backspin": return "백스핀";
            case "Boss_Attack03_SlamUppercut": return "슬램 어퍼컷";
            case "Boss_Sub01_RightLeftSlam": return "좌우 내려찍기";
            case "Boss_Sub02_Whirlwind": return "회오리";
            case "Boss_Sub03_DoubleSealSlam": return "봉인 내려찍기";
            case "Boss_Sub04_RingBurst": return "도넛 폭발";
            case "Boss_Main01_OrbSlam": return "구체 내려찍기";
            case "Boss_Main02_SafeZone": return "안전지대";
            case "Boss_Main03_LightningBeam": return "번개 광선";
            default: return state.Replace("Boss_", "");
        }
    }

    [MenuItem("GameObject/Espoir/보스 이펙트 연결", false, 10)]
    private static void Attach()
    {
        GameObject selected = Selection.activeGameObject;
        Animator animator = selected == null ? null : selected.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            EditorUtility.DisplayDialog(
                "보스 이펙트",
                "Hierarchy에서 보스 Animator가 있는 오브젝트를 선택해주세요.",
                "확인"
            );
            return;
        }

        if (animator.GetComponentInChildren<CEspoirBossFxDriver>(true) != null)
        {
            EditorUtility.DisplayDialog("보스 이펙트", "이미 연결되어 있습니다.", "확인");
            return;
        }

        string[] guids = AssetDatabase.FindAssets(
            "BossEffects t:Prefab",
            new[] { "Assets/Resources/Prefabs/Effects/Boss" }
        );
        if (guids.Length != 1)
        {
            EditorUtility.DisplayDialog(
                "보스 이펙트",
                "BossEffects 프리팹을 찾을 수 없습니다. 패키지를 모두 임포트해주세요.",
                "확인"
            );
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            AssetDatabase.GUIDToAssetPath(guids[0])
        );
        GameObject instance = (GameObject)
            PrefabUtility.InstantiatePrefab(prefab, animator.transform);
        Undo.RegisterCreatedObjectUndo(instance, "보스 이펙트 연결");
        Selection.activeGameObject = instance;
    }
}

[CustomEditor(typeof(CEspoirBossFxPlayer))]
public class CEspoirBossFxPlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "_elements", "holdTime", "holdDuration");
        CEspoirBossFxElementInspector.Draw(
            serializedObject.FindProperty("_elements"),
            serializedObject.FindProperty("data").objectReferenceValue as TextAsset
        );
        serializedObject.ApplyModifiedProperties();
    }
}

internal static class CEspoirBossFxElementInspector
{
    private static readonly System.Collections.Generic.Dictionary<TextAsset, CEspoirBossFxClipData> _CLIPS =
        new System.Collections.Generic.Dictionary<TextAsset, CEspoirBossFxClipData>();
    private static readonly System.Collections.Generic.Dictionary<TextAsset, int> _SELECTED =
        new System.Collections.Generic.Dictionary<TextAsset, int>();
    private static bool _solo;

    internal static void Draw(SerializedProperty elements, TextAsset data, CEspoirBossFxDriver driver = null)
    {
        if (data == null) return;
        if (!_CLIPS.TryGetValue(data, out CEspoirBossFxClipData clip))
        {
            clip = CEspoirBossFxClipData.Parse(data.bytes);
            _CLIPS.Add(data, clip);
        }
        var groups = new System.Collections.Generic.List<CEspoirBossFxPlayer.EEffectGroup>();
        var names = new System.Collections.Generic.List<string>();
        for (int i = 0; i < 9; i++)
        {
            var group = (CEspoirBossFxPlayer.EEffectGroup)i;
            if (FindObject(clip, group, false) < 0) continue;
            groups.Add(group);
            names.Add(GetName(group));
        }
        _SELECTED.TryGetValue(data, out int selected);
        selected = Mathf.Clamp(selected, 0, groups.Count - 1);
        EditorGUILayout.LabelField("조절할 효과", EditorStyles.boldLabel);
        int next = GUILayout.SelectionGrid(selected, names.ToArray(), 3,
            GUILayout.Height(Mathf.CeilToInt(names.Count / 3f) * 28f));
        _SELECTED[data] = next;
        var currentGroup = groups[next];
        if (!Application.isPlaying) _solo = false;
        using (new EditorGUI.DisabledScope(!Application.isPlaying || driver == null))
        {
            bool solo = GUILayout.Toggle(_solo, "재생 중 이 효과만 보기", "Button");
            if (solo != _solo || next != selected)
            {
                _solo = solo;
                if (driver != null) driver.PreviewOnly(_solo ? (int)currentGroup : -1);
            }
        }
        if (Application.isPlaying && driver != null)
            driver.PreviewOnly(_solo ? (int)currentGroup : -1);
        EditorGUILayout.Space(6f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(GetName(currentGroup), EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(Description(currentGroup), MessageType.None);
        SerializedProperty entry = FindEntry(elements, clip, currentGroup);
        int radiusIndex = FindObject(clip, currentGroup, true);
        int primary = radiusIndex >= 0 ? radiusIndex : FindObject(clip, currentGroup, false);
        var value = new CEspoirBossFxPlayer.CElementSetting { isGroup = true, group = currentGroup };
        Defaults(clip, primary, value);
        if (entry != null)
        {
            value.hidden = entry.FindPropertyRelative("hidden").boolValue;
            value.offset = entry.FindPropertyRelative("offset").vector3Value;
            value.rotation = entry.FindPropertyRelative("rotation").floatValue;
            value.scale = entry.FindPropertyRelative("scale").floatValue;
            value.overrideRadius = entry.FindPropertyRelative("overrideRadius").boolValue;
            value.innerRadius = entry.FindPropertyRelative("innerRadius").floatValue;
            value.outerRadius = entry.FindPropertyRelative("outerRadius").floatValue;
            value.overrideDuration = entry.FindPropertyRelative("overrideDuration").boolValue;
            value.duration = entry.FindPropertyRelative("duration").floatValue;
            value.keepAfterAttack = entry.FindPropertyRelative("keepAfterAttack").boolValue;
        }
        EditorGUI.BeginChangeCheck();
        value.hidden = !EditorGUILayout.Toggle("표시", !value.hidden);
        using (new EditorGUI.DisabledScope(value.hidden))
        {
            value.offset = EditorGUILayout.Vector3Field(
                new GUIContent("위치 보정", "보스 기준 X: 오른쪽, Y: 위, Z: 앞. 전체 크기 배율 적용 전 값입니다."), value.offset);
            value.rotation = EditorGUILayout.FloatField("좌우 방향 (도)", value.rotation);
            value.scale = Mathf.Max(0.01f, EditorGUILayout.FloatField("크기 배율", value.scale));
            if (radiusIndex >= 0)
            {
                EditorGUILayout.Space(6f);
                value.overrideRadius = EditorGUILayout.Toggle("도넛 본체 반지름 변경", value.overrideRadius);
                if (value.overrideRadius)
                {
                    value.outerRadius = Mathf.Max(0.01f, EditorGUILayout.FloatField("바깥쪽 반지름", value.outerRadius));
                    value.innerRadius = Mathf.Clamp(EditorGUILayout.FloatField("안쪽 반지름", value.innerRadius),
                        0f, value.outerRadius - 0.01f);
                }
                value.overrideDuration = EditorGUILayout.Toggle("장판 표시 시간 변경", value.overrideDuration);
                if (value.overrideDuration)
                    value.duration = Mathf.Max(0.05f, EditorGUILayout.FloatField("장판마다 표시할 초", value.duration));
                value.keepAfterAttack = EditorGUILayout.Toggle("다음 공격에도 장판 유지", value.keepAfterAttack);
                clip.GetElementTiming(radiusIndex, out float start, out float end, out float hold);
                EditorGUILayout.HelpBox("기본 장판 " + (end - start).ToString("0.##") +
                    "초. 등장·퇴장 포함이며, 여러 장판은 각자 등장한 시점부터 계산합니다. 모션과 피해 시간은 바뀌지 않습니다.", MessageType.None);
            }
        }
        if (EditorGUI.EndChangeCheck())
        {
            if (entry == null)
            {
                elements.arraySize++;
                entry = elements.GetArrayElementAtIndex(elements.arraySize - 1);
            }
            Write(entry, value, primary);
        }
        EditorGUILayout.Space(4f);
        if (GUILayout.Button("이 효과만 기본값으로 복원"))
        {
            var reset = new CEspoirBossFxPlayer.CElementSetting { isGroup = true, group = currentGroup };
            Defaults(clip, primary, reset);
            if (entry != null) Write(entry, reset, primary);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.LabelField("위치·반지름에는 공격 전체 크기와 효과 크기 배율이 적용됩니다.", EditorStyles.wordWrappedMiniLabel);
    }

    private static SerializedProperty FindEntry(SerializedProperty elements, CEspoirBossFxClipData clip,
        CEspoirBossFxPlayer.EEffectGroup group)
    {
        for (int i = 0; i < elements.arraySize; i++)
        {
            var entry = elements.GetArrayElementAtIndex(i);
            int index = entry.FindPropertyRelative("objectIndex").intValue;
            bool matches = entry.FindPropertyRelative("isGroup").boolValue
                ? entry.FindPropertyRelative("group").enumValueIndex == (int)group
                : index >= 0 && index < clip.objs.Length &&
                    CEspoirBossFxPlayer.GetGroup(clip.shaders[clip.objs[index].shader].name) == group;
            if (matches) return entry;
        }
        return null;
    }

    private static int FindObject(CEspoirBossFxClipData clip, CEspoirBossFxPlayer.EEffectGroup group, bool radius)
    {
        for (int i = 0; i < clip.objs.Length; i++)
        {
            var shader = clip.shaders[clip.objs[i].shader];
            if (CEspoirBossFxPlayer.GetGroup(shader.name) != group) continue;
            if (!radius || System.Array.Exists(shader.uniforms, value => value.name == "uR0")) return i;
        }
        return -1;
    }

    private static void Defaults(CEspoirBossFxClipData clip, int index, CEspoirBossFxPlayer.CElementSetting value)
    {
        clip.GetElementTiming(index, out float start, out float end, out float hold);
        value.duration = Mathf.Max(0.05f, end - start);
        foreach (var key in clip.objs[index].keys)
        {
            if (key.uni == null) continue;
            foreach (var uniform in key.uni)
            {
                string name = clip.shaders[clip.objs[index].shader].uniforms[uniform.index].name;
                if (name == "uR0") value.innerRadius = uniform.v[0];

                else if (name == "uR") value.outerRadius = uniform.v[0];
            }
            break;
        }
    }

    private static void Write(SerializedProperty entry, CEspoirBossFxPlayer.CElementSetting value, int index)
    {
        entry.FindPropertyRelative("objectIndex").intValue = index;
        entry.FindPropertyRelative("isGroup").boolValue = true;
        entry.FindPropertyRelative("group").enumValueIndex = (int)value.group;
        entry.FindPropertyRelative("hidden").boolValue = value.hidden;
        entry.FindPropertyRelative("offset").vector3Value = value.offset;
        entry.FindPropertyRelative("rotation").floatValue = value.rotation;
        entry.FindPropertyRelative("scale").floatValue = value.scale;
        entry.FindPropertyRelative("overrideRadius").boolValue = value.overrideRadius;
        entry.FindPropertyRelative("innerRadius").floatValue = value.innerRadius;
        entry.FindPropertyRelative("outerRadius").floatValue = value.outerRadius;
        entry.FindPropertyRelative("overrideDuration").boolValue = value.overrideDuration;
        entry.FindPropertyRelative("duration").floatValue = value.duration;
        entry.FindPropertyRelative("keepAfterAttack").boolValue = value.keepAfterAttack;
    }

    private static string GetName(CEspoirBossFxPlayer.EEffectGroup group)
    {
        switch (group)
        {
            case CEspoirBossFxPlayer.EEffectGroup.Ground: return "장판";
            case CEspoirBossFxPlayer.EEffectGroup.Trail: return "궤적";
            case CEspoirBossFxPlayer.EEffectGroup.Flash: return "섬광";
            case CEspoirBossFxPlayer.EEffectGroup.Debris: return "파편";
            case CEspoirBossFxPlayer.EEffectGroup.Shockwave: return "충격파";
            case CEspoirBossFxPlayer.EEffectGroup.Orb: return "구체";
            case CEspoirBossFxPlayer.EEffectGroup.Whirlwind: return "회오리";
            case CEspoirBossFxPlayer.EEffectGroup.Beam: return "번개·광선";
            default: return "보호막";
        }
    }

    private static string Description(CEspoirBossFxPlayer.EEffectGroup group)
    {
        switch (group)
        {
            case CEspoirBossFxPlayer.EEffectGroup.Ground: return "바닥 장판과 해당 테두리를 함께 이동·회전·확대합니다.";
            case CEspoirBossFxPlayer.EEffectGroup.Debris: return "바위 조각과 외곽선을 함께 조절합니다. 조각 하나씩 고를 필요가 없습니다.";
            case CEspoirBossFxPlayer.EEffectGroup.Shockwave: return "퍼지는 충격파와 공간 왜곡을 함께 조절합니다.";
            case CEspoirBossFxPlayer.EEffectGroup.Orb: return "구체 본체와 주변 광채를 함께 조절합니다.";
            case CEspoirBossFxPlayer.EEffectGroup.Whirlwind: return "회오리 벽과 감기는 띠를 함께 조절합니다.";
            case CEspoirBossFxPlayer.EEffectGroup.Beam: return "번개 기둥·광선·번쩍이는 선을 함께 조절합니다.";
            case CEspoirBossFxPlayer.EEffectGroup.Trail: return "주먹을 따라 생기는 공격 궤적을 조절합니다.";
            case CEspoirBossFxPlayer.EEffectGroup.Flash: return "타격 순간 나타나는 섬광을 조절합니다. 카메라 전체 색상 연출과는 별개입니다.";
            default: return "안전지대를 둘러싸는 보호막을 조절합니다.";
        }
    }
}
