#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateVillageScene
{
    [MenuItem("Tools/Create Village Scene")]
    public static void CreateScene()
    {
        string scenePath = "Assets/Scenes/VillageScene.unity";
        
        // Create new empty scene
        UnityEngine.SceneManagement.Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // Add VillageManager
        GameObject managerObj = new GameObject("VillageManager");
        managerObj.AddComponent<TheLastArk.Village.VillageManager>();
        
        // Save scene
        bool success = EditorSceneManager.SaveScene(newScene, scenePath);
        
        if (success)
        {
            // Add to Build Settings
            var original = EditorBuildSettings.scenes;
            var newSettings = new EditorBuildSettingsScene[original.Length + 1];
            System.Array.Copy(original, newSettings, original.Length);
            var sceneToAdd = new EditorBuildSettingsScene(scenePath, true);
            newSettings[newSettings.Length - 1] = sceneToAdd;
            EditorBuildSettings.scenes = newSettings;
            
            Debug.Log($"Successfully created and added {scenePath} to Build Settings.");
        }
        else
        {
            Debug.LogError($"Failed to save scene at {scenePath}");
        }
    }
}
#endif
