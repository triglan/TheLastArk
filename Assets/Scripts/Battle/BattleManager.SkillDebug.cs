#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheLastArk.Skills;

public partial class BattleManager
{
    public SkillLevelData DebugSkillLevel(BattleCharacter actor, int index, int rank, bool repeatStage)
    {
        var skills = actor?.status?.origin?.activeSkills;
        if (skills == null || index < 0 || index >= skills.Length) return null;
        var levels = skills[index]?.levels;
        if (levels == null || rank < 0 || rank >= levels.Length || levels[rank] == null) return null;
        return PrimordialManaRules.Resolve(SkillStageRules.Resolve(levels[rank], repeatStage ? 1 : 0), actor.status);
    }

    public string DebugSkillReason(BattleCharacter actor, int index, int rank, bool repeatStage, BattleCharacter target, bool effects)
    {
        if (!IsPlayerTurn) return "플레이어 턴에만 실행할 수 있습니다. 연출 중에는 기다려 주세요.";
        if (!DebugLiving(actor) || !playerParty.Contains(actor)) return "살아 있는 아군을 선택하세요.";
        var level = DebugSkillLevel(actor, index, rank, repeatStage);
        if (level == null) return "스킬 데이터가 없습니다.";
        if (level.targetType == TargetType.Self) target = actor;
        if (!DebugLiving(target)) return "살아 있는 대상을 선택하세요.";
        if (level.excludeSelf && target == actor) return "자신에게 사용할 수 없는 스킬입니다.";
        if (level.targetType != TargetType.Self && !TargetResolver.IsValid(target, level.targetType, playerParty, enemyParty, actor, true))
            return "이 스킬에 맞는 적 또는 아군을 선택하세요.";
        if (!effects) return null;
        if (level.special == SkillSpecial.SplitDie) return "주사위 분할은 보관함에서 테스트하세요. 이펙트만 재생은 가능합니다.";
        return PrimordialManaRules.UnavailableReason(actor.status, level);
    }

    private static bool DebugLiving(BattleCharacter actor) => actor?.status != null
        && actor.gameObject.activeInHierarchy && actor.status.currentHp > 0f;

    public bool DebugPlaySkill(BattleCharacter actor, int index, int rank, bool repeatStage, BattleCharacter target, int dieValue, bool effects)
    {
        if (DebugSkillReason(actor, index, rank, repeatStage, target, effects) != null) return false;
        var skill = actor.status.origin.activeSkills[index];
        var level = DebugSkillLevel(actor, index, rank, repeatStage);
        if (level.targetType == TargetType.Self) target = actor;
        var targets = TargetResolver.Resolve(target, level.targetType, playerParty, enemyParty, actor, true);
        targets.RemoveAll(c => !DebugLiving(c));
        if (!effects)
        {
            var playback = new SkillVfxPlayback(level.visualEffect, level.effects);
            if (level.effects != null)
                foreach (var effect in level.effects)
                {
                    if (effect == null) continue;
                    var recipients = effect.targetScope == EffectTargetScope.Self ? new List<BattleCharacter> { actor }
                        : effect.targetScope == EffectTargetScope.AllAllies ? playerParty.FindAll(DebugLiving) : targets;
                    foreach (var recipient in recipients) playback.Play(effect, actor, recipient, skill.skillName);
                }
            if ((level.effects == null || level.effects.Count == 0) && level.visualEffect?.IsConfigured == true && Application.isPlaying)
                foreach (var recipient in targets) VFXManager.Instance.PlayConfigured(level.visualEffect, level.visualEffect.Recipient(actor, recipient));
            return true;
        }
        // Use the actual effect timeline (including repeats and resource branches), without
        // changing the equipped skill list, consuming pool dice, or advancing the turn.
        var cast = new SkillCast { IgnoreTaunt = true, SelfShield = actor.status.GetStatusValue(EffectType.Shield) };
        foreach (var ally in playerParty)
            if (DebugLiving(ally)) cast.AllyShieldTotal += ally.status.GetStatusValue(EffectType.Shield);
        int diceCount = Mathf.Max(1, level.diceInput?.diceCount ?? 1);
        for (int i = 0; i < diceCount; i++)
        {
            cast.EffectValues.Add(Mathf.Max(0, dieValue));
            cast.InputValues.Add(Mathf.Max(0, dieValue));
        }
        DebugHoldsBattleOpen = true;
        CancelPendingSelection();
        var execution = DebugRunSkill(actor, skill, level, target, targets, cast);
        if (Application.isPlaying) StartCoroutine(execution);
        else
        {
            try { while (execution.MoveNext()) { } }
            finally { (execution as System.IDisposable)?.Dispose(); }
        }
        return true;
    }

    private IEnumerator DebugRunSkill(BattleCharacter actor, SkillInfo skill, SkillLevelData level,
        BattleCharacter target, List<BattleCharacter> targets, SkillCast cast)
    {
        DebugActionRunning = true;
        UpdateTurnEndButtonState();
        try
        {
            var sequence = EffectEngine.ProcessSkillSequence(actor, targets, level, target, cast, skill.skillName);
            try { while (sequence.MoveNext()) yield return sequence.Current; }
            finally { (sequence as System.IDisposable)?.Dispose(); }
        }
        finally
        {
            DebugActionRunning = false;
            UpdateTurnEndButtonState();
            RefreshSkillSlots();
            if (UsesDice) RefreshDiceUI();
            DebugRefreshCombatants();
        }
    }

    public bool DebugFillSkillResources(BattleCharacter actor, bool empty = false)
    {
        if (!IsPlayerTurn || !DebugLiving(actor) || !playerParty.Contains(actor)) return false;
        if (empty) { actor.status.SpendPrimalMana(actor.status.PrimalMana); actor.status.ResetCapacitor(); }
        else { actor.status.GainPrimalMana(actor.status.PrimalManaCapacity); actor.status.GainCapacitor(actor.status.MaxCapacitor); }
        actor.view?.UpdateVisual(actor.status);
        RefreshSkillSlots();
        return true;
    }
}
#endif
