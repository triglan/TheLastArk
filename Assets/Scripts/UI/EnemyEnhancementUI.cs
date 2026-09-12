using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    [DisallowMultipleComponent]
    public sealed class EnemyEnhancementUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, Range(0f, 1f)] private float normalizedHeight = 0.6667f;
        [SerializeField] private Vector2 panelOffset = new Vector2(0f, 38f);

        private EnemyAI enemyAI;
        private GameObject panelRoot;
        private GameObject tooltipRoot;
        private TextMeshProUGUI label;
        private TextMeshProUGUI tooltip;
        private string currentSummary;
        private string currentDetail;
        private readonly List<Image> icons = new List<Image>();

        private void LateUpdate()
        {
            if (enemyAI == null) enemyAI = GetComponent<EnemyAI>();
            if (enemyAI == null || enemyAI.RuntimeState.Source == null) return;

            BuildText(enemyAI.RuntimeState, out string summary, out string detail, out List<Sprite> displayIcons);
            if (summary == currentSummary && detail == currentDetail) return;

            EnsureView();
            currentSummary = summary;
            currentDetail = detail;
            label.text = summary;
            tooltip.text = detail;
            UpdateIcons(displayIcons);
            panelRoot.SetActive(!string.IsNullOrEmpty(summary));
            tooltipRoot.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltipRoot != null && !string.IsNullOrEmpty(currentDetail)) tooltipRoot.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
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
                        : CombatIconLibrary.FindEnhancementIcon();
                    if (icon != null) displayIcons.Add(icon);
                }
            }

            if (GetComponent<BattleCharacter>()?.status?.activeStatusEffects != null)
            {
                foreach (ActiveStatusEffect effect in GetComponent<BattleCharacter>().status.activeStatusEffects)
                {
                    if (effect == null || !effect.IsActive || effect.durationType != StatusDurationType.Permanent) continue;
                    Sprite icon = source.enemyEnhancementIcon != null ? source.enemyEnhancementIcon : CombatIconLibrary.FindEnhancementIcon();
                    if (icon != null) displayIcons.Add(icon);
                    string name = ActiveStatusEffect.GetDisplayName(effect.effectType);
                    labels.Add($"{name} {effect.damagePerTurn:+#;-#;0}");
                    details.Add($"{name} {effect.damagePerTurn:+#;-#;0} (전투 중 영구)");
                }
            }

            summary = string.Join(" · ", labels);
            detail = string.Join("\n", details);
        }

        private void EnsureView()
        {
            if (panelRoot != null) return;

            panelRoot = new GameObject("Enemy Enhancements", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelRoot.transform.SetParent(transform, false);
            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            EnemyNameHoverUI nameHover = GetComponent<EnemyNameHoverUI>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, nameHover != null ? nameHover.NormalizedHeight : normalizedHeight);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = panelOffset;
            panelRect.sizeDelta = new Vector2(220f, 28f);

            Image background = panelRoot.GetComponent<Image>();
            background.color = new Color(0.08f, 0.35f, 0.12f, 0.85f);

            label = CreateText("Label", panelRoot.transform, Vector2.zero, new Vector2(212f, 28f), 16f, TextAlignmentOptions.Center);

            tooltipRoot = new GameObject("Tooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tooltipRoot.transform.SetParent(panelRoot.transform, false);
            RectTransform tooltipRect = tooltipRoot.GetComponent<RectTransform>();
            tooltipRect.anchorMin = tooltipRect.anchorMax = new Vector2(0.5f, 1f);
            tooltipRect.pivot = new Vector2(0.5f, 0f);
            tooltipRect.anchoredPosition = new Vector2(0f, 6f);
            tooltipRect.sizeDelta = new Vector2(220f, 64f);
            Image tooltipBackground = tooltipRoot.GetComponent<Image>();
            tooltipBackground.color = new Color(0f, 0f, 0f, 0.9f);
            tooltipBackground.raycastTarget = false;
            tooltip = CreateText("Text", tooltipRoot.transform, Vector2.zero, new Vector2(204f, 56f), 14f, TextAlignmentOptions.Center);
            tooltip.enableWordWrapping = true;
            tooltipRoot.SetActive(false);

        }

        private void UpdateIcons(List<Sprite> displayIcons)
        {
            while (icons.Count < displayIcons.Count)
            {
                GameObject icon = new GameObject("Enhancement Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                icon.transform.SetParent(panelRoot.transform, false);
                RectTransform iconRect = icon.GetComponent<RectTransform>();
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
                iconRect.pivot = new Vector2(0.5f, 1f);
                iconRect.sizeDelta = new Vector2(28f, 28f);
                Image image = icon.GetComponent<Image>();
                image.raycastTarget = false;
                icon.SetActive(false);
                icons.Add(image);
            }

            int count = displayIcons.Count;
            int rows = Mathf.CeilToInt(count / 5f);
            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(220f, count > 0 ? 32f + rows * 32f : 28f);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = count > 0 ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
            label.rectTransform.anchoredPosition = count > 0 ? new Vector2(0f, 12f) : Vector2.zero;
            for (int i = 0; i < icons.Count; i++)
            {
                bool active = i < count;
                icons[i].gameObject.SetActive(active);
                if (!active) continue;
                icons[i].sprite = displayIcons[i];
                int row = i / 5;
                int column = i % 5;
                int columnsInRow = Mathf.Min(5, count - row * 5);
                icons[i].rectTransform.anchoredPosition = new Vector2((column - (columnsInRow - 1) * 0.5f) * 34f, -2f - row * 32f);
            }
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
