using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// EventScene을 자동 생성하는 에디터 도구.
/// 메뉴: TheLastArk > Create Event Scene
/// </summary>
public class CreateEventScene : MonoBehaviour
{
    [MenuItem("TheLastArk/Create Event Scene")]
    public static void CreateScene()
    {
        // 1. 새 씬 생성
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. SetupEventScene 오브젝트 추가
        GameObject setupObj = new GameObject("SetupEventScene");
        setupObj.AddComponent<SetupEventScene>();

        // 3. EventManager 오브젝트 추가 (씬에 없을 경우를 대비)
        // 실제로는 DontDestroyOnLoad로 유지되지만, 에디터에서 직접 테스트할 때 필요
        GameObject eventMgrObj = new GameObject("EventManager");
        var eventMgr = eventMgrObj.AddComponent<TheLastArk.Map.Events.EventManager>();
        
        // 4. 씬 저장
        string scenePath = "Assets/Scenes/EventScene.unity";
        
        // Scenes 폴더 확인/생성
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"[CreateEventScene] EventScene이 '{scenePath}'에 생성되었습니다!");

        // 5. Build Settings에 자동 등록
        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        
        bool alreadyAdded = false;
        foreach (var s in buildScenes)
        {
            if (s.path == scenePath)
            {
                alreadyAdded = true;
                break;
            }
        }

        if (!alreadyAdded)
        {
            buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log("[CreateEventScene] Build Settings에 EventScene이 등록되었습니다!");
        }

        EditorUtility.DisplayDialog("완료", 
            "EventScene이 생성되고 Build Settings에 등록되었습니다!\n\n" +
            "이벤트 테스트 방법:\n" +
            "1. EventManager 오브젝트에 이벤트 에셋을 등록하세요\n" +
            "2. MapScene에서 ? 노드를 클릭하면 EventScene으로 전환됩니다",
            "확인");
    }
}
