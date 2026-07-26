using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TheLastArk.UI;

namespace TheLastArk.Village
{
    public class VillageManager : MonoBehaviour
    {
        [Header("Background Settings")]
        public Sprite backgroundImage;

        private RectTransform canvasRect;

        private int remainingChoices = 3;
        private System.Collections.Generic.HashSet<string> unlockedFacilities = new System.Collections.Generic.HashSet<string>();
        private TMPro.TextMeshProUGUI remainingChoicesText;
        private Button restButton;

        void Start()
        {
            SetupDefaultUI();
        }

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
                GameObject canvasObj = new GameObject("VillageCanvas");
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
            canvasRect = canvas.GetComponent<RectTransform>();

            // Background Image
            CreateBackground(canvas.transform);

            // Left Panel (Leader Illustration)
            CreateLeftPanel(canvas.transform);

            // Right Panel (Menus)
            CreateRightPanel(canvas.transform);

            // TopBar (ExplorationResourceUI)
            Transform existingRes = canvas.transform.Find("ResourcePanel");
            if (existingRes == null)
            {
                var resourceUI = gameObject.GetComponent<ExplorationResourceUI>();
                if (resourceUI == null) resourceUI = gameObject.AddComponent<ExplorationResourceUI>();
                resourceUI.Initialize(canvas.transform);
            }
        }

        private void CreateBackground(Transform canvasTransform)
        {
            GameObject bgObj = new GameObject("VillageBackground");
            bgObj.transform.SetParent(canvasTransform, false);
            bgObj.transform.SetAsFirstSibling();

            Image bgImage = bgObj.AddComponent<Image>();
            
            if (backgroundImage != null)
            {
                bgImage.sprite = backgroundImage;
                bgImage.color = Color.white;
            }
            else
            {
                bgImage.color = new Color(0.1f, 0.15f, 0.2f, 1f); // Placeholder color
            }
            
            // Set preserveAspect so a 1920x1080 image won't stretch weirdly on different aspect ratios
            bgImage.preserveAspect = true;

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(1920, 1080);
        }

        private void CreateLeftPanel(Transform canvasTransform)
        {
            GameObject leftObj = new GameObject("LeftPanel_LeaderIllustration");
            leftObj.transform.SetParent(canvasTransform, false);

            RectTransform leftRect = leftObj.AddComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0, 0);
            leftRect.anchorMax = new Vector2(0.5f, 1); // Left half of screen
            leftRect.offsetMin = new Vector2(0, 0);
            leftRect.offsetMax = new Vector2(0, -80); // Leave top for resource bar

            Image illImg = leftObj.AddComponent<Image>();
            illImg.color = new Color(1, 1, 1, 0); // Transparent unless we have sprite

            // Create Text for remaining choices above the character
            GameObject textObj = new GameObject("RemainingChoicesText");
            textObj.transform.SetParent(leftObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 1);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0.5f, 1);
            textRect.anchoredPosition = new Vector2(0, -20);
            textRect.sizeDelta = new Vector2(0, 50);

            remainingChoicesText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            remainingChoicesText.text = $"남은 선택지: {remainingChoices} / 3";
            remainingChoicesText.fontSize = 36;
            remainingChoicesText.alignment = TMPro.TextAlignmentOptions.Center;
            remainingChoicesText.color = Color.yellow;
            TMPro.TMP_FontAsset mainFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Main_Fonts");
            if (mainFont != null) remainingChoicesText.font = mainFont;

            // Try to load leader illustration
            if (RunManager.Instance != null && RunManager.Instance.State != null)
            {
                string leaderId = RunManager.Instance.State.leaderCharacterID;
                if (!string.IsNullOrEmpty(leaderId))
                {
                    CharacterData charData = null;
                    CharacterData[] allCharacters = Resources.LoadAll<CharacterData>("Characters");
                    foreach (var data in allCharacters)
                    {
                        if (data.DataId == leaderId)
                        {
                            charData = data;
                            break;
                        }
                    }

                    if (charData != null && charData.standingSprite != null)
                    {
                        illImg.sprite = charData.standingSprite;
                        illImg.color = Color.white;
                        illImg.preserveAspect = true;
                        
                        // Push image down a bit to make room for the text
                        RectTransform imgRect = illImg.GetComponent<RectTransform>();
                        imgRect.offsetMax = new Vector2(0, -80);

                        Debug.Log($"[VillageManager] 리더 캐릭터({leaderId}) 일러스트 로드 완료.");
                    }
                    else
                    {
                        Debug.LogWarning($"[VillageManager] 리더 캐릭터({leaderId}) 데이터 또는 standingSprite를 찾을 수 없습니다.");
                    }
                }
            }
        }

        private void CreateRightPanel(Transform canvasTransform)
        {
            GameObject rightObj = new GameObject("RightPanel_Menus");
            rightObj.transform.SetParent(canvasTransform, false);

            RectTransform rightRect = rightObj.AddComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.6f, 0);
            rightRect.anchorMax = new Vector2(1f, 1);
            rightRect.offsetMin = new Vector2(0, 100);
            rightRect.offsetMax = new Vector2(-100, -150);

            VerticalLayoutGroup vLayout = rightObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(50, 50, 50, 50);
            vLayout.spacing = 30;
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.childControlHeight = false;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.childForceExpandWidth = true;

            TMPro.TMP_FontAsset mainFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Main_Fonts");

            // Buttons: 상점가, 주점, 훈련실, 정비소, 휴식, 기차 관리, 탐색 계속
            CreateMenuButton(rightObj.transform, "상점가 (소모품/유물/장비 거래)", () => TryEnterFacility("Shop", () => 
            {
                var shopUI = FindObjectOfType<ShopUI>();
                if (shopUI == null) 
                {
                    GameObject uiObj = new GameObject("ShopUI");
                    shopUI = uiObj.AddComponent<ShopUI>();
                }
                shopUI.Show();
            }), mainFont);
            
            CreateMenuButton(rightObj.transform, "주점 (용병 카드 구매)", () => TryEnterFacility("Tavern", () => { Debug.Log("주점 - 아직 구현되지 않았습니다."); }), mainFont);
            CreateMenuButton(rightObj.transform, "훈련실 (용병 스킬 변경)", () => TryEnterFacility("TrainingRoom", () => { Debug.Log("훈련실 - 아직 구현되지 않았습니다."); }), mainFont);
            CreateMenuButton(rightObj.transform, "정비소 (기차 강화)", () => TryEnterFacility("Maintenance", () => { Debug.Log("정비소 - 아직 구현되지 않았습니다."); }), mainFont);
            
            // Rest Button
            GameObject restBtnObj = CreateMenuButton(rightObj.transform, "휴식 (체력/정신력/기차 25% 회복)", () => TryEnterFacility("Rest", ApplyRestEffect), mainFont);
            restButton = restBtnObj.GetComponent<Button>();

            // 기차 관리 (Does not consume action points)
            CreateMenuButton(rightObj.transform, "기차 관리", () => 
            {
                var trainUI = FindObjectOfType<TrainManagementUI>();
                if (trainUI == null) 
                {
                    GameObject uiObj = new GameObject("TrainManagementUI");
                    trainUI = uiObj.AddComponent<TrainManagementUI>();
                }
                trainUI.Show();
            }, mainFont, new Color(0.2f, 0.4f, 0.6f, 1f));

            // Return to map button
            CreateMenuButton(rightObj.transform, "탐색 계속", () => 
            {
                Debug.Log("[VillageManager] 마을 나가기 -> MapScene 로드");
                SceneManager.LoadScene("MapScene");
            }, mainFont, new Color(0.6f, 0.2f, 0.2f, 1f));
        }

        private void TryEnterFacility(string facilityId, System.Action onEnter)
        {
            if (unlockedFacilities.Contains(facilityId))
            {
                // Already unlocked, enter freely
                onEnter?.Invoke();
                return;
            }

            bool isFreeRest = (facilityId == "Rest" && TheLastArk.Managers.ResourceManager.Instance != null && TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.FreeRest));

            if (remainingChoices > 0 || isFreeRest)
            {
                // Unlock and enter
                if (!isFreeRest)
                {
                    remainingChoices--;
                    UpdateRemainingChoicesUI();
                }
                
                unlockedFacilities.Add(facilityId);
                onEnter?.Invoke();
            }
            else
            {
                Debug.LogWarning("[VillageManager] 더 이상 새로운 시설을 방문할 수 없습니다. (행동 횟수 소진)");
            }
        }

        private void UpdateRemainingChoicesUI()
        {
            if (remainingChoicesText != null)
            {
                remainingChoicesText.text = $"남은 선택지: {remainingChoices} / 3";
            }
        }

        private void ApplyRestEffect()
        {
            Debug.Log("[VillageManager] 휴식 진행: 파티 및 기차 회복!");

            // 1. Train Heal
            if (TheLastArk.Managers.TrainManager.Instance != null)
            {
                int healAmount = Mathf.RoundToInt(TheLastArk.Managers.TrainManager.Instance.maxTrainDurability * 0.25f);
                TheLastArk.Managers.TrainManager.Instance.IncreaseDurability(healAmount);
            }

            // 2. Party Heal
            if (RunManager.Instance != null && RunManager.Instance.State != null)
            {
                float bonusHealRatio = 0f;
                if (TheLastArk.Managers.ResourceManager.Instance != null)
                {
                    bonusHealRatio = TheLastArk.Managers.ResourceManager.Instance.GetRelicBonus(TheLastArk.Data.RelicEffectType.RestBonusHeal);
                }

                float totalHealRatio = 0.25f + bonusHealRatio;

                foreach (var status in RunManager.Instance.State.partyStatuses)
                {
                    status.currentHp = Mathf.Min(status.FinalMaxHp, status.currentHp + (status.FinalMaxHp * totalHealRatio));
                    status.currentMental = Mathf.Min(status.FinalMaxMental, status.currentMental + (status.FinalMaxMental * totalHealRatio));
                }
            }

            // 휴식은 1회성. 버튼 비활성화
            if (restButton != null)
            {
                restButton.interactable = false;
                restButton.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
                var txt = restButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (txt != null) txt.text = "휴식 (사용 완료)";
            }
        }

        private GameObject CreateMenuButton(Transform parent, string text, UnityEngine.Events.UnityAction onClickAction, TMPro.TMP_FontAsset font, Color? bgColor = null)
        {
            GameObject btnObj = new GameObject($"MenuBtn_{text}");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 80);

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor ?? new Color(0.2f, 0.2f, 0.2f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClickAction);

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            TMPro.TextMeshProUGUI tmp = txtObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 32;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;
            if (font != null) tmp.font = font;

            return btnObj;
        }
    }
}
