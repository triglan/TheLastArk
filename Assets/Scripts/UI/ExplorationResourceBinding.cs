using UnityEngine;
using TMPro;
using TheLastArk.Managers;

namespace TheLastArk.UI
{
    public partial class ExplorationResourceUI
    {
        // Both scenes use the MapScene-authored prefab, including its sprite references.
        private bool TryBindMapHud(Transform parent)
        {
            if (parent == null) return false;
            Transform hud = parent.Find("ExplorationHUD");
            if (hud == null)
            {
                var prefab = Resources.Load<GameObject>("UI/ExplorationHUD");
                if (prefab == null) return false;
                hud = Instantiate(prefab, parent, false).transform;
                hud.name = "ExplorationHUD";
            }
            trainHpText = hud.Find("ResourcePanel/TrainHpText").GetComponent<TextMeshProUGUI>();
            goldText = hud.Find("GoldText").GetComponent<TextMeshProUGUI>();
            relicsContainer = hud.Find("RelicsContainer");
            consumableIcons.Clear();
            consumableTexts.Clear();
            for (int i = 0; i < 3; i++)
            {
                var slot = hud.Find("Consumables/Slot_" + i);
                consumableIcons.Add(slot.Find("Icon").GetComponent<UnityEngine.UI.Image>());
                consumableTexts.Add(slot.Find("Text").GetComponent<TextMeshProUGUI>());
                int index = i;
                BindButton(slot, () => { var battle = FindObjectOfType<BattleManager>(); if (battle != null) battle.SelectConsumable(index); });
            }
            var buttons = hud.Find("ManagementIcons");
            BindButton(buttons.Find("TopBarIcon_기차"), () => ManagementUIManager.Instance.Show(ManagementUIManager.TabType.Train));
            BindButton(buttons.Find("TopBarIcon_캐릭터"), () => ManagementUIManager.Instance.Show(ManagementUIManager.TabType.Character));
            BindButton(buttons.Find("TopBarIcon_가방"), () => ManagementUIManager.Instance.Show(ManagementUIManager.TabType.Inventory));
            BindButton(buttons.Find("TopBarIcon_설정"), () => ManagementUIManager.Instance.ShowSettingsPopup());
            var mapButton = parent.Find("BattleMapButton");
            if (mapButton != null) BindButton(mapButton, BattleMapPopupUI.Show);
            var synergy = GetComponent<SynergyUIBar>();
            if (synergy == null) synergy = gameObject.AddComponent<SynergyUIBar>();
            synergy.Initialize(hud);
            if (!Application.isPlaying) return true;
            // Rebinding a pre-authored HUD must not duplicate event subscriptions.
            OnDestroy();
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnGoldChanged += UpdateGold;
                ResourceManager.Instance.OnConsumablesChanged += UpdateConsumables;
                ResourceManager.Instance.OnRelicsChanged += UpdateRelics;
                UpdateGold(); UpdateConsumables(); UpdateRelics();
            }
            if (TrainManager.Instance != null)
            {
                TrainManager.Instance.OnDurabilityChanged += UpdateTrainHp;
                UpdateTrainHp();
            }
            return true;
        }

        private static void BindButton(Transform target, UnityEngine.Events.UnityAction action)
        {
            if (target == null) return;
            var button = target.GetComponent<UnityEngine.UI.Button>();
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
    }
}
