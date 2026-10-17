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
        private bool showDeveloperTools;
        private ScrollRect settingsScroll;

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
                Canvas canvas = HudCanvasUtility.FindOverlayCanvas();
                if (canvas == null) return;

                GameObject uiObj = new GameObject("SettingsPopupUI");
                uiObj.transform.SetParent(canvas.transform, false);
                Instance = uiObj.AddComponent<SettingsPopupUI>();
            }

            Instance.Open();
        }

        public void Open()
        {
            // Battle uses a separate, higher-order HUD canvas. Keep the modal
            // on that canvas so HUD graphics cannot intercept its close buttons.
            Canvas canvas = HudCanvasUtility.FindOverlayCanvas();
            if (canvas == null) return;
            transform.SetParent(canvas.transform, false);

            if (popupRoot == null)
            {
                BuildUI();
            }

            popupRoot.transform.SetParent(canvas.transform, false);
            popupRoot.transform.SetAsLastSibling();
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
            var canvas = GetComponentInParent<Canvas>() ?? HudCanvasUtility.FindOverlayCanvas();
            popupRoot = ManagementDialog.Create(canvas, "SettingsPopup_Root", "환경 설정", "THE LAST ARK  /  항해 환경", Hide, 0.16f, out var window);
            var tabs = ManagementTheme.Rect("TabsHeader", window);
            ManagementTheme.Top(tabs, 100, 52, 28, 28);
            ManagementTheme.RowLayout(tabs, 8);
            tabsContainer = tabs;
            CreateTabHeaderButton("01   일반", SettingTab.General);
            CreateTabHeaderButton("02   그래픽", SettingTab.Graphics);
            CreateTabHeaderButton("03   소리", SettingTab.Audio);
            CreateTabHeaderButton("04   게임플레이", SettingTab.Gameplay);
            var body = ManagementDialog.Body(window);
            var scroll = ManagementTheme.Scroll(body, "SettingsContentArea", out var content);
            settingsScroll = scroll.GetComponent<ScrollRect>();
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10; layout.padding = new RectOffset(2, 2, 2, 8);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            contentContainer = content;
        }

        private void CreateTabHeaderButton(string label, SettingTab tab)
        {
            ManagementTheme.Button(tabsContainer, $"TabBtn_{tab}", label, () =>
            {
                currentTab = tab; showKeybindsModal = false;
                RefreshTab(); settingsScroll.verticalNormalizedPosition = 1;
            });
        }

        // ─────────────────────────────────────────────────────────────
        // 2. 탭 렌더링 (Tab Switching)
        // ─────────────────────────────────────────────────────────────
        private void RefreshTab()
        {
            // Clear content
            foreach (Transform child in contentContainer)
            {
                child.gameObject.SetActive(false);
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
                    img.color = isSelected ? ManagementTheme.Raised : ManagementTheme.Surface;
                }
                if (outline != null)
                {
                    outline.effectColor = isSelected ? new Color(0.35f, 0.85f, 0.95f, 1f) : new Color(0.2f, 0.28f, 0.38f, 0.6f);
                }
                if (tmp != null)
                {
                    tmp.color = isSelected ? ManagementTheme.Gold : ManagementTheme.Muted;
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
            CreateSectionHeader("일반 설정");
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
            CreateSectionHeader("화면 설정");
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
            CreateSectionHeader("소리 크기");
            // 1. 전체 음량
            CreateVolumeRow("전체 음량", masterVolume, newVal =>
            {
                masterVolume = Mathf.Clamp01(newVal);
                AudioListener.volume = masterVolume;
                PlayerPrefs.SetFloat("Setting.MasterVolume", masterVolume);
            });

            // 2. 배경음 (BGM)
            CreateVolumeRow("배경음 (BGM)", bgmVolume, newVal =>
            {
                bgmVolume = Mathf.Clamp01(newVal);
                PlayerPrefs.SetFloat("Setting.BGMVolume", bgmVolume);
            });

            // 3. 효과음 (SFX)
            CreateVolumeRow("효과음 (SFX)", sfxVolume, newVal =>
            {
                sfxVolume = Mathf.Clamp01(newVal);
                PlayerPrefs.SetFloat("Setting.SFXVolume", sfxVolume);
            });
        }

        // ─────────────────────────────────────────────────────────────
        // 6. [게임플레이 탭] (Gameplay)
        // ─────────────────────────────────────────────────────────────
        private void BuildGameplayTab()
        {
            CreateSectionHeader("전투 조작");
            // 1. 스킬 타겟 온오프 (협업자 구현 연동)
            bool currentSkillTargeting = BattleManager.SkillFirstTargeting;
            CreateToggleRow("스킬 먼저 선택", currentSkillTargeting, newVal =>
            {
                BattleManager.SkillFirstTargeting = newVal;
                NotificationManager.Instance?.ShowMessage(
                    newVal ? "스킬 선선택 타겟 모드 활성화" : "대상 선선택 모드 활성화", Color.green);
                RefreshTab();
            });

            CreateToggleRow("턴 자동 종료", BattleManager.AutoEndDiceTurn, newVal =>
            {
                BattleManager.AutoEndDiceTurn = newVal;
                RefreshTab();
            });

            // 2. 단축키 설정 안내
            CreateActionRow("단축키 안내", "단축키 목록 보기", new Color(0.2f, 0.45f, 0.65f, 1f), () =>
            {
                showKeybindsModal = true;
                RefreshTab();
            });

            CreateActionRow("개발 도구", showDeveloperTools ? "접기" : "펼치기", ManagementTheme.Surface, () =>
            {
                showDeveloperTools = !showDeveloperTools;
                RefreshTab();
            });
            if (!showDeveloperTools) return;
            CreateSectionHeader("개발 도구");
            // 3. 디버그 치트 1 (+1000 Gold)
            CreateActionRow("골드 지급 · F1", "+1,000 G 지급", new Color(0.2f, 0.55f, 0.3f, 1f), () =>
            {
                if (ResourceManager.Instance != null) ResourceManager.Instance.AddGold(1000);
                NotificationManager.Instance?.ShowMessage("[디버그] +1000 Gold 지급 완료!", Color.yellow);
            });

            // 4. 디버그 치트 2 (롱소드 10개)
            CreateActionRow("장비 지급 · F2", "롱소드 10개 지급", new Color(0.2f, 0.45f, 0.75f, 1f), () =>
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
            CreateSectionHeader("단축키 안내");

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

        private (GameObject rowObj, Transform controlArea) CreateRowContainer(string label)
        {
            var row = ManagementTheme.Panel("Row_" + label, contentContainer, ManagementTheme.Surface, true);
            ManagementTheme.Height(row, 92);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 16, 16); layout.spacing = 28;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = true;
            var copy = ManagementTheme.Rect("Copy", row);
            var copySize = copy.gameObject.AddComponent<LayoutElement>();
            copySize.minWidth = 240; copySize.flexibleWidth = 1;
            var title = ManagementTheme.Label(copy, label, 23);
            ManagementTheme.Top(title.rectTransform, 0, 30);
            string description = RowDescription(label);
            var note = ManagementTheme.Label(copy, description, 16, ManagementTheme.Muted);
            ManagementTheme.Top(note.rectTransform, 35, 25);
            if (string.IsNullOrEmpty(description)) ManagementTheme.Place(title.rectTransform, Vector2.zero, Vector2.one);
            var control = ManagementTheme.Rect("ControlArea", row);
            var size = control.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = 360;
            return (row.gameObject, control);
        }

        private static string RowDescription(string label)
        {
            switch (label)
            {
                case "언어": return "표시 언어 · 번역 준비 중";
                case "화면 흔들림": return "전투 중 화면 흔들림 강도";
                case "텍스트 효과": return "전투 텍스트의 연출 효과";
                case "배속 모드": return "전투 진행 속도 설정";
                case "전체화면": return "전체화면과 창 모드 전환";
                case "해상도": return "게임 화면의 크기 선택";
                case "화면 비율": return "화면 가로·세로 비율 선택";
                case "수직 동기화 (V-Sync)": return "화면 갱신 주기에 맞춰 출력";
                case "전체 음량": return "게임의 전체 소리 크기";
                case "배경음 (BGM)": return "음악의 소리 크기";
                case "효과음 (SFX)": return "전투와 조작 효과음의 크기";
                case "스킬 먼저 선택": return "켜짐: 스킬 → 대상  /  꺼짐: 대상 → 스킬";
                case "턴 자동 종료": return "사용할 주사위가 없으면 턴 종료";
                case "단축키 안내": return "자주 사용하는 조작 확인";
                case "개발 도구": return "플레이 테스트용 자원 지급";
                default: return "";
            }
        }

        private void CreateToggleRow(string label, bool currentValue, Action<bool> onToggle)
        {
            var (_, controls) = CreateRowContainer(label);
            var button = ManagementTheme.Button(controls, "CheckboxFrame", currentValue ? "켜짐" : "꺼짐", () => onToggle(!currentValue), currentValue);
            ManagementTheme.Place((RectTransform)button.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-152, -22), new Vector2(0, 22));
            var indicator = ManagementTheme.Panel("State", button.transform, currentValue ? ManagementTheme.Cyan : ManagementTheme.Line);
            ManagementTheme.Place(indicator, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, -4), new Vector2(24, 4));
            indicator.GetComponent<Image>().raycastTarget = false;
        }

        private void CreateStepperRow(string label, string[] options, int currentIndex, Action<int> onIndexChanged)
        {
            var (_, controls) = CreateRowContainer(label);
            var left = ManagementTheme.Button(controls, "LeftArrow", "<", () => onIndexChanged((currentIndex + options.Length - 1) % options.Length));
            ManagementTheme.Place((RectTransform)left.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, -22), new Vector2(44, 22));
            var right = ManagementTheme.Button(controls, "RightArrow", ">", () => onIndexChanged((currentIndex + 1) % options.Length));
            ManagementTheme.Place((RectTransform)right.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-44, -22), new Vector2(0, 22));
            var value = ManagementTheme.Label(controls, options[Mathf.Clamp(currentIndex, 0, options.Length - 1)], 22, ManagementTheme.Gold, TextAlignmentOptions.Center);
            ManagementTheme.Place(value.rectTransform, Vector2.zero, Vector2.one, new Vector2(52, 0), new Vector2(-52, 0));
        }

        private void CreateDropdownRow(string label, string[] options, int currentIndex, Action<int> onIndexChanged)
            => CreateStepperRow(label, options, currentIndex, onIndexChanged);

        private void CreateVolumeRow(string label, float volume, Action<float> onVolumeChanged)
        {
            var (_, controls) = CreateRowContainer(label);
            var percent = ManagementTheme.Label(controls, $"{Mathf.RoundToInt(volume * 100)}%", 22, ManagementTheme.Gold, TextAlignmentOptions.MidlineRight);
            ManagementTheme.Place(percent.rectTransform, new Vector2(1, 0), Vector2.one, new Vector2(-65, 0), Vector2.zero);
            var sliderRect = ManagementTheme.Rect("VolumeSlider", controls);
            ManagementTheme.Place(sliderRect, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(48, -22), new Vector2(-116, 22));
            var track = ManagementTheme.Panel("Track", sliderRect, ManagementTheme.Background);
            ManagementTheme.Place(track, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -4), new Vector2(0, 4));
            var fill = ManagementTheme.Panel("Fill", track, ManagementTheme.Cyan);
            var handleArea = ManagementTheme.Rect("HandleArea", sliderRect);
            ManagementTheme.Place(handleArea, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(8, -14), new Vector2(-8, 14));
            var handle = ManagementTheme.Panel("Handle", handleArea, ManagementTheme.Gold);
            handle.sizeDelta = new Vector2(16, 0);
            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = 0; slider.maxValue = 1; slider.SetValueWithoutNotify(volume);
            slider.onValueChanged.AddListener(value =>
            {
                percent.text = $"{Mathf.RoundToInt(value * 100)}%";
                onVolumeChanged(value);
            });
            var down = ManagementTheme.Button(controls, "VolDown", "−", () => slider.value = Mathf.Max(0, slider.value - 0.1f));
            ManagementTheme.Place((RectTransform)down.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, -20), new Vector2(36, 20));
            var up = ManagementTheme.Button(controls, "VolUp", "+", () => slider.value = Mathf.Min(1, slider.value + 0.1f));
            ManagementTheme.Place((RectTransform)up.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-104, -20), new Vector2(-68, 20));
        }

        private void CreateActionRow(string label, string buttonText, Color btnColor, Action onClick)
        {
            var (_, controls) = CreateRowContainer(label);
            var button = ManagementTheme.Button(controls, "ActionBtn", buttonText, () => onClick?.Invoke());
            ManagementTheme.Place((RectTransform)button.transform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -22), new Vector2(0, 22));
            if (label == "게임 종료" || label == "타이틀로 이동") button.GetComponentInChildren<TextMeshProUGUI>().color = ManagementTheme.Danger;
        }

        private void CreateKeybindRow(string actionName, string keyName)
        {
            var (row, controls) = CreateRowContainer(actionName);
            ManagementTheme.Height(row.transform, 70);
            var badge = ManagementTheme.Panel("KeyBadge", controls, ManagementTheme.Background, true);
            ManagementTheme.Place(badge, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-190, -22), new Vector2(0, 22));
            ManagementTheme.Label(badge, keyName, 21, ManagementTheme.Gold, TextAlignmentOptions.Center);
        }

        private void CreateSectionHeader(string headerText)
        {
            var header = ManagementTheme.Label(contentContainer, headerText, 20, ManagementTheme.Gold);
            ManagementTheme.Height(header.transform, 38);
        }
    }
}
