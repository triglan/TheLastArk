using System.Linq;
using TheLastArk.Data;
using TheLastArk.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    public partial class TrainManagementUI
    {
        private ResourceManager observedResources;
        private TrainManager observedTrain;
        private int carColumns = 2;
        private float carMinimumWidth = 300;

        public void BuildEmbedded(Transform parent, bool maintenance = false)
        {
            carColumns = maintenance ? 4 : 2;
            carMinimumWidth = maintenance ? 350 : 300;
            mainFont = TMPFontManager.MainKoreanFont;
            var title = ManagementTheme.Label(parent, maintenance ? "기차 정비 현황" : "기차 운용 현황", 25);
            ManagementTheme.Top(title.rectTransform, 0, 38);
            statusSummaryText = ManagementTheme.Label(parent, "", 18, ManagementTheme.Gold);
            ManagementTheme.Top(statusSummaryText.rectTransform, 43, 30);
            var hp = ManagementTheme.Panel("Durability", parent, ManagementTheme.Surface, true);
            ManagementTheme.Top(hp, 82, 28);
            var fill = ManagementTheme.Panel("Fill", hp, new Color32(58, 90, 95, 255));
            var hpText = ManagementTheme.Label(hp, "", 16, ManagementTheme.Text, TextAlignmentOptions.Center);
            hp.gameObject.AddComponent<TrainHpUpdater>().Init(hp, fill, hpText);
            var scroll = ManagementTheme.Scroll(parent, "CarScroll", out var content);
            ManagementTheme.Place(scroll, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -128));
            content.gameObject.AddComponent<GridLayoutGroup>();
            content.gameObject.AddComponent<ManagementGridLayout>().Configure(carMinimumWidth, 376, carColumns);
            carsContainer = content;
            CreatePartModal(parent); CreateBuildModal(parent); CreateSynergyModal(parent); CreateModuleModal(parent);
            observedResources = ResourceManager.Instance;
            observedTrain = TrainManager.Instance;
            observedResources.OnGoldChanged += UpdateUI;
            observedTrain.OnTrainCarsChanged += UpdateUI;
            UpdateUI();
        }

        private void OnDestroy()
        {
            if (observedResources != null) observedResources.OnGoldChanged -= UpdateUI;
            if (observedTrain != null) observedTrain.OnTrainCarsChanged -= UpdateUI;
        }

        private GameObject BuildModal(Transform parent, string name, out Transform content, out TextMeshProUGUI title, bool grid = false)
        {
            var root = ManagementTheme.Panel(name, parent, ManagementTheme.Background, true);
            title = ManagementTheme.Label(root, "", 24, ManagementTheme.Gold);
            ManagementTheme.Top(title.rectTransform, 14, 54, 18, 100);
            var close = ManagementTheme.Button(root, "Close", "닫기", () => root.gameObject.SetActive(false));
            ManagementTheme.Place((RectTransform)close.transform, new Vector2(1, 1), Vector2.one, new Vector2(-88, -62), new Vector2(-14, -18));
            var scroll = ManagementTheme.Scroll(root, "Options", out var list);
            ManagementTheme.Place(scroll, Vector2.zero, Vector2.one, new Vector2(16, 16), new Vector2(-16, -84));
            if (grid)
            {
                list.gameObject.AddComponent<GridLayoutGroup>();
                list.gameObject.AddComponent<ManagementGridLayout>().Configure(180, 64, 3);
            }
            else
            {
                var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 12; layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
            }
            content = list;
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        private void BuildCarCard(TrainCar car, int slotIndex)
        {
            if (car == null) return;
            bool empty = car.carType == TrainCarType.Optional;
            var card = ManagementTheme.Panel("Car_Slot_" + slotIndex, carsContainer, ManagementTheme.Surface, true);
            var number = ManagementTheme.Label(card, slotIndex.ToString("00") + (slotIndex <= 2 ? "  /  고정 차량" : "  /  선택 차량"), 14, ManagementTheme.Muted);
            ManagementTheme.Top(number.rectTransform, 12, 22, 16, 16);
            var title = ManagementTheme.Label(card, empty ? "빈 차량" : car.carName, 25, ManagementTheme.Gold);
            ManagementTheme.Top(title.rectTransform, 38, 36, 16, 110);
            var level = ManagementTheme.Label(card, empty ? "미건설" : $"Lv.{car.level} / {car.MaxLevel}", 17, ManagementTheme.Muted, TextAlignmentOptions.MidlineRight);
            ManagementTheme.Place(level.rectTransform, new Vector2(1, 1), Vector2.one, new Vector2(-115, -74), new Vector2(-16, -38));
            var effect = ManagementTheme.Label(card, CarDescription(car), 17, ManagementTheme.Text, TextAlignmentOptions.TopLeft);
            ManagementTheme.Top(effect.rectTransform, 82, 56, 16, 16);
            effect.gameObject.AddComponent<BattleHoverTooltip>().SetContent(CarDescription(car));

            if (empty)
            {
                int cost = TrainManager.Instance.GetDiscountedCost(TrainCar.OptionalCarBuildCost);
                var build = ManagementTheme.Button(card, "BuildButton", $"차량 건설  ·  {cost} G", () => OpenBuildOptionalCarModal(slotIndex - 2), true);
                ManagementTheme.Top((RectTransform)build.transform, 148, 42, 16, 16);
                // Open the list even when gold is short, so the player can inspect each facility.
                var hint = ManagementTheme.Label(card, "의무실 · 전투 강화소\n기도실 · 특성 훈련소\n\n건설할 시설을 선택하세요.", 18, ManagementTheme.Muted, TextAlignmentOptions.Center);
                ManagementTheme.Top(hint.rectTransform, 218, 126, 16, 16);
                return;
            }

            int upgradeCost = TrainManager.Instance.GetDiscountedCost(car.UpgradeCost);
            var upgrade = ManagementTheme.Button(card, "UpgradeButton", car.CanUpgrade ? $"강화  ·  {upgradeCost} G" : "최대 강화 완료", () =>
            {
                if (TrainManager.Instance.TryUpgradeCar(car)) UpdateUI();
            }, true);
            bool nexusLocked = car.carType == TrainCarType.Nexus && TrainManager.IsBattleInProgress;
            upgrade.interactable = car.CanUpgrade && ResourceManager.Instance.Gold >= upgradeCost && !nexusLocked;
            ManagementTheme.Top((RectTransform)upgrade.transform, 148, 42, 16, 16);
            var actions = ManagementTheme.Rect("CarActions", card);
            ManagementTheme.Top(actions, 198, 36, 16, 16); ManagementTheme.RowLayout(actions);
            if (car.carType == TrainCarType.Nexus)
                ManagementTheme.Button(actions, "ChangeModuleButton", "넥서스 모듈 교체", () => OpenModuleSelectModal(car)).interactable = !nexusLocked;
            if (car.carType == TrainCarType.Nexus && car.installedModuleId == NexusModuleDatabase.OriginId)
                ManagementTheme.Button(actions, "OriginPartsButton", "오리진 파츠", () => OpenPartModal(car));
            if (car.IsBuiltOptionalCar)
            {
                if (car.carType == TrainCarType.TraitTrainingCamp)
                    ManagementTheme.Button(actions, "SynergySelectButton", "시너지 선택", () => OpenSynergySelectModal(car));
                int cost = TrainManager.Instance.GetDiscountedCost(TrainCar.OptionalCarDismantleCost);
                var remove = ManagementTheme.Button(actions, "DismantleButton", $"철거 · {cost} G", () =>
                {
                    if (TrainManager.Instance.TryDismantleOptionalCar(slotIndex - 2)) UpdateUI();
                });
                remove.interactable = ResourceManager.Instance.Gold >= cost;
            }
            var partsLabel = ManagementTheme.Label(card, $"장착 파츠  {car.installedParts.Count} / {car.MaxPartSlots}", 16, ManagementTheme.Cyan);
            ManagementTheme.Top(partsLabel.rectTransform, 244, 24, 16, 16);
            for (int i = 0; i < car.MaxPartSlots; i++)
            {
                string id = i < car.installedParts.Count ? car.installedParts[i] : null;
                var part = id != null ? TrainPartDatabase.GetPart(id) : null;
                string partLabel = part == null ? "+ 파츠 장착"
                    : TrainPartDatabase.IsOriginPart(id) ? $"{part.partName} {PartLevelName(car.GetPartLevel(id))}  ·  해제" : part.partName + "  ·  해제";
                var button = ManagementTheme.Button(card, "PartSlot_" + i, partLabel, () =>
                {
                    if (part == null) OpenPartModal(car);
                    else { TrainManager.Instance.TryUninstallPart(car, id); UpdateUI(); }
                });
                button.GetComponentInChildren<TextMeshProUGUI>().fontSize = 17;
                if (nexusLocked) button.interactable = false;
                ManagementTheme.Top((RectTransform)button.transform, 276 + i * 42, 34, 16, 16);
                if (part != null) button.gameObject.AddComponent<BattleHoverTooltip>().SetContent(
                    TrainPartDatabase.IsOriginPart(id) && TrainManager.Instance.OriginPartBalance != null
                        ? TrainManager.Instance.OriginPartBalance.Describe(id, car.GetPartLevel(id)) : part.description);
            }
        }

        private string CarDescription(TrainCar car)
        {
            switch (car.carType)
            {
                case TrainCarType.Nexus:
                    var module = NexusModuleDatabase.GetModule(car.installedModuleId);
                    return (module != null ? module.moduleName : "오리진") + " 모듈\n" +
                        (string.IsNullOrEmpty(car.installedModuleId) || car.installedModuleId == NexusModuleDatabase.OriginId
                            ? $"턴마다 행동 주사위 {car.level + 2}개 제공" : "모듈 교체에서 운용 효과를 확인하세요.");
                case TrainCarType.CrewQuarters: return $"승무원 수용량  {TrainManager.Instance.GetCurrentCrewCount()} / {TrainManager.Instance.GetMaxCrewCapacity()}명";
                case TrainCarType.Infirmary: return $"전투 종료 후 아군 체력 {2 + car.level} 회복";
                case TrainCarType.CombatEnhancement: return $"전투 중 공격력 · 주문력 +{car.level * 5}%";
                case TrainCarType.PrayerRoom: return $"전투 중 치유량 +{TrainManager.Instance.GetPrayerRoomHealMultiplier() * 100:0}%";
                case TrainCarType.TraitTrainingCamp: return $"선택한 시너지 인원 +1\n선택 {car.selectedSynergies.Count} / {car.MaxSelectableSynergies}";
                default: return "시설을 건설해 탐험을 지원하세요.";
            }
        }

        private void PolishModal(GameObject panel)
        {
            foreach (var text in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.color = ManagementTheme.Text; text.font = mainFont;
                text.fontSize = Mathf.Max(text.fontSize, 18);
                text.text = text.text.Replace("<color=yellow>", "<color=#C8AE77>").Replace("<color=cyan>", "<color=#92C7CD>")
                    .Replace("#88CCFF", "#A1B1B5").Replace("<size=14>", "<size=17>").Replace("<size=13>", "<size=16>");
                text.raycastTarget = false;
                text.enableWordWrapping = true;
                text.overflowMode = TextOverflowModes.Ellipsis;
                if (text.transform.parent.name == "Info")
                {
                    text.alignment = TextAlignmentOptions.MidlineLeft;
                    text.raycastTarget = true;
                    var tooltip = text.GetComponent<BattleHoverTooltip>() ?? text.gameObject.AddComponent<BattleHoverTooltip>();
                    tooltip.SetContent(text.text);
                }
            }
            foreach (var layout in panel.GetComponentsInChildren<HorizontalLayoutGroup>(true))
            {
                layout.childControlWidth = true; layout.childForceExpandWidth = false;
                foreach (Transform child in layout.transform)
                {
                    var size = child.GetComponent<LayoutElement>();
                    if (size == null) continue;
                    size.minWidth = child.name == "Info" ? 100 : 115;
                    size.preferredWidth = child.name == "Info" ? 300 : 115;
                    size.flexibleWidth = child.name == "Info" ? 1 : 0;
                }
                ManagementTheme.Height(layout.transform, panel == moduleModalPanel ? 186 : 132);
                var background = layout.GetComponent<Image>(); if (background != null) background.color = ManagementTheme.Surface;
            }
            foreach (var button in panel.GetComponentsInChildren<Button>(true))
            {
                button.GetComponent<Image>().color = ManagementTheme.Raised;
                ManagementTheme.StyleButton(button);
            }
            panel.transform.SetAsLastSibling();
        }
    }
}
