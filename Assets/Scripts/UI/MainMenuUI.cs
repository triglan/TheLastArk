using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

namespace TheLastArk.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI Panels")]
        public GameObject titlePanel;
        public GameObject characterSelectionPanel;
        public Transform characterListContainer;

        [Header("Prefabs/Settings")]
        public TMPro.TMP_FontAsset mainFont;

        private void Start()
        {
            if (mainFont == null)
            {
                mainFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Main_Fonts");
            }
            
            // Build UI if not assigned
            if (titlePanel == null || characterSelectionPanel == null)
            {
                CreateUI();
            }

            ShowTitlePanel();
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

            // Title Panel
            titlePanel = new GameObject("TitlePanel");
            titlePanel.transform.SetParent(canvas.transform, false);
            RectTransform titleRect = titlePanel.AddComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.sizeDelta = Vector2.zero;

            Image titleBg = titlePanel.AddComponent<Image>();
            titleBg.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            GameObject titleTextObj = new GameObject("TitleText");
            titleTextObj.transform.SetParent(titlePanel.transform, false);
            RectTransform titleTextRect = titleTextObj.AddComponent<RectTransform>();
            titleTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleTextRect.pivot = new Vector2(0.5f, 0.5f);
            titleTextRect.anchoredPosition = new Vector2(0, 200);
            titleTextRect.sizeDelta = new Vector2(1600, 400); // 넉넉한 크기
            TextMeshProUGUI titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Last Express:\nTo The Ark";
            titleText.fontSize = 140;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.enableWordWrapping = false; // 잘림 방지
            titleText.color = Color.white;
            if (mainFont != null) titleText.font = mainFont;

            GameObject startBtnObj = new GameObject("StartButton");
            startBtnObj.transform.SetParent(titlePanel.transform, false);
            RectTransform startBtnRect = startBtnObj.AddComponent<RectTransform>();
            startBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
            startBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
            startBtnRect.pivot = new Vector2(0.5f, 0.5f);
            startBtnRect.anchoredPosition = new Vector2(0, -150);
            startBtnRect.sizeDelta = new Vector2(400, 100);
            Image startBtnImg = startBtnObj.AddComponent<Image>();
            startBtnImg.color = new Color(0.2f, 0.6f, 0.2f, 1f);
            Button startBtn = startBtnObj.AddComponent<Button>();
            startBtn.onClick.AddListener(OnStartGameClicked);

            GameObject startBtnTextObj = new GameObject("Text");
            startBtnTextObj.transform.SetParent(startBtnObj.transform, false);
            RectTransform startBtnTextRect = startBtnTextObj.AddComponent<RectTransform>();
            startBtnTextRect.anchorMin = Vector2.zero;
            startBtnTextRect.anchorMax = Vector2.one;
            startBtnTextRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI startBtnText = startBtnTextObj.AddComponent<TextMeshProUGUI>();
            startBtnText.text = "게임 시작";
            startBtnText.fontSize = 36;
            startBtnText.alignment = TextAlignmentOptions.Center;
            startBtnText.color = Color.white;
            if (mainFont != null) startBtnText.font = mainFont;

            // Character Selection Panel
            characterSelectionPanel = new GameObject("CharacterSelectionPanel");
            characterSelectionPanel.transform.SetParent(canvas.transform, false);
            RectTransform charSelRect = characterSelectionPanel.AddComponent<RectTransform>();
            charSelRect.anchorMin = Vector2.zero;
            charSelRect.anchorMax = Vector2.one;
            charSelRect.sizeDelta = Vector2.zero;

            Image charSelBg = characterSelectionPanel.AddComponent<Image>();
            charSelBg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            GameObject guideTextObj = new GameObject("GuideText");
            guideTextObj.transform.SetParent(characterSelectionPanel.transform, false);
            RectTransform guideTextRect = guideTextObj.AddComponent<RectTransform>();
            guideTextRect.anchorMin = new Vector2(0.5f, 1f);
            guideTextRect.anchorMax = new Vector2(0.5f, 1f);
            guideTextRect.pivot = new Vector2(0.5f, 1f);
            guideTextRect.anchoredPosition = new Vector2(0, -50);
            guideTextRect.sizeDelta = new Vector2(800, 100);
            TextMeshProUGUI guideText = guideTextObj.AddComponent<TextMeshProUGUI>();
            guideText.text = "함께할 승무원을 선택해주세요.";
            guideText.fontSize = 48;
            guideText.alignment = TextAlignmentOptions.Center;
            guideText.color = Color.white;
            if (mainFont != null) guideText.font = mainFont;

            GameObject gridArea = new GameObject("GridArea");
            gridArea.transform.SetParent(characterSelectionPanel.transform, false);
            RectTransform gridRect = gridArea.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.05f, 0.05f);
            gridRect.anchorMax = new Vector2(0.95f, 0.85f);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;

            GridLayoutGroup gridLayout = gridArea.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(300, 400);
            gridLayout.spacing = new Vector2(50, 50);
            gridLayout.padding = new RectOffset(50, 50, 50, 50);
            gridLayout.childAlignment = TextAnchor.UpperCenter;

            characterListContainer = gridArea.transform;
        }

        private void ShowTitlePanel()
        {
            titlePanel.SetActive(true);
            characterSelectionPanel.SetActive(false);
        }

        private void OnStartGameClicked()
        {
            titlePanel.SetActive(false);
            characterSelectionPanel.SetActive(true);
            PopulateCharacterList();
        }

        private void PopulateCharacterList()
        {
            // Clear existing
            foreach (Transform child in characterListContainer)
            {
                Destroy(child.gameObject);
            }

            CharacterData[] allCharacters = Resources.LoadAll<CharacterData>("Characters");
            
            if (allCharacters == null || allCharacters.Length == 0)
            {
                Debug.LogWarning("[MainMenuUI] No CharacterData found in Resources/Characters!");
                return;
            }

            foreach (var charData in allCharacters)
            {
                if (charData == null || charData.isEnemy) continue;
                CreateCharacterCard(charData);
            }
        }

        private void CreateCharacterCard(CharacterData data)
        {
            GameObject cardObj = new GameObject($"Card_{data.DataId}");
            cardObj.transform.SetParent(characterListContainer, false);
            // GridLayoutGroup overrides size, so we don't need to manually set sizeDelta or LayoutElement preferredWidth
            RectTransform rect = cardObj.AddComponent<RectTransform>();

            Image bg = cardObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.25f, 0.3f, 1f);

            Button btn = cardObj.AddComponent<Button>();
            btn.onClick.AddListener(() => OnCharacterSelected(data));

            VerticalLayoutGroup vLayout = cardObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(20, 20, 20, 20);
            vLayout.spacing = 15;
            vLayout.childControlHeight = false;

            // Portrait
            if (data.portraitSprite != null)
            {
                GameObject portraitObj = new GameObject("Portrait");
                portraitObj.transform.SetParent(cardObj.transform, false);
                RectTransform pRect = portraitObj.AddComponent<RectTransform>();
                pRect.sizeDelta = new Vector2(200, 200);
                Image pImg = portraitObj.AddComponent<Image>();
                pImg.sprite = data.portraitSprite;
                pImg.preserveAspect = true;
            }

            // Name text
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(cardObj.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(0, 50);
            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = data.DisplayName;
            nameText.fontSize = 32;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            if (mainFont != null) nameText.font = mainFont;

            // Stats preview
            GameObject statsObj = new GameObject("StatsText");
            statsObj.transform.SetParent(cardObj.transform, false);
            RectTransform statsRect = statsObj.AddComponent<RectTransform>();
            statsRect.sizeDelta = new Vector2(0, 80);
            TextMeshProUGUI statsText = statsObj.AddComponent<TextMeshProUGUI>();
            statsText.text = $"HP {data.maxHp} / Men {data.maxMental}\n" +
                             $"Atk {data.baseAttack} / Spell {data.spellPower}\n" +
                             $"Armor {data.armor} / MR {data.magicResist}";
            statsText.fontSize = 24;
            statsText.alignment = TextAlignmentOptions.Center;
            statsText.color = new Color(0.8f, 0.8f, 0.8f);
            if (mainFont != null) statsText.font = mainFont;
        }

        private void OnCharacterSelected(CharacterData data)
        {
            Debug.Log($"[MainMenuUI] Selected initial character: {data.DisplayName} ({data.DataId})");
            
            // 파티 리더로 추가
            if (RunManager.Instance != null)
            {
                RunManager.Instance.StartNewRun();
                RunManager.Instance.AddPartyMember(data);
                
                // TODO: 실제 보유 카드로 등록 (테스트용으로 명함 부여)
                if (TheLastArk.Managers.ResourceManager.Instance != null)
                {
                    TheLastArk.Managers.ResourceManager.Instance.AddCharacterCard(data.DataId, 1);
                }

                // 다음 씬으로 전환
                SceneManager.LoadScene("MapScene");
            }
            else
            {
                Debug.LogError("[MainMenuUI] RunManager.Instance is null!");
            }
        }
    }
}
