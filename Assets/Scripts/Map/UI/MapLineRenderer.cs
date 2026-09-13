using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 맵 노드 간의 연결선을 그리는 컴포넌트입니다.
/// 철로 그림 대신 깔끔하고 세련된 점선(Dashed Line)으로 노드들을 연결합니다.
/// </summary>
public class MapLineRenderer : MonoBehaviour
{
    [Header("Dashed Line Settings (점선 설정)")]
    [Tooltip("점선 두께")]
    [SerializeField] private float dashThickness = 5.0f;
    [Tooltip("점선 하나의 길이")]
    [SerializeField] private float dashLength = 12f;
    [Tooltip("점선 사이의 간격")]
    [SerializeField] private float dashGap = 8f;
    [Tooltip("둥근 모서리 점선(캡슐) 사용 여부")]
    [SerializeField] private bool roundedDashes = true;

    [Header("Line Colors")]
    [Tooltip("비활성 일반 경로: 양피지 위에서 자연스럽고 분명하게 보이는 차분한 빈티지 잉크선")]
    [SerializeField] private Color normalColor = new Color(0.35f, 0.30f, 0.25f, 0.45f);
    [Tooltip("이동 가능한 활성 경로: 선명하고 밝은 순백색")]
    [SerializeField] private Color accessibleColor = new Color(1f, 1f, 1f, 1f);

    private List<LineSegment> lines = new List<LineSegment>();
    private RectTransform parentRect;
    private RectTransform lineContainer;

    private static Sprite dashSprite;

    private void Awake()
    {
        // 씬 파일에 이전의 보이지 않던(0.2 alpha) 설정이나 구버전 값이 남아있을 경우 가시성 좋은 최적값으로 자동 보정
        if (normalColor.r > 0.5f || normalColor.a < 0.35f)
        {
            normalColor = new Color(0.35f, 0.30f, 0.25f, 0.45f);
        }
        if (dashThickness < 4.0f || dashThickness > 6.0f) dashThickness = 5.0f;
        if (dashLength < 8f || dashLength > 16f) dashLength = 12f;
        if (dashGap < 6f || dashGap > 12f) dashGap = 8f;
    }

    private void OnValidate()
    {
        if (dashThickness < 4.0f || dashThickness > 6.0f) dashThickness = 5.0f;
    }

    /// <summary>
    /// 한 줄의 점선 선분 데이터
    /// </summary>
    private class LineSegment
    {
        public RectTransform rectTransform;
        public List<Image> dashImages = new List<Image>();
        public MapNode fromNode;
        public MapNode toNode;

        public void SetColor(Color color)
        {
            for (int i = 0; i < dashImages.Count; i++)
            {
                if (dashImages[i] != null)
                {
                    dashImages[i].color = color;
                }
            }
        }
    }

    public void Initialize(RectTransform parent)
    {
        parentRect = parent;
        
        // 선을 담을 전용 컨테이너 생성 또는 기존 컨테이너 연결
        Transform existing = parentRect.Find("LineContainer");
        if (existing != null)
        {
            lineContainer = existing.GetComponent<RectTransform>();
        }
        else
        {
            GameObject containerObj = new GameObject("LineContainer");
            lineContainer = containerObj.AddComponent<RectTransform>();
            lineContainer.SetParent(parentRect, false);
            
            // 앵커와 여백 초기화
            lineContainer.anchorMin = Vector2.zero;
            lineContainer.anchorMax = Vector2.one;
            lineContainer.offsetMin = Vector2.zero;
            lineContainer.offsetMax = Vector2.zero;
        }

        // 노드가 배치되기 직전에 생성되므로, 맨 앞에 깔리거나 노드 아래에 배치
        lineContainer.SetAsFirstSibling();
    }

    /// <summary>
    /// 두 노드 사이에 점선 연결선을 생성합니다.
    /// 노드 아이콘 내부로 점선이 침범하지 않도록 노드 반지름만큼 양 끝을 띄워서 그립니다.
    /// </summary>
    public void CreateLine(MapNode from, MapNode to, Vector2 fromPos, Vector2 toPos, float nodeRadius = 45f)
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

        Vector2 direction = toPos - fromPos;
        float distance = direction.magnitude;
        if (distance <= 0.001f) return;

        // 노드 경계 바깥에서 점선이 시작되고 끝나도록 오프셋 적용
        float nodePadding = 8f;
        float startOffset = nodeRadius + nodePadding;
        float endOffset = nodeRadius + nodePadding;
        float effectiveDistance = distance - (startOffset + endOffset);

        // 노드 사이 거리가 너무 가까워 점선 1개도 안 들어가면 생략
        if (effectiveDistance < dashLength) return;

        Vector2 dirNorm = direction / distance;
        Vector2 startPos = fromPos + dirNorm * startOffset;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        GameObject lineObj = new GameObject($"Line_{from.id}_{to.id}");
        lineObj.transform.SetParent(lineContainer, false);

        RectTransform rect = lineObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = startPos;
        rect.sizeDelta = new Vector2(effectiveDistance, dashThickness);
        rect.localRotation = Quaternion.Euler(0, 0, angle);

        LineSegment segment = new LineSegment
        {
            rectTransform = rect,
            fromNode = from,
            toNode = to
        };

        // 점선 배치 계산 (유효 거리 effectiveDistance 내에만 균등 배치)
        float step = dashLength + dashGap;
        int count = Mathf.FloorToInt((effectiveDistance + dashGap) / step);
        if (count < 1) count = 1;
        float totalSpan = count * dashLength + (count - 1) * dashGap;
        float startX = Mathf.Max(0f, (effectiveDistance - totalSpan) * 0.5f);

        Sprite currentDashSprite = roundedDashes ? GetOrCreateDashSprite() : null;

        for (int i = 0; i < count; i++)
        {
            float curX = startX + i * step;
            if (curX + dashLength > effectiveDistance + 1f) break;

            GameObject dashObj = new GameObject("Dash");
            dashObj.transform.SetParent(lineObj.transform, false);

            RectTransform dashRect = dashObj.AddComponent<RectTransform>();
            dashRect.anchorMin = new Vector2(0f, 0.5f);
            dashRect.anchorMax = new Vector2(0f, 0.5f);
            dashRect.pivot = new Vector2(0f, 0.5f);
            dashRect.anchoredPosition = new Vector2(curX, 0f);
            dashRect.sizeDelta = new Vector2(dashLength, dashThickness);

            Image dashImg = dashObj.AddComponent<Image>();
            dashImg.sprite = currentDashSprite;
            dashImg.color = normalColor;
            dashImg.raycastTarget = false; // 클릭 이벤트 방해 방지

            segment.dashImages.Add(dashImg);
        }

        lines.Add(segment);
    }

    /// <summary>
    /// 모든 연결선의 색상을 현재 상태에 따라 업데이트합니다.
    /// 현재 노드에서 전진 가능한 노드로 이어지는 선만 밝게 하이라이트합니다.
    /// </summary>
    public void UpdateLineColors(MapNode currentNode, CollapseManager collapseManager = null)
    {
        foreach (var line in lines)
        {
            Color targetColor = normalColor;

            // 현재 노드에서 바로 다음 층(floor + 1)으로 연결되는 선만 하이라이트
            if (currentNode != null)
            {
                bool isForwardAccessible = 
                    (line.fromNode == currentNode && line.toNode != null && line.toNode.floor == currentNode.floor + 1) ||
                    (line.toNode == currentNode && line.fromNode != null && line.fromNode.floor == currentNode.floor + 1);

                if (isForwardAccessible)
                {
                    targetColor = accessibleColor;
                }
            }

            line.SetColor(targetColor);
        }
    }

    /// <summary>
    /// 모든 연결선을 제거합니다.
    /// </summary>
    public void ClearAllLines()
    {
        if (lineContainer == null && parentRect != null)
        {
            Transform existing = parentRect.Find("LineContainer");
            if (existing != null) lineContainer = existing.GetComponent<RectTransform>();
        }

        foreach (var line in lines)
        {
            if (line.rectTransform != null)
            {
                line.rectTransform.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(line.rectTransform.gameObject);
                else DestroyImmediate(line.rectTransform.gameObject);
            }
        }
        lines.Clear();

        if (lineContainer != null)
        {
            for (int i = lineContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = lineContainer.GetChild(i);
                child.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }
    }

    /// <summary>
    /// 부드러운 둥근 모서리(캡슐형) 점선용 절차적 스프라이트 생성
    /// </summary>
    private static Sprite GetOrCreateDashSprite()
    {
        if (dashSprite != null) return dashSprite;

        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        float radius = size * 0.5f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        
        // 9-slice 테두리 (7, 7, 7, 7)로 길이 늘어나도 양 끝이 둥근 캡슐 유지
        dashSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(7, 7, 7, 7));
        return dashSprite;
    }
}