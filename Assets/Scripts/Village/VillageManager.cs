using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TheLastArk.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

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
        private bool restUsed;
        private TheLastArk.Managers.ResourceManager observedResources;
        private readonly Dictionary<string, Button> facilityButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, TextMeshProUGUI> facilityStates = new Dictionary<string, TextMeshProUGUI>();

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

            // Keep both the character and facility controls between the HUD frame bands.
            var safeArea = ManagementTheme.Rect("VillageContent", canvas.transform);
            ManagementTheme.Place(safeArea, new Vector2(0.04f, 0.19f), new Vector2(0.96f, 0.83f));
            CreateLeftPanel(safeArea);
            CreateRightPanel(safeArea);

            // TopBar (ExplorationResourceUI)
            Transform existingRes = canvas.transform.Find("ResourcePanel");
            if (existingRes == null)
            {
                var resourceUI = gameObject.GetComponent<ExplorationResourceUI>();
                if (resourceUI == null) resourceUI = gameObject.AddComponent<ExplorationResourceUI>();
                resourceUI.Initialize(canvas.transform);
            }
            observedResources = TheLastArk.Managers.ResourceManager.Instance;
            observedResources.OnRelicsChanged += UpdateRemainingChoicesUI;
        }

        private void OnDestroy()
        {
            if (observedResources != null) observedResources.OnRelicsChanged -= UpdateRemainingChoicesUI;
        }

        private void CreateBackground(Transform canvasTransform)
        {
            var background = ManagementTheme.Panel("VillageBackground", canvasTransform, new Color(0.10f, 0.15f, 0.20f));
            background.SetAsFirstSibling();
            var image = background.GetComponent<Image>();
            image.raycastTarget = false;
            if (backgroundImage != null)
            {
                image.sprite = backgroundImage; image.color = Color.white;
                image.preserveAspect = false;
                var aspect = background.gameObject.AddComponent<AspectRatioFitter>();
                aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                aspect.aspectRatio = backgroundImage.rect.width / backgroundImage.rect.height;
            }
            var shade = ManagementTheme.Panel("VillageShade", canvasTransform, new Color(0.02f, 0.04f, 0.06f, 0.15f));
            shade.SetSiblingIndex(1); shade.GetComponent<Image>().raycastTarget = false;
        }

        private void CreateLeftPanel(Transform parent)
        {
            var left = ManagementTheme.Rect("LeftPanel_LeaderIllustration", parent);
            ManagementTheme.Place(left, Vector2.zero, new Vector2(0.46f, 1));
            string leaderId = RunManager.Instance.State.leaderCharacterID;
            var data = Resources.LoadAll<CharacterData>("Characters").FirstOrDefault(c => c != null && c.DataId == leaderId);
            var artRect = ManagementTheme.Rect("LeaderArtwork", left);
            ManagementTheme.Place(artRect, Vector2.zero, Vector2.one, new Vector2(16, 92), new Vector2(-16, -8));
            var art = artRect.gameObject.AddComponent<Image>();
            art.sprite = data != null ? data.standingSprite : null;
            art.preserveAspect = true; art.raycastTarget = false; art.enabled = art.sprite != null;
            if (art.sprite == null)
            {
                var empty = ManagementTheme.Label(artRect, "탐험을 이끌 승무원을 기다리는 중", 23, ManagementTheme.Text, TextAlignmentOptions.Center);
                empty.raycastTarget = false;
            }
            var nameplate = ManagementTheme.Panel("LeaderNameplate", left, ManagementTheme.Background, true);
            ManagementTheme.Place(nameplate, Vector2.zero, new Vector2(1, 0), new Vector2(24, 0), new Vector2(-24, 78));
            var tag = ManagementTheme.Label(nameplate, "원정대 리더", 16, ManagementTheme.Gold);
            ManagementTheme.Top(tag.rectTransform, 10, 22, 20, 20);
            var name = ManagementTheme.Label(nameplate, data != null ? data.DisplayName : "리더 미지정", 27);
            ManagementTheme.Top(name.rectTransform, 34, 36, 20, 20);
        }

        private void CreateRightPanel(Transform canvasTransform)
        {
            var panel = ManagementTheme.Panel("VillageFacilities", canvasTransform, ManagementTheme.Background, true);
            ManagementTheme.Place(panel, new Vector2(0.50f, 0), Vector2.one);
            var title = ManagementTheme.Label(panel, "마을 정박", 31);
            ManagementTheme.Top(title.rectTransform, 18, 40, 24, 180);
            remainingChoicesText = ManagementTheme.Label(panel, "", 21, ManagementTheme.Gold, TextAlignmentOptions.MidlineRight);
            ManagementTheme.Top(remainingChoicesText.rectTransform, 18, 40, 240, 24);
            var hint = ManagementTheme.Label(panel, "첫 방문에 행동 1회 소모 · 방문한 시설은 자유롭게 재이용", 17, ManagementTheme.Muted);
            ManagementTheme.Top(hint.rectTransform, 66, 30, 24, 24);
            var scroll = ManagementTheme.Scroll(panel, "FacilityScroll", out var menu);
            ManagementTheme.Place(scroll, Vector2.zero, Vector2.one, new Vector2(20, 84), new Vector2(-20, -108));
            var layout = menu.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10; layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            var rightObj = menu.gameObject;
            var mainFont = TMPFontManager.MainKoreanFont;

            // 개편된 마을 선택지 (상점가, 대장간, 주점, 정비소, 휴식)
            CreateMenuButton(rightObj.transform, "상점가 (소모품/유물 거래)", () => TryEnterFacility("Shop", () => 
            {
                var shopUI = FindObjectOfType<ShopUI>();
                if (shopUI == null) 
                {
                    GameObject uiObj = new GameObject("ShopUI");
                    shopUI = uiObj.AddComponent<ShopUI>();
                }
                shopUI.Show();
            }), mainFont);

            CreateMenuButton(rightObj.transform, "대장간 (장비 구매/합성)", () => TryEnterFacility("Forge", () => 
            {
                var forgeUI = FindObjectOfType<ForgeUI>();
                if (forgeUI == null) 
                {
                    GameObject uiObj = new GameObject("ForgeUI");
                    forgeUI = uiObj.AddComponent<ForgeUI>();
                }
                forgeUI.Show();
            }), mainFont);
            
            CreateMenuButton(rightObj.transform, "주점 (용병 구매/스킬 변경)", () => TryEnterFacility("Tavern", () => 
            {
                var tavernUI = FindObjectOfType<TavernUI>();
                if (tavernUI == null) 
                {
                    GameObject uiObj = new GameObject("TavernUI");
                    tavernUI = uiObj.AddComponent<TavernUI>();
                }
                tavernUI.Show();
            }), mainFont);

            CreateMenuButton(rightObj.transform, "정비소 (기차 정비/강화)", () => TryEnterFacility("Maintenance", () => 
            {
                MaintenanceUI.Show();
            }), mainFont);
            
            // Rest Button
            GameObject restBtnObj = CreateMenuButton(rightObj.transform, "휴식 (체력/정신력/기차 25% 회복)", () => TryEnterFacility("Rest", ApplyRestEffect), mainFont);
            restButton = restBtnObj.GetComponent<Button>();

            // (기차/캐릭터/가방/환경설정 관리는 상단바 아이콘으로 이동됨)

            // Return to map button
            CreateMenuButton(panel, "탐색 계속", () => 
            {
                Debug.Log("[VillageManager] 마을 나가기 -> MapScene 로드");
                SceneManager.LoadScene("MapScene");
            }, mainFont, ManagementTheme.Gold);
            UpdateRemainingChoicesUI();
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
                UpdateRemainingChoicesUI();
            }
            else
            {
                Debug.LogWarning("[VillageManager] 더 이상 새로운 시설을 방문할 수 없습니다. (행동 횟수 소진)");
            }
        }

        private void UpdateRemainingChoicesUI()
        {
            if (remainingChoicesText != null) remainingChoicesText.text = $"남은 행동   {remainingChoices} / 3";
            bool freeRest = TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.FreeRest);
            foreach (var pair in facilityButtons)
            {
                bool used = pair.Key == "Rest" && restUsed;
                bool open = unlockedFacilities.Contains(pair.Key);
                bool free = pair.Key == "Rest" && freeRest;
                bool allowed = !used && (open || remainingChoices > 0 || free);
                pair.Value.interactable = allowed;
                var status = facilityStates[pair.Key];
                status.text = used ? "휴식 완료" : open ? "재방문 가능" : free ? "행동 소모 없음" : allowed ? "행동 1회" : "행동 부족";
                status.color = used || !allowed ? ManagementTheme.Muted : open || free ? ManagementTheme.Cyan : ManagementTheme.Gold;
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

            restUsed = true;
            UpdateRemainingChoicesUI();
        }

        private GameObject CreateMenuButton(Transform parent, string text, UnityEngine.Events.UnityAction onClickAction, TMP_FontAsset font, Color? bgColor = null)
        {
            bool depart = text == "탐색 계속";
            var button = ManagementTheme.Button(parent, $"MenuBtn_{text}", "", onClickAction, depart);
            var rect = (RectTransform)button.transform;
            var title = button.GetComponentInChildren<TextMeshProUGUI>();
            title.alignment = TextAlignmentOptions.MidlineLeft; title.fontSize = 24;
            if (depart)
            {
                ManagementTheme.Place(rect, Vector2.zero, new Vector2(1, 0), new Vector2(22, 18), new Vector2(-22, 66));
                title.text = "탐색 계속   →"; title.alignment = TextAlignmentOptions.Center;
                return button.gameObject;
            }
            ManagementTheme.Height(rect, 90);
            string id, heading, description;
            if (text.StartsWith("상점가")) { id = "Shop"; heading = "상점"; description = "소모품과 유물 거래"; }
            else if (text.StartsWith("대장간")) { id = "Forge"; heading = "대장간"; description = "장비 구매 · 장비 합성"; }
            else if (text.StartsWith("주점")) { id = "Tavern"; heading = "주점"; description = "용병 영입 · 출전 스킬 변경"; }
            else if (text.StartsWith("정비소")) { id = "Maintenance"; heading = "정비소"; description = "기차 칸 강화 · 건설 · 파츠 장착"; }
            else { id = "Rest"; heading = "휴식"; description = "체력 · 정신력 · 기차 내구도 25% 회복"; }
            title.text = heading; ManagementTheme.Top(title.rectTransform, 14, 32, 20, 180);
            var detail = ManagementTheme.Label(rect, description, 17, ManagementTheme.Muted);
            ManagementTheme.Top(detail.rectTransform, 53, 27, 20, 20);
            var state = ManagementTheme.Label(rect, "", 18, ManagementTheme.Gold, TextAlignmentOptions.MidlineRight);
            ManagementTheme.Place(state.rectTransform, Vector2.one, Vector2.one, new Vector2(-190, -46), new Vector2(-20, -14));
            facilityButtons[id] = button; facilityStates[id] = state;
            return button.gameObject;
        }
    }
}
