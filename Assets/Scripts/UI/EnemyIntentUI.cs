using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    [DisallowMultipleComponent]
    public sealed class EnemyIntentUI : MonoBehaviour
    {
        private RectTransform panelRect;
        private Image background;
        private BattleHoverTooltip tooltip;
        private readonly Image[] icons = new Image[3];
        private readonly TextMeshProUGUI[] values = new TextMeshProUGUI[3];
        public RectTransform Panel => panelRect;

        public void Show(EnemyIntentData intent)
        {
            EnsureView();
            if (intent == null) { Hide(); return; }
            Color color = GetColor(intent.VisualType);
            background.color = new Color(color.r, color.g, color.b, 0.92f);
            int count = Mathf.Clamp(intent.Effects.Count, 1, 3);
            panelRect.sizeDelta = new Vector2(Mathf.Max(100f, count * 84f), 28f);
            for (int i = 0; i < icons.Length; i++)
            {
                bool active = i < count;
                icons[i].transform.parent.gameObject.SetActive(active);
                if (!active) continue;
                var effect = i < intent.Effects.Count ? intent.Effects[i] : null;
                icons[i].sprite = effect?.Icon ?? (effect != null && effect.ActionType == EnemyPatternActionType.Effect ? IconLibrary.FindIcon(effect.EffectType) : null);
                if (icons[i].sprite == null && effect != null)
                    icons[i].sprite = effect.ActionType == EnemyPatternActionType.StealGold ? Resources.Load<Sprite>("UI/icon_gold")
                        : intent.VisualType == EnemyIntentVisualType.Attack ? Resources.Load<Sprite>("Map/node_battle")
                        : intent.VisualType == EnemyIntentVisualType.Defend ? IconLibrary.FindArmorIcon() : IconLibrary.FindEnhancementIcon();
                icons[i].gameObject.SetActive(icons[i].sprite != null);
                values[i].text = effect != null ? CompactValue(effect) : GetBadge(intent.VisualType);
                if (i == 2 && intent.Effects.Count > 3) values[i].text += "+";
                var cell = (RectTransform)icons[i].transform.parent;
                cell.anchoredPosition = new Vector2((i - (count - 1) * 0.5f) * 84f, 0f);
                values[i].rectTransform.anchoredPosition = new Vector2(icons[i].sprite != null ? 12f : 0f, 0f);
            }
            tooltip.SetContent(intent.PatternName + "\n" + BuildDetail(intent));
            panelRect.gameObject.SetActive(true);
            GetComponent<CharacterView>()?.RefreshEnemyHUDLayout();
        }

        public void Hide() { if (panelRect != null) panelRect.gameObject.SetActive(false); }

        public void SetPosition(Vector2 position)
        {
            if (panelRect == null) return;
            panelRect.anchoredPosition = position;
        }

        private void EnsureView()
        {
            if (panelRect != null) return;
            var root = new GameObject("Enemy Intent", typeof(RectTransform), typeof(Image), typeof(BattleHoverTooltip));
            root.transform.SetParent(transform, false);
            panelRect = root.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            background = root.GetComponent<Image>();
            tooltip = root.GetComponent<BattleHoverTooltip>();
            for (int i = 0; i < icons.Length; i++)
            {
                var cell = new GameObject("Intent Cell", typeof(RectTransform)); cell.transform.SetParent(root.transform, false);
                ((RectTransform)cell.transform).sizeDelta = new Vector2(84f, 42f);
                var icon = new GameObject("Action Icon", typeof(RectTransform), typeof(Image)); icon.transform.SetParent(cell.transform, false);
                icons[i] = icon.GetComponent<Image>(); icons[i].raycastTarget = false; icons[i].preserveAspect = true;
                icons[i].rectTransform.sizeDelta = new Vector2(24f, 24f); icons[i].rectTransform.anchoredPosition = new Vector2(-28f, 0f);
                var label = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI)); label.transform.SetParent(cell.transform, false);
                values[i] = label.GetComponent<TextMeshProUGUI>(); values[i].fontSize = 18f; values[i].fontStyle = FontStyles.Bold;
                values[i].alignment = TextAlignmentOptions.Center; values[i].raycastTarget = false;
                values[i].rectTransform.sizeDelta = new Vector2(64f, 36f); TMPFontManager.ApplyFont(values[i]);
            }
        }

        public static string CompactValue(EnemyIntentEffectData effect)
        {
            switch (effect.ActionType)
            {
                case EnemyPatternActionType.StealGold: return "G " + effect.RuntimeValue;
                case EnemyPatternActionType.Summon: return "소환";
                case EnemyPatternActionType.Flee: return "도주";
                case EnemyPatternActionType.SetArmor: return "방어 " + effect.RuntimeValue;
                case EnemyPatternActionType.ModifyRuntimeVariable: return (effect.SetRuntimeValue ? "=" : "+") + effect.RuntimeValue;
                case EnemyPatternActionType.SacrificeAllies: return "의식";
                case EnemyPatternActionType.BeginDamageCheck: return "포식";
                case EnemyPatternActionType.ResolveDamageCheck: return "저지";
                case EnemyPatternActionType.RememberTarget: return "표적";
                case EnemyPatternActionType.RemoveStatus: return "해제";
                case EnemyPatternActionType.BreakShield: return "파괴";
                case EnemyPatternActionType.TriggerStatus: return "격발";
            }
            if ((effect.EffectType == EffectType.Damage || effect.EffectType == EffectType.MentalDamage) && effect.HitCount > 1)
                return $"{effect.Value:0.#}×{effect.HitCount}";
            return effect.Value > 0 ? effect.Value.ToString("0.#") : ActiveStatusEffect.GetDisplayName(effect.EffectType);
        }
        public static string BuildDetail(EnemyIntentData intent)
        {
            var text = new StringBuilder("예상 대상: ");
            text.Append(GetTargetLabel(intent));
            foreach (EnemyIntentEffectData effect in intent.Effects)
            {
                string label = GetEffectLabel(effect);
                if (string.IsNullOrEmpty(label)) continue;
                text.Append('\n').Append(label);
            }
            if (!string.IsNullOrEmpty(intent.Forecast)) text.Append('\n').Append(intent.Forecast);
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
            if (effect.ActionType == EnemyPatternActionType.SetArmor) return $"방어 {effect.RuntimeValue}";
            if (effect.ActionType == EnemyPatternActionType.BeginDamageCheck) return "포식 저지 시작";
            if (effect.ActionType == EnemyPatternActionType.ResolveDamageCheck) return $"저지 실패 시 물리 {effect.Value:0.#}";
            if (effect.ActionType == EnemyPatternActionType.Summon) return "아군 소환";
            if (effect.ActionType == EnemyPatternActionType.SacrificeAllies) return "아군 체력 소모 → 의식";
            if (effect.ActionType == EnemyPatternActionType.RemoveStatus) return ActiveStatusEffect.GetDisplayName(effect.EffectType) + " 해제";
            if (effect.ActionType == EnemyPatternActionType.BreakShield) return effect.Value > 0 ? $"보호막 {effect.Value:0.#} 파괴" : "보호막 전체 파괴";
            if (effect.ActionType == EnemyPatternActionType.TriggerStatus) return ActiveStatusEffect.GetDisplayName(effect.EffectType) + " 즉시 발동";
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
                EffectType.Reflect => "반사",
                EffectType.Pierce => "관통",
                EffectType.Taunt => "도발",
                EffectType.Focus => "집중",
                _ => effect.EffectType.ToString()
            };
            bool repeated = effect.EffectType == EffectType.Damage || effect.EffectType == EffectType.MentalDamage;
            if (effect.EffectType == EffectType.Reflect || effect.EffectType == EffectType.Pierce)
                return $"{type} {effect.Value:0.#}%";
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
