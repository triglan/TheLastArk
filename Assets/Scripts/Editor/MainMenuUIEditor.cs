#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TheLastArk.UI;

namespace TheLastArk.Editor
{
    [CustomEditor(typeof(MainMenuUI))]
    public class MainMenuUIEditor : UnityEditor.Editor
    {
        [MenuItem("TheLastArk/현재 씬 UI 완벽 적용 및 저장 (Apply Scene UI)")]
        public static void ApplyCurrentSceneUIScene()
        {
            string scenePath = "Assets/Scenes/MainMenu.unity";
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != scenePath)
            {
                activeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            MainMenuUI menuUI = Object.FindObjectOfType<MainMenuUI>();
            if (menuUI == null)
            {
                Debug.LogError("[MainMenuUIEditor] MainMenuUI not found in scene!");
                return;
            }

            Debug.Log("[MainMenuUIEditor] Applying current Scene UI layout to logic...");
            menuUI.AutoBindSceneReferences();
            menuUI.editorView = MainMenuUI.MainMenuEditorView.CharacterSelect;
            menuUI.ApplyEditorView();

            EditorUtility.SetDirty(menuUI);
            EditorSceneManager.MarkSceneDirty(activeScene);
            bool saved = EditorSceneManager.SaveScene(activeScene);
            Debug.Log($"[MainMenuUIEditor] Current Scene UI applied and saved: {saved}");
            SceneView.lastActiveSceneView?.Repaint();
        }

        [MenuItem("TheLastArk/코드 기본 UI 레이아웃으로 씬 초기화 및 재구축")]
        public static void RebuildMainMenuUIScene()
        {
            string scenePath = "Assets/Scenes/MainMenu.unity";
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != scenePath)
            {
                activeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            MainMenuUI menuUI = Object.FindObjectOfType<MainMenuUI>();
            if (menuUI == null)
            {
                Debug.LogError("[MainMenuUIEditor] MainMenuUI not found in scene!");
                return;
            }

            Debug.Log("[MainMenuUIEditor] Rebuilding MainMenu UI to code default layout...");
            menuUI.BuildUIInEditor();
            menuUI.editorView = MainMenuUI.MainMenuEditorView.CharacterSelect;
            menuUI.ApplyEditorView();

            EditorUtility.SetDirty(menuUI);
            EditorSceneManager.MarkSceneDirty(activeScene);
            bool saved = EditorSceneManager.SaveScene(activeScene);
            Debug.Log($"[MainMenuUIEditor] MainMenu scene rebuild complete. Saved: {saved}");
            SceneView.lastActiveSceneView?.Repaint();
        }

        public override void OnInspectorGUI()
        {
            MainMenuUI menuUI = (MainMenuUI)target;

            // ─── 씬 뷰 빠른 전환 헤더 ───
            EditorGUILayout.Space(8);
            GUIStyle headerBoxStyle = new GUIStyle(EditorStyles.helpBox);
            headerBoxStyle.padding = new RectOffset(12, 12, 10, 10);
            EditorGUILayout.BeginVertical(headerBoxStyle);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 14;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField("🎮 씬(Scene) 뷰 UI 화면 전환 & 편집 도구", titleStyle);
            EditorGUILayout.Space(6);

            // [추천] 현재 씬 UI 적용 버튼 (강조 녹색)
            GUI.backgroundColor = new Color(0.35f, 0.95f, 0.55f);
            if (GUILayout.Button("💾 [현재 씬 화면 UI 완벽 적용 및 저장]\n(씬 뷰에서 배치/수정한 오브젝트를 그대로 게임 로직에 연결)", GUILayout.Height(46)))
            {
                ApplyCurrentSceneUIScene();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(6);

            // 코드 기본 레이아웃 재구축 버튼 (경고창 포함)
            GUI.backgroundColor = new Color(1f, 0.85f, 0.35f);
            if (GUILayout.Button("⚠️ [코드 기본 레이아웃으로 씬 초기화 및 재구축]\n(주의: 씬 뷰에서 직접 수정한 색상/텍스처/배치가 초기화됩니다)", GUILayout.Height(36)))
            {
                if (EditorUtility.DisplayDialog(
                    "UI 레이아웃 초기화 확인",
                    "코드 기본값으로 UI를 완전히 새로 구축하시겠습니까?\n\n※ 주의: 씬(Scene) 뷰에서 직접 조절하신 색상, 텍스처, 크기 등 커스텀 내용이 초기화됩니다.",
                    "초기화 진행", "취소"))
                {
                    RebuildMainMenuUIScene();
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(8);

            // 현재 활성 뷰 상태 표시
            bool isCharSelect = menuUI.editorView == MainMenuUI.MainMenuEditorView.CharacterSelect;
            GUIStyle statusStyle = new GUIStyle(EditorStyles.miniBoldLabel);
            statusStyle.alignment = TextAnchor.MiddleCenter;
            if (isCharSelect)
            {
                GUI.color = new Color(0.4f, 1f, 0.5f, 1f);
                EditorGUILayout.LabelField("● 현재 상태: [캐릭터 선택 창] 씬 뷰에서 즉시 편집 가능", statusStyle);
            }
            else
            {
                GUI.color = new Color(0.5f, 0.8f, 1f, 1f);
                EditorGUILayout.LabelField("● 현재 상태: [타이틀 메뉴] 씬 뷰 표시 중", statusStyle);
            }
            GUI.color = Color.white;
            EditorGUILayout.Space(8);

            // 화면 전환 버튼들
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = isCharSelect ? new Color(0.35f, 0.85f, 0.45f) : new Color(0.85f, 0.85f, 0.85f);
            if (GUILayout.Button("👁️ 캐릭터 선택 창 보기\n(씬 편집 모드)", GUILayout.Height(42)))
            {
                Undo.RecordObject(menuUI, "Switch to Character Select View");
                menuUI.SwitchToCharacterSelectView();
                EditorUtility.SetDirty(menuUI);
                if (!Application.isPlaying)
                {
                    EditorSceneManager.MarkSceneDirty(menuUI.gameObject.scene);
                }
                SceneView.lastActiveSceneView?.Repaint();
            }

            GUI.backgroundColor = !isCharSelect ? new Color(0.35f, 0.75f, 1f) : new Color(0.85f, 0.85f, 0.85f);
            if (GUILayout.Button("🏠 타이틀 화면 보기\n(메인 메뉴)", GUILayout.Height(42)))
            {
                Undo.RecordObject(menuUI, "Switch to Title Menu View");
                menuUI.SwitchToTitleMenuView();
                EditorUtility.SetDirty(menuUI);
                if (!Application.isPlaying)
                {
                    EditorSceneManager.MarkSceneDirty(menuUI.gameObject.scene);
                }
                SceneView.lastActiveSceneView?.Repaint();
            }

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(6);

            // 보조 액션 버튼들
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 프리뷰 데이터 새로고침", GUILayout.Height(28)))
            {
                Undo.RecordObject(menuUI, "Refresh Character Preview");
                menuUI.PopulatePreviewInEditor();
                EditorUtility.SetDirty(menuUI);
                if (!Application.isPlaying)
                {
                    EditorSceneManager.MarkSceneDirty(menuUI.gameObject.scene);
                }
                SceneView.lastActiveSceneView?.Repaint();
            }

            if (GUILayout.Button("🧹 프리뷰 카드 비우기", GUILayout.Height(28)))
            {
                Undo.RecordObject(menuUI, "Clear Character Preview Cards");
                menuUI.ClearPreviewInEditor();
                EditorUtility.SetDirty(menuUI);
                if (!Application.isPlaying)
                {
                    EditorSceneManager.MarkSceneDirty(menuUI.gameObject.scene);
                }
                SceneView.lastActiveSceneView?.Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "💡 캐릭터 선택 창이 활성화된 상태에서 씬(Scene) 뷰의 각 요소(기차 헤더, 승무원 명단, 우측 상세 스탯/시너지/스킬, 하단 버튼 등)를 마우스로 직접 선택하여 위치, 크기, 앵커, 폰트 등을 자유롭게 수정할 수 있습니다.\n" +
                "수정한 레이아웃은 Ctrl+S로 씬을 저장하면 유지되며, 게임 실행(▶) 시에는 자동으로 타이틀 화면부터 정상 시작됩니다.",
                MessageType.Info
            );

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(12);

            // 기본 인스펙터 속성 그리기
            DrawDefaultInspector();
        }
    }
}
#endif
