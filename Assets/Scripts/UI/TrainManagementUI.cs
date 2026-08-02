using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TheLastArk.Managers;
using TheLastArk.Data;

namespace TheLastArk.UI
{
    public class TrainManagementUI : MonoBehaviour
    {
        private GameObject popupPanel;
        private Transform carsContainer;
        private Transform charactersContainer;
        private TMPro.TMP_FontAsset mainFont;

        // Popup details
        private GameObject detailPopupPanel;
        private Image detailPortrait;
        private TextMeshProUGUI detailStatsText;
        private TextMeshProUGUI detailSkillsText;

        public void Show()
        {
            if (popupPanel == null)
            {
                CreateUI();
            }
            UpdateUI();
            popupPanel.SetActive(true);
        }

        public void Hide()
        {
            if (popupPanel != null) popupPanel.SetActive(false);
            if (detailPopupPanel != null) detailPopupPanel.SetActive(false);
        }

        private void CreateUI()
        {
            mainFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Main_Fonts");

            Canvas canvas = FindObjectOfType<Canvas>();
            popupPanel = new GameObject("TrainManagementPopup");
            popupPanel.transform.SetParent(canvas.transform, false);
            popupPanel.transform.SetAsLastSibling();

            RectTransform rect = popupPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = popupPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.95f);

            // 닫기 버튼
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(popupPanel.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-30, -30);
            closeRect.sizeDelta = new Vector2(100, 50);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);

            CreateTextUI(closeBtnObj.transform, "닫기", 24, Color.white, new Vector2(0, 0), Vector2.zero, Vector2.one);

            // 기차 체력바 영역
            GameObject hpBarArea = new GameObject("TrainHpArea");
            hpBarArea.transform.SetParent(popupPanel.transform, false);
            RectTransform hpRect = hpBarArea.AddComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0.5f, 0.95f);
            hpRect.anchorMax = new Vector2(0.5f, 0.95f);
            hpRect.pivot = new Vector2(0.5f, 1f);
            hpRect.anchoredPosition = new Vector2(0, -20);
            hpRect.sizeDelta = new Vector2(600, 40);

            Image hpBg = hpBarArea.AddComponent<Image>();
            hpBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            GameObject hpFill = new GameObject("Fill");
            hpFill.transform.SetParent(hpBarArea.transform, false);
            RectTransform fillRect = hpFill.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(1, 1); // will be adjusted dynamically
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image hpImg = hpFill.AddComponent<Image>();
            hpImg.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            
            GameObject hpTextObj = new GameObject("HpText");
            hpTextObj.transform.SetParent(hpBarArea.transform, false);
            RectTransform hpTextRect = hpTextObj.AddComponent<RectTransform>();
            hpTextRect.anchorMin = Vector2.zero;
            hpTextRect.anchorMax = Vector2.one;
            hpTextRect.offsetMin = Vector2.zero;
            hpTextRect.offsetMax = Vector2.zero;
            TextMeshProUGUI hpText = hpTextObj.AddComponent<TextMeshProUGUI>();
            hpText.fontSize = 24;
            hpText.alignment = TextAlignmentOptions.Center;
            hpText.color = Color.white;
            if (mainFont != null) hpText.font = mainFont;

            hpBarArea.AddComponent<TrainHpUpdater>().Init(hpRect, fillRect, hpText);

            // Top area: train cars
            GameObject carsArea = new GameObject("CarsArea");
            carsArea.transform.SetParent(popupPanel.transform, false);
            RectTransform carsAreaRect = carsArea.AddComponent<RectTransform>();
            carsAreaRect.anchorMin = new Vector2(0.05f, 0.55f);
            carsAreaRect.anchorMax = new Vector2(0.95f, 0.85f);
            carsAreaRect.offsetMin = Vector2.zero;
            carsAreaRect.offsetMax = Vector2.zero;

            Image carsBg = carsArea.AddComponent<Image>();
            carsBg.color = new Color(0.1f, 0.15f, 0.2f, 1f);

            ScrollRect carsScroll = carsArea.AddComponent<ScrollRect>();
            carsScroll.horizontal = true;
            carsScroll.vertical = false;
            
            GameObject carsViewport = new GameObject("Viewport");
            carsViewport.transform.SetParent(carsArea.transform, false);
            RectTransform cVpRect = carsViewport.AddComponent<RectTransform>();
            cVpRect.anchorMin = Vector2.zero;
            cVpRect.anchorMax = Vector2.one;
            cVpRect.sizeDelta = Vector2.zero;
            carsViewport.AddComponent<RectMask2D>();

            GameObject carsContent = new GameObject("Content");
            carsContent.transform.SetParent(carsViewport.transform, false);
            RectTransform cContentRect = carsContent.AddComponent<RectTransform>();
            cContentRect.anchorMin = new Vector2(0, 0);
            cContentRect.anchorMax = new Vector2(0, 1);
            cContentRect.pivot = new Vector2(0, 0.5f);
            cContentRect.sizeDelta = new Vector2(0, 0);

            HorizontalLayoutGroup carsLayout = carsContent.AddComponent<HorizontalLayoutGroup>();
            carsLayout.padding = new RectOffset(20, 20, 20, 20);
            carsLayout.spacing = 20;
            carsLayout.childControlWidth = true;
            carsLayout.childControlHeight = true;
            carsLayout.childForceExpandWidth = false;

            ContentSizeFitter carsFitter = carsContent.AddComponent<ContentSizeFitter>();
            carsFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            carsScroll.viewport = cVpRect;
            carsScroll.content = cContentRect;
            carsContainer = carsContent.transform;

            // Bottom area: character list header
            GameObject charHeaderArea = new GameObject("CharHeader");
            charHeaderArea.transform.SetParent(popupPanel.transform, false);
            RectTransform chRect = charHeaderArea.AddComponent<RectTransform>();
            chRect.anchorMin = new Vector2(0.05f, 0.45f);
            chRect.anchorMax = new Vector2(0.95f, 0.52f);
            chRect.offsetMin = Vector2.zero;
            chRect.offsetMax = Vector2.zero;

            CreateTextUI(charHeaderArea.transform, "Owned Characters", 32, Color.white, new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(0.5f, 1));

            // Bottom area: character list
            GameObject charsArea = new GameObject("CharactersArea");
            charsArea.transform.SetParent(popupPanel.transform, false);
            RectTransform charsAreaRect = charsArea.AddComponent<RectTransform>();
            charsAreaRect.anchorMin = new Vector2(0.05f, 0.05f);
            charsAreaRect.anchorMax = new Vector2(0.95f, 0.42f);
            charsAreaRect.offsetMin = Vector2.zero;
            charsAreaRect.offsetMax = Vector2.zero;

            Image charsBg = charsArea.AddComponent<Image>();
            charsBg.color = new Color(0.15f, 0.1f, 0.2f, 1f);

            ScrollRect charsScroll = charsArea.AddComponent<ScrollRect>();
            charsScroll.horizontal = true;
            charsScroll.vertical = false;

            GameObject charsViewport = new GameObject("Viewport");
            charsViewport.transform.SetParent(charsArea.transform, false);
            RectTransform chVpRect = charsViewport.AddComponent<RectTransform>();
            chVpRect.anchorMin = Vector2.zero;
            chVpRect.anchorMax = Vector2.one;
            chVpRect.sizeDelta = Vector2.zero;
            charsViewport.AddComponent<RectMask2D>();

            GameObject charsContent = new GameObject("Content");
            charsContent.transform.SetParent(charsViewport.transform, false);
            RectTransform chContentRect = charsContent.AddComponent<RectTransform>();
            chContentRect.anchorMin = new Vector2(0, 0);
            chContentRect.anchorMax = new Vector2(0, 1);
            chContentRect.pivot = new Vector2(0, 0.5f);
            chContentRect.sizeDelta = new Vector2(0, 0);

            HorizontalLayoutGroup charsLayout = charsContent.AddComponent<HorizontalLayoutGroup>();
            charsLayout.padding = new RectOffset(20, 20, 20, 20);
            charsLayout.spacing = 20;
            charsLayout.childControlWidth = true;
            charsLayout.childControlHeight = true;
            charsLayout.childForceExpandWidth = false;

            ContentSizeFitter charsFitter = charsContent.AddComponent<ContentSizeFitter>();
            charsFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            charsScroll.viewport = chVpRect;
            charsScroll.content = chContentRect;
            charactersContainer = charsContent.transform;

            CreateDetailPopup(canvas.transform);
        }

        private void CreateDetailPopup(Transform parent)
        {
            detailPopupPanel = new GameObject("DetailPopup");
            detailPopupPanel.transform.SetParent(parent, false);
            detailPopupPanel.transform.SetAsLastSibling();

            RectTransform rect = detailPopupPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.2f);
            rect.anchorMax = new Vector2(0.8f, 0.8f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = detailPopupPanel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.98f);

            // Illustration
            GameObject illObj = new GameObject("Illustration");
            illObj.transform.SetParent(detailPopupPanel.transform, false);
            RectTransform illRect = illObj.AddComponent<RectTransform>();
            illRect.anchorMin = new Vector2(0.05f, 0.1f);
            illRect.anchorMax = new Vector2(0.45f, 0.9f);
            illRect.offsetMin = Vector2.zero;
            illRect.offsetMax = Vector2.zero;
            detailPortrait = illObj.AddComponent<Image>();
            detailPortrait.preserveAspect = true;

            // Stats text
            GameObject statsObj = new GameObject("StatsText");
            statsObj.transform.SetParent(detailPopupPanel.transform, false);
            RectTransform statsRect = statsObj.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.5f, 0.6f);
            statsRect.anchorMax = new Vector2(0.95f, 0.9f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;
            detailStatsText = statsObj.AddComponent<TextMeshProUGUI>();
            detailStatsText.fontSize = 28;
            detailStatsText.color = Color.white;
            if (mainFont != null) detailStatsText.font = mainFont;

            // Skills text
            GameObject skillsObj = new GameObject("SkillsText");
            skillsObj.transform.SetParent(detailPopupPanel.transform, false);
            RectTransform skillsRect = skillsObj.AddComponent<RectTransform>();
            skillsRect.anchorMin = new Vector2(0.5f, 0.1f);
            skillsRect.anchorMax = new Vector2(0.95f, 0.55f);
            skillsRect.offsetMin = Vector2.zero;
            skillsRect.offsetMax = Vector2.zero;
            detailSkillsText = skillsObj.AddComponent<TextMeshProUGUI>();
            detailSkillsText.fontSize = 24;
            detailSkillsText.color = new Color(0.8f, 0.8f, 0.8f);
            if (mainFont != null) detailSkillsText.font = mainFont;

            // 닫기 버튼
            GameObject closeBtnObj = new GameObject("DetailCloseBtn");
            closeBtnObj.transform.SetParent(detailPopupPanel.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-20, -20);
            closeRect.sizeDelta = new Vector2(80, 40);
            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.color = Color.red;
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => detailPopupPanel.SetActive(false));
            CreateTextUI(closeBtnObj.transform, "X", 24, Color.white, Vector2.zero, Vector2.zero, Vector2.one);

            detailPopupPanel.SetActive(false);
        }

        private TextMeshProUGUI CreateTextUI(Transform parent, string text, int fontSize, Color color, Vector2 anchoredPos, Vector2 minA, Vector2 maxA)
        {
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(parent, false);
            RectTransform rect = txtObj.AddComponent<RectTransform>();
            rect.anchorMin = minA;
            rect.anchorMax = maxA;
            rect.anchoredPosition = anchoredPos;
            if (minA == Vector2.zero && maxA == Vector2.one)
            {
                rect.sizeDelta = Vector2.zero;
            }
            else
            {
                rect.sizeDelta = new Vector2(300, 50); // fallback size
            }
            
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) tmp.font = mainFont;

            return tmp;
        }

        private void UpdateUI()
        {
            UpdateCarsUI();
            UpdateCharactersUI();
        }

        private void UpdateCarsUI()
        {
            foreach (Transform child in carsContainer) Destroy(child.gameObject);

            if (TrainManager.Instance == null) return;

            if (TrainManager.Instance.coreCar != null)
                CreateCarUI(TrainManager.Instance.coreCar);

            foreach (var car in TrainManager.Instance.additionalCars)
            {
                CreateCarUI(car);
            }
        }

        private void CreateCarUI(TrainCar car)
        {
            GameObject carObj = new GameObject($"Car_{car.carName}");
            carObj.transform.SetParent(carsContainer, false);
            RectTransform rect = carObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(250, 0); // width 250
            LayoutElement layout = carObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 250;

            Image bg = carObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.25f, 0.3f, 1f);

            VerticalLayoutGroup vLayout = carObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(15, 15, 15, 15);
            vLayout.spacing = 15;
            vLayout.childControlHeight = false;

            CreateTextUI(carObj.transform, $"[{car.carType}] {car.carName}\n<size=18>Lv.{car.level}</size>", 22, Color.yellow, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform.sizeDelta = new Vector2(220, 60);
            
            string p1 = car.installedParts.Count > 0 ? car.installedParts[0] : "Empty";
            string p2 = car.installedParts.Count > 1 ? car.installedParts[1] : "Empty";
            CreateTextUI(carObj.transform, $"Parts: [{p1}] [{p2}]", 18, Color.cyan, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform.sizeDelta = new Vector2(220, 30);
        }

        private void UpdateCharactersUI()
        {
            foreach (Transform child in charactersContainer) Destroy(child.gameObject);

            if (ResourceManager.Instance == null || RunManager.Instance == null) return;

            var partyDataIDs = RunManager.Instance.State.partyDataIDs;
            var leaderID = RunManager.Instance.State.leaderCharacterID;

            CharacterData[] allCharacters = Resources.LoadAll<CharacterData>("Characters");
            Dictionary<string, CharacterData> characterById = new Dictionary<string, CharacterData>();
            foreach (var data in allCharacters)
            {
                if (data == null || data.isEnemy) continue;
                characterById[data.DataId] = data;
            }

            HashSet<string> displayedCharacterIds = new HashSet<string>();
            foreach (string partyId in partyDataIDs)
            {
                if (!characterById.TryGetValue(partyId, out CharacterData partyData)) continue;

                displayedCharacterIds.Add(partyId);
                bool isLeader = partyId == leaderID;
                CreateCharUI(partyData, isLeader, true);
            }

            foreach (var data in allCharacters)
            {
                if (data == null || data.isEnemy || displayedCharacterIds.Contains(data.DataId)) continue;

                string dataId = data.DataId;
                int cards = ResourceManager.Instance.GetCardCount(dataId);
                if (cards > 0)
                {
                    CreateCharUI(data, false, false);
                }
            }
        }

        private void CreateCharUI(CharacterData data, bool isLeader, bool inParty)
        {
            string charId = data.DataId;
            string charName = data.DisplayName;
            int cardCount = ResourceManager.Instance.GetCardCount(charId);
            int level = ResourceManager.Instance.GetCharacterLevelFromCards(cardCount);
            if (level < 0) level = 0;
            int nextTarget = 0;
            if (level == 0) nextTarget = 3;
            else if (level == 1) nextTarget = 6;
            else if (level == 2) nextTarget = 9;
            else if (level == 3) nextTarget = 18;
            else if (level == 4) nextTarget = cardCount;

            GameObject charObj = new GameObject($"Char_{charName}");
            charObj.transform.SetParent(charactersContainer, false);
            RectTransform rect = charObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220, 0);
            LayoutElement layout = charObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 220;

            Image bg = charObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

            // Leader Highlight Border
            if (isLeader)
            {
                GameObject borderObj = new GameObject("LeaderBorder");
                borderObj.transform.SetParent(charObj.transform, false);
                RectTransform borderRect = borderObj.AddComponent<RectTransform>();
                borderRect.anchorMin = Vector2.zero;
                borderRect.anchorMax = Vector2.one;
                borderRect.offsetMin = new Vector2(-5, -5);
                borderRect.offsetMax = new Vector2(5, 5);
                Image borderImg = borderObj.AddComponent<Image>();
                borderImg.color = new Color(1f, 0.8f, 0f, 1f); // Gold
                borderObj.transform.SetAsFirstSibling();
            }

            Button btn = charObj.AddComponent<Button>();
            btn.onClick.AddListener(() => OnCharacterClicked(data));

            VerticalLayoutGroup vLayout = charObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(10, 10, 10, 10);
            vLayout.spacing = 5;
            vLayout.childControlHeight = false;

            // Portrait
            if (data.portraitSprite != null)
            {
                GameObject pObj = new GameObject("Portrait");
                pObj.transform.SetParent(charObj.transform, false);
                RectTransform pRect = pObj.AddComponent<RectTransform>();
                pRect.sizeDelta = new Vector2(150, 150);
                Image pImg = pObj.AddComponent<Image>();
                pImg.sprite = data.portraitSprite;
                pImg.preserveAspect = true;
            }

            // Name
            CreateTextUI(charObj.transform, $"[{charName}] Lv.{level}", 22, Color.green, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform.sizeDelta = new Vector2(200, 30);
            
            // HP/Mental Status (from RunState)
            var cStatus = RunManager.Instance.State.partyStatuses.Find(s => s.origin != null && s.origin.DataId == charId);
            if (cStatus != null)
            {
                CreateTextUI(charObj.transform, $"HP: {cStatus.currentHp}/{cStatus.FinalMaxHp}", 18, new Color(0.2f, 0.8f, 0.2f), Vector2.zero, Vector2.zero, Vector2.zero).rectTransform.sizeDelta = new Vector2(200, 25);
                CreateTextUI(charObj.transform, $"Mental: {cStatus.currentMental}/{cStatus.FinalMaxMental}", 18, new Color(0.2f, 0.6f, 1f), Vector2.zero, Vector2.zero, Vector2.zero).rectTransform.sizeDelta = new Vector2(200, 25);
            }

            // Soul fragments
            if (level < 4)
            {
                CreateTextUI(charObj.transform, $"Cards: {cardCount}/{nextTarget}", 18, Color.white, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform.sizeDelta = new Vector2(200, 25);
            }
            else
            {
                CreateTextUI(charObj.transform, $"Cards: {cardCount} (MAX)", 18, Color.white, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform.sizeDelta = new Vector2(200, 25);
            }
            
            // Leader Label
            if (isLeader)
            {
                CreateTextUI(charObj.transform, "<Leader>", 20, Color.yellow, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform.sizeDelta = new Vector2(200, 25);
            }

            if (inParty)
            {
                CreatePartyOrderControls(charObj.transform, charId, isLeader);
            }
        }

        private void OnCharacterClicked(CharacterData data)
        {
            ShowDetailPopup(data);
        }

        private void CreatePartyOrderControls(Transform parent, string characterId, bool isLeader)
        {
            int partyIndex = RunManager.Instance.State.partyDataIDs.IndexOf(characterId);
            int partyCount = RunManager.Instance.State.partyDataIDs.Count;
            if (partyIndex < 0) return;

            string slotLabel = isLeader ? $"👑 리더 · {partyIndex + 1}번" : $"{partyIndex + 1}번";
            CreateTextUI(parent, slotLabel, 17, isLeader ? Color.yellow : Color.cyan,
                Vector2.zero, Vector2.zero, Vector2.zero).rectTransform.sizeDelta = new Vector2(200, 25);

            GameObject controlsObj = new GameObject("PartyOrderControls");
            controlsObj.transform.SetParent(parent, false);
            RectTransform controlsRect = controlsObj.AddComponent<RectTransform>();
            controlsRect.sizeDelta = new Vector2(200, 36);

            HorizontalLayoutGroup controls = controlsObj.AddComponent<HorizontalLayoutGroup>();
            controls.spacing = 10;
            controls.childAlignment = TextAnchor.MiddleCenter;
            controls.childControlWidth = false;
            controls.childControlHeight = true;
            controls.childForceExpandWidth = false;

            CreatePartyMoveButton(controlsObj.transform, "◀", partyIndex > 0, () =>
            {
                if (RunManager.Instance.MovePartyMember(characterId, -1)) UpdateCharactersUI();
            });

            CreatePartyMoveButton(controlsObj.transform, "▶", partyIndex < partyCount - 1, () =>
            {
                if (RunManager.Instance.MovePartyMember(characterId, 1)) UpdateCharactersUI();
            });
        }

        private void CreatePartyMoveButton(Transform parent, string label, bool interactable, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObj = new GameObject(label == "◀" ? "MoveLeftButton" : "MoveRightButton");
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80, 34);

            LayoutElement layout = buttonObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 80;
            layout.preferredHeight = 34;

            Image image = buttonObj.AddComponent<Image>();
            image.color = interactable
                ? new Color(0.2f, 0.5f, 0.8f, 1f)
                : new Color(0.25f, 0.25f, 0.25f, 0.65f);

            Button button = buttonObj.AddComponent<Button>();
            button.interactable = interactable;
            button.onClick.AddListener(onClick);
            CreateTextUI(buttonObj.transform, label, 22, interactable ? Color.white : Color.gray,
                Vector2.zero, Vector2.zero, Vector2.one);
        }

        private void ShowDetailPopup(CharacterData data)
        {
            if (data.standingSprite != null)
            {
                detailPortrait.sprite = data.standingSprite;
            }
            else if (data.portraitSprite != null)
            {
                detailPortrait.sprite = data.portraitSprite;
            }
            
            // Calculate the current stat values from owned character cards.
            CharacterStatus tempStatus = new CharacterStatus(data);
            
            detailStatsText.text = $"<color=yellow>[ {data.DisplayName} ]</color>\n" +
                                   $"HP: {tempStatus.FinalMaxHp}\n" +
                                   $"Mental: {tempStatus.FinalMaxMental}\n" +
                                   $"Attack: {tempStatus.FinalAttack}\n" +
                                   $"Spell: {tempStatus.FinalSpellPower}\n" +
                                   $"Armor: {tempStatus.FinalArmor}\n" +
                                   $"Magic Resist: {tempStatus.FinalMagicResist}";

            string skillText = "<color=cyan>-- Skills --</color>\n";
            if (data.passiveSkill != null && !string.IsNullOrEmpty(data.passiveSkill.skillName))
            {
                skillText += $"[Passive] {data.passiveSkill.skillName}\n";
            }
            foreach (var s in data.activeSkills)
            {
                if (s != null && !string.IsNullOrEmpty(s.skillName))
                {
                    skillText += $"[Active] {s.skillName} (Cost: {s.baseCost})\n";
                }
            }
            detailSkillsText.text = skillText;

            detailPopupPanel.SetActive(true);
        }
    }

    // Helper to auto update HP bar
    public class TrainHpUpdater : MonoBehaviour
    {
        private RectTransform fill;
        private TextMeshProUGUI text;

        public void Init(RectTransform bgRect, RectTransform fillRect, TextMeshProUGUI tmp)
        {
            fill = fillRect;
            text = tmp;
        }

        private void Update()
        {
            if (TrainManager.Instance != null)
            {
                float cur = TrainManager.Instance.currentTrainDurability;
                float max = TrainManager.Instance.maxTrainDurability;
                float ratio = max > 0 ? cur / max : 0;
                
                if (fill != null) fill.anchorMax = new Vector2(ratio, 1f);
                if (text != null) text.text = $"기차 내구도: {cur} / {max}";
            }
        }
    }
}


