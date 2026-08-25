using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using TheLastArk.Map.Events;

public class CharacterEditorWindow : EditorWindow
{
    private enum CharacterEditorMode { Player, Enemy, Formations, Pools, Events }

    private const string PLAYER_DATA_FOLDER = "Assets/Resources/Characters/Player/Data";
    private const string PLAYER_ILLUST_FOLDER = "Assets/Resources/Characters/Player/Illust";
    private const string ENEMY_DATA_FOLDER = "Assets/Resources/Characters/Enemy/Data";
    private const string ENEMY_ILLUST_FOLDER = "Assets/Resources/Characters/Enemy/Illust";
    private const double SAVE_DELAY_SECONDS = 1.0d;
    private const float LayoutChromeWidth = 12f;
    private const float CharacterListWidth = 230f;
    private const float EventListWidth = 210f;
    private const float EventToolsWidth = 260f;

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
    private List<EffectEntry> statusPaletteTarget;
    private List<EffectEntry> effectPaletteTarget;
    private double nextSaveTime;
    private EnemyEncounterEditorWindow embeddedEncounterEditor;

    private readonly List<GameEventData> cachedEventList = new List<GameEventData>();
    private GameEventData selectedEventData;
    private Vector2 eventLeftScroll;
    private Vector2 eventMiddleScroll;
    private Vector2 eventRightScroll;
    private string eventSearchText = "";
    private int eventCategoryFilter;
    private bool isDirtyEventCache = true;

    private bool IsEnemyMode => editorMode == CharacterEditorMode.Enemy;
    private bool IsCharacterMode => editorMode == CharacterEditorMode.Player || editorMode == CharacterEditorMode.Enemy;
    private float CharacterWorkWidth => Mathf.Max(600f, position.width - CharacterListWidth - LayoutChromeWidth);
    private float CharacterDetailWidth => CharacterWorkWidth * (2f / 3f);
    private float CharacterSheetWidth => CharacterWorkWidth / 3f;
    private float EventDetailWidth => Mathf.Max(320f, position.width - EventListWidth - EventToolsWidth - LayoutChromeWidth);

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
        CharacterEditorWindow[] openedWindows = Resources.FindObjectsOfTypeAll<CharacterEditorWindow>();
        if (openedWindows.Length > 0)
        {
            for (int i = 0; i < openedWindows.Length; i++)
            {
                openedWindows[i].Close();
            }
            return;
        }

        CharacterEditorWindow window = GetWindow<CharacterEditorWindow>("편집기");
        window.minSize = new Vector2(1300, 650);
    }

    private void OnEnable()
    {
        isDirtyCache = true;
        EnsureEmbeddedEncounterEditor();
        EditorApplication.update -= FlushPendingSaveIfReady;
        EditorApplication.update += FlushPendingSaveIfReady;
    }

    private void OnFocus()
    {
        isDirtyCache = true;
        isDirtyEventCache = true;
        if (embeddedEncounterEditor != null) embeddedEncounterEditor.RefreshEmbedded();
    }

    private void OnProjectChange()
    {
        isDirtyCache = true;
        isDirtyEventCache = true;
        Repaint();
    }

    private void OnDisable()
    {
        EditorApplication.update -= FlushPendingSaveIfReady;
        FlushPendingSaveNow();

        if (embeddedEncounterEditor != null)
        {
            DestroyImmediate(embeddedEncounterEditor);
            embeddedEncounterEditor = null;
        }
    }

    private void OnLostFocus() => FlushPendingSaveNow();

    private void OnGUI()
    {
        if (editorMode == CharacterEditorMode.Events)
        {
            RefreshEventCacheIfNeeded();
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawEventLeftPanel();
            DrawEventMiddlePanel();
            DrawEventRightOverviewPanel();
            EditorGUILayout.EndHorizontal();

            FlushPendingSaveIfReady();
            return;
        }

        if (!IsCharacterMode)
        {
            EnsureEmbeddedEncounterEditor();
            EnemyEncounterEditorWindow.EditMode encounterMode = editorMode == CharacterEditorMode.Formations
                ? EnemyEncounterEditorWindow.EditMode.Encounters
                : EnemyEncounterEditorWindow.EditMode.Pools;
            embeddedEncounterEditor.DrawEmbedded(encounterMode, DrawModeToolbar);
            return;
        }

        RefreshCacheIfNeeded();
        if (selectedData != null && selectedData.isEnemy != IsEnemyMode)
        {
            selectedData = null;
            iconFolderPath = "";
        }

        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        DrawLeftPanel();
        DrawMiddlePanel();
        DrawRightOverviewPanel();
        EditorGUILayout.EndHorizontal();

        FlushPendingSaveIfReady();
    }

    private void DrawModeToolbar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        int characterModeIndex = editorMode == CharacterEditorMode.Player
            ? 0
            : editorMode == CharacterEditorMode.Enemy ? 1 : -1;
        int nextCharacterModeIndex = GUILayout.Toolbar(characterModeIndex, new[] { "아군", "적" });
        if (nextCharacterModeIndex >= 0 && nextCharacterModeIndex != characterModeIndex)
            SetEditorMode(nextCharacterModeIndex == 0 ? CharacterEditorMode.Player : CharacterEditorMode.Enemy);

        int otherModeIndex = editorMode == CharacterEditorMode.Formations
            ? 0
            : editorMode == CharacterEditorMode.Pools
            ? 1
            : editorMode == CharacterEditorMode.Events ? 2 : -1;
        int nextOtherModeIndex = GUILayout.Toolbar(otherModeIndex, new[] { "포메이션", "풀", "이벤트" });
        if (nextOtherModeIndex >= 0 && nextOtherModeIndex != otherModeIndex)
        {
            if (nextOtherModeIndex == 0) SetEditorMode(CharacterEditorMode.Formations);
            else if (nextOtherModeIndex == 1) SetEditorMode(CharacterEditorMode.Pools);
            else if (nextOtherModeIndex == 2) SetEditorMode(CharacterEditorMode.Events);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4f);
    }

    private void SetEditorMode(CharacterEditorMode nextMode)
    {
        if (editorMode == nextMode) return;

        FlushPendingSaveNow();
        editorMode = nextMode;
        selectedData = null;
        selectedEventData = null;
        iconFolderPath = "";
        leftScroll = Vector2.zero;
        middleScroll = Vector2.zero;
        rightScroll = Vector2.zero;
        GUI.FocusControl(null);

        if (!IsCharacterMode)
        {
            EnsureEmbeddedEncounterEditor();
            embeddedEncounterEditor.RefreshEmbedded();
        }
    }

    private void EnsureEmbeddedEncounterEditor()
    {
        if (embeddedEncounterEditor != null) return;

        embeddedEncounterEditor = CreateInstance<EnemyEncounterEditorWindow>();
        embeddedEncounterEditor.hideFlags = HideFlags.HideAndDontSave;
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
        BackfillMissingPlayerIds();
        cachedCharacterList.Sort(CompareCharactersForEditor);
        isDirtyCache = false;
    }

    private static int CompareCharactersForEditor(CharacterData a, CharacterData b)
    {
        if (ReferenceEquals(a, b)) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        if (a.isEnemy != b.isEnemy)
            return a.isEnemy ? 1 : -1;

        if (!a.isEnemy)
        {
            bool aHasValidId = CharacterData.IsValidCharacterId(a.characterId);
            bool bHasValidId = CharacterData.IsValidCharacterId(b.characterId);

            if (aHasValidId != bHasValidId)
                return aHasValidId ? -1 : 1;

            if (aHasValidId && bHasValidId)
            {
                int.TryParse(a.characterId, out int aId);
                int.TryParse(b.characterId, out int bId);
                int idComparison = aId.CompareTo(bId);
                if (idComparison != 0) return idComparison;
            }
            else
            {
                int invalidIdComparison = string.Compare(
                    a.characterId,
                    b.characterId,
                    System.StringComparison.OrdinalIgnoreCase);
                if (invalidIdComparison != 0) return invalidIdComparison;
            }
        }

        int nameComparison = string.Compare(
            a.DataName,
            b.DataName,
            System.StringComparison.OrdinalIgnoreCase);
        if (nameComparison != 0) return nameComparison;

        return string.Compare(
            AssetDatabase.GetAssetPath(a),
            AssetDatabase.GetAssetPath(b),
            System.StringComparison.OrdinalIgnoreCase);
    }

    private void BackfillMissingPlayerIds()
    {
        int nextId = 1;
        bool changed = false;

        cachedCharacterList.Sort((a, b) => string.Compare(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b), System.StringComparison.OrdinalIgnoreCase));
        foreach (CharacterData data in cachedCharacterList)
        {
            if (data == null || data.isEnemy) continue;

            if (string.IsNullOrWhiteSpace(data.jobName))
            {
                data.jobName = string.IsNullOrWhiteSpace(data.characterName) ? "NewAlly" : data.characterName.Trim();
                EditorUtility.SetDirty(data);
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(data.characterId)) continue;

            while (IsPlayerIdInUse(CharacterData.FormatCharacterId(nextId), data) && nextId <= 99)
                nextId++;

            if (nextId <= 99)
            {
                data.characterId = CharacterData.FormatCharacterId(nextId);
                EditorUtility.SetDirty(data);
                changed = true;
                nextId++;
            }
        }

        if (changed) AssetDatabase.SaveAssets();
    }

    private static Vector2 BeginVerticalScrollView(Vector2 scrollPosition)
    {
        scrollPosition.x = 0f;
        return GUILayout.BeginScrollView(
            scrollPosition,
            false,
            false,
            GUIStyle.none,
            GUI.skin.verticalScrollbar,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true));
    }

    private static void EndVerticalScrollView()
    {
        GUILayout.EndScrollView();
    }

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(CharacterListWidth), GUILayout.ExpandHeight(true));
        DrawModeToolbar();
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField(IsEnemyMode ? "적 캐릭터 목록" : "아군 캐릭터 목록", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("검색", EditorStyles.boldLabel);
        searchText = EditorGUILayout.TextField(searchText);

        if (GUILayout.Button(IsEnemyMode ? "+ 새 적 캐릭터 생성" : "+ 새 아군 캐릭터 생성", GUILayout.Height(30)))
        {
            CreateNewCharacter(IsEnemyMode);
            isDirtyCache = true;
        }

        EditorGUILayout.Space(5);
        leftScroll = BeginVerticalScrollView(leftScroll);
        foreach (CharacterData data in cachedCharacterList)
        {
            if (data == null || data.isEnemy != IsEnemyMode) continue;
            string displayName = GetEditorDisplayName(data);
            if (!string.IsNullOrEmpty(searchText) && !displayName.ToLower().Contains(searchText.ToLower())) continue;

            EditorGUILayout.BeginHorizontal();
            bool isSelected = selectedData == data;
            float nameButtonWidth = Mathf.Max(80f, CharacterListWidth - 60f);
            var nameContent = new GUIContent(displayName, displayName);
            if (GUILayout.Toggle(isSelected, nameContent, "Button", GUILayout.Width(nameButtonWidth), GUILayout.Height(25)) && !isSelected)
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
        EndVerticalScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawMiddlePanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(CharacterDetailWidth), GUILayout.ExpandHeight(true));
        if (selectedData == null)
        {
            EditorGUILayout.HelpBox(IsEnemyMode ? "적 캐릭터를 선택하세요." : "아군 캐릭터를 선택하세요.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        middleScroll = BeginVerticalScrollView(middleScroll);
        EditorGUI.BeginChangeCheck();

        if (string.IsNullOrEmpty(iconFolderPath))
            iconFolderPath = $"{GetIllustFolder(selectedData.isEnemy)}/{GetImageBindingKey(selectedData)}";

        DrawNameSection();
        DrawSpriteSection();
        DrawBaseStatsSection();
        DrawSynergiesSection();
        DrawValidationSection();
        DrawBulkImageSection();

        EditorGUILayout.Space(12);
        if (selectedData.isEnemy) DrawEnemyPatternEditor();
        else DrawPlayerSkillSection();

        if (EditorGUI.EndChangeCheck()) MarkDirtyAndSave(selectedData);
        EndVerticalScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawNameSection()
    {
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField(selectedData.isEnemy ? "Enemy Data" : "Player Data", EditorStyles.boldLabel);
        if (!selectedData.isEnemy)
        {
            EditorGUI.BeginChangeCheck();
            string newId = EditorGUILayout.DelayedTextField("ID (00-99)", selectedData.characterId);
            if (EditorGUI.EndChangeCheck())
            {
                selectedData.characterId = CharacterData.NormalizeCharacterId(newId);
                MarkDirtyAndSave(selectedData);
                isDirtyCache = true;
            }

            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.DelayedTextField("Name", selectedData.characterName);
            if (EditorGUI.EndChangeCheck() && !string.IsNullOrWhiteSpace(newName))
            {
                selectedData.characterName = newName.Trim();
                MarkDirtyAndSave(selectedData);
                isDirtyCache = true;
            }

            EditorGUI.BeginChangeCheck();
            string newJobName = EditorGUILayout.DelayedTextField("Job", selectedData.jobName);
            if (EditorGUI.EndChangeCheck() && !string.IsNullOrWhiteSpace(newJobName))
            {
                selectedData.jobName = newJobName.Trim();
                RenameSelectedAssetToDataName();
                MarkDirtyAndSave(selectedData);
                iconFolderPath = "";
                isDirtyCache = true;
            }

            selectedData.regionId = EditorGUILayout.TextField("Region", selectedData.regionId);

            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUI.BeginChangeCheck();
        string newEnemyName = EditorGUILayout.DelayedTextField("Name", selectedData.characterName);
        if (EditorGUI.EndChangeCheck() && !string.IsNullOrWhiteSpace(newEnemyName))
        {
            selectedData.characterName = newEnemyName.Trim();
            RenameSelectedAssetToDataName();
            MarkDirtyAndSave(selectedData);
            iconFolderPath = "";
            isDirtyCache = true;
        }

        selectedData.regionId = EditorGUILayout.TextField("Region", selectedData.regionId);
        EditorGUILayout.LabelField("역할 설명");
        selectedData.enemyRoleDescription = EditorGUILayout.TextArea(selectedData.enemyRoleDescription ?? "", GUILayout.MinHeight(42));

        EditorGUILayout.EndVertical();
        return;
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
        if (!selectedData.isEnemy)
        {
            selectedData.baseAttack = EditorGUILayout.FloatField("기본 공격력", selectedData.baseAttack);
            selectedData.spellPower = Mathf.Max(0f, EditorGUILayout.FloatField("주문력", selectedData.spellPower));
        }
        selectedData.armor = Mathf.Max(0f, EditorGUILayout.FloatField("방어력", selectedData.armor));
        selectedData.magicResist = Mathf.Max(0f, EditorGUILayout.FloatField("마법 저항력", selectedData.magicResist));
        EditorGUILayout.EndVertical();
    }

    private void DrawSynergiesSection()
    {
        if (selectedData == null) return;
        if (selectedData.isEnemy)
        {
            DrawEnemyFactionSection();
            return;
        }

        EditorGUILayout.BeginVertical("helpbox");
        if (selectedData.synergies == null)
        {
            selectedData.synergies = new System.Collections.Generic.List<TheLastArk.Data.SynergyType>();
        }

        EditorGUILayout.LabelField($"시너지 설정 (등록된 시너지: {selectedData.synergies.Count}개)", EditorStyles.boldLabel);

        // 1. 현재 등록된 시너지 뱃지 (클릭 시 삭제)
        if (selectedData.synergies.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < selectedData.synergies.Count; i++)
            {
                var synType = selectedData.synergies[i];
                var info = TheLastArk.Character.SynergyDatabase.GetInfo(synType);

                GUI.backgroundColor = new Color(0.2f, 0.6f, 0.9f);
                if (GUILayout.Button($"{info.displayName}  [X]", GUILayout.Height(24)))
                {
                    selectedData.synergies.RemoveAt(i);
                    MarkDirtyAndSave(selectedData);
                    GUI.backgroundColor = Color.white;
                    break;
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("등록된 시너지가 없습니다. 아래 버튼을 눌러 시너지를 부여하세요.", MessageType.Info);
        }

        EditorGUILayout.Space(6);

        // 2. 세력 시너지 매트릭스
        EditorGUILayout.LabelField("── [세력 시너지] ───────────────────────", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        int factionCols = 0;
        foreach (TheLastArk.Data.SynergyType synType in System.Enum.GetValues(typeof(TheLastArk.Data.SynergyType)))
        {
            var info = TheLastArk.Character.SynergyDatabase.GetInfo(synType);
            if (!info.isFaction) continue;

            bool hasSyn = selectedData.synergies.Contains(synType);
            GUI.backgroundColor = hasSyn ? new Color(0.3f, 0.9f, 0.4f) : Color.white;
            bool newHas = GUILayout.Toggle(hasSyn, info.displayName, "Button", GUILayout.Height(24));
            GUI.backgroundColor = Color.white;

            if (newHas != hasSyn)
            {
                if (newHas) selectedData.synergies.Add(synType);
                else selectedData.synergies.Remove(synType);
                MarkDirtyAndSave(selectedData);
            }

            factionCols++;
            if (factionCols % 3 == 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // 3. 직업 시너지 매트릭스
        EditorGUILayout.LabelField("── [직업 시너지] ───────────────────────", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        int jobCols = 0;
        foreach (TheLastArk.Data.SynergyType synType in System.Enum.GetValues(typeof(TheLastArk.Data.SynergyType)))
        {
            var info = TheLastArk.Character.SynergyDatabase.GetInfo(synType);
            if (info.isFaction) continue;

            if (synType == TheLastArk.Data.SynergyType.Defender || synType == TheLastArk.Data.SynergyType.Steam ||
                synType == TheLastArk.Data.SynergyType.Mechanic || synType == TheLastArk.Data.SynergyType.Vanguard) continue;

            bool hasSyn = selectedData.synergies.Contains(synType);
            GUI.backgroundColor = hasSyn ? new Color(0.3f, 0.85f, 0.95f) : Color.white;
            bool newHas = GUILayout.Toggle(hasSyn, info.displayName, "Button", GUILayout.Height(24));
            GUI.backgroundColor = Color.white;

            if (newHas != hasSyn)
            {
                if (newHas) selectedData.synergies.Add(synType);
                else selectedData.synergies.Remove(synType);
                MarkDirtyAndSave(selectedData);
            }

            jobCols++;
            if (jobCols % 3 == 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawEnemyFactionSection()
    {
        if (selectedData.synergies == null)
            selectedData.synergies = new List<TheLastArk.Data.SynergyType>();

        var factions = new List<TheLastArk.Data.SynergyType>();
        var labels = new List<string> { "없음" };
        int selectedIndex = 0;

        foreach (TheLastArk.Data.SynergyType type in System.Enum.GetValues(typeof(TheLastArk.Data.SynergyType)))
        {
            var info = TheLastArk.Character.SynergyDatabase.GetInfo(type);
            if (!info.isFaction) continue;
            factions.Add(type);
            labels.Add(info.displayName);
            if (selectedData.synergies.Contains(type)) selectedIndex = factions.Count;
        }

        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField("적 세력", EditorStyles.boldLabel);
        int nextIndex = EditorGUILayout.Popup("세력", selectedIndex, labels.ToArray());
        if (nextIndex != selectedIndex)
        {
            selectedData.synergies.RemoveAll(type => TheLastArk.Character.SynergyDatabase.GetInfo(type).isFaction);
            if (nextIndex > 0) selectedData.synergies.Add(factions[nextIndex - 1]);
            MarkDirtyAndSave(selectedData);
        }
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
            iconFolderPath = $"{GetIllustFolder(selectedData.isEnemy)}/{GetImageBindingKey(selectedData)}";
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
            string suffix = ExtractSuffix(Path.GetFileNameWithoutExtension(path), GetImageBindingKey(selectedData));
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
        EditorGUILayout.LabelField("적 행동 패턴", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("위에서 아래 순서로 실행한 뒤 처음으로 돌아갑니다.", MessageType.None);

        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField("사이클 성장", EditorStyles.miniBoldLabel);
        selectedData.damageBonusPerCycle = Mathf.Max(0f, EditorGUILayout.FloatField("사이클당 힘", selectedData.damageBonusPerCycle));
        selectedData.bleedBonusPerCycle = Mathf.Max(0f, EditorGUILayout.FloatField("사이클당 출혈", selectedData.bleedBonusPerCycle));
        EditorGUILayout.LabelField($"사이클 종료: 직접 피해 +{selectedData.damageBonusPerCycle:0.##}, 출혈 +{selectedData.bleedBonusPerCycle:0.##}", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndVertical();

        DrawPatternPresetButtons();

        for (int i = 0; i < selectedData.enemyPatterns.Count; i++)
        {
            EnemyPatternData pattern = selectedData.enemyPatterns[i] ?? CreateEnemyPattern($"패턴 {i + 1}", TargetType.SingleEnemy, EnemyTargetSelection.Random, EffectType.Damage, 5f);
            selectedData.enemyPatterns[i] = pattern;
            if (pattern.effects == null) pattern.effects = new List<EffectEntry>();

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{i + 1}", GUILayout.Width(24));
            pattern.patternName = EditorGUILayout.TextField(pattern.patternName);
            if (GUILayout.Button("복", GUILayout.Width(22)))
            {
                selectedData.enemyPatterns.Insert(i + 1, CloneEnemyPattern(pattern));
                MarkDirtyAndSave(selectedData);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            if (GUILayout.Button("↑", GUILayout.Width(22)) && i > 0) { SwapEnemyPatterns(i, i - 1); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break; }
            if (GUILayout.Button("↓", GUILayout.Width(22)) && i < selectedData.enemyPatterns.Count - 1) { SwapEnemyPatterns(i, i + 1); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break; }
            if (GUILayout.Button("×", GUILayout.Width(22))) { selectedData.enemyPatterns.RemoveAt(i); MarkDirtyAndSave(selectedData); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break; }
            EditorGUILayout.EndHorizontal();

            EnemyTargetSide side = GetEnemyTargetSide(pattern.targetType);
            EnemyTargetRange range = GetEnemyTargetRange(pattern.targetType);
            DrawTargetSettings(ref side, ref pattern.targetSelection, ref range);

            if (side == EnemyTargetSide.Self) range = EnemyTargetRange.Single;
            pattern.targetType = ToTargetType(side, range);
            DrawEnemyEffectList(pattern.effects);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawPatternPresetButtons()
    {
        int columns = position.width >= 900f ? 4 : 2;
        int clicked = GUILayout.SelectionGrid(-1, new[] { "단일유저", "전체유저", "단일적", "전체적" }, columns, GUILayout.Height(20));
        switch (clicked)
        {
            case 0: AddEnemyPattern(CreateEnemyPattern("단일 공격", TargetType.SingleEnemy, EnemyTargetSelection.Random, EffectType.Damage, 5f)); break;
            case 1: AddEnemyPattern(CreateEnemyPattern("전체 공격", TargetType.AllEnemy, EnemyTargetSelection.Random, EffectType.Damage, 4f)); break;
            case 2: AddEnemyPattern(CreateEnemyPattern("단일 지원", TargetType.Friendly, EnemyTargetSelection.LowestHp, EffectType.Heal, 5f)); break;
            case 3: AddEnemyPattern(CreateEnemyPattern("전체 지원", TargetType.AllFriendly, EnemyTargetSelection.Random, EffectType.Strength, 20f)); break;
        }
    }

    private static void DrawTargetSettings(ref EnemyTargetSide side, ref EnemyTargetSelection selection, ref EnemyTargetRange range)
    {
        string[] sideLabels = { "유저", "적", "자신" };
        string[] selectionLabels = { "랜덤", "리더", "HP 최저", "HP 최고" };
        string[] rangeLabels = { "단일", "왼쪽", "오른쪽", "양옆", "전체" };

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("대상", GUILayout.Width(28));
        side = (EnemyTargetSide)EditorGUILayout.Popup((int)side, sideLabels, GUILayout.Width(65));
        GUILayout.Label("선택", GUILayout.Width(28));
        selection = (EnemyTargetSelection)EditorGUILayout.Popup((int)selection, selectionLabels, GUILayout.Width(72));
        GUILayout.Label("범위", GUILayout.Width(28));
        range = (EnemyTargetRange)EditorGUILayout.Popup((int)range, rangeLabels, GUILayout.Width(65));
        EditorGUILayout.EndHorizontal();
    }

    private void AddEnemyPattern(EnemyPatternData pattern)
    {
        selectedData.enemyPatterns.Add(pattern);
        MarkDirtyAndSave(selectedData);
    }

    private static EnemyPatternData CreateEnemyPattern(string name, TargetType targetType, EnemyTargetSelection selection, EffectType effectType, float value)
    {
        return new EnemyPatternData
        {
            patternName = name,
            targetType = targetType,
            targetSelection = selection,
            effects = new List<EffectEntry>
            {
                new EffectEntry
                {
                    type = effectType,
                    damageType = DamageType.Physical,
                    multiplier = 0f,
                    fixedValue = effectType == EffectType.Damage || effectType == EffectType.Heal ? value : 0f,
                    value = effectType == EffectType.Strength ? value : 0f
                }
            }
        };
    }

    private static EnemyPatternData CloneEnemyPattern(EnemyPatternData source)
    {
        var copy = new EnemyPatternData
        {
            patternName = $"{source.patternName} 복사",
            targetType = source.targetType,
            targetSelection = source.targetSelection,
            customVfxName = source.customVfxName,
            effects = new List<EffectEntry>()
        };

        foreach (EffectEntry effect in source.effects)
        {
            copy.effects.Add(new EffectEntry
            {
                type = effect.type,
                damageType = effect.damageType,
                multiplier = effect.multiplier,
                fixedValue = effect.fixedValue,
                useActualResult = effect.useActualResult,
                value = effect.value,
                secondaryValue = effect.secondaryValue,
                hitCount = effect.hitCount,
                duration = effect.duration,
                charges = effect.charges,
                skillSlot = effect.skillSlot,
                customVfxName = effect.customVfxName
            });
        }
        return copy;
    }

    private static EnemyTargetSide GetEnemyTargetSide(TargetType type)
    {
        if (type == TargetType.Self) return EnemyTargetSide.Self;
        return type == TargetType.Friendly || type == TargetType.AllFriendly
            || type == TargetType.FriendlyLeft || type == TargetType.FriendlyRight || type == TargetType.FriendlyAdjacent
            ? EnemyTargetSide.Enemy
            : EnemyTargetSide.User;
    }

    private static EnemyTargetRange GetEnemyTargetRange(TargetType type)
    {
        switch (type)
        {
            case TargetType.LeftEnemy:
            case TargetType.FriendlyLeft: return EnemyTargetRange.Left;
            case TargetType.RightEnemy:
            case TargetType.FriendlyRight: return EnemyTargetRange.Right;
            case TargetType.AdjacentEnemy:
            case TargetType.FriendlyAdjacent: return EnemyTargetRange.Adjacent;
            case TargetType.AllEnemy:
            case TargetType.AllFriendly: return EnemyTargetRange.All;
            default: return EnemyTargetRange.Single;
        }
    }

    private static TargetType ToTargetType(EnemyTargetSide side, EnemyTargetRange range)
    {
        if (side == EnemyTargetSide.Self) return TargetType.Self;
        if (side == EnemyTargetSide.Enemy)
        {
            switch (range)
            {
                case EnemyTargetRange.Left: return TargetType.FriendlyLeft;
                case EnemyTargetRange.Right: return TargetType.FriendlyRight;
                case EnemyTargetRange.Adjacent: return TargetType.FriendlyAdjacent;
                case EnemyTargetRange.All: return TargetType.AllFriendly;
                default: return TargetType.Friendly;
            }
        }

        switch (range)
        {
            case EnemyTargetRange.Left: return TargetType.LeftEnemy;
            case EnemyTargetRange.Right: return TargetType.RightEnemy;
            case EnemyTargetRange.Adjacent: return TargetType.AdjacentEnemy;
            case EnemyTargetRange.All: return TargetType.AllEnemy;
            default: return TargetType.SingleEnemy;
        }
    }

    private enum EnemyEffectPreset
    {
        PhysicalDamage,
        MagicalDamage,
        TrueDamage,
        Heal,
        Stun,
        Bleed,
        Taunt,
        Counter,
        Shield,
        Resurrection
    }

    private void DrawEnemyEffectList(List<EffectEntry> effects)
    {
        if (effects == null) return;

        DrawEffectPaletteToggle(effects);
        DrawStatusPaletteToggle(effects);

        string[] effectLabels =
        {
            "물리 피해", "마법 피해", "고정 피해", "회복",
            "기절", "출혈", "도발", "반격", "보호막", "부활"
        };

        for (int i = 0; i < effects.Count; i++)
        {
            EffectEntry effect = effects[i];
            EnemyEffectPreset current = GetEffectPreset(effect);

            EditorGUILayout.BeginHorizontal();
            if (IsConfiguredStatus(effect.type))
                effect.type = (EffectType)EditorGUILayout.EnumPopup(effect.type, GUILayout.Width(100));
            else
            {
                EnemyEffectPreset next = (EnemyEffectPreset)EditorGUILayout.Popup((int)current, effectLabels, GUILayout.Width(85));
                if (next != current) ApplyEffectPreset(effect, next, true);
            }

            if (IsConfiguredStatus(effect.type))
            {
                DrawConfiguredStatusFields(effect);
            }
            else if (effect.type == EffectType.Damage)
            {
                effect.damageType = (DamageType)EditorGUILayout.EnumPopup(effect.damageType, GUILayout.Width(58));
                DrawEnemyFixedValueField(effect, "피해");
                GUILayout.Label("타격", GUILayout.Width(28));
                effect.hitCount = Mathf.Max(1, EditorGUILayout.IntField(Mathf.Max(1, effect.hitCount), GUILayout.Width(32)));
            }
            else if (effect.type == EffectType.Heal)
            {
                DrawEnemyFixedValueField(effect, "회복");
            }
            else if (effect.type == EffectType.Shield)
            {
                DrawEnemyFixedValueField(effect, "보호막");
                GUILayout.Label("턴", GUILayout.Width(18));
                effect.duration = Mathf.Max(1, EditorGUILayout.IntField(effect.duration, GUILayout.Width(32)));
            }
            GUILayout.Label("연계", GUILayout.Width(28));
            effect.useActualResult = EditorGUILayout.Toggle(effect.useActualResult, GUILayout.Width(18));
            if (GUILayout.Button("X", GUILayout.Width(22))) { effects.RemoveAt(i); i--; }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawEnemyFixedValueField(EffectEntry effect, string label)
    {
        float legacyBase = effect.type == EffectType.Damage && effect.damageType == DamageType.Magical
            ? selectedData.spellPower
            : selectedData.baseAttack;
        bool isLegacy = effect.fixedValue <= 0f && !Mathf.Approximately(effect.multiplier, 0f);
        float displayedValue = isLegacy ? legacyBase * effect.multiplier : effect.fixedValue;

        GUILayout.Label(label, GUILayout.Width(40));
        EditorGUI.BeginChangeCheck();
        float nextValue = Mathf.Max(0f, EditorGUILayout.FloatField(displayedValue, GUILayout.Width(45)));
        if (EditorGUI.EndChangeCheck())
        {
            effect.fixedValue = nextValue;
            effect.multiplier = 0f;
            effect.useActualResult = false;
        }
    }

    private static EffectEntry CreateEffect(EnemyEffectPreset preset)
    {
        var effect = new EffectEntry();
        ApplyEffectPreset(effect, preset, true);
        return effect;
    }

    private static EnemyEffectPreset GetEffectPreset(EffectEntry effect)
    {
        if (effect.type == EffectType.Damage)
        {
            if (effect.damageType == DamageType.Magical) return EnemyEffectPreset.MagicalDamage;
            if (effect.damageType == DamageType.True) return EnemyEffectPreset.TrueDamage;
            return EnemyEffectPreset.PhysicalDamage;
        }

        switch (effect.type)
        {
            case EffectType.Heal: return EnemyEffectPreset.Heal;
            case EffectType.Stun: return EnemyEffectPreset.Stun;
            case EffectType.Bleed: return EnemyEffectPreset.Bleed;
            case EffectType.Taunt: return EnemyEffectPreset.Taunt;
            case EffectType.Counter: return EnemyEffectPreset.Counter;
            case EffectType.Shield: return EnemyEffectPreset.Shield;
            case EffectType.Resurrection: return EnemyEffectPreset.Resurrection;
            default: return EnemyEffectPreset.PhysicalDamage;
        }
    }

    private static void ApplyEffectPreset(EffectEntry effect, EnemyEffectPreset preset, bool resetValues)
    {
        effect.type = EffectType.Damage;
        effect.damageType = DamageType.Physical;

        switch (preset)
        {
            case EnemyEffectPreset.MagicalDamage: effect.damageType = DamageType.Magical; break;
            case EnemyEffectPreset.TrueDamage: effect.damageType = DamageType.True; break;
            case EnemyEffectPreset.Heal: effect.type = EffectType.Heal; break;
            case EnemyEffectPreset.Stun: effect.type = EffectType.Stun; break;
            case EnemyEffectPreset.Bleed: effect.type = EffectType.Bleed; break;
            case EnemyEffectPreset.Taunt: effect.type = EffectType.Taunt; break;
            case EnemyEffectPreset.Counter: effect.type = EffectType.Counter; break;
            case EnemyEffectPreset.Shield: effect.type = EffectType.Shield; break;
            case EnemyEffectPreset.Resurrection: effect.type = EffectType.Resurrection; break;
        }

        if (!resetValues) return;
        effect.multiplier = 0f;
        effect.fixedValue = preset == EnemyEffectPreset.PhysicalDamage || preset == EnemyEffectPreset.MagicalDamage
            || preset == EnemyEffectPreset.TrueDamage || preset == EnemyEffectPreset.Heal || preset == EnemyEffectPreset.Shield ? 5f : 0f;
        effect.value = preset == EnemyEffectPreset.Bleed ? 1f : 0f;
        effect.hitCount = 1;
        effect.duration = 3;
        effect.useActualResult = false;
    }

    private void DrawEffectList(List<EffectEntry> effects)
    {
        if (effects == null) return;
        DrawEffectPaletteToggle(effects);
        DrawStatusPaletteToggle(effects);
        for (int i = 0; i < effects.Count; i++)
        {
            EffectEntry effect = effects[i];
            EditorGUILayout.BeginHorizontal();
            EffectType previousType = effect.type;
            effect.type = (EffectType)EditorGUILayout.EnumPopup(effect.type, GUILayout.Width(90));
            if (effect.type != previousType && RequiresDuration(effect.type)) effect.duration = 3;
            if (IsConfiguredStatus(effect.type))
            {
                DrawConfiguredStatusFields(effect);
            }
            else
            {
            if (effect.type == EffectType.Damage)
                effect.damageType = (DamageType)EditorGUILayout.EnumPopup(effect.damageType, GUILayout.Width(80));
            GUILayout.Label("배율", GUILayout.Width(32));
            effect.multiplier = EditorGUILayout.FloatField(effect.multiplier, GUILayout.Width(45));
            GUILayout.Label("고정", GUILayout.Width(32));
            effect.fixedValue = EditorGUILayout.FloatField(effect.fixedValue, GUILayout.Width(45));
            if (effect.type == EffectType.Shield)
            {
                GUILayout.Label("턴", GUILayout.Width(18));
                effect.duration = Mathf.Max(1, EditorGUILayout.IntField(effect.duration, GUILayout.Width(32)));
            }
            }
            GUILayout.Label("결과", GUILayout.Width(32));
            effect.useActualResult = EditorGUILayout.Toggle(effect.useActualResult, GUILayout.Width(20));
            if (GUILayout.Button("X", GUILayout.Width(20))) { effects.RemoveAt(i); break; }
            EditorGUILayout.EndHorizontal();
        }
    }

    private static bool RequiresDuration(EffectType type)
    {
        return type == EffectType.Stun || type == EffectType.Bleed || type == EffectType.Focus
            || type == EffectType.Taunt || type == EffectType.Counter || type == EffectType.Shield
            || type == EffectType.Poison || type == EffectType.Burn;
    }

    private static bool IsConfiguredStatus(EffectType type)
    {
        return (int)type >= (int)EffectType.Blockade || type == EffectType.Stun || type == EffectType.Bleed
            || type == EffectType.Poison || type == EffectType.Burn || type == EffectType.Strength
            || type == EffectType.Taunt || type == EffectType.Counter || type == EffectType.Focus;
    }

    private static bool UsesCharges(EffectType type)
    {
        return type == EffectType.Counter || type == EffectType.Taunt || type == EffectType.Guard;
    }

    private static void DrawConfiguredStatusFields(EffectEntry effect)
    {
        if (effect.type == EffectType.Blockade)
        {
            GUILayout.Label("스킬칸", GUILayout.Width(42));
            effect.skillSlot = EditorGUILayout.IntPopup(effect.skillSlot,
                new[] { "모두", "1", "2", "3", "4" }, new[] { -1, 0, 1, 2, 3 }, GUILayout.Width(48));
        }
        else if (effect.type != EffectType.Fatigue && effect.type != EffectType.Stun
            && effect.type != EffectType.Focus && !UsesCharges(effect.type))
        {
            string valueLabel = effect.type == EffectType.Confusion ? "확률%" : effect.type == EffectType.Frost ? "AP+" : "수치";
            GUILayout.Label(valueLabel, GUILayout.Width(38));
            effect.value = EditorGUILayout.FloatField(effect.value, GUILayout.Width(45));
        }

        if (UsesCharges(effect.type))
        {
            GUILayout.Label("횟수", GUILayout.Width(32));
            effect.charges = Mathf.Max(1, EditorGUILayout.IntField(effect.charges, GUILayout.Width(35)));
        }
        else
        {
            GUILayout.Label("지속시간", GUILayout.Width(52));
            effect.duration = Mathf.Max(1, EditorGUILayout.IntField(effect.duration, GUILayout.Width(35)));
        }
    }

    private void DrawStatusPaletteToggle(List<EffectEntry> effects)
    {
        bool isOpen = ReferenceEquals(statusPaletteTarget, effects);
        if (GUILayout.Button(isOpen ? "- 상태이상 닫기" : "+ 상태이상", GUILayout.Width(110)))
        {
            statusPaletteTarget = isOpen ? null : effects;
            if (!isOpen) effectPaletteTarget = null;
        }
        if (!ReferenceEquals(statusPaletteTarget, effects)) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        DrawStatusGroup(effects, "행동 제한", new[] { "봉쇄", "피로", "혼란", "기절", "냉기" },
            new[] { EffectType.Blockade, EffectType.Fatigue, EffectType.Confusion, EffectType.Stun, EffectType.Frost },
            new[] { 0f, 0f, 30f, 0f, 1f });
        DrawStatusGroup(effects, "지속 피해", new[] { "출혈", "독", "화상" },
            new[] { EffectType.Bleed, EffectType.Poison, EffectType.Burn }, new[] { 10f, 20f, 5f });
        DrawStatusGroup(effects, "정신력", new[] { "공포", "압박", "절망" },
            new[] { EffectType.Fear, EffectType.Pressure, EffectType.Despair }, new[] { 5f, 3f, 30f });
        DrawStatusGroup(effects, "능력치", new[] { "힘", "약화", "보호", "취약", "관통" },
            new[] { EffectType.Strength, EffectType.Weakness, EffectType.Protection, EffectType.Vulnerable, EffectType.Pierce },
            new[] { 20f, 20f, 20f, 20f, 20f });
        DrawStatusGroup(effects, "대응/보호", new[] { "반격", "도발", "방호" },
            new[] { EffectType.Counter, EffectType.Taunt, EffectType.Guard }, new[] { 0f, 0f, 0f });
        DrawStatusGroup(effects, "특수", new[] { "집중" },
            new[] { EffectType.Focus }, new[] { 0f });
        EditorGUILayout.EndVertical();
    }

    private void DrawStatusGroup(List<EffectEntry> effects, string title, string[] labels, EffectType[] types, float[] values)
    {
        EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
        int selected = GUILayout.SelectionGrid(-1, labels, Mathf.Min(5, labels.Length), GUILayout.Height(22));
        if (selected < 0) return;

        EffectType type = types[selected];
        effects.Add(new EffectEntry
        {
            type = type,
            value = values[selected],
            duration = 3,
            charges = 3,
            skillSlot = type == EffectType.Blockade ? 0 : -1
        });
        GUI.changed = true;
    }

    private void DrawEffectPaletteToggle(List<EffectEntry> effects)
    {
        bool isOpen = ReferenceEquals(effectPaletteTarget, effects);
        if (GUILayout.Button(isOpen ? "- 효과 닫기" : "+ 효과 추가", GUILayout.Width(110)))
        {
            effectPaletteTarget = isOpen ? null : effects;
            if (!isOpen) statusPaletteTarget = null;
        }
        if (!ReferenceEquals(effectPaletteTarget, effects)) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("즉시 효과", EditorStyles.miniBoldLabel);
        string[] labels = { "물리 피해", "마법 피해", "고정 피해", "회복", "보호막", "부활" };
        int selected = GUILayout.SelectionGrid(-1, labels, 4, GUILayout.Height(44));
        if (selected >= 0)
        {
            bool enemyFixedValue = selectedData != null && selectedData.isEnemy;
            EffectEntry effect = selected switch
            {
                0 => new EffectEntry { type = EffectType.Damage, damageType = DamageType.Physical, multiplier = enemyFixedValue ? 0f : 1f, fixedValue = enemyFixedValue ? 5f : 0f },
                1 => new EffectEntry { type = EffectType.Damage, damageType = DamageType.Magical, multiplier = enemyFixedValue ? 0f : 1f, fixedValue = enemyFixedValue ? 5f : 0f },
                2 => new EffectEntry { type = EffectType.Damage, damageType = DamageType.True, multiplier = enemyFixedValue ? 0f : 1f, fixedValue = enemyFixedValue ? 5f : 0f },
                3 => new EffectEntry { type = EffectType.Heal, multiplier = enemyFixedValue ? 0f : 1f, fixedValue = enemyFixedValue ? 5f : 0f },
                4 => new EffectEntry { type = EffectType.Shield, multiplier = enemyFixedValue ? 0f : 1f, fixedValue = enemyFixedValue ? 5f : 0f, duration = 3 },
                _ => new EffectEntry { type = EffectType.Resurrection }
            };
            effects.Add(effect);
            GUI.changed = true;
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawRightOverviewPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(CharacterSheetWidth), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField(IsEnemyMode ? "적 데이터 시트" : "아군 데이터 시트", EditorStyles.boldLabel);
        rightScroll = BeginVerticalScrollView(rightScroll);
        float rowWidth = Mathf.Max(180f, CharacterSheetWidth - 18f);
        float firstRowFieldsWidth = Mathf.Max(90f, rowWidth - 94f);
        float statFieldsWidth = Mathf.Max(180f, rowWidth - 114f);
        float hpFieldWidth = statFieldsWidth * 0.2f;
        float smallStatFieldWidth = statFieldsWidth * 0.15f;
        foreach (CharacterData data in cachedCharacterList)
        {
            if (data == null || data.isEnemy != IsEnemyMode) continue;
            if (selectedData == data) GUI.backgroundColor = Color.cyan;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(ValidateCharacterData(data).Summary, GUILayout.Width(40));
            EditorGUI.BeginChangeCheck();
            if (data.isEnemy)
            {
                data.characterName = EditorGUILayout.TextField(data.characterName, GUILayout.Width(firstRowFieldsWidth));
            }
            else
            {
                string previousId = data.characterId;
                float idWidth = Mathf.Max(26f, firstRowFieldsWidth * 0.14f);
                float jobWidth = Mathf.Max(42f, firstRowFieldsWidth * 0.32f);
                float nameWidth = Mathf.Max(42f, firstRowFieldsWidth - idWidth - jobWidth - 4f);
                data.characterId = CharacterData.NormalizeCharacterId(EditorGUILayout.TextField(data.characterId, GUILayout.Width(idWidth)));
                if (data.characterId != previousId) isDirtyCache = true;
                data.jobName = EditorGUILayout.TextField(data.jobName, GUILayout.Width(jobWidth));
                data.characterName = EditorGUILayout.TextField(data.characterName, GUILayout.Width(nameWidth));
            }
            bool nameChanged = EditorGUI.EndChangeCheck();
            if (GUILayout.Button("선택", GUILayout.Width(40))) { selectedData = data; iconFolderPath = ""; }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            GUILayout.Label("HP", GUILayout.Width(18));
            data.maxHp = EditorGUILayout.IntField(Mathf.RoundToInt(data.maxHp), GUILayout.Width(hpFieldWidth));
            GUILayout.Label("MP", GUILayout.Width(18));
            data.maxMental = EditorGUILayout.IntField(Mathf.RoundToInt(data.maxMental), GUILayout.Width(hpFieldWidth));
            if (!data.isEnemy)
            {
                GUILayout.Label("공", GUILayout.Width(14));
                data.baseAttack = EditorGUILayout.IntField(Mathf.RoundToInt(data.baseAttack), GUILayout.Width(smallStatFieldWidth));
                GUILayout.Label("마", GUILayout.Width(14));
                data.spellPower = EditorGUILayout.IntField(Mathf.RoundToInt(data.spellPower), GUILayout.Width(smallStatFieldWidth));
            }
            GUILayout.Label("방", GUILayout.Width(14));
            data.armor = EditorGUILayout.IntField(Mathf.RoundToInt(data.armor), GUILayout.Width(smallStatFieldWidth));
            GUILayout.Label("저", GUILayout.Width(14));
            data.magicResist = EditorGUILayout.IntField(Mathf.RoundToInt(data.magicResist), GUILayout.Width(smallStatFieldWidth));
            if (EditorGUI.EndChangeCheck() || nameChanged)
            {
                MarkDirtyAndSave(data);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
        }
        EndVerticalScrollView();
        EditorGUILayout.EndVertical();
    }

    private void CreateNewCharacter(bool isEnemy)
    {
        // 새 캐릭터 데이터는 종류에 따라 정해진 리소스 폴더에 바로 만듭니다.
        string folder = GetDataFolder(isEnemy);
        EnsureAssetFolder(folder);
        string defaultDataName = isEnemy ? "NewEnemy" : "NewAlly";
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{defaultDataName}_Data.asset");
        CharacterData data = CreateInstance<CharacterData>();
        InitData(data, isEnemy);
        data.characterName = NormalizeCharacterName(Path.GetFileNameWithoutExtension(path), isEnemy);
        if (!isEnemy)
        {
            int nextId = GetNextPlayerId();
            if (nextId > 99)
            {
                EditorUtility.DisplayDialog("ID limit reached", "Player character IDs are limited to 00-99.", "OK");
                DestroyImmediate(data);
                return;
            }

            data.characterId = CharacterData.FormatCharacterId(nextId);
            data.jobName = NormalizeCharacterName(Path.GetFileNameWithoutExtension(path), false);
        }
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
        int nextId = origin.isEnemy ? -1 : GetNextPlayerId();
        if (!origin.isEnemy && nextId > 99)
        {
            EditorUtility.DisplayDialog("ID limit reached", "Player character IDs are limited to 00-99.", "OK");
            return;
        }

        string copyName = $"{origin.DataName}_Copy";
        string newPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{copyName}_Data.asset");
        AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(origin), newPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        CharacterData copy = AssetDatabase.LoadAssetAtPath<CharacterData>(newPath);
        if (copy != null && !copy.isEnemy)
        {
            copy.characterId = CharacterData.FormatCharacterId(nextId);
            copy.jobName = copyName;
            copy.characterName = $"{origin.characterName}_Copy";
            EditorUtility.SetDirty(copy);
            AssetDatabase.SaveAssets();
        }
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
        data.characterId = isEnemy ? "" : "00";
        data.characterName = isEnemy ? "NewEnemy" : "NewAlly";
        data.jobName = isEnemy ? "" : "NewAlly";
        data.maxHp = Mathf.Max(1f, data.maxHp);
        data.maxMental = Mathf.Max(1f, data.maxMental);
        data.baseAttack = Mathf.Max(1f, data.baseAttack);
        data.spellPower = Mathf.Max(0f, data.spellPower);
        data.armor = Mathf.Max(0f, data.armor);
        data.magicResist = Mathf.Max(0f, data.magicResist);
        data.levelStatMultipliers = new float[5] { 0f, 0.5f, 1.0f, 2.0f, 3.0f };

        if (isEnemy)
        {
            data.enemyRoleDescription = "";
            data.damageBonusPerCycle = 0f;
            data.bleedBonusPerCycle = 0f;
            data.passiveSkill = null;
            data.activeSkills = null;
            data.enemyPatterns = new List<EnemyPatternData>
            {
                new EnemyPatternData
                {
                    patternName = "패턴 1",
                    targetType = TargetType.SingleEnemy,
                    effects = new List<EffectEntry>
                    {
                        new EffectEntry { type = EffectType.Damage, multiplier = 0f, fixedValue = 5f, hitCount = 1 }
                    }
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
        if (data.armor < 0f) result.errors.Add("방어력은 0 이상이어야 합니다.");
        if (data.magicResist < 0f) result.errors.Add("마법 저항력은 0 이상이어야 합니다.");
        if (!data.isEnemy)
        {
            if (data.baseAttack <= 0f) result.errors.Add("기본 공격력은 0보다 커야 합니다.");
            if (data.spellPower < 0f) result.errors.Add("주문력은 0 이상이어야 합니다.");
            if (!CharacterData.IsValidCharacterId(data.characterId)) result.errors.Add("Player ID must be 00-99.");
            else if (IsPlayerIdInUse(data.characterId, data)) result.errors.Add($"Duplicate player ID: {data.characterId}");

            if (string.IsNullOrWhiteSpace(data.jobName)) result.errors.Add("Job name is empty.");
            else if (!string.IsNullOrEmpty(path) && Path.GetFileNameWithoutExtension(path) != $"{data.jobName}_Data")
                result.warnings.Add($"Asset name should be {data.jobName}_Data.");
        }
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
            result.errors.Add("행동 패턴이 없습니다.");
            return;
        }
        for (int i = 0; i < data.enemyPatterns.Count; i++)
        {
            EnemyPatternData pattern = data.enemyPatterns[i];
            if (pattern == null)
            {
                result.errors.Add($"{i + 1}번 패턴이 없습니다.");
                continue;
            }
            if (pattern.effects == null || pattern.effects.Count == 0)
            {
                result.errors.Add($"{i + 1}번 패턴에 효과가 없습니다.");
                continue;
            }

            for (int effectIndex = 0; effectIndex < pattern.effects.Count; effectIndex++)
            {
                EffectEntry effect = pattern.effects[effectIndex];
                if (effect == null)
                {
                    result.errors.Add($"{i + 1}번 패턴의 {effectIndex + 1}번 효과가 없습니다.");
                    continue;
                }
                if (effect.type == EffectType.Damage && effect.fixedValue <= 0f && Mathf.Approximately(effect.multiplier, 0f))
                    result.errors.Add($"{i + 1}번 패턴의 피해량은 0보다 커야 합니다.");
                if (effect.type == EffectType.Damage && effect.hitCount < 1)
                    result.errors.Add($"{i + 1}번 패턴의 타격 횟수는 1 이상이어야 합니다.");
            }
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

    private string GetEditorDisplayName(CharacterData data)
    {
        if (data == null) return "(None)";
        if (data.isEnemy) return string.IsNullOrWhiteSpace(data.characterName) ? "(No Name)" : data.characterName;
        return $"{data.characterId} / {data.jobName} / {data.characterName}";
    }

    private string GetImageBindingKey(CharacterData data)
    {
        if (data == null) return "";
        return data.isEnemy ? data.characterName : data.jobName;
    }

    private int GetNextPlayerId(CharacterData excluded = null)
    {
        int maxId = 0;
        foreach (CharacterData data in cachedCharacterList)
        {
            if (data == null || data == excluded || data.isEnemy) continue;
            if (!CharacterData.IsValidCharacterId(data.characterId)) continue;
            if (int.TryParse(data.characterId, out int id) && id > maxId)
                maxId = id;
        }

        return maxId + 1;
    }

    private bool IsPlayerIdInUse(string id, CharacterData excluded = null)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        foreach (CharacterData data in cachedCharacterList)
        {
            if (data == null || data == excluded || data.isEnemy) continue;
            if (data.characterId == id) return true;
        }
        return false;
    }

    private void RenameSelectedAssetToDataName()
    {
        if (selectedData == null) return;

        string dataName = selectedData.DataName;
        if (string.IsNullOrWhiteSpace(dataName)) return;

        string assetPath = AssetDatabase.GetAssetPath(selectedData);
        if (string.IsNullOrEmpty(assetPath)) return;

        string error = AssetDatabase.RenameAsset(assetPath, $"{dataName}_Data");
        if (!string.IsNullOrEmpty(error))
            Debug.LogWarning($"[CharacterEditor] Rename failed: {error}");
        AssetDatabase.Refresh();
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

    // ─────────────────────────────────────────────────────────────
    // 이벤트 편집기 (Events Tab) 구현
    // ─────────────────────────────────────────────────────────────

    private void RefreshEventCacheIfNeeded()
    {
        if (!isDirtyEventCache) return;
        cachedEventList.Clear();
        string[] guids = AssetDatabase.FindAssets("t:GameEventData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameEventData data = AssetDatabase.LoadAssetAtPath<GameEventData>(path);
            if (data != null) cachedEventList.Add(data);
        }
        cachedEventList.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        isDirtyEventCache = false;
    }

    private void CreateNewEvent()
    {
        EnsureAssetFolder("Assets/Resources/Events/Common");
        EnsureAssetFolder("Assets/Resources/Events/Stage1");
        EnsureAssetFolder("Assets/Resources/Events/Stage2");
        EnsureAssetFolder("Assets/Resources/Events/Stage3");

        string subFolder = eventCategoryFilter == 2 ? "Stage1" : eventCategoryFilter == 3 ? "Stage2" : eventCategoryFilter == 4 ? "Stage3" : "Common";
        string folderPath = $"Assets/Resources/Events/{subFolder}";
        int count = 1;
        string assetPath = $"{folderPath}/NewEvent_{count}.asset";
        while (AssetDatabase.LoadAssetAtPath<GameEventData>(assetPath) != null)
        {
            count++;
            assetPath = $"{folderPath}/NewEvent_{count}.asset";
        }

        GameEventData newEvent = ScriptableObject.CreateInstance<GameEventData>();
        newEvent.eventID = $"evt_new_{count}_{System.DateTime.Now:HHmmss}";
        newEvent.eventTitle = $"새 이벤트 {count}";
        newEvent.eventDescription = "이벤트에 대한 설명문을 입력하세요.";

        EventOption defaultOption = new EventOption
        {
            optionText = "첫 번째 선택지",
            requirementType = EventRequirementType.None,
            outcomes = new List<EventOutcome>
            {
                new EventOutcome
                {
                    outcomeText = "결과 텍스트를 입력하세요.",
                    probability = 100,
                    rewards = new List<EventReward>()
                }
            }
        };
        newEvent.options = new List<EventOption> { defaultOption };

        AssetDatabase.CreateAsset(newEvent, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        selectedEventData = newEvent;
        isDirtyEventCache = true;
        Debug.Log($"[CharacterEditor] 새 이벤트 생성 완료: {assetPath}");
    }

    private void DuplicateEvent(GameEventData target)
    {
        if (target == null) return;

        string sourcePath = AssetDatabase.GetAssetPath(target);
        string directory = Path.GetDirectoryName(sourcePath);
        string filename = Path.GetFileNameWithoutExtension(sourcePath);
        string newPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{filename}_Copy.asset");

        if (AssetDatabase.CopyAsset(sourcePath, newPath))
        {
            AssetDatabase.Refresh();
            GameEventData copy = AssetDatabase.LoadAssetAtPath<GameEventData>(newPath);
            if (copy != null)
            {
                copy.eventID = $"{copy.eventID}_copy";
                copy.eventTitle = $"{copy.eventTitle} (복사본)";
                EditorUtility.SetDirty(copy);
                AssetDatabase.SaveAssets();
                selectedEventData = copy;
                isDirtyEventCache = true;
            }
        }
    }

    private void DeleteEvent(GameEventData target)
    {
        if (target == null) return;
        string path = AssetDatabase.GetAssetPath(target);
        if (EditorUtility.DisplayDialog("이벤트 삭제", $"정말로 '{target.eventTitle}' 이벤트를 삭제하시겠습니까?\n\n경로: {path}", "삭제", "취소"))
        {
            if (selectedEventData == target) selectedEventData = null;
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
            isDirtyEventCache = true;
        }
    }

    private void DrawEventLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(EventListWidth), GUILayout.ExpandHeight(true));
        DrawModeToolbar();
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("이벤트 목록", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("카테고리:", GUILayout.Width(65));
        eventCategoryFilter = EditorGUILayout.Popup(eventCategoryFilter, new[] { "전체", "Common", "Stage1", "Stage2", "Stage3" });
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("검색", EditorStyles.boldLabel);
        eventSearchText = EditorGUILayout.TextField(eventSearchText);

        if (GUILayout.Button("+ 새 이벤트 생성", GUILayout.Height(30)))
        {
            CreateNewEvent();
        }

        EditorGUILayout.Space(5);
        eventLeftScroll = BeginVerticalScrollView(eventLeftScroll);

        foreach (GameEventData data in cachedEventList)
        {
            if (data == null) continue;
            string assetPath = AssetDatabase.GetAssetPath(data);

            if (eventCategoryFilter == 1 && !assetPath.Contains("/Events/Common/")) continue;
            if (eventCategoryFilter == 2 && !assetPath.Contains("/Events/Stage1/")) continue;
            if (eventCategoryFilter == 3 && !assetPath.Contains("/Events/Stage2/")) continue;
            if (eventCategoryFilter == 4 && !assetPath.Contains("/Events/Stage3/")) continue;

            string title = string.IsNullOrWhiteSpace(data.eventTitle) ? data.name : data.eventTitle;
            if (!string.IsNullOrEmpty(eventSearchText) && !title.ToLower().Contains(eventSearchText.ToLower()) && !data.name.ToLower().Contains(eventSearchText.ToLower())) continue;

            EditorGUILayout.BeginHorizontal();
            bool isSelected = selectedEventData == data;
            string category = assetPath.Contains("/Events/Common/") ? "Common"
                : assetPath.Contains("/Events/Stage1/") ? "Stage1"
                : assetPath.Contains("/Events/Stage2/") ? "Stage2"
                : assetPath.Contains("/Events/Stage3/") ? "Stage3"
                : "Event";
            string displayLabel = $"[{category}] {title}";
            float eventNameWidth = Mathf.Max(80f, EventListWidth - 60f);
            var eventContent = new GUIContent(displayLabel, displayLabel);
            if (GUILayout.Toggle(isSelected, eventContent, "Button", GUILayout.Width(eventNameWidth), GUILayout.Height(25)) && !isSelected)
            {
                selectedEventData = data;
                GUI.FocusControl(null);
            }
            if (GUILayout.Button("C", GUILayout.Width(22), GUILayout.Height(25)))
            {
                DuplicateEvent(data);
            }
            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(25)))
            {
                DeleteEvent(data);
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        EndVerticalScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawEventMiddlePanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("이벤트 상세 편집", EditorStyles.boldLabel);

        if (selectedEventData == null)
        {
            EditorGUILayout.HelpBox("좌측 목록에서 편집할 이벤트를 선택하거나 새 이벤트를 생성하세요.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        eventMiddleScroll = EditorGUILayout.BeginScrollView(eventMiddleScroll);

        EditorGUI.BeginChangeCheck();

        // 1. 기본 정보
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("[기본 정보]", EditorStyles.boldLabel);

        string currentAssetPath = AssetDatabase.GetAssetPath(selectedEventData);
        int currentStageCategory = currentAssetPath.Contains("/Events/Stage1/") ? 1 : currentAssetPath.Contains("/Events/Stage2/") ? 2 : currentAssetPath.Contains("/Events/Stage3/") ? 3 : 0;
        int nextStageCategory = EditorGUILayout.Popup("등장 스테이지 (Category)", currentStageCategory, new[] { "Common (모든 스테이지 공용)", "Stage 1 전용", "Stage 2 전용", "Stage 3 전용" });
        if (nextStageCategory != currentStageCategory)
        {
            string targetFolder = nextStageCategory == 1 ? "Assets/Resources/Events/Stage1" : nextStageCategory == 2 ? "Assets/Resources/Events/Stage2" : nextStageCategory == 3 ? "Assets/Resources/Events/Stage3" : "Assets/Resources/Events/Common";
            EnsureAssetFolder(targetFolder);
            string fileName = Path.GetFileName(currentAssetPath);
            string targetPath = $"{targetFolder}/{fileName}";
            if (currentAssetPath != targetPath)
            {
                AssetDatabase.MoveAsset(currentAssetPath, targetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                isDirtyEventCache = true;
            }
        }

        selectedEventData.eventID = EditorGUILayout.TextField("이벤트 고유 ID (eventID)", selectedEventData.eventID);
        selectedEventData.eventTitle = EditorGUILayout.TextField("이벤트 제목 (eventTitle)", selectedEventData.eventTitle);

        EditorGUILayout.LabelField("이벤트 내용 설명문 (eventDescription)");
        selectedEventData.eventDescription = EditorGUILayout.TextArea(selectedEventData.eventDescription, GUILayout.Height(70));

        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("이벤트 이미지 (eventImage)", GUILayout.Width(170));
        selectedEventData.eventImage = (Sprite)EditorGUILayout.ObjectField(selectedEventData.eventImage, typeof(Sprite), false);
        EditorGUILayout.EndHorizontal();

        // 이미지 미리보기
        if (selectedEventData.eventImage != null && selectedEventData.eventImage.texture != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(175, false);
            Rect previewRect = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(false));
            GUI.DrawTexture(previewRect, selectedEventData.eventImage.texture, ScaleMode.ScaleToFit);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        selectedEventData.imageOffset = EditorGUILayout.Vector2Field("이미지 오프셋 (imageOffset)", selectedEventData.imageOffset);
        selectedEventData.imageScale = EditorGUILayout.FloatField("이미지 스케일 (imageScale)", selectedEventData.imageScale);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);

        // 2. 선택지 목록 (확정 실행 방식)
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("[선택지 목록 (Options)]", EditorStyles.boldLabel);
        if (GUILayout.Button("+ 선택지 추가", GUILayout.Width(110)))
        {
            if (selectedEventData.options == null) selectedEventData.options = new List<EventOption>();
            selectedEventData.options.Add(new EventOption
            {
                optionText = "새 선택지",
                requirementType = EventRequirementType.None,
                outcomes = new List<EventOutcome>
                {
                    new EventOutcome { outcomeText = "결과 텍스트를 입력하세요.", probability = 100, rewards = new List<EventReward>() }
                }
            });
        }
        EditorGUILayout.EndHorizontal();

        if (selectedEventData.options != null)
        {
            for (int i = 0; i < selectedEventData.options.Count; i++)
            {
                EventOption option = selectedEventData.options[i];
                EditorGUILayout.BeginVertical(GUI.skin.box);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"선택지 #{i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("선택지 삭제", GUILayout.Width(90)))
                {
                    selectedEventData.options.RemoveAt(i);
                    i--;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                option.optionText = EditorGUILayout.TextField("선택지 텍스트", option.optionText);
                option.requirementType = (EventRequirementType)EditorGUILayout.EnumPopup("요구 조건 타입", option.requirementType);

                if (option.requirementType != EventRequirementType.None)
                {
                    option.requirementValue = EditorGUILayout.IntField("요구 조건 값", option.requirementValue);
                    if (option.requirementType == EventRequirementType.RequireRelic)
                    {
                        option.requirementDataID = EditorGUILayout.TextField("요구 유물 ID", option.requirementDataID);
                    }
                }

                // Outcomes for this Option (Deterministic)
                if (option.outcomes == null || option.outcomes.Count == 0)
                {
                    option.outcomes = new List<EventOutcome>
                    {
                        new EventOutcome { outcomeText = "결과 텍스트를 입력하세요.", probability = 100, rewards = new List<EventReward>() }
                    };
                }

                for (int j = 0; j < option.outcomes.Count; j++)
                {
                    EventOutcome outcome = option.outcomes[j];
                    outcome.probability = 100; // 확정 발생 100% 고정

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("결과 내용 (Outcome Text)", EditorStyles.miniBoldLabel);
                    outcome.outcomeText = EditorGUILayout.TextArea(outcome.outcomeText, GUILayout.Height(50));

                    // Rewards for this Outcome
                    EditorGUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("└ 보상/페널티 목록", EditorStyles.miniBoldLabel);
                    if (GUILayout.Button("+ 보상 추가", GUILayout.Width(80)))
                    {
                        if (outcome.rewards == null) outcome.rewards = new List<EventReward>();
                        outcome.rewards.Add(new EventReward { rewardType = EventRewardType.GainGold, rewardValue = 50 });
                    }
                    EditorGUILayout.EndHorizontal();

                    if (outcome.rewards != null)
                    {
                        for (int k = 0; k < outcome.rewards.Count; k++)
                        {
                            EventReward reward = outcome.rewards[k];
                            EditorGUILayout.BeginHorizontal();
                            reward.rewardType = (EventRewardType)EditorGUILayout.EnumPopup(reward.rewardType, GUILayout.Width(140));
                            reward.rewardValue = EditorGUILayout.IntField(reward.rewardValue, GUILayout.Width(60));
                            reward.rewardDataID = EditorGUILayout.TextField(reward.rewardDataID);
                            if (GUILayout.Button("X", GUILayout.Width(22)))
                            {
                                outcome.rewards.RemoveAt(k);
                                k--;
                                EditorGUILayout.EndHorizontal();
                                continue;
                            }
                            outcome.rewards[k] = reward;
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    option.outcomes[j] = outcome;
                    EditorGUILayout.EndVertical();
                }

                selectedEventData.options[i] = option;
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
        }

        EditorGUILayout.EndVertical();

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(selectedEventData);
            hasPendingSave = true;
            nextSaveTime = EditorApplication.timeSinceStartup + SAVE_DELAY_SECONDS;
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawEventRightOverviewPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(230), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("이벤트 정보 & 도구", EditorStyles.boldLabel);

        if (selectedEventData == null)
        {
            EditorGUILayout.HelpBox("선택된 이벤트가 없습니다.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        eventRightScroll = EditorGUILayout.BeginScrollView(eventRightScroll);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("에셋명", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField(selectedEventData.name, EditorStyles.wordWrappedLabel);

        EditorGUILayout.LabelField("에셋 경로", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(selectedEventData), EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField($"선택지 수: {selectedEventData.options?.Count ?? 0}개");
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("[선택지 개요]", EditorStyles.boldLabel);

        if (selectedEventData.options != null)
        {
            for (int i = 0; i < selectedEventData.options.Count; i++)
            {
                var opt = selectedEventData.options[i];
                string reqStr = opt.requirementType != EventRequirementType.None ? $" ({opt.requirementType})" : "";
                int rewardCount = 0;
                if (opt.outcomes != null)
                {
                    foreach (var outc in opt.outcomes)
                    {
                        if (outc.rewards != null) rewardCount += outc.rewards.Count;
                    }
                }
                EditorGUILayout.HelpBox($"선택지 #{i + 1}: {opt.optionText}{reqStr}\n보상 항목: {rewardCount}개 (확정 실행)", MessageType.None);
            }
        }

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("[테스트 도구]", EditorStyles.boldLabel);

        if (GUILayout.Button("이벤트 UI 미리보기 (Game 뷰)", GUILayout.Height(35)))
        {
            EventPopupUI.PreviewInEditor(selectedEventData);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
        }

        if (GUILayout.Button("미리보기 닫기 (X)", GUILayout.Height(25)))
        {
            var existing = GameObject.Find("EventPopup_Preview");
            if (existing != null) DestroyImmediate(existing);
        }

        EditorGUILayout.Space(5);
        if (GUILayout.Button("에셋 위치 열기", GUILayout.Height(25)))
        {
            EditorGUIUtility.PingObject(selectedEventData);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }
}
