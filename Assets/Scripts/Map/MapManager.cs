using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TheLastArk.Map.Events;

/// <summary>
/// 맵 씬의 메인 매니저.
/// 맵 생성, UI 배치, 플레이어 이동, 붕괴 시스템을 총괄합니다.
/// 씬에 Canvas와 함께 배치하면 자동으로 맵을 구성합니다.
/// </summary>
public class MapManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector 설정
    // ─────────────────────────────────────────────

    [Header("Map Settings")]
    [Tooltip("랜덤 시드 (-1이면 매번 다름)")]
    [SerializeField] private int mapSeed = -1;

    [Header("UI References")]
    [Tooltip("노드가 배치될 부모 RectTransform (Canvas 하위)")]
    [SerializeField] private RectTransform mapContainer;

    [Header("UI Settings")]
    [SerializeField] private float nodeSize = 150f;
    [SerializeField] private float mapSpacingX = 250f;   // 층(Floor) 간 X 간격
    [SerializeField] private float mapSpacingY = 200f;   // 노드 간 Y 간격

    [Header("Info Panel")]
    [SerializeField] private TMPro.TextMeshProUGUI turnInfoText;
    [SerializeField] private TMPro.TextMeshProUGUI floorInfoText;
    [SerializeField] private TMPro.TextMeshProUGUI collapseWarningText;

    // ─────────────────────────────────────────────
    // 내부 데이터
    // ─────────────────────────────────────────────

    private MapData mapData;
    private TMPro.TMP_FontAsset mainFont;

    private CollapseManager collapseManager;
    private MapLineRenderer lineRenderer;

    private Dictionary<int, MapNodeUI> nodeUIMap = new Dictionary<int, MapNodeUI>();
    private Dictionary<int, Vector2> nodePositions = new Dictionary<int, Vector2>();

    private RectTransform trainIndicator;

    // ─────────────────────────────────────────────
    // 공개 접근자 (MapNodeUI에서 사용)
    // ─────────────────────────────────────────────

    public MapNode GetCurrentNode() => mapData?.currentNode;
    public CollapseManager GetCollapseManager() => collapseManager;

    // ─────────────────────────────────────────────
    // 라이프사이클
    // ─────────────────────────────────────────────

    void Start()
    {
        InitializeMap();
    }

    void OnGUI()
    {
        // 디버깅용 이벤트 강제 트리거 버튼 (화면 좌측 하단)
        if (GUI.Button(new Rect(10, Screen.height - 45, 110, 30), "Debug: Event"))
        {
            var eventMgr = EventManager.Instance;
            var eventData = eventMgr?.GetRandomEvent(1);
            if (eventData != null)
            {
                Debug.Log($"[MapManager Debug] 이벤트 강제 실행: {eventData.eventTitle}");
                EventPopupUI.Show(eventData, () =>
                {
                    UpdateAllVisuals();
                    UpdateInfoPanel();
                });
            }
            else
            {
                Debug.LogWarning("[MapManager] 발생 가능한 이벤트가 없습니다.");
            }
        }

        // 디버깅용 마을 씬 강제 이동 버튼 (화면 좌측 하단)
        if (GUI.Button(new Rect(130, Screen.height - 45, 110, 30), "Debug: Village"))
        {
            Debug.Log("[MapManager Debug] 마을 씬 강제 이동");
            UnityEngine.SceneManagement.SceneManager.LoadScene("VillageScene");
        }
    }

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────

private void InitializeMap()
    {
        // 폰트 로드
        mainFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Main_Fonts");
        if (mainFont == null)
            Debug.LogWarning("[MapManager] Main_Fonts를 찾을 수 없습니다.");

        if (mapContainer == null)
        {
            SetupDefaultUI();
        }

        // 붕괴 매니저 초기화 (이벤트 연결)
        collapseManager = new CollapseManager();
        collapseManager.OnCollapseWarning += OnCollapseWarning;
        collapseManager.OnFloorCollapsed += OnFloorCollapsed;
        collapseManager.OnGameOver += OnGameOver;
        collapseManager.OnTurnChanged += OnTurnChanged;

        // RunManager 연동 (전역 데이터 로드)
        RunManager rm = RunManager.Instance;
        if (rm.CurrentMap != null)
        {
            // 기존 맵 데이터 복원
            mapData = rm.CurrentMap;
            collapseManager.turnCount = rm.CurrentTurn;

            // 저장된 위치가 없으면 1층의 Start 노드로 설정
            if (rm.CurrentNode == null)
            {
                mapData.currentNode = mapData.GetFloorNodes(1)[0];
            }
            else
            {
                mapData.currentNode = mapData.GetNodeById(rm.CurrentNode.id);
            }

            mapData.currentNode.isVisited = true;
            mapData.currentNodeId = mapData.currentNode.id;

            Debug.Log($"[MapManager] RunManager에서 맵 데이터를 복원했습니다. 현재 층: {mapData.currentNode.floor}, 턴: {collapseManager.turnCount}");
        }
        else
        {
            // 새 맵 생성
            mapData = MapGenerator.Generate(mapSeed);
            rm.CurrentMap = mapData;

            mapData.currentNode = mapData.GetFloorNodes(1)[0];
            mapData.currentNode.isVisited = true;
            mapData.currentNodeId = mapData.currentNode.id;

            rm.CurrentNode = mapData.currentNode;
            rm.CurrentTurn = 0;

            Debug.Log("[MapManager] 새 맵을 생성했습니다.");
        }

        // 라인 렌더러 초기화
        lineRenderer = gameObject.AddComponent<MapLineRenderer>();
        lineRenderer.Initialize(mapContainer);

        // UI 배치
        LayoutNodes();
        DrawConnections();
        UpdateAllVisuals();
        UpdateInfoPanel();

        // 자원 UI 생성
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Transform existingRes = canvas.transform.Find("ResourcePanel");
            if (existingRes == null)
            {
                var resourceUI = gameObject.GetComponent<TheLastArk.UI.ExplorationResourceUI>();
                if (resourceUI == null) resourceUI = gameObject.AddComponent<TheLastArk.UI.ExplorationResourceUI>();
                resourceUI.Initialize(canvas.transform);
            }
            
            // 디버그 버튼 생성
            Transform existingDebugBtn = canvas.transform.Find("DebugConsumableButton");
            if (existingDebugBtn == null)
            {
                CreateDebugConsumableButton(canvas.transform);
                CreateDebugRelicButton(canvas.transform);
            }
        }
    }

    private void CreateDebugConsumableButton(Transform canvasTransform)
    {
        GameObject btnObj = new GameObject("DebugConsumableButton");
        btnObj.transform.SetParent(canvasTransform, false);
        btnObj.transform.SetAsLastSibling();

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-180, 80); // 기차 관리 버튼 왼쪽 옆
        rect.sizeDelta = new Vector2(150, 50);

        UnityEngine.UI.Image img = btnObj.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.8f, 0.4f, 0.2f, 1f);

        UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();
        btn.onClick.AddListener(() => {
            Debug.Log("[MapManager] 디버그 소모품 획득 버튼 클릭");
            var consumables = Resources.LoadAll<TheLastArk.Data.ConsumableData>("Consumables");
            var resManager = TheLastArk.Managers.ResourceManager.Instance;
            if (resManager != null && consumables != null && consumables.Length > 0)
            {
                var c = consumables[UnityEngine.Random.Range(0, consumables.Length)];
                resManager.AddConsumable(c);
                Debug.Log($"[Debug] 무작위 소모품 획득: {c.consumableName}");
            }
        });

        CreateButtonText(btnObj.transform, "디버그: 소모품");
    }

    private void CreateDebugRelicButton(Transform canvasTransform)
    {
        GameObject btnObj = new GameObject("DebugRelicButton");
        btnObj.transform.SetParent(canvasTransform, false);
        btnObj.transform.SetAsLastSibling();

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-340, 80); // 소모품 버튼 왼쪽 옆
        rect.sizeDelta = new Vector2(150, 50);

        UnityEngine.UI.Image img = btnObj.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.8f, 0.2f, 0.8f, 1f);

        UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();
        btn.onClick.AddListener(() => {
            Debug.Log("[MapManager] 디버그 유물 획득 버튼 클릭");
            var relics = Resources.LoadAll<TheLastArk.Data.RelicData>("Relics");
            var resManager = TheLastArk.Managers.ResourceManager.Instance;
            if (resManager != null && relics != null && relics.Length > 0)
            {
                var available = new System.Collections.Generic.List<TheLastArk.Data.RelicData>();
                foreach (var r in relics) {
                    if (!resManager.HasRelic(r.relicID)) available.Add(r);
                }
                if (available.Count > 0)
                {
                    var r = available[UnityEngine.Random.Range(0, available.Count)];
                    resManager.AddRelic(r);
                    Debug.Log($"[Debug] 무작위 유물 획득: {r.relicName}");
                }
                else
                {
                    Debug.Log($"[Debug] 모든 유물을 이미 보유 중입니다!");
                }
            }
        });

        CreateButtonText(btnObj.transform, "디버그: 유물");
    }

    private void CreateButtonText(Transform parent, string text)
    {
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(parent, false);
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        TMPro.TextMeshProUGUI tmp = txtObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 20;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;
        if (mainFont != null) tmp.font = mainFont;
    }

    private void CreateTrainManagementButton(Transform canvasTransform)
    {
        GameObject btnObj = new GameObject("TrainManageButton");
        btnObj.transform.SetParent(canvasTransform, false);
        btnObj.transform.SetAsLastSibling();

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-20, 80); // InfoPanel 위에 배치
        rect.sizeDelta = new Vector2(150, 50);

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.4f, 0.6f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => {
            Debug.Log("[MapManager] 기차 관리 버튼 클릭");
            var trainUI = FindObjectOfType<TheLastArk.UI.TrainManagementUI>();
            if (trainUI == null) 
            {
                GameObject uiObj = new GameObject("TrainManagementUI");
                trainUI = uiObj.AddComponent<TheLastArk.UI.TrainManagementUI>();
            }
            trainUI.Show();
        });

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        TMPro.TextMeshProUGUI tmp = txtObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "기차 관리";
        tmp.fontSize = 20;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;
        if (mainFont != null) tmp.font = mainFont;
    }

    /// <summary>
    /// Canvas와 Container가 없을 때 자동 생성합니다.
    /// </summary>
/// <summary>
    /// Canvas와 Container가 없을 때 자동 생성합니다.
    /// </summary>
    private void SetupDefaultUI()
    {
        // Camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
        }

        // EventSystem
        UnityEngine.EventSystems.EventSystem es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("MapCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Scroll View (가로 스크롤)
        GameObject scrollObj = new GameObject("MapScrollView");
        scrollObj.transform.SetParent(canvas.transform, false);
        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        Image scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0, 0, 0, 0); // 배경 완전 투명하게 처리 (뒤에 Canvas 배경 보임)

        RectTransform scrollRectTransform = scrollObj.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(0, 60);
        scrollRectTransform.offsetMax = Vector2.zero;

        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        Image viewportMask = viewportObj.AddComponent<Image>();
        viewportMask.color = Color.white;
        Mask mask = viewportObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content (가로 레이아웃: 왼쪽에서 오른쪽으로)
        GameObject contentObj = new GameObject("MapContent");
        contentObj.transform.SetParent(viewportObj.transform, false);
        mapContainer = contentObj.AddComponent<RectTransform>();
        mapContainer.anchorMin = new Vector2(0f, 0.5f);
        mapContainer.anchorMax = new Vector2(0f, 0.5f);
        mapContainer.pivot = new Vector2(0f, 0.5f);

        // 배경 이미지 (ScrollRect 뒤에 그려지도록 Canvas 정적인 영역에 배치)
        Sprite bgSprite = Resources.Load<Sprite>("Map/MapBackground");
        if (bgSprite != null)
        {
            GameObject bgObj = new GameObject("StaticBackground");
            // Canvas 직속 자식으로 넣어서 ScrollRect와 분리하여 스크롤되지 않도록 설정
            bgObj.transform.SetParent(canvas.transform, false);
            bgObj.transform.SetAsFirstSibling(); // 모든 UI(스크롤, 패널 등) 뒤에 그려지게

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.sprite = bgSprite;
            bgImage.preserveAspect = false; // 화면 비율에 맞추어 확대(stretch)
            bgImage.raycastTarget = false;

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; // 화면 전체 채우기
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Debug.Log("[MapManager] 배경 이미지 로드 성공 (고정 배경 처리)!");
        }
        else
        {
            Debug.LogWarning("[MapManager] 배경 이미지를 찾을 수 없습니다. Resources/Map/MapBackground 경로를 확인하세요.");
        }

        scrollRect.content = mapContainer;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = true;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Info Panel (하단)
        CreateInfoPanel(canvas.transform);
    }

    /// <summary>
    /// 하단 정보 패널을 생성합니다.
    /// </summary>
    private void CreateInfoPanel(Transform canvasTransform)
    {
        GameObject panelObj = new GameObject("InfoPanel");
        panelObj.transform.SetParent(canvasTransform, false);

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.sizeDelta = new Vector2(0, 60);

        // HorizontalLayoutGroup
        HorizontalLayoutGroup layout = panelObj.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 5, 5);
        layout.spacing = 30;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        // 턴 정보
        turnInfoText = CreateInfoLabel(panelObj.transform, "Turn: 0");
        // 층 정보
        floorInfoText = CreateInfoLabel(panelObj.transform, "Floor: 1");
        // 경고 텍스트
        collapseWarningText = CreateInfoLabel(panelObj.transform, "");
        collapseWarningText.color = new Color(1f, 0.3f, 0.2f);
    }

    private TMPro.TextMeshProUGUI CreateInfoLabel(Transform parent, string text)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);

        TMPro.TextMeshProUGUI tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 20;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        if (mainFont != null) tmp.font = mainFont;

        
tmp.color = Color.white;

        return tmp;
    }

    // ─────────────────────────────────────────────
    // 노드 배치
    // ─────────────────────────────────────────────

    private void LayoutNodes()
    {
        nodeUIMap.Clear();
        nodePositions.Clear();

        // 가로 방향 X축 = 층 차이, Y축 = 같은 층의 노드 배치
        float totalWidth = (mapData.totalFloors) * mapSpacingX + 400f; // 양옆 여백 추가
        float maxHeight = MapGenerator.MAX_NODES_PER_FLOOR * mapSpacingY + 400f; // 위아래 여백 추가

        // Content 크기 설정 (화면보다 훨씬 크게)
        mapContainer.sizeDelta = new Vector2(totalWidth, maxHeight);

        for (int floor = 1; floor <= mapData.totalFloors; floor++)
        {
            List<MapNode> floorNodes = mapData.GetFloorNodes(floor);
            int count = floorNodes.Count;

            // X 좌표: 왼쪽(1층)에서 오른쪽(15층)으로
            float x = (floor - 1) * mapSpacingX + 200f;

            // Y 좌표: 중앙 정렬
            float totalHeight = (count - 1) * mapSpacingY;
            float startY = -totalHeight / 2f;

            for (int i = 0; i < count; i++)
            {
                MapNode node = floorNodes[i];
                float y = startY + i * mapSpacingY;

                // 랜덤 오프셋 크게 추가 (1층과 15층 제외)
                if (floor > 1 && floor < mapData.totalFloors)
                {
                    x += Random.Range(-30f, 30f);
                    y += Random.Range(-50f, 50f);
                }

                Vector2 position = new Vector2(x, y);

                MapNodeUI nodeUI = CreateNodeUI(node, position);
                nodeUIMap[node.id] = nodeUI;
                nodePositions[node.id] = position;

                // X를 원래 값으로 복원 (다음 노드를 위해)
                x = (floor - 1) * mapSpacingX + 200f;
            }
        }
    }

private MapNodeUI CreateNodeUI(MapNode node, Vector2 position)
    {
        // 노드 루트
        GameObject nodeObj = new GameObject($"Node_{node.id}_F{node.floor}");
        nodeObj.transform.SetParent(mapContainer, false);

        RectTransform rect = nodeObj.AddComponent<RectTransform>();
        // 가로 레이아웃: 왼쪽 중앙 기준 앵커
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(nodeSize, nodeSize);

        Button button = nodeObj.AddComponent<Button>();

        // 글로우 이펙트
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(nodeObj.transform, false);
        RectTransform glowRect = glowObj.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-10, -10);
        glowRect.offsetMax = new Vector2(10, 10);
        Image glowImage = glowObj.AddComponent<Image>();
        glowImage.color = new Color(1, 1, 1, 0.3f);
        glowObj.SetActive(false);

        // 아이콘
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(nodeObj.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.color = Color.white;

        // 경고 이펙트
        GameObject warningObj = new GameObject("Warning");
        warningObj.transform.SetParent(nodeObj.transform, false);
        RectTransform warningRect = warningObj.AddComponent<RectTransform>();
        warningRect.anchorMin = Vector2.zero;
        warningRect.anchorMax = Vector2.one;
        warningRect.offsetMin = new Vector2(-5, -5);
        warningRect.offsetMax = new Vector2(5, 5);
        Image warningImage = warningObj.AddComponent<Image>();
        warningImage.color = new Color(1, 0.2f, 0.1f, 0.5f);
        warningObj.SetActive(false);

        // 라벨
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(nodeObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TMPro.TextMeshProUGUI label = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
        label.fontSize = 24;
        label.alignment = TMPro.TextAlignmentOptions.Center;
        if (mainFont != null) label.font = mainFont;

        
label.color = Color.white;

        button.targetGraphic = iconImage;
        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;

        MapNodeUI nodeUI = nodeObj.AddComponent<MapNodeUI>();

        // MapNodeUI의 필드들이 public이므로 직접 할당
        nodeUI.nodeIcon = iconImage;
        nodeUI.glowEffect = glowImage;
        nodeUI.warningEffect = warningImage;
        nodeUI.floorLabel = label;

        nodeUI.Setup(node, this);

        return nodeUI;
    }

    /// <summary>
    /// private/SerializeField에 값을 주입하는 헬퍼
    /// </summary>
    private void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(target, value);
        }
        else
        {
            Debug.LogWarning($"[MapManager] Field '{fieldName}' not found on {target.GetType().Name}");
        }
    }

    // ─────────────────────────────────────────────
    // 연결선 그리기
    // ─────────────────────────────────────────────

    private void DrawConnections()
    {
        HashSet<string> drawnPairs = new HashSet<string>();

        foreach (var node in mapData.allNodes)
        {
            if (!nodePositions.ContainsKey(node.id)) continue;

            foreach (var connected in node.connectedNodes)
            {
                if (!nodePositions.ContainsKey(connected.id)) continue;

                // 중복 방지 (A-B와 B-A는 같은 선)
                string pairKey = node.id < connected.id
                    ? $"{node.id}_{connected.id}"
                    : $"{connected.id}_{node.id}";

                if (drawnPairs.Contains(pairKey)) continue;
                drawnPairs.Add(pairKey);

                lineRenderer.CreateLine(
                    node, connected,
                    nodePositions[node.id],
                    nodePositions[connected.id]
                );
            }
        }
    }

    // ─────────────────────────────────────────────
    // 플레이어 이동
    // ─────────────────────────────────────────────

    /// <summary>
    /// MapNodeUI에서 노드 클릭 시 호출됩니다.
    /// </summary>
    public void OnNodeSelected(MapNode targetNode)
    {
        if (targetNode == null) return;

        // 이동 가능 여부 확인
        if (!targetNode.IsAccessibleFrom(mapData.currentNode))
        {
            Debug.Log($"[MapManager] 이동 불가: {targetNode} (연결되지 않음 또는 붕괴됨)");
            return;
        }

        // 이동 안전성 검증
        MoveValidation validation = collapseManager.ValidateMove(targetNode);

        switch (validation)
        {
            case MoveValidation.Blocked_Collapsed:
                Debug.Log($"[MapManager] 이동 불가: {targetNode.floor}층은 이미 붕괴됨");
                return;

            case MoveValidation.Risky_WillCollapse:
                Debug.LogWarning($"[MapManager] ⚠️ 위험! {targetNode.floor}층은 다음 턴에 붕괴됩니다!");
                // 경고를 표시하지만 이동은 허용 (위험 감수)
                break;

            case MoveValidation.Safe:
                break;
        }

        // 이동 실행
        Debug.Log($"[MapManager] 이동: {mapData.currentNode.floor}층 → {targetNode.floor}층 ({targetNode.nodeType})");

        mapData.currentNode = targetNode;
        mapData.currentNodeId = targetNode.id;
        targetNode.isVisited = true;

        // 턴 처리
        CollapseResult result = collapseManager.ProcessTurn(mapData);

        // 기차 내구도 감소
        if (TheLastArk.Managers.TrainManager.Instance != null)
        {
            TheLastArk.Managers.TrainManager.Instance.DecreaseDurability(1);
        }

        // UI 갱신
        UpdateAllVisuals();
        UpdateInfoPanel();

        // 결과 처리
        HandleCollapseResult(result, targetNode);
    }

private void HandleCollapseResult(CollapseResult result, MapNode arrivedNode)
    {
        switch (result)
        {
            case CollapseResult.GameOver:
                Debug.Log("[MapManager] 💀 게임 오버!");
                // TODO: 게임 오버 UI 표시
                break;

            case CollapseResult.Collapsed:
                Debug.Log($"[MapManager] 붕괴 발생 후 {arrivedNode.nodeType} 노드 도착!");
                HandleNodeArrival(arrivedNode);
                break;

            case CollapseResult.Warning:
            case CollapseResult.Normal:
                Debug.Log($"[MapManager] {arrivedNode.nodeType} 노드 도착!");
                HandleNodeArrival(arrivedNode);
                break;
        }
    }

    /// <summary>
    /// 노드 도착 시 타입에 따라 처리합니다.
    /// 이벤트 노드는 팝업으로, 나머지는 씬 전환으로 처리합니다.
    /// </summary>
    private void HandleNodeArrival(MapNode node)
    {
        if (node.nodeType == NodeType.Event)
        {
            // 이벤트: 맵 위에 팝업 표시
            var eventMgr = EventManager.Instance;
            var eventData = eventMgr.GetRandomEvent(1); // TODO: 현재 스테이지 연동
            if (eventData != null)
            {
                Debug.Log($"[MapManager] 이벤트 팝업 표시: {eventData.eventTitle}");
                EventPopupUI.Show(eventData, () =>
                {
                    // 팝업 닫힌 후 맵 UI 갱신
                    Debug.Log("[MapManager] 이벤트 완료, 맵 UI 갱신");
                    UpdateAllVisuals();
                    UpdateInfoPanel();
                });
            }
            else
            {
                Debug.LogWarning("[MapManager] 발생 가능한 이벤트가 없습니다.");
            }
        }
        else
        {
            // 그 외: 씬 전환
            RunManager.Instance.GoToNodeScene(node, collapseManager.turnCount);
        }
    }

    // ─────────────────────────────────────────────
    // 붕괴 이벤트 핸들러
    // ─────────────────────────────────────────────

    private void OnCollapseWarning(int floor)
    {
        Debug.Log($"[MapManager] ⚠️ UI 경고 연출: {floor}층");

        // 경고 대상 층의 노드들에 경고 이펙트
        foreach (var node in mapData.GetFloorNodes(floor))
        {
            if (nodeUIMap.ContainsKey(node.id))
            {
                nodeUIMap[node.id].PlayWarningPulse();
            }
        }

        if (collapseWarningText != null)
        {
            collapseWarningText.text = $"⚠️ WARNING: {floor}층 붕괴 임박!";
        }
    }

    private void OnFloorCollapsed(int floor)
    {
        Debug.Log($"[MapManager] 💥 UI 붕괴 연출: {floor}층");

        // 붕괴된 층의 노드들에 붕괴 이펙트
        foreach (var node in mapData.GetFloorNodes(floor))
        {
            if (nodeUIMap.ContainsKey(node.id))
            {
                nodeUIMap[node.id].PlayCollapseEffect();
            }
        }

        if (collapseWarningText != null)
        {
            collapseWarningText.text = "";
        }
    }

    private void OnGameOver()
    {
        Debug.Log("[MapManager] 💀 게임 오버 UI 처리");
        // TODO: 게임 오버 UI 오버레이 표시
        if (collapseWarningText != null)
        {
            collapseWarningText.text = "💀 GAME OVER";
            collapseWarningText.fontSize = 32;
        }
    }

    private void OnTurnChanged(int turn, int remaining)
    {
        UpdateInfoPanel();
    }

    // ─────────────────────────────────────────────
    // UI 갱신
    // ─────────────────────────────────────────────

    private void UpdateAllVisuals()
    {
        // 모든 노드 UI 갱신
        foreach (var pair in nodeUIMap)
        {
            pair.Value.UpdateVisual();
        }

        // 연결선 색상 갱신
        if (lineRenderer != null)
        {
            lineRenderer.UpdateLineColors(mapData.currentNode, collapseManager);
        }

        // 기차 위치 갱신
        UpdateTrainPosition();
    }

    private void CreateTrainIndicator()
    {
        GameObject trainObj = new GameObject("TrainIndicator");
        trainObj.transform.SetParent(mapContainer, false);
        trainIndicator = trainObj.AddComponent<RectTransform>();
        
        // 노드와 동일한 앵커/피벗 설정 (왼쪽 중앙 기준)
        trainIndicator.anchorMin = new Vector2(0f, 0.5f);
        trainIndicator.anchorMax = new Vector2(0f, 0.5f);
        trainIndicator.pivot = new Vector2(0.5f, 0.5f);
        
        trainIndicator.sizeDelta = new Vector2(100f, 100f);
        Image trainImg = trainObj.AddComponent<Image>();
        trainImg.sprite = Resources.Load<Sprite>("UI/Train");
        
        if (trainImg.sprite == null) 
        {
            // 임시로 눈에 띄게 노란색 네모 표시 (Train 이미지가 없을 경우)
            trainImg.color = Color.yellow;
            Debug.LogWarning("[MapManager] 기차 이미지(UI/Train)를 찾을 수 없어 노란색 사각형으로 대체합니다.");
        }
        else
        {
            trainImg.color = Color.white;
        }
    }

    private void UpdateTrainPosition()
    {
        if (trainIndicator == null) 
        {
            CreateTrainIndicator();
        }

        if (mapData != null && mapData.currentNode != null && nodePositions.ContainsKey(mapData.currentNode.id))
        {
            Vector2 targetPos = nodePositions[mapData.currentNode.id];
            
            // 위치 업데이트 및 항상 가장 위에 렌더링되게 설정
            trainIndicator.anchoredPosition = targetPos;
            trainIndicator.SetAsLastSibling();
        }
    }

    private void UpdateInfoPanel()
    {
        if (turnInfoText != null)
        {
            int remaining = collapseManager.GetTurnsUntilNextCollapse();
            turnInfoText.text = $"Turn: {collapseManager.turnCount}  |  붕괴까지: {remaining}턴";
        }

        if (floorInfoText != null && mapData.currentNode != null)
        {
            floorInfoText.text = $"Floor: {mapData.currentNode.floor}  |  {mapData.currentNode.nodeType}";
        }

        if (collapseWarningText != null && !collapseManager.isWarning)
        {
            collapseWarningText.text = "";
        }
    }

    // ─────────────────────────────────────────────
    // ScrollView를 현재 노드에 포커스
    // ─────────────────────────────────────────────

    private void ScrollToCurrentNode()
    {
        if (mapData.currentNode == null || !nodePositions.ContainsKey(mapData.currentNode.id))
            return;

        // 현재 노드의 Y 위치를 기반으로 스크롤
        Vector2 pos = nodePositions[mapData.currentNode.id];
        float totalHeight = mapContainer.sizeDelta.y;

        ScrollRect scrollRect = mapContainer.GetComponentInParent<ScrollRect>();
        if (scrollRect != null && totalHeight > 0)
        {
            float normalizedY = pos.y / totalHeight;
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedY);
        }
    }
}