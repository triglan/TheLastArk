using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using TheLastArk.Data;
using TheLastArk.Character;

namespace TheLastArk.UI
{
    // 마우스 호버 이벤트를 전달하는 유틸리티 컴포넌트
    public class UIHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string title;
        public string category;
        public string description;

        public System.Action<string, string, string, Vector3> onHoverEnter;
        public System.Action onHoverExit;

        public void OnPointerEnter(PointerEventData eventData)
        {
            onHoverEnter?.Invoke(title, category, description, transform.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            onHoverExit?.Invoke();
        }
    }

    [ExecuteAlways]
    public partial class MainMenuUI : MonoBehaviour
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

        [Header("Character Selection - Dynamic Background & Alive Motion")]
        public Image characterBackgroundImageComponent;
        public CharacterAliveIllustration characterAliveEffect;

        [Header("Character Selection - Top Train Bar")]
        [Tooltip("상단 기차 커스텀/배치 이미지")]
        public Sprite trainCustomizationSprite;
        public Image trainCustomizationImageComponent;

        [Header("Character Selection - Right Detail UI")]
        public Image detailPortraitImageComponent;
        public TextMeshProUGUI detailNameTextComponent;
        public TextMeshProUGUI detailJobTextComponent;
        public TextMeshProUGUI detailStatsTextComponent;
        public Transform detailSynergiesContainer;
        public Transform detailSkillsContainer;
        public Button detailStartButton;

        [Header("Character Selection - Relic Gacha UI")]
        public Image relicPreviewImageComponent;
        public TextMeshProUGUI relicDescTextComponent;
        public Button relicGachaButton;
        public GameObject relicGachaModal;
        public Image gachaRouletteImageComponent;
        public TextMeshProUGUI gachaRouletteNameTextComponent;
        public TextMeshProUGUI gachaRouletteDescTextComponent;
        public Button gachaConfirmButton;
        public UIHoverTrigger drawnRelicHoverTrigger;
        private bool isGachaSpinning = false;

        [Header("Hover Tooltip UI")]
        public GameObject hoverTooltipPanel;
        public RectTransform hoverTooltipRect;
        public TextMeshProUGUI hoverTooltipTitleText;
        public TextMeshProUGUI hoverTooltipCategoryText;
        public TextMeshProUGUI hoverTooltipDescText;

        [Header("Prefabs/Settings")]
        public TMPro.TMP_FontAsset mainFont;

        public enum MainMenuEditorView
        {
            [InspectorName("캐릭터 선택 창 (Character Select)")]
            CharacterSelect,
            [InspectorName("타이틀 화면 (Title Menu)")]
            TitleMenu
        }

        [Header("Editor View Switcher (씬 뷰 작업 화면)")]
        [Tooltip("에디터 씬 뷰에서 작업할 화면을 선택합니다. 플레이 모드 진입 시에는 항상 자동으로 타이틀 메뉴부터 시작됩니다.")]
        public MainMenuEditorView editorView = MainMenuEditorView.CharacterSelect;

        private RectTransform bgRectTransform;
        private CharacterData selectedCharacterData;
        private RelicData selectedStartingRelic;
        private Dictionary<CharacterData, Outline> cardOutlines = new Dictionary<CharacterData, Outline>();

        public static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        private void Awake()
        {
            AutoBindSceneReferences();
            if (Application.isPlaying)
            {
                ShowTitlePanel();
            }
        }

        private void Start()
        {
            if (mainFont == null)
            {
                mainFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Main_Fonts");
            }

            EnsureEventSystemAndRaycaster();

            if (Application.isPlaying)
            {
                if (titlePanel == null || characterSelectionPanel == null)
                {
                    Canvas canvas = FindObjectOfType<Canvas>();
                    if (canvas != null)
                    {
                        CleanExistingPanels(canvas.transform);
                    }
                    CreateUI();
                }

                AutoBindSceneReferences();
                TryAutoLoadAssets();
                ShowTitlePanel();
            }
            else
            {
                AutoBindSceneReferences();
                TryAutoLoadAssets();
                ApplyEditorView();
            }
        }

        private bool IsOldLayoutPresent()
        {
            if (characterSelectionPanel == null) return false;
            bool hasOldRelicPanel = characterSelectionPanel.transform.Find("RelicGachaPanel") != null;
            bool hasNewStartBtn = characterSelectionPanel.transform.Find("ConfirmStartButton") != null;
            return hasOldRelicPanel || !hasNewStartBtn;
        }

        private void EnsureEventSystemAndRaycaster()
        {
            EventSystem es = FindObjectOfType<EventSystem>();
            if (es == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                es = eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }
            else
            {
                if (es.GetComponent<StandaloneInputModule>() == null && es.GetComponent<BaseInputModule>() == null)
                {
                    es.gameObject.AddComponent<StandaloneInputModule>();
                }
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        /// <summary>
        /// 씬(Scene)에 존재하는 실제 UI 오브젝트들을 자동으로 탐색하여 모든 참조 필드에 연결합니다.
        /// 사용자가 씬 뷰에서 직접 배치하고 꾸민 오브젝트들을 파괴하지 않고 완벽히 연동시킵니다.
        /// </summary>
        [ContextMenu("Auto-Bind Scene UI (현재 씬 UI 자동 연결 및 적용)")]
        public void AutoBindSceneReferences()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            Transform cTrans = canvas.transform;

            // 1. Panels
            if (titlePanel == null)
            {
                Transform t = cTrans.Find("TitlePanel");
                if (t != null) titlePanel = t.gameObject;
            }
            if (characterSelectionPanel == null)
            {
                Transform t = cTrans.Find("CharacterSelectionPanel");
                if (t != null) characterSelectionPanel = t.gameObject;
            }
            if (notificationModal == null)
            {
                Transform t = cTrans.Find("NotificationModal");
                if (t != null) notificationModal = t.gameObject;
            }
            if (relicGachaModal == null)
            {
                Transform t = cTrans.Find("RelicGachaModal");
                if (t != null) relicGachaModal = t.gameObject;
            }
            if (hoverTooltipPanel == null)
            {
                Transform t = cTrans.Find("HoverTooltipModal");
                if (t != null)
                {
                    hoverTooltipPanel = t.gameObject;
                    hoverTooltipRect = t.GetComponent<RectTransform>();
                }
            }

            // 2. Title Panel references
            if (titlePanel != null)
            {
                if (backgroundImageComponent == null)
                {
                    Transform bgTrans = titlePanel.transform.Find("BackgroundImage");
                    if (bgTrans != null) backgroundImageComponent = bgTrans.GetComponent<Image>();
                }
                if (titleImageComponent == null)
                {
                    Transform tImgTrans = titlePanel.transform.Find("TitleImage");
                    if (tImgTrans != null) titleImageComponent = tImgTrans.GetComponent<Image>();
                }
                if (titleTextComponent == null)
                {
                    Transform tTxtTrans = titlePanel.transform.Find("TitleText");
                    if (tTxtTrans != null) titleTextComponent = tTxtTrans.GetComponent<TextMeshProUGUI>();
                }
            }

            // 3. Notification references
            if (notificationModal != null && notificationTextComponent == null)
            {
                Transform msgTrans = notificationModal.transform.Find("MessageBox/MessageText");
                if (msgTrans == null) msgTrans = notificationModal.transform.Find("MessageText");
                if (msgTrans != null) notificationTextComponent = msgTrans.GetComponent<TextMeshProUGUI>();
            }

            // 4. Character Selection Panel references
            if (characterSelectionPanel != null)
            {
                Transform csTrans = characterSelectionPanel.transform;

                // 4-1. Background Illustration Layer
                Transform bgLayer = csTrans.Find("CharacterBackgroundLayer");
                if (bgLayer != null)
                {
                    Transform illuTrans = bgLayer.Find("IllustrationImage");
                    if (illuTrans != null)
                    {
                        characterBackgroundImageComponent = illuTrans.GetComponent<Image>();
                        characterAliveEffect = illuTrans.GetComponent<CharacterAliveIllustration>();
                        if (characterAliveEffect == null)
                        {
                            characterAliveEffect = illuTrans.gameObject.AddComponent<CharacterAliveIllustration>();
                        }
                    }
                }

                // 4-2. Train Customization Header
                Transform trainHeader = csTrans.Find("TrainCustomizationHeader");
                if (trainHeader != null)
                {
                    Transform trainImg = trainHeader.Find("TrainImage");
                    if (trainImg != null)
                    {
                        trainCustomizationImageComponent = trainImg.GetComponent<Image>();
                    }
                }

                // 4-3. Character Grid / List Area
                Transform gridPanel = csTrans.Find("CharacterGridPanel");
                if (gridPanel != null)
                {
                    Transform gridArea = gridPanel.Find("GridArea");
                    if (gridArea != null) characterListContainer = gridArea;
                }
                if (characterListContainer == null)
                {
                    Transform gridArea = csTrans.Find("GridArea");
                    if (gridArea != null) characterListContainer = gridArea;
                }

                if (characterListContainer != null)
                {
                    GridLayoutGroup gridLayout = characterListContainer.GetComponent<GridLayoutGroup>();
                    if (gridLayout != null)
                    {
                        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                        gridLayout.constraintCount = 5;
                        gridLayout.cellSize = new Vector2(152, 185);
                        gridLayout.spacing = new Vector2(14, 14);
                    }
                }

                // 4-4. Character Detail Area
                Transform detailPanel = csTrans.Find("CharacterDetailPanel");
                if (detailPanel == null) detailPanel = csTrans.Find("RightDetailPanel");
                if (detailPanel != null)
                {
                    Transform portraitImg = detailPanel.Find("DetailPortrait/DetailPortraitImage");
                    if (portraitImg == null) portraitImg = detailPanel.Find("DetailPortraitImage");
                    if (portraitImg != null) detailPortraitImageComponent = portraitImg.GetComponent<Image>();

                    Transform nameTrans = detailPanel.Find("InfoContainer/DetailNameText");
                    if (nameTrans == null) nameTrans = detailPanel.Find("DetailNameText");
                    if (nameTrans != null) detailNameTextComponent = nameTrans.GetComponent<TextMeshProUGUI>();

                    Transform jobTrans = detailPanel.Find("InfoContainer/DetailJobText");
                    if (jobTrans == null) jobTrans = detailPanel.Find("DetailJobText");
                    if (jobTrans != null) detailJobTextComponent = jobTrans.GetComponent<TextMeshProUGUI>();

                    Transform statsTrans = detailPanel.Find("InfoContainer/DetailStatsText");
                    if (statsTrans == null) statsTrans = detailPanel.Find("DetailStatsText");
                    if (statsTrans != null) detailStatsTextComponent = statsTrans.GetComponent<TextMeshProUGUI>();

                    Transform synCont = detailPanel.Find("SynergiesPanel/SynergiesContainer");
                    if (synCont == null) synCont = detailPanel.Find("SynergiesContainer");
                    if (synCont != null)
                    {
                        detailSynergiesContainer = synCont;
                        var oldV = synCont.GetComponent<VerticalLayoutGroup>();
                        if (oldV != null) oldV.enabled = false;

                        HorizontalLayoutGroup hg = synCont.GetComponent<HorizontalLayoutGroup>();
                        if (hg == null) hg = synCont.gameObject.AddComponent<HorizontalLayoutGroup>();
                        hg.enabled = true;
                        hg.spacing = 8f;
                        hg.childAlignment = TextAnchor.MiddleLeft;
                        hg.childControlWidth = false;
                        hg.childControlHeight = false;
                        hg.childForceExpandWidth = false;
                        hg.childForceExpandHeight = false;
                    }

                    Transform skillCont = detailPanel.Find("SkillsContainer");
                    if (skillCont != null)
                    {
                        detailSkillsContainer = skillCont;
                        var existingHL = skillCont.GetComponent<HorizontalLayoutGroup>();
                        if (existingHL != null) existingHL.enabled = false;
                    }
                }

                // 4-5. Action Buttons & Relic Slot
                Transform startBtn = csTrans.Find("ConfirmStartButton");
                if (startBtn == null && detailPanel != null) startBtn = detailPanel.Find("ConfirmStartButton");
                if (startBtn != null) detailStartButton = startBtn.GetComponent<Button>();

                Transform gachaBtn = csTrans.Find("RelicGachaButton");
                if (gachaBtn != null) relicGachaButton = gachaBtn.GetComponent<Button>();

                Transform relicSlot = csTrans.Find("DrawnRelicSlot");
                if (relicSlot != null)
                {
                    Transform relicImg = relicSlot.Find("DrawnRelicImage");
                    if (relicImg != null) relicPreviewImageComponent = relicImg.GetComponent<Image>();

                    drawnRelicHoverTrigger = relicSlot.GetComponent<UIHoverTrigger>();
                    if (drawnRelicHoverTrigger == null)
                    {
                        drawnRelicHoverTrigger = relicSlot.gameObject.AddComponent<UIHoverTrigger>();
                    }
                    drawnRelicHoverTrigger.onHoverEnter = ShowHoverTooltip;
                    drawnRelicHoverTrigger.onHoverExit = HideHoverTooltip;
                }
            }

            // 5. Relic Gacha Modal references
            if (relicGachaModal != null)
            {
                Transform box = relicGachaModal.transform.Find("GachaBox");
                if (box != null)
                {
                    Transform rImg = box.Find("IconFrame/GachaRouletteImage");
                    if (rImg != null) gachaRouletteImageComponent = rImg.GetComponent<Image>();

                    Transform rName = box.Find("GachaRouletteName");
                    if (rName != null) gachaRouletteNameTextComponent = rName.GetComponent<TextMeshProUGUI>();

                    Transform rDesc = box.Find("GachaRouletteDesc");
                    if (rDesc != null) gachaRouletteDescTextComponent = rDesc.GetComponent<TextMeshProUGUI>();

                    Transform confirmBtn = box.Find("GachaConfirmButton");
                    if (confirmBtn != null) gachaConfirmButton = confirmBtn.GetComponent<Button>();
                }
            }

            // 6. Hover Tooltip Modal references
            if (hoverTooltipPanel != null)
            {
                Transform t = hoverTooltipPanel.transform.Find("Title");
                if (t != null) hoverTooltipTitleText = t.GetComponent<TextMeshProUGUI>();

                Transform c = hoverTooltipPanel.transform.Find("Category");
                if (c != null) hoverTooltipCategoryText = c.GetComponent<TextMeshProUGUI>();

                Transform d = hoverTooltipPanel.transform.Find("Desc");
                if (d != null) hoverTooltipDescText = d.GetComponent<TextMeshProUGUI>();
            }

            // 7. Rebind button listeners to the detected objects
            BindAllButtonListeners();
        }

        private void BindAllButtonListeners()
        {
            if (titlePanel != null)
            {
                Transform btnContainer = titlePanel.transform.Find("ButtonContainer");
                if (btnContainer != null)
                {
                    RebindButton(btnContainer, "Btn_새 게임", OnNewGameClicked);
                    RebindButton(btnContainer, "Btn_불러오기", OnLoadGameClicked);
                    RebindButton(btnContainer, "Btn_특전", OnPerksClicked);
                    RebindButton(btnContainer, "Btn_설정", OnSettingsClicked);
                    RebindButton(btnContainer, "Btn_게임 종료", OnQuitGameClicked);
                }
            }

            if (characterSelectionPanel != null)
            {
                // Find ConfirmStartButton wherever it is located
                if (detailStartButton == null)
                {
                    Transform startBtnTrans = characterSelectionPanel.transform.Find("ConfirmStartButton");
                    if (startBtnTrans == null)
                    {
                        Transform detailPanel = characterSelectionPanel.transform.Find("CharacterDetailPanel");
                        if (detailPanel == null) detailPanel = characterSelectionPanel.transform.Find("RightDetailPanel");
                        if (detailPanel != null) startBtnTrans = detailPanel.Find("ConfirmStartButton");
                    }
                    if (startBtnTrans != null) detailStartButton = startBtnTrans.GetComponent<Button>();
                }
                if (detailStartButton != null)
                {
                    detailStartButton.onClick.RemoveAllListeners();
                    detailStartButton.onClick.AddListener(OnStartRunConfirmed);
                }

                // Find RelicGachaButton
                if (relicGachaButton == null)
                {
                    Transform gachaBtnTrans = characterSelectionPanel.transform.Find("RelicGachaButton");
                    if (gachaBtnTrans != null) relicGachaButton = gachaBtnTrans.GetComponent<Button>();
                }
                if (relicGachaButton != null)
                {
                    relicGachaButton.onClick.RemoveAllListeners();
                    relicGachaButton.onClick.AddListener(OnRelicGachaClicked);
                }

                // Find BackButton
                Transform backBtnTrans = characterSelectionPanel.transform.Find("BackButton");
                if (backBtnTrans != null)
                {
                    Button btn = backBtnTrans.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(ShowTitlePanel);
                    }
                }

                // Train Car Slots
                if (trainCustomizationImageComponent != null)
                {
                    foreach (Transform child in trainCustomizationImageComponent.transform)
                    {
                        Button btn = child.GetComponent<Button>();
                        if (btn != null)
                        {
                            string carName = child.name.Replace("CarSlot_", "");
                            btn.onClick.RemoveAllListeners();
                            btn.onClick.AddListener(() => OnTrainCarClicked(carName));
                        }
                    }
                }
            }

            // Gacha Confirm Button
            if (gachaConfirmButton != null)
            {
                gachaConfirmButton.onClick.RemoveAllListeners();
                gachaConfirmButton.onClick.AddListener(() =>
                {
                    if (relicGachaModal != null) relicGachaModal.SetActive(false);
                });
            }

            // Notification Modal buttons (OKButton 등 모든 닫기 버튼 바인딩)
            if (notificationModal != null)
            {
                Button[] modalButtons = notificationModal.GetComponentsInChildren<Button>(true);
                foreach (var b in modalButtons)
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() =>
                    {
                        if (notificationModal != null) notificationModal.SetActive(false);
                    });
                }
            }
        }

        private void RebindButton(Transform container, string buttonName, UnityEngine.Events.UnityAction action)
        {
            Transform t = container.Find(buttonName);
            if (t != null)
            {
                Button btn = t.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(action);
                }
            }
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                HandleParallaxEffect();
                UpdateHoverTooltipPosition();
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                ApplyVisualAssets();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        ApplyEditorView();
                    }
                };
#endif
            }
        }

        public void ApplyEditorView()
        {
            if (Application.isPlaying) return;

            if (editorView == MainMenuEditorView.CharacterSelect)
            {
                if (titlePanel != null) titlePanel.SetActive(false);
                if (characterSelectionPanel != null)
                {
                    characterSelectionPanel.SetActive(true);
                    characterSelectionPanel.transform.SetAsLastSibling();

                    PopulateCharacterList();
                }
                if (notificationModal != null) notificationModal.SetActive(false);
                if (relicGachaModal != null) relicGachaModal.SetActive(false);
                HideHoverTooltip();
            }
            else
            {
                if (characterSelectionPanel != null) characterSelectionPanel.SetActive(false);
                if (titlePanel != null) titlePanel.SetActive(true);
                if (notificationModal != null) notificationModal.SetActive(false);
                if (relicGachaModal != null) relicGachaModal.SetActive(false);
                HideHoverTooltip();
            }
        }

        [ContextMenu("View: Character Select (캐릭터 선택 창 보기)")]
        public void SwitchToCharacterSelectView()
        {
            editorView = MainMenuEditorView.CharacterSelect;
            ApplyEditorView();
        }

        [ContextMenu("View: Title Menu (타이틀 화면 보기)")]
        public void SwitchToTitleMenuView()
        {
            editorView = MainMenuEditorView.TitleMenu;
            ApplyEditorView();
        }

        [ContextMenu("Preview: Populate Characters (프리뷰 카드 채우기)")]
        public void PopulatePreviewInEditor()
        {
            PopulateCharacterList();
        }

        [ContextMenu("Preview: Clear Preview Cards (프리뷰 카드 비우기)")]
        public void ClearPreviewInEditor()
        {
            if (characterListContainer != null)
            {
                for (int i = characterListContainer.childCount - 1; i >= 0; i--)
                {
                    SafeDestroy(characterListContainer.GetChild(i).gameObject);
                }
            }
            if (detailSynergiesContainer != null)
            {
                for (int i = detailSynergiesContainer.childCount - 1; i >= 0; i--)
                {
                    detailSynergiesContainer.GetChild(i).gameObject.SetActive(false);
                    SafeDestroy(detailSynergiesContainer.GetChild(i).gameObject);
                }
            }
            if (detailSkillsContainer != null)
            {
                for (int i = detailSkillsContainer.childCount - 1; i >= 0; i--)
                {
                    SafeDestroy(detailSkillsContainer.GetChild(i).gameObject);
                }
            }
            if (detailPortraitImageComponent != null)
            {
                detailPortraitImageComponent.sprite = null;
                detailPortraitImageComponent.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
            if (detailNameTextComponent != null) detailNameTextComponent.text = "캐릭터 이름";
            if (detailJobTextComponent != null) detailJobTextComponent.text = "<color=#7FFFD4>방주 승무원</color>";
            if (detailStatsTextComponent != null) detailStatsTextComponent.text = "기본 능력치 정보";

            if (characterAliveEffect != null) characterAliveEffect.SetIllustration(null);
            else if (characterBackgroundImageComponent != null) characterBackgroundImageComponent.sprite = null;

            cardOutlines.Clear();
            selectedCharacterData = null;
        }

        [ContextMenu("Build Main Menu Scene UI (초기화 및 재생성)")]
        public void BuildUIInEditor()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                CleanExistingPanels(canvas.transform);
            }
            CreateUI();
            TryAutoLoadAssets();
            PopulateCharacterList();
            ApplyEditorView();
        }

        [ContextMenu("Clear Main Menu Scene UI")]
        public void ClearUIInEditor()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                CleanExistingPanels(canvas.transform);
            }
        }

        private void HandleParallaxEffect()
        {
            if (!enableParallax || bgRectTransform == null) return;
            if (titlePanel == null || !titlePanel.activeSelf) return;

            float mouseX = Mathf.Clamp01(Input.mousePosition.x / Screen.width);
            float mouseY = Mathf.Clamp01(Input.mousePosition.y / Screen.height);

            float offsetX = mouseX - 0.5f;
            float offsetY = mouseY - 0.5f;

            Vector2 targetPosition = new Vector2(backgroundOffsetX - offsetX * parallaxStrength, -offsetY * parallaxStrength);

            bgRectTransform.anchoredPosition = Vector2.Lerp(
                bgRectTransform.anchoredPosition,
                targetPosition,
                Time.deltaTime * parallaxSmoothness
            );
        }

        private void UpdateHoverTooltipPosition()
        {
            if (hoverTooltipPanel == null || !hoverTooltipPanel.activeSelf || hoverTooltipRect == null) return;

            Vector2 mousePos = Input.mousePosition;
            float targetX = mousePos.x + 20f;
            float targetY = mousePos.y - 20f;

            float screenW = Screen.width > 0 ? Screen.width : 1920f;
            float screenH = Screen.height > 0 ? Screen.height : 1080f;

            float w = hoverTooltipRect.sizeDelta.x > 0 ? hoverTooltipRect.sizeDelta.x : 320f;
            float h = hoverTooltipRect.sizeDelta.y > 0 ? hoverTooltipRect.sizeDelta.y : 180f;

            if (targetX + w > screenW)
            {
                targetX = mousePos.x - w - 10f;
            }
            if (targetY - h < 0)
            {
                targetY = mousePos.y + h + 10f;
            }

            hoverTooltipRect.position = new Vector3(targetX, targetY, 0);
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
            if (Camera.main == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                Camera cam = camObj.AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
                camObj.AddComponent<AudioListener>();
            }

            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
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

            CleanExistingPanels(canvas.transform);

            // 1. Title Panel
            titlePanel = new GameObject("TitlePanel");
            titlePanel.transform.SetParent(canvas.transform, false);
            RectTransform titlePanelRect = titlePanel.AddComponent<RectTransform>();
            titlePanelRect.anchorMin = Vector2.zero;
            titlePanelRect.anchorMax = Vector2.one;
            titlePanelRect.sizeDelta = Vector2.zero;

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

            // Attach Sky Effects Layer (All In 1 Sprite Shader Starfield & Shooting Stars)
            MainMenuSkyEffects skyEffects = bgObj.AddComponent<MainMenuSkyEffects>();
            skyEffects.Initialize(bgObj.transform);

            GameObject titleAreaObj = new GameObject("TitleArea");
            titleAreaObj.transform.SetParent(titlePanel.transform, false);
            RectTransform titleAreaRect = titleAreaObj.AddComponent<RectTransform>();
            titleAreaRect.anchorMin = new Vector2(0.5f, 0.72f);
            titleAreaRect.anchorMax = new Vector2(0.5f, 0.72f);
            titleAreaRect.pivot = new Vector2(0.5f, 0.5f);
            titleAreaRect.anchoredPosition = Vector2.zero;
            titleAreaRect.sizeDelta = new Vector2(1600, 440);

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

            // 2. Buttons Container
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

            CreateMenuButton(btnContainerObj.transform, "새 게임", OnNewGameClicked, new Color(0.2f, 0.45f, 0.35f, 1f));
            CreateMenuButton(btnContainerObj.transform, "불러오기", OnLoadGameClicked, new Color(0.2f, 0.25f, 0.35f, 1f));
            CreateMenuButton(btnContainerObj.transform, "특전", OnPerksClicked, new Color(0.28f, 0.22f, 0.35f, 1f));
            CreateMenuButton(btnContainerObj.transform, "설정", OnSettingsClicked, new Color(0.22f, 0.25f, 0.3f, 1f));
            CreateMenuButton(btnContainerObj.transform, "게임 종료", OnQuitGameClicked, new Color(0.35f, 0.18f, 0.18f, 1f));

            // 3. Modals & Panels
            CreateNotificationModal(canvas.transform);
            CreateCharacterSelectionPanel(canvas.transform);
            CreateRelicGachaModal(canvas.transform);
            CreateHoverTooltipModal(canvas.transform);
        }

        private void CleanExistingPanels(Transform canvasTransform)
        {
            foreach (string panelName in new string[] { "TitlePanel", "CharacterSelectionPanel", "NotificationModal", "HoverTooltipModal", "RelicGachaModal" })
            {
                Transform existing = canvasTransform.Find(panelName);
                if (existing != null)
                {
                    SafeDestroy(existing.gameObject);
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
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(baseAccentColor.r, baseAccentColor.g, baseAccentColor.b, 0.45f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

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
            tmp.raycastTarget = false;
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

        private void CreateHoverTooltipModal(Transform parent)
        {
            hoverTooltipPanel = new GameObject("HoverTooltipModal");
            hoverTooltipPanel.transform.SetParent(parent, false);

            hoverTooltipRect = hoverTooltipPanel.AddComponent<RectTransform>();
            hoverTooltipRect.pivot = new Vector2(0f, 1f); // 좌상단 피벗
            hoverTooltipRect.sizeDelta = new Vector2(340, 200);

            Image bg = hoverTooltipPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.16f, 0.98f);
            bg.raycastTarget = false;

            Outline outline = hoverTooltipPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.78f, 0.35f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            VerticalLayoutGroup vgroup = hoverTooltipPanel.AddComponent<VerticalLayoutGroup>();
            vgroup.padding = new RectOffset(14, 14, 12, 12);
            vgroup.spacing = 6;
            vgroup.childAlignment = TextAnchor.UpperLeft;
            vgroup.childControlWidth = true;
            vgroup.childControlHeight = false;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(hoverTooltipPanel.transform, false);
            hoverTooltipTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            hoverTooltipTitleText.fontSize = 20;
            hoverTooltipTitleText.fontStyle = FontStyles.Bold;
            hoverTooltipTitleText.color = new Color(0.95f, 0.82f, 0.35f, 1f);
            hoverTooltipTitleText.raycastTarget = false;
            if (mainFont != null) hoverTooltipTitleText.font = mainFont;

            // Category
            GameObject catObj = new GameObject("Category");
            catObj.transform.SetParent(hoverTooltipPanel.transform, false);
            hoverTooltipCategoryText = catObj.AddComponent<TextMeshProUGUI>();
            hoverTooltipCategoryText.fontSize = 14;
            hoverTooltipCategoryText.color = new Color(0.5f, 0.85f, 0.95f, 1f);
            hoverTooltipCategoryText.raycastTarget = false;
            if (mainFont != null) hoverTooltipCategoryText.font = mainFont;

            // Description
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(hoverTooltipPanel.transform, false);
            hoverTooltipDescText = descObj.AddComponent<TextMeshProUGUI>();
            hoverTooltipDescText.fontSize = 15;
            hoverTooltipDescText.lineSpacing = 4;
            hoverTooltipDescText.color = new Color(0.9f, 0.92f, 0.95f, 1f);
            hoverTooltipDescText.raycastTarget = false;
            if (mainFont != null) hoverTooltipDescText.font = mainFont;

            hoverTooltipPanel.SetActive(false);
        }

        public void ShowHoverTooltip(string title, string category, string description, Vector3 worldPos)
        {
            if (hoverTooltipPanel == null) return;

            if (hoverTooltipTitleText != null) hoverTooltipTitleText.text = title;
            if (hoverTooltipCategoryText != null) hoverTooltipCategoryText.text = category;
            if (hoverTooltipDescText != null) hoverTooltipDescText.text = description;

            hoverTooltipPanel.SetActive(true);
            hoverTooltipPanel.transform.SetAsLastSibling();
            UpdateHoverTooltipPosition();
        }

        public void HideHoverTooltip()
        {
            if (hoverTooltipPanel != null)
            {
                hoverTooltipPanel.SetActive(false);
            }
        }

        private void CreateRelicGachaModal(Transform parent)
        {
            relicGachaModal = new GameObject("RelicGachaModal");
            relicGachaModal.transform.SetParent(parent, false);

            RectTransform modalRect = relicGachaModal.AddComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.sizeDelta = Vector2.zero;

            Image bg = relicGachaModal.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.80f);
            bg.raycastTarget = true;

            GameObject boxObj = new GameObject("GachaBox");
            boxObj.transform.SetParent(relicGachaModal.transform, false);
            RectTransform boxRect = boxObj.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(560, 360);

            Image boxImg = boxObj.AddComponent<Image>();
            boxImg.color = new Color(0.12f, 0.10f, 0.18f, 0.98f);
            boxImg.raycastTarget = true;

            Outline boxOutline = boxObj.AddComponent<Outline>();
            boxOutline.effectColor = new Color(0.75f, 0.55f, 0.95f, 0.9f);
            boxOutline.effectDistance = new Vector2(2, -2);

            // Title
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(boxObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.82f);
            titleRect.anchorMax = new Vector2(0.95f, 0.96f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "[시작 특전 유물 소환]";
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.95f, 0.82f, 0.35f, 1f);
            titleText.raycastTarget = false;
            if (mainFont != null) titleText.font = mainFont;

            // Icon Frame (Center Top)
            GameObject iconFrameObj = new GameObject("IconFrame");
            iconFrameObj.transform.SetParent(boxObj.transform, false);
            RectTransform iconFrameRect = iconFrameObj.AddComponent<RectTransform>();
            iconFrameRect.anchorMin = new Vector2(0.5f, 0.58f);
            iconFrameRect.anchorMax = new Vector2(0.5f, 0.58f);
            iconFrameRect.pivot = new Vector2(0.5f, 0.5f);
            iconFrameRect.sizeDelta = new Vector2(100, 100);

            Image iconFrameBg = iconFrameObj.AddComponent<Image>();
            iconFrameBg.color = new Color(0.2f, 0.16f, 0.28f, 0.9f);

            Outline iconOutline = iconFrameObj.AddComponent<Outline>();
            iconOutline.effectColor = new Color(0.85f, 0.65f, 0.95f, 0.9f);

            GameObject iconImgObj = new GameObject("GachaRouletteImage");
            iconImgObj.transform.SetParent(iconFrameObj.transform, false);
            RectTransform iRect = iconImgObj.AddComponent<RectTransform>();
            iRect.anchorMin = Vector2.zero;
            iRect.anchorMax = Vector2.one;
            iRect.sizeDelta = Vector2.zero;

            gachaRouletteImageComponent = iconImgObj.AddComponent<Image>();
            gachaRouletteImageComponent.preserveAspect = true;

            // Name
            GameObject nameObj = new GameObject("GachaRouletteName");
            nameObj.transform.SetParent(boxObj.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.05f, 0.36f);
            nameRect.anchorMax = new Vector2(0.95f, 0.46f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            gachaRouletteNameTextComponent = nameObj.AddComponent<TextMeshProUGUI>();
            gachaRouletteNameTextComponent.text = "유물 뽑기를 시작하세요!";
            gachaRouletteNameTextComponent.fontSize = 22;
            gachaRouletteNameTextComponent.fontStyle = FontStyles.Bold;
            gachaRouletteNameTextComponent.alignment = TextAlignmentOptions.Center;
            gachaRouletteNameTextComponent.color = Color.white;
            if (mainFont != null) gachaRouletteNameTextComponent.font = mainFont;

            // Desc
            GameObject descObj = new GameObject("GachaRouletteDesc");
            descObj.transform.SetParent(boxObj.transform, false);
            RectTransform descRect = descObj.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.05f, 0.20f);
            descRect.anchorMax = new Vector2(0.95f, 0.35f);
            descRect.offsetMin = Vector2.zero;
            descRect.offsetMax = Vector2.zero;

            gachaRouletteDescTextComponent = descObj.AddComponent<TextMeshProUGUI>();
            gachaRouletteDescTextComponent.text = "룰렛이 돌아가고 무작위 보너스 유물이 결정됩니다.";
            gachaRouletteDescTextComponent.fontSize = 15;
            gachaRouletteDescTextComponent.alignment = TextAlignmentOptions.Center;
            gachaRouletteDescTextComponent.color = new Color(0.85f, 0.88f, 0.95f, 0.9f);
            if (mainFont != null) gachaRouletteDescTextComponent.font = mainFont;

            // Confirm Button
            GameObject confirmBtnObj = new GameObject("GachaConfirmButton");
            confirmBtnObj.transform.SetParent(boxObj.transform, false);
            RectTransform confirmRect = confirmBtnObj.AddComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.3f, 0.04f);
            confirmRect.anchorMax = new Vector2(0.7f, 0.17f);
            confirmRect.offsetMin = Vector2.zero;
            confirmRect.offsetMax = Vector2.zero;

            Image confirmImg = confirmBtnObj.AddComponent<Image>();
            confirmImg.color = new Color(0.2f, 0.45f, 0.35f, 1f);

            gachaConfirmButton = confirmBtnObj.AddComponent<Button>();
            gachaConfirmButton.onClick.AddListener(() => relicGachaModal.SetActive(false));

            GameObject cTextObj = new GameObject("Text");
            cTextObj.transform.SetParent(confirmBtnObj.transform, false);
            RectTransform cTRect = cTextObj.AddComponent<RectTransform>();
            cTRect.anchorMin = Vector2.zero;
            cTRect.anchorMax = Vector2.one;
            cTRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI cText = cTextObj.AddComponent<TextMeshProUGUI>();
            cText.text = "확인";
            cText.fontSize = 20;
            cText.alignment = TextAlignmentOptions.Center;
            cText.color = Color.white;
            if (mainFont != null) cText.font = mainFont;

            relicGachaModal.SetActive(false);
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

            // 0. DYNAMIC CHARACTER BACKGROUND LAYER (일러스트 배경 및 살아있는 모션)
            GameObject bgLayerObj = new GameObject("CharacterBackgroundLayer");
            bgLayerObj.transform.SetParent(characterSelectionPanel.transform, false);
            RectTransform bgLayerRect = bgLayerObj.AddComponent<RectTransform>();
            bgLayerRect.anchorMin = Vector2.zero;
            bgLayerRect.anchorMax = Vector2.one;
            bgLayerRect.sizeDelta = Vector2.zero;

            GameObject bgIllustrationObj = new GameObject("IllustrationImage");
            bgIllustrationObj.transform.SetParent(bgLayerObj.transform, false);
            RectTransform illustRect = bgIllustrationObj.AddComponent<RectTransform>();
            illustRect.anchorMin = Vector2.zero;
            illustRect.anchorMax = Vector2.one;
            illustRect.sizeDelta = new Vector2(160, 100);
            illustRect.localScale = Vector3.one;

            characterBackgroundImageComponent = bgIllustrationObj.AddComponent<Image>();
            characterBackgroundImageComponent.preserveAspect = true;
            characterBackgroundImageComponent.raycastTarget = false;
            characterBackgroundImageComponent.color = Color.white;

            characterAliveEffect = bgIllustrationObj.AddComponent<CharacterAliveIllustration>();

            // 가독성을 위한 비네트 / 틴트 오버레이
            GameObject overlayObj = new GameObject("VignetteOverlay");
            overlayObj.transform.SetParent(bgLayerObj.transform, false);
            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            Image overlayImg = overlayObj.AddComponent<Image>();
            overlayImg.color = new Color(0.05f, 0.06f, 0.09f, 0.45f);
            overlayImg.raycastTarget = false;

            // 1. TOP: Train Bar (50% 높이 확대: Y 0.78 ~ 0.96)
            GameObject trainHeaderObj = new GameObject("TrainCustomizationHeader");
            trainHeaderObj.transform.SetParent(characterSelectionPanel.transform, false);
            RectTransform trainHeaderRect = trainHeaderObj.AddComponent<RectTransform>();
            trainHeaderRect.anchorMin = new Vector2(0.04f, 0.78f);
            trainHeaderRect.anchorMax = new Vector2(0.96f, 0.96f);
            trainHeaderRect.offsetMin = Vector2.zero;
            trainHeaderRect.offsetMax = Vector2.zero;

            Image trainHeaderBg = trainHeaderObj.AddComponent<Image>();
            trainHeaderBg.color = new Color(0.12f, 0.15f, 0.2f, 0.95f);
            trainHeaderBg.raycastTarget = false;

            Outline trainHeaderOutline = trainHeaderObj.AddComponent<Outline>();
            trainHeaderOutline.effectColor = new Color(0.7f, 0.6f, 0.3f, 0.6f);
            trainHeaderOutline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject trainTitleObj = new GameObject("TitleText");
            trainTitleObj.transform.SetParent(trainHeaderObj.transform, false);
            RectTransform trainTitleRect = trainTitleObj.AddComponent<RectTransform>();
            trainTitleRect.anchorMin = new Vector2(0.02f, 0.5f);
            trainTitleRect.anchorMax = new Vector2(0.28f, 0.5f);
            trainTitleRect.pivot = new Vector2(0f, 0.5f);
            trainTitleRect.sizeDelta = new Vector2(300, 40);

            TextMeshProUGUI trainTitleText = trainTitleObj.AddComponent<TextMeshProUGUI>();
            trainTitleText.text = "[방주 열차 편성 & 커스텀]";
            trainTitleText.fontSize = 22;
            trainTitleText.fontStyle = FontStyles.Bold;
            trainTitleText.color = new Color(0.95f, 0.8f, 0.3f, 1f);
            trainTitleText.raycastTarget = false;
            if (mainFont != null) trainTitleText.font = mainFont;

            GameObject trainImgObj = new GameObject("TrainImage");
            trainImgObj.transform.SetParent(trainHeaderObj.transform, false);
            RectTransform trainImgRect = trainImgObj.AddComponent<RectTransform>();
            trainImgRect.anchorMin = new Vector2(0.28f, 0.05f);
            trainImgRect.anchorMax = new Vector2(0.98f, 0.95f);
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

            // 2. LEFT-CENTER: Compact Character Grid (Y 0.12 ~ 0.76)
            GameObject charGridPanel = new GameObject("CharacterGridPanel");
            charGridPanel.transform.SetParent(characterSelectionPanel.transform, false);
            RectTransform charGridPanelRect = charGridPanel.AddComponent<RectTransform>();
            charGridPanelRect.anchorMin = new Vector2(0.04f, 0.12f);
            charGridPanelRect.anchorMax = new Vector2(0.53f, 0.76f);
            charGridPanelRect.offsetMin = Vector2.zero;
            charGridPanelRect.offsetMax = Vector2.zero;

            Image charGridBg = charGridPanel.AddComponent<Image>();
            charGridBg.color = new Color(0.1f, 0.12f, 0.16f, 0.95f);
            charGridBg.raycastTarget = false;

            Outline charGridOutline = charGridPanel.AddComponent<Outline>();
            charGridOutline.effectColor = new Color(0.3f, 0.35f, 0.45f, 0.5f);

            GameObject gridHeaderObj = new GameObject("GridHeader");
            gridHeaderObj.transform.SetParent(charGridPanel.transform, false);
            RectTransform gridHeaderRect = gridHeaderObj.AddComponent<RectTransform>();
            gridHeaderRect.anchorMin = new Vector2(0.03f, 0.92f);
            gridHeaderRect.anchorMax = new Vector2(0.97f, 0.98f);
            gridHeaderRect.offsetMin = Vector2.zero;
            gridHeaderRect.offsetMax = Vector2.zero;

            TextMeshProUGUI gridHeaderText = gridHeaderObj.AddComponent<TextMeshProUGUI>();
            gridHeaderText.text = "[승무원 명단 (리더 선택)]";
            gridHeaderText.fontSize = 22;
            gridHeaderText.fontStyle = FontStyles.Bold;
            gridHeaderText.color = new Color(0.9f, 0.92f, 0.96f, 1f);
            gridHeaderText.raycastTarget = false;
            if (mainFont != null) gridHeaderText.font = mainFont;

            GameObject gridArea = new GameObject("GridArea");
            gridArea.transform.SetParent(charGridPanel.transform, false);
            RectTransform gridRect = gridArea.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.03f, 0.03f);
            gridRect.anchorMax = new Vector2(0.97f, 0.91f);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;

            GridLayoutGroup gridLayout = gridArea.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(152, 185);
            gridLayout.spacing = new Vector2(14, 14);
            gridLayout.padding = new RectOffset(10, 10, 10, 10);
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 5;

            characterListContainer = gridArea.transform;

            // 3. RIGHT: Character Details Panel (유물 영역 통합으로 높이 전면 확장: Y 0.12 ~ 0.76)
            GameObject detailPanel = new GameObject("CharacterDetailPanel");
            detailPanel.transform.SetParent(characterSelectionPanel.transform, false);
            RectTransform detailPanelRect = detailPanel.AddComponent<RectTransform>();
            detailPanelRect.anchorMin = new Vector2(0.55f, 0.12f);
            detailPanelRect.anchorMax = new Vector2(0.96f, 0.76f);
            detailPanelRect.offsetMin = Vector2.zero;
            detailPanelRect.offsetMax = Vector2.zero;

            Image detailBg = detailPanel.AddComponent<Image>();
            detailBg.color = new Color(0.11f, 0.13f, 0.18f, 0.95f);
            detailBg.raycastTarget = false;

            Outline detailOutline = detailPanel.AddComponent<Outline>();
            detailOutline.effectColor = new Color(0.35f, 0.4f, 0.55f, 0.6f);
            detailOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Large Standing Illustration Frame (상단 좌측)
            GameObject detailPortObj = new GameObject("DetailPortrait");
            detailPortObj.transform.SetParent(detailPanel.transform, false);
            RectTransform detailPortRect = detailPortObj.AddComponent<RectTransform>();
            detailPortRect.anchorMin = new Vector2(0.03f, 0.44f);
            detailPortRect.anchorMax = new Vector2(0.33f, 0.96f);
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

            // Info & Stats Container (상단 중앙)
            GameObject infoContainer = new GameObject("InfoContainer");
            infoContainer.transform.SetParent(detailPanel.transform, false);
            RectTransform infoRect = infoContainer.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.34f, 0.44f);
            infoRect.anchorMax = new Vector2(0.68f, 0.96f);
            infoRect.offsetMin = Vector2.zero;
            infoRect.offsetMax = Vector2.zero;

            GameObject nameObj = new GameObject("DetailNameText");
            nameObj.transform.SetParent(infoContainer.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.82f);
            nameRect.anchorMax = new Vector2(1, 1f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            detailNameTextComponent = nameObj.AddComponent<TextMeshProUGUI>();
            detailNameTextComponent.text = "선택된 캐릭터";
            detailNameTextComponent.fontSize = 26;
            detailNameTextComponent.fontStyle = FontStyles.Bold;
            detailNameTextComponent.color = new Color(0.95f, 0.82f, 0.35f, 1f);
            detailNameTextComponent.raycastTarget = false;
            if (mainFont != null) detailNameTextComponent.font = mainFont;

            GameObject jobObj = new GameObject("DetailJobText");
            jobObj.transform.SetParent(infoContainer.transform, false);
            RectTransform jobRect = jobObj.AddComponent<RectTransform>();
            jobRect.anchorMin = new Vector2(0, 0.68f);
            jobRect.anchorMax = new Vector2(1, 0.82f);
            jobRect.offsetMin = Vector2.zero;
            jobRect.offsetMax = Vector2.zero;

            detailJobTextComponent = jobObj.AddComponent<TextMeshProUGUI>();
            detailJobTextComponent.text = "승무원 계급 / 역할";
            detailJobTextComponent.fontSize = 17;
            detailJobTextComponent.color = new Color(0.7f, 0.85f, 0.95f, 1f);
            detailJobTextComponent.raycastTarget = false;
            if (mainFont != null) detailJobTextComponent.font = mainFont;

            GameObject statsObj = new GameObject("DetailStatsText");
            statsObj.transform.SetParent(infoContainer.transform, false);
            RectTransform statsRect = statsObj.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 0f);
            statsRect.anchorMax = new Vector2(1, 0.68f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;

            detailStatsTextComponent = statsObj.AddComponent<TextMeshProUGUI>();
            detailStatsTextComponent.text = "능력치 예시";
            detailStatsTextComponent.fontSize = 15;
            detailStatsTextComponent.lineSpacing = 3;
            detailStatsTextComponent.color = new Color(0.9f, 0.92f, 0.95f, 1f);
            detailStatsTextComponent.raycastTarget = false;
            if (mainFont != null) detailStatsTextComponent.font = mainFont;

            // ─── Synergies Area (능력치 오른쪽 상단) ───
            GameObject synPanelObj = new GameObject("SynergiesPanel");
            synPanelObj.transform.SetParent(detailPanel.transform, false);
            RectTransform synPanelRect = synPanelObj.AddComponent<RectTransform>();
            synPanelRect.anchorMin = new Vector2(0.69f, 0.44f);
            synPanelRect.anchorMax = new Vector2(0.97f, 0.96f);
            synPanelRect.offsetMin = Vector2.zero;
            synPanelRect.offsetMax = Vector2.zero;

            GameObject synHeaderObj = new GameObject("SynergiesHeader");
            synHeaderObj.transform.SetParent(synPanelObj.transform, false);
            RectTransform synHeaderRect = synHeaderObj.AddComponent<RectTransform>();
            synHeaderRect.anchorMin = new Vector2(0, 0.82f);
            synHeaderRect.anchorMax = new Vector2(1, 1.0f);
            synHeaderRect.offsetMin = Vector2.zero;
            synHeaderRect.offsetMax = Vector2.zero;

            TextMeshProUGUI synLabel = synHeaderObj.AddComponent<TextMeshProUGUI>();
            synLabel.text = "[보유 시너지]";
            synLabel.fontSize = 15;
            synLabel.fontStyle = FontStyles.Bold;
            synLabel.color = new Color(0.85f, 0.75f, 0.95f, 1f);
            synLabel.raycastTarget = false;
            if (mainFont != null) synLabel.font = mainFont;

            GameObject synContObj = new GameObject("SynergiesContainer");
            synContObj.transform.SetParent(synPanelObj.transform, false);
            RectTransform synContRect = synContObj.AddComponent<RectTransform>();
            synContRect.anchorMin = new Vector2(0, 0f);
            synContRect.anchorMax = new Vector2(1, 0.80f);
            synContRect.offsetMin = Vector2.zero;
            synContRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup synLayout = synContObj.AddComponent<VerticalLayoutGroup>();
            synLayout.spacing = 6;
            synLayout.childAlignment = TextAnchor.UpperLeft;
            synLayout.childControlWidth = true;
            synLayout.childControlHeight = false;
            detailSynergiesContainer = synContObj.transform;

            // ─── Skills Area (하단 영역 2줄 분할: 상단 패시브 단일 / 하단 액티브 4개) ───
            GameObject skillHeaderObj = new GameObject("SkillsHeader");
            skillHeaderObj.transform.SetParent(detailPanel.transform, false);
            RectTransform skillHeaderRect = skillHeaderObj.AddComponent<RectTransform>();
            skillHeaderRect.anchorMin = new Vector2(0.03f, 0.38f);
            skillHeaderRect.anchorMax = new Vector2(0.97f, 0.43f);
            skillHeaderRect.offsetMin = Vector2.zero;
            skillHeaderRect.offsetMax = Vector2.zero;

            TextMeshProUGUI skillLabel = skillHeaderObj.AddComponent<TextMeshProUGUI>();
            skillLabel.text = "[캐릭터 스킬 (마우스 호버 설명)]";
            skillLabel.fontSize = 17;
            skillLabel.fontStyle = FontStyles.Bold;
            skillLabel.color = new Color(0.95f, 0.85f, 0.4f, 1f);
            skillLabel.raycastTarget = false;
            if (mainFont != null) skillLabel.font = mainFont;

            GameObject skillContObj = new GameObject("SkillsContainer");
            skillContObj.transform.SetParent(detailPanel.transform, false);
            RectTransform skillContRect = skillContObj.AddComponent<RectTransform>();
            skillContRect.anchorMin = new Vector2(0.03f, 0.03f);
            skillContRect.anchorMax = new Vector2(0.97f, 0.38f);
            skillContRect.offsetMin = Vector2.zero;
            skillContRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup skillLayout = skillContObj.AddComponent<VerticalLayoutGroup>();
            skillLayout.spacing = 8;
            skillLayout.childAlignment = TextAnchor.UpperLeft;
            skillLayout.childControlWidth = true;
            skillLayout.childControlHeight = false;
            skillLayout.childForceExpandHeight = false;
            detailSkillsContainer = skillContObj.transform;

            // Passive Row Container
            GameObject pRowObj = new GameObject("PassiveRow");
            pRowObj.transform.SetParent(skillContObj.transform, false);
            RectTransform pRowRect = pRowObj.AddComponent<RectTransform>();
            pRowRect.sizeDelta = new Vector2(0, 72);
            HorizontalLayoutGroup pRowLayout = pRowObj.AddComponent<HorizontalLayoutGroup>();
            pRowLayout.spacing = 12;
            pRowLayout.childAlignment = TextAnchor.MiddleLeft;
            pRowLayout.childControlWidth = false;
            pRowLayout.childControlHeight = false;

            // Active Row Container
            GameObject aRowObj = new GameObject("ActiveRow");
            aRowObj.transform.SetParent(skillContObj.transform, false);
            RectTransform aRowRect = aRowObj.AddComponent<RectTransform>();
            aRowRect.sizeDelta = new Vector2(0, 72);
            HorizontalLayoutGroup aRowLayout = aRowObj.AddComponent<HorizontalLayoutGroup>();
            aRowLayout.spacing = 12;
            aRowLayout.childAlignment = TextAnchor.MiddleLeft;
            aRowLayout.childControlWidth = false;
            aRowLayout.childControlHeight = false;

            // 4. BOTTOM BAR: Screen-wide Bottom Action Bar (Y: 0.02 ~ 0.10)

            // 4-1. Left: Back Button
            GameObject backBtnObj = new GameObject("BackButton");
            backBtnObj.transform.SetParent(characterSelectionPanel.transform, false);
            RectTransform backBtnRect = backBtnObj.AddComponent<RectTransform>();
            backBtnRect.anchorMin = new Vector2(0.04f, 0.03f);
            backBtnRect.anchorMax = new Vector2(0.18f, 0.095f);
            backBtnRect.offsetMin = Vector2.zero;
            backBtnRect.offsetMax = Vector2.zero;

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
            backText.fontSize = 20;
            backText.alignment = TextAlignmentOptions.Center;
            backText.color = Color.white;
            backText.raycastTarget = false;
            if (mainFont != null) backText.font = mainFont;

            // 4-2. Drawn Relic Icon Slot (소환된 유물 미리보기 + 마우스 호버 설명)
            GameObject drawnRelicSlotObj = new GameObject("DrawnRelicSlot");
            drawnRelicSlotObj.transform.SetParent(characterSelectionPanel.transform, false);
            RectTransform drawnRelicSlotRect = drawnRelicSlotObj.AddComponent<RectTransform>();
            drawnRelicSlotRect.anchorMin = new Vector2(0.46f, 0.03f);
            drawnRelicSlotRect.anchorMax = new Vector2(0.52f, 0.095f);
            drawnRelicSlotRect.offsetMin = Vector2.zero;
            drawnRelicSlotRect.offsetMax = Vector2.zero;

            Image drawnRelicSlotBg = drawnRelicSlotObj.AddComponent<Image>();
            drawnRelicSlotBg.color = new Color(0.16f, 0.14f, 0.22f, 0.9f);
            drawnRelicSlotBg.raycastTarget = true;

            Outline drawnRelicOutline = drawnRelicSlotObj.AddComponent<Outline>();
            drawnRelicOutline.effectColor = new Color(0.75f, 0.55f, 0.95f, 0.8f);

            GameObject drawnRelicImgObj = new GameObject("DrawnRelicImage");
            drawnRelicImgObj.transform.SetParent(drawnRelicSlotObj.transform, false);
            RectTransform dImgRect = drawnRelicImgObj.AddComponent<RectTransform>();
            dImgRect.anchorMin = Vector2.zero;
            dImgRect.anchorMax = Vector2.one;
            dImgRect.sizeDelta = Vector2.zero;

            relicPreviewImageComponent = drawnRelicImgObj.AddComponent<Image>();
            relicPreviewImageComponent.preserveAspect = true;
            relicPreviewImageComponent.raycastTarget = false;
            relicPreviewImageComponent.gameObject.SetActive(false);

            drawnRelicHoverTrigger = drawnRelicSlotObj.AddComponent<UIHoverTrigger>();

            // 4-3. Relic Gacha Button (유물 소환 팝업 버튼)
            GameObject gachaBtnObj = new GameObject("RelicGachaButton");
            gachaBtnObj.transform.SetParent(characterSelectionPanel.transform, false);
            RectTransform gachaBtnRect = gachaBtnObj.AddComponent<RectTransform>();
            gachaBtnRect.anchorMin = new Vector2(0.54f, 0.03f);
            gachaBtnRect.anchorMax = new Vector2(0.73f, 0.095f);
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
            gachaText.text = "특전 유물 소환";
            gachaText.fontSize = 20;
            gachaText.alignment = TextAlignmentOptions.Center;
            gachaText.color = Color.white;
            gachaText.raycastTarget = false;
            if (mainFont != null) gachaText.font = mainFont;

            // 4-4. Confirm Start Button (화면 전체 최하단 우측)
            GameObject startBtnObj = new GameObject("ConfirmStartButton");
            startBtnObj.transform.SetParent(characterSelectionPanel.transform, false);
            RectTransform startBtnRect = startBtnObj.AddComponent<RectTransform>();
            startBtnRect.anchorMin = new Vector2(0.75f, 0.03f);
            startBtnRect.anchorMax = new Vector2(0.96f, 0.095f);
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
            startText.text = "여정 시작";
            startText.fontSize = 24;
            startText.fontStyle = FontStyles.Bold;
            startText.alignment = TextAlignmentOptions.Center;
            startText.color = Color.white;
            startText.raycastTarget = false;
            if (mainFont != null) startText.font = mainFont;

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
            HideHoverTooltip();
        }

        public void ShowNotification(string message)
        {
            if (notificationModal != null)
            {
                if (notificationTextComponent != null)
                {
                    notificationTextComponent.text = message;
                }
                else
                {
                    TextMeshProUGUI tmp = notificationModal.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = message;
                }

                Button[] modalButtons = notificationModal.GetComponentsInChildren<Button>(true);
                foreach (var b in modalButtons)
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() =>
                    {
                        if (notificationModal != null) notificationModal.SetActive(false);
                    });
                }

                notificationModal.SetActive(true);
                notificationModal.transform.SetAsLastSibling();
            }
            else
            {
                Debug.Log($"[MainMenuUI Notification] {message}");
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

        public void PopulateCharacterList()
        {
            if (characterListContainer == null) return;

            // 5개 열(FixedColumnCount = 5) 강제 적용
            GridLayoutGroup gridLayout = characterListContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = 5;
                gridLayout.cellSize = new Vector2(152, 185);
                gridLayout.spacing = new Vector2(14, 14);
            }

            CharacterData[] allCharacters = Resources.LoadAll<CharacterData>("Characters");
            if (allCharacters == null || allCharacters.Length == 0)
            {
                Debug.LogWarning("[MainMenuUI] No CharacterData found in Resources/Characters!");
                return;
            }

            cardOutlines.Clear();
            selectedCharacterData = null;

            // 1. Check if matching cards already exist in scene container (preserve user custom styles/layouts)
            bool hasMatchingCards = false;
            foreach (var charData in allCharacters)
            {
                if (charData == null || charData.isEnemy) continue;
                Transform existingCard = characterListContainer.Find($"Card_{charData.DataId}");
                if (existingCard != null)
                {
                    hasMatchingCards = true;
                    Button btn = existingCard.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnCharacterCardClicked(charData));
                    }
                    Outline outline = existingCard.GetComponent<Outline>();
                    if (outline != null)
                    {
                        cardOutlines[charData] = outline;
                    }
                }
            }

            // 2. If no cards exist in scene yet, generate them dynamically
            if (!hasMatchingCards)
            {
                for (int i = characterListContainer.childCount - 1; i >= 0; i--)
                {
                    SafeDestroy(characterListContainer.GetChild(i).gameObject);
                }

                CharacterData firstValidChar = null;
                foreach (var charData in allCharacters)
                {
                    if (charData == null || charData.isEnemy) continue;
                    CreateCharacterCard(charData);
                    if (firstValidChar == null) firstValidChar = charData;
                }

                if (firstValidChar != null)
                {
                    SelectCharacter(firstValidChar);
                }
            }
            else
            {
                // Select first valid character to update detail & background
                foreach (var charData in allCharacters)
                {
                    if (charData != null && !charData.isEnemy)
                    {
                        SelectCharacter(charData);
                        break;
                    }
                }
            }
        }

        private void CreateCharacterCard(CharacterData data)
        {
            GameObject cardObj = new GameObject($"Card_{data.DataId}");
            cardObj.transform.SetParent(characterListContainer, false);
            RectTransform rect = cardObj.GetComponent<RectTransform>();
            if (rect == null) rect = cardObj.AddComponent<RectTransform>();

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

            GameObject portraitObj = new GameObject("Portrait");
            portraitObj.transform.SetParent(cardObj.transform, false);
            RectTransform pRect = portraitObj.GetComponent<RectTransform>();
            if (pRect == null) pRect = portraitObj.AddComponent<RectTransform>();
            pRect.sizeDelta = new Vector2(105, 105);

            LayoutElement pLayout = portraitObj.AddComponent<LayoutElement>();
            pLayout.preferredWidth = 105;
            pLayout.preferredHeight = 105;
            pLayout.minWidth = 100;
            pLayout.minHeight = 100;

            Image pImg = portraitObj.AddComponent<Image>();
            Sprite charSprite = data.portraitSprite != null ? data.portraitSprite : data.standingSprite;
            pImg.sprite = charSprite;
            pImg.color = charSprite != null ? Color.white : new Color(0.2f, 0.25f, 0.35f, 0.8f);
            pImg.preserveAspect = true;
            pImg.raycastTarget = false;

            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(cardObj.transform, false);
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            if (nameRect == null) nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(110, 30);

            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = data.DisplayName;
            nameText.fontSize = 17;
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

            // 0. 캐릭터 고유 배경 일러스트 및 라이브 모션 갱신
            UpdateCharacterBackground(data);

            foreach (var kvp in cardOutlines)
            {
                if (kvp.Value == null) continue;
                if (kvp.Key == data)
                {
                    kvp.Value.effectColor = new Color(0.95f, 0.78f, 0.25f, 1f);
                    kvp.Value.effectDistance = new Vector2(2.5f, -2.5f);
                }
                else
                {
                    kvp.Value.effectColor = new Color(0.3f, 0.35f, 0.45f, 0.5f);
                    kvp.Value.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }

            // 1. 대형 전신 일러스트 & 기본 스탯 정보 (초상화 대신 전신 standingSprite 우선 출력)
            if (detailPortraitImageComponent != null)
            {
                Sprite charSprite = data.standingSprite != null ? data.standingSprite : (data.portraitSprite != null ? data.portraitSprite : null);
                detailPortraitImageComponent.sprite = charSprite;
                detailPortraitImageComponent.preserveAspect = true;
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

            // 2. 기본 스탯 정보 (한 줄에 하나씩 출력)
            if (detailStatsTextComponent != null)
            {
                detailStatsTextComponent.text = 
                    $"<b><color=#E2B056>[기본 능력치]</color></b>\n" +
                    $"• 체력: <color=#7FFFD4>{data.maxHp}</color>\n" +
                    $"• 정신력: <color=#7B68EE>{data.maxMental}</color>\n" +
                    $"• 공격력: <color=#FF6B6B>{data.baseAttack}</color>\n" +
                    $"• 주문력: <color=#4ECDC4>{data.spellPower}</color>\n" +
                    $"• 방어력: <color=#FFE66D>{data.armor}</color>\n" +
                    $"• 마법 저항: <color=#95E1D3>{data.magicResist}</color>";
            }

            // 3. 시너지 바인딩 (가로 정렬 및 마우스 호버 설명)
            if (detailSynergiesContainer != null)
            {
                // 기존 VerticalLayoutGroup이 있다면 제거/비활성화
                var oldVLayouts = detailSynergiesContainer.GetComponents<VerticalLayoutGroup>();
                foreach (var v in oldVLayouts)
                {
                    if (v != null) v.enabled = false;
                }

                HorizontalLayoutGroup sHLayout = detailSynergiesContainer.GetComponent<HorizontalLayoutGroup>();
                if (sHLayout == null) sHLayout = detailSynergiesContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
                sHLayout.enabled = true;
                sHLayout.spacing = 8f;
                sHLayout.childAlignment = TextAnchor.MiddleLeft;
                sHLayout.childControlWidth = false;
                sHLayout.childControlHeight = false;
                sHLayout.childForceExpandWidth = false;
                sHLayout.childForceExpandHeight = false;

                for (int i = detailSynergiesContainer.childCount - 1; i >= 0; i--)
                {
                    detailSynergiesContainer.GetChild(i).gameObject.SetActive(false);
                    SafeDestroy(detailSynergiesContainer.GetChild(i).gameObject);
                }

                if (data.synergies != null && data.synergies.Count > 0)
                {
                    foreach (var synergyType in data.synergies)
                    {
                        CreateSynergyBadgeUI(detailSynergiesContainer, synergyType);
                    }
                }
            }

            // 4. 스킬 바인딩 (상단: 패시브 단일 / 하단: 액티브 4개 2줄 분할 배치)
            UpdateSkillsUI(data);
            ApplyCharacterSelectionPresentation();
        }

        /// <summary>
        /// 스킬 목록을 상단 패시브 1줄(단일), 하단 액티브 1줄(최대 4개)의 2줄 구조로 깔끔하게 배치합니다.
        /// childForceExpandWidth = false로 설정하여 중간 불필요 공백 없이 4개 슬롯이 정확히 정렬됩니다.
        /// </summary>
        private void UpdateSkillsUI(CharacterData data)
        {
            if (detailSkillsContainer == null || data == null) return;

            // 기존 LayoutGroup 비활성화 충돌 방지
            var existingLayouts = detailSkillsContainer.GetComponents<HorizontalOrVerticalLayoutGroup>();
            foreach (var layout in existingLayouts)
            {
                if (layout != null) layout.enabled = false;
            }

            // 1. PassiveRow 탐색 또는 동적 생성 (상단 줄: Y 0.52 ~ 1.0)
            Transform passiveRow = detailSkillsContainer.Find("PassiveRow");
            if (passiveRow == null)
            {
                GameObject pObj = new GameObject("PassiveRow");
                pObj.transform.SetParent(detailSkillsContainer, false);
                passiveRow = pObj.transform;
            }
            RectTransform pRect = passiveRow.GetComponent<RectTransform>();
            if (pRect == null) pRect = passiveRow.gameObject.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0f, 0.52f);
            pRect.anchorMax = new Vector2(1f, 1.0f);
            pRect.offsetMin = Vector2.zero;
            pRect.offsetMax = Vector2.zero;
            pRect.pivot = new Vector2(0.5f, 0.5f);

            HorizontalLayoutGroup pHLayout = passiveRow.GetComponent<HorizontalLayoutGroup>();
            if (pHLayout == null) pHLayout = passiveRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            pHLayout.spacing = 8f;
            pHLayout.childAlignment = TextAnchor.MiddleLeft;
            pHLayout.childControlWidth = false;
            pHLayout.childControlHeight = false;
            pHLayout.childForceExpandWidth = false;
            pHLayout.childForceExpandHeight = false;

            // 2. ActiveRow 탐색 또는 동적 생성 (하단 줄: Y 0.0 ~ 0.48)
            Transform activeRow = detailSkillsContainer.Find("ActiveRow");
            if (activeRow == null)
            {
                GameObject aObj = new GameObject("ActiveRow");
                aObj.transform.SetParent(detailSkillsContainer, false);
                activeRow = aObj.transform;
            }
            RectTransform aRect = activeRow.GetComponent<RectTransform>();
            if (aRect == null) aRect = activeRow.gameObject.AddComponent<RectTransform>();
            aRect.anchorMin = new Vector2(0f, 0.0f);
            aRect.anchorMax = new Vector2(1f, 0.48f);
            aRect.offsetMin = Vector2.zero;
            aRect.offsetMax = Vector2.zero;
            aRect.pivot = new Vector2(0.5f, 0.5f);

            HorizontalLayoutGroup aHLayout = activeRow.GetComponent<HorizontalLayoutGroup>();
            if (aHLayout == null) aHLayout = activeRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            aHLayout.spacing = 8f;
            aHLayout.childAlignment = TextAnchor.MiddleLeft;
            aHLayout.childControlWidth = false;
            aHLayout.childControlHeight = false;
            aHLayout.childForceExpandWidth = false;
            aHLayout.childForceExpandHeight = false;

            // PassiveRow, ActiveRow 이외의 직속 슬롯 오브젝트 정리
            for (int i = detailSkillsContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = detailSkillsContainer.GetChild(i);
                if (child != passiveRow && child != activeRow)
                {
                    SafeDestroy(child.gameObject);
                }
            }

            // 기존 슬롯 비우기
            for (int i = passiveRow.childCount - 1; i >= 0; i--)
            {
                passiveRow.GetChild(i).gameObject.SetActive(false);
                SafeDestroy(passiveRow.GetChild(i).gameObject);
            }
            for (int i = activeRow.childCount - 1; i >= 0; i--)
            {
                activeRow.GetChild(i).gameObject.SetActive(false);
                SafeDestroy(activeRow.GetChild(i).gameObject);
            }

            // 상단 줄: 패시브 스킬 (단일)
            if (data.passiveSkill != null && (!string.IsNullOrEmpty(data.passiveSkill.skillName) || data.passiveSkill.skillIcon != null))
            {
                CreateSkillSlotUI(passiveRow, data.passiveSkill, isPassive: true);
            }

            // 하단 줄: 액티브 스킬 (최대 4개)
            if (data.activeSkills != null)
            {
                foreach (var activeSkill in data.activeSkills)
                {
                    if (activeSkill != null && (!string.IsNullOrEmpty(activeSkill.skillName) || activeSkill.skillIcon != null))
                    {
                        CreateSkillSlotUI(activeRow, activeSkill, isPassive: false);
                    }
                }
            }
        }

        private void CreateSynergyBadgeUI(Transform parent, SynergyType synergyType)
        {
            var info = SynergyDatabase.GetInfo(synergyType);
            string displayName = info != null ? info.displayName : synergyType.ToString();
            string emoji = info != null ? info.iconEmoji : "⚡";

            GameObject badgeObj = new GameObject($"Synergy_{synergyType}");
            badgeObj.transform.SetParent(parent, false);

            RectTransform bRect = badgeObj.GetComponent<RectTransform>();
            if (bRect == null) bRect = badgeObj.AddComponent<RectTransform>();
            bRect.sizeDelta = new Vector2(115, 34);

            LayoutElement le = badgeObj.GetComponent<LayoutElement>();
            if (le == null) le = badgeObj.AddComponent<LayoutElement>();
            le.preferredWidth = 115;
            le.preferredHeight = 34;
            le.minWidth = 90;
            le.minHeight = 30;

            Image bg = badgeObj.AddComponent<Image>();
            bg.color = info != null && info.isFaction ? new Color(0.18f, 0.28f, 0.42f, 0.95f) : new Color(0.28f, 0.22f, 0.38f, 0.95f);
            bg.raycastTarget = true;

            Outline outline = badgeObj.AddComponent<Outline>();
            outline.effectColor = info != null && info.isFaction ? new Color(0.35f, 0.65f, 0.95f, 0.7f) : new Color(0.75f, 0.45f, 0.95f, 0.7f);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(badgeObj.transform, false);
            RectTransform tRect = textObj.GetComponent<RectTransform>();
            if (tRect == null) tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = displayName;
            tmp.fontSize = 13;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            if (mainFont != null) tmp.font = mainFont;

            // Hover Trigger
            UIHoverTrigger trigger = badgeObj.AddComponent<UIHoverTrigger>();
            trigger.title = $"{emoji} {displayName}";
            trigger.category = info != null && info.isFaction ? "[세력 시너지]" : "[직업 시너지]";
            trigger.description = info != null ? $"<b>{info.summaryDescription}</b>\n\n{FormatSynergyTiers(info)}" : "시너지 설명입니다.";
            trigger.onHoverEnter = ShowHoverTooltip;
            trigger.onHoverExit = HideHoverTooltip;
        }

        private string FormatSynergyTiers(SynergyInfo info)
        {
            if (info == null || info.tiers == null || info.tiers.Count == 0) return "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("<color=#FFE66D><b>[단계별 활성화 효과]</b></color>");
            foreach (var tier in info.tiers)
            {
                sb.AppendLine($"• <color=#7FFFD4>{tier.threshold}명</color>: {tier.description}");
            }
            return sb.ToString();
        }

        private void CreateSkillSlotUI(Transform parent, SkillInfo skill, bool isPassive)
        {
            if (skill == null) return;

            string sName = string.IsNullOrEmpty(skill.skillName) ? (isPassive ? "패시브" : "스킬") : skill.skillName;
            GameObject slotObj = new GameObject($"SkillSlot_{sName}");
            slotObj.transform.SetParent(parent, false);

            RectTransform rect = slotObj.GetComponent<RectTransform>();
            if (rect == null) rect = slotObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(64, 64); // 64x64 크기 슬롯

            LayoutElement sLayout = slotObj.GetComponent<LayoutElement>();
            if (sLayout == null) sLayout = slotObj.AddComponent<LayoutElement>();
            sLayout.preferredWidth = 64;
            sLayout.preferredHeight = 64;
            sLayout.minWidth = 64;
            sLayout.minHeight = 64;

            Image bg = slotObj.AddComponent<Image>();
            bg.color = isPassive ? new Color(0.35f, 0.28f, 0.12f, 0.95f) : new Color(0.12f, 0.22f, 0.32f, 0.95f);
            bg.raycastTarget = true;

            Outline outline = slotObj.AddComponent<Outline>();
            outline.effectColor = isPassive ? new Color(0.95f, 0.75f, 0.25f, 0.8f) : new Color(0.35f, 0.75f, 0.95f, 0.8f);

            // Skill Icon
            if (skill.skillIcon != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(slotObj.transform, false);
                RectTransform iRect = iconObj.GetComponent<RectTransform>();
                if (iRect == null) iRect = iconObj.AddComponent<RectTransform>();
                iRect.anchorMin = Vector2.zero;
                iRect.anchorMax = Vector2.one;
                iRect.sizeDelta = Vector2.zero;

                Image img = iconObj.AddComponent<Image>();
                img.sprite = skill.skillIcon;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }

            // Top-Left Cost Badge (AP 텍스트 제외 숫자/P만 표시)
            GameObject tagObj = new GameObject("CostBadge");
            tagObj.transform.SetParent(slotObj.transform, false);
            RectTransform tRect = tagObj.GetComponent<RectTransform>();
            if (tRect == null) tRect = tagObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 0.60f);
            tRect.anchorMax = new Vector2(0.40f, 1.0f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            Image tagBg = tagObj.AddComponent<Image>();
            tagBg.color = isPassive ? new Color(0.4f, 0.3f, 0.05f, 0.95f) : new Color(0.08f, 0.18f, 0.28f, 0.95f);
            tagBg.raycastTarget = false;

            Outline tagOutline = tagObj.AddComponent<Outline>();
            tagOutline.effectColor = isPassive ? new Color(0.95f, 0.8f, 0.2f, 0.9f) : new Color(0.35f, 0.85f, 0.95f, 0.9f);

            GameObject tagTextObj = new GameObject("TagText");
            tagTextObj.transform.SetParent(tagObj.transform, false);
            RectTransform tagTextRect = tagTextObj.GetComponent<RectTransform>();
            if (tagTextRect == null) tagTextRect = tagTextObj.AddComponent<RectTransform>();
            tagTextRect.anchorMin = Vector2.zero;
            tagTextRect.anchorMax = Vector2.one;
            tagTextRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tagTmp = tagTextObj.AddComponent<TextMeshProUGUI>();
            if (tagTmp != null)
            {
                tagTmp.text = isPassive ? "P" : $"{skill.baseCost}";
                tagTmp.fontSize = 13;
                tagTmp.fontStyle = FontStyles.Bold;
                tagTmp.alignment = TextAlignmentOptions.Center;
                tagTmp.color = isPassive ? new Color(0.95f, 0.85f, 0.3f, 1f) : new Color(0.5f, 0.95f, 1f, 1f);
                tagTmp.raycastTarget = false;
                if (mainFont != null) tagTmp.font = mainFont;
            }

            // Hover Trigger
            UIHoverTrigger trigger = slotObj.AddComponent<UIHoverTrigger>();
            trigger.title = isPassive ? $"✨ {sName}" : $"⚔️ {sName}";
            trigger.category = isPassive ? "[패시브 스킬]" : $"[액티브 스킬 - 소모: {skill.baseCost} AP]";
            trigger.description = FormatSkillDescription(skill);
            trigger.onHoverEnter = ShowHoverTooltip;
            trigger.onHoverExit = HideHoverTooltip;
        }

        private string FormatSkillDescription(SkillInfo skill)
        {
            if (skill == null) return "스킬 정보가 없습니다.";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (skill.levels != null && skill.levels.Length > 0 && skill.levels[0] != null)
            {
                var level0 = skill.levels[0];
                sb.AppendLine($"• 대상: <color=#7FFFD4>{level0.targetType}</color>");
                if (level0.effects != null && level0.effects.Count > 0)
                {
                    foreach (var eff in level0.effects)
                    {
                        sb.AppendLine($"• 효과: {eff.type} (계수: {eff.multiplier}x / 고정: {eff.fixedValue})");
                    }
                }
            }
            return sb.Length > 0 ? sb.ToString() : "스킬 효과 정보";
        }

        public void UpdateCharacterBackground(CharacterData data)
        {
            if (data == null) return;
            Sprite bgSprite = CharacterBackgroundLoader.LoadCharacterBackground(data);

            if (characterAliveEffect == null && characterSelectionPanel != null)
            {
                Transform bgLayer = characterSelectionPanel.transform.Find("CharacterBackgroundLayer");
                if (bgLayer != null)
                {
                    Transform illuTrans = bgLayer.Find("IllustrationImage");
                    if (illuTrans != null)
                    {
                        characterBackgroundImageComponent = illuTrans.GetComponent<Image>();
                        characterAliveEffect = illuTrans.GetComponent<CharacterAliveIllustration>();
                        if (characterAliveEffect == null)
                        {
                            characterAliveEffect = illuTrans.gameObject.AddComponent<CharacterAliveIllustration>();
                        }
                    }
                }
            }

            if (characterAliveEffect != null)
            {
                characterAliveEffect.SetIllustration(bgSprite);
            }
            else if (characterBackgroundImageComponent != null)
            {
                characterBackgroundImageComponent.sprite = bgSprite;
                characterBackgroundImageComponent.color = bgSprite != null ? Color.white : new Color(0.08f, 0.09f, 0.12f, 1f);
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
            if (relicGachaModal != null)
            {
                relicGachaModal.SetActive(true);
                relicGachaModal.transform.SetAsLastSibling();
                if (!isGachaSpinning)
                {
                    StartCoroutine(PlayRelicGachaRouletteRoutine());
                }
            }
        }

        private System.Collections.IEnumerator PlayRelicGachaRouletteRoutine()
        {
            isGachaSpinning = true;
            if (gachaConfirmButton != null) gachaConfirmButton.interactable = false;

            RelicData[] allRelics = Resources.LoadAll<RelicData>("Relics");
            if (allRelics == null || allRelics.Length == 0)
            {
                if (gachaRouletteNameTextComponent != null) gachaRouletteNameTextComponent.text = "유물 데이터를 찾을 수 없습니다.";
                isGachaSpinning = false;
                if (gachaConfirmButton != null) gachaConfirmButton.interactable = true;
                yield break;
            }

            int spinCount = Random.Range(16, 24);
            float waitInterval = 0.04f;

            for (int i = 0; i < spinCount; i++)
            {
                RelicData tempRelic = allRelics[Random.Range(0, allRelics.Length)];
                if (gachaRouletteImageComponent != null && tempRelic.icon != null)
                {
                    gachaRouletteImageComponent.sprite = tempRelic.icon;
                    gachaRouletteImageComponent.color = Color.white;
                }
                if (gachaRouletteNameTextComponent != null)
                {
                    gachaRouletteNameTextComponent.text = $"<color=#FFE66D>{tempRelic.relicName}</color>";
                }
                if (gachaRouletteDescTextComponent != null)
                {
                    gachaRouletteDescTextComponent.text = tempRelic.description;
                }

                if (i > spinCount - 6) waitInterval += 0.06f;
                else if (i > spinCount - 10) waitInterval += 0.03f;

                yield return new WaitForSecondsRealtime(waitInterval);
            }

            // 최종 선택
            int finalIndex = Random.Range(0, allRelics.Length);
            selectedStartingRelic = allRelics[finalIndex];

            if (gachaRouletteImageComponent != null && selectedStartingRelic.icon != null)
            {
                gachaRouletteImageComponent.sprite = selectedStartingRelic.icon;
                gachaRouletteImageComponent.color = Color.white;
            }
            if (gachaRouletteNameTextComponent != null)
            {
                gachaRouletteNameTextComponent.text = $"✨ <b><color=#FFD700>[{selectedStartingRelic.relicName}] 당첨!</color></b>";
            }
            if (gachaRouletteDescTextComponent != null)
            {
                gachaRouletteDescTextComponent.text = $"<color=#7FFFD4>{selectedStartingRelic.description}</color>\n\n(여정 시작 시 해당 유물을 보유하고 시작합니다)";
            }

            UpdateDrawnRelicUI();

            isGachaSpinning = false;
            if (gachaConfirmButton != null) gachaConfirmButton.interactable = true;
        }

        private void UpdateDrawnRelicUI()
        {
            if (selectedStartingRelic == null) return;

            Transform emptyRelic = characterSelectionPanel != null
                ? characterSelectionPanel.transform.Find("DrawnRelicSlot/EmptyRelic") : null;
            if (emptyRelic != null) emptyRelic.gameObject.SetActive(false);

            if (relicPreviewImageComponent != null && selectedStartingRelic.icon != null)
            {
                relicPreviewImageComponent.sprite = selectedStartingRelic.icon;
                relicPreviewImageComponent.color = Color.white;
                relicPreviewImageComponent.gameObject.SetActive(true);
            }

            if (drawnRelicHoverTrigger == null && relicPreviewImageComponent != null)
            {
                Transform parent = relicPreviewImageComponent.transform.parent;
                if (parent != null)
                {
                    drawnRelicHoverTrigger = parent.GetComponent<UIHoverTrigger>();
                    if (drawnRelicHoverTrigger == null) drawnRelicHoverTrigger = parent.gameObject.AddComponent<UIHoverTrigger>();
                }
            }

            if (drawnRelicHoverTrigger != null)
            {
                drawnRelicHoverTrigger.title = $"🔮 {selectedStartingRelic.relicName}";
                drawnRelicHoverTrigger.category = "[시작 특전 유물]";
                drawnRelicHoverTrigger.description = selectedStartingRelic.description;
                drawnRelicHoverTrigger.onHoverEnter = ShowHoverTooltip;
                drawnRelicHoverTrigger.onHoverExit = HideHoverTooltip;
            }
        }
    }
}
