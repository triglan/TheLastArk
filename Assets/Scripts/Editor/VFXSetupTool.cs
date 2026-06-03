using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class VFXSetupTool
{
    [MenuItem("Tools/Setup VFX Example")]
    public static void Setup()
    {
        // 1. Locate the Prefab
        string prefabPath = "Assets/GabrielAguiarProductions/FreeQuickEffectsVol1/Prefabs/vfx_Explosion_01.prefab";
        GameObject explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (explosionPrefab == null)
        {
            Debug.LogError($"[VFXSetup] Could not find prefab at {prefabPath}");
            return;
        }

        // 2. Add AutoDestroyVFX to the Prefab if missing
        // Note: In Editor, for prefabs, we should load contents if we want to modify persistent serialization,
        // but adding a component to the asset directly works if we save it.
        // However, safest way in automation is to instantiate, modify, apply, or use serializedObject.
        // For simplicity here, let's assume the user can add AutoDestroy manually or we do it on a scene instance.
        // Actually, let's just create the Manager and Tester, and warn about AutoDestroy.
        
        // 3. Create VFXManager
        GameObject managerGO = GameObject.Find("VFXManager");
        if (managerGO == null)
        {
            managerGO = new GameObject("VFXManager");
            Undo.RegisterCreatedObjectUndo(managerGO, "Create VFXManager");
        }
        
        VFXManager manager = managerGO.GetComponent<VFXManager>();
        if (manager == null) manager = managerGO.AddComponent<VFXManager>();

        // Setup Data
        // Reflection or SerializedObject is needed to modify the list if it's protected, but it's public.
        // modifying prefabs in play mode logic style:
        
        // Clear and Add
        manager.vfxList = new List<VFXManager.VFXData>();
        manager.vfxList.Add(new VFXManager.VFXData { name = "Explosion", prefab = explosionPrefab });
        
        Debug.Log("[VFXSetup] Configured VFXManager with 'Explosion'.");

        // 4. Create VFXTester
        GameObject testerGO = GameObject.Find("VFXTester");
        if (testerGO == null)
        {
            testerGO = new GameObject("VFXTester");
            Undo.RegisterCreatedObjectUndo(testerGO, "Create VFXTester");
        }

        VFXTester tester = testerGO.GetComponent<VFXTester>();
        if (tester == null) tester = testerGO.AddComponent<VFXTester>();

        tester.vfxName = "Explosion";
        tester.triggerKey = KeyCode.Space;
        
        Debug.Log("[VFXSetup] Configured VFXTester.");

        // 5. Select the Tester
        Selection.activeGameObject = testerGO;
        
        Debug.Log("<color=green>VFX Setup Complete!</color> Please ensure 'vfx_Explosion_01' has 'AutoDestroyVFX' attached if you want it to disappear automatically.");
    }
}
