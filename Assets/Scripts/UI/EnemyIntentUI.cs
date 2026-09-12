using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    [DisallowMultipleComponent]
    public sealed class EnemyIntentUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField, Range(0f, 1f)] private float normalizedHeight = 0.6667f;
        [SerializeField] private Vector2 panelOffset = new Vector2(0f, -38f);
        [SerializeField] private Vector2 panelSize = new Vector2(220f, 102f);

        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI badgeText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI detailText;
        private GameObject panelRoot;
        private RectTransform panelRect;
        private readonly Image[] actionIcons = new Image[3];

        public void Show(EnemyIntentData intent)
        {
            CreateDefaultView();
            if (intent == null)
            {
                panelRoot.SetActive(false);
                return;
            }

            Color color = GetColor(intent.VisualType);
            background.color = new Color(color.r, color.g, color.b, 0.85f);
            badgeText.text = GetBadge(intent.VisualType);
            titleText.text = intent.PatternName;
            detailText.text = BuildDetail(intent);
            UpdateIcons(intent);
            panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void CreateDefaultView()
        {
            if (background != null) return;

            GameObject root = new GameObject("Enemy Intent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelRoot = root;
            root.transform.SetParent(transform, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            panelRect = rect;
            EnemyNameHoverUI nameHover = GetComponent<EnemyNameHoverUI>();
            float anchorHeight = nameHover != null ? nameHover.NormalizedHeight : normalizedHeight;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, anchorHeight);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = panelOffset;
            rect.sizeDelta = panelSize;

            background = root.GetComponent<Image>();
            background.raycastTarget = false;

            badgeText = CreateText("Badge", root.transform, new Vector2(8f, -6f), new Vector2(44f, 28f), 16f, TextAlignmentOptions.Center);
            titleText = CreateText("Title", root.transform, new Vector2(56f, -6f), new Vector2(156f, 28f), 18f, TextAlignmentOptions.Left);
            detailText = CreateText("Detail", root.transform, new Vector2(8f, -36f), new Vector2(204f, 60f), 15f, TextAlignmentOptions.TopLeft);
            titleText.fontStyle = FontStyles.Bold;

            for (int i = 0; i < actionIcons.Length; i++)
            {
                GameObject icon = new GameObject("Action Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                icon.transform.SetParent(root.transform, false);
                RectTransform iconRect = icon.GetComponent<RectTransform>();
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
                iconRect.pivot = new Vector2(0.5f, 1f);
                iconRect.sizeDelta = new Vector2(40f, 40f);
                actionIcons[i] = icon.GetComponent<Image>();
                actionIcons[i].raycastTarget = false;
                icon.SetActive(false);
            }
        }

        private void UpdateIcons(EnemyIntentData intent)
        {
            int count = 0;
            foreach (EnemyIntentEffectData effect in intent.Effects)
            {
                if (effect.Icon == null || count == actionIcons.Length) continue;
                actionIcons[count++].sprite = effect.Icon;
            }

            bool hasIcons = count > 0;
            panelRect.sizeDelta = new Vector2(panelSize.x, hasIcons ? 150f : panelSize.y);
            detailText.rectTransform.anchoredPosition = new Vector2(8f, hasIcons ? -84f : -36f);
            for (int i = 0; i < actionIcons.Length; i++)
            {
                bool active = i < count;
                actionIcons[i].gameObject.SetActive(active);
                if (active)
                    actionIcons[i].rectTransform.anchoredPosition = new Vector2((i - (count - 1) * 0.5f) * 48f, -38f);
            }
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            TMPFontManager.ApplyFont(text);
            return text;
        }

        private static string BuildDetail(EnemyIntentData intent)
        {
            var text = new StringBuilder("예상 대상: ");
            text.Append(GetTargetLabel(intent));
            foreach (EnemyIntentEffectData effect in intent.Effects)
            {
                string label = GetEffectLabel(effect);
                if (string.IsNullOrEmpty(label)) continue;
                text.Append('\n').Append(label);
            }
            return text.ToString();
        }

        private static string GetTargetLabel(EnemyIntentData intent)
        {
            if (intent.TargetType == TargetType.Self) return "자신";
            if (intent.TargetType == TargetType.AllEnemy || intent.TargetType == TargetType.AllFriendly) return "전체";
            if (intent.Targets != null && intent.Targets.Count == 1 && intent.Targets[0] != null) return intent.Targets[0].characterName;
            return intent.TargetSelection switch
            {
                EnemyTargetSelection.Leader => "선두",
                EnemyTargetSelection.LowestHp => "체력 최저",
                EnemyTargetSelection.HighestHp => "체력 최고",
                EnemyTargetSelection.Remembered => "기억 대상",
                _ => "무작위"
            };
        }

        private static string GetEffectLabel(EnemyIntentEffectData effect)
        {
            if (effect.ActionType == EnemyPatternActionType.ModifyRuntimeVariable)
                return $"{(string.IsNullOrWhiteSpace(effect.RuntimeDisplayName) ? effect.RuntimeVariableId : effect.RuntimeDisplayName)} {(effect.SetRuntimeValue ? "=" : "+")} {effect.RuntimeValue}";
            if (effect.ActionType == EnemyPatternActionType.StealGold) return $"골드 최대 {effect.RuntimeValue} 탈취";
            if (effect.ActionType == EnemyPatternActionType.Flee) return "도주";
            if (effect.ActionType == EnemyPatternActionType.RememberTarget) return "대상 기억";

            string type = effect.EffectType switch
            {
                EffectType.Damage => effect.DamageType == DamageType.Physical ? "물리" : effect.DamageType == DamageType.Magical ? "마법" : "고정",
                EffectType.MentalDamage => "정신 피해",
                EffectType.Heal => "회복",
                EffectType.MentalHeal => "정신 회복",
                EffectType.Shield => "보호막",
                EffectType.Strength => "힘",
                EffectType.Protection => "보호",
                EffectType.Weakness => "약화",
                EffectType.Vulnerable => "취약",
                EffectType.Bleed => "출혈",
                EffectType.Poison => "독",
                EffectType.Burn => "화상",
                EffectType.Stun => "기절",
                EffectType.Counter => "반격",
                EffectType.Taunt => "도발",
                EffectType.Focus => "집중",
                _ => effect.EffectType.ToString()
            };
            bool repeated = effect.EffectType == EffectType.Damage || effect.EffectType == EffectType.MentalDamage;
            return repeated ? $"{type} {effect.Value:0.##} × {effect.HitCount}" : $"{type} {effect.Value:0.##}";
        }

        private static string GetBadge(EnemyIntentVisualType type)
        {
            return type switch
            {
                EnemyIntentVisualType.Attack => "공격",
                EnemyIntentVisualType.Defend => "방어",
                EnemyIntentVisualType.Support => "강화",
                _ => "특수"
            };
        }

        private static Color GetColor(EnemyIntentVisualType type)
        {
            return type switch
            {
                EnemyIntentVisualType.Attack => new Color(0.55f, 0.12f, 0.08f),
                EnemyIntentVisualType.Defend => new Color(0.08f, 0.3f, 0.55f),
                EnemyIntentVisualType.Support => new Color(0.1f, 0.42f, 0.2f),
                _ => new Color(0.32f, 0.16f, 0.48f)
            };
        }
    }
}
