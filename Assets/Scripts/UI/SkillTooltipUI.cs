using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using TheLastArk.UI;

namespace TheLastArk.UI
{
    public class SkillTooltipUI : MonoBehaviour
    {
        private static SkillTooltipUI instance;
        private static bool isQuitting = false;

        public static bool HasInstance => instance != null && !isQuitting;

        public static SkillTooltipUI Instance
        {
            get
            {
                if (isQuitting) return null;
                if (instance == null)
                {
                    instance = FindObjectOfType<SkillTooltipUI>();
                    if (instance == null && Application.isPlaying)
                    {
                        GameObject go = new GameObject("SkillTooltipUI");
                        instance = go.AddComponent<SkillTooltipUI>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        private GameObject tooltipPanel;
        private RectTransform tooltipRect;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI actorInfoText;
        private TextMeshProUGUI descriptionText;
        private TextMeshProUGUI levelProgressionText;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                isQuitting = true;
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
            float width = tooltipRect.sizeDelta.x;
            float height = tooltipRect.sizeDelta.y;

            // Pivot adjustment based on screen boundaries
            float pivotX = (mousePos.x + width + 30 > Screen.width) ? 1.05f : -0.05f;
            float pivotY = (mousePos.y + height + 30 > Screen.height) ? 1.05f : -0.05f;

            tooltipRect.pivot = new Vector2(pivotX, pivotY);
            tooltipRect.position = mousePos;
        }

        public void ShowTooltip(SkillInfo skill, CharacterStatus status, string charName, RectTransform slotTransform = null)
        {
            if (skill == null) return;
            EnsureUI();

            int skillLevelIdx = status != null ? status.SkillLevelIndex : 0;
            SkillLevelData currentLevelData = (skill.levels != null && skill.levels.Length > 0) ? 
                skill.levels[Mathf.Clamp(skillLevelIdx, 0, skill.levels.Length - 1)] : null;

            int cost = (currentLevelData != null && currentLevelData.overrideCost >= 0) ? currentLevelData.overrideCost : skill.baseCost;

            titleText.text = $"[{skill.skillName}]  <color=#FFD700>({cost} AP)</color>";
            string levelTitle = status != null ? status.LevelTitle : "0강";
            int charLevel = status != null ? status.charLevel : 0;
            actorInfoText.text = $"시전: <color=#00FFCE>{charName}</color> | 강화: <color=#79FF5B>{levelTitle} ({charLevel}강)</color>";

            StringBuilder descSb = new StringBuilder();
            float atk = status != null ? status.FinalAttack : 10f;
            float spellPower = status != null ? status.FinalSpellPower : 0f;

            if (currentLevelData != null && currentLevelData.effects != null && currentLevelData.effects.Count > 0)
            {
                foreach (var effect in currentLevelData.effects)
                {
                    float scalingStat = (effect.type == EffectType.Damage && effect.damageType == DamageType.Magical)
                        || effect.type == EffectType.MentalDamage || effect.type == EffectType.MentalHeal
                        ? spellPower
                        : atk;
                    float val = (scalingStat * effect.multiplier) + effect.fixedValue;
                    if (effect.type == EffectType.Damage)
                    {
                        string damageTypeName = effect.damageType switch
                        {
                            DamageType.Magical => "마법 피해",
                            DamageType.True => "고정 피해",
                            _ => "물리 피해"
                        };
                        string scalingStatName = effect.damageType == DamageType.Magical ? "주문력" : "공격력";
                        string hitText = Mathf.Max(1, effect.hitCount) > 1 ? $" × {Mathf.Max(1, effect.hitCount)}" : "";
                        descSb.AppendLine($"<color=#FF6B6B>{damageTypeName}: {effect.multiplier * 100:F0}% {scalingStatName} + {effect.fixedValue:F1} (예상 피해: {val:F1}{hitText})</color>");
                        continue;
                    }
                    switch (effect.type)
                    {
                        case EffectType.Damage:
                            descSb.AppendLine($"<color=#FF6B6B>[피해] 물리 피해: {effect.multiplier * 100:F0}% 공격력 (데미지: {val:F1})</color>");
                            break;
                        case EffectType.Heal:
                            descSb.AppendLine($"<color=#51CF66>[회복] 체력 회복: {effect.multiplier * 100:F0}% 계수 (회복량: {val:F1})</color>");
                            break;
                        case EffectType.MentalDamage:
                            descSb.AppendLine($"<color=#B197FC>[정신 피해] {effect.multiplier * 100:F0}% 주문력 + {effect.fixedValue:F1} (예상 피해: {val:F1})</color>");
                            break;
                        case EffectType.MentalHeal:
                            descSb.AppendLine($"<color=#74C0FC>[정신 회복] {effect.multiplier * 100:F0}% 주문력 + {effect.fixedValue:F1} (예상 회복: {val:F1})</color>");
                            break;
                        case EffectType.Stun:
                            descSb.AppendLine($"<color=#FCC419>[기절] {effect.duration}턴간 행동 불가</color>");
                            break;
                        case EffectType.Bleed:
                            descSb.AppendLine($"<color=#E03131>[출혈] {effect.value:F0} 피해 ({effect.duration}턴)</color>");
                            break;
                        case EffectType.Taunt:
                            descSb.AppendLine($"<color=#845EF7>[도발] {effect.charges}회 대신 피격</color>");
                            break;
                        case EffectType.Shield:
                            descSb.AppendLine($"<color=#4DABF7>[보호막] 보호막: {val:F1} 획득</color>");
                            break;
                        case EffectType.Counter:
                            descSb.AppendLine($"<color=#FF922B>[반격] 다음 {effect.charges}회 피격 시 반격</color>");
                            break;
                        case EffectType.Poison:
                            descSb.AppendLine($"<color=#51CF66>[독] {effect.value:F0}, 매턴 10% 감소 ({effect.duration}턴)</color>");
                            break;
                        case EffectType.Burn:
                            descSb.AppendLine($"<color=#FF6B6B>[화상] 최대 체력 {effect.value:F0}% 피해 ({effect.duration}턴)</color>");
                            break;
                        case EffectType.Strength:
                            descSb.AppendLine($"<color=#339AF0>[힘] 기본 공격력 +{(effect.value > 0 ? effect.value : effect.multiplier * 100f):F0}% ({effect.duration}턴)</color>");
                            break;
                        case EffectType.Focus:
                            descSb.AppendLine($"<color=#74C0FC>[집중] {effect.duration}턴 동안 도발 무시</color>");
                            break;
                        default:
                            if ((int)effect.type >= (int)EffectType.Blockade)
                                descSb.AppendLine($"[{effect.type}] 수치 {effect.value:F0}, {(effect.charges > 0 && (effect.type == EffectType.Counter || effect.type == EffectType.Taunt || effect.type == EffectType.Guard) ? $"{effect.charges}회" : $"{effect.duration}턴")}");
                            break;
                    }
                }
            }
            else
            {
                descSb.AppendLine("기본 스킬 효과가 적용됩니다.");
            }

            descriptionText.text = descSb.ToString();

            StringBuilder progSb = new StringBuilder();
            progSb.AppendLine("<color=#FFD700>-- 스킬 강화 효과 --</color>");
            progSb.AppendLine(status != null && status.charLevel >= 2 ? "[달성] <color=#79FF5B>2강 달성: 스킬 효과 +1단계 강화</color>" : "  <color=gray>2강 미달성: 스킬 +1단계 프리뷰</color>");
            progSb.AppendLine(status != null && status.charLevel >= 3 ? "[달성] <color=#79FF5B>3강 달성: 스킬 효과 +2단계 최고 강화</color>" : "  <color=gray>3강 미달성: 스킬 +2단계 최고 프리뷰</color>");

            levelProgressionText.text = progSb.ToString();

            tooltipPanel.SetActive(true);
            FollowMousePosition();
        }

        public void ShowTooltip(SkillInfo skill, BattleCharacter actor, RectTransform slotTransform = null)
        {
            if (skill == null) return;
            CharacterStatus st = actor != null ? actor.status : null;
            string cName = actor != null ? actor.characterName : "아군";
            ShowTooltip(skill, st, cName, slotTransform);
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        private void EnsureUI()
        {
            if (tooltipPanel != null) return;

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject cObj = new GameObject("TooltipCanvas");
                canvas = cObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 120;
                cObj.AddComponent<CanvasScaler>();
                cObj.AddComponent<GraphicRaycaster>();
            }

            tooltipPanel = new GameObject("SkillTooltipPanel");
            tooltipPanel.transform.SetParent(canvas.transform, false);

            tooltipRect = tooltipPanel.AddComponent<RectTransform>();
            tooltipRect.sizeDelta = new Vector2(320, 220);

            Image bg = tooltipPanel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f); // 반투명 다크 패널
            bg.raycastTarget = false;

            VerticalLayoutGroup layout = tooltipPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 8;
            layout.childControlHeight = false;

            ContentSizeFitter fitter = tooltipPanel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(tooltipPanel.transform, false);
            LayoutElement tLe = titleObj.AddComponent<LayoutElement>();
            tLe.preferredHeight = 30;
            titleText = CreateTextUI(titleObj.transform, "스킬 이름", 22, Color.yellow);

            // Actor Info
            GameObject actorObj = new GameObject("ActorInfo");
            actorObj.transform.SetParent(tooltipPanel.transform, false);
            LayoutElement aLe = actorObj.AddComponent<LayoutElement>();
            aLe.preferredHeight = 22;
            actorInfoText = CreateTextUI(actorObj.transform, "시전 캐릭터", 16, Color.white);

            // Divider line
            GameObject divObj = new GameObject("Divider");
            divObj.transform.SetParent(tooltipPanel.transform, false);
            LayoutElement dLe = divObj.AddComponent<LayoutElement>();
            dLe.preferredHeight = 2;
            Image dImg = divObj.AddComponent<Image>();
            dImg.color = new Color(1, 1, 1, 0.2f);
            dImg.raycastTarget = false;

            // Description
            GameObject descObj = new GameObject("Description");
            descObj.transform.SetParent(tooltipPanel.transform, false);
            LayoutElement descLe = descObj.AddComponent<LayoutElement>();
            descLe.preferredHeight = 80;
            descriptionText = CreateTextUI(descObj.transform, "스킬 상세 설명", 18, Color.white);

            // Progression
            GameObject progObj = new GameObject("Progression");
            progObj.transform.SetParent(tooltipPanel.transform, false);
            LayoutElement pLe = progObj.AddComponent<LayoutElement>();
            pLe.preferredHeight = 50;
            levelProgressionText = CreateTextUI(progObj.transform, "강화 단계", 14, Color.gray);

            tooltipPanel.SetActive(false);
            TMPFontManager.ApplyFontToAll(tooltipPanel.transform);
        }

        private TextMeshProUGUI CreateTextUI(Transform parent, string text, int fontSize, Color color)
        {
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(parent, false);
            RectTransform rect = txtObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.enableWordWrapping = true;
            tmp.font = TMPFontManager.MainKoreanFont;
            tmp.raycastTarget = false;

            return tmp;
        }
    }
}
