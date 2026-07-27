using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EnemyEncounterEditorWindow : EditorWindow
{
    private const string EncounterFolder = "Assets/Resources/Battle/Encounters";
    private const string PoolFolder = "Assets/Resources/Battle/EncounterPools";
    private const string TablePath = "Assets/Resources/Battle/BattleEncounterTable.asset";

    private enum EditMode { Encounters, Pools }

    private readonly List<Object> assets = new List<Object>();
    private readonly Dictionary<string, bool> regionFoldouts = new Dictionary<string, bool>();
    private EditMode mode;
    private Object selectedAsset;
    private UnityEditor.Editor selectedEditor;
    private Vector2 listScroll;
    private Vector2 inspectorScroll;
    private string newRegionId = EnemyEncounterPool.DefaultRegionId;

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
        EditorGUILayout.BeginHorizontal();
        DrawAssetList();
        DrawSelectedInspector();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawAssetList()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(260f), GUILayout.ExpandHeight(true));
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
        label += $" / {pool.NodeType}";
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
        EditorGUI.BeginChangeCheck();
        selectedEditor.OnInspectorGUI();
        if (EditorGUI.EndChangeCheck() && selectedAsset is EnemyEncounterPool)
            SyncTableRegions();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
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
        else if (selectedAsset is EnemyEncounterPool pool && pool.Encounters.Count == 0)
        {
            EditorGUILayout.HelpBox("Add at least one formation to this pool.", MessageType.Error);
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
        string filter = mode == EditMode.Encounters ? "t:EnemyEncounterData" : "t:EnemyEncounterPool";
        foreach (string guid in AssetDatabase.FindAssets(filter))
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) assets.Add(asset);
        }
        assets.Sort((a, b) => string.CompareOrdinal(GetDisplayLabel(a), GetDisplayLabel(b)));
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
