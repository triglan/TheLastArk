using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using TheLastArk.Data;

namespace TheLastArk.UI
{
    public class EquipmentTooltipUI : MonoBehaviour
    {
        private static EquipmentTooltipUI instance;
        private static bool isQuitting = false;

        public static bool HasInstance => instance != null && !isQuitting;

        public static EquipmentTooltipUI Instance
        {
            get
            {
                if (isQuitting) return null;
                if (instance == null)
                {
                    instance = FindObjectOfType<EquipmentTooltipUI>();
                    if (instance == null && Application.isPlaying)
                    {
                        GameObject go = new GameObject("EquipmentTooltipUI");
                        instance = go.AddComponent<EquipmentTooltipUI>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        private GameObject tooltipPanel;
        private RectTransform tooltipRect;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI categoryText;
        private TextMeshProUGUI statsText;
        private TextMeshProUGUI passiveText;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                EnsureUI();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDisable()
        {
            if (instance == this) HideTooltip();
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void EnsureUI()
        {
            if (tooltipPanel != null) return;

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            tooltipPanel = new GameObject("EquipmentTooltipPanel");
            tooltipPanel.transform.SetParent(canvas.transform, false);
            tooltipPanel.transform.SetAsLastSibling();

            tooltipRect = tooltipPanel.AddComponent<RectTransform>();
            tooltipRect.sizeDelta = new Vector2(320, 240);

            Image bg = tooltipPanel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.14f, 0.96f);

            Outline outline = tooltipPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.8f, 0.2f, 0.9f);
            outline.effectDistance = new Vector2(2, -2);

            VerticalLayoutGroup layout = tooltipPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Text elements placed directly under tooltipPanel VerticalLayoutGroup
            titleText = CreateText(tooltipPanel.transform, "[1성] 장비 이름", 22, Color.cyan);
            categoryText = CreateText(tooltipPanel.transform, "계열: [공격력]", 16, Color.yellow);
            statsText = CreateText(tooltipPanel.transform, "공격력 +10", 18, Color.white);
            passiveText = CreateText(tooltipPanel.transform, "", 16, new Color(1f, 0.85f, 0.3f));

            tooltipPanel.SetActive(false);
            TMPFontManager.ApplyFontToAll(tooltipPanel.transform);
        }

        public void ShowTooltip(EquipmentData eq)
        {
            if (eq == null) return;
            EnsureUI();

            // Header Title
            titleText.text = $"[{eq.starLevel}성] {eq.equipmentName}";
            categoryText.text = $"계열: <color=#FFD700>{eq.category}</color>";

            // Stats Listing
            StringBuilder sb = new StringBuilder();
            if (eq.bonusAttack > 0) sb.AppendLine($"공격력 +{eq.bonusAttack:F0}");
            if (eq.bonusSpellPower > 0) sb.AppendLine($"주문력 +{eq.bonusSpellPower:F0}");
            if (eq.bonusHp > 0) sb.AppendLine($"체력 +{eq.bonusHp:F0}");
            if (eq.bonusMental > 0) sb.AppendLine($"정신력 +{eq.bonusMental:F0}");
            if (eq.bonusArmor > 0) sb.AppendLine($"방어력 +{eq.bonusArmor:F0}");
            if (eq.bonusMagicResist > 0) sb.AppendLine($"마법저항력 +{eq.bonusMagicResist:F0}");
            if (eq.bonusCritRate > 0) sb.AppendLine($"치명타율 +{eq.bonusCritRate:F0}%");

            statsText.text = sb.ToString().TrimEnd();

            // Passive Description
            if (!string.IsNullOrEmpty(eq.passiveSkillName))
            {
                passiveText.gameObject.SetActive(true);
                passiveText.text = $"<color=#FFD700>[고유효과] {eq.passiveSkillName}</color>\n{eq.passiveDescription}";
            }
            else
            {
                passiveText.gameObject.SetActive(false);
            }

            tooltipPanel.SetActive(true);
            FollowMousePosition();
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (tooltipPanel != null && tooltipPanel.activeSelf)
            {
                FollowMousePosition();
            }
        }

        private void FollowMousePosition()
        {
            if (tooltipRect == null) return;
            Vector2 mousePos = Input.mousePosition;
            float pivotX = (mousePos.x + 340 > Screen.width) ? 1.05f : -0.05f;
            float pivotY = (mousePos.y + 250 > Screen.height) ? 1.05f : -0.05f;

            tooltipRect.pivot = new Vector2(pivotX, pivotY);
            tooltipRect.position = mousePos;
        }

        private TextMeshProUGUI CreateText(Transform parent, string text, int fontSize, Color color)
        {
            GameObject txtObj = new GameObject($"Text_{fontSize}");
            txtObj.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.enableWordWrapping = true;
            tmp.font = TMPFontManager.MainKoreanFont;

            return tmp;
        }
    }
}
