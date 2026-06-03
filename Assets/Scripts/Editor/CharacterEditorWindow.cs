using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class CharacterEditorWindow : EditorWindow
{
    private enum CharacterEditorMode { Player, Enemy }

    private const string PLAYER_DATA_FOLDER = "Assets/Resources/Characters/Player/Data";
    private const string PLAYER_ILLUST_FOLDER = "Assets/Resources/Characters/Player/Illust";
    private const string ENEMY_DATA_FOLDER = "Assets/Resources/Characters/Enemy/Data";
    private const string ENEMY_ILLUST_FOLDER = "Assets/Resources/Characters/Enemy/Illust";
    private const double SAVE_DELAY_SECONDS = 1.0d;

    private readonly List<CharacterData> cachedCharacterList = new List<CharacterData>();
    private CharacterEditorMode editorMode = CharacterEditorMode.Player;
    private CharacterData selectedData;
    private Vector2 leftScroll;
    private Vector2 middleScroll;
    private Vector2 rightScroll;
    private string searchText = "";
    private string iconFolderPath = "";
    private int selectedSkillIndex;
    private bool isDirtyCache = true;
    private bool showValidationDetails;
    private bool hasPendingSave;
    private double nextSaveTime;

    private bool IsEnemyMode => editorMode == CharacterEditorMode.Enemy;

    private class CharacterValidationResult
    {
        public readonly List<string> errors = new List<string>();
        public readonly List<string> warnings = new List<string>();
        public bool HasIssues => errors.Count > 0 || warnings.Count > 0;
        public string Summary => errors.Count > 0 ? $"오류 {errors.Count}" : warnings.Count > 0 ? $"경고 {warnings.Count}" : "정상";
    }

    [MenuItem("Window/Battle/캐릭터 편집기 #e")]
    public static void ShowWindow()
    {
        CharacterEditorWindow window = GetWindow<CharacterEditorWindow>("캐릭터 편집기");
        window.minSize = new Vector2(1200, 700);
    }

    private void OnEnable()
    {
        isDirtyCache = true;
        EditorApplication.update -= FlushPendingSaveIfReady;
        EditorApplication.update += FlushPendingSaveIfReady;
    }

    private void OnFocus() => isDirtyCache = true;
    private void OnDisable()
    {
        EditorApplication.update -= FlushPendingSaveIfReady;
        FlushPendingSaveNow();
    }

    private void OnLostFocus() => FlushPendingSaveNow();

    private void OnGUI()
    {
        RefreshCacheIfNeeded();
        if (selectedData != null && selectedData.isEnemy != IsEnemyMode)
        {
            selectedData = null;
            iconFolderPath = "";
        }

        EditorGUILayout.BeginHorizontal();
        DrawLeftPanel();
        DrawMiddlePanel();
        DrawRightOverviewPanel();
        EditorGUILayout.EndHorizontal();

        FlushPendingSaveIfReady();
    }

    private void RefreshCacheIfNeeded()
    {
        // 프로젝트 안의 캐릭터 데이터 에셋을 다시 읽어 목록을 갱신합니다.
        if (!isDirtyCache) return;
        cachedCharacterList.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:CharacterData"))
        {
            CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data != null) cachedCharacterList.Add(data);
        }
        cachedCharacterList.Sort((a, b) => string.Compare(a.characterName, b.characterName, System.StringComparison.OrdinalIgnoreCase));
        isDirtyCache = false;
    }

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(230), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("캐릭터 종류", EditorStyles.boldLabel);
        CharacterEditorMode nextMode = (CharacterEditorMode)GUILayout.Toolbar((int)editorMode, new[] { "아군", "적" });
        if (nextMode != editorMode)
        {
            editorMode = nextMode;
            selectedData = null;
            iconFolderPath = "";
            GUI.FocusControl(null);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("검색", EditorStyles.boldLabel);
        searchText = EditorGUILayout.TextField(searchText);

        if (GUILayout.Button(IsEnemyMode ? "+ 새 적 캐릭터 생성" : "+ 새 아군 캐릭터 생성", GUILayout.Height(30)))
        {
            CreateNewCharacter(IsEnemyMode);
            isDirtyCache = true;
        }

        EditorGUILayout.Space(5);
        leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
        foreach (CharacterData data in cachedCharacterList)
        {
            if (data == null || data.isEnemy != IsEnemyMode) continue;
            string characterName = data.characterName ?? "";
            if (!string.IsNullOrEmpty(searchText) && !characterName.ToLower().Contains(searchText.ToLower())) continue;

            EditorGUILayout.BeginHorizontal();
            string displayName = string.IsNullOrWhiteSpace(characterName) ? "(이름 없음)" : characterName;
            bool isSelected = selectedData == data;
            if (GUILayout.Toggle(isSelected, displayName, "Button", GUILayout.Height(25), GUILayout.ExpandWidth(true)) && !isSelected)
            {
                selectedData = data;
                iconFolderPath = "";
                GUI.FocusControl(null);
            }
            if (GUILayout.Button("C", GUILayout.Width(22), GUILayout.Height(25)))
            {
                DuplicateCharacter(data);
                isDirtyCache = true;
            }
            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(25)))
            {
                DeleteCharacter(data);
                isDirtyCache = true;
                GUI.color = Color.white;
                break;
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawMiddlePanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(520), GUILayout.ExpandHeight(true));
        if (selectedData == null)
        {
            EditorGUILayout.HelpBox(IsEnemyMode ? "적 캐릭터를 선택하세요." : "아군 캐릭터를 선택하세요.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        middleScroll = EditorGUILayout.BeginScrollView(middleScroll);
        EditorGUI.BeginChangeCheck();

        if (string.IsNullOrEmpty(iconFolderPath))
            iconFolderPath = $"{GetIllustFolder(selectedData.isEnemy)}/{selectedData.characterName}";

        DrawNameSection();
        DrawSpriteSection();
        DrawBaseStatsSection();
        DrawValidationSection();
        DrawBulkImageSection();

        EditorGUILayout.Space(12);
        if (selectedData.isEnemy) DrawEnemyPatternEditor();
        else DrawPlayerSkillSection();

        if (EditorGUI.EndChangeCheck()) MarkDirtyAndSave(selectedData);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawNameSection()
    {
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField(selectedData.isEnemy ? "적 데이터" : "아군 데이터", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        string newName = EditorGUILayout.DelayedTextField("캐릭터 이름", selectedData.characterName);
        if (EditorGUI.EndChangeCheck() && !string.IsNullOrWhiteSpace(newName))
        {
            selectedData.characterName = newName.Trim();
            string assetPath = AssetDatabase.GetAssetPath(selectedData);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string error = AssetDatabase.RenameAsset(assetPath, $"{selectedData.characterName}_Data");
                if (!string.IsNullOrEmpty(error)) Debug.LogWarning($"[캐릭터 편집기] 이름 변경 실패: {error}");
                MarkDirtyAndSave(selectedData);
                AssetDatabase.Refresh();
                iconFolderPath = "";
                isDirtyCache = true;
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawSpriteSection()
    {
        EditorGUILayout.BeginHorizontal("helpbox");
        if (!selectedData.isEnemy)
            selectedData.portraitSprite = (Sprite)EditorGUILayout.ObjectField("초상화", selectedData.portraitSprite, typeof(Sprite), false, GUILayout.Height(60));
        selectedData.standingSprite = (Sprite)EditorGUILayout.ObjectField("Standing", selectedData.standingSprite, typeof(Sprite), false, GUILayout.Height(60));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawBaseStatsSection()
    {
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField("기본 능력치", EditorStyles.boldLabel);
        selectedData.maxHp = EditorGUILayout.FloatField("최대 체력", selectedData.maxHp);
        selectedData.maxMental = EditorGUILayout.FloatField("최대 정신력", selectedData.maxMental);
        selectedData.baseAttack = EditorGUILayout.FloatField("기본 공격력", selectedData.baseAttack);
        EditorGUILayout.EndVertical();
    }

    private void DrawValidationSection()
    {
        CharacterValidationResult result = ValidateCharacterData(selectedData);
        MessageType messageType = result.errors.Count > 0 ? MessageType.Error : result.warnings.Count > 0 ? MessageType.Warning : MessageType.Info;

        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField(selectedData.isEnemy ? "적 데이터 확인" : "아군 데이터 확인", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(result.HasIssues ? result.Summary : "전투에 사용할 수 있는 데이터입니다.", messageType);
        showValidationDetails = EditorGUILayout.Foldout(showValidationDetails, "상세 내용", true);
        if (showValidationDetails)
        {
            if (!result.HasIssues) EditorGUILayout.LabelField("- 문제가 없습니다.", EditorStyles.wordWrappedLabel);
            foreach (string error in result.errors) EditorGUILayout.LabelField($"- {error}", EditorStyles.wordWrappedLabel);
            foreach (string warning in result.warnings) EditorGUILayout.LabelField($"- {warning}", EditorStyles.wordWrappedLabel);
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawBulkImageSection()
    {
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField("이미지 자동 바인딩", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("이미지 폴더", GUILayout.Width(82));
        iconFolderPath = EditorGUILayout.TextField(iconFolderPath);
        if (GUILayout.Button("자동 경로", GUILayout.Width(75)))
            iconFolderPath = $"{GetIllustFolder(selectedData.isEnemy)}/{selectedData.characterName}";
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("이미지 전체 바인딩", GUILayout.Height(26))) AutoBindAllImages();
        EditorGUILayout.EndVertical();
    }

    private void AutoBindAllImages()
    {
        // 파일명 규칙에 맞는 이미지를 찾아 캐릭터 데이터에 자동으로 연결합니다.
        string folder = iconFolderPath.TrimEnd('/', '\\');
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string suffix = ExtractSuffix(Path.GetFileNameWithoutExtension(path), selectedData.characterName);
            if (suffix == null) continue;

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) continue;

            string lower = suffix.ToLower();
            if (!selectedData.isEnemy && lower == "portrait") { selectedData.portraitSprite = sprite; count++; }
            else if (lower == "illust") { selectedData.standingSprite = sprite; count++; }
            else if (!selectedData.isEnemy) count += TryBindSkillIcon(lower, sprite) ? 1 : 0;
        }

        MarkDirtyAndSave(selectedData);
        EditorUtility.DisplayDialog("자동 바인딩 완료", $"{count}개 이미지를 연결했습니다.", "확인");
    }

    private bool TryBindSkillIcon(string suffixLower, Sprite sprite)
    {
        if (suffixLower == "skill_0" && selectedData.passiveSkill != null)
        {
            selectedData.passiveSkill.skillIcon = sprite;
            return true;
        }
        if (selectedData.activeSkills == null) return false;
        for (int i = 1; i <= 4; i++)
        {
            if (suffixLower == $"skill_{i}" && selectedData.activeSkills.Length >= i && selectedData.activeSkills[i - 1] != null)
            {
                selectedData.activeSkills[i - 1].skillIcon = sprite;
                return true;
            }
        }
        return false;
    }

    private string ExtractSuffix(string fileNameNoExt, string characterName)
    {
        string prefix = characterName + "_";
        return fileNameNoExt.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase) ? fileNameNoExt.Substring(prefix.Length) : null;
    }

    private void DrawPlayerSkillSection()
    {
        if (selectedData.activeSkills == null || selectedData.activeSkills.Length != 4)
        {
            EditorGUILayout.HelpBox("액티브 스킬 데이터가 손상되었습니다.", MessageType.Error);
            if (GUILayout.Button("데이터 복구"))
            {
                InitData(selectedData, false);
                MarkDirtyAndSave(selectedData);
            }
            return;
        }

        string[] tabs = { "스킬 1", "스킬 2", "스킬 3", "스킬 4", "패시브" };
        selectedSkillIndex = GUILayout.Toolbar(selectedSkillIndex, tabs);
        bool isPassive = selectedSkillIndex == 4;
        SkillInfo skill = isPassive ? selectedData.passiveSkill : selectedData.activeSkills[selectedSkillIndex];
        if (skill == null)
        {
            skill = CreateDefaultSkill();
            if (isPassive) selectedData.passiveSkill = skill;
            else selectedData.activeSkills[selectedSkillIndex] = skill;
        }
        DrawSkillEditor(skill, isPassive);
    }

    private void DrawSkillEditor(SkillInfo skill, bool isPassive)
    {
        EditorGUILayout.BeginVertical("box");
        skill.skillName = EditorGUILayout.TextField("스킬 이름", skill.skillName);
        skill.skillIcon = (Sprite)EditorGUILayout.ObjectField("아이콘", skill.skillIcon, typeof(Sprite), false);
        if (!isPassive) skill.baseCost = EditorGUILayout.IntField("기본 비용", skill.baseCost);
        EnsureSkillLevels(skill);
        for (int i = 0; i < skill.levels.Length; i++)
        {
            SkillLevelData level = skill.levels[i];
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField($"{i + 1}단계", EditorStyles.miniBoldLabel);
            if (!isPassive) level.overrideCost = EditorGUILayout.IntField("비용 변경", level.overrideCost);
            level.targetType = (TargetType)EditorGUILayout.EnumPopup("대상", level.targetType);
            DrawEffectList(level.effects);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawEnemyPatternEditor()
    {
        if (selectedData.enemyPatterns == null) selectedData.enemyPatterns = new List<EnemyPatternData>();
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("행동 패턴", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("패턴은 1번 -> 2번 -> 3번 순서로 실행되고, 마지막 뒤에는 다시 1번으로 돌아갑니다.", MessageType.None);
        if (GUILayout.Button("+ 패턴 추가", GUILayout.Width(110)))
        {
            selectedData.enemyPatterns.Add(new EnemyPatternData { patternName = $"패턴 {selectedData.enemyPatterns.Count + 1}" });
            MarkDirtyAndSave(selectedData);
        }

        for (int i = 0; i < selectedData.enemyPatterns.Count; i++)
        {
            EnemyPatternData pattern = selectedData.enemyPatterns[i] ?? new EnemyPatternData { patternName = $"패턴 {i + 1}" };
            selectedData.enemyPatterns[i] = pattern;
            if (pattern.effects == null) pattern.effects = new List<EffectEntry>();

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{i + 1}", GUILayout.Width(24));
            pattern.patternName = EditorGUILayout.TextField(pattern.patternName);
            if (GUILayout.Button("위", GUILayout.Width(36)) && i > 0) { SwapEnemyPatterns(i, i - 1); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break; }
            if (GUILayout.Button("아래", GUILayout.Width(42)) && i < selectedData.enemyPatterns.Count - 1) { SwapEnemyPatterns(i, i + 1); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break; }
            if (GUILayout.Button("X", GUILayout.Width(24))) { selectedData.enemyPatterns.RemoveAt(i); MarkDirtyAndSave(selectedData); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break; }
            EditorGUILayout.EndHorizontal();

            pattern.targetType = (TargetType)EditorGUILayout.EnumPopup("대상", pattern.targetType);
            DrawEffectList(pattern.effects);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawEffectList(List<EffectEntry> effects)
    {
        if (effects == null) return;
        if (GUILayout.Button("+ 효과 추가", GUILayout.Width(100))) effects.Add(new EffectEntry());
        for (int i = 0; i < effects.Count; i++)
        {
            EffectEntry effect = effects[i];
            EditorGUILayout.BeginHorizontal();
            effect.type = (EffectType)EditorGUILayout.EnumPopup(effect.type, GUILayout.Width(90));
            GUILayout.Label("배율", GUILayout.Width(32));
            effect.multiplier = EditorGUILayout.FloatField(effect.multiplier, GUILayout.Width(45));
            GUILayout.Label("고정", GUILayout.Width(32));
            effect.fixedValue = EditorGUILayout.FloatField(effect.fixedValue, GUILayout.Width(45));
            GUILayout.Label("결과", GUILayout.Width(32));
            effect.useActualResult = EditorGUILayout.Toggle(effect.useActualResult, GUILayout.Width(20));
            if (GUILayout.Button("X", GUILayout.Width(20))) { effects.RemoveAt(i); break; }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawRightOverviewPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField(IsEnemyMode ? "적 데이터 시트" : "아군 데이터 시트", EditorStyles.boldLabel);
        rightScroll = EditorGUILayout.BeginScrollView(rightScroll);
        foreach (CharacterData data in cachedCharacterList)
        {
            if (data == null || data.isEnemy != IsEnemyMode) continue;
            if (selectedData == data) GUI.backgroundColor = Color.cyan;
            EditorGUILayout.BeginHorizontal("box");
            GUILayout.Label(ValidateCharacterData(data).Summary, GUILayout.Width(70));
            EditorGUI.BeginChangeCheck();
            data.characterName = EditorGUILayout.TextField(data.characterName, GUILayout.Width(110));
            data.maxHp = EditorGUILayout.FloatField(data.maxHp, GUILayout.Width(55));
            data.maxMental = EditorGUILayout.FloatField(data.maxMental, GUILayout.Width(55));
            data.baseAttack = EditorGUILayout.FloatField(data.baseAttack, GUILayout.Width(55));
            if (EditorGUI.EndChangeCheck())
            {
                MarkDirtyAndSave(data);
            }
            EditorGUI.BeginDisabledGroup(true);
            int count = data.isEnemy ? (data.enemyPatterns?.Count ?? 0) : (data.activeSkills?.Length ?? 0);
            EditorGUILayout.IntField(count, GUILayout.Width(55));
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("선택", GUILayout.Width(55))) { selectedData = data; iconFolderPath = ""; }
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void CreateNewCharacter(bool isEnemy)
    {
        // 새 캐릭터 데이터는 종류에 따라 정해진 리소스 폴더에 바로 만듭니다.
        string folder = GetDataFolder(isEnemy);
        EnsureAssetFolder(folder);
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{(isEnemy ? "NewEnemy" : "NewAlly")}_Data.asset");
        CharacterData data = CreateInstance<CharacterData>();
        InitData(data, isEnemy);
        data.characterName = NormalizeCharacterName(Path.GetFileNameWithoutExtension(path), isEnemy);
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        selectedData = data;
        editorMode = isEnemy ? CharacterEditorMode.Enemy : CharacterEditorMode.Player;
        iconFolderPath = "";
    }

    private void DuplicateCharacter(CharacterData origin)
    {
        // 복사본도 원본과 같은 아군/적 폴더 안에 만듭니다.
        string folder = GetDataFolder(origin.isEnemy);
        EnsureAssetFolder(folder);
        string newPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{origin.characterName}_Copy_Data.asset");
        AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(origin), newPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void DeleteCharacter(CharacterData data)
    {
        if (!EditorUtility.DisplayDialog("캐릭터 삭제", $"{data.characterName} 데이터를 삭제할까요?", "삭제", "취소")) return;
        AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(data));
        if (selectedData == data) selectedData = null;
        AssetDatabase.Refresh();
    }

    private void InitData(CharacterData data, bool isEnemy)
    {
        // 아군은 스킬 4개와 패시브를 만들고, 적은 기본 행동 패턴만 만듭니다.
        data.isEnemy = isEnemy;
        data.characterName = isEnemy ? "NewEnemy" : "NewAlly";
        data.maxHp = Mathf.Max(1f, data.maxHp);
        data.maxMental = Mathf.Max(1f, data.maxMental);
        data.baseAttack = Mathf.Max(1f, data.baseAttack);
        data.levelStatMultipliers = new float[5] { 0f, 0.5f, 1.0f, 2.0f, 3.0f };

        if (isEnemy)
        {
            data.passiveSkill = null;
            data.activeSkills = null;
            data.enemyPatterns = new List<EnemyPatternData>
            {
                new EnemyPatternData
                {
                    patternName = "패턴 1",
                    targetType = TargetType.SingleEnemy,
                    effects = new List<EffectEntry> { new EffectEntry { type = EffectType.Damage, multiplier = 1f } }
                }
            };
        }
        else
        {
            data.passiveSkill = CreateDefaultSkill();
            data.activeSkills = new SkillInfo[4];
            for (int i = 0; i < data.activeSkills.Length; i++) data.activeSkills[i] = CreateDefaultSkill();
            data.enemyPatterns = new List<EnemyPatternData>();
        }
    }

    private SkillInfo CreateDefaultSkill()
    {
        // 아군 스킬은 3단계 강화 데이터를 기본으로 가집니다.
        SkillInfo skill = new SkillInfo { levels = new SkillLevelData[3] };
        for (int i = 0; i < skill.levels.Length; i++) skill.levels[i] = new SkillLevelData();
        return skill;
    }

    private void EnsureSkillLevels(SkillInfo skill)
    {
        if (skill.levels == null || skill.levels.Length != 3) skill.levels = new SkillLevelData[3];
        for (int i = 0; i < skill.levels.Length; i++)
            if (skill.levels[i] == null) skill.levels[i] = new SkillLevelData();
    }

    private CharacterValidationResult ValidateCharacterData(CharacterData data)
    {
        // 편집 중인 데이터가 전투에서 쓸 수 있는 최소 조건을 만족하는지 확인합니다.
        CharacterValidationResult result = new CharacterValidationResult();
        if (data == null) { result.errors.Add("캐릭터 데이터가 없습니다."); return result; }
        string path = AssetDatabase.GetAssetPath(data);
        string expectedFolder = data.isEnemy ? ENEMY_DATA_FOLDER : PLAYER_DATA_FOLDER;
        if (!string.IsNullOrEmpty(path) && !path.StartsWith(expectedFolder, System.StringComparison.OrdinalIgnoreCase))
            result.warnings.Add($"기본 데이터 폴더가 아닙니다: {path}");
        if (string.IsNullOrWhiteSpace(data.characterName)) result.errors.Add("캐릭터 이름이 비어 있습니다.");
        if (data.maxHp <= 0f) result.errors.Add("최대 체력은 0보다 커야 합니다.");
        if (data.maxMental <= 0f) result.errors.Add("최대 정신력은 0보다 커야 합니다.");
        if (data.baseAttack <= 0f) result.errors.Add("기본 공격력은 0보다 커야 합니다.");
        if (data.isEnemy) ValidateEnemyPatterns(data, result);
        else ValidatePlayerSkills(data, result);
        return result;
    }

    private void ValidatePlayerSkills(CharacterData data, CharacterValidationResult result)
    {
        if (data.passiveSkill == null) result.errors.Add("패시브 스킬이 없습니다.");
        if (data.activeSkills == null || data.activeSkills.Length != 4) result.errors.Add("아군 액티브 스킬은 4개여야 합니다.");
    }

    private void ValidateEnemyPatterns(CharacterData data, CharacterValidationResult result)
    {
        if (data.enemyPatterns == null || data.enemyPatterns.Count == 0)
        {
            result.warnings.Add("행동 패턴이 없습니다. 전투 중 기본 공격을 사용합니다.");
            return;
        }
        for (int i = 0; i < data.enemyPatterns.Count; i++)
        {
            EnemyPatternData pattern = data.enemyPatterns[i];
            if (pattern == null) result.errors.Add($"{i + 1}번 패턴이 없습니다.");
            else if (pattern.effects == null || pattern.effects.Count == 0) result.warnings.Add($"{i + 1}번 패턴에 효과가 없습니다.");
        }
    }

    private void SwapEnemyPatterns(int a, int b)
    {
        // 패턴 실행 순서를 바꿉니다.
        EnemyPatternData temp = selectedData.enemyPatterns[a];
        selectedData.enemyPatterns[a] = selectedData.enemyPatterns[b];
        selectedData.enemyPatterns[b] = temp;
        MarkDirtyAndSave(selectedData);
    }

    private string NormalizeCharacterName(string rawName, bool isEnemy)
    {
        // 파일명에서 데이터 접미사를 제거해 캐릭터 이름으로 씁니다.
        string fallback = isEnemy ? "NewEnemy" : "NewAlly";
        if (string.IsNullOrWhiteSpace(rawName)) return fallback;
        string normalized = rawName.Trim();
        if (normalized.EndsWith("_Data", System.StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring(0, normalized.Length - "_Data".Length);
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private string GetDataFolder(bool isEnemy) => isEnemy ? ENEMY_DATA_FOLDER : PLAYER_DATA_FOLDER;
    private string GetIllustFolder(bool isEnemy) => isEnemy ? ENEMY_ILLUST_FOLDER : PLAYER_ILLUST_FOLDER;

    private void MarkDirtyAndSave(CharacterData data)
    {
        // 입력 중 렉이 생기지 않도록 실제 저장은 잠시 뒤에 한 번만 처리합니다.
        if (data == null) return;
        EditorUtility.SetDirty(data);
        hasPendingSave = true;
        nextSaveTime = EditorApplication.timeSinceStartup + SAVE_DELAY_SECONDS;
    }

    private void FlushPendingSaveIfReady()
    {
        if (!hasPendingSave || EditorApplication.timeSinceStartup < nextSaveTime) return;
        FlushPendingSaveNow();
    }

    private void FlushPendingSaveNow()
    {
        if (!hasPendingSave) return;
        hasPendingSave = false;
        AssetDatabase.SaveAssets();
    }

    private void EnsureAssetFolder(string folderPath)
    {
        // 저장 대상 폴더가 없으면 에셋 폴더부터 차례대로 생성합니다.
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
