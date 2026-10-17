using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using TheLastArk.Data;
using TheLastArk.Managers;
using TheLastArk.Character;

namespace TheLastArk.UI
{
    public class TavernUI : MonoBehaviour
    {
        private GameObject popupPanel;
        private enum TabType { HireMercenary, SkillChange }
        private TabType currentTab = TabType.HireMercenary;
        private Transform contentArea;
        private TextMeshProUGUI balanceText, crewText;
        private Button hireTab, skillTab;
        private ResourceManager observedResources;
        private TrainManager observedTrain;
        private readonly List<CharacterData> availableMercenaries = new List<CharacterData>();
        private readonly List<int> mercenaryPrices = new List<int>();
        private readonly List<bool> mercenaryHired = new List<bool>();
        private CharacterData selectedCharacter;

        public void Show()
        {
            if (popupPanel == null) CreateUI();
            if (observedResources == null)
            {
                observedResources = ResourceManager.Instance;
                observedResources.OnGoldChanged += RefreshVisible;
                observedTrain = TrainManager.Instance;
                observedTrain.OnTrainCarsChanged += RefreshVisible;
            }
            GenerateMercenaries();
            SwitchTab(TabType.HireMercenary);
            popupPanel.SetActive(true); popupPanel.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            FindObjectOfType<SkillTooltipUI>()?.HideTooltip();
            if (popupPanel != null) popupPanel.SetActive(false);
        }
        private void RefreshVisible() { if (popupPanel != null && popupPanel.activeInHierarchy) RefreshUI(); }
        private void OnDestroy()
        {
            if (observedResources != null) observedResources.OnGoldChanged -= RefreshVisible;
            if (observedTrain != null) observedTrain.OnTrainCarsChanged -= RefreshVisible;
        }

        private void CreateUI()
        {
            popupPanel = ManagementDialog.Create(HudCanvasUtility.FindOverlayCanvas(), "TavernPopup", "마을 주점", "TAVERN  /  용병 영입 · 출전 스킬", Hide, 0.08f, out var window);
            var toolbar = ManagementDialog.Toolbar(window);
            balanceText = ManagementTheme.Label(toolbar, "", 25, ManagementTheme.Gold);
            ManagementTheme.Place(balanceText.rectTransform, Vector2.zero, new Vector2(0.28f, 1), new Vector2(18, 0), Vector2.zero);
            crewText = ManagementTheme.Label(toolbar, "", 19, ManagementTheme.Muted);
            ManagementTheme.Place(crewText.rectTransform, new Vector2(0.28f, 0), new Vector2(0.56f, 1));
            var tabs = ManagementTheme.Rect("TavernTabsArea", toolbar);
            ManagementTheme.Place(tabs, new Vector2(0.58f, 0), Vector2.one, new Vector2(0, 7), new Vector2(-8, -7));
            ManagementTheme.RowLayout(tabs, 8);
            hireTab = ManagementTheme.Button(tabs, "Tab_용병 고용", "01   용병 영입", () => SwitchTab(TabType.HireMercenary));
            skillTab = ManagementTheme.Button(tabs, "Tab_스킬 변경", "02   스킬 변경", () => SwitchTab(TabType.SkillChange));
            contentArea = ManagementDialog.Body(window);
        }

        private void GenerateMercenaries()
        {
            availableMercenaries.Clear();
            mercenaryPrices.Clear();
            mercenaryHired.Clear();

            var allChars = Resources.LoadAll<CharacterData>("Characters").ToList();
            if (allChars.Count == 0) allChars = Resources.LoadAll<CharacterData>("").ToList();

            List<CharacterData> candidates = new List<CharacterData>();
            foreach (var c in allChars)
            {
                if (c != null && !c.isEnemy) candidates.Add(c);
            }

            int extraMerc = 0;
            if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.TavernExtraMerc))
            {
                extraMerc = Mathf.RoundToInt(ResourceManager.Instance.GetRelicBonus(RelicEffectType.TavernExtraMerc));
            }
            int count = Mathf.Min(3 + extraMerc, candidates.Count);

            float discount = 0f;
            if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.TavernDiscount))
            {
                discount = ResourceManager.Instance.GetRelicBonus(RelicEffectType.TavernDiscount);
            }

            for (int i = 0; i < count; i++)
            {
                availableMercenaries.Add(candidates[i]);
                int basePrice = 150 + (i * 50);
                int finalPrice = Mathf.RoundToInt(basePrice * (1f - discount));
                mercenaryPrices.Add(finalPrice);
                mercenaryHired.Add(false);
            }
        }

        private void SwitchTab(TabType tab) { currentTab = tab; RefreshUI(); }

        private void RefreshUI()
        {
            if (contentArea == null) return;
            FindObjectOfType<SkillTooltipUI>()?.HideTooltip();
            foreach (Transform child in contentArea) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            balanceText.text = $"보유 골드   {ResourceManager.Instance.Gold:N0} G";
            crewText.text = $"편성 {RunManager.Instance.State.partyDataIDs.Count}/{RunManager.MaxPartySize}  ·  수용 {TrainManager.Instance.GetCurrentCrewCount()}/{TrainManager.Instance.GetMaxCrewCapacity()}";
            StyleTab(hireTab, currentTab == TabType.HireMercenary);
            StyleTab(skillTab, currentTab == TabType.SkillChange);
            if (currentTab == TabType.HireMercenary) BuildHireMercenaryView();
            else BuildSkillChangeView();
        }

        private static void StyleTab(Button button, bool selected)
        {
            button.GetComponent<Image>().color = selected ? ManagementTheme.Raised : ManagementTheme.Surface;
            button.GetComponentInChildren<TextMeshProUGUI>().color = selected ? ManagementTheme.Gold : ManagementTheme.Muted;
        }

        private string HireUnavailable(CharacterData data, bool hired, int price)
        {
            if (hired) return "영입 완료";
            if (RunManager.Instance.State.partyDataIDs.Contains(data.DataId)) return "이미 편성 중인 승무원입니다.";
            if (TrainManager.Instance.IsCrewAtMaxCapacity()) return "승무원실 수용 인원이 가득 찼습니다.";
            if (!RunManager.Instance.CanAddPartyMember(data)) return "파티 편성 인원이 가득 찼습니다.";
            return ResourceManager.Instance.CanAfford(price) ? "" : "골드가 부족합니다.";
        }

        private void Hire(int index)
        {
            var data = availableMercenaries[index];
            int price = mercenaryPrices[index];
            string reason = HireUnavailable(data, mercenaryHired[index], price);
            if (!string.IsNullOrEmpty(reason)) { NotificationManager.Instance?.ShowMessage(reason, ManagementTheme.Gold); return; }
            mercenaryHired[index] = true;
            if (!ResourceManager.Instance.TrySpendGold(price)) { mercenaryHired[index] = false; RefreshUI(); return; }
            RunManager.Instance.AddPartyMember(data);
            ResourceManager.Instance.AddCharacterCard(data.DataId, 1);
            NotificationManager.Instance?.ShowMessage($"[{data.DisplayName}] 영입 완료!", ManagementTheme.Cyan);
            RefreshUI();
        }

        private void BuildHireMercenaryView()
        {
            var tip = ManagementTheme.Label(contentArea, "새로운 동료를 맞이하세요. 영입한 용병은 파티에 바로 합류합니다.", 20, ManagementTheme.Muted);
            ManagementTheme.Top(tip.rectTransform, 0, 36);
            var scroll = ManagementTheme.Scroll(contentArea, "MercenaryScroll", out var list);
            ManagementTheme.Place(scroll, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -48));
            list.gameObject.AddComponent<GridLayoutGroup>();
            list.gameObject.AddComponent<ManagementGridLayout>().Configure(380, 550, 3);
            for (int i = 0; i < availableMercenaries.Count; i++)
            {
                int index = i; var data = availableMercenaries[i]; int price = mercenaryPrices[i];
                bool hired = mercenaryHired[i];
                var card = ManagementTheme.Panel($"MercCard_{i}", list, ManagementTheme.Surface, true);
                var art = ManagementTheme.Panel("Portrait", card, ManagementTheme.Background);
                ManagementTheme.Top(art, 16, 220, 16, 16);
                var picture = ManagementTheme.Rect("Art", art).gameObject.AddComponent<Image>();
                picture.sprite = data.portraitSprite != null ? data.portraitSprite : data.standingSprite;
                picture.preserveAspect = true; picture.raycastTarget = false; picture.enabled = picture.sprite != null;
                var name = ManagementTheme.Label(card, data.DisplayName, 27);
                ManagementTheme.Top(name.rectTransform, 250, 42, 20, 20);
                string traits = string.Join(" · ", data.synergies.Select(t => SynergyDatabase.GetInfo(t)?.displayName ?? t.ToString()));
                var synergies = ManagementTheme.Label(card, traits, 18, ManagementTheme.Gold);
                ManagementTheme.Top(synergies.rectTransform, 298, 48, 20, 20);
                synergies.raycastTarget = true; synergies.gameObject.AddComponent<BattleHoverTooltip>().SetContent(traits);
                var stats = ManagementTheme.Label(card, $"체력  {data.maxHp}    정신  {data.maxMental}\n공격  {data.baseAttack}    주문  {data.spellPower}\n방어  {data.armor}    마법 저항  {data.magicResist}", 20, ManagementTheme.Muted);
                ManagementTheme.Top(stats.rectTransform, 350, 84, 20, 20);
                string unavailable = HireUnavailable(data, hired, price);
                var state = ManagementTheme.Label(card, unavailable, 17, ManagementTheme.Gold);
                ManagementTheme.Top(state.rectTransform, 438, 40, 20, 20);
                var cost = ManagementTheme.Label(card, $"{price} G", 27, ManagementTheme.Gold);
                ManagementTheme.Place(cost.rectTransform, Vector2.zero, new Vector2(0.5f, 0), new Vector2(20, 18), new Vector2(0, 64));
                var button = ManagementTheme.Button(card, "HireButton", hired ? "영입 완료" : "영입", () => Hire(index), true);
                ManagementTheme.Place((RectTransform)button.transform, new Vector2(0.55f, 0), new Vector2(1, 0), new Vector2(0, 18), new Vector2(-20, 64));
                button.interactable = string.IsNullOrEmpty(unavailable);
            }
            if (availableMercenaries.Count == 0) ManagementTheme.Label(scroll, "지금 영입할 수 있는 용병이 없습니다.", 24, ManagementTheme.Muted, TextAlignmentOptions.Center);
        }

        private void BuildSkillChangeView()
        {
            var tip = ManagementTheme.Label(contentArea, "승무원을 선택해 출전 스킬 2종을 무작위로 변경합니다.", 20, ManagementTheme.Muted);
            ManagementTheme.Top(tip.rectTransform, 0, 36);
            var all = Resources.LoadAll<CharacterData>("Characters");
            var party = RunManager.Instance.State.partyDataIDs.Select(id => all.FirstOrDefault(c => c.DataId == id)).Where(c => c != null).ToList();
            if (party.Count == 0)
            {
                var empty = ManagementTheme.Label(contentArea, "편성된 승무원이 없습니다.\n<size=19>용병을 영입하거나 승무원 창에서 파티를 편성하세요.</size>", 25, ManagementTheme.Muted, TextAlignmentOptions.Center);
                return;
            }
            if (selectedCharacter == null || !party.Contains(selectedCharacter)) selectedCharacter = party[0];
            var tabs = ManagementTheme.Rect("PartyTabs", contentArea);
            ManagementTheme.Top(tabs, 48, 62); ManagementTheme.RowLayout(tabs);
            foreach (var data in party)
            {
                var choice = data;
                var button = ManagementTheme.Button(tabs, "CharTab_" + data.DisplayName, data.DisplayName, () => { selectedCharacter = choice; RefreshUI(); }, selectedCharacter == data);
                button.GetComponentInChildren<TextMeshProUGUI>().fontSize = 21;
            }
            var status = RunManager.Instance.State.partyStatuses.FirstOrDefault(s => s.origin != null && s.origin.DataId == selectedCharacter.DataId);
            if (status == null)
            {
                status = new CharacterStatus(selectedCharacter);
                RunManager.Instance.State.equipmentLoadouts.ApplyTo(status);
                RunManager.Instance.State.partyStatuses.Add(status);
            }
            var validSkills = Enumerable.Range(0, selectedCharacter.activeSkills.Length).Where(i => selectedCharacter.activeSkills[i] != null).ToList();
            var scroll = ManagementTheme.Scroll(contentArea, "SkillScroll", out var skills);
            ManagementTheme.Place(scroll, Vector2.zero, Vector2.one, new Vector2(0, 100), new Vector2(0, -130));
            skills.gameObject.AddComponent<GridLayoutGroup>();
            skills.gameObject.AddComponent<ManagementGridLayout>().Configure(480, 172, 2);
            foreach (int index in validSkills)
            {
                var skill = selectedCharacter.activeSkills[index];
                bool selected = status.selectedActiveSkillIndices.Contains(index);
                var card = ManagementTheme.Panel("SkillCard_" + index, skills, selected ? ManagementTheme.Raised : ManagementTheme.Surface, true);
                var art = ManagementTheme.Rect("SkillIcon", card).gameObject.AddComponent<Image>();
                ManagementTheme.Place(art.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -94), new Vector2(94, -20));
                art.sprite = skill.skillIcon; art.preserveAspect = true; art.raycastTarget = false; art.enabled = skill.skillIcon != null;
                if (skill.skillIcon == null)
                {
                    var fallback = ManagementTheme.Panel("SkillNumber", art.transform, ManagementTheme.Background, true);
                    fallback.GetComponent<Image>().raycastTarget = false;
                    ManagementTheme.Label(fallback, (index + 1).ToString("00"), 28, ManagementTheme.Gold, TextAlignmentOptions.Center);
                }
                var title = ManagementTheme.Label(card, string.IsNullOrWhiteSpace(skill.skillName) ? $"스킬 {index + 1}" : skill.skillName, 25);
                ManagementTheme.Top(title.rectTransform, 20, 38, 112, 20);
                var level = skill.levels != null && skill.levels.Length > 0 ? skill.levels[Mathf.Clamp(status.SkillLevelIndex, 0, skill.levels.Length - 1)] : null;
                var rule = ManagementTheme.Label(card, level?.diceInput?.Describe() ?? "주사위 제한 없음", 19, ManagementTheme.Muted);
                ManagementTheme.Top(rule.rectTransform, 65, 38, 112, 20);
                var state = ManagementTheme.Label(card, selected ? "출전 스킬" : "대기 스킬", 18, selected ? ManagementTheme.Gold : ManagementTheme.Muted);
                ManagementTheme.Top(state.rectTransform, 128, 28, 20, 20);
                var trigger = card.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => SkillTooltipUI.Instance.ShowTooltip(skill, status, selectedCharacter.DisplayName));
                trigger.triggers.Add(enter);
                var leave = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                leave.callback.AddListener(_ => SkillTooltipUI.Instance.HideTooltip()); trigger.triggers.Add(leave);
            }
            var footer = ManagementTheme.Panel("SkillChangeSummary", contentArea, ManagementTheme.Surface, true);
            ManagementTheme.Place(footer, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 82));
            string message = validSkills.Count < 2 ? "변경할 수 있는 스킬이 부족합니다." : !ResourceManager.Instance.CanAfford(50) ? "변경 비용 50 G가 부족합니다." : "스킬에 마우스를 올리면 상세 효과를 볼 수 있습니다.";
            var note = ManagementTheme.Label(footer, message, 19, ManagementTheme.Muted);
            ManagementTheme.Place(note.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 0), new Vector2(-360, 0));
            var reroll = ManagementTheme.Button(footer, "RerollSkillsButton", "2종 무작위 변경 · 50 G", () =>
            {
                if (validSkills.Count < 2 || !ResourceManager.Instance.TrySpendGold(50)) return;
                var pool = new List<int>(validSkills);
                int first = pool[Random.Range(0, pool.Count)]; pool.Remove(first);
                int second = pool[Random.Range(0, pool.Count)];
                status.selectedActiveSkillIndices = new List<int> { first, second };
                NotificationManager.Instance?.ShowMessage($"[{selectedCharacter.DisplayName}] 출전 스킬 2종이 변경되었습니다.", ManagementTheme.Cyan);
                RefreshUI();
            }, true);
            ManagementTheme.Place((RectTransform)reroll.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-330, -25), new Vector2(-18, 25));
            reroll.interactable = validSkills.Count >= 2 && ResourceManager.Instance.CanAfford(50);
        }
    }
}
