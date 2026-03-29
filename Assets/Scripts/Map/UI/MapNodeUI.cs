using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 맵의 개별 노드를 시각화하는 UI 컴포넌트입니다.
/// Button 위에 아이콘 Image를 배치하고, 상태에 따라 색상/이펙트를 변경합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class MapNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // ─────────────────────────────────────────────
    // 참조
    // ─────────────────────────────────────────────

    [Header("References")]
    public Image nodeIcon;
    public Image glowEffect;
    public Image warningEffect;
    public TMPro.TextMeshProUGUI floorLabel;

    private Button button;
    private MapNode nodeData;
    private MapManager mapManager;

    // ─────────────────────────────────────────────
    // 노드 타입별 색상
    // ─────────────────────────────────────────────

    private static readonly Color COLOR_START    = new Color(0.3f, 0.8f, 1.0f);  // 하늘색
    private static readonly Color COLOR_COMBAT   = new Color(0.9f, 0.3f, 0.3f);  // 빨강
    private static readonly Color COLOR_EVENT    = new Color(1.0f, 0.85f, 0.2f); // 노랑
    private static readonly Color COLOR_ELITE    = new Color(0.7f, 0.3f, 0.9f);  // 보라
    private static readonly Color COLOR_REST     = new Color(0.3f, 0.9f, 0.4f);  // 초록
    private static readonly Color COLOR_BOSS     = new Color(1.0f, 0.7f, 0.1f);  // 금색
    private static readonly Color COLOR_COLLAPSED = new Color(0.2f, 0.2f, 0.2f, 0.5f); // 어두운 회색
    private static readonly Color COLOR_DIMMED   = new Color(0.5f, 0.5f, 0.5f, 0.6f);  // 흐린 회색

    // ─────────────────────────────────────────────
    // 노드 타입별 라벨
    // ─────────────────────────────────────────────

    private static readonly string LABEL_START  = "GO";
    private static readonly string LABEL_COMBAT = "B";
    private static readonly string LABEL_EVENT  = "?";
    private static readonly string LABEL_ELITE  = "E!";
    private static readonly string LABEL_REST   = "R";
    private static readonly string LABEL_BOSS   = "BOSS";

    // ─────────────────────────────────────────────
    // 상태
    // ─────────────────────────────────────────────

    private bool isCurrentNode;
    private bool isAccessible;
    private bool isHovered;

    private Sprite originalIconSprite;

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────

    /// <summary>
    /// 노드 데이터를 설정하고 UI를 초기화합니다.
    /// </summary>
    public void Setup(MapNode data, MapManager manager)
    {
        nodeData = data;
        mapManager = manager;
        button = GetComponent<Button>();

        if (originalIconSprite == null && nodeIcon != null)
        {
            originalIconSprite = nodeIcon.sprite;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnNodeClicked);

        // 아이콘 텍스트 설정
        if (floorLabel != null)
        {
            floorLabel.text = GetNodeLabel(data.nodeType);
        }

        UpdateVisual();
    }

    public MapNode GetNodeData() => nodeData;

    // ─────────────────────────────────────────────
    // 시각 업데이트
    // ─────────────────────────────────────────────

    /// <summary>
    /// 현재 상태에 따라 노드의 시각적 표현을 업데이트합니다.
    /// </summary>
    public void UpdateVisual()
    {
        if (nodeData == null) return;

        // 현재 위치 & 접근 가능 여부 갱신
        if (mapManager != null)
        {
            isCurrentNode = (mapManager.GetCurrentNode() == nodeData);
            isAccessible = !nodeData.isCollapsed &&
                           mapManager.GetCurrentNode() != null &&
                           nodeData.IsConnectedTo(mapManager.GetCurrentNode());
        }

        // 색상 결정
        Color targetColor;

        if (nodeData.isCollapsed)
        {
            targetColor = COLOR_COLLAPSED;
            button.interactable = false;
        }
        else if (isCurrentNode)
        {
            targetColor = GetNodeColor(nodeData.nodeType);
            button.interactable = false; // 현재 위치는 클릭 불가
        }
        else if (isAccessible)
        {
            targetColor = GetNodeColor(nodeData.nodeType);
            button.interactable = true;
        }
        else
        {
            targetColor = COLOR_DIMMED;
            button.interactable = false;
        }

        // 아이콘 색상 및 스프라이트 적용
        if (nodeIcon != null)
        {
            Sprite customIcon = GetNodeIconSprite(nodeData.nodeType);
            if (customIcon != null)
            {
                nodeIcon.sprite = customIcon;
                
                // 커스텀 아이콘이면 형태 유지를 위해 기본 white로 고정 (회색/dim 처리 제거)
                nodeIcon.color = Color.white;
                
                if (floorLabel != null) floorLabel.gameObject.SetActive(false);
            }
            else
            {
                nodeIcon.sprite = originalIconSprite;
                nodeIcon.color = targetColor;
                if (floorLabel != null) floorLabel.gameObject.SetActive(true);
            }

            // 시작과 보스 노드일 경우 아이콘 크기를 2배로 키웁니다.
            if (nodeData.nodeType == NodeType.Start || nodeData.nodeType == NodeType.Boss)
            {
                nodeIcon.transform.localScale = Vector3.one * 2f;
            }
            else
            {
                nodeIcon.transform.localScale = Vector3.one;
            }
        }

        // 현재 위치 글로우 (기차 표시로 대체됨)
        if (glowEffect != null)
        {
            glowEffect.gameObject.SetActive(false);
        }

        // 경고 이펙트
        if (warningEffect != null)
        {
            bool showWarning = mapManager != null &&
                               mapManager.GetCollapseManager() != null &&
                               mapManager.GetCollapseManager().IsFloorWarned(nodeData.floor);
            warningEffect.gameObject.SetActive(showWarning);
        }

        // 방문 여부에 따른 살짝 어둡게
        if (nodeData.isVisited && !isCurrentNode && !nodeData.isCollapsed)
        {
            if (nodeIcon != null)
            {
                Color visited = nodeIcon.color;
                visited.a = 0.7f;
                nodeIcon.color = visited;
            }
        }
    }

    /// <summary>
    /// 붕괴 경고 시 빨간 펄스 이펙트를 재생합니다.
    /// </summary>
    public void PlayWarningPulse()
    {
        if (warningEffect != null)
        {
            warningEffect.gameObject.SetActive(true);
            // TODO: DOTween이나 코루틴으로 펄스 애니메이션 추가
        }
    }

    /// <summary>
    /// 붕괴 연출을 재생합니다.
    /// </summary>
    public void PlayCollapseEffect()
    {
        if (nodeIcon != null)
        {
            nodeIcon.color = COLOR_COLLAPSED;
        }
        button.interactable = false;
        // TODO: 붕괴 파티클/셰이크 이펙트 추가
    }

    // ─────────────────────────────────────────────
    // 이벤트 핸들러
    // ─────────────────────────────────────────────

    private void OnNodeClicked()
    {
        if (nodeData == null || mapManager == null) return;

        mapManager.OnNodeSelected(nodeData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (isAccessible && nodeIcon != null)
        {
            // 호버 시 색상 강조
            Color hoverColor = GetNodeIconSprite(nodeData.nodeType) != null ? Color.white : GetNodeColor(nodeData.nodeType);
            hoverColor = Color.Lerp(hoverColor, Color.white, 0.3f);
            nodeIcon.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateVisual();
    }

    // ─────────────────────────────────────────────
    // 유틸리티
    // ─────────────────────────────────────────────

    private static Color GetNodeColor(NodeType type)
    {
        switch (type)
        {
            case NodeType.Start:  return COLOR_START;
            case NodeType.Combat: return COLOR_COMBAT;
            case NodeType.Event:  return COLOR_EVENT;
            case NodeType.Elite:  return COLOR_ELITE;
            case NodeType.Rest:   return COLOR_REST;
            case NodeType.Boss:   return COLOR_BOSS;
            default:              return Color.white;
        }
    }

    private static string GetNodeLabel(NodeType type)
    {
        switch (type)
        {
            case NodeType.Start:  return LABEL_START;
            case NodeType.Combat: return LABEL_COMBAT;
            case NodeType.Event:  return LABEL_EVENT;
            case NodeType.Elite:  return LABEL_ELITE;
            case NodeType.Rest:   return LABEL_REST;
            case NodeType.Boss:   return LABEL_BOSS;
            default:              return "?";
        }
    }
    private static Sprite GetNodeIconSprite(NodeType type)
    {
        Sprite s = null;
        switch (type)
        {
            case NodeType.Start:  s = Resources.Load<Sprite>("UI/Node_Start"); break;
            case NodeType.Combat: s = Resources.Load<Sprite>("UI/Node_Enemy"); break;
            case NodeType.Event:  s = Resources.Load<Sprite>("UI/Node_Event"); break;
            case NodeType.Elite:  s = Resources.Load<Sprite>("UI/Node_Elite"); break;
            case NodeType.Rest:   s = Resources.Load<Sprite>("UI/Node_Rest"); break;
            case NodeType.Boss:   s = Resources.Load<Sprite>("UI/Node_Boss_1"); break;
        }
        
        if (s == null)
        {
            Debug.LogWarning($"[MapNodeUI] 아이콘 로드 실패: {type} 노드의 이미지를 Resources/UI/ 에서 찾을 수 없습니다.");
        }
        
        return s;
    }
}