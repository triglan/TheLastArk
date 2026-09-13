using UnityEngine;

/// <summary>
/// MapGenerator의 결과를 검증하는 테스트 스크립트입니다.
/// 씬에 빈 GameObject를 놓고 이 컴포넌트를 붙여서 Play하면 콘솔에 결과가 출력됩니다.
/// </summary>
public class MapGeneratorTest : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("고정 시드값 (-1이면 매번 다른 맵)")]
    public int seed = 42;

void Start()
    {
        Debug.Log("===== [MapGeneratorTest] 맵 생성 + 붕괴 테스트 시작 =====");

        MapData mapData = MapGenerator.Generate(seed);

        if (mapData == null)
        {
            Debug.LogError("[MapGeneratorTest] 맵 생성 실패!");
            return;
        }

        // 시작 노드 설정
        mapData.currentNode = mapData.GetFloorNodes(1)[0];
        mapData.currentNodeId = mapData.currentNode.id;

        // ─── 전진 이동 시뮬레이션 테스트 ───
        Debug.Log("\n===== [ForwardMoveTest] 전진 이동 시뮬레이션 시작 =====");

        int currentTurn = 0;
        while (mapData.currentNode != null && mapData.currentNode.floor < mapData.totalFloors)
        {
            currentTurn++;
            var accessible = mapData.GetAccessibleNodes();
            Debug.Log($"\n--- 턴 {currentTurn} (현재: {mapData.currentNode.floor}층 [{mapData.currentNode.nodeType}]) ---");
            Debug.Log($"  이동 가능한 전진 노드 수: {accessible.Count}");

            if (accessible.Count == 0)
            {
                Debug.LogError($"  [오류] {mapData.currentNode.floor}층에서 다음 층으로 이동할 수 있는 노드가 없습니다!");
                break;
            }

            // 첫 번째 이동 가능한 노드로 전진
            MapNode nextNode = accessible[0];
            bool canAccess = nextNode.IsAccessibleFrom(mapData.currentNode);
            Debug.Log($"  이동 시도: {mapData.currentNode.floor}층 → {nextNode.floor}층 ({nextNode.nodeType}), 이동가능 여부: {canAccess}");

            mapData.currentNode = nextNode;
            mapData.currentNodeId = nextNode.id;
            mapData.currentNode.isVisited = true;
        }

        // 최종 상태 출력
        Debug.Log("\n===== [ForwardMoveTest] 최종 상태 =====");
        Debug.Log($"  최종 도달 위치: {mapData.currentNode}");
        Debug.Log($"  총 소요 턴: {currentTurn}");
        Debug.Log("===== [MapGeneratorTest] 전체 테스트 완료 =====");
    }
}