#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace TheLastArk.Editor
{
    public static class MapNodeDiagnostic
    {
        [MenuItem("TheLastArk/진단: 맵 노드 및 UI 상태 전수 검사")]
        public static void RunDiagnostic()
        {
            Debug.Log("==================== [MapNodeDiagnostic] 진단 시작 ====================");
            Debug.Log($"[MapNodeDiagnostic] isPlaying: {Application.isPlaying}");

            // 1. EventSystem 검사
            var es = Object.FindObjectOfType<EventSystem>();
            if (es == null)
            {
                Debug.LogWarning("[MapNodeDiagnostic] ⚠️ EventSystem이 씬에 존재하지 않습니다.");
            }
            else
            {
                Debug.Log($"[MapNodeDiagnostic] ✅ EventSystem 존재: {es.gameObject.name}, active={es.gameObject.activeInHierarchy}, enabled={es.enabled}");
                var inputModule = es.GetComponent<BaseInputModule>();
                Debug.Log($"[MapNodeDiagnostic]    InputModule: {(inputModule != null ? inputModule.GetType().Name : "None")}");
            }

            // 2. Canvas & GraphicRaycaster 검사
            var canvases = Object.FindObjectsOfType<Canvas>();
            Debug.Log($"[MapNodeDiagnostic] 씬 내 Canvas 개수: {canvases.Length}");
            foreach (var c in canvases)
            {
                var gr = c.GetComponent<GraphicRaycaster>();
                Debug.Log($"[MapNodeDiagnostic]    Canvas '{c.name}': renderMode={c.renderMode}, sortingOrder={c.sortingOrder}, GraphicRaycaster={(gr != null && gr.enabled ? "✅ On" : "❌ OFF/None")}");
            }

            // 3. MapManager 검사
            var mapManager = Object.FindObjectOfType<MapManager>();
            if (mapManager == null)
            {
                Debug.LogWarning("[MapNodeDiagnostic] ⚠️ 현재 씬에서 MapManager를 찾을 수 없습니다. (MapScene을 열고 실행해주세요)");
                return;
            }

            Debug.Log($"[MapNodeDiagnostic] MapManager: isReadOnly={mapManager.isReadOnly}, mapContainer={(mapManager.GetMapContainer() != null ? mapManager.GetMapContainer().name : "null")}");
            var cur = mapManager.GetCurrentNode();
            Debug.Log($"[MapNodeDiagnostic] MapManager.GetCurrentNode(): {(cur != null ? $"id:{cur.id}, floor:{cur.floor}, type:{cur.nodeType}" : "null")}");

            // 4. 노드 UI 전수 검사
            var nodeUIs = Object.FindObjectsOfType<MapNodeUI>(true);
            Debug.Log($"[MapNodeDiagnostic] 씬 내 MapNodeUI 개수: {nodeUIs.Length}");

            int activeCount = 0;
            int interactableCount = 0;
            int accessibleCount = 0;
            int hasClickListenerCount = 0;

            foreach (var nui in nodeUIs)
            {
                if (!nui.gameObject.activeInHierarchy) continue;
                activeCount++;

                var btn = nui.GetComponent<Button>();
                var data = nui.GetNodeData();
                bool hasBtn = btn != null;
                bool isInteractable = hasBtn && btn.interactable;
                bool isRaycastTarget = nui.nodeIcon != null && nui.nodeIcon.raycastTarget;

                if (isInteractable) interactableCount++;
                if (hasBtn && btn.onClick.GetPersistentEventCount() > 0) hasClickListenerCount++;

                // 처음 몇 개 및 중요한 노드 상세 출력 (1층 및 2층 노드)
                if (data != null && data.floor <= 3)
                {
                    bool isAccessibleFromCur = cur != null && data.IsAccessibleFrom(cur);
                    bool isConnected = cur != null && data.IsConnectedTo(cur);
                    if (isAccessibleFromCur) accessibleCount++;

                    Debug.Log($"[MapNodeDiagnostic] 노드 '{nui.gameObject.name}': Floor={data.floor}, Id={data.id}, Type={data.nodeType} " +
                              $"| Active={nui.gameObject.activeInHierarchy} " +
                              $"| Btn={(hasBtn ? (btn.interactable ? "Interactable" : "Disabled") : "NoBtn")} " +
                              $"| IconRC={(nui.nodeIcon != null ? nui.nodeIcon.raycastTarget.ToString() : "null")} " +
                              $"| IconColor={(nui.nodeIcon != null ? nui.nodeIcon.color.ToString() : "null")} " +
                              $"| IconSprite={(nui.nodeIcon != null && nui.nodeIcon.sprite != null ? nui.nodeIcon.sprite.name : "null")} " +
                              $"| IsConnToCur={isConnected}, IsAcc={isAccessibleFromCur}");
                }
                else if (data == null)
                {
                    Debug.LogWarning($"[MapNodeDiagnostic] ⚠️ 노드 '{nui.gameObject.name}': nodeData가 NULL입니다! (Setup 미실행 가능성)");
                }
            }

            Debug.Log($"[MapNodeDiagnostic] 집계: 활성 노드={activeCount}, Interactable 노드={interactableCount}, 1~3층 접근가능 노드={accessibleCount}");

            // 5. ScrollView 및 캔버스 상위 오버레이 레이캐스트 방해 검사
            var allGraphics = Object.FindObjectsOfType<Graphic>(true);
            int blockingCount = 0;
            foreach (var g in allGraphics)
            {
                if (!g.raycastTarget || !g.gameObject.activeInHierarchy) continue;
                if (g.transform.IsChildOf(mapManager.transform) || (mapManager.GetMapContainer() != null && g.transform.IsChildOf(mapManager.GetMapContainer())))
                    continue;

                // 맵 영역을 가릴 수 있는 전면 그래픽 확인
                var rt = g.rectTransform;
                if (rt.rect.width > 500 && rt.rect.height > 500 && g.color.a < 0.05f)
                {
                    Debug.LogWarning($"[MapNodeDiagnostic] ⚠️ 투명하면서 넓은 Raycast Graphic 발견 (클릭 차단 위험): '{g.gameObject.name}' on '{g.transform.parent?.name}', size={rt.rect.size}, color={g.color}");
                    blockingCount++;
                }
            }
            if (blockingCount == 0)
            {
                Debug.Log("[MapNodeDiagnostic] ✅ 전면 투명 방해 Graphic 없음.");
            }

            Debug.Log("==================== [MapNodeDiagnostic] 진단 완료 ====================");
        }
    }
}
#endif
