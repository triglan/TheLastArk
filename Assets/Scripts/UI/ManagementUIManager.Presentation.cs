using System.Collections.Generic;
using System.Linq;
using TheLastArk.Character;
using TheLastArk.Data;
using TheLastArk.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    public partial class ManagementUIManager
    {
        private InventoryCategory inventoryCategory;
        private InventorySort inventorySort;
        private string selectedInventoryKey;
        private TextMeshProUGUI inventoryTitle, inventoryDescription;
        private readonly Dictionary<string, UnityEngine.UI.Image> inventoryCards = new Dictionary<string, UnityEngine.UI.Image>();
        private ResourceManager boundResources;
        private readonly Dictionary<string, CharacterStatus> previewStatuses = new Dictionary<string, CharacterStatus>();
        private List<CharacterStatus> previewRunStatuses;

        private CharacterStatus ResolveManagementStatus(CharacterData data)
        {
            var runStatuses = RunManager.Instance.State.partyStatuses;
            if (!ReferenceEquals(previewRunStatuses, runStatuses))
            {
                previewStatuses.Clear();
                previewRunStatuses = runStatuses;
            }
            // The battle scene can be started directly, before run statuses are populated.
            var battle = FindObjectOfType<BattleManager>();
            var live = battle != null ? battle.playerParty.FirstOrDefault(c => c != null && c.status?.origin != null && c.status.origin.DataId == data.DataId) : null;
            var status = live != null ? live.status : runStatuses.FirstOrDefault(s => s?.origin != null && s.origin.DataId == data.DataId);
            if (status == null && !previewStatuses.TryGetValue(data.DataId, out status)) status = new CharacterStatus(data);
            ResourceManager.Instance.EquipmentLoadouts.ApplyTo(status);
            previewStatuses[data.DataId] = status;
            return status;
        }

        private bool SetManagementEquipment(CharacterStatus status, int slot, EquipmentData equipment)
        {
            if (ResourceManager.Instance.TryEquip(status, slot, equipment, out string reason)) return true;
            NotificationManager.Instance?.ShowMessage(reason, ManagementTheme.Gold);
            return false;
        }

        private void BindInventoryChanges()
        {
            if (boundResources != null) return;
            boundResources = ResourceManager.Instance;
            boundResources.OnEquipmentsChanged += RefreshVisibleInventory;
            boundResources.OnRelicsChanged += RefreshVisibleInventory;
            boundResources.OnConsumablesChanged += RefreshVisibleInventory;
        }

        private void RefreshVisibleInventory()
        {
            if (popupPanel != null && popupPanel.activeSelf && currentTab == TabType.Inventory) RefreshTab();
        }

        private void OnDestroy()
        {
            if (boundResources != null)
            {
                boundResources.OnEquipmentsChanged -= RefreshVisibleInventory;
                boundResources.OnRelicsChanged -= RefreshVisibleInventory;
                boundResources.OnConsumablesChanged -= RefreshVisibleInventory;
            }
            if (popupPanel != null) Destroy(popupPanel);
            if (instance == this) instance = null;
        }

        private void BuildShell()
        {
            var canvas = HudCanvasUtility.FindOverlayCanvas();
            if (canvas == null)
            {
                var go = new GameObject("ManagementCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 90;
                var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }
            var main = ManagementTheme.Panel("ManagementPopupPanel", canvas.transform, ManagementTheme.Background, true);
            ManagementPanelLayout.DockRight(main);
            popupPanel = main.gameObject;
            // A solid inner surface prevents the battle portraits from bleeding through the text.
            var header = ManagementTheme.Panel("Header", main, ManagementTheme.Surface);
            ManagementTheme.Top(header, 1, 78, 1, 1);
            var title = ManagementTheme.Label(header, "방주 관리", 29, ManagementTheme.Text);
            ManagementTheme.Place(title.rectTransform, Vector2.zero, Vector2.one, new Vector2(24, 20), new Vector2(-110, -10));
            var sub = ManagementTheme.Label(header, "THE LAST ARK  /  CREW & CARGO", 12, ManagementTheme.Gold);
            ManagementTheme.Place(sub.rectTransform, Vector2.zero, Vector2.one, new Vector2(26, 2), new Vector2(-110, -49));
            var close = ManagementTheme.Button(header, "CloseButton", "닫기", Hide);
            ManagementTheme.Place((RectTransform)close.transform, new Vector2(1, 0), Vector2.one, new Vector2(-94, 18), new Vector2(-20, -18));
            var tabs = ManagementTheme.Rect("TopTabBar", main);
            ManagementTheme.Top(tabs, 92, 48, 20, 20);
            ManagementTheme.RowLayout(tabs, 8);
            tabButtonsContainer = tabs;
            CreateTabButton(tabs, TabType.Train, "01   기차 관리");
            CreateTabButton(tabs, TabType.Character, "02   승무원");
            CreateTabButton(tabs, TabType.Inventory, "03   인벤토리");
            var content = ManagementTheme.Rect("ContentArea", main);
            ManagementTheme.Place(content, Vector2.zero, Vector2.one, new Vector2(20, 32), new Vector2(-20, -158));
            contentContainer = content;
            var footer = ManagementTheme.Label(main, "THE LAST ARK     ·     관리 기록", 12, ManagementTheme.Muted);
            ManagementTheme.Place(footer.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(24, 5), new Vector2(-24, 25));
            BuildDetailShell();
        }

        private void BuildCrewView()
        {
            var root = ManagementTheme.Rect("CharacterRoot", contentContainer);
            var party = RunManager.Instance.State.partyDataIDs;
            string leader = RunManager.Instance.State.leaderCharacterID;
            var heading = ManagementTheme.Label(root, $"승무원 명부  <size=18><color=#A1B1B5>편성 {party.Count} / 4</color></size>", 25);
            ManagementTheme.Top(heading.rectTransform, 0, 38);
            var synergy = SynergyCalculator.CalculateActiveSynergies();
            string synergyText = string.Join("   ·   ", synergy.Select(pair => $"{SynergyDatabase.GetInfo(pair.Key)?.displayName ?? pair.Key.ToString()} {pair.Value}"));
            var summary = ManagementTheme.Label(root, string.IsNullOrEmpty(synergyText) ? "활성 시너지 없음" : synergyText, 17, ManagementTheme.Cyan);
            ManagementTheme.Top(summary.rectTransform, 40, 50);
            summary.gameObject.AddComponent<BattleHoverTooltip>().SetContent(synergyText);
            var scroll = ManagementTheme.Scroll(root, "CrewScroll", out var content);
            ManagementTheme.Place(scroll, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -104));
            content.gameObject.AddComponent<GridLayoutGroup>();
            content.gameObject.AddComponent<ManagementGridLayout>().Configure(300, 226, 2);
            var all = Resources.LoadAll<CharacterData>("Characters").Where(d => d != null && !d.isEnemy)
                .GroupBy(d => d.DataId).Select(g => g.First())
                .Where(d => party.Contains(d.DataId) || ResourceManager.Instance.GetCardCount(d.DataId) > 0)
                .OrderBy(d => party.Contains(d.DataId) ? party.IndexOf(d.DataId) : 100).ThenBy(d => d.DisplayName).ToList();
            foreach (var data in all) CreateCrewCard(content, data, party.Contains(data.DataId), data.DataId == leader);
            if (all.Count == 0) EmptyState(root, "아직 승무원이 없습니다", "캐릭터를 영입하면 이곳에서 편성과 장비를 관리할 수 있습니다.");
        }

        private void CreateCrewCard(Transform parent, CharacterData data, bool inParty, bool leader)
        {
            var status = ResolveManagementStatus(data);
            var card = ManagementTheme.Panel("Card_" + data.DataId, parent, ManagementTheme.Surface, true);
            var button = card.gameObject.AddComponent<UnityEngine.UI.Button>(); ManagementTheme.StyleButton(button);
            button.onClick.AddListener(() => ShowCharacterDetailPopup(data));
            var tag = ManagementTheme.Label(card, inParty ? $"{RunManager.Instance.State.partyDataIDs.IndexOf(data.DataId) + 1:00}  /  {(leader ? "리더" : "편성 중")}" : "대기 승무원", 15, leader ? ManagementTheme.Gold : ManagementTheme.Cyan);
            ManagementTheme.Top(tag.rectTransform, 10, 24, 14, 14);
            var portrait = ManagementTheme.Panel("Portrait", card, ManagementTheme.Background);
            ManagementTheme.Place(portrait, new Vector2(0, 0), new Vector2(0.33f, 1), new Vector2(14, 60), new Vector2(-4, -44));
            var art = ManagementTheme.Rect("Art", portrait).gameObject.AddComponent<UnityEngine.UI.Image>();
            art.sprite = data.portraitSprite; art.preserveAspect = true; art.raycastTarget = false; art.enabled = art.sprite != null;
            var info = ManagementTheme.Rect("Info", card);
            ManagementTheme.Place(info, new Vector2(0.35f, 0), Vector2.one, new Vector2(4, 55), new Vector2(-14, -43));
            var name = ManagementTheme.Label(info, data.DisplayName, 23); ManagementTheme.Top(name.rectTransform, 0, 54);
            name.enableAutoSizing = true; name.fontSizeMin = 18; name.fontSizeMax = 23;
            var rank = ManagementTheme.Label(info, $"Lv.{status.charLevel}   {status.LevelTitle}", 16, ManagementTheme.Gold);
            ManagementTheme.Top(rank.rectTransform, 56, 22);
            var stats = ManagementTheme.Label(info, $"체력  {status.currentHp:0}/{status.FinalMaxHp:0}\n정신  {status.currentMental:0}/{status.FinalMaxMental:0}", 17, ManagementTheme.Muted);
            ManagementTheme.Top(stats.rectTransform, 83, 46);
            var actions = ManagementTheme.Rect("Actions", card);
            ManagementTheme.Place(actions, Vector2.zero, new Vector2(1, 0), new Vector2(14, 12), new Vector2(-14, 48));
            ManagementTheme.RowLayout(actions, 6);
            actions.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var detail = ManagementTheme.Button(actions, "Details", "상세 정보", () => ShowCharacterDetailPopup(data));
            detail.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            if (inParty)
            {
                int index = RunManager.Instance.State.partyDataIDs.IndexOf(data.DataId);
                var left = ManagementTheme.Button(actions, "MoveLeftButton", "<", () => { if (RunManager.Instance.MovePartyMember(data.DataId, -1)) RefreshTab(); });
                var right = ManagementTheme.Button(actions, "MoveRightButton", ">", () => { if (RunManager.Instance.MovePartyMember(data.DataId, 1)) RefreshTab(); });
                foreach (var move in new[] { left, right }) { var size = move.gameObject.AddComponent<LayoutElement>(); size.preferredWidth = 40; size.flexibleWidth = 0; }
                left.interactable = index > 0; right.interactable = index < RunManager.Instance.State.partyDataIDs.Count - 1;
            }
        }

        private void BuildInventoryGrid()
        {
            inventoryCards.Clear();
            var root = ManagementTheme.Rect("InventoryRoot", contentContainer);
            var resources = ResourceManager.Instance;
            var all = ManagementInventory.Build(resources.Equipments, resources.Relics, resources.Consumables);
            var entries = ManagementInventory.FilterAndSort(all, inventoryCategory, inventorySort);
            var title = ManagementTheme.Label(root, $"보급품 보관함  <size=17><color=#A1B1B5>{all.Count}종 · {all.Sum(e => e.Count)}개</color></size>", 25);
            ManagementTheme.Top(title.rectTransform, 0, 38, 0, 250);
            var sort = ManagementTheme.Button(root, "InventorySort", "정렬 · " + ManagementInventory.SortName(inventorySort), () => { inventorySort = (InventorySort)(((int)inventorySort + 1) % 4); RefreshTab(); });
            ManagementTheme.Place((RectTransform)sort.transform, new Vector2(1, 1), Vector2.one, new Vector2(-230, -40), Vector2.zero);
            var filters = ManagementTheme.Rect("InventoryFilters", root); ManagementTheme.Top(filters, 52, 40);
            ManagementTheme.RowLayout(filters);
            foreach (InventoryCategory category in System.Enum.GetValues(typeof(InventoryCategory)))
            {
                var chosen = category;
                int count = all.Where(e => chosen == InventoryCategory.All || e.Category == chosen).Sum(e => e.Count);
                ManagementTheme.Button(filters, "Filter_" + category, $"{ManagementInventory.CategoryName(category)}  {count}", () => { inventoryCategory = chosen; RefreshTab(); }, category == inventoryCategory);
            }
            var scroll = ManagementTheme.Scroll(root, "InventoryScroll", out var content);
            ManagementTheme.Place(scroll, Vector2.zero, Vector2.one, new Vector2(0, 196), new Vector2(0, -108));
            content.gameObject.AddComponent<GridLayoutGroup>();
            content.gameObject.AddComponent<ManagementGridLayout>().Configure(165, 200, 4);
            var detail = ManagementTheme.Panel("ItemDetails", root, ManagementTheme.Surface, true);
            ManagementTheme.Place(detail, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 180));
            inventoryTitle = ManagementTheme.Label(detail, "아이템을 선택하세요", 22, ManagementTheme.Gold);
            ManagementTheme.Top(inventoryTitle.rectTransform, 14, 32, 18, 18);
            var detailScroll = ManagementTheme.Scroll(detail, "ItemDescriptionScroll", out var detailContent);
            ManagementTheme.Place(detailScroll, Vector2.zero, Vector2.one, new Vector2(18, 16), new Vector2(-18, -55));
            detailContent.gameObject.AddComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
            inventoryDescription = ManagementTheme.Label(detailContent, "아이템을 누르면 능력치와 효과를 확인할 수 있습니다.", 18, ManagementTheme.Muted, TextAlignmentOptions.TopLeft);
            foreach (var entry in entries) CreateInventoryCard(content, entry);
            var selected = entries.FirstOrDefault(e => e.Key == selectedInventoryKey);
            if (selected != null) SelectInventoryItem(selected);
            if (entries.Count == 0)
            {
                string label = inventoryCategory == InventoryCategory.All ? "보관함이 비어 있습니다" : $"보유한 {ManagementInventory.CategoryName(inventoryCategory)}이 없습니다";
                EmptyState(scroll, label, "획득한 아이템은 이곳에 자동으로 보관됩니다.");
            }
        }

        private void CreateInventoryCard(Transform parent, InventoryEntry entry)
        {
            var card = ManagementTheme.Panel("InventoryItem_" + entry.Key, parent, ManagementTheme.Surface, true);
            inventoryCards[entry.Key] = card.GetComponent<UnityEngine.UI.Image>();
            var button = card.gameObject.AddComponent<UnityEngine.UI.Button>(); ManagementTheme.StyleButton(button);
            button.onClick.AddListener(() => SelectInventoryItem(entry));
            var category = ManagementTheme.Label(card, entry.CategoryLabel, 13, ManagementTheme.Muted);
            ManagementTheme.Top(category.rectTransform, 8, 20, 12, 55);
            var count = ManagementTheme.Label(card, "×" + entry.Count, 17, ManagementTheme.Gold, TextAlignmentOptions.MidlineRight);
            ManagementTheme.Top(count.rectTransform, 8, 20, 60, 12);
            var icon = ManagementTheme.Rect("Icon", card);
            ManagementTheme.Place(icon, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-35, -100), new Vector2(35, -30));
            if (entry.Icon != null) { var art = icon.gameObject.AddComponent<UnityEngine.UI.Image>(); art.sprite = entry.Icon; art.preserveAspect = true; art.raycastTarget = false; }
            else ManagementTheme.InventorySymbol(icon, entry.Category);
            var name = ManagementTheme.Label(card, entry.Name, 18, ManagementTheme.Text, TextAlignmentOptions.Center);
            ManagementTheme.Top(name.rectTransform, 104, 40, 8, 8);
            string availability = entry.Equipment != null ? $"\n사용 가능 {ResourceManager.Instance.EquipmentLoadouts.AvailableCount(ResourceManager.Instance.Equipments, entry.Equipment)} / 보유 {entry.Count}" : "";
            var subtitle = ManagementTheme.Label(card, entry.Subtitle + availability, 14, entry.Rank >= 3 ? ManagementTheme.Gold : ManagementTheme.Muted, TextAlignmentOptions.Center);
            ManagementTheme.Top(subtitle.rectTransform, 147, 45, 8, 8);
        }

        private void SelectInventoryItem(InventoryEntry entry)
        {
            selectedInventoryKey = entry.Key;
            foreach (var item in inventoryCards) if (item.Value != null) item.Value.color = item.Key == entry.Key ? ManagementTheme.Raised : ManagementTheme.Surface;
            inventoryTitle.text = entry.Name + $"  <size=16>{entry.Subtitle}  ·  {entry.Count}개</size>";
            string owners = entry.Equipment != null ? ResourceManager.Instance.EquipmentLoadouts.Owners(entry.Equipment) + "\n" : "";
            inventoryDescription.text = owners + entry.Description + (entry.Category == InventoryCategory.Equipment ? "\n<color=#92C7CD>승무원 상세 정보에서 장착할 수 있습니다.</color>" : "");
        }

        private static void EmptyState(Transform parent, string title, string description)
        {
            var text = ManagementTheme.Label(parent, $"<size=24>{title}</size>\n\n{description}", 17, ManagementTheme.Muted, TextAlignmentOptions.Center);
            ManagementTheme.Place(text.rectTransform, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.8f));
        }

        private void BuildDetailShell()
        {
            var root = ManagementTheme.Panel("CharDetailPopup", popupPanel.transform, ManagementTheme.Background, true);
            ManagementTheme.Place(root, Vector2.zero, Vector2.one, new Vector2(20, 32), new Vector2(-20, -158));
            charDetailPopupPanel = root.gameObject;
            var close = ManagementTheme.Button(root, "CloseDetail", "<  명부", () => charDetailPopupPanel.SetActive(false));
            ManagementTheme.Place((RectTransform)close.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -38), new Vector2(130, 0));
            var portrait = ManagementTheme.Panel("IllustrationFrame", root, ManagementTheme.Surface, true);
            ManagementTheme.Place(portrait, new Vector2(0, 0.51f), new Vector2(0.35f, 1), new Vector2(0, 12), new Vector2(0, -55));
            detailStandingImage = ManagementTheme.Rect("Illustration", portrait).gameObject.AddComponent<UnityEngine.UI.Image>();
            detailStandingImage.preserveAspect = true; detailStandingImage.raycastTarget = false;
            detailTitleText = ManagementTheme.Label(root, "", 28, ManagementTheme.Gold);
            ManagementTheme.Place(detailTitleText.rectTransform, new Vector2(0.39f, 0.83f), Vector2.one, new Vector2(0, 0), new Vector2(0, -48));
            detailStatsText = ManagementTheme.Label(root, "", 21, ManagementTheme.Text, TextAlignmentOptions.TopLeft);
            detailStatsText.lineSpacing = 12;
            ManagementTheme.Place(detailStatsText.rectTransform, new Vector2(0.39f, 0.51f), new Vector2(1, 0.81f));
            var skillsHeader = ManagementTheme.Label(root, "전투 스킬", 21, ManagementTheme.Cyan);
            ManagementTheme.Place(skillsHeader.rectTransform, new Vector2(0, 0.46f), new Vector2(1, 0.51f));
            var skills = ManagementTheme.Rect("SkillsArea", root);
            ManagementTheme.Place(skills, new Vector2(0, 0.29f), new Vector2(1, 0.45f));
            ManagementTheme.RowLayout(skills, 10); detailSkillsArea = skills;
            var equipmentHeader = ManagementTheme.Label(root, "장착 장비", 21, ManagementTheme.Cyan);
            ManagementTheme.Place(equipmentHeader.rectTransform, new Vector2(0, 0.23f), new Vector2(1, 0.28f));
            var equipment = ManagementTheme.Rect("EquipArea", root);
            ManagementTheme.Place(equipment, new Vector2(0, 0.10f), new Vector2(1, 0.22f));
            ManagementTheme.RowLayout(equipment, 10); detailEquipmentsArea = equipment;
            detailDeckButton = ManagementTheme.Button(root, "DeckButton", "", null);
            ManagementTheme.Place((RectTransform)detailDeckButton.transform, Vector2.zero, new Vector2(1, 0.075f));
            detailDeckButtonText = detailDeckButton.GetComponentInChildren<TextMeshProUGUI>();
            root.gameObject.SetActive(false);
        }
    }
}
