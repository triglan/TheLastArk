using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    [DisallowMultipleComponent]
    public sealed class EnemyEnhancementUI : MonoBehaviour
    {
        private EnemyAI enemyAI;
        private RectTransform panel;
        private TextMeshProUGUI label;
        private Image icon;
        private BattleHoverTooltip tooltip;
        private string currentSummary;
        private string currentDetail;
        public float LayoutHeight => panel != null && panel.gameObject.activeSelf ? 36f : 0f;

        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (enemyAI == null) enemyAI = GetComponent<EnemyAI>();
            if (enemyAI == null || enemyAI.RuntimeState.Source == null) return;
            BuildText(enemyAI.RuntimeState, out string summary, out string detail, out List<Sprite> displayIcons);
            if (summary == currentSummary && detail == currentDetail) return;
            EnsureView();
            currentSummary = summary;
            currentDetail = detail;
            label.text = "강화";
            icon.sprite = displayIcons.Count > 0 ? displayIcons[0] : null;
            icon.gameObject.SetActive(icon.sprite != null);
            tooltip.SetContent(summary + "\n" + detail);
            panel.gameObject.SetActive(!string.IsNullOrEmpty(summary));
            GetComponent<CharacterView>()?.RefreshEnemyHUDLayout();
        }

        public void SetPosition(Vector2 position)
        {
            if (panel != null) panel.anchoredPosition = position;
        }

        private void BuildText(EnemyRuntimeState state, out string summary, out string detail, out List<Sprite> displayIcons)
        {
            var labels = new List<string>();
            var details = new List<string>();
            displayIcons = new List<Sprite>();
            CharacterData source = state.Source;

            if (source.enemyRuntimeVariables != null)
            {
                foreach (EnemyRuntimeVariableDefinition definition in source.enemyRuntimeVariables)
                {
                    if (definition == null || !definition.showInUi || string.IsNullOrWhiteSpace(definition.id)) continue;
                    int value = state.Get(definition.id);
                    if (value == 0) continue;

                    string name = string.IsNullOrWhiteSpace(definition.displayName) ? definition.id : definition.displayName;
                    labels.Add($"{name} {value:+#;-#;0}");
                    details.Add(string.IsNullOrWhiteSpace(definition.displayDescription)
                        ? $"{name} {value:+#;-#;0}"
                        : $"{definition.displayDescription} ({value:+#;-#;0})");
                    Sprite icon = definition.displayIcon != null ? definition.displayIcon
                        : source.enemyEnhancementIcon != null ? source.enemyEnhancementIcon
                        : IconLibrary.FindEnhancementIcon();
                    if (icon != null) displayIcons.Add(icon);
                }
            }

            summary = string.Join(" · ", labels);
            detail = string.Join("\n", details);
        }

        private void EnsureView()
        {
            if (panel != null) return;
            var root = new GameObject("Enemy Enhancements", typeof(RectTransform), typeof(Image), typeof(BattleHoverTooltip));
            root.transform.SetParent(transform, false);
            panel = root.GetComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(100f, 28f);
            root.GetComponent<Image>().color = new Color(0.08f, 0.35f, 0.12f, 0.9f);
            tooltip = root.GetComponent<BattleHoverTooltip>();
            label = CreateText("Label", panel, new Vector2(12f, 0f), new Vector2(64f, 28f), 18f, TextAlignmentOptions.Center);
            var iconObject = new GameObject("Enhancement Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(panel, false);
            icon = iconObject.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.rectTransform.sizeDelta = new Vector2(24f, 24f);
            icon.rectTransform.anchoredPosition = new Vector2(-30f, 0f);
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            TMPFontManager.ApplyFont(text);
            return text;
        }
    }
}
