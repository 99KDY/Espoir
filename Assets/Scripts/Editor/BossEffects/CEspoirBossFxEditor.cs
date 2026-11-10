using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CEspoirBossFxDriver))]
public class CEspoirBossFxEditor : Editor
{
    private bool[] _expanded;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox(
            "보스 Animator 아래에 두면 자동 연결됩니다. 공격 모션과 피해 판정은 변경하지 않습니다.",
            MessageType.Info
        );
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("_animator"),
            new GUIContent("보스 Animator (자동)")
        );
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("_origin"),
            new GUIContent("생성 기준 (자동)")
        );
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("_scale"),
            new GUIContent("전체 크기")
        );
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("_offset"),
            new GUIContent("전체 위치 보정 (m)")
        );
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("_screenEffects"),
            new GUIContent("화면 연출")
        );
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("_effectLights"),
            new GUIContent("주변 조명")
        );
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("_clearOnNextAttack"),
            new GUIContent("다음 공격 시 이전 이펙트 종료")
        );
        SerializedProperty attacks = serializedObject.FindProperty("_attacks");
        if (_expanded == null || _expanded.Length != attacks.arraySize)
        {
            _expanded = new bool[attacks.arraySize];
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("공격별 조절", EditorStyles.boldLabel);
        for (int i = 0; i < attacks.arraySize; i++)
        {
            SerializedProperty entry = attacks.GetArrayElementAtIndex(i);
            SerializedProperty prefab = entry.FindPropertyRelative("prefab");
            string label = entry.FindPropertyRelative("state").stringValue.Replace("Boss_", "");
            _expanded[i] = EditorGUILayout.Foldout(_expanded[i], (i + 1) + ". " + label, true);
            if (!_expanded[i])
                continue;
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(prefab, new GUIContent("이펙트 프리팹"));
            EditorGUILayout.PropertyField(
                entry.FindPropertyRelative("scale"),
                new GUIContent("크기 배율")
            );
            EditorGUILayout.PropertyField(
                entry.FindPropertyRelative("offset"),
                new GUIContent("위치 보정 (m)")
            );
            EditorGUILayout.PropertyField(
                entry.FindPropertyRelative("holdTime"),
                new GUIContent("머무를 시점 (초)")
            );
            EditorGUILayout.PropertyField(
                entry.FindPropertyRelative("holdDuration"),
                new GUIContent("추가로 머무는 시간 (초)")
            );
            EditorGUILayout.HelpBox(
                "추가 시간 0은 원본입니다. 설정한 시점의 이펙트 전체를 잠시 유지합니다. 보스와 피해 판정 시간은 바뀌지 않습니다.",
                MessageType.None
            );
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
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
