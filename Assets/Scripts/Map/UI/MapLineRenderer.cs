using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 맵 노드 간의 연결선을 그리는 컴포넌트입니다.
/// UI Image를 사용하여 두 노드 사이에 선을 그립니다.
/// </summary>
public class MapLineRenderer : MonoBehaviour
{
    [Header("Line Settings")]
    [SerializeField] private float lineWidth = 3f;
    [SerializeField] private Color normalColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
    [SerializeField] private Color accessibleColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color collapsedColor = new Color(0.3f, 0.1f, 0.1f, 0.4f);
    [SerializeField] private Color warningColor = new Color(1f, 0.3f, 0.1f, 0.8f);

    private List<LineSegment> lines = new List<LineSegment>();
    private RectTransform parentRect;

    /// <summary>
    /// 한 줄의 선분 데이터
    /// </summary>
    private class LineSegment
    {
        public RectTransform rectTransform;
        public Image image;
        public MapNode fromNode;
        public MapNode toNode;
    }

    public void Initialize(RectTransform parent)
    {
        parentRect = parent;
    }

    /// <summary>
    /// 두 노드 사이에 연결선을 생성합니다.
    /// </summary>
    public void CreateLine(MapNode from, MapNode to, Vector2 fromPos, Vector2 toPos)
    {
        // 중복 방지: 이미 같은 쌍의 선이 있으면 생략
        foreach (var existing in lines)
        {
            if ((existing.fromNode == from && existing.toNode == to) ||
                (existing.fromNode == to && existing.toNode == from))
            {
                return;
            }
        }

        GameObject lineObj = new GameObject($"Line_{from.id}_{to.id}");
        lineObj.transform.SetParent(parentRect, false);

        RectTransform rect = lineObj.AddComponent<RectTransform>();
        Image image = lineObj.AddComponent<Image>();
        image.color = normalColor;

        // 선 위치 & 회전 계산
        UpdateLineTransform(rect, fromPos, toPos);

        // 선을 노드 뒤에 배치 (sibling index 0)
        lineObj.transform.SetAsFirstSibling();

        LineSegment segment = new LineSegment
        {
            rectTransform = rect,
            image = image,
            fromNode = from,
            toNode = to
        };

        lines.Add(segment);
    }

    /// <summary>
    /// 두 점 사이에 선을 배치합니다.
    /// </summary>
private void UpdateLineTransform(RectTransform rect, Vector2 fromPos, Vector2 toPos)
    {
        Vector2 direction = toPos - fromPos;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 노드와 동일한 앵커 (가로 레이아웃: 왼쪽 중앙)
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);

        rect.anchoredPosition = fromPos;
        rect.sizeDelta = new Vector2(distance, lineWidth);
        rect.localRotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// 모든 연결선의 색상을 현재 상태에 따라 업데이트합니다.
    /// </summary>
    public void UpdateLineColors(MapNode currentNode, CollapseManager collapseManager)
    {
        foreach (var line in lines)
        {
            if (line.image == null) continue;

            bool fromCollapsed = line.fromNode.isCollapsed;
            bool toCollapsed = line.toNode.isCollapsed;

            // 둘 다 또는 하나라도 붕괴된 경우
            if (fromCollapsed || toCollapsed)
            {
                line.image.color = collapsedColor;
                continue;
            }

            // 경고 중인 층의 선
            if (collapseManager != null)
            {
                bool fromWarned = collapseManager.IsFloorWarned(line.fromNode.floor);
                bool toWarned = collapseManager.IsFloorWarned(line.toNode.floor);

                if (fromWarned || toWarned)
                {
                    line.image.color = warningColor;
                    continue;
                }
            }

            // 현재 노드에서 이동 가능한 선
            if (currentNode != null)
            {
                bool isAccessible = (line.fromNode == currentNode && !line.toNode.isCollapsed) ||
                                     (line.toNode == currentNode && !line.fromNode.isCollapsed);

                if (isAccessible)
                {
                    line.image.color = accessibleColor;
                    continue;
                }
            }

            // 기본 색상
            line.image.color = normalColor;
        }
    }

    /// <summary>
    /// 모든 연결선을 제거합니다.
    /// </summary>
    public void ClearAllLines()
    {
        foreach (var line in lines)
        {
            if (line.rectTransform != null)
            {
                Destroy(line.rectTransform.gameObject);
            }
        }
        lines.Clear();
    }
}