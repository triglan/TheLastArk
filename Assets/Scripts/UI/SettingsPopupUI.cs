using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheLastArk.Data;
using TheLastArk.Managers;

namespace TheLastArk.UI
{
    /// <summary>
    /// Slay the Spire 2 스타일의 다크 판타지 환경설정 팝업 UI
    /// 일반, 그래픽, 소리, 게임플레이(스킬 타겟 등) 4대 탭 제공
    /// </summary>
    public class SettingsPopupUI : MonoBehaviour
    {
        public static SettingsPopupUI Instance { get; private set; }

        public enum SettingTab
        {
            General,    // 일반
            Graphics,   // 그래픽
            Audio,      // 소리
            Gameplay    // 게임플레이
        }

        private GameObject popupRoot;
        private Transform contentContainer;
        private Transform tabsContainer;
        private SettingTab currentTab = SettingTab.General;

        // ── 설정 상태 캐시 ──
        private int languageIndex = 0;
        private readonly string[] languages = { "한국어", "영어 (English)", "일본어 (Japanese)", "중국어 (Chinese)" };

        private int screenShakeIndex = 1;
        private readonly string[] screenShakeOptions = { "끔", "보통", "강함" };

        private bool textEffects = true;
        private bool speedMode = false;

        private bool isFullscreen = true;
        private int displayIndex = 0;
        private readonly string[] displayOptions = { "디스플레이 1", "디스플레이 2" };

        private int resolutionIndex = 0;
        private readonly string[] resolutionOptions = { "1920 x 1080", "2560 x 1440", "1600 x 900", "1280 x 720" };

        private int aspectRatioIndex = 0;
        private readonly string[] aspectRatios = { "16:9", "16:10", "21:9" };

        private bool vsync = true;

        private float masterVolume = 1.0f;
        private float bgmVolume = 1.0f;
        private float sfxVolume = 1.0f;

        private bool showKeybindsModal = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LoadSettings();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (popupRoot != null && popupRoot.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (showKeybindsModal)
                    {
                        showKeybindsModal = false;
                        RefreshTab();
                    }
                    else
                    {
                        Hide();
                    }
                }
            }
        }

        public static void Show()
        {
            if (Instance == null)
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas == null) return;

                GameObject uiObj = new GameObject("SettingsPopupUI");
                uiObj.transform.SetParent(canvas.transform, false);
                Instance = uiObj.AddComponent<SettingsPopupUI>();
            }

            Instance.Open();
        }

        public void Open()
        {
            if (popupRoot == null)
            {
                BuildUI();
            }

            LoadSettings();
            popupRoot.SetActive(true);
            RefreshTab();
        }

        public void Hide()
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        private void LoadSettings()
        {
            languageIndex = PlayerPrefs.GetInt("Setting.Language", 0);
            screenShakeIndex = PlayerPrefs.GetInt("Setting.ScreenShake", 1);
            textEffects = PlayerPrefs.GetInt("Setting.TextEffects", 1) == 1;
            speedMode = PlayerPrefs.GetInt("Setting.SpeedMode", 0) == 1;
            isFullscreen = Screen.fullScreen;
            displayIndex = PlayerPrefs.GetInt("Setting.Display", 0);
            resolutionIndex = PlayerPrefs.GetInt("Setting.Resolution", 0);
            aspectRatioIndex = PlayerPrefs.GetInt("Setting.AspectRatio", 0);
            vsync = QualitySettings.vSyncCount > 0;
            masterVolume = PlayerPrefs.GetFloat("Setting.MasterVolume", 1.0f);
            bgmVolume = PlayerPrefs.GetFloat("Setting.BGMVolume", 1.0f);
            sfxVolume = PlayerPrefs.GetFloat("Setting.SFXVolume", 1.0f);
        }

        // ─────────────────────────────────────────────────────────────
        // 1. UI 계층 구조 생성 (Main Window Frame)
        // ─────────────────────────────────────────────────────────────
        private void BuildUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();

            // Fullscreen Dim Backdrop
            popupRoot = new GameObject("SettingsPopup_Root");
            popupRoot.transform.SetParent(canvas.transform, false);
            popupRoot.transform.SetAsLastSibling();

            RectTransform rootRect = popupRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image rootDim = popupRoot.AddComponent<Image>();
            rootDim.color = new Color(0.02f, 0.04f, 0.06f, 0.8f);

            // Centered Main Window Panel (Width: 1080, Height: 680)
            GameObject windowObj = new GameObject("SettingsWindow");
            windowObj.transform.SetParent(popupRoot.transform, false);

            RectTransform winRect = windowObj.AddComponent<RectTransform>();
            winRect.anchorMin = new Vector2(0.5f, 0.5f);
            winRect.anchorMax = new Vector2(0.5f, 0.5f);
            winRect.pivot = new Vector2(0.5f, 0.5f);
            winRect.sizeDelta = new Vector2(1080, 680);

            // Window Background & Border
            Image winBg = windowObj.AddComponent<Image>();
            winBg.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

            Outline winOutline = windowObj.AddComponent<Outline>();
            winOutline.effectColor = new Color(0.24f, 0.38f, 0.5f, 0.8f);
            winOutline.effectDistance = new Vector2(2, -2);

            // ── Top Tabs Bar ──────────────────────────────────────────
            GameObject tabsObj = new GameObject("TabsHeader");
            tabsObj.transform.SetParent(windowObj.transform, false);

            RectTransform tabsRect = tabsObj.AddComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0.06f, 0.89f);
            tabsRect.anchorMax = new Vector2(0.94f, 0.97f);
            tabsRect.offsetMin = Vector2.zero;
            tabsRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup tabsLayout = tabsObj.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.spacing = 15;
            tabsLayout.childAlignment = TextAnchor.MiddleCenter;
            tabsLayout.childControlWidth = true;
            tabsLayout.childControlHeight = true;
            tabsLayout.childForceExpandWidth = true;
            tabsLayout.childForceExpandHeight = true;

            tabsContainer = tabsObj.transform;

            CreateTabHeaderButton("일반", SettingTab.General);
            CreateTabHeaderButton("그래픽", SettingTab.Graphics);
            CreateTabHeaderButton("소리", SettingTab.Audio);
            CreateTabHeaderButton("게임플레이", SettingTab.Gameplay);

            // Top-right Close Button (X)
            GameObject closeXObj = new GameObject("CloseXButton");
            closeXObj.transform.SetParent(windowObj.transform, false);
            RectTransform closeXRect = closeXObj.AddComponent<RectTransform>();
            closeXRect.anchorMin = new Vector2(1f, 1f);
            closeXRect.anchorMax = new Vector2(1f, 1f);
            closeXRect.pivot = new Vector2(1f, 1f);
            closeXRect.anchoredPosition = new Vector2(-12, -12);
            closeXRect.sizeDelta = new Vector2(36, 36);

            Image closeXImg = closeXObj.AddComponent<Image>();
            closeXImg.color = new Color(0.7f, 0.2f, 0.2f, 1f);
            Button closeXBtn = closeXObj.AddComponent<Button>();
            closeXBtn.onClick.AddListener(Hide);
            CreateTextUI(closeXObj.transform, "X", 20, Color.white);

            // ── Main Content Area ─────────────────────────────────────
            GameObject contentObj = new GameObject("SettingsContentArea");
            contentObj.transform.SetParent(windowObj.transform, false);

            RectTransform cRect = contentObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.06f, 0.11f);
            cRect.anchorMax = new Vector2(0.94f, 0.87f);
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;

            // Subtle inner frame background
            Image cBg = contentObj.AddComponent<Image>();
            cBg.color = new Color(0.05f, 0.07f, 0.1f, 0.7f);

            // Scroll Rect & Viewport
            ScrollRect sRect = contentObj.AddComponent<ScrollRect>();
            sRect.horizontal = false;
            sRect.vertical = true;
            sRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(contentObj.transform, false);
            RectTransform vpRect = viewportObj.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = new Vector2(15, 10);
            vpRect.offsetMax = new Vector2(-15, -10);

            Image vpMaskImg = viewportObj.AddComponent<Image>();
            vpMaskImg.color = Color.white;
            Mask mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject itemsObj = new GameObject("ContentItems");
            itemsObj.transform.SetParent(viewportObj.transform, false);
            RectTransform itemsRect = itemsObj.AddComponent<RectTransform>();
            itemsRect.anchorMin = new Vector2(0f, 1f);
            itemsRect.anchorMax = new Vector2(1f, 1f);
            itemsRect.pivot = new Vector2(0.5f, 1f);
            itemsRect.offsetMin = Vector2.zero;
            itemsRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup vLayout = itemsObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(20, 20, 15, 15);
            vLayout.spacing = 12;
            vLayout.childControlHeight = true;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.childForceExpandWidth = true;

            ContentSizeFitter csf = itemsObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sRect.viewport = vpRect;
            sRect.content = itemsRect;

            contentContainer = itemsObj.transform;

            // ── Bottom Bar: Back Button ───────────────────────────────
            GameObject bottomBarObj = new GameObject("BottomBar");
            bottomBarObj.transform.SetParent(windowObj.transform, false);
            RectTransform bRect = bottomBarObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.05f, 0.02f);
            bRect.anchorMax = new Vector2(0.95f, 0.10f);
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;

            // Back Button (Bottom-Left Banner style)
            GameObject backBtnObj = new GameObject("BackBannerButton");
            backBtnObj.transform.SetParent(bottomBarObj.transform, false);
            RectTransform backRect = backBtnObj.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0f, 0.5f);
            backRect.anchorMax = new Vector2(0f, 0.5f);
            backRect.pivot = new Vector2(0f, 0.5f);
            backRect.sizeDelta = new Vector2(130, 36);

            Image backImg = backBtnObj.AddComponent<Image>();
            backImg.color = new Color(0.65f, 0.22f, 0.22f, 1f);
            Outline backOutline = backBtnObj.AddComponent<Outline>();
            backOutline.effectColor = new Color(0.85f, 0.35f, 0.35f, 0.8f);

            Button backBtn = backBtnObj.AddComponent<Button>();
            backBtn.onClick.AddListener(Hide);
            CreateTextUI(backBtnObj.transform, "<  닫기", 18, Color.white);

            TMPFontManager.ApplyFontToAll(windowObj.transform);
        }

        private void CreateTabHeaderButton(string label, SettingTab tab)
        {
            GameObject btnObj = new GameObject($"TabBtn_{tab}");
            btnObj.transform.SetParent(tabsContainer, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.28f, 0.38f, 0.6f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                currentTab = tab;
                showKeybindsModal = false;
                RefreshTab();
            });

            CreateTextUI(btnObj.transform, label, 20, new Color(0.6f, 0.72f, 0.82f, 1f));
        }

        // ─────────────────────────────────────────────────────────────
        // 2. 탭 렌더링 (Tab Switching)
        // ─────────────────────────────────────────────────────────────
        private void RefreshTab()
        {
            // Clear content
            foreach (Transform child in contentContainer)
            {
                Destroy(child.gameObject);
            }

            // Update Tab Button Highlight
            foreach (Transform tabBtn in tabsContainer)
            {
                Image img = tabBtn.GetComponent<Image>();
                Outline outline = tabBtn.GetComponent<Outline>();
                TextMeshProUGUI tmp = tabBtn.GetComponentInChildren<TextMeshProUGUI>();

                bool isSelected = tabBtn.name == $"TabBtn_{currentTab}";
                if (img != null)
                {
                    img.color = isSelected ? new Color(0.12f, 0.28f, 0.38f, 1f) : new Color(0.10f, 0.14f, 0.20f, 0.95f);
                }
                if (outline != null)
                {
                    outline.effectColor = isSelected ? new Color(0.35f, 0.85f, 0.95f, 1f) : new Color(0.2f, 0.28f, 0.38f, 0.6f);
                }
                if (tmp != null)
                {
                    tmp.color = isSelected ? new Color(0.95f, 0.98f, 1f, 1f) : new Color(0.6f, 0.72f, 0.82f, 1f);
                    tmp.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
                }
            }

            if (showKeybindsModal)
            {
                BuildKeybindsModal();
                TMPFontManager.ApplyFontToAll(contentContainer);
                return;
            }

            switch (currentTab)
            {
                case SettingTab.General:
                    BuildGeneralTab();
                    break;
                case SettingTab.Graphics:
                    BuildGraphicsTab();
                    break;
                case SettingTab.Audio:
                    BuildAudioTab();
                    break;
                case SettingTab.Gameplay:
                    BuildGameplayTab();
                    break;
            }

            TMPFontManager.ApplyFontToAll(contentContainer);
        }

        // ─────────────────────────────────────────────────────────────
        // 3. [일반 탭] (General)
        // ─────────────────────────────────────────────────────────────
        private void BuildGeneralTab()
        {
            // 1. 언어 설정
            CreateDropdownRow("언어", languages, languageIndex, newIdx =>
            {
                languageIndex = newIdx;
                PlayerPrefs.SetInt("Setting.Language", languageIndex);
                NotificationManager.Instance?.ShowMessage($"언어: {languages[languageIndex]} (준비 중)", Color.cyan);
                RefreshTab();
            });

            // 2. 화면 흔들림
            CreateStepperRow("화면 흔들림", screenShakeOptions, screenShakeIndex, newIdx =>
            {
                screenShakeIndex = newIdx;
                PlayerPrefs.SetInt("Setting.ScreenShake", screenShakeIndex);
                RefreshTab();
            });

            // 3. 텍스트 효과
            CreateToggleRow("텍스트 효과", textEffects, newVal =>
            {
                textEffects = newVal;
                PlayerPrefs.SetInt("Setting.TextEffects", textEffects ? 1 : 0);
                RefreshTab();
            });

            // 4. 배속 모드
            CreateToggleRow("배속 모드", speedMode, newVal =>
            {
                speedMode = newVal;
                PlayerPrefs.SetInt("Setting.SpeedMode", speedMode ? 1 : 0);
                RefreshTab();
            });

            // 5. 타이틀로 이동
            CreateActionRow("타이틀로 이동", "타이틀로", new Color(0.2f, 0.45f, 0.65f, 1f), () =>
            {
                Debug.Log("[SettingsPopup] Load Scene: MainMenu");
                Hide();
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            });

            // 6. 게임 종료
            CreateActionRow("게임 종료", "게임 종료", new Color(0.7f, 0.2f, 0.2f, 1f), () =>
            {
                Debug.Log("[SettingsPopup] Application.Quit()");
                Application.Quit();
            });
        }

        // ─────────────────────────────────────────────────────────────
        // 4. [그래픽 탭] (Graphics)
        // ─────────────────────────────────────────────────────────────
        private void BuildGraphicsTab()
        {
            // 1. 전체화면
            CreateToggleRow("전체화면", isFullscreen, newVal =>
            {
                isFullscreen = newVal;
                Screen.fullScreen = isFullscreen;
                PlayerPrefs.SetInt("Setting.Fullscreen", isFullscreen ? 1 : 0);
                RefreshTab();
            });

            // 2. 디스플레이 선택
            CreateDropdownRow("디스플레이 선택", displayOptions, displayIndex, newIdx =>
            {
                displayIndex = newIdx;
                PlayerPrefs.SetInt("Setting.Display", displayIndex);
                RefreshTab();
            });

            // 3. 해상도
            CreateDropdownRow("해상도", resolutionOptions, resolutionIndex, newIdx =>
            {
                resolutionIndex = newIdx;
                PlayerPrefs.SetInt("Setting.Resolution", resolutionIndex);
                ApplyResolution(resolutionIndex);
                RefreshTab();
            });

            // 4. 화면 비율
            CreateStepperRow("화면 비율", aspectRatios, aspectRatioIndex, newIdx =>
            {
                aspectRatioIndex = newIdx;
                PlayerPrefs.SetInt("Setting.AspectRatio", aspectRatioIndex);
                RefreshTab();
            });

            // 5. 수직 동기화 (V-Sync)
            CreateToggleRow("수직 동기화 (V-Sync)", vsync, newVal =>
            {
                vsync = newVal;
                QualitySettings.vSyncCount = vsync ? 1 : 0;
                RefreshTab();
            });
        }

        private void ApplyResolution(int idx)
        {
            switch (idx)
            {
                case 0: Screen.SetResolution(1920, 1080, Screen.fullScreen); break;
                case 1: Screen.SetResolution(2560, 1440, Screen.fullScreen); break;
                case 2: Screen.SetResolution(1600, 900, Screen.fullScreen); break;
                case 3: Screen.SetResolution(1280, 720, Screen.fullScreen); break;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 5. [소리 탭] (Audio)
        // ─────────────────────────────────────────────────────────────
        private void BuildAudioTab()
        {
            // 1. 전체 음량
            CreateVolumeRow("전체 음량", masterVolume, newVal =>
            {
                masterVolume = Mathf.Clamp01(newVal);
                AudioListener.volume = masterVolume;
                PlayerPrefs.SetFloat("Setting.MasterVolume", masterVolume);
                RefreshTab();
            });

            // 2. 배경음 (BGM)
            CreateVolumeRow("배경음 (BGM)", bgmVolume, newVal =>
            {
                bgmVolume = Mathf.Clamp01(newVal);
                PlayerPrefs.SetFloat("Setting.BGMVolume", bgmVolume);
                RefreshTab();
            });

            // 3. 효과음 (SFX)
            CreateVolumeRow("효과음 (SFX)", sfxVolume, newVal =>
            {
                sfxVolume = Mathf.Clamp01(newVal);
                PlayerPrefs.SetFloat("Setting.SFXVolume", sfxVolume);
                RefreshTab();
            });
        }

        // ─────────────────────────────────────────────────────────────
        // 6. [게임플레이 탭] (Gameplay)
        // ─────────────────────────────────────────────────────────────
        private void BuildGameplayTab()
        {
            // 1. 스킬 타겟 온오프 (협업자 구현 연동)
            bool currentSkillTargeting = BattleManager.SkillFirstTargeting;
            CreateToggleRow("스킬을 먼저 선택한 후 대상을 클릭", currentSkillTargeting, newVal =>
            {
                BattleManager.SkillFirstTargeting = newVal;
                NotificationManager.Instance?.ShowMessage(
                    newVal ? "스킬 선선택 타겟 모드 활성화" : "대상 선선택 모드 활성화", Color.green);
                RefreshTab();
            });

            // 2. 단축키 설정 안내
            CreateActionRow("단축키 설정", "단축키 목록 보기", new Color(0.2f, 0.45f, 0.65f, 1f), () =>
            {
                showKeybindsModal = true;
                RefreshTab();
            });

            // 3. 디버그 치트 1 (+1000 Gold)
            CreateActionRow("[디버그] 골드 치트", "+1000 Gold 지급 (단축키: F1)", new Color(0.2f, 0.55f, 0.3f, 1f), () =>
            {
                if (ResourceManager.Instance != null) ResourceManager.Instance.AddGold(1000);
                NotificationManager.Instance?.ShowMessage("[디버그] +1000 Gold 지급 완료!", Color.yellow);
            });

            // 4. 디버그 치트 2 (롱소드 10개)
            CreateActionRow("[디버그] 장비 치트", "롱소드 10개 지급 (단축키: F2)", new Color(0.2f, 0.45f, 0.75f, 1f), () =>
            {
                var longsword = EquipmentDatabase.GetEquipment("Longsword");
                if (longsword != null && ResourceManager.Instance != null)
                {
                    for (int i = 0; i < 10; i++) ResourceManager.Instance.AddEquipment(longsword);
                    NotificationManager.Instance?.ShowMessage("[디버그] 롱소드 10개 지급 완료!", Color.cyan);
                }
            });
        }

        // ─────────────────────────────────────────────────────────────
        // 7. 단축키 모달 뷰 (Keybinds Modal View)
        // ─────────────────────────────────────────────────────────────
        private void BuildKeybindsModal()
        {
            CreateSectionHeader("── 게임 단축키 안내 ──");

            CreateKeybindRow("스킬 1 ~ 4 사용", "1, 2, 3, 4");
            CreateKeybindRow("턴 종료 / 진행", "Space");
            CreateKeybindRow("가방 (인벤토리)", "I");
            CreateKeybindRow("탐사 지도 확인", "M");
            CreateKeybindRow("기차 관리", "T");
            CreateKeybindRow("캐릭터 & 덱", "C");
            CreateKeybindRow("환경설정 / 닫기", "ESC");
            CreateKeybindRow("[디버그] +1000 Gold", "F1");
            CreateKeybindRow("[디버그] 롱소드 10개", "F2");

            CreateActionRow("돌아가기", "설정 목록으로 돌아가기", new Color(0.3f, 0.5f, 0.7f, 1f), () =>
            {
                showKeybindsModal = false;
                RefreshTab();
            });
        }

        // ─────────────────────────────────────────────────────────────
        // 8. 설정 항목 Row 컴포넌트 팩토리 (Slay the Spire 2 Style)
        // ─────────────────────────────────────────────────────────────

        /// <summary>기본 Row 컨테이너 생성</summary>
        private (GameObject rowObj, Transform controlArea) CreateRowContainer(string label)
        {
            GameObject rowObj = new GameObject($"Row_{label}");
            rowObj.transform.SetParent(contentContainer, false);

            LayoutElement rowLe = rowObj.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 52;
            rowLe.minHeight = 52;

            HorizontalLayoutGroup hLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(15, 15, 6, 6);
            hLayout.spacing = 20;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlHeight = true;
            hLayout.childControlWidth = false;
            hLayout.childForceExpandWidth = false;

            // Left Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(rowObj.transform, false);
            LayoutElement labelLe = labelObj.AddComponent<LayoutElement>();
            labelLe.preferredWidth = 540;

            TextMeshProUGUI labelTmp = CreateTextUI(labelObj.transform, label, 20, new Color(0.88f, 0.92f, 0.96f, 1f));
            labelTmp.alignment = TextAlignmentOptions.Left;

            // Right Control Area
            GameObject controlObj = new GameObject("ControlArea");
            controlObj.transform.SetParent(rowObj.transform, false);
            LayoutElement ctrlLe = controlObj.AddComponent<LayoutElement>();
            ctrlLe.preferredWidth = 360;

            return (rowObj, controlObj.transform);
        }

        /// <summary>토글 (체크박스) Row 생성 - Picture 2/3 스타일 체크박스</summary>
        private void CreateToggleRow(string label, bool currentValue, Action<bool> onToggle)
        {
            var (_, ctrlArea) = CreateRowContainer(label);

            HorizontalLayoutGroup ctrlLayout = ctrlArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            ctrlLayout.childAlignment = TextAnchor.MiddleRight;
            ctrlLayout.childControlWidth = false;

            GameObject boxObj = new GameObject("CheckboxFrame");
            boxObj.transform.SetParent(ctrlArea, false);

            LayoutElement boxLe = boxObj.AddComponent<LayoutElement>();
            boxLe.preferredWidth = 34;
            boxLe.preferredHeight = 34;

            Image boxImg = boxObj.AddComponent<Image>();
            boxImg.color = new Color(0.12f, 0.17f, 0.25f, 1f);

            Outline boxOutline = boxObj.AddComponent<Outline>();
            boxOutline.effectColor = new Color(0.35f, 0.5f, 0.65f, 0.9f);
            boxOutline.effectDistance = new Vector2(1.5f, -1.5f);

            Button boxBtn = boxObj.AddComponent<Button>();
            boxBtn.onClick.AddListener(() => onToggle(!currentValue));

            if (currentValue)
            {
                // Gold checked indicator
                GameObject checkObj = new GameObject("Checkmark");
                checkObj.transform.SetParent(boxObj.transform, false);
                RectTransform cRect = checkObj.AddComponent<RectTransform>();
                cRect.anchorMin = new Vector2(0.15f, 0.15f);
                cRect.anchorMax = new Vector2(0.85f, 0.85f);
                cRect.offsetMin = Vector2.zero;
                cRect.offsetMax = Vector2.zero;

                Image checkImg = checkObj.AddComponent<Image>();
                checkImg.color = new Color(0.95f, 0.75f, 0.2f, 1f);

                TextMeshProUGUI checkTmp = CreateTextUI(checkObj.transform, "V", 20, new Color(0.08f, 0.1f, 0.14f, 1f));
                checkTmp.fontStyle = FontStyles.Bold;
            }
        }

        /// <summary>스텝 선택기 `<  Value  >` Row 생성</summary>
        private void CreateStepperRow(string label, string[] options, int currentIndex, Action<int> onIndexChanged)
        {
            var (_, ctrlArea) = CreateRowContainer(label);

            HorizontalLayoutGroup ctrlLayout = ctrlArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            ctrlLayout.childAlignment = TextAnchor.MiddleRight;
            ctrlLayout.spacing = 10;
            ctrlLayout.childControlWidth = false;

            // Left Arrow Button `<`
            GameObject leftBtnObj = new GameObject("LeftArrow");
            leftBtnObj.transform.SetParent(ctrlArea, false);
            LayoutElement leftLe = leftBtnObj.AddComponent<LayoutElement>();
            leftLe.preferredWidth = 32;
            leftLe.preferredHeight = 32;

            Image lImg = leftBtnObj.AddComponent<Image>();
            lImg.color = new Color(0.15f, 0.22f, 0.32f, 1f);
            Button lBtn = leftBtnObj.AddComponent<Button>();
            lBtn.onClick.AddListener(() =>
            {
                int next = currentIndex - 1;
                if (next < 0) next = options.Length - 1;
                onIndexChanged(next);
            });
            CreateTextUI(leftBtnObj.transform, "<", 18, new Color(0.95f, 0.78f, 0.25f, 1f));

            // Value Text Box
            GameObject valObj = new GameObject("ValueText");
            valObj.transform.SetParent(ctrlArea, false);
            LayoutElement valLe = valObj.AddComponent<LayoutElement>();
            valLe.preferredWidth = 140;
            valLe.preferredHeight = 32;

            string curText = (currentIndex >= 0 && currentIndex < options.Length) ? options[currentIndex] : "";
            TextMeshProUGUI valTmp = CreateTextUI(valObj.transform, curText, 18, Color.white);
            valTmp.alignment = TextAlignmentOptions.Center;

            // Right Arrow Button `>`
            GameObject rightBtnObj = new GameObject("RightArrow");
            rightBtnObj.transform.SetParent(ctrlArea, false);
            LayoutElement rightLe = rightBtnObj.AddComponent<LayoutElement>();
            rightLe.preferredWidth = 32;
            rightLe.preferredHeight = 32;

            Image rImg = rightBtnObj.AddComponent<Image>();
            rImg.color = new Color(0.15f, 0.22f, 0.32f, 1f);
            Button rBtn = rightBtnObj.AddComponent<Button>();
            rBtn.onClick.AddListener(() =>
            {
                int next = (currentIndex + 1) % options.Length;
                onIndexChanged(next);
            });
            CreateTextUI(rightBtnObj.transform, ">", 18, new Color(0.95f, 0.78f, 0.25f, 1f));
        }

        /// <summary>드롭다운 스타일 프레임 버튼 Row 생성 (클릭 시 다음 항목으로 순환)</summary>
        private void CreateDropdownRow(string label, string[] options, int currentIndex, Action<int> onIndexChanged)
        {
            var (_, ctrlArea) = CreateRowContainer(label);

            HorizontalLayoutGroup ctrlLayout = ctrlArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            ctrlLayout.childAlignment = TextAnchor.MiddleRight;
            ctrlLayout.childControlWidth = false;

            GameObject dropObj = new GameObject("DropdownFrame");
            dropObj.transform.SetParent(ctrlArea, false);

            LayoutElement dropLe = dropObj.AddComponent<LayoutElement>();
            dropLe.preferredWidth = 220;
            dropLe.preferredHeight = 34;

            Image dropImg = dropObj.AddComponent<Image>();
            dropImg.color = new Color(0.14f, 0.22f, 0.32f, 1f);

            Outline outline = dropObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.28f, 0.45f, 0.6f, 0.8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button dropBtn = dropObj.AddComponent<Button>();
            dropBtn.onClick.AddListener(() =>
            {
                int next = (currentIndex + 1) % options.Length;
                onIndexChanged(next);
            });

            string curText = (currentIndex >= 0 && currentIndex < options.Length) ? options[currentIndex] : "";
            TextMeshProUGUI tmp = CreateTextUI(dropObj.transform, $"{curText}   <color=#FFD700>[v]</color>", 17, Color.white);
            tmp.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>볼륨 조절 Row 생성 (< 바 + 퍼센트 >)</summary>
        private void CreateVolumeRow(string label, float volume, Action<float> onVolumeChanged)
        {
            var (_, ctrlArea) = CreateRowContainer(label);

            HorizontalLayoutGroup ctrlLayout = ctrlArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            ctrlLayout.childAlignment = TextAnchor.MiddleRight;
            ctrlLayout.spacing = 8;
            ctrlLayout.childControlWidth = false;

            // Volume Down `<`
            GameObject downObj = new GameObject("VolDown");
            downObj.transform.SetParent(ctrlArea, false);
            LayoutElement dLe = downObj.AddComponent<LayoutElement>();
            dLe.preferredWidth = 30;
            dLe.preferredHeight = 30;

            Image dImg = downObj.AddComponent<Image>();
            dImg.color = new Color(0.15f, 0.22f, 0.32f, 1f);
            Button dBtn = downObj.AddComponent<Button>();
            dBtn.onClick.AddListener(() => onVolumeChanged(Mathf.Max(0f, volume - 0.1f)));
            CreateTextUI(downObj.transform, "<", 18, new Color(0.95f, 0.78f, 0.25f, 1f));

            // Bar background & Fill
            GameObject barObj = new GameObject("VolBar");
            barObj.transform.SetParent(ctrlArea, false);
            LayoutElement barLe = barObj.AddComponent<LayoutElement>();
            barLe.preferredWidth = 120;
            barLe.preferredHeight = 18;

            Image barBg = barObj.AddComponent<Image>();
            barBg.color = new Color(0.12f, 0.15f, 0.2f, 1f);

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barObj.transform, false);
            RectTransform fRect = fillObj.AddComponent<RectTransform>();
            fRect.anchorMin = new Vector2(0f, 0f);
            fRect.anchorMax = new Vector2(Mathf.Clamp01(volume), 1f);
            fRect.offsetMin = Vector2.zero;
            fRect.offsetMax = Vector2.zero;

            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.25f, 0.7f, 0.85f, 1f);

            // Volume Up `>`
            GameObject upObj = new GameObject("VolUp");
            upObj.transform.SetParent(ctrlArea, false);
            LayoutElement uLe = upObj.AddComponent<LayoutElement>();
            uLe.preferredWidth = 30;
            uLe.preferredHeight = 30;

            Image uImg = upObj.AddComponent<Image>();
            uImg.color = new Color(0.15f, 0.22f, 0.32f, 1f);
            Button uBtn = upObj.AddComponent<Button>();
            uBtn.onClick.AddListener(() => onVolumeChanged(Mathf.Min(1f, volume + 0.1f)));
            CreateTextUI(upObj.transform, ">", 18, new Color(0.95f, 0.78f, 0.25f, 1f));

            // Percent Text
            GameObject pctObj = new GameObject("VolPercent");
            pctObj.transform.SetParent(ctrlArea, false);
            LayoutElement pctLe = pctObj.AddComponent<LayoutElement>();
            pctLe.preferredWidth = 55;
            pctLe.preferredHeight = 30;

            TextMeshProUGUI pctTmp = CreateTextUI(pctObj.transform, $"{Mathf.RoundToInt(volume * 100)}%", 17, Color.yellow);
            pctTmp.alignment = TextAlignmentOptions.Right;
        }

        /// <summary>버튼 실행형 Row 생성</summary>
        private void CreateActionRow(string label, string buttonText, Color btnColor, Action onClick)
        {
            var (_, ctrlArea) = CreateRowContainer(label);

            HorizontalLayoutGroup ctrlLayout = ctrlArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            ctrlLayout.childAlignment = TextAnchor.MiddleRight;
            ctrlLayout.childControlWidth = false;

            GameObject btnObj = new GameObject("ActionBtn");
            btnObj.transform.SetParent(ctrlArea, false);

            LayoutElement btnLe = btnObj.AddComponent<LayoutElement>();
            btnLe.preferredWidth = 230;
            btnLe.preferredHeight = 34;

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = btnColor;

            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.3f);
            outline.effectDistance = new Vector2(1, -1);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            CreateTextUI(btnObj.transform, buttonText, 16, Color.white);
        }

        /// <summary>단축키 목록용 Row</summary>
        private void CreateKeybindRow(string actionName, string keyName)
        {
            var (_, ctrlArea) = CreateRowContainer(actionName);

            HorizontalLayoutGroup ctrlLayout = ctrlArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            ctrlLayout.childAlignment = TextAnchor.MiddleRight;
            ctrlLayout.childControlWidth = false;

            GameObject keyObj = new GameObject("KeyBadge");
            keyObj.transform.SetParent(ctrlArea, false);

            LayoutElement kLe = keyObj.AddComponent<LayoutElement>();
            kLe.preferredWidth = 140;
            kLe.preferredHeight = 30;

            Image kImg = keyObj.AddComponent<Image>();
            kImg.color = new Color(0.18f, 0.25f, 0.35f, 1f);

            TextMeshProUGUI kTmp = CreateTextUI(keyObj.transform, keyName, 16, new Color(0.95f, 0.85f, 0.3f, 1f));
            kTmp.alignment = TextAlignmentOptions.Center;
        }

        private void CreateSectionHeader(string headerText)
        {
            GameObject headObj = new GameObject("SectionHeader");
            headObj.transform.SetParent(contentContainer, false);
            LayoutElement le = headObj.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            le.minHeight = 40;

            TextMeshProUGUI tmp = CreateTextUI(headObj.transform, headerText, 20, new Color(0.35f, 0.85f, 0.95f, 1f));
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
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
