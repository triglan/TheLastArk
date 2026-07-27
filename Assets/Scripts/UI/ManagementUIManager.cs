using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TheLastArk.Managers;
using TheLastArk.Data;
using TheLastArk.Character;

namespace TheLastArk.UI
{
    public class ManagementUIManager : MonoBehaviour
    {
        private static ManagementUIManager instance;
        public static ManagementUIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ManagementUIManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("ManagementUIManager");
                        instance = go.AddComponent<ManagementUIManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        public enum TabType
        {
            Train,      // 기차
            Character,  // 캐릭터 & 덱
            Inventory   // 가방 & 유물 & 장비
        }

        private GameObject popupPanel;
        private GameObject settingsPopupPanel;
        private GameObject charDetailPopupPanel;

        private TabType currentTab = TabType.Character;

        // Container References
        private Transform tabButtonsContainer;
        private Transform contentContainer;

        // Detail Popup UI References
        private Image detailStandingImage;
        private TextMeshProUGUI detailTitleText;
        private TextMeshProUGUI detailStatsText;
        private TextMeshProUGUI detailSkillsText;
        private Button detailDeckButton;
        private TextMeshProUGUI detailDeckButtonText;
        private Button detailLeaderButton;
        private TextMeshProUGUI detailLeaderButtonText;

        private CharacterData currentDetailCharacter;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Toggle Management Window with Shortcut Hotkey 'M' or 'C'
            if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.C))
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            if (popupPanel != null && popupPanel.activeSelf)
            {
                Hide();
            }
            else
            {
                Show(currentTab);
            }
        }

        public void Show(TabType initialTab = TabType.Character)
        {
            if (popupPanel == null)
            {
                CreateUI();
            }

            currentTab = initialTab;
            popupPanel.SetActive(true);
            RefreshTab();
        }

        public void Hide()
        {
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }
            if (charDetailPopupPanel != null)
            {
                charDetailPopupPanel.SetActive(false);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 전용 환경설정 팝업 (ShowSettingsPopup)
        // ─────────────────────────────────────────────────────────────
        public void ShowSettingsPopup()
        {
            if (settingsPopupPanel == null)
            {
                CreateSettingsPopupUI();
            }
            settingsPopupPanel.SetActive(true);
        }

        private void CreateSettingsPopupUI()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            settingsPopupPanel = new GameObject("SettingsPopupPanel");
            settingsPopupPanel.transform.SetParent(canvas.transform, false);
            settingsPopupPanel.transform.SetAsLastSibling();

            RectTransform mainRect = settingsPopupPanel.AddComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0.25f, 0.2f);
            mainRect.anchorMax = new Vector2(0.75f, 0.8f);
            mainRect.offsetMin = Vector2.zero;
            mainRect.offsetMax = Vector2.zero;

            Image bg = settingsPopupPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.14f, 0.98f);

            // Red Close Button (X)
            GameObject closeBtnObj = new GameObject("CloseBtn");
            closeBtnObj.transform.SetParent(settingsPopupPanel.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-15, -15);
            closeRect.sizeDelta = new Vector2(45, 45);

            Image cImg = closeBtnObj.AddComponent<Image>();
            cImg.color = new Color(0.85f, 0.2f, 0.2f, 1f);
            Button cBtn = closeBtnObj.AddComponent<Button>();
            cBtn.onClick.AddListener(() => settingsPopupPanel.SetActive(false));
            CreateTextUI(closeBtnObj.transform, "X", 26, Color.white);

            VerticalLayoutGroup layout = settingsPopupPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 40);
            layout.spacing = 20;

            GameObject titleObj = new GameObject("SettingsTitle");
            titleObj.transform.SetParent(settingsPopupPanel.transform, false);
            CreateTextUI(titleObj.transform, "⚙️ 게임 환경 설정", 32, Color.yellow);

            // BGM Volume option
            GameObject bgmObj = new GameObject("BgmSetting");
            bgmObj.transform.SetParent(settingsPopupPanel.transform, false);
            LayoutElement bLe = bgmObj.AddComponent<LayoutElement>();
            bLe.preferredHeight = 50;
            CreateTextUI(bgmObj.transform, "배경음 (BGM) 볼륨: 100%", 24, Color.white);

            // SFX Volume option
            GameObject sfxObj = new GameObject("SfxSetting");
            sfxObj.transform.SetParent(settingsPopupPanel.transform, false);
            LayoutElement sLe = sfxObj.AddComponent<LayoutElement>();
            sLe.preferredHeight = 50;
            CreateTextUI(sfxObj.transform, "효과음 (SFX) 볼륨: 100%", 24, Color.white);

            // Quit Game Button
            GameObject quitBtnObj = new GameObject("QuitBtn");
            quitBtnObj.transform.SetParent(settingsPopupPanel.transform, false);
            LayoutElement qLe = quitBtnObj.AddComponent<LayoutElement>();
            qLe.preferredHeight = 60;
            Image qImg = quitBtnObj.AddComponent<Image>();
            qImg.color = new Color(0.7f, 0.2f, 0.2f, 1f);
            Button qBtn = quitBtnObj.AddComponent<Button>();
            qBtn.onClick.AddListener(() =>
            {
                Debug.Log("[SettingsPopup] Application.Quit()");
                Application.Quit();
            });
            CreateTextUI(quitBtnObj.transform, "🚪 게임 종료", 26, Color.white);

            TMPFontManager.ApplyFontToAll(settingsPopupPanel.transform);
        }

        private void CreateUI()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("ManagementCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 90;
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            popupPanel = new GameObject("ManagementPopupPanel");
            popupPanel.transform.SetParent(canvas.transform, false);
            popupPanel.transform.SetAsLastSibling();

            RectTransform mainRect = popupPanel.AddComponent<RectTransform>();
            mainRect.anchorMin = Vector2.zero;
            mainRect.anchorMax = Vector2.one;
            mainRect.offsetMin = Vector2.zero;
            mainRect.offsetMax = Vector2.zero;

            Image bg = popupPanel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.07f, 0.1f, 0.95f);

            // ── 상단 탭 바 (Top Navigation Bar - 3 Tabs: 기차, 캐릭터, 가방) ──
            GameObject topBar = new GameObject("TopTabBar");
            topBar.transform.SetParent(popupPanel.transform, false);
            RectTransform topBarRect = topBar.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0, 0.9f);
            topBarRect.anchorMax = new Vector2(1, 1f);
            topBarRect.offsetMin = new Vector2(20, 10);
            topBarRect.offsetMax = new Vector2(-20, -10);

            Image topBarBg = topBar.AddComponent<Image>();
            topBarBg.color = new Color(0.12f, 0.15f, 0.2f, 1f);

            HorizontalLayoutGroup topLayout = topBar.AddComponent<HorizontalLayoutGroup>();
            topLayout.padding = new RectOffset(10, 10, 10, 10);
            topLayout.spacing = 15;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;

            tabButtonsContainer = topBar.transform;

            CreateTabButton(topBar.transform, TabType.Train, "🚆 기차 관리");
            CreateTabButton(topBar.transform, TabType.Character, "👤 캐릭터 & 덱");
            CreateTabButton(topBar.transform, TabType.Inventory, "🎒 가방 / 유물 / 장비");

            // 닫기 버튼 (X)
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(topBar.transform, false);
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);
            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            LayoutElement closeLe = closeBtnObj.AddComponent<LayoutElement>();
            closeLe.preferredWidth = 100;
            CreateTextUI(closeBtnObj.transform, "닫기 (X)", 22, Color.white);

            // ── 메인 콘텐츠 영역 (Content Area) ─────────────────────
            GameObject contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(popupPanel.transform, false);
            RectTransform contentRect = contentArea.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 0.9f);
            contentRect.offsetMin = new Vector2(20, 20);
            contentRect.offsetMax = new Vector2(-20, -10);

            contentContainer = contentArea.transform;

            // Character Detail Popup Modal Overlay
            CreateCharDetailPopup(canvas.transform);

            TMPFontManager.ApplyFontToAll(popupPanel.transform);
        }

        private void CreateTabButton(Transform parent, TabType tab, string label)
        {
            GameObject btnObj = new GameObject($"TabBtn_{tab}");
            btnObj.transform.SetParent(parent, false);
            
            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.25f, 0.35f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                currentTab = tab;
                RefreshTab();
            });

            CreateTextUI(btnObj.transform, label, 24, Color.white);
        }

        private void RefreshTab()
        {
            // Clear content container
            foreach (Transform child in contentContainer)
            {
                Destroy(child.gameObject);
            }

            // Update Tab Button Highlight
            foreach (Transform tabBtn in tabButtonsContainer)
            {
                Image img = tabBtn.GetComponent<Image>();
                if (img != null)
                {
                    bool isSelected = tabBtn.name == $"TabBtn_{currentTab}";
                    img.color = isSelected ? new Color(0.25f, 0.5f, 0.8f, 1f) : new Color(0.2f, 0.25f, 0.35f, 1f);
                }
            }

            switch (currentTab)
            {
                case TabType.Train:
                    BuildTrainTab();
                    break;
                case TabType.Character:
                    BuildCharacterTab();
                    break;
                case TabType.Inventory:
                    BuildInventoryTab();
                    break;
            }

            TMPFontManager.ApplyFontToAll(contentContainer);
        }

        // ─────────────────────────────────────────────────────────────
        // 1. 기차 탭 (Train Tab)
        // ─────────────────────────────────────────────────────────────
        private void BuildTrainTab()
        {
            GameObject trainPanel = new GameObject("TrainPanel");
            trainPanel.transform.SetParent(contentContainer, false);
            RectTransform rect = trainPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = trainPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 20;

            // Durability Title & Bar
            GameObject hpObj = new GameObject("DurabilityBar");
            hpObj.transform.SetParent(trainPanel.transform, false);
            LayoutElement hpLe = hpObj.AddComponent<LayoutElement>();
            hpLe.preferredHeight = 60;
            Image hpBg = hpObj.AddComponent<Image>();
            hpBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            float curHp = TrainManager.Instance != null ? TrainManager.Instance.currentTrainDurability : 100f;
            float maxHp = TrainManager.Instance != null ? TrainManager.Instance.maxTrainDurability : 100f;
            CreateTextUI(hpObj.transform, $"🚆 기차 내구도: {curHp} / {maxHp}", 28, Color.yellow);

            // Car list text
            GameObject carTitle = new GameObject("CarTitle");
            carTitle.transform.SetParent(trainPanel.transform, false);
            CreateTextUI(carTitle.transform, "보유 객차 및 모듈 목록", 26, Color.cyan);

            if (TrainManager.Instance != null && TrainManager.Instance.coreCar != null)
            {
                GameObject coreObj = new GameObject("CoreCar");
                coreObj.transform.SetParent(trainPanel.transform, false);
                LayoutElement coreLe = coreObj.AddComponent<LayoutElement>();
                coreLe.preferredHeight = 50;
                Image cBg = coreObj.AddComponent<Image>();
                cBg.color = new Color(0.15f, 0.25f, 0.35f, 1f);
                CreateTextUI(coreObj.transform, $"[엔진] {TrainManager.Instance.coreCar.carName} (Lv.{TrainManager.Instance.coreCar.level})", 22, Color.white);
            }

            if (TrainManager.Instance != null)
            {
                foreach (var car in TrainManager.Instance.additionalCars)
                {
                    GameObject carObj = new GameObject($"Car_{car.carName}");
                    carObj.transform.SetParent(trainPanel.transform, false);
                    LayoutElement le = carObj.AddComponent<LayoutElement>();
                    le.preferredHeight = 50;
                    Image cBg = carObj.AddComponent<Image>();
                    cBg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
                    CreateTextUI(carObj.transform, $"[{car.carType}] {car.carName} (Lv.{car.level})", 20, Color.white);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 2. 캐릭터 & 덱 탭 (Picture 2 디자인 완벽 적용)
        // ─────────────────────────────────────────────────────────────
        private void BuildCharacterTab()
        {
            GameObject charRoot = new GameObject("CharacterRoot");
            charRoot.transform.SetParent(contentContainer, false);
            RectTransform rootRect = charRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup mainLayout = charRoot.AddComponent<VerticalLayoutGroup>();
            mainLayout.padding = new RectOffset(20, 20, 20, 20);
            mainLayout.spacing = 15;

            // Top Header: Active Synergies summary
            GameObject synSummaryObj = new GameObject("SynergySummary");
            synSummaryObj.transform.SetParent(charRoot.transform, false);
            LayoutElement synLe = synSummaryObj.AddComponent<LayoutElement>();
            synLe.preferredHeight = 45;
            Image synBg = synSummaryObj.AddComponent<Image>();
            synBg.color = new Color(0.12f, 0.16f, 0.24f, 1f);

            var activeSynergies = SynergyCalculator.CalculateActiveSynergies();
            string synText = "✨ 활성 시너지: ";
            if (activeSynergies == null || activeSynergies.Count == 0) synText += "없음";
            else
            {
                foreach (var kvp in activeSynergies)
                {
                    synText += $"[{kvp.Key}:{kvp.Value}명] ";
                }
            }
            CreateTextUI(synSummaryObj.transform, synText, 20, Color.cyan);

            // Horizontal ScrollView for Owned Character Cards (Picture 2 카드 뷰)
            GameObject scrollObj = new GameObject("CardsScrollArea");
            scrollObj.transform.SetParent(charRoot.transform, false);
            LayoutElement scrollLe = scrollObj.AddComponent<LayoutElement>();
            scrollLe.preferredHeight = 450;
            scrollLe.flexibleWidth = 1f;

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0.08f, 0.09f, 0.12f, 0.9f);

            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform cRect = content.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 0);
            cRect.anchorMax = new Vector2(0, 1);
            cRect.pivot = new Vector2(0, 0.5f);
            cRect.sizeDelta = new Vector2(0, 0);

            HorizontalLayoutGroup hLayout = content.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(20, 20, 20, 20);
            hLayout.spacing = 20;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRect;
            scrollRect.content = cRect;

            // Load Owned Characters (Card Count >= 1 or in Party)
            CharacterData[] allCharacters = Resources.LoadAll<CharacterData>("Characters");
            var partyDataIDs = RunManager.Instance != null ? RunManager.Instance.State.partyDataIDs : new List<string>();
            string leaderID = RunManager.Instance != null ? RunManager.Instance.State.leaderCharacterID : "";

            int displayedCount = 0;
            foreach (var data in allCharacters)
            {
                if (data == null || data.isEnemy) continue;
                string charId = data.DataId;

                int cardCount = ResourceManager.Instance != null ? ResourceManager.Instance.GetCardCount(charId) : 0;
                bool inParty = partyDataIDs.Contains(charId);

                // 카드 1장 이상 보유 또는 현재 덱에 포함된 경우만 보유 캐릭터로 판정
                if (cardCount >= 1 || inParty)
                {
                    displayedCount++;
                    bool isLeader = (charId == leaderID);
                    CreateCharacterCardUI(content.transform, data, cardCount, inParty, isLeader);
                }
            }

            if (displayedCount == 0)
            {
                CreateTextUI(content.transform, "보유 중인 캐릭터가 없습니다.", 24, Color.gray);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Character Card UI Widget (Picture 2 캐릭터 카드 형태)
        // ─────────────────────────────────────────────────────────────
        private void CreateCharacterCardUI(Transform parent, CharacterData data, int cardCount, bool inParty, bool isLeader)
        {
            string charId = data.DataId;
            int level = ResourceManager.Instance != null ? ResourceManager.Instance.GetCharacterLevelFromCards(cardCount) : 0;
            if (level < 0) level = 0;

            int nextTarget = 3;
            if (level == 1) nextTarget = 6;
            else if (level == 2) nextTarget = 9;
            else if (level == 3) nextTarget = 18;
            else if (level >= 4) nextTarget = cardCount;

            CharacterStatus status = new CharacterStatus(data);

            GameObject cardObj = new GameObject($"Card_{data.DisplayName}");
            cardObj.transform.SetParent(parent, false);

            LayoutElement le = cardObj.AddComponent<LayoutElement>();
            le.preferredWidth = 200;
            le.preferredHeight = 400;

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.12f, 0.14f, 0.18f, 1f);

            // Leader Highlight Gold Border (Picture 2 형태)
            if (isLeader)
            {
                GameObject borderObj = new GameObject("LeaderBorder");
                borderObj.transform.SetParent(cardObj.transform, false);
                RectTransform bRect = borderObj.AddComponent<RectTransform>();
                bRect.anchorMin = Vector2.zero;
                bRect.anchorMax = Vector2.one;
                bRect.offsetMin = new Vector2(-4, -4);
                bRect.offsetMax = new Vector2(4, 4);

                Image bImg = borderObj.AddComponent<Image>();
                bImg.color = new Color(1f, 0.82f, 0.1f, 1f); // Gold
                borderObj.transform.SetAsFirstSibling();
            }

            Button btn = cardObj.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                ShowCharacterDetailPopup(data);
            });

            VerticalLayoutGroup vLayout = cardObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(10, 10, 10, 10);
            vLayout.spacing = 8;
            vLayout.childControlHeight = false;

            // 1. Portrait Image
            GameObject portraitObj = new GameObject("Portrait");
            portraitObj.transform.SetParent(cardObj.transform, false);
            LayoutElement pLe = portraitObj.AddComponent<LayoutElement>();
            pLe.preferredHeight = 160;

            Image pImg = portraitObj.AddComponent<Image>();
            if (data.portraitSprite != null)
            {
                pImg.sprite = data.portraitSprite;
                pImg.preserveAspect = true;
            }
            else
            {
                pImg.color = new Color(0.2f, 0.25f, 0.35f, 1f);
            }

            // 2. Name & Level (Picture 2: [Guardian] Lv.0)
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(cardObj.transform, false);
            LayoutElement nLe = nameObj.AddComponent<LayoutElement>();
            nLe.preferredHeight = 30;
            CreateTextUI(nameObj.transform, $"[{data.DisplayName}] Lv.{level}", 22, Color.green);

            // 3. HP (Picture 2: HP: 400/400)
            GameObject hpObj = new GameObject("HpText");
            hpObj.transform.SetParent(cardObj.transform, false);
            LayoutElement hpLe = hpObj.AddComponent<LayoutElement>();
            hpLe.preferredHeight = 25;
            CreateTextUI(hpObj.transform, $"HP: {status.currentHp}/{status.FinalMaxHp}", 18, new Color(0.3f, 0.9f, 0.3f));

            // 4. Mental (Picture 2: Mental: 300/300)
            GameObject mentalObj = new GameObject("MentalText");
            mentalObj.transform.SetParent(cardObj.transform, false);
            LayoutElement mLe = mentalObj.AddComponent<LayoutElement>();
            mLe.preferredHeight = 25;
            CreateTextUI(mentalObj.transform, $"Mental: {status.currentMental}/{status.FinalMaxMental}", 18, new Color(0.3f, 0.7f, 1f));

            // 5. Cards Progress (Picture 2: Cards: 1/3)
            GameObject cardsObj = new GameObject("CardsText");
            cardsObj.transform.SetParent(cardObj.transform, false);
            LayoutElement cLe = cardsObj.AddComponent<LayoutElement>();
            cLe.preferredHeight = 25;
            string cardsStr = (level >= 4) ? $"Cards: {cardCount} (MAX)" : $"Cards: {cardCount}/{nextTarget}";
            CreateTextUI(cardsObj.transform, cardsStr, 18, Color.white);

            // 6. Leader Label (Picture 2: <Leader>)
            if (isLeader)
            {
                GameObject leaderLabelObj = new GameObject("LeaderLabel");
                leaderLabelObj.transform.SetParent(cardObj.transform, false);
                LayoutElement lLe = leaderLabelObj.AddComponent<LayoutElement>();
                lLe.preferredHeight = 25;
                CreateTextUI(leaderLabelObj.transform, "〈Leader〉", 20, Color.yellow);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Character Detail Popup Modal (Picture 2 팝업 상세창)
        // ─────────────────────────────────────────────────────────────
        private void CreateCharDetailPopup(Transform parent)
        {
            charDetailPopupPanel = new GameObject("CharDetailPopupPanel");
            charDetailPopupPanel.transform.SetParent(parent, false);
            charDetailPopupPanel.transform.SetAsLastSibling();

            RectTransform rect = charDetailPopupPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 0.15f);
            rect.anchorMax = new Vector2(0.85f, 0.85f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = charDetailPopupPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.09f, 0.12f, 0.98f);

            // Red Close Button (X) (Picture 2 형태)
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(charDetailPopupPanel.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-15, -15);
            closeRect.sizeDelta = new Vector2(50, 40);

            Image cImg = closeBtnObj.AddComponent<Image>();
            cImg.color = new Color(0.9f, 0.15f, 0.15f, 1f);

            Button cBtn = closeBtnObj.AddComponent<Button>();
            cBtn.onClick.AddListener(() => charDetailPopupPanel.SetActive(false));
            CreateTextUI(closeBtnObj.transform, "X", 26, Color.white);

            // Left Side: Full Standing Illustration (Picture 2)
            GameObject illObj = new GameObject("Illustration");
            illObj.transform.SetParent(charDetailPopupPanel.transform, false);
            RectTransform illRect = illObj.AddComponent<RectTransform>();
            illRect.anchorMin = new Vector2(0.05f, 0.05f);
            illRect.anchorMax = new Vector2(0.45f, 0.95f);
            illRect.offsetMin = Vector2.zero;
            illRect.offsetMax = Vector2.zero;

            detailStandingImage = illObj.AddComponent<Image>();
            detailStandingImage.preserveAspect = true;

            // Right Side: Stats, Skills, Deck Action Buttons
            GameObject rightArea = new GameObject("RightArea");
            rightArea.transform.SetParent(charDetailPopupPanel.transform, false);
            RectTransform rightRect = rightArea.AddComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.48f, 0.05f);
            rightRect.anchorMax = new Vector2(0.95f, 0.9f);
            rightRect.offsetMin = Vector2.zero;
            rightRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup rLayout = rightArea.AddComponent<VerticalLayoutGroup>();
            rLayout.spacing = 15;
            rLayout.childControlHeight = false;

            // Header Title ([ Guardian ])
            GameObject titleObj = new GameObject("DetailTitleText");
            titleObj.transform.SetParent(rightArea.transform, false);
            LayoutElement tLe = titleObj.AddComponent<LayoutElement>();
            tLe.preferredHeight = 40;
            detailTitleText = CreateTextUI(titleObj.transform, "[ Guardian ]", 32, Color.yellow);

            // Stats Text
            GameObject statsObj = new GameObject("DetailStatsText");
            statsObj.transform.SetParent(rightArea.transform, false);
            LayoutElement sLe = statsObj.AddComponent<LayoutElement>();
            sLe.preferredHeight = 110;
            detailStatsText = CreateTextUI(statsObj.transform, "HP: 400\nMental: 300\nAttack: 25", 26, Color.white);

            // Skills Text
            GameObject skillsObj = new GameObject("DetailSkillsText");
            skillsObj.transform.SetParent(rightArea.transform, false);
            LayoutElement skLe = skillsObj.AddComponent<LayoutElement>();
            skLe.preferredHeight = 140;
            detailSkillsText = CreateTextUI(skillsObj.transform, "-- Skills --", 22, new Color(0.8f, 0.8f, 0.8f));

            // Action Buttons Container (Deck Toggle, Leader Assign)
            GameObject actionsObj = new GameObject("ActionButtons");
            actionsObj.transform.SetParent(rightArea.transform, false);
            LayoutElement actLe = actionsObj.AddComponent<LayoutElement>();
            actLe.preferredHeight = 50;

            HorizontalLayoutGroup actLayout = actionsObj.AddComponent<HorizontalLayoutGroup>();
            actLayout.spacing = 15;

            // Deck Toggle Button
            GameObject deckBtnObj = new GameObject("DeckButton");
            deckBtnObj.transform.SetParent(actionsObj.transform, false);
            LayoutElement dLe = deckBtnObj.AddComponent<LayoutElement>();
            dLe.preferredWidth = 160;
            dLe.preferredHeight = 45;

            Image dImg = deckBtnObj.AddComponent<Image>();
            dImg.color = new Color(0.2f, 0.5f, 0.8f, 1f);
            detailDeckButton = deckBtnObj.AddComponent<Button>();
            detailDeckButtonText = CreateTextUI(deckBtnObj.transform, "⚔️ 덱에 참가", 20, Color.white);

            // Leader Assign Button
            GameObject leaderBtnObj = new GameObject("LeaderButton");
            leaderBtnObj.transform.SetParent(actionsObj.transform, false);
            LayoutElement lLe = leaderBtnObj.AddComponent<LayoutElement>();
            lLe.preferredWidth = 160;
            lLe.preferredHeight = 45;

            Image lImg = leaderBtnObj.AddComponent<Image>();
            lImg.color = new Color(0.8f, 0.6f, 0.1f, 1f);
            detailLeaderButton = leaderBtnObj.AddComponent<Button>();
            detailLeaderButtonText = CreateTextUI(leaderBtnObj.transform, "👑 리더 지정", 20, Color.white);

            charDetailPopupPanel.SetActive(false);
        }

        private void ShowCharacterDetailPopup(CharacterData data)
        {
            if (data == null) return;
            currentDetailCharacter = data;

            string charId = data.DataId;
            var partyIDs = RunManager.Instance != null ? RunManager.Instance.State.partyDataIDs : new List<string>();
            string leaderID = RunManager.Instance != null ? RunManager.Instance.State.leaderCharacterID : "";

            bool inParty = partyIDs.Contains(charId);
            bool isLeader = (charId == leaderID);

            // 1. Standing Sprite Illustration
            if (data.standingSprite != null) detailStandingImage.sprite = data.standingSprite;
            else if (data.portraitSprite != null) detailStandingImage.sprite = data.portraitSprite;
            else detailStandingImage.sprite = null;

            // 2. Title & Stats
            CharacterStatus status = new CharacterStatus(data);
            detailTitleText.text = $"[ {data.DisplayName} ]";
            detailStatsText.text = $"HP: {status.FinalMaxHp}\n" +
                                   $"Mental: {status.FinalMaxMental}\n" +
                                   $"Attack: {status.FinalAttack}";

            // 3. Skills List
            string skillsStr = "<color=cyan>-- Skills --</color>\n";
            if (data.passiveSkill != null && !string.IsNullOrEmpty(data.passiveSkill.skillName))
            {
                skillsStr += $"[Passive] {data.passiveSkill.skillName}\n";
            }
            foreach (var s in data.activeSkills)
            {
                if (s != null && !string.IsNullOrEmpty(s.skillName))
                {
                    skillsStr += $"[Active] {s.skillName} (Cost: {s.baseCost})\n";
                }
            }
            detailSkillsText.text = skillsStr;

            // 4. Deck Toggle Button
            detailDeckButton.onClick.RemoveAllListeners();
            detailDeckButtonText.text = inParty ? "덱에서 제외" : "⚔️ 덱에 참가";
            detailDeckButton.GetComponent<Image>().color = inParty ? new Color(0.8f, 0.3f, 0.3f, 1f) : new Color(0.2f, 0.6f, 0.3f, 1f);
            detailDeckButton.onClick.AddListener(() =>
            {
                if (RunManager.Instance != null)
                {
                    if (inParty)
                    {
                        RunManager.Instance.State.partyDataIDs.Remove(charId);
                        RunManager.Instance.State.partyStatuses.RemoveAll(s => s.origin != null && s.origin.DataId == charId);
                        if (RunManager.Instance.State.leaderCharacterID == charId)
                        {
                            RunManager.Instance.State.leaderCharacterID = RunManager.Instance.State.partyDataIDs.Count > 0 ? RunManager.Instance.State.partyDataIDs[0] : "";
                        }
                    }
                    else
                    {
                        RunManager.Instance.AddPartyMember(data);
                    }
                    ShowCharacterDetailPopup(data);
                    RefreshTab();
                }
            });

            // 5. Leader Button
            detailLeaderButton.onClick.RemoveAllListeners();
            detailLeaderButtonText.text = isLeader ? "👑 현재 리더" : "👑 리더 지정";
            detailLeaderButton.GetComponent<Image>().color = isLeader ? new Color(0.5f, 0.5f, 0.5f, 1f) : new Color(0.9f, 0.65f, 0.1f, 1f);
            detailLeaderButton.interactable = inParty; // 덱 참가 상태일 때만 리더 지정 가능
            detailLeaderButton.onClick.AddListener(() =>
            {
                if (RunManager.Instance != null && inParty)
                {
                    RunManager.Instance.State.leaderCharacterID = charId;
                    ShowCharacterDetailPopup(data);
                    RefreshTab();
                }
            });

            charDetailPopupPanel.SetActive(true);
            TMPFontManager.ApplyFontToAll(charDetailPopupPanel.transform);
        }

        // ─────────────────────────────────────────────────────────────
        // 3. 가방 / 유물 / 장비 탭 (Inventory & Equipment Tab)
        // ─────────────────────────────────────────────────────────────
        private void BuildInventoryTab()
        {
            GameObject invRoot = new GameObject("InventoryRoot");
            invRoot.transform.SetParent(contentContainer, false);
            RectTransform rect = invRoot.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = invRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 15;
            layout.childControlHeight = false;

            // Section 1: Relics
            GameObject relicTitle = new GameObject("RelicTitle");
            relicTitle.transform.SetParent(invRoot.transform, false);
            LayoutElement rtLe = relicTitle.AddComponent<LayoutElement>();
            rtLe.preferredHeight = 35;
            CreateTextUI(relicTitle.transform, "🏺 보유 유물 (Relics)", 26, Color.yellow);

            GameObject relicGrid = new GameObject("RelicGrid");
            relicGrid.transform.SetParent(invRoot.transform, false);
            LayoutElement rgLe = relicGrid.AddComponent<LayoutElement>();
            rgLe.preferredHeight = 120;
            Image rgBg = relicGrid.AddComponent<Image>();
            rgBg.color = new Color(0.12f, 0.15f, 0.2f, 1f);

            HorizontalLayoutGroup rLayout = relicGrid.AddComponent<HorizontalLayoutGroup>();
            rLayout.padding = new RectOffset(10, 10, 10, 10);
            rLayout.spacing = 10;

            if (ResourceManager.Instance != null && ResourceManager.Instance.Relics.Count > 0)
            {
                foreach (var relic in ResourceManager.Instance.Relics)
                {
                    GameObject item = new GameObject($"Relic_{relic.relicName}");
                    item.transform.SetParent(relicGrid.transform, false);
                    LayoutElement iLe = item.AddComponent<LayoutElement>();
                    iLe.preferredWidth = 150;
                    Image iBg = item.AddComponent<Image>();
                    iBg.color = new Color(0.25f, 0.3f, 0.4f, 1f);
                    CreateTextUI(item.transform, $"{relic.relicName}\n<size=14>{relic.description}</size>", 18, Color.white);
                }
            }
            else
            {
                CreateTextUI(relicGrid.transform, "보유 중인 유물이 없습니다.", 20, Color.gray);
            }

            // Section 2: Consumables
            GameObject conTitle = new GameObject("ConsumableTitle");
            conTitle.transform.SetParent(invRoot.transform, false);
            LayoutElement ctLe = conTitle.AddComponent<LayoutElement>();
            ctLe.preferredHeight = 35;
            CreateTextUI(conTitle.transform, "🧪 소모품 (Consumables)", 26, Color.cyan);

            GameObject conGrid = new GameObject("ConsumableGrid");
            conGrid.transform.SetParent(invRoot.transform, false);
            LayoutElement cgLe = conGrid.AddComponent<LayoutElement>();
            cgLe.preferredHeight = 80;
            Image cgBg = conGrid.AddComponent<Image>();
            cgBg.color = new Color(0.12f, 0.15f, 0.2f, 1f);

            HorizontalLayoutGroup cLayout = conGrid.AddComponent<HorizontalLayoutGroup>();
            cLayout.padding = new RectOffset(10, 10, 10, 10);
            cLayout.spacing = 10;

            if (ResourceManager.Instance != null && ResourceManager.Instance.Consumables.Count > 0)
            {
                foreach (var con in ResourceManager.Instance.Consumables)
                {
                    GameObject item = new GameObject($"Con_{con.consumableName}");
                    item.transform.SetParent(conGrid.transform, false);
                    LayoutElement iLe = item.AddComponent<LayoutElement>();
                    iLe.preferredWidth = 150;
                    Image iBg = item.AddComponent<Image>();
                    iBg.color = new Color(0.2f, 0.35f, 0.3f, 1f);
                    CreateTextUI(item.transform, $"{con.consumableName}", 18, Color.white);
                }
            }
            else
            {
                CreateTextUI(conGrid.transform, "보유 중인 소모품이 없습니다.", 20, Color.gray);
            }

            // Section 3: Equipment & Synthesis
            GameObject eqTitle = new GameObject("EquipmentTitle");
            eqTitle.transform.SetParent(invRoot.transform, false);
            LayoutElement etLe = eqTitle.AddComponent<LayoutElement>();
            etLe.preferredHeight = 35;
            CreateTextUI(eqTitle.transform, "⚔️ 장비 인벤토리 & 합성", 26, Color.magenta);

            GameObject eqGrid = new GameObject("EquipmentGrid");
            eqGrid.transform.SetParent(invRoot.transform, false);
            LayoutElement egLe = eqGrid.AddComponent<LayoutElement>();
            egLe.preferredHeight = 100;
            Image egBg = eqGrid.AddComponent<Image>();
            egBg.color = new Color(0.12f, 0.15f, 0.2f, 1f);

            CreateTextUI(eqGrid.transform, "장비 시스템 활성화됨 (장비 보유 내역 없음)", 20, Color.gray);
        }

        private TextMeshProUGUI CreateTextUI(Transform parent, string text, int fontSize, Color color)
        {
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(parent, false);
            RectTransform rect = txtObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.font = TMPFontManager.MainKoreanFont;

            return tmp;
        }
    }
}
