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
    public partial class ManagementUIManager : MonoBehaviour
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

            BindInventoryChanges();
            currentTab = initialTab;
            popupPanel.transform.SetAsLastSibling();
            popupPanel.SetActive(true);
            RefreshTab();
        }

        public void Hide()
        {
            if (equipSelectModalPanel != null) equipSelectModalPanel.SetActive(false);
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
            SettingsPopupUI.Show();
        }

        public void GrantDebugGold(int amount = 1000)
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.AddGold(amount);
            }
            NotificationManager.Instance?.ShowMessage($"[디버그] {amount} Gold 지급 완료!", Color.yellow);
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
                NotificationManager.Instance?.ShowMessage($"[디버그] 롱소드 {count}개 지급 완료!", Color.cyan);
            }
            else
            {
                NotificationManager.Instance?.ShowMessage("[디버그] 롱소드 장비 데이터를 찾을 수 없습니다.", Color.red);
            }
        }

        private void CreateUI()
        {
            BuildShell();
        }

        private void CreateTabButton(Transform parent, TabType tab, string label)
        {
            ManagementTheme.Button(parent, $"TabBtn_{tab}", label, () =>
            {
                currentTab = tab;
                RefreshTab();
            });
        }

        private void RefreshTab()
        {
            if (charDetailPopupPanel != null) charDetailPopupPanel.SetActive(false);
            if (equipSelectModalPanel != null) equipSelectModalPanel.SetActive(false);
            // Clear content container
            foreach (Transform child in contentContainer)
            {
                child.gameObject.SetActive(false); Destroy(child.gameObject);
            }

            // Update Tab Button Highlight
            foreach (Transform tabBtn in tabButtonsContainer)
            {
                Image img = tabBtn.GetComponent<Image>();
                if (img != null)
                {
                    bool isSelected = tabBtn.name == $"TabBtn_{currentTab}";
                    img.color = isSelected ? ManagementTheme.Raised : ManagementTheme.Surface;
                    tabBtn.GetComponentInChildren<TextMeshProUGUI>().color = isSelected ? ManagementTheme.Gold : ManagementTheme.Muted;
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
            var root = ManagementTheme.Rect("TrainPanel", contentContainer);
            root.gameObject.AddComponent<TrainManagementUI>().BuildEmbedded(root);
        }

        // ─────────────────────────────────────────────────────────────
        // 2. 캐릭터 & 덱 탭 (Picture 2 디자인 완벽 적용)
        // ─────────────────────────────────────────────────────────────
        private void BuildCharacterTab()
        {
            BuildCrewView();
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
            le.preferredHeight = inParty ? 330 : 260;

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

            if (inParty)
            {
                CreatePartyOrderControls(cardObj.transform, charId, isLeader);
            }
        }

        private void CreatePartyOrderControls(Transform parent, string characterId, bool isLeader)
        {
            int partyIndex = RunManager.Instance.State.partyDataIDs.IndexOf(characterId);
            int partyCount = RunManager.Instance.State.partyDataIDs.Count;
            if (partyIndex < 0) return;

            GameObject slotTextObj = new GameObject("PartySlotText");
            slotTextObj.transform.SetParent(parent, false);
            LayoutElement slotTextLayout = slotTextObj.AddComponent<LayoutElement>();
            slotTextLayout.preferredHeight = 26;
            string slotLabel = isLeader ? $"[리더] {partyIndex + 1}번" : $"{partyIndex + 1}번";
            CreateTextUI(slotTextObj.transform, slotLabel, 17, isLeader ? Color.yellow : Color.cyan);

            GameObject controlsObj = new GameObject("PartyOrderControls");
            controlsObj.transform.SetParent(parent, false);
            LayoutElement controlsLayout = controlsObj.AddComponent<LayoutElement>();
            controlsLayout.preferredHeight = 34;

            HorizontalLayoutGroup controls = controlsObj.AddComponent<HorizontalLayoutGroup>();
            controls.spacing = 10;
            controls.childAlignment = TextAnchor.MiddleCenter;
            controls.childControlWidth = false;
            controls.childControlHeight = true;
            controls.childForceExpandWidth = false;

            CreatePartyMoveButton(controlsObj.transform, "<", partyIndex > 0, () =>
            {
                if (RunManager.Instance.MovePartyMember(characterId, -1)) RefreshTab();
            });

            CreatePartyMoveButton(controlsObj.transform, ">", partyIndex < partyCount - 1, () =>
            {
                if (RunManager.Instance.MovePartyMember(characterId, 1)) RefreshTab();
            });
        }

        private void CreatePartyMoveButton(Transform parent, string label, bool interactable, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObj = new GameObject(label == "<" ? "MoveLeftButton" : "MoveRightButton");
            buttonObj.transform.SetParent(parent, false);

            LayoutElement layout = buttonObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 80;
            layout.preferredHeight = 32;

            Image image = buttonObj.AddComponent<Image>();
            image.color = interactable
                ? new Color(0.2f, 0.5f, 0.8f, 1f)
                : new Color(0.25f, 0.25f, 0.25f, 0.65f);

            Button button = buttonObj.AddComponent<Button>();
            button.interactable = interactable;
            button.onClick.AddListener(onClick);
            CreateTextUI(buttonObj.transform, label, 22, interactable ? Color.white : Color.gray);
        }

        // ─────────────────────────────────────────────────────────────
        // Character Detail Popup (7종 스탯, 4종 스킬 호버 툴팁, 장비 2슬롯)
        // ─────────────────────────────────────────────────────────────
        private void CreateCharacterDetailPopupUI()
        {
            BuildDetailShell();
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
            CharacterStatus status = ResolveManagementStatus(data);

            detailTitleText.text = $"{data.DisplayName}\n<size=18>Lv.{status.charLevel}  ·  {status.LevelTitle}</size>";
            detailStandingImage.enabled = detailStandingImage.sprite != null;
            
            // 7종 스탯 (공격력, 주문력, 체력, 정신력, 방어력, 마법저항력, 치명)
            detailStatsText.text = 
                $"공격력: <color=#E8E5D6>{status.FinalAttack:F0}</color>    주문력: <color=#E8E5D6>{status.FinalSpellPower:F0}</color>\n" +
                $"체력: <color=#92C7CD>{status.currentHp:F0} / {status.FinalMaxHp:F0}</color>    정신력: <color=#92C7CD>{status.currentMental:F0} / {status.FinalMaxMental:F0}</color>\n" +
                $"방어력: <color=#E8E5D6>{status.FinalArmor:F0}</color>    마법저항: <color=#E8E5D6>{status.FinalMagicResist:F0}</color>\n" +
                $"치명타율: <color=#C8AE77>{status.FinalCritRate:F0}%</color>";

            // 3. Render 4 Active Skill Icons & Hover Tooltips
            foreach (Transform child in detailSkillsArea) { child.gameObject.SetActive(false); Destroy(child.gameObject); }

            bool isLeaderChar = isLeader;
            int leaderSkillIdx = isLeaderChar ? status.EnsureLeaderExtraSkill() : -1;

            for (int i = 0; i < data.activeSkills.Length; i++)
            {
                int skillIndex = i;
                SkillInfo skill = data.activeSkills[i];
                if (skill == null) continue;
                string displayName = string.IsNullOrWhiteSpace(skill.skillName) ? $"스킬 {i + 1}" : skill.skillName;

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
                    sImg.color = ManagementTheme.Raised; // 일반 선택 스킬 (녹색)
                }
                else if (isLeaderSkill)
                {
                    sImg.color = ManagementTheme.Raised; // 리더 고정 스킬 (금색/황동색)
                }
                else
                {
                    sImg.color = ManagementTheme.Surface; // 미선택 (회색 실루엣)
                }

                Button sBtn = sBtnObj.AddComponent<Button>();
                sBtn.onClick.AddListener(() =>
                {
                    if (isLeaderSkill && isLeaderChar)
                    {
                        NotificationManager.Instance?.ShowMessage("[리더 전용] 리더 전용 스킬입니다 (전투 시 자동 포함)", Color.yellow);
                    }
                    else
                    {
                        NotificationManager.Instance?.ShowMessage("[안내] 장착 스킬은 주점(마을)에서 골드를 지불하여 랜덤 변경할 수 있습니다.", Color.cyan);
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
                    skillLabel = $"[장착] {displayName}";
                    labelColor = ManagementTheme.Cyan;
                }
                else if (isLeaderSkill)
                {
                    skillLabel = isLeaderChar ? $"[리더] {displayName}" : $"[리더] {displayName}";
                    labelColor = ManagementTheme.Gold;
                }
                else
                {
                    skillLabel = string.IsNullOrWhiteSpace(skill.skillName) ? $"스킬 {i + 1}" : skill.skillName;
                    labelColor = ManagementTheme.Muted;
                }

                ManagementTheme.Border(sBtnObj.transform, ManagementTheme.Line);
                ManagementTheme.StyleButton(sBtn);
                var skillText = CreateTextUI(sBtnObj.transform, skillLabel, 19, labelColor);
                skillText.margin = new Vector4(8, 6, 8, 6);
                if (skill.skillIcon != null)
                {
                    var iconRect = ManagementTheme.Rect("SkillArt", sBtnObj.transform);
                    ManagementTheme.Place(iconRect, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-26, -60), new Vector2(26, -8));
                    var icon = iconRect.gameObject.AddComponent<Image>();
                    icon.sprite = skill.skillIcon; icon.preserveAspect = true; icon.raycastTarget = false;
                    ManagementTheme.Place(skillText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0, 0), new Vector2(0, -60));
                }
            }

            // 4. Render 2 Equipment Slots (장비 슬롯 2칸)
            foreach (Transform child in detailEquipmentsArea) { child.gameObject.SetActive(false); Destroy(child.gameObject); }

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
                eqImg.color = equipped != null ? ManagementTheme.Raised : ManagementTheme.Surface;

                Button eqBtn = eqBtnObj.AddComponent<Button>();
                ManagementTheme.StyleButton(eqBtn);
                ManagementTheme.Border(eqBtnObj.transform, ManagementTheme.Line);

                if (equipped == null)
                {
                    // 빈 슬롯: [+] 표시
                    CreateTextUI(eqBtnObj.transform, "+ 장비 장착", 20, ManagementTheme.Muted);
                    eqBtn.onClick.AddListener(() =>
                    {
                        ShowEquipmentSelectModal(data, status, slotIndex);
                    });
                }
                else
                {
                    // 장착된 슬롯: 장비 이름 + 클릭하여 비교 · 교체
                    string eqLabel = $"[{equipped.starLevel}성] {equipped.equipmentName}\n<size=16><color=#92C7CD>클릭하여 비교 · 교체</color></size>";
                    CreateTextUI(eqBtnObj.transform, eqLabel, 20, ManagementTheme.Gold);

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
                        ShowEquipmentSelectModal(data, status, slotIndex);
                    });
                }
            }

            // 5. Deck Toggle Button
            detailDeckButton.onClick.RemoveAllListeners();
            detailDeckButtonText.text = inParty ? "편성에서 제외" : "파티에 편성";
            detailDeckButton.GetComponent<Image>().color = ManagementTheme.Raised;
            detailDeckButton.onClick.AddListener(() =>
            {
                if (RunManager.Instance != null)
                {
                    if (inParty)
                    {
                        RunManager.Instance.RemovePartyMember(charId);
                    }
                    else
                    {
                        if (!RunManager.Instance.CanAddPartyMember(data))
                        {
                            NotificationManager.Instance?.ShowMessage("파티는 최대 4명까지 편성할 수 있습니다!", Color.red);
                            return;
                        }

                        RunManager.Instance.AddPartyMember(data);
                    }
                    RefreshTab();
                    ShowCharacterDetailPopup(data);
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
            BuildEquipmentSelector(charData, status, slotIndex);
        }

        // ─────────────────────────────────────────────────────────────
        // 3. 가방 / 유물 / 장비 탭 (Inventory & Equipment Tab)
        // ─────────────────────────────────────────────────────────────
        private void BuildInventoryTab()
        {
            BuildInventoryGrid();
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
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Min(14, fontSize);
            tmp.fontSizeMax = fontSize;
            tmp.raycastTarget = false;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.font = TMPFontManager.MainKoreanFont;

            return tmp;
        }
    }
}
