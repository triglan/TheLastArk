using System.Collections;
using System.Collections.Generic;
using TheLastArk.Battle;
using TheLastArk.Skills;
using TheLastArk.UI;
using UnityEngine;

/// <summary>
/// Shared panic (players and enemies): starts when sanity hits 0, ends when real healing lifts it above 0,
/// cleared at battle end. One panic = one penalty on that character's own actions.
/// </summary>
public partial class BattleManager
{
    private bool _resolvingPanicTurn;
    private bool _executingPanicSkill;
    private PanicPresentationUI _panicPresentation;
    private readonly List<BattleCharacter> _panicActors = new List<BattleCharacter>();

    private void InitializePanic()
    {
        _panicPresentation = gameObject.AddComponent<PanicPresentationUI>();
        foreach (var party in new[] { playerParty, enemyParty })
            foreach (var actor in party)
                TrackPanic(actor);
    }

    /// <summary>Starts watching a combatant's panic (also used for summons joining mid-battle).</summary>
    public void TrackPanic(BattleCharacter actor)
    {
        if (actor?.status == null || _panicActors.Contains(actor)) return;
        actor.status.panic = new PanicState();
        actor.PanicChanged += OnPanicChanged;
        _panicActors.Add(actor);
        actor.SynchronizePanic();
    }

    private void ClearBattlePanic(bool refreshVisuals = true)
    {
        foreach (var actor in _panicActors)
        {
            if (actor == null) continue;
            actor.PanicChanged -= OnPanicChanged;
            actor.status?.panic?.Clear();
            if (refreshVisuals && actor.status != null) actor.view?.UpdateVisual(actor.status);
        }
        _panicActors.Clear();
    }

    private void OnPanicChanged(BattleCharacter actor)
    {
        if (actor.status.panic.IsActive && RelicRules.IsAlly(actor)) _panicPresentation?.Enqueue(actor);
        actor.view?.UpdateVisual(actor.status);
        RefreshSkillSlots();
    }

    private static PanicType PanicOf(BattleCharacter actor)
        => actor?.status?.panic != null ? actor.status.panic.Type : PanicType.None;

    /// <summary>강박 / 무기력 limits. Charge insertion into a not-yet-ready attack skill stays allowed (charge is kept).</summary>
    public string GetPanicSkillUnavailableReason(BattleCharacter actor, SkillInfo skill)
    {
        var panic = actor?.status?.panic;
        if (panic == null || !panic.IsActive || skill == null) return null;
        if (panic.Type == PanicType.Compulsion && panic.LockedSkill != null && panic.LockedSkill != skill)
            return $"강박 · {panic.LockedSkill.skillName}만 사용 가능";
        if (panic.Type == PanicType.Lethargy && IsAttackLevel(GetSkillLevel(skill, actor)))
        {
            DiceInputRule rule = GetDiceRule(skill, actor);
            if (rule.IsCharge && !IsChargeReady(actor, skill)) return null;
            return "무기력 · 공격 스킬 사용 불가";
        }
        return null;
    }

    /// <summary>강박: the first skill actually fired after the panic began becomes the only usable one.</summary>
    private static void LockCompulsion(BattleCharacter actor, SkillInfo skill)
    {
        var panic = actor?.status?.panic;
        if (panic != null && panic.Type == PanicType.Compulsion && panic.LockedSkill == null) panic.LockedSkill = skill;
    }

    /// <summary>환각 applies to single-primary attack skills.</summary>
    private static bool IsHallucinatingAttack(BattleCharacter actor, SkillLevelData level)
        => PanicOf(actor) == PanicType.Hallucination && IsAttackLevel(level) && TargetResolver.IsTauntAffected(level.targetType);

    /// <summary>A random living combatant from both sides, the attacker included.</summary>
    public BattleCharacter HallucinationTarget(BattleCharacter actor)
    {
        var candidates = new List<BattleCharacter>();
        foreach (var party in new[] { playerParty, enemyParty })
            foreach (var unit in party)
                if (unit?.status != null && unit.status.currentHp > 0f) candidates.Add(unit);
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    /// <summary>발작: at its side's turn end, every other living member of that side loses 5 sanity (snapshot, no immediate chain).</summary>
    private static void ApplySeizure(List<BattleCharacter> party)
    {
        if (party == null) return;
        var seizing = party.FindAll(c => c?.status != null && c.status.currentHp > 0f && PanicOf(c) == PanicType.Seizure);
        foreach (var source in seizing)
            foreach (var other in party)
                if (other != null && other != source && other.status != null && other.status.currentHp > 0f)
                    other.TakeMentalDamage(PanicState.SeizureSanityLoss);
    }

    private IEnumerator ResolvePanicTurnStart()
    {
        // Shared panics have no turn-start actions; just refresh what panic limits show.
        RefreshSkillSlots();
        RefreshDiceUI();
        UpdateTurnEndButtonState();
        yield break;
    }

    public bool CanAcceptDieOnSkill(int dieId, BattleCharacter actor, SkillInfo skill)
    {
        if (!UsesDice || !IsPlayerTurn || !CanUseDiceSkill(actor, skill)) return false;
        var die = _dicePool.Find(dieId);
        if (die == null || !die.IsAvailable || !GetDiceRule(skill, actor).AcceptsValue(GetDieInputValue(actor, die))) return false;
        if (IsChargeReady(actor, skill)) return false;
        return _pendingDiceActor != actor || _pendingDiceSkill != skill || !_selectedDiceIds.Contains(dieId);
    }
}
