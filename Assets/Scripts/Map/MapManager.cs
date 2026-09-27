using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TheLastArk.Map.Events;

/// <summary>
/// 맵 씬의 메인 매니저.
/// 맵 생성, UI 배치, 플레이어 이동, 붕괴 시스템을 총괄합니다.
/// 씬에 Canvas와 함께 배치하면 자동으로 맵을 구성합니다.
/// </summary>
[ExecuteAlways]
public class MapManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector 설정
    // ─────────────────────────────────────────────

    [Header("Map Settings")]
    [Tooltip("랜덤 시드 (-1이면 매번 다름)")]
    [SerializeField] private int mapSeed = -1;

    [Tooltip("이 맵에서 등장할 Stage 전용 이벤트 카테고리")]
    [SerializeField, Min(1)] private int eventStage = 1;

    [Header("UI References")]
    [Tooltip("노드가 배치될 부모 RectTransform (Canvas 하위)")]
    [SerializeField] private RectTransform mapContainer;

    [Header("UI Settings")]
    [Tooltip("노드 아이콘 기본 크기 (과도한 크기를 줄여 정돈된 85px 적용)")]
    [SerializeField] private float nodeSize = 85f;
    [Tooltip("가로 방향 노드 간격")]
    [SerializeField] private float mapSpacingX = 300f;   // 층(Floor) 간 X 간격
    [Tooltip("세로 방향 노드 간격 (상하단 바 침범 방지를 위해 안전 범위로 압축)")]
    [SerializeField] private float mapSpacingY = 120f;   // 노드 간 Y 간격

    [Header("Info Panel")]
    [SerializeField] private TMPro.TextMeshProUGUI turnInfoText;
    [SerializeField] private TMPro.TextMeshProUGUI floorInfoText;
    [SerializeField] private TMPro.TextMeshProUGUI collapseWarningText;

    [Header("Node Icon Sprites (Assets/Sprites/Map)")]
    [Tooltip("일반 전투 노드 아이콘 (node_battle.png)")]
    [SerializeField] private Sprite combatNodeSprite;
    [Tooltip("엘리트 노드 아이콘 (node_elite.png)")]
    [SerializeField] private Sprite eliteNodeSprite;
    [Tooltip("이벤트 노드 아이콘 (node_event.png)")]
    [SerializeField] private Sprite eventNodeSprite;
    [Tooltip("휴식/마을 노드 아이콘 (node_rest.png)")]
    [SerializeField] private Sprite restNodeSprite;
    [Tooltip("시작 노드 아이콘")]
    [SerializeField] private Sprite startNodeSprite;
    [Tooltip("보스 노드 아이콘")]
    [SerializeField] private Sprite bossNodeSprite;

    [Header("Editor Preview Settings")]
    [Tooltip("에디터 씬 뷰에서 미리보기 생성 시 사용할 시드")]
    public int previewSeed = 12345;
    [Tooltip("에디터 씬 뷰에서 플레이어 현재 위치로 지정할 층")]
    [Range(1, 15)] public int previewFloor = 1;

    // ─────────────────────────────────────────────
    // 내부 데이터
    // ─────────────────────────────────────────────

    private MapData mapData;
    private TMPro.TMP_FontAsset mainFont;

    private int currentTurn = 0;
    private MapLineRenderer lineRenderer;

    private Dictionary<int, MapNodeUI> nodeUIMap = new Dictionary<int, MapNodeUI>();
    private Dictionary<int, Vector2> nodePositions = new Dictionary<int, Vector2>();

    [HideInInspector] public bool isReadOnly = false;
    [HideInInspector] public Transform customCanvasTransform = null;

    // ─────────────────────────────────────────────
    // 공개 접근자 (MapNodeUI 및 Editor에서 사용)
    // ─────────────────────────────────────────────

    public MapNode GetCurrentNode() => mapData?.currentNode;
    public CollapseManager GetCollapseManager() => null;
    public RectTransform GetMapContainer() => mapContainer;
    public MapData GetMapData() => mapData;

    public Sprite GetNodeSprite(NodeType type)
    {
        switch (type)
        {
            case NodeType.Combat:
                return combatNodeSprite != null ? combatNodeSprite : (Resources.Load<Sprite>("Map/node_battle") ?? Resources.Load<Sprite>("UI/Node_Enemy"));
            case NodeType.Elite:
                return eliteNodeSprite != null ? eliteNodeSprite : (Resources.Load<Sprite>("Map/node_elite") ?? Resources.Load<Sprite>("UI/Node_Elite"));
            case NodeType.Event:
                return eventNodeSprite != null ? eventNodeSprite : (Resources.Load<Sprite>("Map/node_event") ?? Resources.Load<Sprite>("UI/Node_Event"));
            case NodeType.Rest:
                return restNodeSprite != null ? restNodeSprite : (Resources.Load<Sprite>("Map/node_rest") ?? Resources.Load<Sprite>("UI/Node_Rest"));
            case NodeType.Start:
                return startNodeSprite != null ? startNodeSprite : (Resources.Load<Sprite>("Map/node_start") ?? Resources.Load<Sprite>("UI/Node_Start"));
            case NodeType.Boss:
                return bossNodeSprite != null ? bossNodeSprite : Resources.Load<Sprite>("UI/Node_Boss_1");
            default:
                return null;
        }
    }

    public static void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    // ─────────────────────────────────────────────
    // 라이프사이클
    // ─────────────────────────────────────────────

    void Awake()
    {
        // 씬 파일에 이전 120 크기나 이전 간격이 직렬화되어 남아있을 경우 최적값으로 보정
        if (nodeSize > 100f || nodeSize < 60f) nodeSize = 85f;
        if (mapSpacingX < 260f || mapSpacingX > 340f) mapSpacingX = 300f;
        if (mapSpacingY > 140f || mapSpacingY < 80f) mapSpacingY = 120f;

        if (!Application.isPlaying)
        {
            AutoBindSceneReferences();
        }
    }

    void OnValidate()
    {
        if (nodeSize > 100f || nodeSize < 60f) nodeSize = 85f;
        if (mapSpacingX < 260f || mapSpacingX > 340f) mapSpacingX = 300f;
        if (mapSpacingY > 140f || mapSpacingY < 80f) mapSpacingY = 120f;
    }

    void Start()
    {
        if (Application.isPlaying)
        {
            InitializeMap();
        }
        else
        {
            AutoBindSceneReferences();
        }
    }

    void OnGUI()
    {
        if (!Application.isPlaying || isReadOnly) return;

        // 디버깅용 이벤트 강제 트리거 버튼 (화면 좌측 하단)
        if (GUI.Button(new Rect(10, Screen.height - 45, 110, 30), "Debug: Event"))
        {
            var eventMgr = EventManager.Instance;
            var eventData = eventMgr?.GetRandomEvent(eventStage);
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
    // 씬 객체 자동 연결 (Auto-bind)
    // ─────────────────────────────────────────────

    public bool AutoBindSceneReferences()
    {
        if (mainFont == null)
        {
            mainFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Main_Fonts");
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return false;

        Transform parentTransform = customCanvasTransform != null ? customCanvasTransform : canvas.transform;

        if (mapContainer == null)
        {
            Transform content = parentTransform.Find("MapScrollView/Viewport/MapContent");
            if (content == null)
            {
                var allRects = parentTransform.GetComponentsInChildren<RectTransform>(true);
                foreach (var r in allRects)
                {
                    if (r.name == "MapContent") { content = r; break; }
                }
            }
            if (content != null)
            {
                mapContainer = content.GetComponent<RectTransform>();
            }
        }

        Transform infoPanel = parentTransform.Find("InfoPanel");
        if (infoPanel != null)
        {
            var labels = infoPanel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            foreach (var lbl in labels)
            {
                string txt = lbl.text ?? "";
                string n = lbl.name;
                if (n == "TurnLabel" || txt.StartsWith("Turn"))
                {
                    turnInfoText = lbl;
                }
                else if (n == "FloorLabel" || txt.StartsWith("Floor"))
                {
                    floorInfoText = lbl;
                }
                else if (n == "WarningLabel" || (lbl.color.r > 0.8f && lbl.color.g < 0.5f))
                {
                    collapseWarningText = lbl;
                }
            }
        }

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<MapLineRenderer>();
        }

#if UNITY_EDITOR
        if (combatNodeSprite == null) combatNodeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Map/node_battle.png");
        if (eliteNodeSprite == null) eliteNodeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Map/node_elite.png");
        if (eventNodeSprite == null) eventNodeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Map/node_event.png");
        if (restNodeSprite == null) restNodeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Map/node_rest.png");
        if (startNodeSprite == null || startNodeSprite.name == "Node_Start")
        {
            var loadedStart = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Map/node_start.png");
            if (loadedStart != null) startNodeSprite = loadedStart;
        }
        if (bossNodeSprite == null) bossNodeSprite = Resources.Load<Sprite>("UI/Node_Boss_1");
#endif

        return mapContainer != null;
    }

    // ─────────────────────────────────────────────
    // 에디터 씬 UI 구축 & 미리보기 관리
    // ─────────────────────────────────────────────

    /// <summary>
    /// 에디터 씬 화면에서 인게임 UI 계층 구조 전체를 생성/배치하고 15층 미리보기 맵을 띄웁니다.
    /// </summary>
    public void BuildUIInEditor(bool generatePreview = true)
    {
        mainFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Main_Fonts");

        // 1. Camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.transform.position = new Vector3(0, 0, -10f);
        }

        // 2. EventSystem
        var es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 3. Canvas
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

        Transform parentTransform = customCanvasTransform != null ? customCanvasTransform : canvas.transform;

        // 4. Background (Canvas 직속 자식 최하단에 배치하여 항상 뒤에 그려짐)
        Transform bgTransform = parentTransform.Find("StaticBackground");
        if (bgTransform == null)
        {
            Sprite bgSprite = Resources.Load<Sprite>("Map/MapBackground");
            GameObject bgObj = new GameObject("StaticBackground");
            bgObj.transform.SetParent(parentTransform, false);
            bgObj.transform.SetAsFirstSibling();

            Image bgImage = bgObj.AddComponent<Image>();
            if (bgSprite != null) bgImage.sprite = bgSprite;
            bgImage.preserveAspect = false;
            bgImage.raycastTarget = false;

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
        }

        // 5. Scroll View & Viewport & MapContent
        Transform scrollTransform = parentTransform.Find("MapScrollView");
        if (scrollTransform == null)
        {
            GameObject scrollObj = new GameObject("MapScrollView");
            scrollObj.transform.SetParent(parentTransform, false);
            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0, 0, 0, 0);

            RectTransform scrollRectTransform = scrollObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(0, 60);
            scrollRectTransform.offsetMax = new Vector2(0, -90);

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

            // Content
            GameObject contentObj = new GameObject("MapContent");
            contentObj.transform.SetParent(viewportObj.transform, false);
            mapContainer = contentObj.AddComponent<RectTransform>();
            mapContainer.anchorMin = new Vector2(0f, 0.5f);
            mapContainer.anchorMax = new Vector2(0f, 0.5f);
            mapContainer.pivot = new Vector2(0f, 0.5f);

            scrollRect.content = mapContainer;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }
        else
        {
            Transform content = scrollTransform.Find("Viewport/MapContent");
            if (content != null) mapContainer = content.GetComponent<RectTransform>();
        }

        // 6. InfoPanel
        Transform infoTransform = parentTransform.Find("InfoPanel");
        if (infoTransform == null)
        {
            CreateInfoPanel(parentTransform);
        }

        // 7. ResourcePanel (상단 바)
        Transform resTransform = parentTransform.Find("ResourcePanel");
        if (resTransform == null)
        {
            var resourceUI = gameObject.GetComponent<TheLastArk.UI.ExplorationResourceUI>();
            if (resourceUI == null) resourceUI = gameObject.AddComponent<TheLastArk.UI.ExplorationResourceUI>();
            resourceUI.Initialize(parentTransform);
        }

        // 8. Buttons
        if (parentTransform.Find("TrainManageButton") == null)
        {
            CreateTrainManagementButton(parentTransform);
        }
        if (parentTransform.Find("DebugConsumableButton") == null)
        {
            CreateDebugConsumableButton(parentTransform);
        }
        if (parentTransform.Find("DebugRelicButton") == null)
        {
            CreateDebugRelicButton(parentTransform);
        }

        // 9. LineRenderer 컴포넌트
        lineRenderer = GetComponent<MapLineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<MapLineRenderer>();
        }
        if (mapContainer != null)
        {
            lineRenderer.Initialize(mapContainer);
        }

        AutoBindSceneReferences();

        // 10. 미리보기 맵 생성
        if (generatePreview)
        {
            GeneratePreviewMap(previewSeed);
        }
    }

    /// <summary>
    /// 에디터 씬 뷰에서 15층 미리보기 맵을 생성하여 노드, 선, 기차 아이콘을 실시간으로 확인합니다.
    /// </summary>
    public void GeneratePreviewMap(int seed = -1)
    {
        if (mapContainer == null)
        {
            AutoBindSceneReferences();
            if (mapContainer == null)
            {
                Debug.LogWarning("[MapManager] mapContainer가 없어 미리보기를 생성할 수 없습니다. BuildUIInEditor()를 먼저 실행하세요.");
                return;
            }
        }

        ClearPreviewNodes();

        if (seed < 0) seed = previewSeed;
        if (seed < 0) seed = 12345;

        mainFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Main_Fonts");

        if (lineRenderer == null) lineRenderer = GetComponent<MapLineRenderer>();
        if (lineRenderer == null) lineRenderer = gameObject.AddComponent<MapLineRenderer>();
        lineRenderer.Initialize(mapContainer);

        mapData = MapGenerator.Generate(seed);
        if (mapData != null)
        {
            int floor = Mathf.Clamp(previewFloor, 1, mapData.totalFloors);
            var floorNodes = mapData.GetFloorNodes(floor);
            mapData.currentNode = floorNodes.Count > 0 ? floorNodes[0] : mapData.GetFloorNodes(1)[0];
            mapData.currentNode.isVisited = true;
            mapData.currentNodeId = mapData.currentNode.id;

            LayoutNodes();
            DrawConnections();
            UpdateAllVisuals();
            UpdateInfoPanel();
            ScrollToCurrentNode();
            Debug.Log($"[MapManager] 에디터 미리보기 맵 생성 완료 (시드: {seed}, 현재 위치: {mapData.currentNode.floor}층)");
        }
    }

    /// <summary>
    /// 생성된 미리보기 노드 및 연결선을 씬에서 정리합니다.
    /// </summary>
    public void ClearPreviewNodes()
    {
        if (mapContainer != null)
        {
            // 1. LineContainer 내부의 모든 연결선 완전히 제거
            Transform lineCont = mapContainer.Find("LineContainer");
            if (lineCont != null)
            {
                for (int i = lineCont.childCount - 1; i >= 0; i--)
                {
                    Transform lineChild = lineCont.GetChild(i);
                    lineChild.gameObject.SetActive(false);
                    SafeDestroy(lineChild.gameObject);
                }
            }

            // 2. mapContainer 직속 자식 노드 및 잔여 선 제거
            List<GameObject> toDestroy = new List<GameObject>();
            for (int i = 0; i < mapContainer.childCount; i++)
            {
                Transform child = mapContainer.GetChild(i);
                if (child.name.StartsWith("Node_") || child.name == "TrainIndicator" || child.name.StartsWith("Line_"))
                {
                    // 즉시 비활성화하여 같은 프레임에서 이중으로 렌더링되거나 겹쳐 보이는 현상 방지
                    child.gameObject.SetActive(false);
                    toDestroy.Add(child.gameObject);
                }
            }
            foreach (var go in toDestroy)
            {
                SafeDestroy(go);
            }
        }

        nodeUIMap.Clear();
        nodePositions.Clear();

        if (lineRenderer == null) lineRenderer = GetComponent<MapLineRenderer>();
        if (lineRenderer != null)
        {
            if (mapContainer != null) lineRenderer.Initialize(mapContainer);
            lineRenderer.ClearAllLines();
        }
    }

    // ─────────────────────────────────────────────
    // 게임 런타임 초기화
    // ─────────────────────────────────────────────

    private void InitializeMap()
    {
        mainFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Main_Fonts");
        if (mainFont == null)
            Debug.LogWarning("[MapManager] Main_Fonts를 찾을 수 없습니다.");

        AutoBindSceneReferences();

        if (mapContainer == null)
        {
            BuildUIInEditor(false);
        }
        else
        {
            // 에디터 미리보기 노드가 남아있다면 런타임 노드 생성을 위해 정리
            ClearPreviewNodes();
        }

        // RunManager 연동 (전역 데이터 로드)
        RunManager rm = RunManager.Instance;

        // 에디터에서 MapScene 직접 테스트 실행 중 이전 세션의 구버전 맵(최대 3개 노드 맵)이 잔존하는 경우 새 맵으로 갱신
        if (rm.CurrentMap != null && Application.isEditor)
        {
            int maxNodesInSaved = 0;
            for (int f = 1; f <= rm.CurrentMap.totalFloors; f++)
            {
                maxNodesInSaved = Mathf.Max(maxNodesInSaved, rm.CurrentMap.GetFloorNodes(f).Count);
            }
            if (maxNodesInSaved <= 3)
            {
                Debug.Log("[MapManager] 이전 세션의 구버전(최대 3개) 맵 데이터가 감지되어 새로운 3~5개 노드 맵으로 재생성합니다.");
                rm.CurrentMap = null;
                rm.CurrentNode = null;
            }
        }

        if (rm.CurrentMap != null)
        {
            // 기존 맵 데이터 복원
            mapData = rm.CurrentMap;
            currentTurn = rm.CurrentTurn;

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

            Debug.Log($"[MapManager] RunManager에서 맵 데이터를 복원했습니다. 현재 층: {mapData.currentNode.floor}, 턴: {currentTurn}");
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
            currentTurn = 0;

            Debug.Log("[MapManager] 새 맵을 생성했습니다.");
        }

        // 라인 렌더러 초기화
        if (lineRenderer == null) lineRenderer = GetComponent<MapLineRenderer>();
        if (lineRenderer == null) lineRenderer = gameObject.AddComponent<MapLineRenderer>();
        lineRenderer.Initialize(mapContainer);

        // UI 배치
        LayoutNodes();
        DrawConnections();
        UpdateAllVisuals();
        UpdateInfoPanel();
        ScrollToCurrentNode();

        // 자원 UI 및 디버그 버튼 생성 (읽기 전용 모드에서는 생략)
        if (!isReadOnly)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Transform parentTransform = customCanvasTransform != null ? customCanvasTransform : canvas.transform;
                {
                    var resourceUI = gameObject.GetComponent<TheLastArk.UI.ExplorationResourceUI>();
                    if (resourceUI == null) resourceUI = gameObject.AddComponent<TheLastArk.UI.ExplorationResourceUI>();
                    resourceUI.Initialize(parentTransform);
                }
                
                if (parentTransform.Find("TrainManageButton") == null)
                {
                    CreateTrainManagementButton(parentTransform);
                }

                if (parentTransform.Find("DebugConsumableButton") == null)
                {
                    CreateDebugConsumableButton(parentTransform);
                }
                if (parentTransform.Find("DebugRelicButton") == null)
                {
                    CreateDebugRelicButton(parentTransform);
                }
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
    private void SetupDefaultUI()
    {
        BuildUIInEditor(false);
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
        turnInfoText.gameObject.name = "TurnLabel";

        // 층 정보
        floorInfoText = CreateInfoLabel(panelObj.transform, "Floor: 1");
        floorInfoText.gameObject.name = "FloorLabel";

        // 경고 텍스트
        collapseWarningText = CreateInfoLabel(panelObj.transform, "");
        collapseWarningText.gameObject.name = "WarningLabel";
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
        float totalWidth = (mapData.totalFloors) * mapSpacingX + 600f; // 양옆 여백 추가
        float maxHeight = MapGenerator.MAX_NODES_PER_FLOOR * mapSpacingY + 600f; // 위아래 여백 추가

        // Content 크기 설정 (화면보다 훨씬 크게)
        mapContainer.sizeDelta = new Vector2(totalWidth, maxHeight);

        // 상하 안전 범위: 상하단 기어 바 및 UI 패널과의 충돌을 원천 방지 (최대 ±240px)
        const float maxSafeY = 240f;

        for (int floor = 1; floor <= mapData.totalFloors; floor++)
        {
            List<MapNode> floorNodes = mapData.GetFloorNodes(floor);
            int count = floorNodes.Count;

            // X 좌표: 왼쪽(1층)에서 오른쪽(15층)으로
            float x = (floor - 1) * mapSpacingX + 300f;

            // Y 간격: 노드 수가 많을 경우 안전 범위 내에 쏙 들어가도록 비례 압축
            float effectiveSpacingY = count > 1 ? Mathf.Min(mapSpacingY, (maxSafeY * 2f) / (count - 1)) : 0f;
            float totalHeight = (count - 1) * effectiveSpacingY;
            float startY = -totalHeight / 2f;

            for (int i = 0; i < count; i++)
            {
                MapNode node = floorNodes[i];
                float y = startY + i * effectiveSpacingY;

                // 랜덤 오프셋 추가 (1층과 15층 제외) - 상하단 바 침범 방지를 위해 Y 흔들림을 최소화
                if (floor > 1 && floor < mapData.totalFloors)
                {
                    x += Random.Range(-35f, 35f);
                    y += Random.Range(-8f, 8f);
                }

                // 상하단 바를 절대 뚫지 못하도록 Y 위치를 안전 경계 안으로 클램핑
                y = Mathf.Clamp(y, -maxSafeY, maxSafeY);

                Vector2 position = new Vector2(x, y);

                MapNodeUI nodeUI = CreateNodeUI(node, position);
                nodeUIMap[node.id] = nodeUI;
                nodePositions[node.id] = position;

                // X를 원래 값으로 복원 (다음 노드를 위해)
                x = (floor - 1) * mapSpacingX + 300f;
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

        // [핵심 개선] 루트 오브젝트에 넉넉한 85x85 완전 투명 히트박스 Image 추가
        // 아이콘 내부 투명도나 비율과 무관하게 노드 영역 전체가 마우스 클릭을 100% 수신
        Image hitBoxImage = nodeObj.AddComponent<Image>();
        hitBoxImage.color = new Color(0f, 0f, 0f, 0f);
        hitBoxImage.raycastTarget = true;

        Button button = nodeObj.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = hitBoxImage;
        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;

        // 글로우 이펙트 (현재 위치 주변에 은은하게 퍼지는 푸른색 원형 아우라)
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(nodeObj.transform, false);
        RectTransform glowRect = glowObj.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-15, -15);
        glowRect.offsetMax = new Vector2(15, 15);
        Image glowImage = glowObj.AddComponent<Image>();
        glowImage.sprite = MapNodeUI.GetOrCreateCircleGlowSprite();
        glowImage.color = new Color(0.20f, 0.70f, 1.0f, 0.22f);
        glowImage.raycastTarget = false;
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
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false; // 부모 hitBoxImage가 단일하게 이벤트를 수신하도록 false

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
        warningImage.raycastTarget = false;
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
        label.raycastTarget = false; // 클릭 이벤트 가로챔 방지
        if (mainFont != null) label.font = mainFont;
        label.color = Color.white;

        MapNodeUI nodeUI = nodeObj.AddComponent<MapNodeUI>();

        // MapNodeUI의 필드들 직접 할당
        nodeUI.nodeIcon = iconImage;
        nodeUI.glowEffect = glowImage;
        nodeUI.warningEffect = warningImage;
        nodeUI.floorLabel = label;

        nodeUI.Setup(node, this);

        return nodeUI;
    }

    /// <summary>
    /// 현재 위치 노드를 클릭했을 때 호출됩니다.
    /// 이동 가능한 다음 층 노드들을 펄스 강조하여 유저를 시각적으로 유도합니다.
    /// </summary>
    public void NotifyCurrentNodeClicked()
    {
        if (mapData == null || mapData.currentNode == null) return;

        var accessible = mapData.GetAccessibleNodes();
        foreach (var n in accessible)
        {
            if (nodeUIMap.TryGetValue(n.id, out var ui) && ui != null)
            {
                ui.TriggerPulseHighlight();
            }
        }
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
        float nodeRadius = nodeSize * 0.5f;

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
                    nodePositions[connected.id],
                    nodeRadius
                );
            }
        }
    }

    // ─────────────────────────────────────────────
    // 플레이어 이동
    // ─────────────────────────────────────────────

    /// <summary>
    /// MapNodeUI에서 노드 클릭 시 호출됩니다.
    /// 바로 다음 층(floor + 1)으로만 전진 이동이 가능합니다.
    /// </summary>
    public void OnNodeSelected(MapNode targetNode)
    {
        if (isReadOnly || targetNode == null) return;

        // 이동 가능 여부 확인 (오직 바로 다음 층으로만 전진 가능)
        if (!targetNode.IsAccessibleFrom(mapData.currentNode))
        {
            Debug.Log($"[MapManager] 이동 불가: {targetNode} (다음 층으로만 전진 가능)");
            return;
        }

        // 이동 실행
        Debug.Log($"[MapManager] 이동: {mapData.currentNode.floor}층 → {targetNode.floor}층 ({targetNode.nodeType})");

        mapData.currentNode = targetNode;
        mapData.currentNodeId = targetNode.id;
        targetNode.isVisited = true;
        currentTurn++;

        if (RunManager.Instance != null)
        {
            RunManager.Instance.CurrentTurn = currentTurn;
        }

        // 기차 내구도 감소
        if (TheLastArk.Managers.TrainManager.Instance != null)
        {
            TheLastArk.Managers.TrainManager.Instance.DecreaseDurability(1);
        }

        // UI 갱신
        UpdateAllVisuals();
        UpdateInfoPanel();

        HandleNodeArrival(targetNode);
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
            var eventData = eventMgr.GetRandomEvent(eventStage);
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
            RunManager.Instance.GoToNodeScene(node, currentTurn);
        }
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
            lineRenderer.UpdateLineColors(mapData.currentNode);
        }
    }

    private void UpdateInfoPanel()
    {
        if (turnInfoText != null)
        {
            turnInfoText.text = $"Turn: {currentTurn}";
        }

        if (floorInfoText != null && mapData != null && mapData.currentNode != null)
        {
            floorInfoText.text = $"Floor: {mapData.currentNode.floor}  |  {mapData.currentNode.nodeType}";
        }

        if (collapseWarningText != null)
        {
            collapseWarningText.text = "";
        }
    }

    // ─────────────────────────────────────────────
    // ScrollView를 현재 노드에 포커스
    // ─────────────────────────────────────────────

    public void ScrollToCurrentNode()
    {
        if (mapData == null || mapData.currentNode == null || !nodePositions.ContainsKey(mapData.currentNode.id) || mapContainer == null)
            return;

        // 현재 노드의 X 위치를 기반으로 가로 스크롤 정렬
        Vector2 pos = nodePositions[mapData.currentNode.id];
        ScrollRect scrollRect = mapContainer.GetComponentInParent<ScrollRect>();
        if (scrollRect != null && mapContainer.sizeDelta.x > 0)
        {
            float viewportWidth = scrollRect.viewport != null ? scrollRect.viewport.rect.width : 1920f;
            float scrollableWidth = mapContainer.sizeDelta.x - viewportWidth;
            if (scrollableWidth > 0)
            {
                float targetScrollX = pos.x - (viewportWidth / 2f);
                scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(targetScrollX / scrollableWidth);
            }
        }
    }
}
