using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using TheLastArk.Data;

namespace TheLastArk.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Title & Background Sprites")]
        [Tooltip("타이틀 로고 이미지 (미지정 시 Resources/UI/TitleLogo 혹은 기본 TextMeshPro 타이틀 사용)")]
        public Sprite titleSprite;
        [Tooltip("메인 메뉴 배경 이미지 (미지정 시 Resources/UI/MainMenuBackground 혹은 기본 배경색 사용)")]
        public Sprite backgroundSprite;

        [Header("Parallax Settings (마우스 연동 패럴랙스)")]
        [Tooltip("마우스 위치에 따른 배경 패럴랙스 이동 효과 사용 여부")]
        public bool enableParallax = true;
        [Tooltip("배경 이미지 확대 비율")]
        public float backgroundScale = 1.45f;
        [Tooltip("배경 기본 X 오프셋 (음수 값일수록 왼쪽으로 이동)")]
        public float backgroundOffsetX = -180f;
        [Tooltip("마우스 이동에 따른 배경 최대 이동 반응 거리(px)")]
        public float parallaxStrength = 45f;
        [Tooltip("배경 이동의 부드러움 (Lerp 속도)")]
        public float parallaxSmoothness = 8f;

        [Header("UI References")]
        public Image backgroundImageComponent;
        public Image titleImageComponent;
        public TextMeshProUGUI titleTextComponent;
        public GameObject titlePanel;
        public GameObject characterSelectionPanel;
        public Transform characterListContainer;
        public Transform buttonContainer;
        public GameObject notificationModal;
        public TextMeshProUGUI notificationTextComponent;

        [Header("Character Selection - Top Train Bar")]
        [Tooltip("상단 기차 커스텀/배치 이미지")]
        public Sprite trainCustomizationSprite;
        public Image trainCustomizationImageComponent;

        [Header("Character Selection - Right Detail UI")]
        public Image detailPortraitImageComponent;
        public TextMeshProUGUI detailNameTextComponent;
        public TextMeshProUGUI detailJobTextComponent;
        public TextMeshProUGUI detailStatsTextComponent;
        public Button detailStartButton;

        [Header("Character Selection - Relic Gacha UI")]
        public Image relicPreviewImageComponent;
        public TextMeshProUGUI relicDescTextComponent;
        public Button relicGachaButton;

        [Header("Prefabs/Settings")]
        public TMPro.TMP_FontAsset mainFont;

        private RectTransform bgRectTransform;
        private CharacterData selectedCharacterData;
        private RelicData selectedStartingRelic;
        private Dictionary<CharacterData, Outline> cardOutlines = new Dictionary<CharacterData, Outline>();

        private void Awake()
        {
            ShowTitlePanel();
        }

        private void Start()
        {
            if (mainFont == null)
            {
                mainFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Main_Fonts");
            }

            // UI가 인스펙터에 미할당되어 있거나 씬에 새로 구성해야 할 경우
            if (titlePanel == null || characterSelectionPanel == null)
            {
                CreateUI();
            }

            // 스프라이트 런타임 자동 로드 시도
            TryAutoLoadAssets();

            // 시작 시 무조건 메인 타이틀 화면만 켜기
            ShowTitlePanel();
        }

        private void Update()
        {
            HandleParallaxEffect();
        }

        private void HandleParallaxEffect()
        {
            if (!enableParallax || bgRectTransform == null) return;
            if (titlePanel == null || !titlePanel.activeSelf) return;

            // 마우스 위치 (0 ~ 1 정규화)
            float mouseX = Mathf.Clamp01(Input.mousePosition.x / Screen.width);
            float mouseY = Mathf.Clamp01(Input.mousePosition.y / Screen.height);

            // 화면 중앙(0.5, 0.5) 기준 오프셋 (-0.5 ~ +0.5)
            float offsetX = mouseX - 0.5f;
            float offsetY = mouseY - 0.5f;

            // 입체감을 부여하는 역방향 타겟 위치 계산 (기본 X 오프셋 반영)
            Vector2 targetPosition = new Vector2(backgroundOffsetX - offsetX * parallaxStrength, -offsetY * parallaxStrength);

            // 부드러운 보간 이동 (Lerp)
            bgRectTransform.anchoredPosition = Vector2.Lerp(
                bgRectTransform.anchoredPosition,
                targetPosition,
                Time.deltaTime * parallaxSmoothness
            );
        }

        private void TryAutoLoadAssets()
        {
            if (titleSprite == null)
            {
                titleSprite = Resources.Load<Sprite>("UI/TitleLogo");
                if (titleSprite == null) titleSprite = Resources.Load<Sprite>("TitleLogo");
            }

            if (backgroundSprite == null)
            {
                backgroundSprite = Resources.Load<Sprite>("UI/MainMenuBackground");
                if (backgroundSprite == null) backgroundSprite = Resources.Load<Sprite>("Backgrounds/MainMenuBackground");
            }

            ApplyVisualAssets();
        }

        public void ApplyVisualAssets()
        {
            // 배경 및 패럴랙스 트랜스폼 적용 (확대 & 왼쪽 이동)
            if (backgroundImageComponent != null)
            {
                bgRectTransform = backgroundImageComponent.rectTransform;
                if (bgRectTransform != null)
                {
                    bgRectTransform.localScale = new Vector3(backgroundScale, backgroundScale, 1f);
                    bgRectTransform.anchoredPosition = new Vector2(backgroundOffsetX, 0);
                }

                if (backgroundSprite != null)
                {
                    backgroundImageComponent.sprite = backgroundSprite;
                    backgroundImageComponent.color = Color.white;
                }
                else
                {
                    backgroundImageComponent.sprite = null;
                    backgroundImageComponent.color = new Color(0.07f, 0.08f, 0.12f, 1f);
                }
            }

            // 타이틀 적용 (이미지 vs 텍스트)
            if (titleImageComponent != null && titleTextComponent != null)
            {
                if (titleSprite != null)
                {
                    titleImageComponent.sprite = titleSprite;
                    titleImageComponent.gameObject.SetActive(true);
                    titleTextComponent.gameObject.SetActive(false);
                }
                else
                {
                    titleImageComponent.gameObject.SetActive(false);
                    titleTextComponent.gameObject.SetActive(true);
                }
            }
        }

        private void CreateUI()
        {
            // 1. 카메라 확인 및 생성
            if (Camera.main == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                Camera cam = camObj.AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
                camObj.AddComponent<AudioListener>();
            }

            // 2. EventSystem 확인 및 생성
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 중복 생성 방지: 기존 씬에 동일 이름의 패널이 남아있으면 정리
            CleanExistingPanels(canvas.transform);

            // 3. Title Panel
            titlePanel = new GameObject("TitlePanel");
            titlePanel.transform.SetParent(canvas.transform, false);
            RectTransform titlePanelRect = titlePanel.AddComponent<RectTransform>();
            titlePanelRect.anchorMin = Vector2.zero;
            titlePanelRect.anchorMax = Vector2.one;
            titlePanelRect.sizeDelta = Vector2.zero;

            // Background Image Object
            GameObject bgObj = new GameObject("BackgroundImage");
            bgObj.transform.SetParent(titlePanel.transform, false);
            bgRectTransform = bgObj.AddComponent<RectTransform>();
            bgRectTransform.anchorMin = Vector2.zero;
            bgRectTransform.anchorMax = Vector2.one;
            bgRectTransform.pivot = new Vector2(0.5f, 0.5f);
            bgRectTransform.anchoredPosition = new Vector2(backgroundOffsetX, 0);
            bgRectTransform.sizeDelta = Vector2.zero;
            bgRectTransform.localScale = new Vector3(backgroundScale, backgroundScale, 1f);

            backgroundImageComponent = bgObj.AddComponent<Image>();
            backgroundImageComponent.color = new Color(0.07f, 0.08f, 0.12f, 1f);
            backgroundImageComponent.raycastTarget = false;

            // Title Area (상단 중앙 - 1600x440)
            GameObject titleAreaObj = new GameObject("TitleArea");
            titleAreaObj.transform.SetParent(titlePanel.transform, false);
            RectTransform titleAreaRect = titleAreaObj.AddComponent<RectTransform>();
            titleAreaRect.anchorMin = new Vector2(0.5f, 0.72f);
            titleAreaRect.anchorMax = new Vector2(0.5f, 0.72f);
            titleAreaRect.pivot = new Vector2(0.5f, 0.5f);
            titleAreaRect.anchoredPosition = Vector2.zero;
            titleAreaRect.sizeDelta = new Vector2(1600, 440);

            // Title Image
            GameObject titleImgObj = new GameObject("TitleImage");
            titleImgObj.transform.SetParent(titleAreaObj.transform, false);
            RectTransform titleImgRect = titleImgObj.AddComponent<RectTransform>();
            titleImgRect.anchorMin = Vector2.zero;
            titleImgRect.anchorMax = Vector2.one;
            titleImgRect.sizeDelta = Vector2.zero;
            titleImageComponent = titleImgObj.AddComponent<Image>();
            titleImageComponent.preserveAspect = true;
            titleImageComponent.raycastTarget = false;
            titleImgObj.SetActive(false);

            // Title Text (Fallback)
            GameObject titleTextObj = new GameObject("TitleText");
            titleTextObj.transform.SetParent(titleAreaObj.transform, false);
            RectTransform titleTextRect = titleTextObj.AddComponent<RectTransform>();
            titleTextRect.anchorMin = Vector2.zero;
            titleTextRect.anchorMax = Vector2.one;
            titleTextRect.sizeDelta = Vector2.zero;
            titleTextComponent = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleTextComponent.text = "<color=#E2B056>THE LAST ARK</color>\n<size=42%><color=#A8B2C1>마지막 방주를 향하여</color></size>";
            titleTextComponent.fontSize = 180;
            titleTextComponent.alignment = TextAlignmentOptions.Center;
            titleTextComponent.enableWordWrapping = false;
            titleTextComponent.raycastTarget = false;
            if (mainFont != null) titleTextComponent.font = mainFont;

            // 4. Buttons Container (하단 세로 정렬 - 0.22f)
            GameObject btnContainerObj = new GameObject("ButtonContainer");
            btnContainerObj.transform.SetParent(titlePanel.transform, false);
            buttonContainer = btnContainerObj.transform;

            RectTransform btnContainerRect = btnContainerObj.AddComponent<RectTransform>();
            btnContainerRect.anchorMin = new Vector2(0.5f, 0.22f);
            btnContainerRect.anchorMax = new Vector2(0.5f, 0.22f);
            btnContainerRect.pivot = new Vector2(0.5f, 0.5f);
            btnContainerRect.anchoredPosition = Vector2.zero;
            btnContainerRect.sizeDelta = new Vector2(400, 320);

            VerticalLayoutGroup vgroup = btnContainerObj.AddComponent<VerticalLayoutGroup>();
            vgroup.spacing = 10;
            vgroup.childAlignment = TextAnchor.UpperCenter;
            vgroup.childControlWidth = true;
            vgroup.childControlHeight = false;
            vgroup.childForceExpandWidth = true;
            vgroup.childForceExpandHeight = false;

            // 메인 메뉴 버튼 5종
            CreateMenuButton(btnContainerObj.transform, "새 게임", OnNewGameClicked, new Color(0.2f, 0.45f, 0.35f, 1f));
            CreateMenuButton(btnContainerObj.transform, "불러오기", OnLoadGameClicked, new Color(0.2f, 0.25f, 0.35f, 1f));
            CreateMenuButton(btnContainerObj.transform, "특전", OnPerksClicked, new Color(0.28f, 0.22f, 0.35f, 1f));
            CreateMenuButton(btnContainerObj.transform, "설정", OnSettingsClicked, new Color(0.22f, 0.25f, 0.3f, 1f));
            CreateMenuButton(btnContainerObj.transform, "게임 종료", OnQuitGameClicked, new Color(0.35f, 0.18f, 0.18f, 1f));

            // 5. Notification Modal Popup
            CreateNotificationModal(canvas.transform);

            // 6. Character Selection Panel
            CreateCharacterSelectionPanel(canvas.transform);
        }

        private void CleanExistingPanels(Transform canvasTransform)
        {
            foreach (string panelName in new string[] { "TitlePanel", "CharacterSelectionPanel", "NotificationModal" })
            {
                Transform existing = canvasTransform.Find(panelName);
                if (existing != null)
                {
                    Destroy(existing.gameObject);
                }
            }
        }

        private GameObject CreateMenuButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, Color baseAccentColor)
        {
            GameObject btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 52);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.08f, 0.1f, 0.14f, 0.45f);
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.08f, 0.1f, 0.14f, 0.45f);
            colors.highlightedColor = new Color(baseAccentColor.r, baseAccentColor.g, baseAccentColor.b, 0.75f);
            colors.pressedColor = new Color(0.04f, 0.05f, 0.08f, 0.85f);
            colors.selectedColor = new Color(baseAccentColor.r, baseAccentColor.g, baseAccentColor.b, 0.75f);
            colors.disabledColor = new Color(0.05f, 0.05f, 0.05f, 0.25f);
            btn.colors = colors;

            btn.onClick.AddListener(onClick);

            // Outline Frame
            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(baseAccentColor.r, baseAccentColor.g, baseAccentColor.b, 0.45f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            // Button Label Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 1f, 1f, 0.95f);
            tmp.raycastTarget = false; // 클릭 방해 제거
            if (mainFont != null) tmp.font = mainFont;

            return btnObj;
        }

        private void CreateNotificationModal(Transform parent)
        {
            notificationModal = new GameObject("NotificationModal");
            notificationModal.transform.SetParent(parent, false);

            RectTransform modalRect = notificationModal.AddComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.sizeDelta = Vector2.zero;

            Image bg = notificationModal.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.65f);
            bg.raycastTarget = true;

            // Box Panel
            GameObject boxObj = new GameObject("Box");
            boxObj.transform.SetParent(notificationModal.transform, false);
            RectTransform boxRect = boxObj.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(500, 240);

            Image boxImg = boxObj.AddComponent<Image>();
            boxImg.color = new Color(0.12f, 0.14f, 0.18f, 0.98f);
            boxImg.raycastTarget = true;

            Outline boxOutline = boxObj.AddComponent<Outline>();
            boxOutline.effectColor = new Color(0.7f, 0.6f, 0.3f, 0.8f);
            boxOutline.effectDistance = new Vector2(2, -2);

            // Text
            GameObject msgObj = new GameObject("MessageText");
            msgObj.transform.SetParent(boxObj.transform, false);
            RectTransform msgRect = msgObj.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.05f, 0.35f);
            msgRect.anchorMax = new Vector2(0.95f, 0.9f);
            msgRect.offsetMin = Vector2.zero;
            msgRect.offsetMax = Vector2.zero;

            notificationTextComponent = msgObj.AddComponent<TextMeshProUGUI>();
            notificationTextComponent.text = "알림 내용";
            notificationTextComponent.fontSize = 24;
            notificationTextComponent.alignment = TextAlignmentOptions.Center;
            notificationTextComponent.color = Color.white;
            notificationTextComponent.raycastTarget = false;
            if (mainFont != null) notificationTextComponent.font = mainFont;

            // OK Button
            GameObject okBtnObj = new GameObject("OKButton");
            okBtnObj.transform.SetParent(boxObj.transform, false);
            RectTransform okRect = okBtnObj.AddComponent<RectTransform>();
            okRect.anchorMin = new Vector2(0.5f, 0.1f);
            okRect.anchorMax = new Vector2(0.5f, 0.1f);
            okRect.pivot = new Vector2(0.5f, 0f);
            okRect.sizeDelta = new Vector2(160, 48);

            Image okImg = okBtnObj.AddComponent<Image>();
            okImg.color = new Color(0.25f, 0.35f, 0.45f, 1f);
            okImg.raycastTarget = true;

            Button okBtn = okBtnObj.AddComponent<Button>();
            okBtn.onClick.AddListener(() => notificationModal.SetActive(false));

            GameObject okTextObj = new GameObject("Text");
            okTextObj.transform.SetParent(okBtnObj.transform, false);
            RectTransform okTextRect = okTextObj.AddComponent<RectTransform>();
            okTextRect.anchorMin = Vector2.zero;
            okTextRect.anchorMax = Vector2.one;
            okTextRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI okText = okTextObj.AddComponent<TextMeshProUGUI>();
            okText.text = "확인";
            okText.fontSize = 22;
            okText.alignment = TextAlignmentOptions.Center;
            okText.color = Color.white;
            okText.raycastTarget = false;
            if (mainFont != null) okText.font = mainFont;

            notificationModal.SetActive(false);
        }

        private void CreateCharacterSelectionPanel(Transform parent)
        {
            characterSelectionPanel = new GameObject("CharacterSelectionPanel");
            characterSelectionPanel.transform.SetParent(parent, false);
            RectTransform charSelRect = characterSelectionPanel.AddComponent<RectTransform>();
            charSelRect.anchorMin = Vector2.zero;
            charSelRect.anchorMax = Vector2.one;
            charSelRect.sizeDelta = Vector2.zero;

            Image charSelBg = characterSelectionPanel.AddComponent<Image>();
            charSelBg.color = new Color(0.08f, 0.09f, 0.12f, 1f);
            charSelBg.raycastTarget = true;

            // ==========================================
            // 1. TOP: Train Customization Header Bar (기차 커스텀)
            // ==========================================
            GameObject trainHeaderObj = new GameObject("TrainCustomizationHeader");
            trainHeaderObj.transform.SetParent(characterSelectionPanel.transform, false);
            RectTransform trainHeaderRect = trainHeaderObj.AddComponent<RectTransform>();
            trainHeaderRect.anchorMin = new Vector2(0.04f, 0.84f);
            trainHeaderRect.anchorMax = new Vector2(0.96f, 0.96f);
            trainHeaderRect.offsetMin = Vector2.zero;
            trainHeaderRect.offsetMax = Vector2.zero;

            Image trainHeaderBg = trainHeaderObj.AddComponent<Image>();
            trainHeaderBg.color = new Color(0.12f, 0.15f, 0.2f, 0.95f);
            trainHeaderBg.raycastTarget = false;

            Outline trainHeaderOutline = trainHeaderObj.AddComponent<Outline>();
            trainHeaderOutline.effectColor = new Color(0.7f, 0.6f, 0.3f, 0.6f);
            trainHeaderOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Train Bar Title Label
            GameObject trainTitleObj = new GameObject("TitleText");
            trainTitleObj.transform.SetParent(trainHeaderObj.transform, false);
            RectTransform trainTitleRect = trainTitleObj.AddComponent<RectTransform>();
            trainTitleRect.anchorMin = new Vector2(0.02f, 0.5f);
            trainTitleRect.anchorMax = new Vector2(0.28f, 0.5f);
            trainTitleRect.pivot = new Vector2(0f, 0.5f);
            trainTitleRect.anchoredPosition = Vector2.zero;
            trainTitleRect.sizeDelta = new Vector2(300, 40);

            TextMeshProUGUI trainTitleText = trainTitleObj.AddComponent<TextMeshProUGUI>();
            trainTitleText.text = "🚆 방주 열차 편성 & 커스텀";
            trainTitleText.fontSize = 22;
            trainTitleText.fontStyle = FontStyles.Bold;
            trainTitleText.color = new Color(0.95f, 0.8f, 0.3f, 1f);
            trainTitleText.raycastTarget = false;
            if (mainFont != null) trainTitleText.font = mainFont;

            // Train Sprite Image Container
            GameObject trainImgObj = new GameObject("TrainImage");
            trainImgObj.transform.SetParent(trainHeaderObj.transform, false);
            RectTransform trainImgRect = trainImgObj.AddComponent<RectTransform>();
            trainImgRect.anchorMin = new Vector2(0.28f, 0.1f);
            trainImgRect.anchorMax = new Vector2(0.98f, 0.9f);
            trainImgRect.offsetMin = Vector2.zero;
            trainImgRect.offsetMax = Vector2.zero;

            trainCustomizationImageComponent = trainImgObj.AddComponent<Image>();
            trainCustomizationImageComponent.preserveAspect = true;
            trainCustomizationImageComponent.raycastTarget = false;
            if (trainCustomizationSprite != null)
            {
                trainCustomizationImageComponent.sprite = trainCustomizationSprite;
                trainCustomizationImageComponent.color = Color.white;
            }
            else
            {
                trainCustomizationImageComponent.color = new Color(0.18f, 0.22f, 0.28f, 0.8f);
                CreateTrainCarSlotsPreview(trainImgObj.transform);
            }

            // ==========================================
            // 2. LEFT-CENTER: Compact Character Grid (캐릭터 선택)
            // ==========================================
            GameObject charGridPanel = new GameObject("CharacterGridPanel");
            charGridPanel.transform.SetParent(characterSelectionPanel.transform, false);
            RectTransform charGridPanelRect = charGridPanel.AddComponent<RectTransform>();
            charGridPanelRect.anchorMin = new Vector2(0.04f, 0.12f);
            charGridPanelRect.anchorMax = new Vector2(0.55f, 0.82f);
            charGridPanelRect.offsetMin = Vector2.zero;
            charGridPanelRect.offsetMax = Vector2.zero;

            Image charGridBg = charGridPanel.AddComponent<Image>();
            charGridBg.color = new Color(0.1f, 0.12f, 0.16f, 0.95f);
            charGridBg.raycastTarget = false;

            Outline charGridOutline = charGridPanel.AddComponent<Outline>();
            charGridOutline.effectColor = new Color(0.3f, 0.35f, 0.45f, 0.5f);

            // Section Header Text
            GameObject gridHeaderObj = new GameObject("GridHeader");
            gridHeaderObj.transform.SetParent(charGridPanel.transform, false);
            RectTransform gridHeaderRect = gridHeaderObj.AddComponent<RectTransform>();
            gridHeaderRect.anchorMin = new Vector2(0.03f, 0.92f);
            gridHeaderRect.anchorMax = new Vector2(0.97f, 0.98f);
            gridHeaderRect.offsetMin = Vector2.zero;
            gridHeaderRect.offsetMax = Vector2.zero;

            TextMeshProUGUI gridHeaderText = gridHeaderObj.AddComponent<TextMeshProUGUI>();
            gridHeaderText.text = "📋 승무원 명단 (리더 선택)";
            gridHeaderText.fontSize = 22;
            gridHeaderText.fontStyle = FontStyles.Bold;
            gridHeaderText.color = new Color(0.9f, 0.92f, 0.96f, 1f);
            gridHeaderText.raycastTarget = false;
            if (mainFont != null) gridHeaderText.font = mainFont;

            // Grid Container
            GameObject gridArea = new GameObject("GridArea");
            gridArea.transform.SetParent(charGridPanel.transform, false);
            RectTransform gridRect = gridArea.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.03f, 0.03f);
            gridRect.anchorMax = new Vector2(0.97f, 0.91f);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;

            GridLayoutGroup gridLayout = gridArea.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(130, 155);
            gridLayout.spacing = new Vector2(16, 16);
            gridLayout.padding = new RectOffset(12, 12, 12, 12);
            gridLayout.childAlignment = TextAnchor.UpperLeft;

            characterListContainer = gridArea.transform;

            // ==========================================
            // 3. RIGHT-TOP: Character Details Panel (우측 상단 상세 정보)
            // ==========================================
            GameObject detailPanel = new GameObject("CharacterDetailPanel");
            detailPanel.transform.SetParent(characterSelectionPanel.transform, false);
            RectTransform detailPanelRect = detailPanel.AddComponent<RectTransform>();
            detailPanelRect.anchorMin = new Vector2(0.57f, 0.46f);
            detailPanelRect.anchorMax = new Vector2(0.96f, 0.82f);
            detailPanelRect.offsetMin = Vector2.zero;
            detailPanelRect.offsetMax = Vector2.zero;

            Image detailBg = detailPanel.AddComponent<Image>();
            detailBg.color = new Color(0.11f, 0.13f, 0.18f, 0.95f);
            detailBg.raycastTarget = false;

            Outline detailOutline = detailPanel.AddComponent<Outline>();
            detailOutline.effectColor = new Color(0.35f, 0.4f, 0.55f, 0.6f);
            detailOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Large Portrait (왼쪽)
            GameObject detailPortObj = new GameObject("DetailPortrait");
            detailPortObj.transform.SetParent(detailPanel.transform, false);
            RectTransform detailPortRect = detailPortObj.AddComponent<RectTransform>();
            detailPortRect.anchorMin = new Vector2(0.04f, 0.28f);
            detailPortRect.anchorMax = new Vector2(0.38f, 0.92f);
            detailPortRect.offsetMin = Vector2.zero;
            detailPortRect.offsetMax = Vector2.zero;

            Image detailPortBg = detailPortObj.AddComponent<Image>();
            detailPortBg.color = new Color(0.16f, 0.2f, 0.26f, 0.9f);
            detailPortBg.raycastTarget = false;

            GameObject detailPortImgObj = new GameObject("DetailPortraitImage");
            detailPortImgObj.transform.SetParent(detailPortObj.transform, false);
            RectTransform dPortImgRect = detailPortImgObj.AddComponent<RectTransform>();
            dPortImgRect.anchorMin = Vector2.zero;
            dPortImgRect.anchorMax = Vector2.one;
            dPortImgRect.sizeDelta = Vector2.zero;

            detailPortraitImageComponent = detailPortImgObj.AddComponent<Image>();
            detailPortraitImageComponent.preserveAspect = true;
            detailPortraitImageComponent.raycastTarget = false;

            // Right Side Info Container
            GameObject infoContainer = new GameObject("InfoContainer");
            infoContainer.transform.SetParent(detailPanel.transform, false);
            RectTransform infoRect = infoContainer.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.40f, 0.28f);
            infoRect.anchorMax = new Vector2(0.96f, 0.94f);
            infoRect.offsetMin = Vector2.zero;
            infoRect.offsetMax = Vector2.zero;

            // Name Text
            GameObject nameObj = new GameObject("DetailNameText");
            nameObj.transform.SetParent(infoContainer.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.78f);
            nameRect.anchorMax = new Vector2(1, 1f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            detailNameTextComponent = nameObj.AddComponent<TextMeshProUGUI>();
            detailNameTextComponent.text = "선택된 캐릭터";
            detailNameTextComponent.fontSize = 30;
            detailNameTextComponent.fontStyle = FontStyles.Bold;
            detailNameTextComponent.color = new Color(0.95f, 0.82f, 0.35f, 1f);
            detailNameTextComponent.raycastTarget = false;
            if (mainFont != null) detailNameTextComponent.font = mainFont;

            // Job / Role Text
            GameObject jobObj = new GameObject("DetailJobText");
            jobObj.transform.SetParent(infoContainer.transform, false);
            RectTransform jobRect = jobObj.AddComponent<RectTransform>();
            jobRect.anchorMin = new Vector2(0, 0.62f);
            jobRect.anchorMax = new Vector2(1, 0.78f);
            jobRect.offsetMin = Vector2.zero;
            jobRect.offsetMax = Vector2.zero;

            detailJobTextComponent = jobObj.AddComponent<TextMeshProUGUI>();
            detailJobTextComponent.text = "승무원 계급 / 역할";
            detailJobTextComponent.fontSize = 20;
            detailJobTextComponent.color = new Color(0.7f, 0.85f, 0.95f, 1f);
            detailJobTextComponent.raycastTarget = false;
            if (mainFont != null) detailJobTextComponent.font = mainFont;

            // Detailed Stats Text
            GameObject statsObj = new GameObject("DetailStatsText");
            statsObj.transform.SetParent(infoContainer.transform, false);
            RectTransform statsRect = statsObj.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 0f);
            statsRect.anchorMax = new Vector2(1, 0.60f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;

            detailStatsTextComponent = statsObj.AddComponent<TextMeshProUGUI>();
            detailStatsTextComponent.text = "능력치 예시";
            detailStatsTextComponent.fontSize = 20;
            detailStatsTextComponent.lineSpacing = 6;
            detailStatsTextComponent.color = new Color(0.9f, 0.92f, 0.95f, 1f);
            detailStatsTextComponent.raycastTarget = false;
            if (mainFont != null) detailStatsTextComponent.font = mainFont;

            // "여정 시작" Button (우측 하단)
            GameObject startBtnObj = new GameObject("ConfirmStartButton");
            startBtnObj.transform.SetParent(detailPanel.transform, false);
            RectTransform startBtnRect = startBtnObj.AddComponent<RectTransform>();
            startBtnRect.anchorMin = new Vector2(0.55f, 0.05f);
            startBtnRect.anchorMax = new Vector2(0.96f, 0.23f);
            startBtnRect.offsetMin = Vector2.zero;
            startBtnRect.offsetMax = Vector2.zero;

            Image startBtnImg = startBtnObj.AddComponent<Image>();
            startBtnImg.color = new Color(0.18f, 0.48f, 0.32f, 1f);
            startBtnImg.raycastTarget = true;

            detailStartButton = startBtnObj.AddComponent<Button>();
            ColorBlock sColors = detailStartButton.colors;
            sColors.normalColor = new Color(0.18f, 0.48f, 0.32f, 1f);
            sColors.highlightedColor = new Color(0.25f, 0.65f, 0.42f, 1f);
            sColors.pressedColor = new Color(0.12f, 0.35f, 0.22f, 1f);
            detailStartButton.colors = sColors;
            detailStartButton.onClick.AddListener(OnStartRunConfirmed);

            Outline sOutline = startBtnObj.AddComponent<Outline>();
            sOutline.effectColor = new Color(0.4f, 0.85f, 0.55f, 0.7f);
            sOutline.effectDistance = new Vector2(1.2f, -1.2f);

            GameObject startTextObj = new GameObject("Text");
            startTextObj.transform.SetParent(startBtnObj.transform, false);
            RectTransform startTextRect = startTextObj.AddComponent<RectTransform>();
            startTextRect.anchorMin = Vector2.zero;
            startTextRect.anchorMax = Vector2.one;
            startTextRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI startText = startTextObj.AddComponent<TextMeshProUGUI>();
            startText.text = "🚀 여정 시작";
            startText.fontSize = 24;
            startText.fontStyle = FontStyles.Bold;
            startText.alignment = TextAlignmentOptions.Center;
            startText.color = Color.white;
            startText.raycastTarget = false;
            if (mainFont != null) startText.font = mainFont;

            // ==========================================
            // 4. RIGHT-BOTTOM: Relic Gacha / Perks Area (우측 하단 유물 가챠)
            // ==========================================
            GameObject relicPanel = new GameObject("RelicGachaPanel");
            relicPanel.transform.SetParent(characterSelectionPanel.transform, false);
            RectTransform relicPanelRect = relicPanel.AddComponent<RectTransform>();
            relicPanelRect.anchorMin = new Vector2(0.57f, 0.12f);
            relicPanelRect.anchorMax = new Vector2(0.96f, 0.44f);
            relicPanelRect.offsetMin = Vector2.zero;
            relicPanelRect.offsetMax = Vector2.zero;

            Image relicBg = relicPanel.AddComponent<Image>();
            relicBg.color = new Color(0.14f, 0.12f, 0.2f, 0.95f);
            relicBg.raycastTarget = false;

            Outline relicOutline = relicPanel.AddComponent<Outline>();
            relicOutline.effectColor = new Color(0.6f, 0.45f, 0.75f, 0.6f);
            relicOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Relic Title
            GameObject relicTitleObj = new GameObject("RelicTitle");
            relicTitleObj.transform.SetParent(relicPanel.transform, false);
            RectTransform relicTitleRect = relicTitleObj.AddComponent<RectTransform>();
            relicTitleRect.anchorMin = new Vector2(0.04f, 0.82f);
            relicTitleRect.anchorMax = new Vector2(0.96f, 0.96f);
            relicTitleRect.offsetMin = Vector2.zero;
            relicTitleRect.offsetMax = Vector2.zero;

            TextMeshProUGUI relicTitleText = relicTitleObj.AddComponent<TextMeshProUGUI>();
            relicTitleText.text = "🔮 시작 유물 & 특전 소환";
            relicTitleText.fontSize = 22;
            relicTitleText.fontStyle = FontStyles.Bold;
            relicTitleText.color = new Color(0.85f, 0.72f, 0.95f, 1f);
            relicTitleText.raycastTarget = false;
            if (mainFont != null) relicTitleText.font = mainFont;

            // Relic Preview Frame
            GameObject relicSlotObj = new GameObject("RelicSlot");
            relicSlotObj.transform.SetParent(relicPanel.transform, false);
            RectTransform relicSlotRect = relicSlotObj.AddComponent<RectTransform>();
            relicSlotRect.anchorMin = new Vector2(0.06f, 0.22f);
            relicSlotRect.anchorMax = new Vector2(0.24f, 0.75f);
            relicSlotRect.offsetMin = Vector2.zero;
            relicSlotRect.offsetMax = Vector2.zero;

            Image relicSlotBg = relicSlotObj.AddComponent<Image>();
            relicSlotBg.color = new Color(0.2f, 0.16f, 0.28f, 0.9f);
            relicSlotBg.raycastTarget = false;

            Outline slotOutline = relicSlotObj.AddComponent<Outline>();
            slotOutline.effectColor = new Color(0.7f, 0.5f, 0.85f, 0.7f);

            GameObject relicPreviewImgObj = new GameObject("RelicPreviewImage");
            relicPreviewImgObj.transform.SetParent(relicSlotObj.transform, false);
            RectTransform rImgRect = relicPreviewImgObj.AddComponent<RectTransform>();
            rImgRect.anchorMin = Vector2.zero;
            rImgRect.anchorMax = Vector2.one;
            rImgRect.sizeDelta = Vector2.zero;

            relicPreviewImageComponent = relicPreviewImgObj.AddComponent<Image>();
            relicPreviewImageComponent.preserveAspect = true;
            relicPreviewImageComponent.raycastTarget = false;

            // Relic Gacha Description Area
            GameObject relicDescObj = new GameObject("RelicDesc");
            relicDescObj.transform.SetParent(relicPanel.transform, false);
            RectTransform relicDescRect = relicDescObj.AddComponent<RectTransform>();
            relicDescRect.anchorMin = new Vector2(0.28f, 0.45f);
            relicDescRect.anchorMax = new Vector2(0.96f, 0.78f);
            relicDescRect.offsetMin = Vector2.zero;
            relicDescRect.offsetMax = Vector2.zero;

            relicDescTextComponent = relicDescObj.AddComponent<TextMeshProUGUI>();
            relicDescTextComponent.text = "탐험 시작 시 지급되는 무작위 보너스 유물을 뽑습니다.";
            relicDescTextComponent.fontSize = 18;
            relicDescTextComponent.color = new Color(0.82f, 0.8f, 0.88f, 1f);
            relicDescTextComponent.raycastTarget = false;
            if (mainFont != null) relicDescTextComponent.font = mainFont;

            // Gacha Button (클릭 가능하도록 레이캐스트 보장)
            GameObject gachaBtnObj = new GameObject("RelicGachaButton");
            gachaBtnObj.transform.SetParent(relicPanel.transform, false);
            RectTransform gachaBtnRect = gachaBtnObj.AddComponent<RectTransform>();
            gachaBtnRect.anchorMin = new Vector2(0.28f, 0.15f);
            gachaBtnRect.anchorMax = new Vector2(0.96f, 0.42f);
            gachaBtnRect.offsetMin = Vector2.zero;
            gachaBtnRect.offsetMax = Vector2.zero;

            Image gachaBtnImg = gachaBtnObj.AddComponent<Image>();
            gachaBtnImg.color = new Color(0.32f, 0.22f, 0.45f, 1f);
            gachaBtnImg.raycastTarget = true;

            relicGachaButton = gachaBtnObj.AddComponent<Button>();
            ColorBlock gColors = relicGachaButton.colors;
            gColors.normalColor = new Color(0.32f, 0.22f, 0.45f, 1f);
            gColors.highlightedColor = new Color(0.48f, 0.32f, 0.65f, 1f);
            gColors.pressedColor = new Color(0.2f, 0.14f, 0.3f, 1f);
            relicGachaButton.colors = gColors;
            relicGachaButton.onClick.AddListener(OnRelicGachaClicked);

            Outline gachaOutline = gachaBtnObj.AddComponent<Outline>();
            gachaOutline.effectColor = new Color(0.75f, 0.55f, 0.95f, 0.8f);

            GameObject gachaTextObj = new GameObject("Text");
            gachaTextObj.transform.SetParent(gachaBtnObj.transform, false);
            RectTransform gachaTextRect = gachaTextObj.AddComponent<RectTransform>();
            gachaTextRect.anchorMin = Vector2.zero;
            gachaTextRect.anchorMax = Vector2.one;
            gachaTextRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI gachaText = gachaTextObj.AddComponent<TextMeshProUGUI>();
            gachaText.text = "🔮 특전 유물 뽑기";
            gachaText.fontSize = 20;
            gachaText.alignment = TextAlignmentOptions.Center;
            gachaText.color = Color.white;
            gachaText.raycastTarget = false;
            if (mainFont != null) gachaText.font = mainFont;

            // ==========================================
            // 5. BOTTOM-LEFT: Back Button (뒤로 가기)
            // ==========================================
            GameObject backBtnObj = new GameObject("BackButton");
            backBtnObj.transform.SetParent(characterSelectionPanel.transform, false);
            RectTransform backBtnRect = backBtnObj.AddComponent<RectTransform>();
            backBtnRect.anchorMin = new Vector2(0.04f, 0.04f);
            backBtnRect.anchorMax = new Vector2(0.04f, 0.04f);
            backBtnRect.pivot = new Vector2(0f, 0f);
            backBtnRect.anchoredPosition = Vector2.zero;
            backBtnRect.sizeDelta = new Vector2(160, 48);

            Image backImg = backBtnObj.AddComponent<Image>();
            backImg.color = new Color(0.16f, 0.18f, 0.24f, 1f);
            backImg.raycastTarget = true;

            Button backBtn = backBtnObj.AddComponent<Button>();
            backBtn.onClick.AddListener(ShowTitlePanel);

            GameObject backTextObj = new GameObject("Text");
            backTextObj.transform.SetParent(backBtnObj.transform, false);
            RectTransform backTextRect = backTextObj.AddComponent<RectTransform>();
            backTextRect.anchorMin = Vector2.zero;
            backTextRect.anchorMax = Vector2.one;
            backTextRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI backText = backTextObj.AddComponent<TextMeshProUGUI>();
            backText.text = "← 메인 화면";
            backText.fontSize = 22;
            backText.alignment = TextAlignmentOptions.Center;
            backText.color = Color.white;
            backText.raycastTarget = false;
            if (mainFont != null) backText.font = mainFont;

            // 생성 직후 캐릭터 선택창 비활성화
            characterSelectionPanel.SetActive(false);
        }

        private void CreateTrainCarSlotsPreview(Transform parent)
        {
            HorizontalLayoutGroup hgroup = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
            hgroup.spacing = 14;
            hgroup.padding = new RectOffset(10, 10, 6, 6);
            hgroup.childAlignment = TextAnchor.MiddleCenter;
            hgroup.childControlWidth = true;
            hgroup.childControlHeight = true;

            string[] carNames = { "01. 넥서스 칸 (엔진)", "02. 승무원실 (거주구)", "03. 선택 칸 #1 (미건설/확장)", "04. 선택 칸 #2 (미건설/확장)" };
            foreach (var carName in carNames)
            {
                string nameCopy = carName;
                GameObject carSlot = new GameObject($"CarSlot_{nameCopy}");
                carSlot.transform.SetParent(parent, false);

                Image img = carSlot.AddComponent<Image>();
                img.color = new Color(0.14f, 0.18f, 0.25f, 0.9f);
                img.raycastTarget = true;

                Button btn = carSlot.AddComponent<Button>();
                ColorBlock cb = btn.colors;
                cb.normalColor = new Color(0.14f, 0.18f, 0.25f, 0.9f);
                cb.highlightedColor = new Color(0.28f, 0.38f, 0.55f, 1f);
                cb.pressedColor = new Color(0.1f, 0.12f, 0.18f, 1f);
                btn.colors = cb;
                btn.onClick.AddListener(() => OnTrainCarClicked(nameCopy));

                Outline outline = carSlot.AddComponent<Outline>();
                outline.effectColor = new Color(0.4f, 0.5f, 0.65f, 0.5f);

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(carSlot.transform, false);
                RectTransform tRect = textObj.AddComponent<RectTransform>();
                tRect.anchorMin = Vector2.zero;
                tRect.anchorMax = Vector2.one;
                tRect.sizeDelta = Vector2.zero;

                TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
                tmp.text = nameCopy;
                tmp.fontSize = 17;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.85f, 0.9f, 0.95f, 0.9f);
                tmp.raycastTarget = false;
                if (mainFont != null) tmp.font = mainFont;
            }
        }

        public void ShowTitlePanel()
        {
            if (titlePanel != null) titlePanel.SetActive(true);
            if (characterSelectionPanel != null) characterSelectionPanel.SetActive(false);
            if (notificationModal != null) notificationModal.SetActive(false);
        }

        public void ShowNotification(string message)
        {
            if (notificationModal != null && notificationTextComponent != null)
            {
                notificationTextComponent.text = message;
                notificationModal.SetActive(true);
                notificationModal.transform.SetAsLastSibling();
            }
        }

        // ─── Button Click Handlers ───

        private void OnNewGameClicked()
        {
            if (titlePanel != null) titlePanel.SetActive(false);
            if (characterSelectionPanel != null)
            {
                characterSelectionPanel.SetActive(true);
                characterSelectionPanel.transform.SetAsLastSibling();
                PopulateCharacterList();
            }
        }

        private void OnLoadGameClicked()
        {
            ShowNotification("저장된 게임 기록이 없습니다.\n'새 게임'을 눌러 여정을 시작하세요!");
        }

        private void OnPerksClicked()
        {
            ShowNotification("특전 & 특성 해금 시스템은\n현재 준비 중입니다.");
        }

        private void OnSettingsClicked()
        {
            SettingsPopupUI.Show();
        }

        private void OnQuitGameClicked()
        {
            Debug.Log("[MainMenuUI] Game Quit Requested.");
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void OnTrainCarClicked(string carName)
        {
            ShowNotification($"🚆 {carName}\n\n[열차 파츠 커스텀]\n해당 기차 칸의 모듈과 무장을 커스텀할 수 있습니다.\n(기차 커스텀 그래픽 바인딩 준비 완료)");
        }

        private void PopulateCharacterList()
        {
            if (characterListContainer == null) return;

            foreach (Transform child in characterListContainer)
            {
                Destroy(child.gameObject);
            }

            cardOutlines.Clear();
            selectedCharacterData = null;

            CharacterData[] allCharacters = Resources.LoadAll<CharacterData>("Characters");

            if (allCharacters == null || allCharacters.Length == 0)
            {
                Debug.LogWarning("[MainMenuUI] No CharacterData found in Resources/Characters!");
                return;
            }

            CharacterData firstValidChar = null;
            foreach (var charData in allCharacters)
            {
                if (charData == null || charData.isEnemy) continue;
                CreateCharacterCard(charData);
                if (firstValidChar == null) firstValidChar = charData;
            }

            // 첫 번째 캐릭터 자동 선택
            if (firstValidChar != null)
            {
                SelectCharacter(firstValidChar);
            }
        }

        private void CreateCharacterCard(CharacterData data)
        {
            GameObject cardObj = new GameObject($"Card_{data.DataId}");
            cardObj.transform.SetParent(characterListContainer, false);
            RectTransform rect = cardObj.AddComponent<RectTransform>();

            Image bg = cardObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.18f, 0.24f, 0.95f);
            bg.raycastTarget = true;

            Outline cardOutline = cardObj.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.3f, 0.35f, 0.45f, 0.5f);
            cardOutline.effectDistance = new Vector2(1.5f, -1.5f);
            cardOutlines[data] = cardOutline;

            Button btn = cardObj.AddComponent<Button>();
            btn.onClick.AddListener(() => OnCharacterCardClicked(data));

            VerticalLayoutGroup vLayout = cardObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(6, 6, 6, 6);
            vLayout.spacing = 4;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;

            // Compact Portrait (100x100 고정 레이아웃)
            GameObject portraitObj = new GameObject("Portrait");
            portraitObj.transform.SetParent(cardObj.transform, false);
            RectTransform pRect = portraitObj.AddComponent<RectTransform>();
            pRect.sizeDelta = new Vector2(100, 100);

            LayoutElement pLayout = portraitObj.AddComponent<LayoutElement>();
            pLayout.preferredWidth = 100;
            pLayout.preferredHeight = 100;
            pLayout.minWidth = 100;
            pLayout.minHeight = 100;

            Image pImg = portraitObj.AddComponent<Image>();
            Sprite charSprite = data.portraitSprite != null ? data.portraitSprite : data.standingSprite;
            pImg.sprite = charSprite;
            pImg.color = charSprite != null ? Color.white : new Color(0.2f, 0.25f, 0.35f, 0.8f);
            pImg.preserveAspect = true;
            pImg.raycastTarget = false;

            // Name text
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(cardObj.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(110, 30);

            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = data.DisplayName;
            nameText.fontSize = 18;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            nameText.raycastTarget = false;
            if (mainFont != null) nameText.font = mainFont;
        }

        private void OnCharacterCardClicked(CharacterData data)
        {
            SelectCharacter(data);
        }

        private void SelectCharacter(CharacterData data)
        {
            if (data == null) return;
            selectedCharacterData = data;

            // Update Outlines
            foreach (var kvp in cardOutlines)
            {
                if (kvp.Value == null) continue;
                if (kvp.Key == data)
                {
                    kvp.Value.effectColor = new Color(0.95f, 0.78f, 0.25f, 1f); // Glowing Gold for Selected
                    kvp.Value.effectDistance = new Vector2(2.5f, -2.5f);
                }
                else
                {
                    kvp.Value.effectColor = new Color(0.3f, 0.35f, 0.45f, 0.5f);
                    kvp.Value.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }

            // Update Detail Panel (우측 상단)
            if (detailPortraitImageComponent != null)
            {
                Sprite charSprite = data.portraitSprite != null ? data.portraitSprite : data.standingSprite;
                detailPortraitImageComponent.sprite = charSprite;
                detailPortraitImageComponent.color = charSprite != null ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }

            if (detailNameTextComponent != null)
            {
                detailNameTextComponent.text = data.DisplayName;
            }

            if (detailJobTextComponent != null)
            {
                string role = string.IsNullOrEmpty(data.jobName) ? "방주 승무원" : data.jobName;
                detailJobTextComponent.text = $"<color=#7FFFD4>{role}</color>";
            }

            if (detailStatsTextComponent != null)
            {
                detailStatsTextComponent.text = 
                    $"<b><color=#E2B056>[기본 능력치]</color></b>\n" +
                    $"• 최대 체력: <color=#7FFFD4>{data.maxHp}</color>   • 정신력: <color=#7B68EE>{data.maxMental}</color>\n" +
                    $"• 기본 공격: <color=#FF6B6B>{data.baseAttack}</color>   • 주문력: <color=#4ECDC4>{data.spellPower}</color>\n" +
                    $"• 물리 방어: <color=#FFE66D>{data.armor}</color>   • 마법 저항: <color=#95E1D3>{data.magicResist}</color>";
            }
        }

        private void OnStartRunConfirmed()
        {
            if (selectedCharacterData == null)
            {
                ShowNotification("출발할 승무원을 먼저 선택해주세요!");
                return;
            }

            Debug.Log($"[MainMenuUI] Starting run with selected leader: {selectedCharacterData.DisplayName} ({selectedCharacterData.DataId})");

            if (RunManager.Instance != null)
            {
                RunManager.Instance.StartNewRun();
                RunManager.Instance.AddPartyMember(selectedCharacterData);

                // 시작 특전 유물이 선택되어 있다면 등록
                if (selectedStartingRelic != null)
                {
                    RunManager.Instance.State.relicIDs.Add(selectedStartingRelic.relicID);
                    Debug.Log($"[MainMenuUI] Starting relic added: {selectedStartingRelic.relicName} ({selectedStartingRelic.relicID})");
                }

                if (TheLastArk.Managers.ResourceManager.Instance != null)
                {
                    TheLastArk.Managers.ResourceManager.Instance.AddCharacterCard(selectedCharacterData.DataId, 1);
                }

                SceneManager.LoadScene("MapScene");
            }
            else
            {
                Debug.LogError("[MainMenuUI] RunManager.Instance is null!");
            }
        }

        private void OnRelicGachaClicked()
        {
            RelicData[] allRelics = Resources.LoadAll<RelicData>("Relics");
            if (allRelics == null || allRelics.Length == 0)
            {
                ShowNotification("🔮 유물 데이터를 찾을 수 없습니다.");
                return;
            }

            // 무작위 유물 가챠 소환
            int randomIndex = Random.Range(0, allRelics.Length);
            selectedStartingRelic = allRelics[randomIndex];

            if (relicPreviewImageComponent != null && selectedStartingRelic.icon != null)
            {
                relicPreviewImageComponent.sprite = selectedStartingRelic.icon;
                relicPreviewImageComponent.color = Color.white;
            }

            if (relicDescTextComponent != null)
            {
                relicDescTextComponent.text = $"<b><color=#FFE66D>[{selectedStartingRelic.relicName}]</color></b>\n<size=85%>{selectedStartingRelic.description}</size>";
            }

            ShowNotification($"🔮 <b>[특전 유물 소환 완료!]</b>\n\n<color=#FFE66D><b>{selectedStartingRelic.relicName}</b></color>\n<size=85%>{selectedStartingRelic.description}</size>\n\n(여정 시작 시 해당 유물을 보유하고 시작합니다)");
        }
    }
}
