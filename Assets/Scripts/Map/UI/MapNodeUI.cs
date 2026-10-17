using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 맵의 개별 노드를 시각화하는 UI 컴포넌트입니다.
/// Button 위에 아이콘 Image를 배치하고, 상태에 따라 색상/이펙트를 변경합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class MapNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
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
    private bool isPressed;

    private Sprite originalIconSprite;
    private static Sprite circleGlowSprite;
    private static Material iconTintMaterial;

    private static readonly Color COLOR_CURRENT_NODE = new Color(0.20f, 0.70f, 1.0f, 1f); // 은은하고 세련된 스카이 블루 (현재 위치)

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

        if (button != null)
        {
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveAllListeners();
            if (Application.isPlaying)
            {
                button.onClick.AddListener(OnNodeClicked);
            }
        }

        // 아이콘 텍스트 설정
        if (floorLabel != null)
        {
            floorLabel.text = GetNodeLabel(data.nodeType);
            floorLabel.raycastTarget = false;
        }

        UpdateVisual();
    }

    public MapNode GetNodeData() => nodeData;

    void Update()
    {
        if (isCurrentNode)
        {
            // 내가 지금 있는 위치: 살짝 은은하게 반짝이는 부드러운 호흡 애니메이션
            float pulse = Mathf.Sin(Time.time * 2.2f) * 0.035f; // -0.035 ~ +0.035
            float baseScale = isPressed ? 1.05f : 1.14f;
            transform.localScale = Vector3.one * (baseScale + pulse);

            if (glowEffect != null && glowEffect.gameObject.activeSelf)
            {
                // 푸른색 아우라 글로우도 은은한 강도(0.18 ~ 0.25)로 숨쉬듯 깜빡임
                glowEffect.color = new Color(COLOR_CURRENT_NODE.r, COLOR_CURRENT_NODE.g, COLOR_CURRENT_NODE.b, 0.22f + pulse * 1.0f);
            }
        }
    }

    // ─────────────────────────────────────────────
    // 시각 업데이트
    // ─────────────────────────────────────────────

    /// <summary>
    /// 현재 상태에 따라 노드의 시각적 표현을 업데이트합니다.
    /// </summary>
    public void UpdateVisual()
    {
        if (nodeData == null) return;

        // 현재 위치 & 접근 가능 여부 갱신 (오직 바로 다음 층으로 전진만 가능)
        if (mapManager != null)
        {
            MapNode cur = mapManager.GetCurrentNode();
            isCurrentNode = (cur != null && cur.id == nodeData.id);
            isAccessible = (cur != null && nodeData.floor == cur.floor + 1 && nodeData.IsConnectedTo(cur));
        }

        if (button != null)
        {
            button.transition = Selectable.Transition.None;
            // 버튼 자체는 항상 클릭 이벤트를 수신하도록 활성화 (현재 노드 클릭 시 안내 및 클릭 누락 방지)
            button.interactable = mapManager != null && !mapManager.isReadOnly;
        }

        // 아이콘 색상 및 스프라이트 적용
        if (nodeIcon != null)
        {
            Sprite customIcon = GetNodeIconSprite(nodeData.nodeType);
            if (customIcon != null)
            {
                nodeIcon.sprite = customIcon;

                if (isCurrentNode)
                {
                    // 1. 내가 지금 있는 위치: 맑고 세련된 푸른색(Sky Blue)으로 변경
                    nodeIcon.material = GetOrCreateTintMaterial();
                    nodeIcon.color = COLOR_CURRENT_NODE;
                    if (!isHovered && !isPressed) transform.localScale = Vector3.one * 1.14f;

                    if (glowEffect != null)
                    {
                        glowEffect.sprite = GetOrCreateCircleGlowSprite();
                        glowEffect.color = new Color(COLOR_CURRENT_NODE.r, COLOR_CURRENT_NODE.g, COLOR_CURRENT_NODE.b, 0.22f);
                        glowEffect.gameObject.SetActive(true);
                    }
                }
                else if (isAccessible)
                {
                    // 2. 그다음 갈 수 있는 노드: 더 크고(1.15배) 진하게(100% 불투명) 표시
                    nodeIcon.material = null;
                    nodeIcon.color = Color.white;
                    if (!isHovered && !isPressed) transform.localScale = Vector3.one * 1.15f;

                    if (glowEffect != null) glowEffect.gameObject.SetActive(false);
                }
                else
                {
                    // 3. 다른데(지나온 노드 또는 아직 갈 수 없는 먼 노드): 연하게(30% 반투명) 작게(0.95배) 표시
                    nodeIcon.material = null;
                    nodeIcon.color = new Color(1f, 1f, 1f, 0.30f);
                    if (!isHovered && !isPressed) transform.localScale = Vector3.one * 0.95f;

                    if (glowEffect != null) glowEffect.gameObject.SetActive(false);
                }

                if (floorLabel != null) floorLabel.gameObject.SetActive(false);
            }
            else
            {
                nodeIcon.sprite = originalIconSprite;
                nodeIcon.color = isCurrentNode || isAccessible ? GetNodeColor(nodeData.nodeType) : COLOR_DIMMED;
                if (floorLabel != null) floorLabel.gameObject.SetActive(true);
            }
        }

        if (warningEffect != null)
        {
            warningEffect.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 외부에서 일시적인 펄스 강조 연출을 트리거할 때 사용합니다.
    /// </summary>
    public void TriggerPulseHighlight()
    {
        if (this == null || !gameObject.activeInHierarchy) return;
        StopAllCoroutines();
        StartCoroutine(CoPulseHighlight());
    }

    private System.Collections.IEnumerator CoPulseHighlight()
    {
        Vector3 orig = transform.localScale;
        for (int i = 0; i < 2; i++)
        {
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 6f;
                transform.localScale = Vector3.Lerp(orig, orig * 1.25f, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
        }
        transform.localScale = orig;
    }

    /// <summary>
    /// 붕괴 시스템 제거 (호환성 유지용 빈 메서드)
    /// </summary>
    public void PlayWarningPulse()
    {
    }

    /// <summary>
    /// 붕괴 시스템 제거 (호환성 유지용 빈 메서드)
    /// </summary>
    public void PlayCollapseEffect()
    {
    }

    // ─────────────────────────────────────────────
    // 이벤트 핸들러
    // ─────────────────────────────────────────────

    private void OnNodeClicked()
    {
        if (nodeData == null || mapManager == null || mapManager.isReadOnly) return;

        if (isCurrentNode)
        {
            // 현재 노드 클릭 시: 친절한 안내 및 다음 이동 가능 노드 유도 연출
            Debug.Log($"[MapNodeUI] 현재 위치 노드 클릭됨: {nodeData.floor}층 ({nodeData.nodeType}). 다음 층의 연결된 노드를 선택하세요.");
            mapManager.NotifyCurrentNodeClicked();
            return;
        }

        if (isAccessible)
        {
            // 정상 전진 이동 실행
            Debug.Log($"[MapNodeUI] 노드 클릭 → 이동 실행: {nodeData.floor}층 ({nodeData.nodeType}, Id={nodeData.id})");
            mapManager.OnNodeSelected(nodeData);
        }
        else
        {
            // 아직 도달할 수 없는 먼 노드 또는 잘못된 노드 클릭 시
            Debug.Log($"[MapNodeUI] 아직 이동할 수 없는 노드 클릭됨: {nodeData.floor}층 (현재 층에서 연결된 바로 다음 층만 전진 가능)");
            TriggerPulseHighlight();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        isPressed = true;
        // 마우스를 누르는 순간 쫀득한 시각적 눌림 반응 (0.92배)
        float target = (isAccessible ? 1.20f : (isCurrentNode ? 1.14f : 0.95f)) * 0.92f;
        transform.localScale = Vector3.one * target;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        isPressed = false;
        if (isHovered && isAccessible && !isCurrentNode)
        {
            transform.localScale = Vector3.one * 1.25f;
        }
        else
        {
            UpdateVisual();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (isAccessible && !isCurrentNode && nodeIcon != null)
        {
            // 이동 가능한 노드에 마우스를 올리면 반응감 있게 확대 피드백 (1.25배)
            transform.localScale = Vector3.one * (isPressed ? 1.10f : 1.25f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        if (!isCurrentNode)
        {
            UpdateVisual();
        }
    }

    // ─────────────────────────────────────────────
    // 유틸리티
    // ─────────────────────────────────────────────

    public static Sprite GetOrCreateCircleGlowSprite()
    {
        if (circleGlowSprite != null) return circleGlowSprite;

        int size = 64;
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
                float t = Mathf.Clamp01(dist / radius);
                float alpha = Mathf.Pow(Mathf.Cos(t * Mathf.PI * 0.5f), 2f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        circleGlowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return circleGlowSprite;
    }

    public static Material GetOrCreateTintMaterial()
    {
        if (iconTintMaterial != null) return iconTintMaterial;
        Shader s = Shader.Find("UI/MapNodeIconTint");
        if (s != null)
        {
            iconTintMaterial = new Material(s);
        }
        return iconTintMaterial;
    }

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

    private Sprite GetNodeIconSprite(NodeType type)
    {
        if (mapManager != null)
        {
            Sprite s = mapManager.GetNodeSprite(type);
            if (s != null) return s;
        }

        Sprite fallback = null;
        switch (type)
        {
            case NodeType.Start:  fallback = Resources.Load<Sprite>("Map/node_start") ?? Resources.Load<Sprite>("UI/Node_Start"); break;
            case NodeType.Combat: fallback = Resources.Load<Sprite>("Map/node_battle") ?? Resources.Load<Sprite>("UI/Node_Enemy"); break;
            case NodeType.Event:  fallback = Resources.Load<Sprite>("Map/node_event") ?? Resources.Load<Sprite>("UI/Node_Event"); break;
            case NodeType.Elite:  fallback = Resources.Load<Sprite>("Map/node_elite") ?? Resources.Load<Sprite>("UI/Node_Elite"); break;
            case NodeType.Rest:   fallback = Resources.Load<Sprite>("Map/node_rest") ?? Resources.Load<Sprite>("UI/Node_Rest"); break;
            case NodeType.Boss:   fallback = Resources.Load<Sprite>("UI/Node_Boss_1"); break;
        }
        
        if (fallback == null)
        {
            Debug.LogWarning($"[MapNodeUI] 아이콘 로드 실패: {type} 노드의 이미지를 찾을 수 없습니다.");
        }
        
        return fallback;
    }
}
