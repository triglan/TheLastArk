using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
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
        private Transform detailSkillsArea;
        private Transform detailEquipmentsArea;
        private Button detailDeckButton;
        private TextMeshProUGUI detailDeckButtonText;
        private Button detailLeaderButton;
        private TextMeshProUGUI detailLeaderButtonText;
        private GameObject equipSelectModalPanel;

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

            // Debug Cheats: F1 (+1000 Gold), F2 (10x Longswords)
            if (Input.GetKeyDown(KeyCode.F1))
            {
                GrantDebugGold(1000);
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                GrantDebugLongswords(10);
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

            // Debug Cheat 1: +1000 Gold
            GameObject goldBtnObj = new GameObject("DebugGoldBtn");
            goldBtnObj.transform.SetParent(settingsPopupPanel.transform, false);
            LayoutElement gLe = goldBtnObj.AddComponent<LayoutElement>();
            gLe.preferredHeight = 50;
            Image gImg = goldBtnObj.AddComponent<Image>();
            gImg.color = new Color(0.2f, 0.6f, 0.3f, 1f);
            Button gBtn = goldBtnObj.AddComponent<Button>();
            gBtn.onClick.AddListener(() => GrantDebugGold(1000));
            CreateTextUI(goldBtnObj.transform, "💰 [디버그] +1000 Gold 지급 (단축키: F1)", 22, Color.white);

            // Debug Cheat 2: 10x Longswords
            GameObject swordBtnObj = new GameObject("DebugSwordBtn");
            swordBtnObj.transform.SetParent(settingsPopupPanel.transform, false);
            LayoutElement swLe = swordBtnObj.AddComponent<LayoutElement>();
            swLe.preferredHeight = 50;
            Image swImg = swordBtnObj.AddComponent<Image>();
            swImg.color = new Color(0.2f, 0.5f, 0.8f, 1f);
            Button swBtn = swordBtnObj.AddComponent<Button>();
            swBtn.onClick.AddListener(() => GrantDebugLongswords(10));
            CreateTextUI(swordBtnObj.transform, "🗡️ [디버그] 롱소드 10개 지급 (단축키: F2)", 22, Color.white);

            // Quit Game Button
            GameObject quitBtnObj = new GameObject("QuitBtn");
            quitBtnObj.transform.SetParent(settingsPopupPanel.transform, false);
            LayoutElement qLe = quitBtnObj.AddComponent<LayoutElement>();
            qLe.preferredHeight = 50;
            Image qImg = quitBtnObj.AddComponent<Image>();
            qImg.color = new Color(0.7f, 0.2f, 0.2f, 1f);
            Button qBtn = quitBtnObj.AddComponent<Button>();
            qBtn.onClick.AddListener(() =>
            {
                Debug.Log("[SettingsPopup] Application.Quit()");
                Application.Quit();
            });
            CreateTextUI(quitBtnObj.transform, "🚪 게임 종료", 24, Color.white);

            TMPFontManager.ApplyFontToAll(settingsPopupPanel.transform);
        }

        public void GrantDebugGold(int amount = 1000)
        {
            if (RunManager.Instance != null && RunManager.Instance.State != null)
            {
                RunManager.Instance.State.gold += amount;
            }
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.AddGold(amount);
            }
            NotificationManager.Instance?.ShowMessage($"💰 [디버그] {amount} Gold 지급 완료!", Color.yellow);
        }

        public void GrantDebugLongswords(int count = 10)
        {
            var longsword = EquipmentDatabase.GetEquipment("Longsword");
            if (longsword != null && ResourceManager.Instance != null)
            {
                for (int i = 0; i < count; i++)
                {
                    ResourceManager.Instance.AddEquipment(longsword);
                }
                NotificationManager.Instance?.ShowMessage($"🗡️ [디버그] 롱소드 {count}개 지급 완료!", Color.cyan);
            }
            else
            {
                NotificationManager.Instance?.ShowMessage("[디버그] 롱소드 장비 데이터를 찾을 수 없습니다.", Color.red);
            }
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
            CreateCharacterDetailPopupUI();

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
        // Character Card UI Widget (정사각형 초상화 + 정보 축소 형태)
        // ─────────────────────────────────────────────────────────────
        private void CreateCharacterCardUI(Transform parent, CharacterData data, int cardCount, bool inParty, bool isLeader)
        {
            string charId = data.DataId;
            int level = ResourceManager.Instance != null ? ResourceManager.Instance.GetCharacterLevelFromCards(cardCount) : 0;
            if (level < 0) level = 0;

            CharacterStatus status = new CharacterStatus(data);

            GameObject cardObj = new GameObject($"Card_{data.DisplayName}");
            cardObj.transform.SetParent(parent, false);

            LayoutElement le = cardObj.AddComponent<LayoutElement>();
            le.preferredWidth = 220;
            le.preferredHeight = 260; // 정사각형 컴팩트 카드 레이아웃

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.12f, 0.14f, 0.18f, 1f);

            // Leader Highlight Gold Border
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
            vLayout.spacing = 6;
            vLayout.childControlHeight = false;

            // 1. Portrait Image (사진 크게)
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

            // 2. Name & Level (정보 작게)
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(cardObj.transform, false);
            LayoutElement nLe = nameObj.AddComponent<LayoutElement>();
            nLe.preferredHeight = 28;
            CreateTextUI(nameObj.transform, $"[{data.DisplayName}] Lv.{level}", 20, Color.green);

            // 3. Compact Info (체력 / 카운트)
            GameObject hpObj = new GameObject("HpText");
            hpObj.transform.SetParent(cardObj.transform, false);
            LayoutElement hpLe = hpObj.AddComponent<LayoutElement>();
            hpLe.preferredHeight = 22;
            CreateTextUI(hpObj.transform, $"HP: {status.FinalMaxHp} | 정신: {status.FinalMaxMental}", 16, Color.white);
        }

        // ─────────────────────────────────────────────────────────────
        // Character Detail Popup (7종 스탯, 4종 스킬 호버 툴팁, 장비 2슬롯)
        // ─────────────────────────────────────────────────────────────
        private void CreateCharacterDetailPopupUI()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            charDetailPopupPanel = new GameObject("CharDetailPopup");
            charDetailPopupPanel.transform.SetParent(canvas.transform, false);
            charDetailPopupPanel.transform.SetAsLastSibling();

            RectTransform rect = charDetailPopupPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.05f);
            rect.anchorMax = new Vector2(0.9f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = charDetailPopupPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.15f, 0.98f);

            // Close Button
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(charDetailPopupPanel.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-20, -20);
            closeRect.sizeDelta = new Vector2(40, 40);

            Image cImg = closeBtnObj.AddComponent<Image>();
            cImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            Button cBtn = closeBtnObj.AddComponent<Button>();
            cBtn.onClick.AddListener(() => charDetailPopupPanel.SetActive(false));
            CreateTextUI(closeBtnObj.transform, "X", 26, Color.white);

            // Left Side: Full Standing Illustration
            GameObject illObj = new GameObject("Illustration");
            illObj.transform.SetParent(charDetailPopupPanel.transform, false);
            RectTransform illRect = illObj.AddComponent<RectTransform>();
            illRect.anchorMin = new Vector2(0.03f, 0.05f);
            illRect.anchorMax = new Vector2(0.42f, 0.95f);
            illRect.offsetMin = Vector2.zero;
            illRect.offsetMax = Vector2.zero;

            detailStandingImage = illObj.AddComponent<Image>();
            detailStandingImage.preserveAspect = true;

            // Right Side: Title, 7 Stats, Skills, Equipment 2 Slots, Deck Buttons
            GameObject rightArea = new GameObject("RightArea");
            rightArea.transform.SetParent(charDetailPopupPanel.transform, false);
            RectTransform rightRect = rightArea.AddComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.44f, 0.03f);
            rightRect.anchorMax = new Vector2(0.97f, 0.95f);
            rightRect.offsetMin = Vector2.zero;
            rightRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup rLayout = rightArea.AddComponent<VerticalLayoutGroup>();
            rLayout.spacing = 10;
            rLayout.childControlHeight = false;

            // 1. Header Title
            GameObject titleObj = new GameObject("DetailTitleText");
            titleObj.transform.SetParent(rightArea.transform, false);
            LayoutElement tLe = titleObj.AddComponent<LayoutElement>();
            tLe.preferredHeight = 40;
            detailTitleText = CreateTextUI(titleObj.transform, "[ Character Name ]", 30, Color.yellow);

            // 2. 7 Stats Section (7종 스탯 출력)
            GameObject statsObj = new GameObject("DetailStatsText");
            statsObj.transform.SetParent(rightArea.transform, false);
            LayoutElement sLe = statsObj.AddComponent<LayoutElement>();
            sLe.preferredHeight = 150;
            detailStatsText = CreateTextUI(statsObj.transform, "Stats Loading...", 20, Color.white);

            // 3. Active Skills Section (4종 스킬 아이콘 + 호버 툴팁)
            GameObject skillsHeaderObj = new GameObject("SkillsHeaderText");
            skillsHeaderObj.transform.SetParent(rightArea.transform, false);
            LayoutElement skhLe = skillsHeaderObj.AddComponent<LayoutElement>();
            skhLe.preferredHeight = 25;
            CreateTextUI(skillsHeaderObj.transform, "📜 보유 스킬 목록 (마우스 호버 시 툴팁 출력)", 20, Color.cyan);

            GameObject skillsAreaObj = new GameObject("SkillsArea");
            skillsAreaObj.transform.SetParent(rightArea.transform, false);
            LayoutElement skLe = skillsAreaObj.AddComponent<LayoutElement>();
            skLe.preferredHeight = 70;

            HorizontalLayoutGroup skLayout = skillsAreaObj.AddComponent<HorizontalLayoutGroup>();
            skLayout.spacing = 15;
            detailSkillsArea = skillsAreaObj.transform;

            // 4. Equipment Slots Section (장비 2칸)
            GameObject equipHeaderObj = new GameObject("EquipHeaderText");
            equipHeaderObj.transform.SetParent(rightArea.transform, false);
            LayoutElement eqhLe = equipHeaderObj.AddComponent<LayoutElement>();
            eqhLe.preferredHeight = 25;
            CreateTextUI(equipHeaderObj.transform, "🛡️ 장착 장비 (최대 2개)", 20, new Color(1f, 0.85f, 0.2f));

            GameObject equipAreaObj = new GameObject("EquipArea");
            equipAreaObj.transform.SetParent(rightArea.transform, false);
            LayoutElement eqLe = equipAreaObj.AddComponent<LayoutElement>();
            eqLe.preferredHeight = 65;

            HorizontalLayoutGroup eqLayout = equipAreaObj.AddComponent<HorizontalLayoutGroup>();
            eqLayout.spacing = 20;
            detailEquipmentsArea = equipAreaObj.transform;

            // 5. Action Buttons Container
            GameObject actionsObj = new GameObject("ActionButtons");
            actionsObj.transform.SetParent(rightArea.transform, false);
            LayoutElement actLe = actionsObj.AddComponent<LayoutElement>();
            actLe.preferredHeight = 50;

            HorizontalLayoutGroup actLayout = actionsObj.AddComponent<HorizontalLayoutGroup>();
            actLayout.spacing = 20;

            // Deck Toggle Button
            GameObject deckBtnObj = new GameObject("DeckButton");
            deckBtnObj.transform.SetParent(actionsObj.transform, false);
            LayoutElement dLe = deckBtnObj.AddComponent<LayoutElement>();
            dLe.preferredWidth = 180;
            dLe.preferredHeight = 45;

            Image dImg = deckBtnObj.AddComponent<Image>();
            dImg.color = new Color(0.2f, 0.5f, 0.8f, 1f);
            detailDeckButton = deckBtnObj.AddComponent<Button>();
            detailDeckButtonText = CreateTextUI(deckBtnObj.transform, "⚔️ 덱에 참가", 20, Color.white);

            // Leader Assign Button
            GameObject leaderBtnObj = new GameObject("LeaderButton");
            leaderBtnObj.transform.SetParent(actionsObj.transform, false);
            LayoutElement lLe = leaderBtnObj.AddComponent<LayoutElement>();
            lLe.preferredWidth = 180;
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
            if (charDetailPopupPanel == null) CreateCharacterDetailPopupUI();

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

            // 2. Character Status & 7 Stats Display
            CharacterStatus status = null;
            if (RunManager.Instance != null && RunManager.Instance.State != null)
            {
                status = RunManager.Instance.State.partyStatuses.FirstOrDefault(s => s.origin != null && s.origin.DataId == charId);
            }
            if (status == null) status = new CharacterStatus(data);

            detailTitleText.text = $"[ {data.DisplayName} ]  Lv.{status.charLevel} ({status.LevelTitle})";
            
            // 7종 스탯 (공격력, 주문력, 체력, 정신력, 방어력, 마법저항력, 치명)
            detailStatsText.text = 
                $"⚔️ 공격력: <color=#FF6B6B>{status.FinalAttack:F0}</color>    |    🔮 주문력: <color=#CC5DE8>{status.FinalSpellPower:F0}</color>\n" +
                $"❤️ 체력: <color=#51CF66>{status.currentHp:F0} / {status.FinalMaxHp:F0}</color>    |    🧠 정신력: <color=#339AF0>{status.currentMental:F0} / {status.FinalMaxMental:F0}</color>\n" +
                $"🛡️ 방어력: <color=#FCC419>{status.FinalArmor:F0}</color>    |    💠 마법저항력: <color=#20C997>{status.FinalMagicResist:F0}</color>\n" +
                $"🎯 치명타율: <color=#FF922B>{status.FinalCritRate:F0}%</color>";

            // 3. Render 4 Active Skill Icons & Hover Tooltips
            foreach (Transform child in detailSkillsArea) Destroy(child.gameObject);

            bool isLeaderChar = isLeader;
            if (isLeaderChar)
            {
                status.EnsureLeaderExtraSkill();
            }
            int leaderSkillIdx = status.leaderExtraSkillIndex;

            for (int i = 0; i < data.activeSkills.Length; i++)
            {
                int skillIndex = i;
                SkillInfo skill = data.activeSkills[i];
                if (skill == null) continue;

                bool isEquipped = status.selectedActiveSkillIndices.Contains(skillIndex);
                bool isLeaderSkill = (skillIndex == leaderSkillIdx);

                GameObject sBtnObj = new GameObject($"SkillIcon_{i}");
                sBtnObj.transform.SetParent(detailSkillsArea, false);

                LayoutElement sLe = sBtnObj.AddComponent<LayoutElement>();
                sLe.preferredWidth = 105;
                sLe.preferredHeight = 60;

                Image sImg = sBtnObj.AddComponent<Image>();
                if (isEquipped)
                {
                    sImg.color = new Color(0.12f, 0.52f, 0.28f, 1f); // 일반 선택 스킬 (녹색)
                }
                else if (isLeaderSkill)
                {
                    sImg.color = isLeaderChar ? new Color(0.75f, 0.55f, 0.1f, 1f) : new Color(0.45f, 0.38f, 0.15f, 0.75f); // 리더 고정 스킬 (금색/황동색)
                }
                else
                {
                    sImg.color = new Color(0.18f, 0.2f, 0.25f, 0.45f); // 미선택 (회색 실루엣)
                }

                Button sBtn = sBtnObj.AddComponent<Button>();
                sBtn.onClick.AddListener(() =>
                {
                    if (isLeaderSkill && isLeaderChar)
                    {
                        NotificationManager.Instance?.ShowMessage("👑 리더 전용 스킬입니다 (전투 시 자동 포함)", Color.yellow);
                    }
                    else
                    {
                        NotificationManager.Instance?.ShowMessage("💡 장착 스킬은 주점(마을)에서 골드를 지불하여 랜덤 변경할 수 있습니다.", Color.cyan);
                    }
                });

                // Mouse Hover Tooltip EventTrigger
                var trigger = sBtnObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                
                var entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
                entryEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
                entryEnter.callback.AddListener((_) =>
                {
                    if (SkillTooltipUI.Instance != null)
                    {
                        SkillTooltipUI.Instance.ShowTooltip(skill, status, data.DisplayName);
                    }
                });
                trigger.triggers.Add(entryEnter);

                var entryExit = new UnityEngine.EventSystems.EventTrigger.Entry();
                entryExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
                entryExit.callback.AddListener((_) =>
                {
                    if (SkillTooltipUI.Instance != null)
                    {
                        SkillTooltipUI.Instance.HideTooltip();
                    }
                });
                trigger.triggers.Add(entryExit);

                string skillLabel;
                Color labelColor;

                if (isEquipped)
                {
                    skillLabel = $"✓ {skill.skillName}";
                    labelColor = Color.green;
                }
                else if (isLeaderSkill)
                {
                    skillLabel = isLeaderChar ? $"👑 {skill.skillName}" : $"👑 {skill.skillName}";
                    labelColor = isLeaderChar ? Color.yellow : new Color(0.85f, 0.7f, 0.2f);
                }
                else
                {
                    skillLabel = skill.skillName;
                    labelColor = Color.gray;
                }

                CreateTextUI(sBtnObj.transform, skillLabel, 15, labelColor);
            }

            // 4. Render 2 Equipment Slots (장비 슬롯 2칸)
            foreach (Transform child in detailEquipmentsArea) Destroy(child.gameObject);

            for (int sIdx = 0; sIdx < CharacterStatus.EquipmentSlotCount; sIdx++)
            {
                int slotIndex = sIdx;
                TheLastArk.Data.EquipmentData equipped = status.GetEquippedItem(slotIndex);

                GameObject eqBtnObj = new GameObject($"EquipSlot_{slotIndex}");
                eqBtnObj.transform.SetParent(detailEquipmentsArea, false);

                LayoutElement eqLe = eqBtnObj.AddComponent<LayoutElement>();
                eqLe.preferredWidth = 200;
                eqLe.preferredHeight = 55;

                Image eqImg = eqBtnObj.AddComponent<Image>();
                eqImg.color = equipped != null ? new Color(0.12f, 0.32f, 0.48f, 0.95f) : new Color(0.18f, 0.2f, 0.26f, 0.8f);

                Button eqBtn = eqBtnObj.AddComponent<Button>();

                if (equipped == null)
                {
                    // 빈 슬롯: [+] 표시
                    CreateTextUI(eqBtnObj.transform, "[ ➕ 장비 장착 ]", 18, Color.gray);
                    eqBtn.onClick.AddListener(() =>
                    {
                        ShowEquipmentSelectModal(data, status, slotIndex);
                    });
                }
                else
                {
                    // 장착된 슬롯: 장비 이름 + 클릭 시 해제
                    string eqLabel = $"★{equipped.starLevel} {equipped.equipmentName}\n<size=14><color=cyan>클릭 시 해제</color></size>";
                    CreateTextUI(eqBtnObj.transform, eqLabel, 16, Color.yellow);

                    var trigger = eqBtnObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                    var entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
                    entryEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
                    entryEnter.callback.AddListener((_) =>
                    {
                        if (EquipmentTooltipUI.Instance != null) EquipmentTooltipUI.Instance.ShowTooltip(equipped);
                    });
                    trigger.triggers.Add(entryEnter);

                    var entryExit = new UnityEngine.EventSystems.EventTrigger.Entry();
                    entryExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
                    entryExit.callback.AddListener((_) =>
                    {
                        if (EquipmentTooltipUI.Instance != null) EquipmentTooltipUI.Instance.HideTooltip();
                    });
                    trigger.triggers.Add(entryExit);

                    eqBtn.onClick.AddListener(() =>
                    {
                        // 장비 해제
                        if (EquipmentTooltipUI.Instance != null) EquipmentTooltipUI.Instance.HideTooltip();
                        status.SetEquippedItem(slotIndex, null);
                        NotificationManager.Instance?.ShowMessage($"[{equipped.equipmentName}] 장비를 해제했습니다.", Color.yellow);
                        ShowCharacterDetailPopup(data);
                    });
                }
            }

            // 5. Deck Toggle Button
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

            // 6. Leader Button
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
        // Equipment Selection Inventory Modal Popup (장비 교체/장착 인벤토리 모달)
        // ─────────────────────────────────────────────────────────────
        private void ShowEquipmentSelectModal(CharacterData charData, CharacterStatus status, int slotIndex)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (equipSelectModalPanel == null)
            {
                equipSelectModalPanel = new GameObject("EquipSelectModal");
                equipSelectModalPanel.transform.SetParent(canvas.transform, false);
                equipSelectModalPanel.transform.SetAsLastSibling();

                RectTransform rect = equipSelectModalPanel.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.2f, 0.15f);
                rect.anchorMax = new Vector2(0.8f, 0.85f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                Image bg = equipSelectModalPanel.AddComponent<Image>();
                bg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f);
            }

            foreach (Transform child in equipSelectModalPanel.transform) Destroy(child.gameObject);

            // Close Button
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(equipSelectModalPanel.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-15, -15);
            closeRect.sizeDelta = new Vector2(36, 36);

            Image cImg = closeBtnObj.AddComponent<Image>();
            cImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            Button cBtn = closeBtnObj.AddComponent<Button>();
            cBtn.onClick.AddListener(() => equipSelectModalPanel.SetActive(false));
            CreateTextUI(closeBtnObj.transform, "X", 22, Color.white);

            // Modal Title
            GameObject titleObj = new GameObject("ModalTitle");
            titleObj.transform.SetParent(equipSelectModalPanel.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.05f, 0.88f);
            tRect.anchorMax = new Vector2(0.95f, 0.96f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            CreateTextUI(titleObj.transform, $"🛡️ [{charData.DisplayName}] 슬롯 {slotIndex + 1} 장비 선택", 26, Color.yellow);

            // Content Grid Area
            GameObject contentObj = new GameObject("ModalContent");
            contentObj.transform.SetParent(equipSelectModalPanel.transform, false);
            RectTransform cRect = contentObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.05f, 0.05f);
            cRect.anchorMax = new Vector2(0.95f, 0.85f);
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;

            GridLayoutGroup grid = contentObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(240, 100);
            grid.spacing = new Vector2(15, 15);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            var inventoryEquips = ResourceManager.Instance != null ? ResourceManager.Instance.Equipments : new List<TheLastArk.Data.EquipmentData>();

            if (inventoryEquips == null || inventoryEquips.Count == 0)
            {
                CreateTextUI(contentObj.transform, "보유 중인 미장착 장비가 없습니다.\n(대장간에서 장비를 구매해 보세요!)", 20, Color.gray);
            }
            else
            {
                foreach (var eq in inventoryEquips)
                {
                    if (eq == null) continue;
                    var equipData = eq;

                    GameObject cardObj = new GameObject($"EquipItem_{equipData.equipmentName}");
                    cardObj.transform.SetParent(contentObj.transform, false);

                    Image bg = cardObj.AddComponent<Image>();
                    bg.color = new Color(0.12f, 0.16f, 0.25f, 0.95f);

                    var trigger = cardObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                    var entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
                    entryEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
                    entryEnter.callback.AddListener((_) =>
                    {
                        if (EquipmentTooltipUI.Instance != null) EquipmentTooltipUI.Instance.ShowTooltip(equipData);
                    });
                    trigger.triggers.Add(entryEnter);

                    var entryExit = new UnityEngine.EventSystems.EventTrigger.Entry();
                    entryExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
                    entryExit.callback.AddListener((_) =>
                    {
                        if (EquipmentTooltipUI.Instance != null) EquipmentTooltipUI.Instance.HideTooltip();
                    });
                    trigger.triggers.Add(entryExit);

                    Button btn = cardObj.AddComponent<Button>();
                    btn.onClick.AddListener(() =>
                    {
                        if (EquipmentTooltipUI.Instance != null) EquipmentTooltipUI.Instance.HideTooltip();
                        status.SetEquippedItem(slotIndex, equipData);
                        NotificationManager.Instance?.ShowMessage($"[{charData.DisplayName}]에게 [{equipData.equipmentName}] 장착 완료!", Color.green);
                        equipSelectModalPanel.SetActive(false);
                        ShowCharacterDetailPopup(charData);
                    });

                    string cardLabel = $"★{equipData.starLevel} {equipData.equipmentName}\n<size=14><color=gray>{equipData.category}</color></size>";
                    CreateTextUI(cardObj.transform, cardLabel, 18, Color.cyan);
                }
            }

            equipSelectModalPanel.SetActive(true);
            TMPFontManager.ApplyFontToAll(equipSelectModalPanel.transform);
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
