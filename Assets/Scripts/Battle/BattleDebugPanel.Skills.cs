#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TheLastArk.Battle
{
    public sealed partial class BattleDebugPanel
    {
        private RectTransform skillContent;
        private int debugSkillIndex, debugSkillRank, debugSkillTarget;
        private int debugDie = 6;
        private bool debugRepeatStage;
        private readonly List<UnityEngine.UI.Button> skillActors = new List<UnityEngine.UI.Button>();
        private readonly List<UnityEngine.UI.Button> skillChoices = new List<UnityEngine.UI.Button>();
        private readonly List<UnityEngine.UI.Button> skillRanks = new List<UnityEngine.UI.Button>();
        private readonly List<UnityEngine.UI.Button> skillTargets = new List<UnityEngine.UI.Button>();
        private readonly List<BattleCharacter> targetChoices = new List<BattleCharacter>();
        private UnityEngine.UI.Button stageButton, visualButton, effectsButton;
        private TextMeshProUGUI diceText, skillState;

        private void ShowDebugPage(int index)
        {
            enemyContent.gameObject.SetActive(index == 0);
            panicContent.gameObject.SetActive(index == 1);
            skillContent.gameObject.SetActive(index == 2);
            Tell(index == 2 ? "미장착 스킬도 선택 가능 · 이펙트만 재생은 피해/자원 변화 없음" : "");
            RefreshView();
        }

        private void BuildSkillView()
        {
            skillContent = Rect("SkillControls", panel, 0, 120, 610, 652);
            Label(skillContent, "01  테스트할 아군", 18, 0, 574, 24, 20);
            for (int i = 0; i < manager.playerParty.Count; i++)
            {
                int index = i;
                skillActors.Add(Button("SkillActor" + i, skillContent, "", 18 + i % 2 * 291, 30 + i / 2 * 42, 283, 38,
                    () => { selectedAlly = index; RefreshView(); }, false));
            }
            Label(skillContent, "02  모든 액티브 스킬 · 장착 제한 없음", 18, 118, 574, 24, 20);
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                skillChoices.Add(Button("DebugSkill" + i, skillContent, "", 18 + i % 2 * 291, 148 + i / 2 * 44, 283, 40,
                    () => { debugSkillIndex = index; RefreshView(); }, false));
            }
            for (int i = 0; i < 3; i++)
            {
                int rank = i;
                skillRanks.Add(Button("SkillRank" + i, skillContent, i == 0 ? "기본" : "+" + i,
                    18 + i * 120, 242, 112, 34, () => { debugSkillRank = rank; RefreshView(); }, false));
            }
            stageButton = Button("CastStage", skillContent, "첫 시전", 388, 242, 204, 34,
                () => { debugRepeatStage = !debugRepeatStage; RefreshView(); }, false);
            Label(skillContent, "효과 눈금 (주사위 1개당)", 18, 289, 310, 32, 18);
            Button("LessDice", skillContent, "−", 340, 289, 42, 32, () => { debugDie = Mathf.Max(0, debugDie - 1); RefreshView(); }, false);
            diceText = Label(skillContent, "", 390, 289, 144, 32, 20);
            Button("MoreDice", skillContent, "+", 550, 289, 42, 32, () => { debugDie = Mathf.Min(99, debugDie + 1); RefreshView(); }, false);
            Button("FillResources", skillContent, "마력 / 축전 최대", 18, 330, 283, 34, () => { manager.DebugFillSkillResources(SelectedAlly); RefreshView(); });
            Button("EmptyResources", skillContent, "마력 / 축전 0", 309, 330, 283, 34, () => { manager.DebugFillSkillResources(SelectedAlly, true); RefreshView(); });
            Label(skillContent, "03  대상 · 자신 대상은 자동 선택", 18, 378, 574, 24, 20);
            for (int i = 0; i < 8; i++)
            {
                int index = i;
                skillTargets.Add(Button("SkillTarget" + i, skillContent, "", 18 + i % 4 * 145, 408 + i / 4 * 44, 139, 40,
                    () => { debugSkillTarget = index; RefreshView(); }, false));
            }
            visualButton = Button("PreviewSkillVfx", skillContent, "이펙트만 재생", 18, 508, 283, 44, () => PlayDebugSkill(false), false);
            effectsButton = Button("ForceSkillEffects", skillContent, "스킬 효과 강제 발동", 309, 508, 283, 44, () => PlayDebugSkill(true), false);
            skillState = Label(skillContent, "", 18, 562, 574, 76, 17);
            skillState.enableWordWrapping = true;
            skillContent.gameObject.SetActive(false);
        }

        private void RefreshSkillView()
        {
            if (skillContent == null) return;
            for (int i = 0; i < skillActors.Count; i++)
                SetChoice(skillActors[i], manager.playerParty[i]?.characterName ?? "없음", selectedAlly == i);
            var actor = SelectedAlly;
            var skills = actor?.status?.origin?.activeSkills;
            for (int i = 0; i < skillChoices.Count; i++)
            {
                var skill = skills != null && i < skills.Length ? skills[i] : null;
                SetChoice(skillChoices[i], skill?.skillName ?? "없음", debugSkillIndex == i);
                skillChoices[i].interactable = skill != null;
            }
            for (int i = 0; i < skillRanks.Count; i++) skillRanks[i].targetGraphic.color = i == debugSkillRank ? Accent : ButtonColor;
            SetChoice(stageButton, debugRepeatStage ? "재시전 단계" : "첫 시전 단계", debugRepeatStage);
            diceText.text = debugDie.ToString();
            targetChoices.Clear();
            targetChoices.AddRange(manager.playerParty);
            targetChoices.AddRange(manager.enemyParty);
            for (int i = 0; i < skillTargets.Count; i++)
            {
                var target = i < targetChoices.Count ? targetChoices[i] : null;
                bool alive = target?.status != null && target.gameObject.activeInHierarchy && target.status.currentHp > 0;
                SetChoice(skillTargets[i], target == null ? "빈 슬롯" : (manager.playerParty.Contains(target) ? "아군 " : "적 ") + target.characterName, debugSkillTarget == i);
                skillTargets[i].interactable = alive;
            }
            var selected = debugSkillTarget < targetChoices.Count ? targetChoices[debugSkillTarget] : null;
            string reason = manager.DebugSkillReason(actor, debugSkillIndex, debugSkillRank, debugRepeatStage, selected, false);
            visualButton.interactable = reason == null;
            string effectsReason = manager.DebugSkillReason(actor, debugSkillIndex, debugSkillRank, debugRepeatStage, selected, true);
            effectsButton.interactable = effectsReason == null;
            skillState.text = $"마력 {actor?.status?.PrimalMana ?? 0} · 축전 {actor?.status?.CapacitorCharge ?? 0}\n"
                + (reason ?? effectsReason ?? "강제 발동: 주사위/충전/횟수 제한 무시 · 피해와 자원 변화는 실제 적용")
                + "\n전투 전체 규칙은 일반 스킬 사용으로 최종 확인하세요.";
        }

        private static void SetChoice(UnityEngine.UI.Button button, string text, bool selected)
        {
            button.GetComponentInChildren<TextMeshProUGUI>().text = text;
            button.targetGraphic.color = selected ? Accent : ButtonColor;
        }

        private void PlayDebugSkill(bool effects)
        {
            var target = debugSkillTarget < targetChoices.Count ? targetChoices[debugSkillTarget] : null;
            bool played = manager.DebugPlaySkill(SelectedAlly, debugSkillIndex, debugSkillRank, debugRepeatStage, target, debugDie, effects);
            Tell(played ? effects ? "스킬 효과 발동 · 연습 모드 ON · 적 관리에서 재소환 가능" : "이펙트 재생 · 피해/자원 변화 없음" : "대상과 발동 조건을 확인하세요.");
            RefreshView();
        }
    }
}
#endif
