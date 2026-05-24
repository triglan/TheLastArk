using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

// ──────────────────────────────────────────────
//  스킬 프리셋 저장용 데이터 (EditorPrefs + JSON)
// ──────────────────────────────────────────────
[System.Serializable]
public class SkillPreset
{
    public string presetName;
    public string skillName;
    public int baseCost;
    public List<SkillLevelPreset> levels = new List<SkillLevelPreset>();
}

[System.Serializable]
public class SkillLevelPreset
{
    public int overrideCost;
    public int targetType;      // enum → int 직렬화
    public List<EffectPreset> effects = new List<EffectPreset>();
}

[System.Serializable]
public class EffectPreset
{
    public int type;            // enum → int 직렬화
    public float multiplier;
    public float fixedValue;
    public bool useActualResult;
}

[System.Serializable]
public class SkillPresetLibrary
{
    public List<SkillPreset> presets = new List<SkillPreset>();
}

// ──────────────────────────────────────────────
//  메인 에디터 창
// ──────────────────────────────────────────────
public class CharacterEditorWindow : EditorWindow
{
    // ── 패널 스크롤 ──
    private Vector2 leftScroll, middleScroll, rightScroll, presetScroll;

    // ── 선택 상태 ──
    private CharacterData selectedData;
    private int selectedSkillIndex = 0; // 0~3: Active, 4: Passive

    // ── 검색 ──
    private string searchText = "";

    // ── 캐릭터 목록 캐시 ──
    private List<CharacterData> cachedCharacterList = new List<CharacterData>();
    private bool isDirtyCache = true;

    // ── 스킬 프리셋 ──
    private SkillPresetLibrary presetLibrary = new SkillPresetLibrary();
    private bool showPresetPanel = false;
    private const string PRESET_KEY = "CharacterEditor_SkillPresets";

    // ── 일괄 이미지 바인딩 ──
    private string iconFolderPath = "";

    [MenuItem("Window/Battle/Character Editor #e")]
    public static void ShowWindow()
    {
        var window = GetWindow<CharacterEditorWindow>("캐릭터 편집기");
        window.minSize = new Vector2(1200, 700);
    }

    private void OnEnable() { LoadPresets(); }
    private void OnFocus() { isDirtyCache = true; }

    // ────────────────────────────────────────────
    //  캐릭터 목록 캐시
    // ────────────────────────────────────────────
    private void RefreshCacheIfNeeded()
    {
        if (!isDirtyCache) return;
        cachedCharacterList.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:CharacterData"))
        {
            CharacterData d = AssetDatabase.LoadAssetAtPath<CharacterData>(
                AssetDatabase.GUIDToAssetPath(guid));
            if (d != null) cachedCharacterList.Add(d);
        }
        isDirtyCache = false;
    }

    // ────────────────────────────────────────────
    //  OnGUI 진입점
    // ────────────────────────────────────────────
    private void OnGUI()
    {
        RefreshCacheIfNeeded();
        EditorGUILayout.BeginHorizontal();
        DrawLeftPanel();
        DrawMiddlePanel();
        DrawRightOverviewPanel();
        EditorGUILayout.EndHorizontal();
    }

    // ════════════════════════════════════════════
    //  왼쪽 패널 — 목록 + 파일 관리
    // ════════════════════════════════════════════
    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(220), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("🔍 캐릭터 검색", EditorStyles.boldLabel);
        searchText = EditorGUILayout.TextField(searchText);

        if (GUILayout.Button("＋ 새 캐릭터 생성", GUILayout.Height(30)))
        {
            CreateNewCharacter();
            isDirtyCache = true;
        }

        EditorGUILayout.Space(5);
        leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
        foreach (CharacterData data in cachedCharacterList)
        {
            if (data == null) continue;
            if (!string.IsNullOrEmpty(searchText) &&
                !data.characterName.ToLower().Contains(searchText.ToLower())) continue;

            EditorGUILayout.BeginHorizontal();
            bool isSelected = (selectedData == data);
            if (GUILayout.Toggle(isSelected, data.characterName, "Button",
                GUILayout.Height(25), GUILayout.ExpandWidth(true)))
            {
                if (!isSelected)
                {
                    selectedData = data;
                    iconFolderPath = "";
                    GUI.FocusControl(null);
                }
            }
            if (GUILayout.Button("C", GUILayout.Width(20), GUILayout.Height(25)))
            {
                DuplicateCharacter(data);
                isDirtyCache = true;
            }
            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(25)))
            {
                DeleteCharacter(data);
                isDirtyCache = true;
                break;          // 삭제 직후 foreach 중단 (잘못된 참조 방지)
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // ════════════════════════════════════════════
    //  중앙 패널 — 상세 편집
    // ════════════════════════════════════════════
    private void DrawMiddlePanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(500), GUILayout.ExpandHeight(true));
        if (selectedData == null)
        {
            EditorGUILayout.HelpBox("캐릭터를 선택하세요.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }
        if (selectedData.activeSkills == null || selectedData.activeSkills.Length != 4)
        {
            EditorGUILayout.HelpBox("activeSkills 데이터가 손상되었습니다.", MessageType.Error);
            if (GUILayout.Button("데이터 복구")) { InitData(selectedData); EditorUtility.SetDirty(selectedData); }
            EditorGUILayout.EndVertical();
            return;
        }

        middleScroll = EditorGUILayout.BeginScrollView(middleScroll);
        EditorGUI.BeginChangeCheck();

        // iconFolderPath가 비어있으면 캐릭터명 기반 기본 경로 자동 채움
        if (string.IsNullOrEmpty(iconFolderPath))
            iconFolderPath = $"Assets/Resources/Characters/Player/Illust/{selectedData.characterName}";

        // ── [NEW] 에셋 이름 변경 ──
        DrawAssetRenameSection();

        // ── 스프라이트 ──
        EditorGUILayout.BeginHorizontal();
        selectedData.portraitSprite = (Sprite)EditorGUILayout.ObjectField(
            "초상화", selectedData.portraitSprite, typeof(Sprite), false, GUILayout.Height(60));
        selectedData.standingSprite = (Sprite)EditorGUILayout.ObjectField(
            "전신 샷", selectedData.standingSprite, typeof(Sprite), false, GUILayout.Height(60));
        EditorGUILayout.EndHorizontal();

        // ── 기본 능력치 ──
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField("기본 능력치 (고정 배율 적용 전)", EditorStyles.boldLabel);
        selectedData.maxHp = EditorGUILayout.FloatField("최대 체력", selectedData.maxHp);
        selectedData.maxMental = EditorGUILayout.FloatField("최대 정신력", selectedData.maxMental);
        selectedData.baseAttack = EditorGUILayout.FloatField("기본 공격력", selectedData.baseAttack);
        EditorGUILayout.EndVertical();

        // ── [NEW] 일괄 아이콘 바인딩 ──
        EditorGUILayout.Space(10);
        DrawBulkIconBindingSection();

        // ── 스킬 탭 ──
        EditorGUILayout.Space(15);
        string[] tabs = { "스킬 1", "스킬 2", "스킬 3", "스킬 4", "패시브" };
        selectedSkillIndex = GUILayout.Toolbar(selectedSkillIndex, tabs);

        SkillInfo currentSkill;
        if (selectedSkillIndex < 4)
        {
            if (selectedData.activeSkills[selectedSkillIndex] == null)
                selectedData.activeSkills[selectedSkillIndex] = CreateDefaultSkill();
            currentSkill = selectedData.activeSkills[selectedSkillIndex];
        }
        else
        {
            if (selectedData.passiveSkill == null)
                selectedData.passiveSkill = CreateDefaultSkill();
            currentSkill = selectedData.passiveSkill;
        }

        // ── [NEW] 프리셋 저장/불러오기 바 ──
        DrawSkillPresetBar(currentSkill);

        // ── 스킬 편집기 ──
        DrawSkillEditor(currentSkill);

        if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(selectedData);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // ────────────────────────────────────────────
    //  캐릭터 이름 섹션
    //  characterName 변경 → .asset 파일명도 즉시 동기화 ({name}_Data)
    // ────────────────────────────────────────────
    private void DrawAssetRenameSection()
    {
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField($"📂 {selectedData.characterName}", EditorStyles.whiteLargeLabel);

        EditorGUI.BeginChangeCheck();
        string newName = EditorGUILayout.TextField("캐릭터 이름", selectedData.characterName);
        if (EditorGUI.EndChangeCheck() && !string.IsNullOrWhiteSpace(newName))
        {
            string trimmed = newName.Trim();
            string assetPath = AssetDatabase.GetAssetPath(selectedData);

            selectedData.characterName = trimmed;
            EditorUtility.SetDirty(selectedData);
            AssetDatabase.SaveAssets();

            // .asset 파일명을 {이름}_Data 로 즉시 동기화
            string assetFileName = $"{trimmed}_Data";
            string error = AssetDatabase.RenameAsset(assetPath, assetFileName);
            if (!string.IsNullOrEmpty(error))
                Debug.LogWarning($"[이름 동기화 실패] {error}");
            else
            {
                AssetDatabase.Refresh();
                isDirtyCache = true;
                iconFolderPath = ""; // 폴더 경로도 새 이름 기반으로 초기화
            }
        }

        EditorGUILayout.HelpBox(
            "이름 입력 시 .asset 파일명이 자동으로 {이름}_Data.asset 으로 동기화됩니다.",
            MessageType.None);

        EditorGUILayout.EndVertical();
    }

    // ────────────────────────────────────────────
    //  일괄 이미지 자동 바인딩
    //
    //  로직: 지정 폴더의 .png 파일을 전부 스캔 →
    //        파일명에서 캐릭터명_ 을 제거한 접미사로 종류 판별
    //        접미사 규칙 (대소문자 무시):
    //          Portrait          → portraitSprite
    //          Illust            → standingSprite
    //          Skill_0           → passiveSkill.skillIcon
    //          Skill_1 ~ Skill_4 → activeSkills[0~3].skillIcon
    //
    //  + 이름 규격화: 폴더 내 이미지를 규칙에 맞게 일괄 이름 변경
    // ────────────────────────────────────────────
    private void DrawBulkIconBindingSection()
    {
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField("🖼 이미지 자동 바인딩", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("이미지 폴더", GUILayout.Width(70));
        iconFolderPath = EditorGUILayout.TextField(iconFolderPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string abs = EditorUtility.OpenFolderPanel("이미지 폴더 선택", Application.dataPath, "");
            if (!string.IsNullOrEmpty(abs) && abs.StartsWith(Application.dataPath))
                iconFolderPath = "Assets" + abs.Substring(Application.dataPath.Length);
        }
        if (GUILayout.Button("자동 경로", GUILayout.Width(65)))
            iconFolderPath = $"Assets/Resources/Characters/Player/Illust/{selectedData.characterName}";
        EditorGUILayout.EndHorizontal();

        string n = selectedData.characterName;
        EditorGUILayout.HelpBox(
            $"폴더 내 파일명에서 캐릭터명 뒷부분으로 자동 분류합니다.\n" +
            $"  Portrait  → 초상화     │  Illust    → 전신샷\n" +
            $"  Skill_0   → 패시브     │  Skill_1~4 → 액티브 스킬\n" +
            $"예) {n}_Portrait.png / {n}_Skill_0.png",
            MessageType.None);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("▶ 전체 이미지 자동 바인딩", GUILayout.Height(26)))
            AutoBindAllImages();

        GUI.color = new Color(1f, 0.85f, 0.4f);
        if (GUILayout.Button("✏ 이름 규격화", GUILayout.Height(26), GUILayout.Width(100)))
            OpenImageRenameWindow();
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void AutoBindAllImages()
    {
        if (selectedData == null) return;
        string folder = iconFolderPath.TrimEnd('/', '\\');

        // 폴더 내 .png 파일을 전부 수집
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("바인딩 실패",
                $"폴더 [{folder}] 에서 스프라이트를 찾을 수 없습니다.\n폴더 경로를 확인하세요.", "확인");
            return;
        }

        // 결과 추적용
        bool foundPortrait = false, foundIllust = false;
        int[] skillResults = new int[5]; // 0=패시브, 1~4=액티브 (0=미매칭, 1=매칭)

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileNameNoExt = Path.GetFileNameWithoutExtension(path);

            // 캐릭터명_ 접두사 제거 후 접미사만 추출 (대소문자 무시)
            string suffix = ExtractSuffix(fileNameNoExt, selectedData.characterName);
            if (suffix == null) continue; // 이 캐릭터와 무관한 파일

            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp == null) continue;

            string suffixLower = suffix.ToLower();

            if (suffixLower == "portrait")
            {
                selectedData.portraitSprite = sp;
                foundPortrait = true;
            }
            else if (suffixLower == "illust")
            {
                selectedData.standingSprite = sp;
                foundIllust = true;
            }
            else if (suffixLower == "skill_0" && selectedData.passiveSkill != null)
            {
                selectedData.passiveSkill.skillIcon = sp;
                skillResults[0] = 1;
            }
            else if (selectedData.activeSkills != null)
            {
                for (int i = 1; i <= 4; i++)
                {
                    if (suffixLower == $"skill_{i}" && selectedData.activeSkills[i - 1] != null)
                    {
                        selectedData.activeSkills[i - 1].skillIcon = sp;
                        skillResults[i] = 1;
                        break;
                    }
                }
            }
        }

        EditorUtility.SetDirty(selectedData);
        AssetDatabase.SaveAssets();

        // ── 결과 리포트 ──
        int totalSkill = 0;
        for (int i = 0; i < 5; i++) totalSkill += skillResults[i];
        int total = (foundPortrait ? 1 : 0) + (foundIllust ? 1 : 0) + totalSkill;

        string skillDetail = "";
        string[] skillLabels = { "패시브(0)", "액티브1", "액티브2", "액티브3", "액티브4" };
        for (int i = 0; i < 5; i++)
            skillDetail += $"  Skill_{i} ({skillLabels[i]}): {(skillResults[i] == 1 ? "✓" : "✗")}\n";

        EditorUtility.DisplayDialog("자동 바인딩 완료",
            $"총 {total} / 7 개 바인딩 완료\n\n" +
            $"초상화:  {(foundPortrait ? "✓" : "✗")}\n" +
            $"전신샷:  {(foundIllust ? "✓" : "✗")}\n\n" +
            $"스킬 아이콘:\n{skillDetail}",
            "확인");
    }

    /// <summary>
    /// 파일명에서 "{캐릭터명}_" 접두사를 제거하고 접미사를 반환합니다.
    /// 캐릭터명과 일치하지 않으면 null을 반환합니다.
    /// 대소문자 무시. 예) "Assassin_Skill_1" → "Skill_1"
    /// </summary>
    private string ExtractSuffix(string fileNameNoExt, string characterName)
    {
        string prefix = characterName + "_";
        if (fileNameNoExt.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            return fileNameNoExt.Substring(prefix.Length);
        return null;
    }

    // ────────────────────────────────────────────
    //  이름 규격화 창
    //  폴더 내 이미지를 미리보기 후 규칙에 맞게 일괄 이름 변경
    // ────────────────────────────────────────────
    private void OpenImageRenameWindow()
    {
        ImageRenameWindow.Open(selectedData.characterName, iconFolderPath);
    }

    // ────────────────────────────────────────────
    //  [NEW] 스킬 프리셋 바
    // ────────────────────────────────────────────
    private void DrawSkillPresetBar(SkillInfo skill)
    {
        EditorGUILayout.BeginHorizontal();
        GUI.color = new Color(0.6f, 1f, 0.8f);
        if (GUILayout.Button("💾 프리셋 저장", GUILayout.Height(22)))
        {
            if (EditorUtility.DisplayDialog("프리셋 저장",
                $"[{skill.skillName}]을 프리셋으로 저장하시겠습니까?", "저장", "취소"))
            {
                presetLibrary.presets.Add(SkillToPreset(skill, skill.skillName));
                SavePresets();
                showPresetPanel = true;
            }
        }
        GUI.color = new Color(0.8f, 0.9f, 1f);
        if (GUILayout.Button($"📂 프리셋 목록 {(showPresetPanel ? "▲" : "▼")}", GUILayout.Height(22)))
            showPresetPanel = !showPresetPanel;
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        if (showPresetPanel) DrawPresetLibraryPanel(skill);
    }

    private void DrawPresetLibraryPanel(SkillInfo targetSkill)
    {
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField("📚 저장된 스킬 프리셋", EditorStyles.boldLabel);

        if (presetLibrary.presets.Count == 0)
        {
            EditorGUILayout.HelpBox("저장된 프리셋이 없습니다.", MessageType.Info);
        }
        else
        {
            presetScroll = EditorGUILayout.BeginScrollView(presetScroll, GUILayout.MaxHeight(160));
            for (int i = presetLibrary.presets.Count - 1; i >= 0; i--)
            {
                var preset = presetLibrary.presets[i];
                EditorGUILayout.BeginHorizontal();
                preset.presetName = EditorGUILayout.TextField(preset.presetName, GUILayout.ExpandWidth(true));

                GUI.color = new Color(0.8f, 0.9f, 1f);
                if (GUILayout.Button("불러오기", GUILayout.Width(65)))
                {
                    if (EditorUtility.DisplayDialog("프리셋 불러오기",
                        $"[{preset.presetName}]을(를) 현재 스킬에 덮어씌우시겠습니까?", "적용", "취소"))
                    {
                        ApplyPreset(preset, targetSkill);
                        EditorUtility.SetDirty(selectedData);
                    }
                }
                GUI.color = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("X", GUILayout.Width(22)))
                {
                    presetLibrary.presets.RemoveAt(i);
                    SavePresets();
                    GUI.color = Color.white;
                    break;
                }
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("전체 삭제", GUILayout.Height(20)))
            {
                if (EditorUtility.DisplayDialog("전체 삭제", "모든 프리셋을 삭제하시겠습니까?", "삭제", "취소"))
                {
                    presetLibrary.presets.Clear();
                    SavePresets();
                }
            }
            GUI.color = Color.white;
        }
        EditorGUILayout.EndVertical();
    }

    // ────────────────────────────────────────────
    //  스킬 편집기 + [NEW] 단계별 복사 버튼
    // ────────────────────────────────────────────
    private void DrawSkillEditor(SkillInfo skill)
    {
        if (skill == null) return;
        EditorGUILayout.BeginVertical("box");
        skill.skillName = EditorGUILayout.TextField("스킬명", skill.skillName);
        skill.skillIcon = (Sprite)EditorGUILayout.ObjectField("아이콘", skill.skillIcon, typeof(Sprite), false);

        bool isPassive = (selectedSkillIndex == 4);
        if (!isPassive) skill.baseCost = EditorGUILayout.IntField("기본 코스트", skill.baseCost);

        if (skill.levels == null || skill.levels.Length != 3)
        {
            skill.levels = new SkillLevelData[3];
            for (int k = 0; k < 3; k++) skill.levels[k] = new SkillLevelData();
        }

        for (int i = 0; i < 3; i++)
        {
            if (skill.levels[i] == null) skill.levels[i] = new SkillLevelData();

            EditorGUILayout.BeginVertical("helpbox");

            // ── 단계 헤더 + 복사 버튼 ──
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{i + 1}단계 상세 설정", EditorStyles.miniBoldLabel);
            if (i < 2)
            {
                // i=0: 1단계 → 2,3단계 / i=1: 2단계 → 3단계
                string copyLabel = (i == 0) ? "→ 2,3단계 복사" : "→ 3단계 복사";
                GUI.color = new Color(1f, 0.95f, 0.6f);
                if (GUILayout.Button(copyLabel, GUILayout.Width(105), GUILayout.Height(18)))
                    CopyLevelDown(skill, i);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            var lv = skill.levels[i];
            if (!isPassive) lv.overrideCost = EditorGUILayout.IntField("코스트 변동", lv.overrideCost);
            lv.targetType = (TargetType)EditorGUILayout.EnumPopup("타겟 범위", lv.targetType);

            if (GUILayout.Button("+ 효과 추가", GUILayout.Width(100)))
                lv.effects.Add(new EffectEntry());

            for (int e = 0; e < lv.effects.Count; e++)
            {
                var eff = lv.effects[e];
                EditorGUILayout.BeginHorizontal();
                eff.type = (EffectType)EditorGUILayout.EnumPopup(eff.type, GUILayout.Width(90));
                GUILayout.Label(new GUIContent("배율", "공격력에 곱해지는 수치 (1.0 = 100%)"), GUILayout.Width(30));
                eff.multiplier = EditorGUILayout.FloatField(eff.multiplier, GUILayout.Width(40));
                GUILayout.Label(new GUIContent("고정", "배율 계산 후 더해지는 고정 수치"), GUILayout.Width(30));
                eff.fixedValue = EditorGUILayout.FloatField(eff.fixedValue, GUILayout.Width(40));
                GUILayout.Label(new GUIContent("최종 피해 기반",
                    "체크 시 이전 효과의 계산 결과값을 기반으로 계산함"), GUILayout.Width(90));
                eff.useActualResult = EditorGUILayout.Toggle(eff.useActualResult, GUILayout.Width(20));
                if (GUILayout.Button("X", GUILayout.Width(20))) { lv.effects.RemoveAt(e); break; }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
    }

    // fromIndex 단계를 이후 단계(들)에 깊은 복사
    private void CopyLevelDown(SkillInfo skill, int fromIndex)
    {
        for (int to = fromIndex + 1; to < 3; to++)
            skill.levels[to] = DeepCopyLevelData(skill.levels[fromIndex]);
        EditorUtility.SetDirty(selectedData);
    }

    private SkillLevelData DeepCopyLevelData(SkillLevelData src)
    {
        var copy = new SkillLevelData
        {
            overrideCost = src.overrideCost,
            targetType = src.targetType,
            effects = new List<EffectEntry>()
        };
        foreach (var e in src.effects)
            copy.effects.Add(new EffectEntry
            {
                type = e.type,
                multiplier = e.multiplier,
                fixedValue = e.fixedValue,
                useActualResult = e.useActualResult
            });
        return copy;
    }

    // ════════════════════════════════════════════
    //  오른쪽 패널 — 밸런스 시트
    // ════════════════════════════════════════════
    private void DrawRightOverviewPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("📊 전체 캐릭터 밸런스 시트", EditorStyles.boldLabel);

        rightScroll = EditorGUILayout.BeginScrollView(rightScroll);
        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("이름", GUILayout.Width(100));
        GUILayout.Label("HP", GUILayout.Width(50));
        GUILayout.Label("ATK", GUILayout.Width(50));
        GUILayout.Label("스킬1(Lv1) 배율", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        foreach (CharacterData d in cachedCharacterList)
        {
            if (d == null) continue;
            if (selectedData == d) GUI.backgroundColor = Color.cyan;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal("box");
            d.characterName = EditorGUILayout.TextField(d.characterName, GUILayout.Width(100));
            d.maxHp = EditorGUILayout.FloatField(d.maxHp, GUILayout.Width(50));
            d.baseAttack = EditorGUILayout.FloatField(d.baseAttack, GUILayout.Width(50));

            bool hasEffect = d.activeSkills != null && d.activeSkills.Length > 0
                          && d.activeSkills[0]?.levels?.Length > 0
                          && d.activeSkills[0].levels[0]?.effects?.Count > 0;
            if (hasEffect)
                d.activeSkills[0].levels[0].effects[0].multiplier =
                    EditorGUILayout.FloatField(
                        d.activeSkills[0].levels[0].effects[0].multiplier, GUILayout.Width(100));
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.FloatField(0f, GUILayout.Width(100));
                EditorGUI.EndDisabledGroup();
            }

            if (GUILayout.Button("선택", GUILayout.Width(40))) { selectedData = d; GUI.FocusControl(null); }
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(d);

            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // ════════════════════════════════════════════
    //  프리셋 직렬화
    // ════════════════════════════════════════════
    private void SavePresets()
    {
        EditorPrefs.SetString(PRESET_KEY, JsonUtility.ToJson(presetLibrary, true));
    }

    private void LoadPresets()
    {
        if (EditorPrefs.HasKey(PRESET_KEY))
            presetLibrary = JsonUtility.FromJson<SkillPresetLibrary>(
                EditorPrefs.GetString(PRESET_KEY)) ?? new SkillPresetLibrary();
    }

    private SkillPreset SkillToPreset(SkillInfo skill, string name)
    {
        var p = new SkillPreset { presetName = name, skillName = skill.skillName, baseCost = skill.baseCost };
        if (skill.levels == null) return p;
        foreach (var lv in skill.levels)
        {
            var lp = new SkillLevelPreset
            {
                overrideCost = lv?.overrideCost ?? -1,
                targetType = (int)(lv?.targetType ?? 0)
            };
            if (lv?.effects != null)
                foreach (var e in lv.effects)
                    lp.effects.Add(new EffectPreset
                    {
                        type = (int)e.type,
                        multiplier = e.multiplier,
                        fixedValue = e.fixedValue,
                        useActualResult = e.useActualResult
                    });
            p.levels.Add(lp);
        }
        return p;
    }

    private void ApplyPreset(SkillPreset preset, SkillInfo target)
    {
        target.skillName = preset.skillName;
        target.baseCost = preset.baseCost;
        if (target.levels == null || target.levels.Length != 3)
            target.levels = new SkillLevelData[3];

        for (int i = 0; i < 3 && i < preset.levels.Count; i++)
        {
            var src = preset.levels[i];
            target.levels[i] = new SkillLevelData
            {
                overrideCost = src.overrideCost,
                targetType = (TargetType)src.targetType,
                effects = new List<EffectEntry>()
            };
            foreach (var ep in src.effects)
                target.levels[i].effects.Add(new EffectEntry
                {
                    type = (EffectType)ep.type,
                    multiplier = ep.multiplier,
                    fixedValue = ep.fixedValue,
                    useActualResult = ep.useActualResult
                });
        }
    }

    // ════════════════════════════════════════════
    //  유틸리티
    // ════════════════════════════════════════════
    private void CreateNewCharacter()
    {
        string path = EditorUtility.SaveFilePanelInProject("새 캐릭터", "NewCharacter", "asset", "");
        if (string.IsNullOrEmpty(path)) return;
        CharacterData data = CreateInstance<CharacterData>();
        InitData(data);
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        selectedData = data;
    }

    private void DuplicateCharacter(CharacterData origin)
    {
        string newPath = AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(origin));
        AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(origin), newPath);
        AssetDatabase.SaveAssets();
    }

    private void DeleteCharacter(CharacterData data)
    {
        if (EditorUtility.DisplayDialog("캐릭터 삭제",
            $"{data.characterName}를 삭제하시겠습니까?", "삭제", "취소"))
        {
            AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(data));
            if (selectedData == data) selectedData = null;
            AssetDatabase.Refresh();
        }
    }

    private SkillInfo CreateDefaultSkill()
    {
        var s = new SkillInfo { levels = new SkillLevelData[3] };
        for (int j = 0; j < 3; j++) s.levels[j] = new SkillLevelData();
        return s;
    }

    private void InitData(CharacterData data)
    {
        data.passiveSkill = CreateDefaultSkill();
        data.activeSkills = new SkillInfo[4];
        for (int i = 0; i < 4; i++) data.activeSkills[i] = CreateDefaultSkill();
    }
}

// ══════════════════════════════════════════════════════════════════
//  이미지 이름 규격화 창
//  폴더 내 이미지를 미리보기로 확인 후 {캐릭터명}_규칙명 으로 일괄 변경
// ══════════════════════════════════════════════════════════════════
public class ImageRenameWindow : EditorWindow
{
    private class RenameEntry
    {
        public string assetPath;
        public string originalName;   // 확장자 제외 원본
        public string assignedSuffix; // 사용자가 버튼으로 지정한 접미사. null=미지정, ""=무시
        public Texture2D preview;
        public bool apply = true;
        public bool alreadyCorrect; // 이미 규칙에 맞는 파일

        // 최종 파일명: {캐릭터명}_{assignedSuffix}
        public string ProposedName(string charName) =>
            string.IsNullOrEmpty(assignedSuffix) ? null : $"{charName}_{assignedSuffix}";
    }

    // 버튼으로 선택할 수 있는 접미사 목록
    private static readonly string[] SUFFIXES =
        { "Portrait", "Illust", "Skill_0", "Skill_1", "Skill_2", "Skill_3", "Skill_4" };

    private string characterName;
    private string folderPath;
    private List<RenameEntry> entries = new List<RenameEntry>();
    private Vector2 scroll;

    // 각 접미사가 이미 다른 항목에 배정됐는지 추적
    private HashSet<string> assignedSuffixes = new HashSet<string>();

    public static void Open(string charName, string folder)
    {
        var win = GetWindow<ImageRenameWindow>("이미지 이름 규격화");
        win.characterName = charName;
        win.folderPath = folder;
        win.minSize = new Vector2(700, 480);
        win.ScanFolder();
    }

    private void ScanFolder()
    {
        entries.Clear();
        assignedSuffixes.Clear();
        if (string.IsNullOrEmpty(folderPath)) return;

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string nameNoExt = Path.GetFileNameWithoutExtension(path);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            // 이미 규칙에 맞는지 확인
            string matchedSuffix = null;
            foreach (string s in SUFFIXES)
            {
                if (nameNoExt.Equals($"{characterName}_{s}", System.StringComparison.OrdinalIgnoreCase))
                { matchedSuffix = s; break; }
            }

            bool correct = matchedSuffix != null;
            if (correct) assignedSuffixes.Add(matchedSuffix);

            entries.Add(new RenameEntry
            {
                assetPath = path,
                originalName = nameNoExt,
                assignedSuffix = correct ? matchedSuffix : null, // 맞으면 자동 배정
                preview = tex,
                apply = !correct, // 이미 맞으면 기본 체크 해제
                alreadyCorrect = correct
            });
        }
    }

    private void OnGUI()
    {
        // ── 상단 정보 + 툴바 ──
        EditorGUILayout.LabelField($"📁 {folderPath}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"캐릭터: {characterName}  |  총 {entries.Count}개 파일");
        EditorGUILayout.Space(4);

        // ── 일괄 적용 버튼 행 ──
        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        if (GUILayout.Button("☑ 미배정 전체 선택", GUILayout.Height(24)))
        {
            foreach (var e in entries)
                if (!e.alreadyCorrect && !string.IsNullOrEmpty(e.assignedSuffix))
                    e.apply = true;
        }
        if (GUILayout.Button("☐ 전체 해제", GUILayout.Height(24)))
        {
            foreach (var e in entries)
                if (!e.alreadyCorrect) e.apply = false;
        }
        if (GUILayout.Button("↺ 배정 초기화", GUILayout.Height(24)))
        {
            foreach (var e in entries)
                if (!e.alreadyCorrect) { e.assignedSuffix = null; e.apply = false; }
            RebuildAssignedSet();
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("🔄 다시 스캔", GUILayout.Height(24), GUILayout.Width(90)))
            ScanFolder();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // ── 헤더 행 ──
        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("미리보기", GUILayout.Width(54));
        GUILayout.Label("원본 파일명", GUILayout.Width(160));
        GUILayout.Label("← 버튼 클릭으로 종류 지정 →", GUILayout.Width(370));
        GUILayout.Label("적용", GUILayout.Width(30));
        EditorGUILayout.EndHorizontal();

        // ── 항목 목록 ──
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var entry in entries)
        {
            if (entry.alreadyCorrect)
            {
                // 이미 맞는 항목은 간략하게 표시
                EditorGUILayout.BeginHorizontal("box");
                DrawPreview(entry.preview);
                GUI.color = new Color(0.6f, 1f, 0.6f);
                EditorGUILayout.LabelField($"✓ {entry.originalName}", GUILayout.Width(545));
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
                continue;
            }

            EditorGUILayout.BeginHorizontal("box");
            DrawPreview(entry.preview);

            // 원본 이름
            EditorGUILayout.LabelField(entry.originalName, GUILayout.Width(160));

            // ── 접미사 선택 버튼 7개 ──
            foreach (string suffix in SUFFIXES)
            {
                bool isSelected = entry.assignedSuffix == suffix;
                bool takenByOther = assignedSuffixes.Contains(suffix) && !isSelected;

                // 선택됨=녹색 / 다른 항목에 배정됨=어둡게 / 기본=흰색
                if (isSelected) GUI.color = new Color(0.4f, 1f, 0.5f);
                else if (takenByOther) GUI.color = new Color(0.5f, 0.5f, 0.5f);
                else GUI.color = Color.white;

                if (GUILayout.Button(suffix, GUILayout.Width(52), GUILayout.Height(22)))
                {
                    if (isSelected)
                    {
                        // 같은 버튼 재클릭 → 배정 해제
                        entry.assignedSuffix = null;
                        entry.apply = false;
                    }
                    else if (!takenByOther)
                    {
                        // 기존 배정 해제 후 새로 배정
                        if (entry.assignedSuffix != null)
                            assignedSuffixes.Remove(entry.assignedSuffix);
                        entry.assignedSuffix = suffix;
                        assignedSuffixes.Add(suffix);
                        entry.apply = true;
                    }
                    // takenByOther면 클릭 무시
                }
                GUI.color = Color.white;
            }

            // 적용 체크박스
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(entry.assignedSuffix));
            entry.apply = EditorGUILayout.Toggle(
                entry.apply && !string.IsNullOrEmpty(entry.assignedSuffix),
                GUILayout.Width(20));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        // ── 하단 적용 버튼 ──
        EditorGUILayout.Space(6);
        int applyCount = entries.FindAll(e =>
            e.apply && !string.IsNullOrEmpty(e.assignedSuffix) && !e.alreadyCorrect).Count;

        GUI.color = applyCount > 0 ? new Color(0.5f, 1f, 0.6f) : Color.gray;
        if (GUILayout.Button($"✅  이름 변경 적용 ({applyCount}개)", GUILayout.Height(32)))
        {
            if (applyCount > 0) ApplyRenames();
        }
        GUI.color = Color.white;
    }

    private void DrawPreview(Texture2D tex)
    {
        if (tex != null)
            GUILayout.Label(tex, GUILayout.Width(48), GUILayout.Height(48));
        else
            GUILayout.Label("?", GUILayout.Width(48), GUILayout.Height(48));
    }

    private void RebuildAssignedSet()
    {
        assignedSuffixes.Clear();
        foreach (var e in entries)
            if (e.assignedSuffix != null) assignedSuffixes.Add(e.assignedSuffix);
    }

    private void ApplyRenames()
    {
        int success = 0, fail = 0;
        foreach (var entry in entries)
        {
            if (!entry.apply || string.IsNullOrEmpty(entry.assignedSuffix) || entry.alreadyCorrect)
                continue;
            string newName = entry.ProposedName(characterName);
            string err = AssetDatabase.RenameAsset(entry.assetPath, newName);
            if (string.IsNullOrEmpty(err)) success++;
            else
            {
                fail++;
                Debug.LogWarning($"[이름 변경 실패] {entry.originalName} → {newName}: {err}");
            }
        }
        AssetDatabase.Refresh();
        ScanFolder();
        EditorUtility.DisplayDialog("이름 변경 완료", $"성공: {success}개  /  실패: {fail}개", "확인");
    }
}