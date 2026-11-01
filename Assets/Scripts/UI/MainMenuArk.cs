using TheLastArk.Data;
using TheLastArk.Run;
using UnityEngine;
using TMPro;

namespace TheLastArk.UI
{
    public partial class MainMenuUI
    {
        private ArkScreen arkScreen;
        private void OnEnable() { if(Application.isPlaying) ArkProgression.Changed += RefreshArkFeatureVisibility; }
        private void OnDisable() { ArkProgression.Changed -= RefreshArkFeatureVisibility; }

        private void OpenArk()
        {
            HideHoverTooltip();
            if(titlePanel!=null)titlePanel.SetActive(false);
            if(characterSelectionPanel!=null)characterSelectionPanel.SetActive(false);
            if(arkScreen==null)
            {
                var parent=titlePanel!=null?titlePanel.transform.parent:FindObjectOfType<Canvas>().transform;
                arkScreen=ArkScreen.Create(parent);
            }
            arkScreen.Open(ShowTitlePanel);
        }

        public void RefreshArkFeatureVisibility()
        {
            if(!Application.isPlaying)return;
            bool relic=ArkProgression.IsUnlocked(ArkProgressionConfig.RelicNode);
            bool train=ArkProgression.IsUnlocked(ArkProgressionConfig.TrainNode);
            if(relicGachaButton!=null)relicGachaButton.gameObject.SetActive(relic);
            if(characterSelectionPanel!=null)
            {
                var root=characterSelectionPanel.transform;
                var slot=root.Find("DrawnRelicSlot");if(slot!=null)slot.gameObject.SetActive(relic);
                var header=root.Find("TrainCustomizationHeader");if(header!=null)header.gameObject.SetActive(train);
            }
            if(!relic)
            {
                selectedStartingRelic=null;
                if(relicGachaModal!=null)relicGachaModal.SetActive(false);
                if(relicPreviewImageComponent!=null)relicPreviewImageComponent.sprite=null;
            }
        }

        private void BindArkTitle()
        {
            if(titlePanel==null)return;
            var buttons=titlePanel.transform.Find("ButtonContainer");if(buttons==null)return;
            var entry=buttons.Find("Btn_특전")??buttons.Find("Btn_방주");
            if(entry!=null)
            {
                var label=entry.GetComponentInChildren<TextMeshProUGUI>(true);if(label!=null)label.text="방주";
                var button=entry.GetComponent<UnityEngine.UI.Button>();
                if(button!=null){button.onClick.RemoveAllListeners();button.onClick.AddListener(OnPerksClicked);}
            }
            if(relicGachaModal!=null)
                foreach(var label in relicGachaModal.GetComponentsInChildren<TextMeshProUGUI>(true))
                    if(label.text.Contains("특전"))label.text=label.text.Replace("특전","방주");
            RefreshArkFeatureVisibility();
        }
    }
}
