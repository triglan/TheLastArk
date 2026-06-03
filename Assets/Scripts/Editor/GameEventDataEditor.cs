using UnityEngine;
using UnityEditor;
using TheLastArk.Map.Events;

namespace TheLastArk.Editor
{
    [CustomEditor(typeof(GameEventData))]
    public class GameEventDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 기본 인스펙터 UI 그리기
            DrawDefaultInspector();

            GameEventData eventData = (GameEventData)target;

            GUILayout.Space(20);
            
            EditorGUILayout.LabelField("UI Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("게임을 실행하지 않고 현재 설정된 이벤트 UI 레이아웃을 확인합니다.\n미리보기 창은 Game 탭에서 확인할 수 있습니다.", MessageType.Info);

            if (GUILayout.Button("Preview Event UI", GUILayout.Height(40)))
            {
                // 프리뷰 생성
                EventPopupUI.PreviewInEditor(eventData);
                
                // 생성된 UI를 편하게 볼 수 있도록 Game 뷰 포커스 (선택 사항)
                EditorApplication.ExecuteMenuItem("Window/General/Game");
            }

            if (GUILayout.Button("Close Preview", GUILayout.Height(30)))
            {
                var existing = GameObject.Find("EventPopup_Preview");
                if (existing != null) DestroyImmediate(existing);
            }
        }
    }
}
