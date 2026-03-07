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

        // ─── 붕괴 시스템 테스트 ───
        Debug.Log("\n===== [CollapseTest] 붕괴 시뮬레이션 시작 =====");

        CollapseManager collapse = new CollapseManager();

        // 이벤트 구독
        collapse.OnCollapseWarning += (floor) => Debug.Log($"  [Event] ⚠️ 경고: {floor}층 붕괴 예정!");
        collapse.OnFloorCollapsed += (floor) => Debug.Log($"  [Event] 💥 붕괴: {floor}층 붕괴 완료!");
        collapse.OnGameOver += () => Debug.Log("  [Event] 💀 게임 오버!");
        collapse.OnTurnChanged += (turn, remaining) => Debug.Log($"  [Event] 턴 변경: {turn}턴 (붕괴까지 {remaining}턴)");

        // 9턴 시뮬레이션 (3번의 붕괴 발생 예상)
        for (int i = 0; i < 9; i++)
        {
            Debug.Log($"\n--- 이동 {i + 1} ---");

            // 시뮬레이션: 플레이어가 위로 이동
            var accessible = mapData.GetAccessibleNodes();
            if (accessible.Count > 0)
            {
                // 가능하면 위층으로 이동
                MapNode nextNode = null;
                foreach (var node in accessible)
                {
                    if (node.floor > mapData.currentNode.floor)
                    {
                        nextNode = node;
                        break;
                    }
                }
                if (nextNode == null) nextNode = accessible[0];

                // 이동 안전성 확인
                MoveValidation validation = collapse.ValidateMove(nextNode);
                Debug.Log($"  이동 검증 [{mapData.currentNode.floor}층 → {nextNode.floor}층]: {validation}");

                mapData.currentNode = nextNode;
                mapData.currentNodeId = nextNode.id;
                Debug.Log($"  현재 위치: {mapData.currentNode}");
            }

            // 턴 처리
            CollapseResult result = collapse.ProcessTurn(mapData);
            Debug.Log($"  턴 결과: {result}");

            if (result == CollapseResult.GameOver)
            {
                Debug.Log("  💀 시뮬레이션 중단 (게임 오버)");
                break;
            }
        }

        // 최종 상태 출력
        Debug.Log("\n===== [CollapseTest] 최종 상태 =====");
        collapse.DebugPrint();
        Debug.Log($"  현재 플레이어 위치: {mapData.currentNode}");
        Debug.Log($"  이동 가능 노드 수: {mapData.GetAccessibleNodes().Count}");
        Debug.Log("===== [MapGeneratorTest] 전체 테스트 완료 =====");
    }
}