using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TheLastArk.Map.Events;

public class EnemyEncounterEditorWindow : EditorWindow
{
    private const string EncounterFolder = "Assets/Resources/Battle/Encounters";
    private const string PoolFolder = "Assets/Resources/Battle/EncounterPools";
    private const string TablePath = "Assets/Resources/Battle/BattleEncounterTable.asset";
    private const string RewardTablePath = "Assets/Resources/Battle/BattleRewardTable.asset";

    public enum EditMode { Encounters, Pools }

    private readonly List<Object> assets = new List<Object>();
    private readonly Dictionary<string, bool> regionFoldouts = new Dictionary<string, bool>();
    private readonly List<GameEventData> eventAssets = new List<GameEventData>();
    private EditMode mode;
    private Object selectedAsset;
    private UnityEditor.Editor selectedEditor;
    private Vector2 listScroll;
    private Vector2 inspectorScroll;
    private Vector2 rewardScroll;
    private int selectedRewardStage;
    private string newRegionId = EnemyEncounterPool.DefaultRegionId;
    private string eventSearchText;

    [MenuItem("Window/Battle/Enemy Encounter Editor")]
    public static void ShowWindow()
    {
        EnemyEncounterEditorWindow window = GetWindow<EnemyEncounterEditorWindow>("Enemy Encounters");
        window.minSize = new Vector2(800f, 520f);
        window.RefreshAssets();
    }

    private void OnEnable() => RefreshAssets();

    private void OnDisable()
    {
        if (selectedEditor != null) DestroyImmediate(selectedEditor);
    }

    private void OnGUI()
    {
        EditMode nextMode = (EditMode)GUILayout.Toolbar((int)mode, new[] { "Formations", "Pools" });
        if (nextMode != mode)
        {
            mode = nextMode;
            SelectAsset(null);
            RefreshAssets();
        }

        EditorGUILayout.Space(4f);
        DrawEditorBody(null);
    }

    public void DrawEmbedded(EditMode embeddedMode, System.Action drawLeftHeader)
    {
        if (mode != embeddedMode)
        {
            mode = embeddedMode;
            SelectAsset(null);
            RefreshAssets();
        }

        DrawEditorBody(drawLeftHeader);
    }

    public void RefreshEmbedded()
    {
        RefreshAssets();
    }

    private void DrawEditorBody(System.Action drawLeftHeader)
    {
        EditorGUILayout.BeginHorizontal();
        DrawAssetList(drawLeftHeader);
        DrawSelectedInspector();
        if (mode == EditMode.Pools && (!(selectedAsset is EnemyEncounterPool pool) || pool.NodeType != NodeType.Event))
            DrawRewardSettingsPanel();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawAssetList(System.Action drawLeftHeader)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(260f), GUILayout.ExpandHeight(true));
        drawLeftHeader?.Invoke();
        if (drawLeftHeader != null) EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField(mode == EditMode.Encounters ? "Enemy Formations" : "Maps / Encounter Pools", EditorStyles.boldLabel);

        if (mode == EditMode.Pools)
        {
            DrawPoolTreeList();
            EditorGUILayout.EndVertical();
            return;
        }

        if (GUILayout.Button(mode == EditMode.Encounters ? "+ Create Formation" : "+ Create Pool", GUILayout.Height(28f)))
            CreateAssetForCurrentMode();

        listScroll = EditorGUILayout.BeginScrollView(listScroll);
        foreach (Object asset in assets)
        {
            if (asset == null) continue;
            bool isSelected = selectedAsset == asset;
            if (GUILayout.Toggle(isSelected, GetDisplayLabel(asset), "Button") && !isSelected)
                SelectAsset(asset);
        }
        EditorGUILayout.EndScrollView();

        EditorGUI.BeginDisabledGroup(selectedAsset == null);
        GUI.color = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("Delete Selected")) DeleteSelectedAsset();
        GUI.color = Color.white;
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndVertical();
    }

    private void DrawPoolTreeList()
    {
        BattleEncounterTable table = GetOrCreateTable();
        table.SyncRegionsFromPools();

        EditorGUILayout.BeginHorizontal();
        newRegionId = EditorGUILayout.TextField(newRegionId);
        if (GUILayout.Button("+ Map", GUILayout.Width(58f)))
            CreateRegion(table, newRegionId);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4f);
        listScroll = EditorGUILayout.BeginScrollView(listScroll);
        foreach (EnemyEncounterRegionPools region in table.Regions)
        {
            if (region == null) continue;

            string regionId = region.RegionId;
            if (!regionFoldouts.ContainsKey(regionId))
                regionFoldouts[regionId] = true;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            regionFoldouts[regionId] = EditorGUILayout.Foldout(regionFoldouts[regionId], regionId, true, EditorStyles.boldLabel);
            if (GUILayout.Button("+ Pool", GUILayout.Width(62f)))
                CreatePoolForRegion(regionId);
            EditorGUILayout.EndHorizontal();

            if (regionFoldouts[regionId])
            {
                foreach (EnemyEncounterPool pool in region.Pools)
                {
                    if (pool == null) continue;
                    DrawPoolRow(pool);
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();

        EditorGUI.BeginDisabledGroup(selectedAsset == null);
        GUI.color = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("Delete Selected")) DeleteSelectedAsset();
        GUI.color = Color.white;
        EditorGUI.EndDisabledGroup();
    }

    private void DrawPoolRow(EnemyEncounterPool pool)
    {
        bool isSelected = selectedAsset == pool;
        string label = $"{pool.DisplayName} - {pool.MinFloor}~{pool.MaxFloor}F";
        label += pool.NodeType == NodeType.Event
            ? $" / Event / {pool.Events.Count} Events"
            : $" / {pool.NodeType}";
        if (GUILayout.Toggle(isSelected, label, "Button") && !isSelected)
            SelectAsset(pool);
    }

    private void DrawSelectedInspector()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (selectedAsset == null)
        {
            EditorGUILayout.HelpBox("Select or create an asset.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.LabelField(GetDisplayLabel(selectedAsset), EditorStyles.boldLabel);
        DrawNameEditor();
        DrawValidationMessage();
        inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);
        if (selectedEditor == null || selectedEditor.target != selectedAsset)
        {
            if (selectedEditor != null) DestroyImmediate(selectedEditor);
            selectedEditor = UnityEditor.Editor.CreateEditor(selectedAsset);
        }
        SerializedObject serializedInspector = selectedEditor.serializedObject;
        serializedInspector.Update();
        EditorGUI.BeginChangeCheck();
        if (selectedAsset is EnemyEncounterPool pool)
        {
            if (pool.NodeType == NodeType.Event) DrawPoolEventEditor(serializedInspector);
            else DrawPoolRewardStageSelector(serializedInspector);
            EditorGUILayout.Space(8f);
            DrawVisiblePropertiesExcluding(serializedInspector, "m_Script", "displayName", "rewardStageId", "legacyRewardStage",
                "events", pool.NodeType == NodeType.Event ? "encounters" : "");
        }
        else
        {
            DrawVisiblePropertiesExcluding(serializedInspector, "m_Script", "displayName");
        }
        bool inspectorChanged = EditorGUI.EndChangeCheck();
        serializedInspector.ApplyModifiedProperties();
        if (inspectorChanged && selectedAsset is EnemyEncounterPool)
            SyncTableRegions();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawPoolRewardStageSelector(SerializedObject serializedPool)
    {
        BattleRewardTable table = GetOrCreateRewardTable();
        SerializedProperty stageId = serializedPool.FindProperty("rewardStageId");
        string currentId = selectedAsset is EnemyEncounterPool pool ? pool.RewardStageId : stageId.stringValue;

        EditorGUILayout.LabelField("보상 단계", EditorStyles.boldLabel);
        const int stagesPerRow = 10;
        for (int rowStart = 0; rowStart < table.Stages.Count; rowStart += stagesPerRow)
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = rowStart; i < Mathf.Min(rowStart + stagesPerRow, table.Stages.Count); i++)
            {
                BattleRewardStage stage = table.Stages[i];
                if (stage == null) continue;
                bool selected = string.Equals(currentId, stage.Id, System.StringComparison.Ordinal);
                if (GUILayout.Toggle(selected, stage.DisplayName, EditorStyles.radioButton) && !selected)
                {
                    stageId.stringValue = stage.Id;
                    currentId = stage.Id;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawPoolEventEditor(SerializedObject serializedPool)
    {
        SerializedProperty events = serializedPool.FindProperty("events");
        if (events == null) return;

        EditorGUILayout.LabelField($"포함된 이벤트 ({events.arraySize})", EditorStyles.boldLabel);
        for (int i = 0; i < events.arraySize; i++)
        {
            SerializedProperty item = events.GetArrayElementAtIndex(i);
            GameEventData eventData = item.objectReferenceValue as GameEventData;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(eventData != null ? GetEventLabel(eventData) : "(Missing Event)");
            EditorGUI.BeginDisabledGroup(eventData == null);
            if (GUILayout.Button("보기", GUILayout.Width(42f)))
            {
                Selection.activeObject = eventData;
                EditorGUIUtility.PingObject(eventData);
            }
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("−", GUILayout.Width(24f)))
            {
                item.objectReferenceValue = null;
                events.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                return;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("이벤트 빠른 추가", EditorStyles.boldLabel);
        eventSearchText = EditorGUILayout.TextField("검색", eventSearchText);

        List<GameEventData> matches = new List<GameEventData>();
        foreach (GameEventData eventData in eventAssets)
        {
            if (eventData == null || !MatchesEventSearch(eventData, eventSearchText)) continue;
            matches.Add(eventData);

            bool added = ContainsEvent(events, eventData);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(GetEventLabel(eventData));
            EditorGUI.BeginDisabledGroup(added);
            if (GUILayout.Button(added ? "추가됨" : "+", GUILayout.Width(52f))) AddEvent(events, eventData);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUI.BeginDisabledGroup(matches.Count == 0);
        if (GUILayout.Button("검색 결과 모두 추가"))
        {
            foreach (GameEventData eventData in matches)
                if (!ContainsEvent(events, eventData)) AddEvent(events, eventData);
        }
        EditorGUI.EndDisabledGroup();
    }

    private static void AddEvent(SerializedProperty events, GameEventData eventData)
    {
        int index = events.arraySize;
        events.InsertArrayElementAtIndex(index);
        events.GetArrayElementAtIndex(index).objectReferenceValue = eventData;
    }

    private static bool ContainsEvent(SerializedProperty events, GameEventData eventData)
    {
        for (int i = 0; i < events.arraySize; i++)
            if (events.GetArrayElementAtIndex(i).objectReferenceValue == eventData) return true;
        return false;
    }

    private static bool MatchesEventSearch(GameEventData eventData, string search)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        return ContainsIgnoreCase(eventData.eventTitle, search)
            || ContainsIgnoreCase(eventData.eventID, search)
            || ContainsIgnoreCase(eventData.name, search);
    }

    private static bool ContainsIgnoreCase(string text, string search)
    {
        return !string.IsNullOrEmpty(text) && text.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetEventLabel(GameEventData eventData)
    {
        string path = AssetDatabase.GetAssetPath(eventData);
        string category = path.Contains("/Events/Common/") ? "Common"
            : path.Contains("/Events/Stage1/") ? "Stage1"
            : "Event";
        string title = string.IsNullOrWhiteSpace(eventData.eventTitle) ? eventData.name : eventData.eventTitle;
        return $"[{category}] {title}";
    }

    private void DrawRewardSettingsPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(330f), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("단계별 보상 설정", EditorStyles.boldLabel);

        BattleRewardTable table = GetOrCreateRewardTable();
        SerializedObject serializedTable = new SerializedObject(table);
        serializedTable.Update();
        SerializedProperty stages = serializedTable.FindProperty("stages");
        selectedRewardStage = Mathf.Clamp(selectedRewardStage, 0, Mathf.Max(0, stages.arraySize - 1));

        rewardScroll = EditorGUILayout.BeginScrollView(rewardScroll);
        for (int i = 0; i < stages.arraySize; i++)
        {
            SerializedProperty stage = stages.GetArrayElementAtIndex(i);
            string stageName = stage.FindPropertyRelative("displayName").stringValue;
            if (GUILayout.Toggle(selectedRewardStage == i, stageName, "Button")) selectedRewardStage = i;
        }

        if (GUILayout.Button("+ 보상 단계 추가"))
        {
            int index = stages.arraySize;
            stages.InsertArrayElementAtIndex(index);
            SerializedProperty added = stages.GetArrayElementAtIndex(index);
            added.FindPropertyRelative("id").stringValue = System.Guid.NewGuid().ToString("N");
            added.FindPropertyRelative("displayName").stringValue = $"{index + 1}단계";
            selectedRewardStage = index;
        }

        if (stages.arraySize > 0)
        {
            EditorGUILayout.Space(8f);
            SerializedProperty selected = stages.GetArrayElementAtIndex(selectedRewardStage);
            SerializedProperty id = selected.FindPropertyRelative("id");
            EditorGUILayout.PropertyField(selected.FindPropertyRelative("displayName"), new GUIContent("단계 이름"));

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(selectedRewardStage == 0);
            if (GUILayout.Button("↑"))
            {
                stages.MoveArrayElement(selectedRewardStage, selectedRewardStage - 1);
                selectedRewardStage--;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(selectedRewardStage >= stages.arraySize - 1);
            if (GUILayout.Button("↓"))
            {
                stages.MoveArrayElement(selectedRewardStage, selectedRewardStage + 1);
                selectedRewardStage++;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            DrawRewardFields(selected.FindPropertyRelative("reward"));

            EditorGUI.BeginDisabledGroup(stages.arraySize <= 1);
            GUI.color = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("선택 단계 삭제")) TryDeleteRewardStage(stages, selectedRewardStage, id.stringValue);
            GUI.color = Color.white;
            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.EndScrollView();
        if (serializedTable.ApplyModifiedProperties()) EditorUtility.SetDirty(table);
        EditorGUILayout.EndVertical();
    }

    private static void DrawRewardFields(SerializedProperty reward)
    {
        if (reward == null) return;
        EditorGUILayout.Space(6f);
        EditorGUILayout.BeginVertical("box");
        SerializedProperty giveGold = reward.FindPropertyRelative("giveGold");
        EditorGUILayout.PropertyField(giveGold, new GUIContent("골드 지급"));
        if (giveGold.boolValue)
            EditorGUILayout.PropertyField(reward.FindPropertyRelative("goldAmount"), new GUIContent("골드 수량"));

        SerializedProperty giveCard = reward.FindPropertyRelative("giveCharacterCard");
        EditorGUILayout.PropertyField(giveCard, new GUIContent("캐릭터 카드 지급"));
        if (giveCard.boolValue)
        {
            EditorGUILayout.PropertyField(reward.FindPropertyRelative("cardAmount"), new GUIContent("선택 카드 지급 수량"));
            EditorGUILayout.LabelField("카드 후보 규칙", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(reward.FindPropertyRelative("card1Rule"), new GUIContent("후보 1"));
            EditorGUILayout.PropertyField(reward.FindPropertyRelative("card2Rule"), new GUIContent("후보 2"));
            EditorGUILayout.PropertyField(reward.FindPropertyRelative("card3Rule"), new GUIContent("후보 3"));
        }
        EditorGUILayout.EndVertical();
    }

    private static void TryDeleteRewardStage(SerializedProperty stages, int index, string stageId)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyEncounterPool"))
        {
            EnemyEncounterPool pool = AssetDatabase.LoadAssetAtPath<EnemyEncounterPool>(AssetDatabase.GUIDToAssetPath(guid));
            if (pool == null || pool.RewardStageId != stageId) continue;
            EditorUtility.DisplayDialog("보상 단계 삭제 불가", $"'{pool.DisplayName}' Pool이 이 단계를 사용 중입니다.", "확인");
            return;
        }

        if (EditorUtility.DisplayDialog("보상 단계 삭제", "선택한 보상 단계를 삭제할까요?", "삭제", "취소"))
            stages.DeleteArrayElementAtIndex(index);
    }

    private static void DrawVisiblePropertiesExcluding(SerializedObject serializedObject, params string[] excludedPropertyPaths)
    {
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (System.Array.IndexOf(excludedPropertyPaths, property.propertyPath) >= 0) continue;

            EditorGUILayout.PropertyField(property, true);
        }
    }

    private void DrawNameEditor()
    {
        if (!(selectedAsset is EnemyEncounterData) && !(selectedAsset is EnemyEncounterPool)) return;

        SerializedObject serializedAsset = new SerializedObject(selectedAsset);
        SerializedProperty displayName = serializedAsset.FindProperty("displayName");
        if (displayName == null) return;

        EditorGUI.BeginChangeCheck();
        string label = selectedAsset is EnemyEncounterData ? "Formation Name" : "Pool Name";
        string nextName = EditorGUILayout.DelayedTextField(label, displayName.stringValue);
        if (!EditorGUI.EndChangeCheck()) return;

        nextName = string.IsNullOrWhiteSpace(nextName) ? selectedAsset.name : nextName.Trim();
        displayName.stringValue = nextName;
        serializedAsset.ApplyModifiedProperties();

        RenameAsset(selectedAsset, nextName, selectedAsset is EnemyEncounterData ? "Formation" : "Pool");
        EditorUtility.SetDirty(selectedAsset);
        AssetDatabase.SaveAssets();
        RefreshAssets();
        SelectAsset(selectedAsset);
        Selection.activeObject = selectedAsset;
    }

    private void DrawValidationMessage()
    {
        if (selectedAsset is EnemyEncounterData encounter)
        {
            if (!encounter.HasAnyEnemy)
                EditorGUILayout.HelpBox("At least one enemy slot is required.", MessageType.Error);
            else
                EditorGUILayout.HelpBox("Slots are ordered from left to right. Empty slots stay disabled.", MessageType.Info);
        }
        else if (selectedAsset is EnemyEncounterPool pool)
        {
            if (pool.NodeType == NodeType.Event && pool.Events.Count == 0)
                EditorGUILayout.HelpBox("이 Event Pool에 이벤트를 하나 이상 추가하세요.", MessageType.Error);
            else if (pool.NodeType != NodeType.Event && pool.Encounters.Count == 0)
                EditorGUILayout.HelpBox("Add at least one formation to this pool.", MessageType.Error);

            if (pool.NodeType != NodeType.Event)
            {
                BattleRewardSettings reward = pool.ActiveReward;
                if (reward != null && reward.giveGold && reward.goldAmount == 0)
                    EditorGUILayout.HelpBox("이 Pool의 적용 보상 단계 골드가 0입니다.", MessageType.Warning);
                if (reward != null && reward.giveCharacterCard && reward.cardAmount < 1)
                    EditorGUILayout.HelpBox("캐릭터 카드 지급 수량은 1 이상이어야 합니다.", MessageType.Error);
            }
        }
    }

    private void CreateAssetForCurrentMode()
    {
        if (mode == EditMode.Encounters)
        {
            EnsureAssetFolder(EncounterFolder);
            EnemyEncounterData encounter = CreateInstance<EnemyEncounterData>();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{EncounterFolder}/NewFormation_Formation.asset");
            AssetDatabase.CreateAsset(encounter, path);
            SaveAndSelect(encounter);
            return;
        }

        EnsureAssetFolder(PoolFolder);
        EnemyEncounterPool pool = CreateInstance<EnemyEncounterPool>();
        string poolPath = AssetDatabase.GenerateUniqueAssetPath($"{PoolFolder}/NewPool_Pool.asset");
        AssetDatabase.CreateAsset(pool, poolPath);
        RegisterPool(GetOrCreateTable(), pool);
        SaveAndSelect(pool);
    }

    private void CreatePoolForRegion(string regionId)
    {
        EnsureAssetFolder(PoolFolder);
        EnemyEncounterPool pool = CreateInstance<EnemyEncounterPool>();
        SetSerializedString(pool, "regionId", regionId);
        string poolPath = AssetDatabase.GenerateUniqueAssetPath($"{PoolFolder}/{SanitizeAssetName(regionId)}_NewPool_Pool.asset");
        AssetDatabase.CreateAsset(pool, poolPath);
        RegisterPool(GetOrCreateTable(), pool);
        SaveAndSelect(pool);
    }

    private void CreateRegion(BattleEncounterTable table, string regionId)
    {
        if (table == null) return;

        string normalized = string.IsNullOrWhiteSpace(regionId) ? EnemyEncounterPool.DefaultRegionId : regionId.Trim();
        table.EnsureRegion(normalized);
        regionFoldouts[normalized] = true;
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        RefreshAssets();
    }

    private void DeleteSelectedAsset()
    {
        if (!EditorUtility.DisplayDialog("Delete Asset", $"Delete {selectedAsset.name}?", "Delete", "Cancel")) return;
        if (selectedAsset is EnemyEncounterPool pool) UnregisterPool(GetOrCreateTable(), pool);

        string path = AssetDatabase.GetAssetPath(selectedAsset);
        SelectAsset(null);
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.SaveAssets();
        RefreshAssets();
    }

    private BattleEncounterTable GetOrCreateTable()
    {
        BattleEncounterTable table = AssetDatabase.LoadAssetAtPath<BattleEncounterTable>(TablePath);
        if (table != null)
        {
            table.SyncRegionsFromPools();
            EditorUtility.SetDirty(table);
            return table;
        }

        EnsureAssetFolder("Assets/Resources/Battle");
        table = CreateInstance<BattleEncounterTable>();
        AssetDatabase.CreateAsset(table, TablePath);
        return table;
    }

    private BattleRewardTable GetOrCreateRewardTable()
    {
        BattleRewardTable table = AssetDatabase.LoadAssetAtPath<BattleRewardTable>(RewardTablePath);
        if (table != null)
        {
            table.EnsureDefaults();
            return table;
        }

        EnsureAssetFolder("Assets/Resources/Battle");
        table = CreateInstance<BattleRewardTable>();
        table.EnsureDefaults();
        AssetDatabase.CreateAsset(table, RewardTablePath);
        AssetDatabase.SaveAssets();
        return table;
    }

    private static void RegisterPool(BattleEncounterTable table, EnemyEncounterPool pool)
    {
        table.RegisterPool(pool);
        AddLegacyPoolReference(table, pool);
        EditorUtility.SetDirty(table);
    }

    private static void UnregisterPool(BattleEncounterTable table, EnemyEncounterPool pool)
    {
        table.UnregisterPool(pool);
        SerializedObject serializedTable = new SerializedObject(table);
        SerializedProperty pools = serializedTable.FindProperty("pools");
        for (int i = pools.arraySize - 1; i >= 0; i--)
        {
            if (pools.GetArrayElementAtIndex(i).objectReferenceValue != pool) continue;
            pools.GetArrayElementAtIndex(i).objectReferenceValue = null;
            pools.DeleteArrayElementAtIndex(i);
        }
        serializedTable.ApplyModifiedProperties();
        EditorUtility.SetDirty(table);
    }

    private static void AddLegacyPoolReference(BattleEncounterTable table, EnemyEncounterPool pool)
    {
        SerializedObject serializedTable = new SerializedObject(table);
        SerializedProperty pools = serializedTable.FindProperty("pools");
        for (int i = 0; i < pools.arraySize; i++)
        {
            if (pools.GetArrayElementAtIndex(i).objectReferenceValue == pool) return;
        }

        pools.InsertArrayElementAtIndex(pools.arraySize);
        pools.GetArrayElementAtIndex(pools.arraySize - 1).objectReferenceValue = pool;
        serializedTable.ApplyModifiedProperties();
    }

    private void SaveAndSelect(Object asset)
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshAssets();
        SelectAsset(asset);
        Selection.activeObject = asset;
    }

    private void RefreshAssets()
    {
        assets.Clear();
        eventAssets.Clear();
        string filter = mode == EditMode.Encounters ? "t:EnemyEncounterData" : "t:EnemyEncounterPool";
        foreach (string guid in AssetDatabase.FindAssets(filter))
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) assets.Add(asset);
        }
        assets.Sort((a, b) => string.CompareOrdinal(GetDisplayLabel(a), GetDisplayLabel(b)));
        foreach (string guid in AssetDatabase.FindAssets("t:GameEventData"))
        {
            GameEventData eventData = AssetDatabase.LoadAssetAtPath<GameEventData>(AssetDatabase.GUIDToAssetPath(guid));
            if (eventData != null) eventAssets.Add(eventData);
        }
        eventAssets.Sort((a, b) => string.Compare(GetEventLabel(a), GetEventLabel(b), System.StringComparison.OrdinalIgnoreCase));
        Repaint();
    }

    private void SelectAsset(Object asset)
    {
        selectedAsset = asset;
        inspectorScroll = Vector2.zero;
        if (selectedEditor != null) DestroyImmediate(selectedEditor);
        selectedEditor = null;
    }

    private static void EnsureAssetFolder(string folderPath)
    {
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

    private void SyncTableRegions()
    {
        BattleEncounterTable table = GetOrCreateTable();
        table.SyncRegionsFromPools();
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        RefreshAssets();
    }

    private static string GetDisplayLabel(Object asset)
    {
        if (asset is EnemyEncounterData encounter) return encounter.DisplayName;
        if (asset is EnemyEncounterPool pool) return pool.DisplayName;
        return asset != null ? asset.name : "(None)";
    }

    private static void RenameAsset(Object asset, string displayName, string suffix)
    {
        if (asset == null) return;

        string path = AssetDatabase.GetAssetPath(asset);
        if (string.IsNullOrEmpty(path)) return;

        string assetName = $"{SanitizeAssetName(displayName)}_{suffix}";
        if (asset.name == assetName) return;

        string error = AssetDatabase.RenameAsset(path, assetName);
        if (!string.IsNullOrEmpty(error))
            Debug.LogWarning($"[EnemyEncounterEditor] Rename failed: {error}");
    }

    private static void SetSerializedString(Object asset, string propertyName, string value)
    {
        SerializedObject serializedAsset = new SerializedObject(asset);
        SerializedProperty property = serializedAsset.FindProperty(propertyName);
        if (property == null) return;

        property.stringValue = value;
        serializedAsset.ApplyModifiedProperties();
        EditorUtility.SetDirty(asset);
    }

    private static string SanitizeAssetName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return "Unnamed";

        char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
        string result = rawName.Trim();
        foreach (char invalidChar in invalidChars)
        {
            result = result.Replace(invalidChar.ToString(), "");
        }

        result = result.Replace(" ", "");
        return string.IsNullOrWhiteSpace(result) ? "Unnamed" : result;
    }
}
