using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TheLastArk.Managers;

namespace TheLastArk.UI
{
    public class ExplorationResourceUI : MonoBehaviour
    {
        private TextMeshProUGUI trainHpText;
        private TextMeshProUGUI goldText;
        private List<Image> consumableIcons = new List<Image>();
        private List<TextMeshProUGUI> consumableTexts = new List<TextMeshProUGUI>();
        private Transform relicsContainer;

        public void Initialize(Transform parent)
        {
            // 상단 자원 패널 생성
            GameObject panelObj = new GameObject("ResourcePanel");
            panelObj.transform.SetParent(parent, false);
            panelObj.transform.SetAsLastSibling(); // 제일 위에 렌더링되게
            
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(0.5f, 1);
            panelRect.sizeDelta = new Vector2(0, 80);

            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            HorizontalLayoutGroup layout = panelObj.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.spacing = 30;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;

            // 폰트 로드
            TMPro.TMP_FontAsset mainFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Main_Fonts");

            // 기차 내구도 UI
            GameObject trainHpObj = new GameObject("TrainHpText");
            trainHpObj.transform.SetParent(panelObj.transform, false);
            trainHpText = trainHpObj.AddComponent<TextMeshProUGUI>();
            trainHpText.fontSize = 28;
            trainHpText.color = Color.green;
            trainHpText.text = "🚂 HP: 100/100";
            trainHpText.alignment = TextAlignmentOptions.Left;
            if (mainFont != null) trainHpText.font = mainFont;

            CreateDivider(panelObj.transform);

            // 골드 UI
            GameObject goldObj = new GameObject("GoldText");
            goldObj.transform.SetParent(panelObj.transform, false);
            goldText = goldObj.AddComponent<TextMeshProUGUI>();
            goldText.fontSize = 30;
            goldText.color = Color.yellow;
            goldText.text = "💰 0 G";
            goldText.alignment = TextAlignmentOptions.Left;
            if (mainFont != null) goldText.font = mainFont;

            CreateDivider(panelObj.transform);

            // 소모품 UI 텍스트 컴포넌트 저장을 위한 리스트
            consumableTexts = new List<TextMeshProUGUI>();

            // 소모품 UI (3칸)
            GameObject consumableContainer = new GameObject("Consumables");
            consumableContainer.transform.SetParent(panelObj.transform, false);
            HorizontalLayoutGroup consLayout = consumableContainer.AddComponent<HorizontalLayoutGroup>();
            consLayout.spacing = 10;
            consLayout.childControlWidth = false;
            consLayout.childControlHeight = false;
            consLayout.childForceExpandWidth = false;
            consLayout.childForceExpandHeight = false;

            for (int i = 0; i < 3; i++)
            {
                GameObject slotObj = new GameObject($"Slot_{i}");
                slotObj.transform.SetParent(consumableContainer.transform, false);
                RectTransform slotRect = slotObj.AddComponent<RectTransform>();
                slotRect.sizeDelta = new Vector2(70, 70); // 정사각형으로 변경
                
                Image bgImage = slotObj.AddComponent<Image>();
                bgImage.color = new Color(0, 0, 0, 0.5f); // 빈 칸 반투명 배경

                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(slotObj.transform, false);
                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.sizeDelta = Vector2.zero;
                
                Image iconImage = iconObj.AddComponent<Image>();
                iconImage.color = new Color(1, 1, 1, 0); // 처음엔 투명하게
                consumableIcons.Add(iconImage);

                // 이름 텍스트 표시용
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(slotObj.transform, false);
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI textTmp = textObj.AddComponent<TextMeshProUGUI>();
                textTmp.fontSize = 16;
                textTmp.color = Color.white;
                textTmp.alignment = TextAlignmentOptions.Center;
                textTmp.enableWordWrapping = false; // 글자가 세로로 줄바꿈되지 않도록 설정
                if (mainFont != null) textTmp.font = mainFont;
                consumableTexts.Add(textTmp);

                // 전투 중 클릭을 위한 Button 컴포넌트 추가
                UnityEngine.UI.Button btn = slotObj.AddComponent<UnityEngine.UI.Button>();
                int slotIndex = i; // 클로저용 변수
                btn.onClick.AddListener(() => {
                    var bm = FindObjectOfType<BattleManager>();
                    if (bm != null)
                    {
                        bm.SelectConsumable(slotIndex);
                    }
                });
            }

            // 유물 목록 UI (좌측 상단, 자원 바 아래)
            GameObject relicsObj = new GameObject("RelicsContainer");
            relicsObj.transform.SetParent(parent, false);
            relicsContainer = relicsObj.transform;

            RectTransform relicsRect = relicsObj.AddComponent<RectTransform>();
            relicsRect.anchorMin = new Vector2(0, 1);
            relicsRect.anchorMax = new Vector2(0, 1);
            relicsRect.pivot = new Vector2(0, 1);
            relicsRect.anchoredPosition = new Vector2(20, -90); // 높이 80짜리 패널 아래
            relicsRect.sizeDelta = new Vector2(600, 40);

            HorizontalLayoutGroup relicsLayout = relicsObj.AddComponent<HorizontalLayoutGroup>();
            relicsLayout.spacing = 5;
            relicsLayout.childControlWidth = true;

            // 이벤트 구독
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnGoldChanged += UpdateGold;
                ResourceManager.Instance.OnConsumablesChanged += UpdateConsumables;
                ResourceManager.Instance.OnRelicsChanged += UpdateRelics;
                
                UpdateGold();
                UpdateConsumables();
                UpdateRelics();
            }

            if (TrainManager.Instance != null)
            {
                TrainManager.Instance.OnDurabilityChanged += UpdateTrainHp;
                UpdateTrainHp();
            }
        }

        private void CreateDivider(Transform parent)
        {
            GameObject div = new GameObject("Divider");
            div.transform.SetParent(parent, false);
            RectTransform rect = div.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(2, 60);
            Image img = div.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.3f);
        }

        private void UpdateTrainHp()
        {
            if (trainHpText != null && TrainManager.Instance != null)
            {
                trainHpText.text = $"🚂 HP: {TrainManager.Instance.currentTrainDurability}/{TrainManager.Instance.maxTrainDurability}";
            }
        }

        private void UpdateGold()
        {
            if (goldText != null && ResourceManager.Instance != null)
            {
                goldText.text = $"💰 {ResourceManager.Instance.Gold} G";
            }
        }

        private void UpdateConsumables()
        {
            if (ResourceManager.Instance == null) return;
            var list = ResourceManager.Instance.Consumables;

            for (int i = 0; i < 3; i++)
            {
                if (i < list.Count && list[i] != null)
                {
                    if (list[i].icon != null)
                    {
                        consumableIcons[i].sprite = list[i].icon;
                        consumableIcons[i].color = Color.white;
                    }
                    else
                    {
                        consumableIcons[i].sprite = null;
                        consumableIcons[i].color = new Color(0, 0.5f, 0.5f, 0.8f); // 아이콘 없을 시 대체 배경색
                    }
                    consumableTexts[i].text = list[i].consumableName; // 이름 표시
                }
                else
                {
                    consumableIcons[i].sprite = null;
                    consumableIcons[i].color = new Color(1, 1, 1, 0);
                    consumableTexts[i].text = "";
                }
            }
        }

        private void UpdateRelics()
        {
            if (ResourceManager.Instance == null || relicsContainer == null) return;
            
            foreach (Transform child in relicsContainer)
            {
                Destroy(child.gameObject);
            }

            var list = ResourceManager.Instance.Relics;
            foreach (var relic in list)
            {
                GameObject iconObj = new GameObject($"Relic_{relic.relicName}");
                iconObj.transform.SetParent(relicsContainer, false);
                RectTransform rect = iconObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(40, 40);

                Image img = iconObj.AddComponent<Image>();
                if (relic.icon != null) img.sprite = relic.icon;
                else img.color = Color.yellow; // 이미지가 없을 때의 대체 색상
            }
        }

        private void OnDestroy()
        {
            if (ResourceManager.IsInitialized)
            {
                ResourceManager.Instance.OnGoldChanged -= UpdateGold;
                ResourceManager.Instance.OnConsumablesChanged -= UpdateConsumables;
                ResourceManager.Instance.OnRelicsChanged -= UpdateRelics;
            }
            if (TrainManager.IsInitialized)
            {
                TrainManager.Instance.OnDurabilityChanged -= UpdateTrainHp;
            }
        }
    }
}
