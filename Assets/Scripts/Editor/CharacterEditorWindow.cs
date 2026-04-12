using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CharacterEditorWindow : EditorWindow
{
    private Vector2 scrollPos;
    private CharacterData selectedData;
    private int selectedSkillIndex = 0;

    [MenuItem("Window/Battle/Character Editor")]
    public static void ShowWindow()
    {
        GetWindow<CharacterEditorWindow>("캐릭터 편집기");
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        // --- 왼쪽: 캐릭터 리스트 ---
        DrawLeftPanel();

        // --- 오른쪽: 세부 편집창 ---
        DrawRightPanel();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(200), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("캐릭터 목록", EditorStyles.boldLabel);

        if (GUILayout.Button("새 캐릭터 생성", GUILayout.Height(30)))
        {
            CreateNewCharacter();
        }

        EditorGUILayout.Space(10);

        string[] guids = AssetDatabase.FindAssets("t:CharacterData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);

            if (GUILayout.Toggle(selectedData == data, data.characterName, "Button", GUILayout.Height(25)))
            {
                selectedData = data;
                GUI.FocusControl(null); // 포커스 해제하여 즉시 반영
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawRightPanel()
    {
        if (selectedData == null)
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("편집할 캐릭터를 선택하세요.");
            EditorGUILayout.EndVertical();
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

        // 기본 정보 섹션
        EditorGUILayout.LabelField($"◆ {selectedData.characterName} 편집", EditorStyles.whiteLargeLabel);
        EditorGUILayout.Space(10);

        selectedData.characterName = EditorGUILayout.TextField("캐릭터 이름", selectedData.characterName);
        selectedData.portraitSprite = (Sprite)EditorGUILayout.ObjectField("초상화", selectedData.portraitSprite, typeof(Sprite), false);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("기본 스탯 (0강 기준)", EditorStyles.boldLabel);
        selectedData.maxHp = EditorGUILayout.FloatField("최대 체력", selectedData.maxHp);
        selectedData.maxMental = EditorGUILayout.FloatField("최대 정신력", selectedData.maxMental);
        selectedData.baseAttack = EditorGUILayout.FloatField("기본 공격력", selectedData.baseAttack);

        EditorGUILayout.Space(15);

        // 스킬 편집 섹션 (탭 방식)
        EditorGUILayout.LabelField("액티브 스킬 설정", EditorStyles.boldLabel);
        string[] skillNames = new string[4];
        for (int i = 0; i < 4; i++)
            skillNames[i] = string.IsNullOrEmpty(selectedData.activeSkills[i]?.skillName) ? $"스킬 {i + 1}" : selectedData.activeSkills[i].skillName;

        selectedSkillIndex = GUILayout.Toolbar(selectedSkillIndex, skillNames);

        DrawSkillEditor(selectedData.activeSkills[selectedSkillIndex]);

        EditorGUILayout.Space(20);
        if (GUI.changed)
        {
            EditorUtility.SetDirty(selectedData);
            AssetDatabase.SaveAssets();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    private void DrawSkillEditor(SkillInfo skill)
    {
        if (skill == null) return;

        EditorGUILayout.BeginVertical(GUI.skin.textArea);
        skill.skillName = EditorGUILayout.TextField("스킬명", skill.skillName);
        skill.skillIcon = (Sprite)EditorGUILayout.ObjectField("아이콘", skill.skillIcon, typeof(Sprite), false);
        skill.baseCost = EditorGUILayout.IntField("기본 코스트", skill.baseCost);

        EditorGUILayout.Space(10);

        // 레벨별 데이터 3단계를 가로로 나열하거나 세로로 명확히 표시
        for (int i = 0; i < 3; i++)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField($"{i + 1}단계 강화 데이터", EditorStyles.miniBoldLabel);

            var levelData = skill.levels[i];
            levelData.overrideCost = EditorGUILayout.IntField("코스트 (-1은 기본값)", levelData.overrideCost);
            levelData.targetType = (TargetType)EditorGUILayout.EnumPopup("타겟 범위", levelData.targetType);

            // 이펙트 리스트
            if (GUILayout.Button("+ 효과 추가", GUILayout.Width(100)))
                levelData.effects.Add(new EffectEntry());

            for (int e = 0; e < levelData.effects.Count; e++)
            {
                EditorGUILayout.BeginHorizontal();
                var effect = levelData.effects[e];
                effect.type = (EffectType)EditorGUILayout.EnumPopup(effect.type, GUILayout.Width(100));
                effect.multiplier = EditorGUILayout.FloatField("배율", effect.multiplier, GUILayout.Width(80));
                effect.fixedValue = EditorGUILayout.FloatField("고정치", effect.fixedValue, GUILayout.Width(80));
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    levelData.effects.RemoveAt(e);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
    }

    private void CreateNewCharacter()
    {
        string path = EditorUtility.SaveFilePanelInProject("새 캐릭터 생성", "NewCharacter", "asset", "데이터 이름을 입력하세요.");
        if (string.IsNullOrEmpty(path)) return;

        CharacterData newData = CreateInstance<CharacterData>();
        // 기본 구조 초기화 로직
        newData.activeSkills = new SkillInfo[4];
        for (int i = 0; i < 4; i++)
        {
            newData.activeSkills[i] = new SkillInfo { levels = new SkillLevelData[3] };
            for (int j = 0; j < 3; j++) newData.activeSkills[i].levels[j] = new SkillLevelData();
        }

        AssetDatabase.CreateAsset(newData, path);
        AssetDatabase.SaveAssets();
        selectedData = newData;
    }
}