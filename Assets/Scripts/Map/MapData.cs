using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 한 스테이지의 전체 맵 데이터를 담는 클래스입니다.
/// MapGenerator에 의해 생성되며, 맵의 모든 노드와 층 구조를 관리합니다.
/// </summary>
[System.Serializable]
public class MapData
{
    /// <summary>전체 노드 리스트</summary>
    public List<MapNode> allNodes = new List<MapNode>();

    /// <summary>층별로 그룹핑된 노드 (floors[0] = 1층 노드들)</summary>
    [System.NonSerialized]
    public List<List<MapNode>> floors = new List<List<MapNode>>();

    /// <summary>총 층 수 (기본 15)</summary>
    public int totalFloors;

    /// <summary>현재 플레이어가 위치한 노드</summary>
    [System.NonSerialized]
    public MapNode currentNode;

    /// <summary>현재 플레이어가 위치한 노드의 ID (직렬화용)</summary>
    public int currentNodeId = -1;

    public MapData(int totalFloors = 15)
    {
        this.totalFloors = totalFloors;
        this.allNodes = new List<MapNode>();
        this.floors = new List<List<MapNode>>();

        // 층 리스트 초기화
        for (int i = 0; i < totalFloors; i++)
        {
            floors.Add(new List<MapNode>());
        }
    }

    /// <summary>
    /// 노드를 맵에 추가합니다. 자동으로 해당 층의 리스트에도 등록됩니다.
    /// </summary>
    public void AddNode(MapNode node)
    {
        allNodes.Add(node);

        int floorIndex = node.floor - 1; // floor는 1-based, index는 0-based
        if (floorIndex >= 0 && floorIndex < floors.Count)
        {
            floors[floorIndex].Add(node);
        }
    }

    /// <summary>
    /// ID로 노드를 검색합니다.
    /// </summary>
    public MapNode GetNodeById(int id)
    {
        return allNodes.Find(n => n.id == id);
    }

    /// <summary>
    /// 특정 층의 모든 노드를 반환합니다.
    /// </summary>
    public List<MapNode> GetFloorNodes(int floor)
    {
        int floorIndex = floor - 1;
        if (floorIndex >= 0 && floorIndex < floors.Count)
        {
            return floors[floorIndex];
        }
        return new List<MapNode>();
    }

    /// <summary>
    /// 현재 노드에서 이동 가능한 노드 목록을 반환합니다.
    /// (연결되어 있고, 붕괴되지 않은 노드만)
    /// </summary>
    public List<MapNode> GetAccessibleNodes()
    {
        if (currentNode == null) return new List<MapNode>();

        return currentNode.connectedNodes
            .Where(n => !n.isCollapsed)
            .ToList();
    }

    /// <summary>
    /// 특정 층의 모든 노드를 붕괴 처리합니다.
    /// </summary>
    public void CollapseFloor(int floor)
    {
        var floorNodes = GetFloorNodes(floor);
        foreach (var node in floorNodes)
        {
            node.isCollapsed = true;
        }
    }

    /// <summary>
    /// 직렬화 후 참조를 복원합니다.
    /// connectedNodeIds를 기반으로 connectedNodes 리스트를 재구성합니다.
    /// </summary>
    public void RebuildReferences()
    {
        // 층 리스트 재구성
        floors = new List<List<MapNode>>();
        for (int i = 0; i < totalFloors; i++)
        {
            floors.Add(new List<MapNode>());
        }

        foreach (var node in allNodes)
        {
            int floorIndex = node.floor - 1;
            if (floorIndex >= 0 && floorIndex < floors.Count)
            {
                floors[floorIndex].Add(node);
            }

            // connectedNodes 리스트 초기화
            node.connectedNodes = new List<MapNode>();
        }

        // 연결 관계 복원
        foreach (var node in allNodes)
        {
            foreach (int connectedId in node.connectedNodeIds)
            {
                MapNode connectedNode = GetNodeById(connectedId);
                if (connectedNode != null && !node.connectedNodes.Contains(connectedNode))
                {
                    node.connectedNodes.Add(connectedNode);
                }
            }
        }

        // 현재 노드 복원
        if (currentNodeId >= 0)
        {
            currentNode = GetNodeById(currentNodeId);
        }
    }

    /// <summary>
    /// 맵 데이터를 Debug.Log로 출력합니다 (디버그용).
    /// </summary>
    public void DebugPrint()
    {
        UnityEngine.Debug.Log($"=== MapData: {totalFloors} Floors, {allNodes.Count} Nodes ===");

        for (int f = 1; f <= totalFloors; f++)
        {
            var floorNodes = GetFloorNodes(f);
            string nodesInfo = string.Join(", ", floorNodes.Select(n =>
                $"{n.nodeType}(id:{n.id}, conn:{n.connectedNodes.Count})"));
            UnityEngine.Debug.Log($"  Floor {f,2}: [{nodesInfo}]");
        }

        if (currentNode != null)
        {
            UnityEngine.Debug.Log($"  Current Node: {currentNode}");
        }
    }
}