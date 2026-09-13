using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵을 절차적으로 생성하는 정적 클래스입니다.
/// 노드 배치 → 간선 연결 → 타입 할당 → 검증 순서로 맵을 구축합니다.
/// </summary>
public static class MapGenerator
{
    // ─────────────────────────────────────────────
    // 설정값 (상수)
    // ─────────────────────────────────────────────

    public const int TOTAL_FLOORS = 15;
    public const int MIN_NODES_PER_FLOOR = 3;
    public const int MAX_NODES_PER_FLOOR = 5; // 최대 5개 노드까지 다채롭게 등장 (3~5개)
    public const int ELITE_FORCED_FLOOR = 9;

    private static readonly int[] REST_GUARANTEED_FLOORS = { 5, 14 };

    private const int WEIGHT_COMBAT = 50;
    private const int WEIGHT_EVENT = 25;
    private const int WEIGHT_ELITE = 15;
    private const int WEIGHT_REST = 10;

    // ─────────────────────────────────────────────
    // 메인 생성 메서드
    // ─────────────────────────────────────────────

    public static MapData Generate(int seed = -1)
    {
        if (seed >= 0) Random.InitState(seed);

        MapData mapData = new MapData(TOTAL_FLOORS);
        int nextId = 0;

        GenerateNodes(mapData, ref nextId);
        WireEdges(mapData);
        AssignNodeTypes(mapData);

        bool valid = Validate(mapData);
        if (!valid)
        {
            Debug.LogWarning("[MapGenerator] 맵 검증 실패! 재생성을 시도합니다.");
            return Generate(seed >= 0 ? seed + 1 : -1);
        }

        Debug.Log("[MapGenerator] 맵 생성 완료!");
        mapData.DebugPrint();
        return mapData;
    }

    // ─────────────────────────────────────────────
    // Step 1: 노드 배치 (층당 3~5개로 풍성하게 구성)
    // ─────────────────────────────────────────────

    private static void GenerateNodes(MapData mapData, ref int nextId)
    {
        for (int floor = 1; floor <= TOTAL_FLOORS; floor++)
        {
            int nodeCount;
            if (floor == 1) nodeCount = 1;                        // 시작 노드 1개
            else if (floor == TOTAL_FLOORS) nodeCount = 1;        // 보스 노드 1개
            else if (floor == TOTAL_FLOORS - 1) nodeCount = Random.Range(2, 4); // 보스 직전 2~3개 수렴
            else nodeCount = Random.Range(MIN_NODES_PER_FLOOR, MAX_NODES_PER_FLOOR + 1); // 3~5개 노드 (4~5개 빈번 등장)

            for (int i = 0; i < nodeCount; i++)
            {
                MapNode node = new MapNode(nextId++, floor, i, NodeType.Combat);
                mapData.AddNode(node);
            }
        }
    }

    // ─────────────────────────────────────────────
    // Step 2: 간선 연결 (존재하는 노드에만 정확히 1:1 대응 연결)
    // ─────────────────────────────────────────────

    private static void WireEdges(MapData mapData)
    {
        for (int floor = 1; floor < TOTAL_FLOORS; floor++)
        {
            List<MapNode> curFloor = mapData.GetFloorNodes(floor);
            List<MapNode> nxtFloor = mapData.GetFloorNodes(floor + 1);

            if (curFloor.Count == 0 || nxtFloor.Count == 0) continue;

            curFloor.Sort((a, b) => a.indexInFloor.CompareTo(b.indexInFloor));
            nxtFloor.Sort((a, b) => a.indexInFloor.CompareTo(b.indexInFloor));

            // [중요] 1층 시작점 등 단일 노드 층인 경우:
            // 다음 층에 실제로 생성된 노드 개수(nxtFloor.Count)만큼만 정확하게 1개씩 연결
            // 다음 층 노드가 3개면 오직 3개만, 5개면 5개만 연결되어 허공으로 선이 뻗지 않음
            if (curFloor.Count == 1)
            {
                foreach (var nxt in nxtFloor)
                {
                    curFloor[0].ConnectTo(nxt);
                }
                continue;
            }

            // 다음 층이 1개(예: 보스 층)인 경우:
            // 현재 층의 모든 노드가 보스 노드로 1개씩 연결
            if (nxtFloor.Count == 1)
            {
                foreach (var cur in curFloor)
                {
                    cur.ConnectTo(nxtFloor[0]);
                }
                continue;
            }

            // 1) 기본 전진 연결: 각 현재 노드에서 가장 인접한 다음 층 노드로 1개 연결
            foreach (var node in curFloor)
            {
                MapNode target = FindClosestByRatio(node, curFloor, nxtFloor);
                node.ConnectTo(target);
            }

            // 2) 다음 층에서 연결이 없는 고립 노드 구제 (교차 방지 및 분기 수 제한 적용)
            foreach (var node in nxtFloor)
            {
                bool hasIncoming = false;
                foreach (var cur in curFloor)
                {
                    if (cur.IsConnectedTo(node)) { hasIncoming = true; break; }
                }

                if (!hasIncoming)
                {
                    MapNode target = FindBestSourceForOrphan(node, curFloor, nxtFloor);
                    if (target != null)
                    {
                        target.ConnectTo(node);
                    }
                }
            }

            // 3) 교차하지 않는 선택지 분기 추가 (과도한 거미줄 방지: 층당 최대 1개, 35% 확률, 최대 2개 분기까지만)
            if (curFloor.Count >= 2 && nxtFloor.Count >= 2 && Random.value < 0.35f)
            {
                AddOneNonCrossingEdge(curFloor, nxtFloor);
            }
        }
    }

    /// <summary>
    /// 비율 기반으로 가장 적절한 대상 노드를 찾습니다.
    /// </summary>
    private static MapNode FindClosestByRatio(MapNode source, List<MapNode> sourceFloor, List<MapNode> targetFloor)
    {
        if (targetFloor.Count == 1) return targetFloor[0];

        float sourceRatio = (sourceFloor.Count <= 1)
            ? 0.5f
            : (float)source.indexInFloor / (sourceFloor.Count - 1);

        MapNode best = targetFloor[0];
        float bestDist = float.MaxValue;

        for (int i = 0; i < targetFloor.Count; i++)
        {
            float targetRatio = (float)i / (targetFloor.Count - 1);
            float dist = Mathf.Abs(sourceRatio - targetRatio);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = targetFloor[i];
            }
        }

        return best;
    }

    private static MapNode FindBestSourceForOrphan(MapNode orphan, List<MapNode> curFloor, List<MapNode> nxtFloor)
    {
        if (curFloor.Count == 1) return curFloor[0];

        float targetRatio = (float)orphan.indexInFloor / Mathf.Max(1, nxtFloor.Count - 1);
        MapNode best = null;
        float bestScore = float.MaxValue;

        foreach (var cur in curFloor)
        {
            float curRatio = (float)cur.indexInFloor / Mathf.Max(1, curFloor.Count - 1);
            float dist = Mathf.Abs(curRatio - targetRatio);

            // 이미 연결된 선과 교차하는지 검사
            bool crosses = false;
            foreach (var other in curFloor)
            {
                if (other == cur) continue;
                foreach (var conn in other.connectedNodes)
                {
                    if (nxtFloor.Contains(conn))
                    {
                        if (EdgesCross(cur.indexInFloor, orphan.indexInFloor, other.indexInFloor, conn.indexInFloor))
                        {
                            crosses = true;
                            break;
                        }
                    }
                }
                if (crosses) break;
            }

            if (crosses) continue;

            // 이미 다음 층으로 2개 이상 분기된 노드는 피함
            int forwardCount = 0;
            foreach (var conn in cur.connectedNodes)
                if (nxtFloor.Contains(conn)) forwardCount++;

            float score = dist + forwardCount * 2.0f;
            if (score < bestScore)
            {
                bestScore = score;
                best = cur;
            }
        }

        return best != null ? best : FindClosestByRatio(orphan, nxtFloor, curFloor);
    }

    private static void AddOneNonCrossingEdge(List<MapNode> curFloor, List<MapNode> nxtFloor)
    {
        List<System.Tuple<int, int>> existing = new List<System.Tuple<int, int>>();
        foreach (var node in curFloor)
            foreach (var conn in node.connectedNodes)
                if (nxtFloor.Contains(conn))
                    existing.Add(new System.Tuple<int, int>(node.indexInFloor, conn.indexInFloor));

        // 인접한 미연결 쌍이면서, 분기 수가 1개 이하인 노드만 후보로 등록
        List<System.Tuple<MapNode, MapNode>> candidates = new List<System.Tuple<MapNode, MapNode>>();
        foreach (var cur in curFloor)
        {
            int forwardCount = 0;
            foreach (var conn in cur.connectedNodes)
                if (nxtFloor.Contains(conn)) forwardCount++;
            if (forwardCount >= 2) continue; // 이미 2개 분기된 노드는 추가 분기 금지

            foreach (var nxt in nxtFloor)
            {
                if (cur.IsConnectedTo(nxt)) continue;
                int normIdx = Mathf.RoundToInt(
                    (float)cur.indexInFloor / Mathf.Max(1, curFloor.Count - 1) * (nxtFloor.Count - 1));
                if (Mathf.Abs(normIdx - nxt.indexInFloor) <= 1)
                    candidates.Add(new System.Tuple<MapNode, MapNode>(cur, nxt));
            }
        }

        ShuffleList(candidates);

        foreach (var c in candidates)
        {
            bool crosses = false;
            foreach (var e in existing)
                if (EdgesCross(e.Item1, e.Item2, c.Item1.indexInFloor, c.Item2.indexInFloor))
                { crosses = true; break; }

            if (!crosses)
            {
                c.Item1.ConnectTo(c.Item2);
                return;
            }
        }
    }

    private static bool EdgesCross(int ci1, int ni1, int ci2, int ni2)
    {
        return (ci1 < ci2 && ni1 > ni2) || (ci1 > ci2 && ni1 < ni2);
    }

    // ─────────────────────────────────────────────
    // Step 3: 노드 타입 할당
    // ─────────────────────────────────────────────

    private static void AssignNodeTypes(MapData mapData)
    {
        foreach (var n in mapData.GetFloorNodes(1)) n.nodeType = NodeType.Start;
        foreach (var n in mapData.GetFloorNodes(TOTAL_FLOORS)) n.nodeType = NodeType.Boss;
        foreach (var n in mapData.GetFloorNodes(ELITE_FORCED_FLOOR)) n.nodeType = NodeType.Elite;

        for (int floor = 2; floor <= TOTAL_FLOORS - 1; floor++)
        {
            if (floor == ELITE_FORCED_FLOOR) continue;
            foreach (var n in mapData.GetFloorNodes(floor)) n.nodeType = GetRandomNodeType();
        }

        foreach (int floor in REST_GUARANTEED_FLOORS)
        {
            var nodes = mapData.GetFloorNodes(floor);
            bool hasRest = false;
            foreach (var n in nodes) if (n.nodeType == NodeType.Rest) { hasRest = true; break; }
            if (!hasRest && nodes.Count > 0) nodes[Random.Range(0, nodes.Count)].nodeType = NodeType.Rest;
        }
    }

    private static NodeType GetRandomNodeType()
    {
        int roll = Random.Range(0, 100);
        if (roll < WEIGHT_COMBAT) return NodeType.Combat;
        if (roll < WEIGHT_COMBAT + WEIGHT_EVENT) return NodeType.Event;
        if (roll < WEIGHT_COMBAT + WEIGHT_EVENT + WEIGHT_ELITE) return NodeType.Elite;
        return NodeType.Rest;
    }

    // ─────────────────────────────────────────────
    // Step 4: 검증
    // ─────────────────────────────────────────────

    private static bool Validate(MapData mapData)
    {
        bool valid = true;

        for (int floor = 1; floor <= TOTAL_FLOORS; floor++)
        {
            int count = mapData.GetFloorNodes(floor).Count;
            if (floor == 1 && count != 1) { Debug.LogError("[Validate] 1층 노드 수 오류"); valid = false; }
            else if (floor == TOTAL_FLOORS && count != 1) { Debug.LogError("[Validate] 15층 노드 수 오류"); valid = false; }
            else if (floor > 1 && floor < TOTAL_FLOORS && (count < MIN_NODES_PER_FLOOR || count > MAX_NODES_PER_FLOOR))
            { Debug.LogError($"[Validate] {floor}층 노드 수 범위 초과: {count}"); valid = false; }
        }

        foreach (var n in mapData.GetFloorNodes(1)) if (n.nodeType != NodeType.Start) { valid = false; }
        foreach (var n in mapData.GetFloorNodes(TOTAL_FLOORS)) if (n.nodeType != NodeType.Boss) { valid = false; }
        foreach (var n in mapData.GetFloorNodes(ELITE_FORCED_FLOOR)) if (n.nodeType != NodeType.Elite) { valid = false; }

        foreach (int floor in REST_GUARANTEED_FLOORS)
        {
            bool hr = false;
            foreach (var n in mapData.GetFloorNodes(floor)) if (n.nodeType == NodeType.Rest) { hr = true; break; }
            if (!hr) { Debug.LogError($"[Validate] {floor}층에 Rest 없음"); valid = false; }
        }

        foreach (var n in mapData.allNodes)
            if (n.connectedNodes.Count == 0) { Debug.LogError($"[Validate] 연결 없음: {n}"); valid = false; }

        if (!HasPathBFS(mapData, 1, TOTAL_FLOORS))
        { Debug.LogError("[Validate] 1→15층 경로 없음"); valid = false; }

        return valid;
    }

    private static bool HasPathBFS(MapData mapData, int startFloor, int targetFloor)
    {
        var startNodes = mapData.GetFloorNodes(startFloor);
        if (startNodes.Count == 0) return false;

        HashSet<int> visited = new HashSet<int>();
        Queue<MapNode> queue = new Queue<MapNode>();
        foreach (var s in startNodes) { queue.Enqueue(s); visited.Add(s.id); }

        while (queue.Count > 0)
        {
            MapNode cur = queue.Dequeue();
            if (cur.floor == targetFloor) return true;
            foreach (var nb in cur.connectedNodes)
                if (!visited.Contains(nb.id)) { visited.Add(nb.id); queue.Enqueue(nb); }
        }
        return false;
    }

    private static void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i]; list[i] = list[j]; list[j] = temp;
        }
    }


private static void AddCrossLaneEdge(List<MapNode> curFloor, List<MapNode> nxtFloor)
    {
        List<System.Tuple<MapNode, MapNode>> candidates = new List<System.Tuple<MapNode, MapNode>>();

        foreach (var cur in curFloor)
        {
            foreach (var nxt in nxtFloor)
            {
                if (cur.IsConnectedTo(nxt)) continue;

                float curRatio = (curFloor.Count <= 1) ? 0.5f : (float)cur.indexInFloor / (curFloor.Count - 1);
                float nxtRatio = (nxtFloor.Count <= 1) ? 0.5f : (float)nxt.indexInFloor / (nxtFloor.Count - 1);
                float diff = Mathf.Abs(curRatio - nxtRatio);

                // 인접 레인만 허용 (0.15~0.45) — 2칸 건너뛰기 방지
                if (diff >= 0.15f && diff <= 0.45f)
                {
                    candidates.Add(new System.Tuple<MapNode, MapNode>(cur, nxt));
                }
            }
        }

        if (candidates.Count > 0)
        {
            var pick = candidates[Random.Range(0, candidates.Count)];
            pick.Item1.ConnectTo(pick.Item2);
        }
    }
}