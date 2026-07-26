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
    private EditMode mode;
    private Object selectedAsset;
    private UnityEditor.Editor selectedEditor;
    private Vector2 listScroll;
    private Vector2 inspectorScroll;

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
        EditorGUILayout.LabelField(mode == EditMode.Encounters ? "Enemy Formations" : "Encounter Pools", EditorStyles.boldLabel);

        if (GUILayout.Button(mode == EditMode.Encounters ? "+ Create Formation" : "+ Create Pool", GUILayout.Height(28f)))
            CreateAssetForCurrentMode();

        listScroll = EditorGUILayout.BeginScrollView(listScroll);
        foreach (Object asset in assets)
        {
            if (asset == null) continue;
            bool isSelected = selectedAsset == asset;
            if (GUILayout.Toggle(isSelected, asset.name, "Button") && !isSelected)
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

    private void DrawSelectedInspector()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (selectedAsset == null)
        {
            EditorGUILayout.HelpBox("Select or create an asset.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.LabelField(selectedAsset.name, EditorStyles.boldLabel);
        DrawValidationMessage();
        inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);
        if (selectedEditor == null || selectedEditor.target != selectedAsset)
        {
            if (selectedEditor != null) DestroyImmediate(selectedEditor);
            selectedEditor = UnityEditor.Editor.CreateEditor(selectedAsset);
        }
        selectedEditor.OnInspectorGUI();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
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
            string path = AssetDatabase.GenerateUniqueAssetPath($"{EncounterFolder}/EnemyEncounter.asset");
            AssetDatabase.CreateAsset(encounter, path);
            SaveAndSelect(encounter);
            return;
        }

        EnsureAssetFolder(PoolFolder);
        EnemyEncounterPool pool = CreateInstance<EnemyEncounterPool>();
        string poolPath = AssetDatabase.GenerateUniqueAssetPath($"{PoolFolder}/EnemyEncounterPool.asset");
        AssetDatabase.CreateAsset(pool, poolPath);
        RegisterPool(GetOrCreateTable(), pool);
        SaveAndSelect(pool);
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
        if (table != null) return table;

        EnsureAssetFolder("Assets/Resources/Battle");
        table = CreateInstance<BattleEncounterTable>();
        AssetDatabase.CreateAsset(table, TablePath);
        return table;
    }

    private static void RegisterPool(BattleEncounterTable table, EnemyEncounterPool pool)
    {
        SerializedObject serializedTable = new SerializedObject(table);
        SerializedProperty pools = serializedTable.FindProperty("pools");
        pools.InsertArrayElementAtIndex(pools.arraySize);
        pools.GetArrayElementAtIndex(pools.arraySize - 1).objectReferenceValue = pool;
        serializedTable.ApplyModifiedProperties();
        EditorUtility.SetDirty(table);
    }

    private static void UnregisterPool(BattleEncounterTable table, EnemyEncounterPool pool)
    {
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
        assets.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
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
}
