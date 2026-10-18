using System.Collections;
using System.Collections.Generic;
using TheLastArk.Battle;
using TheLastArk.UI;
using UnityEngine;

public partial class BattleManager
{
    private bool _resolvingPanicTurn;
    private bool _executingPanicSkill;
    private int _panicResolvedTurn = -1;
    private PanicPresentationUI _panicPresentation;
    private readonly List<BattleCharacter> _panicActors = new List<BattleCharacter>();

    private void InitializePanic()
    {
        _panicPresentation = gameObject.AddComponent<PanicPresentationUI>();
        foreach (var actor in playerParty)
        {
            if (actor?.status == null) continue;
            actor.status.panic = new PanicState();
            actor.PanicChanged += OnPanicChanged;
            _panicActors.Add(actor);
            actor.SynchronizePanic();
        }
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
        if (actor.status.panic.IsActive) _panicPresentation?.Enqueue(actor);
        actor.view?.UpdateVisual(actor.status);
        RefreshSkillSlots();
    }

    public string GetPanicSkillUnavailableReason(BattleCharacter actor, SkillInfo skill)
    {
        var panic = actor?.status?.panic;
        if (panic == null || !panic.IsActive) return null;
        if (panic.Type == PanicType.Despair) return "절망 · 스킬 사용 불가";
        if (panic.Type != PanicType.Compulsion || panic.RequiredSkill == null) return null;
        // Do not trap a character when a target dies or the shared dice/AP are spent by another ally.
        if (!IsPanicCandidate(actor, panic.RequiredSkill)) panic.RequiredSkill = null;
        return panic.RequiredSkill == null || panic.RequiredSkill == skill
            ? null : $"강박증 · {panic.RequiredSkill.skillName} 먼저 사용";
    }

    private SkillLevelData PanicSkillLevel(BattleCharacter actor, SkillInfo skill)
    {
        return actor?.status == null || skill?.levels == null || skill.levels.Length == 0
            ? null : skill.levels[Mathf.Clamp(actor.status.SkillLevelIndex, 0, skill.levels.Length - 1)];
    }

    private List<BattleCharacter> PanicTargets(BattleCharacter actor, SkillInfo skill)
    {
        var targets = new List<BattleCharacter>();
        var level = PanicSkillLevel(actor, skill);
        if (level == null) return targets;
        if (level.targetType == TargetType.Self)
        {
            if (actor.status.currentHp > 0f) targets.Add(actor);
            return targets;
        }
        foreach (var party in new[] { playerParty, enemyParty })
            foreach (var target in party)
                if (target?.status != null && target.status.currentHp > 0f
                    && TargetResolver.IsValid(target, level.targetType, playerParty, enemyParty, actor)) targets.Add(target);
        return targets;
    }

    private int PreviewSkillCost(BattleCharacter actor, SkillInfo skill, SkillLevelData level)
    {
        int cost = level.overrideCost >= 0 ? level.overrideCost : skill.baseCost;
        if (_arcanaState.isEmperorActiveThisTurn || (_sinState.currentSin == SinType.Pride && !_sinState.isIndulgedCurrentSin)) cost = 0;
        else
        {
            if (_arcanaState.foolFreeSkillsRemaining > 0 || _limitFirstSkillFreeThisTurn) cost = 0;
            if (_arcanaState.isHierophantActiveThisTurn && cost >= 3) cost--;
            bool attack = level.targetType != TargetType.Friendly && level.targetType != TargetType.AllFriendly && level.targetType != TargetType.Self
                || (level.effects != null && level.effects.Exists(e => e.type == EffectType.Damage));
            if (_arcanaState.isStrengthActiveThisTurn && attack) cost++;
        }
        return cost + Mathf.Max(0, Mathf.RoundToInt(actor.status.GetStatusValue(EffectType.Frost)));
    }

    private List<int> PanicDice(BattleCharacter actor, SkillInfo skill)
    {
        var ids = new List<int>();
        var rule = GetDiceRule(skill, actor);
        // Charge skills only act when ready (fired without dice); panic never inserts charge dice.
        if (rule.IsCharge) return IsChargeReady(actor, skill) ? ids : null;
        foreach (var die in _dicePool.Dice)
            if (die.IsAvailable && rule.AcceptsValue(GetDieInputValue(actor, die))) ids.Add(die.Id);
        ids.Sort((a, b) => _dicePool.Find(b).Value.CompareTo(_dicePool.Find(a).Value));
        int count = Mathf.Max(1, rule.diceCount);
        if (ids.Count < count) return null;
        if (ids.Count > count) ids.RemoveRange(count, ids.Count - count);
        return ids;
    }

    private bool IsPanicCandidate(BattleCharacter actor, SkillInfo skill)
    {
        var level = PanicSkillLevel(actor, skill);
        if (level == null || actor.status.currentHp <= 0f || !actor.status.dynamicActiveSkill.Contains(skill)) return false;
        if (actor.status.GetStatus(EffectType.Stun) != null) return false;
        if (actor.status.GetStatus(EffectType.Fatigue) != null && actor.status.lastUsedSkill == skill) return false;
        int slot = actor.status.dynamicActiveSkill.IndexOf(skill);
        if (actor.status.activeStatusEffects.Exists(e => e.IsActive && e.effectType == EffectType.Blockade && (e.skillSlot < 0 || e.skillSlot == slot))) return false;
        if (_sinState.charmedCharacters != null && _sinState.charmedCharacters.Contains(actor)) return false;
        if (_arcanaState.emperorBannedSkillsNextTurn != null && _arcanaState.emperorBannedSkillsNextTurn.Contains(skill)) return false;
        if (_arcanaState.isEmperorActiveThisTurn && _arcanaState.emperorUsedSkillsThisTurn.Contains(skill)) return false;
        bool martyr = TheLastArk.Managers.TrainManager.IsInitialized && TheLastArk.Managers.TrainManager.Instance.nexusCar != null
            && TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TheLastArk.Data.TrainPartEffectType.SinMartyrVow);
        if (!martyr && _sinState.currentSin == SinType.Sloth && !_sinState.isIndulgedCurrentSin && _sinState.skillsUsedThisTurn >= 3) return false;
        if (PanicTargets(actor, skill).Count == 0) return false;
        return UsesDice ? !_usedDiceSkills.Contains((actor, skill)) && PanicDice(actor, skill) != null
            : currentAP >= PreviewSkillCost(actor, skill, level);
    }

    private IEnumerator ResolvePanicTurnStart()
    {
        if (_panicResolvedTurn == _turnCount) yield break;
        _panicResolvedTurn = _turnCount;
        _resolvingPanicTurn = true;
        UpdateTurnEndButtonState();
        RefreshDiceUI();
        // Snapshot before damage: newly infected allies wait until their next turn.
        var actors = playerParty.FindAll(a => a?.status != null && a.status.currentHp > 0f && a.status.panic.IsActive);
        foreach (var actor in actors) actor.status.panic.RequiredSkill = null;
        foreach (var actor in actors)
        {
            while (_panicPresentation != null && _panicPresentation.IsPresenting) yield return null;
            if (_currentPhase != BattlePhase.PlayerTurn) break;
            if (actor == null || actor.status.currentHp <= 0f) continue;
            var panic = actor.status.panic;
            var allies = playerParty.FindAll(a => a != actor && a?.status != null && a.status.currentHp > 0f);
            switch (panic.Type)
            {
                case PanicType.SelfLoathing:
                    actor.SpendHealth(PanicState.SelfDamage(actor.status.currentHp, actor.status.FinalMaxHp));
                    break;
                case PanicType.Delirium:
                    if (allies.Count > 0) allies[Random.Range(0, allies.Count)].ReceiveDamage(
                        actor.status.FinalAttack * 0.3f + actor.status.FinalSpellPower * 0.3f, actor, DamageType.Physical, false);
                    break;
                case PanicType.ContagiousFear:
                    if (allies.Count > 0)
                    {
                        var victim = allies[Random.Range(0, allies.Count)];
                        victim.TakeMentalDamage(victim.status.FinalMaxMental * 0.1f);
                    }
                    break;
                case PanicType.Compulsion:
                case PanicType.Impulse:
                    var skills = actor.status.dynamicActiveSkill.FindAll(s => IsPanicCandidate(actor, s));
                    if (skills.Count == 0) break;
                    int roll = Random.Range(1, skills.Count + 1);
                    var skill = skills[roll - 1];
                    if (panic.Type == PanicType.Compulsion) panic.RequiredSkill = skill;
                    else
                    {
                        if (Application.isPlaying)
                            NotificationManager.Instance?.ShowMessage($"[충동] {actor.characterName} · 주사위 {roll}/{skills.Count} → {skill.skillName}", Color.yellow);
                        var targets = PanicTargets(actor, skill);
                        _selectedDiceIds.Clear();
                        if (UsesDice) _selectedDiceIds.AddRange(PanicDice(actor, skill));
                        _selection.Set(skill, actor);
                        _executingPanicSkill = true;
                        try { PerformSkillOnTarget(targets[Random.Range(0, targets.Count)]); }
                        finally { _executingPanicSkill = false; CancelPendingSelection(); }
                    }
                    break;
            }
            actor.view?.UpdateVisual(actor.status);
            if (TryEndBattleIfPartyWiped()) break;
            yield return new WaitForSecondsRealtime(0.3f);
        }
        while (_panicPresentation != null && _panicPresentation.IsPresenting) yield return null;
        _resolvingPanicTurn = false;
        RefreshSkillSlots();
        RefreshDiceUI();
        UpdateTurnEndButtonState();
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
