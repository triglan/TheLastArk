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
    public const int MAX_NODES_PER_FLOOR = 5;
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
    // Step 1: 노드 배치
    // ─────────────────────────────────────────────

    private static void GenerateNodes(MapData mapData, ref int nextId)
    {
        for (int floor = 1; floor <= TOTAL_FLOORS; floor++)
        {
            int nodeCount;
            if (floor == 1) nodeCount = 1;
            else if (floor == TOTAL_FLOORS) nodeCount = 1;
            else nodeCount = Random.Range(MIN_NODES_PER_FLOOR, MAX_NODES_PER_FLOOR + 1);

            for (int i = 0; i < nodeCount; i++)
            {
                MapNode node = new MapNode(nextId++, floor, i, NodeType.Combat);
                mapData.AddNode(node);
            }
        }
    }

    // ─────────────────────────────────────────────
    // Step 2: 간선 연결 (1~2개 제한, 교차 방지)
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

            // 1) 기본 연결: 각 노드에서 다음 층으로 1개 보장
            foreach (var node in curFloor)
            {
                MapNode target = FindClosestByRatio(node, curFloor, nxtFloor);
                node.ConnectTo(target);
            }

            // 2) 다음 층에서 연결 없는 노드 구제
            foreach (var node in nxtFloor)
            {
                if (node.connectedNodes.Count == 0)
                {
                    MapNode target = FindClosestByRatio(node, nxtFloor, curFloor);
                    node.ConnectTo(target);
                }
            }

            // 3) 크로스 레인 연결 (인접 레인만, 2칸 건너뛰기 금지)
            //    1개 확정 + 40% 확률로 +1, 10% 확률로 +2
            if (curFloor.Count >= 2 && nxtFloor.Count >= 2)
            {
                int crossCount = 1; // 확정 1개

                float roll = Random.value;
                if (roll < 0.10f)
                    crossCount += 2;     // 10%: 총 3개
                else if (roll < 0.50f)
                    crossCount += 1;     // 40%: 총 2개
                // 나머지 50%: 총 1개

                for (int i = 0; i < crossCount; i++)
                {
                    AddCrossLaneEdge(curFloor, nxtFloor);
                }
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

    private static void AddOneNonCrossingEdge(List<MapNode> curFloor, List<MapNode> nxtFloor)
    {
        List<System.Tuple<int, int>> existing = new List<System.Tuple<int, int>>();
        foreach (var node in curFloor)
            foreach (var conn in node.connectedNodes)
                if (nxtFloor.Contains(conn))
                    existing.Add(new System.Tuple<int, int>(node.indexInFloor, conn.indexInFloor));

        // 인접한(인덱스 차이 1) 미연결 쌍만 후보
        List<System.Tuple<MapNode, MapNode>> candidates = new List<System.Tuple<MapNode, MapNode>>();
        foreach (var cur in curFloor)
        {
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