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
    public class ClusterCardUI : MonoBehaviour
    {
        private static ClusterCardUI _instance;
        public static ClusterCardUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ClusterCardUI>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ClusterCardUI");
                        _instance = go.AddComponent<ClusterCardUI>();
                    }
                }
                return _instance;
            }
        }

        private GameObject popupPanel;
        private Transform cardsContainer;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI subInfoText;
        private TextMeshProUGUI handStatusText;
        private TextMeshProUGUI resultSummaryText;

        private Button rerollButton;
        private TextMeshProUGUI rerollBtnText;
        private Image rerollBtnImg;

        private Button fateResetButton;
        private TextMeshProUGUI fateResetBtnText;
        private Image fateResetBtnImg;

        private Button confirmButton;
        private TextMeshProUGUI confirmBtnText;

        private TMP_FontAsset mainFont;

        private TrainCar currentNexusCar;
        private ClusterDeckSession session = new ClusterDeckSession();
        private HashSet<int> selectedIndicesToReroll = new HashSet<int>();
        private ClusterHandResult currentPreviewResult;
        private Action<int, ClusterHandResult> onConfirmCallback;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
        }

        public void ShowCardDraw(TrainCar nexusCar, Action<int, ClusterHandResult> onConfirm)
        {
            currentNexusCar = nexusCar;
            onConfirmCallback = onConfirm;
            selectedIndicesToReroll.Clear();

            if (popupPanel == null)
            {
                CreateUI();
            }

            session.StartNewTurnSession(nexusCar);
            EvaluateAndRefreshUI();

            popupPanel.SetActive(true);
            popupPanel.transform.SetAsLastSibling();
        }

        private void CreateUI()
        {
            mainFont = Resources.Load<TMP_FontAsset>("Fonts/Main_Fonts");

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("ClusterCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            popupPanel = new GameObject("ClusterCardPopup");
            popupPanel.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = popupPanel.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Semi-transparent dark backdrop
            Image bgOverlay = popupPanel.AddComponent<Image>();
            bgOverlay.color = new Color(0f, 0f, 0f, 0.82f);

            // Center Modal Window
            GameObject modalBox = new GameObject("ModalBox");
            modalBox.transform.SetParent(popupPanel.transform, false);
            RectTransform modalRect = modalBox.AddComponent<RectTransform>();
            modalRect.anchorMin = new Vector2(0.5f, 0.5f);
            modalRect.anchorMax = new Vector2(0.5f, 0.5f);
            modalRect.pivot = new Vector2(0.5f, 0.5f);
            modalRect.sizeDelta = new Vector2(680, 500);

            Image modalBg = modalBox.AddComponent<Image>();
            modalBg.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

            VerticalLayoutGroup vLayout = modalBox.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(20, 20, 16, 16);
            vLayout.spacing = 8;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            // 1. Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(modalBox.transform, false);
            LayoutElement tLe = titleObj.AddComponent<LayoutElement>();
            tLe.preferredHeight = 34;
            tLe.flexibleWidth = 1f;
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "<color=#FFCC00><b>[모듈: 클러스터] 포커 덱 완성</b></color>";
            titleText.fontSize = 23;
            titleText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) titleText.font = mainFont;

            // 2. Sub Info (Level & Divisor & Rerolls)
            GameObject subObj = new GameObject("SubInfo");
            subObj.transform.SetParent(modalBox.transform, false);
            LayoutElement sLe = subObj.AddComponent<LayoutElement>();
            sLe.preferredHeight = 22;
            sLe.flexibleWidth = 1f;
            subInfoText = subObj.AddComponent<TextMeshProUGUI>();
            subInfoText.text = "클러스터 사양";
            subInfoText.fontSize = 14;
            subInfoText.color = new Color(0.7f, 0.85f, 1f, 1f);
            subInfoText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) subInfoText.font = mainFont;

            // 3. Cards Area (5 cards container)
            GameObject cardsArea = new GameObject("CardsArea");
            cardsArea.transform.SetParent(modalBox.transform, false);
            LayoutElement caLe = cardsArea.AddComponent<LayoutElement>();
            caLe.preferredHeight = 150;
            caLe.flexibleWidth = 1f;

            Image caBg = cardsArea.AddComponent<Image>();
            caBg.color = new Color(0.04f, 0.06f, 0.10f, 0.90f);

            HorizontalLayoutGroup caHlg = cardsArea.AddComponent<HorizontalLayoutGroup>();
            caHlg.padding = new RectOffset(12, 12, 10, 10);
            caHlg.spacing = 10;
            caHlg.childAlignment = TextAnchor.MiddleCenter;
            caHlg.childControlWidth = true;
            caHlg.childControlHeight = true;
            caHlg.childForceExpandWidth = true;
            caHlg.childForceExpandHeight = true;

            cardsContainer = cardsArea.transform;

            // 4. Hand Status & Expected Hand AP
            GameObject handStatusObj = new GameObject("HandStatus");
            handStatusObj.transform.SetParent(modalBox.transform, false);
            LayoutElement hsLe = handStatusObj.AddComponent<LayoutElement>();
            hsLe.preferredHeight = 32;
            hsLe.flexibleWidth = 1f;
            handStatusText = handStatusObj.AddComponent<TextMeshProUGUI>();
            handStatusText.text = "현재 족보";
            handStatusText.fontSize = 19;
            handStatusText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) handStatusText.font = mainFont;

            // 5. Result Summary Text (문양 효과 프리뷰)
            GameObject sumObj = new GameObject("Summary");
            sumObj.transform.SetParent(modalBox.transform, false);
            LayoutElement sumLe = sumObj.AddComponent<LayoutElement>();
            sumLe.preferredHeight = 52;
            sumLe.flexibleWidth = 1f;
            resultSummaryText = sumObj.AddComponent<TextMeshProUGUI>();
            resultSummaryText.text = "문양 효과 요약";
            resultSummaryText.fontSize = 14;
            resultSummaryText.color = Color.white;
            resultSummaryText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) resultSummaryText.font = mainFont;

            // 6. Action Buttons Row
            GameObject btnRow = new GameObject("ButtonRow");
            btnRow.transform.SetParent(modalBox.transform, false);
            LayoutElement brLe = btnRow.AddComponent<LayoutElement>();
            brLe.preferredHeight = 48;
            brLe.flexibleWidth = 1f;

            HorizontalLayoutGroup brHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            brHlg.spacing = 12;
            brHlg.childControlWidth = true;
            brHlg.childControlHeight = true;
            brHlg.childForceExpandWidth = true;
            brHlg.childForceExpandHeight = true;

            // 6-1. Reroll Selected Button (선택 카드 다시 뽑기)
            GameObject rerollObj = new GameObject("RerollButton");
            rerollObj.transform.SetParent(btnRow.transform, false);
            rerollBtnImg = rerollObj.AddComponent<Image>();
            rerollBtnImg.color = new Color(0.24f, 0.40f, 0.75f, 1f);
            rerollButton = rerollObj.AddComponent<Button>();
            rerollButton.onClick.AddListener(OnRerollClicked);

            GameObject rTxtObj = new GameObject("Text");
            rTxtObj.transform.SetParent(rerollObj.transform, false);
            RectTransform rTxtRect = rTxtObj.AddComponent<RectTransform>();
            rTxtRect.anchorMin = Vector2.zero;
            rTxtRect.anchorMax = Vector2.one;
            rTxtRect.offsetMin = Vector2.zero;
            rTxtRect.offsetMax = Vector2.zero;
            rerollBtnText = rTxtObj.AddComponent<TextMeshProUGUI>();
            rerollBtnText.text = "선택 카드 교체";
            rerollBtnText.fontSize = 15;
            rerollBtnText.color = Color.white;
            rerollBtnText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) rerollBtnText.font = mainFont;

            // 6-2. Fate Reset Button (운명 재설정 - 1회 전체 교체)
            GameObject fateObj = new GameObject("FateResetButton");
            fateObj.transform.SetParent(btnRow.transform, false);
            fateResetBtnImg = fateObj.AddComponent<Image>();
            fateResetBtnImg.color = new Color(0.65f, 0.35f, 0.15f, 1f);
            fateResetButton = fateObj.AddComponent<Button>();
            fateResetButton.onClick.AddListener(OnFateResetClicked);

            GameObject fTxtObj = new GameObject("Text");
            fTxtObj.transform.SetParent(fateObj.transform, false);
            RectTransform fTxtRect = fTxtObj.AddComponent<RectTransform>();
            fTxtRect.anchorMin = Vector2.zero;
            fTxtRect.anchorMax = Vector2.one;
            fTxtRect.offsetMin = Vector2.zero;
            fTxtRect.offsetMax = Vector2.zero;
            fateResetBtnText = fTxtObj.AddComponent<TextMeshProUGUI>();
            fateResetBtnText.text = "운명 재설정 (전체)";
            fateResetBtnText.fontSize = 14;
            fateResetBtnText.color = Color.white;
            fateResetBtnText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) fateResetBtnText.font = mainFont;

            // 6-3. Confirm Button (덱 확정)
            GameObject confirmObj = new GameObject("ConfirmButton");
            confirmObj.transform.SetParent(btnRow.transform, false);
            Image cImg = confirmObj.AddComponent<Image>();
            cImg.color = new Color(0.18f, 0.65f, 0.35f, 1f);
            confirmButton = confirmObj.AddComponent<Button>();
            confirmButton.onClick.AddListener(OnConfirmClicked);

            GameObject cTxtObj = new GameObject("Text");
            cTxtObj.transform.SetParent(confirmObj.transform, false);
            RectTransform cTxtRect = cTxtObj.AddComponent<RectTransform>();
            cTxtRect.anchorMin = Vector2.zero;
            cTxtRect.anchorMax = Vector2.one;
            cTxtRect.offsetMin = Vector2.zero;
            cTxtRect.offsetMax = Vector2.zero;
            confirmBtnText = cTxtObj.AddComponent<TextMeshProUGUI>();
            confirmBtnText.text = "덱 확정 및 전투 개시";
            confirmBtnText.fontSize = 16;
            confirmBtnText.color = Color.white;
            confirmBtnText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) confirmBtnText.font = mainFont;
        }

        private void EvaluateAndRefreshUI()
        {
            currentPreviewResult = ClusterCardManager.EvaluateHand(session.currentHand, currentNexusCar, simulateClover: false);

            int level = currentNexusCar != null ? currentNexusCar.level : 0;
            int divisor = Mathf.Max(1, 5 - level);

            subInfoText.text = $"<color=yellow>넥서스 Lv.{level}</color> | 남은 재시도: <color=#00FFCC><b>{session.remainingRerolls}</b>회</color> | 효과 수식: <b>[숫자 / {divisor}]</b> | 카드를 클릭하여 교체 선택";

            // Render cards in container
            foreach (Transform child in cardsContainer)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < session.currentHand.Count; i++)
            {
                int cardIndex = i;
                ClusterCard card = session.currentHand[i];
                bool isSelected = selectedIndicesToReroll.Contains(cardIndex);

                GameObject cardObj = new GameObject($"Card_{i}");
                cardObj.transform.SetParent(cardsContainer, false);

                Image cBg = cardObj.AddComponent<Image>();
                cBg.color = isSelected ? new Color(0.35f, 0.15f, 0.18f, 1f) : new Color(0.14f, 0.18f, 0.26f, 1f);

                Button cardBtn = cardObj.AddComponent<Button>();
                cardBtn.onClick.AddListener(() => OnCardClicked(cardIndex));

                // Outline or Border effect
                Outline outline = cardObj.AddComponent<Outline>();
                outline.effectColor = isSelected ? new Color(1f, 0.3f, 0.3f, 1f) : (card.isWildCard ? new Color(1f, 0.85f, 0.2f, 0.8f) : new Color(0.3f, 0.4f, 0.6f, 0.4f));
                outline.effectDistance = isSelected ? new Vector2(3, 3) : new Vector2(1.5f, 1.5f);

                VerticalLayoutGroup cVlg = cardObj.AddComponent<VerticalLayoutGroup>();
                cVlg.padding = new RectOffset(6, 6, 6, 6);
                cVlg.spacing = 2;
                cVlg.childAlignment = TextAnchor.MiddleCenter;
                cVlg.childControlWidth = true;
                cVlg.childControlHeight = true;
                cVlg.childForceExpandWidth = true;
                cVlg.childForceExpandHeight = false;

                // 1. Status tag (선택됨 or 문양 태그)
                GameObject tagObj = new GameObject("TagText");
                tagObj.transform.SetParent(cardObj.transform, false);
                LayoutElement tagLe = tagObj.AddComponent<LayoutElement>();
                tagLe.preferredHeight = 16;
                tagLe.flexibleWidth = 1f;
                TextMeshProUGUI tagTmp = tagObj.AddComponent<TextMeshProUGUI>();
                tagTmp.text = isSelected ? "<color=#FF5555><b>[교체 선택]</b></color>" : $"<color=gray>#{i + 1}</color>";
                tagTmp.fontSize = 11;
                tagTmp.alignment = TextAlignmentOptions.Center;
                if (mainFont != null) tagTmp.font = mainFont;

                // 2. Suit Symbol & Number (Big)
                GameObject valObj = new GameObject("ValueText");
                valObj.transform.SetParent(cardObj.transform, false);
                LayoutElement vLe = valObj.AddComponent<LayoutElement>();
                vLe.preferredHeight = 65;
                vLe.flexibleWidth = 1f;
                TextMeshProUGUI vTmp = valObj.AddComponent<TextMeshProUGUI>();
                string colHex = ColorUtility.ToHtmlStringRGB(card.SuitColor);
                if (card.isWildCard)
                {
                    vTmp.text = $"<color=#{colHex}><b>★</b></color>\n<size=12><color=#FFD700>WILD</color></size>";
                }
                else
                {
                    vTmp.text = $"<color=#{colHex}><b>{card.SuitSymbol}</b></color> <color=white><b>{card.number}</b></color>";
                }
                vTmp.fontSize = 24;
                vTmp.alignment = TextAlignmentOptions.Center;
                if (mainFont != null) vTmp.font = mainFont;

                // 3. Card Individual Effect Description
                GameObject effObj = new GameObject("EffectText");
                effObj.transform.SetParent(cardObj.transform, false);
                LayoutElement effLe = effObj.AddComponent<LayoutElement>();
                effLe.preferredHeight = 36;
                effLe.flexibleWidth = 1f;
                TextMeshProUGUI effTmp = effObj.AddComponent<TextMeshProUGUI>();

                int suitVal = ClusterCardManager.CalculateSuitValue(card.resolvedNumber, level);
                string effDesc = card.resolvedSuit switch
                {
                    CardSuit.Spade => $"<color=#4DABF7>적 피해 {suitVal}</color>",
                    CardSuit.Diamond => $"<color=#FFD700>아군 보호 {suitVal}</color>",
                    CardSuit.Heart => $"<color=#FF5555>방어막 {suitVal}</color>",
                    CardSuit.Clover => $"<color=#55FF77>♣AP {card.resolvedNumber * 7}%</color>",
                    _ => ""
                };
                effTmp.text = effDesc;
                effTmp.fontSize = 11;
                effTmp.alignment = TextAlignmentOptions.Center;
                if (mainFont != null) effTmp.font = mainFont;
            }

            // Hand Status Text
            string handColor = currentPreviewResult.handType >= PokerHandType.Straight ? "#FFD700" : (currentPreviewResult.handType >= PokerHandType.TwoPair ? "#00FFCC" : "#FFFFFF");
            string ampStr = currentPreviewResult.hasPatternAmplifier ? " <color=#FFD700>(+문양증폭 1AP)</color>" : "";
            handStatusText.text = $"완성 족보: <color={handColor}><b>[{currentPreviewResult.handNameKorean}]</b></color> -> <b>기본 +{currentPreviewResult.baseHandAP} AP</b>{ampStr}";

            // Result Summary Text (Effect Breakdown)
            string spadeStr = currentPreviewResult.spadeDamageTotal > 0 ? $"<color=#4DABF7>♠적 피해: {currentPreviewResult.spadeDamageTotal}</color>  " : "";
            string heartStr = currentPreviewResult.heartShieldTotal > 0 ? $"<color=#FF5555>♥아군 방어막: {currentPreviewResult.heartShieldTotal}</color>  " : "";
            string diaStr = currentPreviewResult.diamondProtectionTotal > 0 ? $"<color=#FFD700>♦아군 보호: {currentPreviewResult.diamondProtectionTotal}</color>  " : "";
            string cloverStr = $"<color=#55FF77>♣클로버 확률 AP 보너스</color>";

            resultSummaryText.text = $"{spadeStr}{heartStr}{diaStr}\n{cloverStr} (확정 시 확률 판정 적용)";

            // Update Reroll Button
            bool canReroll = session.CanReroll() && selectedIndicesToReroll.Count > 0;
            rerollButton.interactable = session.CanReroll();
            if (rerollBtnImg != null)
            {
                rerollBtnImg.color = session.CanReroll() ? new Color(0.24f, 0.40f, 0.75f, 1f) : new Color(0.35f, 0.35f, 0.40f, 1f);
            }
            rerollBtnText.text = selectedIndicesToReroll.Count > 0
                ? $"선택 {selectedIndicesToReroll.Count}장 교체 ({session.remainingRerolls}회 남음)"
                : $"카드 선택 후 교체 ({session.remainingRerolls}회 남음)";

            // Update Fate Reset Button
            fateResetButton.gameObject.SetActive(session.hasFateResetPart);
            if (session.hasFateResetPart)
            {
                bool canFateReset = !session.hasFateResetUsed;
                fateResetButton.interactable = canFateReset;
                if (fateResetBtnImg != null)
                {
                    fateResetBtnImg.color = canFateReset ? new Color(0.75f, 0.40f, 0.15f, 1f) : new Color(0.35f, 0.35f, 0.40f, 1f);
                }
                fateResetBtnText.text = canFateReset ? "운명 재설정 (전체 리롤 1/1)" : "운명 재설정 (사용 완료)";
            }

            // Update Confirm Button
            confirmBtnText.text = $"덱 확정 (+{currentPreviewResult.baseHandAP + currentPreviewResult.amplifierAP} ~ 최대 AP)";
        }

        private void OnCardClicked(int index)
        {
            if (selectedIndicesToReroll.Contains(index))
            {
                selectedIndicesToReroll.Remove(index);
            }
            else
            {
                selectedIndicesToReroll.Add(index);
            }
            EvaluateAndRefreshUI();
        }

        private void OnRerollClicked()
        {
            if (selectedIndicesToReroll.Count == 0)
            {
                NotificationManager.Instance?.ShowMessage("교체할 카드를 1장 이상 클릭하여 선택해주세요.", Color.yellow);
                return;
            }

            if (!session.CanReroll())
            {
                NotificationManager.Instance?.ShowMessage("더 이상 카드 재시도 횟수가 남아있지 않습니다.", Color.red);
                return;
            }

            var list = new List<int>(selectedIndicesToReroll);
            session.RerollSelected(list);
            selectedIndicesToReroll.Clear();

            NotificationManager.Instance?.ShowMessage($"카드 {list.Count}장 교체 완료! (남은 재시도: {session.remainingRerolls}회)", Color.cyan);
            EvaluateAndRefreshUI();
        }

        private void OnFateResetClicked()
        {
            if (!session.hasFateResetPart || session.hasFateResetUsed) return;

            session.FateReset();
            selectedIndicesToReroll.Clear();

            NotificationManager.Instance?.ShowMessage("[운명 재설정] 5장의 카드를 전부 새로 드로우했습니다!", Color.green);
            EvaluateAndRefreshUI();
        }

        private void OnConfirmClicked()
        {
            // 최종 확정 평가 (클로버 확률 판정 수행)
            var finalResult = ClusterCardManager.EvaluateHand(session.currentHand, currentNexusCar, simulateClover: true);

            popupPanel.SetActive(false);
            onConfirmCallback?.Invoke(finalResult.totalGainedAP, finalResult);
        }
    }
}
