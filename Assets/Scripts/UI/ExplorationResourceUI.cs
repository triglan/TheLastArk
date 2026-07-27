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
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(0, 80);

            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            HorizontalLayoutGroup layout = panelObj.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(15, 15, 5, 5);
            layout.spacing = 15;
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
            consLayout.spacing = 8;
            consLayout.childControlWidth = false;
            consLayout.childControlHeight = false;
            consLayout.childForceExpandWidth = false;
            consLayout.childForceExpandHeight = false;

            for (int i = 0; i < 3; i++)
            {
                GameObject slotObj = new GameObject($"Slot_{i}");
                slotObj.transform.SetParent(consumableContainer.transform, false);
                RectTransform slotRect = slotObj.AddComponent<RectTransform>();
                slotRect.sizeDelta = new Vector2(60, 60);
                
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
                textTmp.fontSize = 14;
                textTmp.color = Color.white;
                textTmp.alignment = TextAlignmentOptions.Center;
                textTmp.enableWordWrapping = false;
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

            // ── Flexible Spacer: 소모품과 관리 아이콘 사이를 밀어내서 아이콘을 우측 끝으로 ──
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(panelObj.transform, false);
            spacer.AddComponent<RectTransform>();
            LayoutElement spacerLe = spacer.AddComponent<LayoutElement>();
            spacerLe.flexibleWidth = 1f; // 남은 공간을 전부 차지

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

            // ── 상단바 우측 끝 고정: 관리 아이콘 4개 (기차, 캐릭터, 가방, 환경설정) ──
            GameObject mgmtIconsContainer = new GameObject("ManagementIcons", typeof(RectTransform));
            mgmtIconsContainer.transform.SetParent(panelObj.transform, false);

            RectTransform mgmtRect = mgmtIconsContainer.GetComponent<RectTransform>();
            mgmtRect.anchorMin = new Vector2(1, 0.5f);
            mgmtRect.anchorMax = new Vector2(1, 0.5f);
            mgmtRect.pivot = new Vector2(1, 0.5f);
            mgmtRect.anchoredPosition = new Vector2(-15, 0); // 화면 우측 끝에서 15px 띄움
            mgmtRect.sizeDelta = new Vector2(270, 60);

            // 상단바 HorizontalLayoutGroup이 수동 앵커 위치를 무시하지 못하도록 ignoreLayout = true 설정
            LayoutElement mgmtLe = mgmtIconsContainer.AddComponent<LayoutElement>();
            mgmtLe.ignoreLayout = true;

            HorizontalLayoutGroup mgmtLayout = mgmtIconsContainer.AddComponent<HorizontalLayoutGroup>();
            mgmtLayout.spacing = 8;
            mgmtLayout.childControlWidth = false;
            mgmtLayout.childControlHeight = false;
            mgmtLayout.childForceExpandWidth = false;
            mgmtLayout.childForceExpandHeight = false;
            mgmtLayout.childAlignment = TextAnchor.MiddleRight;

            // 1) 기차 아이콘
            CreateTopBarIconButton(mgmtIconsContainer.transform, "🚆", "기차",
                new Color(0.2f, 0.35f, 0.55f, 1f), mainFont, () =>
                {
                    ManagementUIManager.Instance.Show(ManagementUIManager.TabType.Train);
                });

            // 2) 캐릭터 아이콘
            CreateTopBarIconButton(mgmtIconsContainer.transform, "👤", "캐릭터",
                new Color(0.25f, 0.45f, 0.3f, 1f), mainFont, () =>
                {
                    ManagementUIManager.Instance.Show(ManagementUIManager.TabType.Character);
                });

            // 3) 가방 아이콘
            CreateTopBarIconButton(mgmtIconsContainer.transform, "🎒", "가방",
                new Color(0.5f, 0.35f, 0.2f, 1f), mainFont, () =>
                {
                    ManagementUIManager.Instance.Show(ManagementUIManager.TabType.Inventory);
                });

            // 4) 환경설정 아이콘 (단독 팝업 호출)
            CreateTopBarIconButton(mgmtIconsContainer.transform, "⚙️", "설정",
                new Color(0.35f, 0.35f, 0.35f, 1f), mainFont, () =>
                {
                    ManagementUIManager.Instance.ShowSettingsPopup();
                });

            // 시너지 표시 바 생성
            SynergyUIBar synBar = gameObject.GetComponent<SynergyUIBar>();
            if (synBar == null) synBar = gameObject.AddComponent<SynergyUIBar>();
            synBar.Initialize(parent);

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

        /// <summary>상단바 우측 아이콘 버튼 하나를 생성합니다.</summary>
        private void CreateTopBarIconButton(Transform parent, string icon, string label,
            Color bgColor, TMP_FontAsset font, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject($"TopBarIcon_{label}");
            btnObj.transform.SetParent(parent, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(60, 60);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            // 아이콘 이모지 (상단)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.35f);
            iconRect.anchorMax = new Vector2(1, 1f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            TextMeshProUGUI iconTmp = iconObj.AddComponent<TextMeshProUGUI>();
            iconTmp.text = icon;
            iconTmp.fontSize = 24;
            iconTmp.color = Color.white;
            iconTmp.alignment = TextAlignmentOptions.Center;
            if (font != null) iconTmp.font = font;

            // 라벨 텍스트 (하단)
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 0.35f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.fontSize = 12;
            labelTmp.color = Color.white;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.enableWordWrapping = false;
            if (font != null) labelTmp.font = font;
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
