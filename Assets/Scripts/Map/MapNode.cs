using System.Collections.Generic;

/// <summary>
/// 맵의 개별 노드 데이터를 담는 순수 C# 클래스입니다.
/// MonoBehaviour가 아니므로 데이터와 시각적 표현이 분리됩니다.
/// </summary>
[System.Serializable]
public class MapNode
{
    /// <summary>노드 고유 식별자</summary>
    public int id;

    /// <summary>층 번호 (1~15)</summary>
    public int floor;

    /// <summary>해당 층 내에서의 인덱스 (0부터 시작)</summary>
    public int indexInFloor;

    /// <summary>노드의 종류 (전투, 이벤트, 엘리트, 마을, 보스 등)</summary>
    public NodeType nodeType;

    /// <summary>양방향 연결된 노드 목록 (앞/뒤/옆 모두 포함)</summary>
    [System.NonSerialized]
    public List<MapNode> connectedNodes = new List<MapNode>();

    /// <summary>연결된 노드들의 ID 목록 (직렬화용)</summary>
    public List<int> connectedNodeIds = new List<int>();

    /// <summary>이 노드가 붕괴되었는지 여부</summary>
    public bool isCollapsed;

    /// <summary>플레이어가 이 노드를 방문했는지 여부</summary>
    public bool isVisited;

    public MapNode(int id, int floor, int indexInFloor, NodeType nodeType)
    {
        this.id = id;
        this.floor = floor;
        this.indexInFloor = indexInFloor;
        this.nodeType = nodeType;
        this.connectedNodes = new List<MapNode>();
        this.connectedNodeIds = new List<int>();
        this.isCollapsed = false;
        this.isVisited = false;
    }

    /// <summary>
    /// 다른 노드와 양방향 연결을 추가합니다.
    /// 이미 연결되어 있으면 무시합니다.
    /// </summary>
    public void ConnectTo(MapNode other)
    {
        if (other == null || other == this) return;

        if (!connectedNodes.Contains(other))
        {
            connectedNodes.Add(other);
            connectedNodeIds.Add(other.id);
        }

        if (!other.connectedNodes.Contains(this))
        {
            other.connectedNodes.Add(this);
            other.connectedNodeIds.Add(this.id);
        }
    }

    /// <summary>
    /// 이 노드가 특정 노드와 연결되어 있는지 확인합니다.
    /// </summary>
    public bool IsConnectedTo(MapNode other)
    {
        return connectedNodes.Contains(other);
    }

    /// <summary>
    /// 이 노드로 이동이 가능한지 확인합니다.
    /// 붕괴되지 않았고, 현재 노드와 연결되어 있어야 합니다.
    /// </summary>
    public bool IsAccessibleFrom(MapNode currentNode)
    {
        return !isCollapsed && IsConnectedTo(currentNode);
    }

    public override string ToString()
    {
        return $"[Node {id}] Floor:{floor} Index:{indexInFloor} Type:{nodeType} Collapsed:{isCollapsed} Connections:{connectedNodes.Count}";
    }
}