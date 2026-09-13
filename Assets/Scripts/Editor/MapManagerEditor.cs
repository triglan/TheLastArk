#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TheLastArk.Editor
{
    [CustomEditor(typeof(MapManager))]
    public class MapManagerEditor : UnityEditor.Editor
    {
        [MenuItem("TheLastArk/맵 씬(Map Scene) UI 전체 구축 및 씬 화면 편집 활성화")]
        public static void BuildMapSceneUI()
        {
            string scenePath = "Assets/Scenes/MapScene.unity";
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != scenePath)
            {
                activeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            MapManager manager = Object.FindObjectOfType<MapManager>();
            if (manager == null)
            {
                Debug.LogError("[MapManagerEditor] 씬에서 MapManager를 찾을 수 없습니다!");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Build Map Scene UI");
            manager.BuildUIInEditor(true);

            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(activeScene);
            bool saved = EditorSceneManager.SaveScene(activeScene);
            Debug.Log($"[MapManagerEditor] 맵 씬 UI 전체 생성 및 저장 완료: {saved}");
            SceneView.lastActiveSceneView?.Repaint();
        }

        [MenuItem("TheLastArk/현재 맵 씬 UI 완벽 적용 및 저장 (Apply Scene UI)")]
        public static void ApplyCurrentSceneUIScene()
        {
            string scenePath = "Assets/Scenes/MapScene.unity";
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != scenePath)
            {
                activeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            MapManager manager = Object.FindObjectOfType<MapManager>();
            if (manager == null)
            {
                Debug.LogError("[MapManagerEditor] 씬에서 MapManager를 찾을 수 없습니다!");
                return;
            }

            manager.AutoBindSceneReferences();
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(activeScene);
            bool saved = EditorSceneManager.SaveScene(activeScene);
            Debug.Log($"[MapManagerEditor] 현재 맵 씬 UI 적용 및 저장 완료: {saved}");
            SceneView.lastActiveSceneView?.Repaint();
        }

        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            EditorApplication.delayCall += () =>
            {
                // 플레이 모드 중이거나 플레이 모드로 진입 중일 때는 에디터 씬 저장 로직 실행 금지
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;

                var activeScene = EditorSceneManager.GetActiveScene();
                if (activeScene.path == "Assets/Scenes/MapScene.unity")
                {
                    MapManager manager = Object.FindObjectOfType<MapManager>();
                    if (manager != null)
                    {
                        manager.AutoBindSceneReferences();
                        if (manager.GetMapContainer() == null)
                        {
                            Debug.Log("[MapManagerEditor] MapScene이 열려 있어 자동으로 UI를 생성하고 씬 뷰에 표시합니다.");
                            manager.BuildUIInEditor(true);
                            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                            {
                                EditorUtility.SetDirty(manager);
                                EditorSceneManager.MarkSceneDirty(activeScene);
                                EditorSceneManager.SaveScene(activeScene);
                            }
                            SceneView.lastActiveSceneView?.Repaint();
                        }
                        else
                        {
                            // 새 아이콘 및 변경된 노드 크기(120)가 즉시 반영되도록 미리보기 맵 갱신
                            manager.GeneratePreviewMap(manager.previewSeed);
                            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                            {
                                EditorUtility.SetDirty(manager);
                                EditorSceneManager.MarkSceneDirty(activeScene);
                                EditorSceneManager.SaveScene(activeScene);
                            }
                            SceneView.lastActiveSceneView?.Repaint();
                        }
                    }
                }
            };
        }

        public override void OnInspectorGUI()
        {
            MapManager manager = (MapManager)target;

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("▶️ 현재 플레이 모드(Play Mode) 실행 중입니다. 인게임에서 실시간으로 노드를 클릭하고 이동할 수 있습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8);
            GUIStyle headerBoxStyle = new GUIStyle(EditorStyles.helpBox);
            headerBoxStyle.padding = new RectOffset(12, 12, 10, 10);
            EditorGUILayout.BeginVertical(headerBoxStyle);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 14;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField("🗺️ 맵 씬(Scene) 뷰 화면 편집 & 미리보기 도구", titleStyle);
            EditorGUILayout.Space(6);

            // 상태 표시
            bool hasContainer = manager.GetMapContainer() != null;
            bool hasCanvas = Object.FindObjectOfType<Canvas>() != null;

            if (hasContainer && hasCanvas)
            {
                EditorGUILayout.HelpBox("✅ 씬에 맵 UI와 노드가 활성화되어 있습니다.\n씬(Scene) 뷰에서 바로 UI 배치, 배경, 버튼, 노드 크기 등을 확인하고 마우스로 직접 수정할 수 있습니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("⚠️ 씬에 맵 UI가 없습니다 (빈 씬 상태).\n아래 [맵 씬 UI 전체 생성 및 씬에 배치] 버튼을 누르면 인게임 화면 그대로 씬 뷰에 즉시 나타납니다.", MessageType.Warning);
            }
            EditorGUILayout.Space(6);

            // 1. UI 전체 생성 버튼 (하늘색)
            GUI.backgroundColor = new Color(0.35f, 0.85f, 1.0f);
            if (GUILayout.Button("🚀 [맵 씬 UI 전체 생성 및 씬 화면에 즉시 배치]\n(카메라, 캔버스, 배경, 상하단 UI, 노드/연결선 15층 전체 생성)", GUILayout.Height(46)))
            {
                Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Build Map Scene UI");
                manager.BuildUIInEditor(true);
                EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
                SceneView.lastActiveSceneView?.Repaint();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(4);

            // 2. 현재 씬 UI 적용 및 저장 (초록색)
            GUI.backgroundColor = new Color(0.35f, 0.95f, 0.55f);
            if (GUILayout.Button("💾 [현재 씬 화면 UI 완벽 적용 및 저장]\n(씬 뷰에서 배치/수정한 오브젝트를 그대로 저장)", GUILayout.Height(40)))
            {
                manager.AutoBindSceneReferences();
                EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
                EditorSceneManager.SaveScene(manager.gameObject.scene);
                SceneView.lastActiveSceneView?.Repaint();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(4);

            // 3. 미리보기 맵 새로고침 (랜덤 시드) & 노드 정리
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.9f, 0.4f);
            if (GUILayout.Button("🎲 [미리보기 맵 새로고침\n(랜덤 시드)]", GUILayout.Height(38)))
            {
                manager.previewSeed = UnityEngine.Random.Range(1000, 99999);
                manager.GeneratePreviewMap(manager.previewSeed);
                EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
                SceneView.lastActiveSceneView?.Repaint();
            }

            GUI.backgroundColor = new Color(0.95f, 0.5f, 0.5f);
            if (GUILayout.Button("🧹 [미리보기 노드 정리\n(UI 틀만 유지)]", GUILayout.Height(38)))
            {
                manager.ClearPreviewNodes();
                EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
                SceneView.lastActiveSceneView?.Repaint();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);

            // 기본 인스펙터 그리기
            DrawDefaultInspector();
        }
    }
}
#endif
