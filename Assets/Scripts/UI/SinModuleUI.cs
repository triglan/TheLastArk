using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheLastArk.Data;
using TheLastArk.Battle;
using TheLastArk.Managers;

namespace TheLastArk.UI
{
    public class SinModuleUI : MonoBehaviour
    {
        private static SinModuleUI _instance;
        public static SinModuleUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SinModuleUI>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SinModuleUI");
                        _instance = go.AddComponent<SinModuleUI>();
                    }
                }
                return _instance;
            }
        }

        // Popup Modal Elements
        private GameObject popupPanel;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI subInfoText;
        private TextMeshProUGUI descText;
        private TextMeshProUGUI apBadgeText;
        private Button confirmButton;
        private Outline modalOutline;

        // Persistent HUD Badge & Indulgence Button
        private GameObject hudPanel;
        private TextMeshProUGUI hudSinText;
        private Button indulgenceButton;
        private TextMeshProUGUI indulgenceBtnText;

        private TMP_FontAsset mainFont;

        private TrainCar currentNexusCar;
        private SinActiveState currentSinState;
        private Action<int> onConfirmCallback;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
        }

        public void ShowSinManifestation(SinType sin, TrainCar nexusCar, SinActiveState sinState, Action<int> onConfirm)
        {
            currentNexusCar = nexusCar;
            currentSinState = sinState;
            onConfirmCallback = onConfirm;

            if (popupPanel == null)
            {
                CreateUI();
            }

            var info = SinModuleManager.GetSinInfo(sin);
            int ap = SinModuleManager.CalculateSinAP(sin, nexusCar);
            string desc = SinModuleManager.GetSinDetailedDescription(sin, nexusCar);

            titleText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(info.themeColor)}><b>{info.FullTitle} 강림!</b></color>";
            subInfoText.text = $"<color=#00FFCC>3턴간 유지</color> | 넥서스 강화 Lv.{nexusCar?.level ?? 0}";
            apBadgeText.text = $"<color=yellow><b>+ {ap} AP 획득</b></color>";
            descText.text = desc;

            if (modalOutline != null)
            {
                modalOutline.effectColor = info.themeColor;
            }

            popupPanel.SetActive(true);
            popupPanel.transform.SetAsLastSibling();

            UpdateHUD(sinState, nexusCar);
        }

        public void UpdateHUD(SinActiveState sinState, TrainCar nexusCar)
        {
            currentSinState = sinState;
            currentNexusCar = nexusCar;

            if (hudPanel == null)
            {
                CreateHUD();
            }

            if (sinState == null || !sinState.currentSin.HasValue || sinState.remainingTurns <= 0)
            {
                hudPanel.SetActive(false);
                return;
            }

            hudPanel.SetActive(true);
            var info = SinModuleManager.GetSinInfo(sinState.currentSin.Value);
            string statusStr = sinState.isIndulgedCurrentSin ? " <color=green>(면죄됨)</color>" : "";
            hudSinText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(info.themeColor)}><b>[{info.nameKorean}의 죄]</b></color> (남은 턴: <color=yellow>{sinState.remainingTurns}</color>/3){statusStr}";

            // Indulgence Button visibility & interactability
            bool hasIndulgencePart = nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.SinIndulgence);
            bool canUseIndulgence = hasIndulgencePart && !sinState.isIndulgenceUsedInBattle && !sinState.isIndulgedCurrentSin;

            indulgenceButton.gameObject.SetActive(hasIndulgencePart);
            indulgenceButton.interactable = canUseIndulgence;
            indulgenceBtnText.text = sinState.isIndulgenceUsedInBattle ? "면죄부 소진" : "면죄부 사용";
        }

        private void CreateUI()
        {
            mainFont = Resources.Load<TMP_FontAsset>("Fonts/Main_Fonts");

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("SinCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            popupPanel = new GameObject("SinPopupPanel");
            popupPanel.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = popupPanel.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image bgOverlay = popupPanel.AddComponent<Image>();
            bgOverlay.color = new Color(0.04f, 0.02f, 0.03f, 0.88f);

            // Modal Box
            GameObject modalBox = new GameObject("ModalBox");
            modalBox.transform.SetParent(popupPanel.transform, false);
            RectTransform modalRect = modalBox.AddComponent<RectTransform>();
            modalRect.anchorMin = new Vector2(0.5f, 0.5f);
            modalRect.anchorMax = new Vector2(0.5f, 0.5f);
            modalRect.pivot = new Vector2(0.5f, 0.5f);
            modalRect.sizeDelta = new Vector2(560, 420);

            Image modalBg = modalBox.AddComponent<Image>();
            modalBg.color = new Color(0.1f, 0.07f, 0.08f, 0.98f);

            modalOutline = modalBox.AddComponent<Outline>();
            modalOutline.effectDistance = new Vector2(3f, 3f);
            modalOutline.effectColor = new Color(0.9f, 0.2f, 0.2f);

            VerticalLayoutGroup vLayout = modalBox.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(24, 24, 20, 20);
            vLayout.spacing = 10;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            // 1. Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(modalBox.transform, false);
            LayoutElement tLe = titleObj.AddComponent<LayoutElement>();
            tLe.preferredHeight = 36;
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.fontSize = 24;
            titleText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) titleText.font = mainFont;

            // 2. Sub Info
            GameObject subObj = new GameObject("SubInfo");
            subObj.transform.SetParent(modalBox.transform, false);
            LayoutElement sLe = subObj.AddComponent<LayoutElement>();
            sLe.preferredHeight = 22;
            subInfoText = subObj.AddComponent<TextMeshProUGUI>();
            subInfoText.fontSize = 14;
            subInfoText.color = new Color(0.8f, 0.8f, 0.9f);
            subInfoText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) subInfoText.font = mainFont;

            // 3. AP Badge
            GameObject badgeObj = new GameObject("ApBadge");
            badgeObj.transform.SetParent(modalBox.transform, false);
            LayoutElement bLe = badgeObj.AddComponent<LayoutElement>();
            bLe.preferredHeight = 30;
            apBadgeText = badgeObj.AddComponent<TextMeshProUGUI>();
            apBadgeText.fontSize = 18;
            apBadgeText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) apBadgeText.font = mainFont;

            // 4. Description Box
            GameObject descBox = new GameObject("DescBox");
            descBox.transform.SetParent(modalBox.transform, false);
            LayoutElement dLe = descBox.AddComponent<LayoutElement>();
            dLe.preferredHeight = 180;

            Image descBg = descBox.AddComponent<Image>();
            descBg.color = new Color(0.16f, 0.12f, 0.14f, 1f);

            GameObject descTxtObj = new GameObject("Text");
            descTxtObj.transform.SetParent(descBox.transform, false);
            RectTransform dRect = descTxtObj.AddComponent<RectTransform>();
            dRect.anchorMin = Vector2.zero;
            dRect.anchorMax = Vector2.one;
            dRect.offsetMin = new Vector2(14, 10);
            dRect.offsetMax = new Vector2(-14, -10);

            descText = descTxtObj.AddComponent<TextMeshProUGUI>();
            descText.fontSize = 14;
            descText.color = Color.white;
            descText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) descText.font = mainFont;

            // 5. Confirm Button
            GameObject cObj = new GameObject("ConfirmButton");
            cObj.transform.SetParent(modalBox.transform, false);
            LayoutElement cLe = cObj.AddComponent<LayoutElement>();
            cLe.preferredHeight = 44;

            Image cImg = cObj.AddComponent<Image>();
            cImg.color = new Color(0.6f, 0.15f, 0.2f, 1f);
            confirmButton = cObj.AddComponent<Button>();
            confirmButton.onClick.AddListener(OnConfirmClicked);

            GameObject cTxtObj = new GameObject("Text");
            cTxtObj.transform.SetParent(cObj.transform, false);
            RectTransform cTxtRect = cTxtObj.AddComponent<RectTransform>();
            cTxtRect.anchorMin = Vector2.zero;
            cTxtRect.anchorMax = Vector2.one;
            cTxtRect.offsetMin = Vector2.zero;
            cTxtRect.offsetMax = Vector2.zero;

            TextMeshProUGUI cTxt = cTxtObj.AddComponent<TextMeshProUGUI>();
            cTxt.text = "<b>죄악 수용 (진행)</b>";
            cTxt.fontSize = 16;
            cTxt.color = Color.white;
            cTxt.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) cTxt.font = mainFont;
        }

        private void CreateHUD()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            hudPanel = new GameObject("SinHUDIndicator");
            hudPanel.transform.SetParent(canvas.transform, false);

            RectTransform hRect = hudPanel.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 1f);
            hRect.anchorMax = new Vector2(0f, 1f);
            hRect.pivot = new Vector2(0f, 1f);
            hRect.anchoredPosition = new Vector2(16, -55);
            hRect.sizeDelta = new Vector2(280, 50);

            Image hBg = hudPanel.AddComponent<Image>();
            hBg.color = new Color(0.08f, 0.05f, 0.07f, 0.85f);

            Outline hOut = hudPanel.AddComponent<Outline>();
            hOut.effectColor = new Color(0.8f, 0.2f, 0.2f, 0.6f);
            hOut.effectDistance = new Vector2(1f, 1f);

            HorizontalLayoutGroup hLayout = hudPanel.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(8, 8, 4, 4);
            hLayout.spacing = 8;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = true;

            // HUD Sin Text
            GameObject txtObj = new GameObject("SinText");
            txtObj.transform.SetParent(hudPanel.transform, false);
            LayoutElement tLe = txtObj.AddComponent<LayoutElement>();
            tLe.preferredWidth = 175;
            hudSinText = txtObj.AddComponent<TextMeshProUGUI>();
            hudSinText.fontSize = 12;
            hudSinText.alignment = TextAlignmentOptions.Left;
            hudSinText.color = Color.white;
            if (mainFont != null) hudSinText.font = mainFont;

            // Indulgence Button
            GameObject indObj = new GameObject("IndulgenceBtn");
            indObj.transform.SetParent(hudPanel.transform, false);
            LayoutElement iLe = indObj.AddComponent<LayoutElement>();
            iLe.preferredWidth = 85;

            Image indImg = indObj.AddComponent<Image>();
            indImg.color = new Color(0.7f, 0.5f, 0.1f, 1f);
            indulgenceButton = indObj.AddComponent<Button>();
            indulgenceButton.onClick.AddListener(OnIndulgenceClicked);

            GameObject indTxtObj = new GameObject("Text");
            indTxtObj.transform.SetParent(indObj.transform, false);
            RectTransform iRect = indTxtObj.AddComponent<RectTransform>();
            iRect.anchorMin = Vector2.zero;
            iRect.anchorMax = Vector2.one;
            iRect.offsetMin = Vector2.zero;
            iRect.offsetMax = Vector2.zero;

            indulgenceBtnText = indTxtObj.AddComponent<TextMeshProUGUI>();
            indulgenceBtnText.text = "면죄부 사용";
            indulgenceBtnText.fontSize = 11;
            indulgenceBtnText.color = Color.white;
            indulgenceBtnText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) indulgenceBtnText.font = mainFont;
        }

        private void OnConfirmClicked()
        {
            popupPanel.SetActive(false);
            if (currentSinState != null && currentSinState.currentSin.HasValue)
            {
                int ap = SinModuleManager.CalculateSinAP(currentSinState.currentSin.Value, currentNexusCar);
                onConfirmCallback?.Invoke(ap);
            }
        }

        private void OnIndulgenceClicked()
        {
            if (currentSinState == null || currentSinState.isIndulgenceUsedInBattle) return;

            currentSinState.isIndulgenceUsedInBattle = true;
            currentSinState.isIndulgedCurrentSin = true;
            currentSinState.rerollSinNextTurn = true;

            NotificationManager.Instance?.ShowMessage("<color=yellow>[면죄부 발동]</color> 현재 죄악의 부가 효과를 모두 정화했습니다! (다음 턴 새로운 죄악 재발동)", Color.yellow);
            UpdateHUD(currentSinState, currentNexusCar);
        }
    }
}
