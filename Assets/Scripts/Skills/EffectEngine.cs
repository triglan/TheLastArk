using UnityEngine;
using System.Collections.Generic;
using TheLastArk.Data;
using TheLastArk.Managers;
using TheLastArk.UI;

public static class EffectEngine
{
    // 이번 턴 동안 각 적에게 부여된 화상 횟수 추적 (화염 망치용)
    private static Dictionary<BattleCharacter, int> _turnBurnCounts = new Dictionary<BattleCharacter, int>();
    private static int _lastTrackedTurn = -1;

    public static void ResetTurnBurnCounts(int currentTurn)
    {
        if (_lastTrackedTurn != currentTurn)
        {
            _lastTrackedTurn = currentTurn;
            _turnBurnCounts.Clear();
        }
    }

    /// <summary>Dice / shield inputs of the skill being fired (set by BattleManager around one fire; null for enemies).</summary>
    public static TheLastArk.Skills.SkillCast CurrentCast { get; set; }

    private static int CastDiceSum => CurrentCast?.DiceSum ?? 0;

    public static void ProcessSkill(BattleCharacter actor, List<BattleCharacter> targets, SkillLevelData data, string skillName = "", System.Action<DamageResult> onDamage = null)
    {
        ProcessSkill(actor, targets, data, null, skillName, onDamage);
    }

    /// <summary>Targets of one effect: the skill's targets, the caster, or every living ally of the caster.</summary>
    private static List<BattleCharacter> ScopedTargets(EffectEntry effect, BattleCharacter actor, List<BattleCharacter> targets)
    {
        switch (effect.targetScope)
        {
            case EffectTargetScope.Self:
                return new List<BattleCharacter> { actor };
            case EffectTargetScope.AllAllies:
                var bm = Object.FindObjectOfType<BattleManager>();
                bool enemySide = actor.status.origin != null && actor.status.origin.isEnemy;
                var party = bm == null ? null : enemySide ? bm.enemyParty : bm.playerParty;
                return party == null ? new List<BattleCharacter> { actor }
                    : party.FindAll(c => c != null && c.status != null && c.status.currentHp > 0f);
            default:
                return targets;
        }
    }

    /// <summary>Effective multiplier / fixed amount after dice scaling.</summary>
    public static float ScaledMultiplier(EffectEntry effect, int dice) => effect.multiplier + effect.multiplierPerDie * dice;
    public static float ScaledFixed(EffectEntry effect, int dice) => effect.fixedValue + effect.fixedPerDie * dice;
    public static float ScaledStatusValue(float baseValue, EffectEntry effect, int dice)
        => baseValue + (effect.valuePerDieDivisor > 0 ? Mathf.Floor(dice / (float)effect.valuePerDieDivisor) : 0f);

    /// <summary>
    /// Runs one skill / pattern as a single attack: taunt lock, once-per-skill protect, and 반격/반사 after all hits.
    /// </summary>
    public static void ProcessSkill(BattleCharacter actor, List<BattleCharacter> targets, SkillLevelData data, BattleCharacter primaryTarget, string skillName = "", System.Action<DamageResult> onDamage = null)
    {
        if (actor == null || actor.status == null || targets == null || data?.effects == null) return;
        var current = TheLastArk.Battle.AttackContext.Current;
        if (current != null && current.IsPatternScope && current.Attacker == actor)
        {
            current.Retarget(primaryTarget ?? InferPrimary(targets, data.targetType), data.targetType);
            ProcessSkillEffects(actor, targets, data, skillName, onDamage);
            return;
        }
        var context = TheLastArk.Battle.AttackContext.Begin(actor, primaryTarget ?? InferPrimary(targets, data.targetType), data.targetType);
        try { ProcessSkillEffects(actor, targets, data, skillName, onDamage); }
        finally { context.End(); }
    }

    /// <summary>Main target of a single-primary attack: the middle of a full adjacent hit, otherwise the first.</summary>
    private static BattleCharacter InferPrimary(List<BattleCharacter> targets, TargetType type)
    {
        if (targets.Count == 0) return null;
        return type == TargetType.AdjacentEnemy && targets.Count == 3 ? targets[1] : targets[0];
    }

    private static void ProcessSkillEffects(BattleCharacter actor, List<BattleCharacter> targets, SkillLevelData data, string skillName, System.Action<DamageResult> onDamage)
    {
        // 스킬 또는 패턴의 효과를 대상들에게 순서대로 적용합니다.

        float lastCalculatedValue = 0f;

        // 스킬 레벨에 지정된 커스텀 VFX가 있으면 대상들에게 재생
        if (Application.isPlaying && !string.IsNullOrEmpty(data.customVfxName))
        {
            foreach (BattleCharacter target in targets)
            {
                if (target != null)
                {
                    VFXManager.Instance?.SpawnVFX(data.customVfxName, target.transform.position);
                }
            }
        }

        foreach (EffectEntry effect in data.effects)
        {
            if (effect == null) continue;
            if (effect.type == EffectType.Taunt)
            {
                BattleCharacter target = targets.Find(candidate => candidate != null && candidate.status != null);
                if (target != null) ExecuteEffect(effect, actor, target, skillName, ref lastCalculatedValue);
                continue;
            }

            if (effect.type != EffectType.Focus) continue;
            ExecuteEffect(effect, actor, actor, skillName, ref lastCalculatedValue);
        }

        bool focusOnlyThisSkill = false;
        foreach (EffectEntry effect in data.effects)
        {
            if (effect == null) continue;
            if (effect.type == EffectType.Focus) { focusOnlyThisSkill |= effect.focusOnlyThisSkill; continue; }
            foreach (BattleCharacter target in ScopedTargets(effect, actor, targets))
            {
                if (target == null || target.status == null) continue;
                int hits = CurrentCast != null && CurrentCast.MatchedSecondUse && effect.matchedHitCount > 0
                    ? effect.matchedHitCount : effect.hitCount;
                int executions = effect.type == EffectType.Damage || effect.type == EffectType.MentalDamage
                    ? Mathf.Max(1, hits)
                    : 1;
                for (int i = 0; i < executions; i++)
                {
                    if ((effect.type == EffectType.Damage || effect.type == EffectType.MentalDamage)
                        && (target.status.currentHp <= 0f || actor.status.currentHp <= 0f)) break;
                    ExecuteEffect(effect, actor, target, skillName, ref lastCalculatedValue, onDamage);
                }
            }
        }
        // 집중 that lasts only for this skill.
        if (focusOnlyThisSkill) actor.status.activeStatusEffects.RemoveAll(e => e != null && e.effectType == EffectType.Focus);
    }

    private static void ExecuteEffect(EffectEntry effect, BattleCharacter actor, BattleCharacter target, string skillName, ref float lastCalculatedValue, System.Action<DamageResult> onDamage = null)
    {
        // 1. VFX 재생: 커스텀 지정이 있으면 그것을 재생, 없으면 스킬명/효과 타입별 스마트 VFX 자동 재생
        if (!Application.isPlaying)
        {
            // EditMode 테스트와 데이터 미리보기에서는 씬 VFX를 생성하지 않습니다.
        }
        else if (!string.IsNullOrEmpty(effect.customVfxName))
        {
            VFXManager.Instance?.SpawnVFX(effect.customVfxName, target.transform.position);
        }
        else
        {
            VFXManager.Instance?.PlaySmartSkillEffect(skillName, effect.type, effect.damageType, target);
        }

        int dice = CastDiceSum;
        if (effect.minDie > 0 && dice < effect.minDie) return;
        if (ResistedByFortress(effect.type, actor, target)) return;
        float chance = effect.chance + effect.chancePerDie * dice;
        // 불확실한 천칭: skill chance effects +25%p for allies (crit excluded).
        if (chance < 100f && TheLastArk.Battle.RelicRules.IsAlly(actor) && TheLastArk.Battle.RelicRules.Has(RelicEffectType.UncertainScales))
            chance += TheLastArk.Battle.RelicRules.Value(RelicEffectType.UncertainScales) * 100f;
        if (chance < 100f && Random.value * 100f >= Mathf.Max(0f, chance)) return;
        float multiplier = ScaledMultiplier(effect, dice);

        // 기본값은 시전자의 최종 공격력이고, 옵션에 따라 직전 결과를 다시 씁니다.
        bool usesSpellPower = (effect.type == EffectType.Damage && effect.damageType == DamageType.Magical)
            || effect.type == EffectType.MentalDamage || effect.type == EffectType.MentalHeal;
        bool usesActorStat = effect.type == EffectType.Damage ||
                             effect.type == EffectType.Heal ||
                             effect.type == EffectType.Shield ||
                             effect.type == EffectType.MentalDamage ||
                             effect.type == EffectType.MentalHeal ||
                             effect.type == EffectType.Bleed ||
                             effect.type == EffectType.Poison;
        float baseValue = effect.useActualResult
            ? lastCalculatedValue
            : !usesActorStat || Mathf.Approximately(multiplier, 0f)
                ? 0f
                : StatFor(effect, actor, usesSpellPower);
        float calculatedValue = (baseValue * multiplier) + ScaledFixed(effect, dice);
        if (effect.type == EffectType.Damage && actor.status.origin != null && actor.status.origin.isEnemy
            && Mathf.Approximately(multiplier, 0f))
        {
            if (effect.damageType == DamageType.Physical)
                calculatedValue += actor.status.AttackStatusModifier;
            else if (effect.damageType == DamageType.Magical)
                calculatedValue += actor.status.SpellPowerStatusModifier;
        }

        switch (effect.type)
        {
            case EffectType.Damage:
                // 치명타 판정
                bool isCrit = Random.value < (actor.status.FinalCritRate * 0.01f);
                bool isCritHit = false;
                float critScale = 1f;
                if (isCrit)
                {
                    float critMultiplier = TheLastArk.Character.SynergyEffects.CritDamagePercent(actor.status) * 0.01f;
                    // [저격수의 눈] 사수 아군의 치명타 피해량 +25%
                    if (actor.status.HasSynergy(SynergyType.Ranger) &&
                        ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.SniperEye))
                    {
                        critMultiplier += 0.25f;
                    }
                    if (target != null && actor.status.HasEquipment("Odachi")) critMultiplier += OdachiCritDamage;
                    calculatedValue = new TheLastArk.Battle.ValueModifier().AddMultiplier(critMultiplier).Raw(calculatedValue);
                    if (CurrentCast != null) CurrentCast.HadCrit = true;
                    critScale = critMultiplier;
                    Debug.Log($"[치명타!] {actor.characterName}의 공격이 치명타로 적중! (배율: {critMultiplier}x)");

                    // [유리 칼날] 치명타 공격 스킬 대상에게 출혈 3·3라운드, 대상별 스킬당 1회
                    if (CurrentCast != null && TheLastArk.Battle.RelicRules.IsAlly(actor) && TheLastArk.Battle.RelicRules.Has(RelicEffectType.GlassBlade)
                        && CurrentCast.Once.Add((target, "glass")))
                    {
                        target.status.ApplyStatusEffect(EffectType.Bleed, TheLastArk.Battle.RelicRules.Value(RelicEffectType.GlassBlade), 3, 0f, 0, -1, actor);
                    }
                    isCritHit = true;
                }

                if (effect.bonusPercentPerTargetBleed > 0f)
                    calculatedValue = new TheLastArk.Battle.ValueModifier()
                        .AddIncrease(target.status.GetStatusValue(EffectType.Bleed) * effect.bonusPercentPerTargetBleed * 0.01f)
                        .Raw(calculatedValue);
                if (CurrentCast != null)
                    calculatedValue = new TheLastArk.Battle.ValueModifier()
                        .AddIncrease(effect.damageType == DamageType.Physical ? CurrentCast.PhysicalDamageBonus : 0f)
                        .AddIncrease(CurrentCast.DamageBonus).Raw(calculatedValue);
                if (CurrentCast != null && target.status.origin != null && target.status.origin.isEnemy) CurrentCast.HitTargets.Add(target);
                var attack = TheLastArk.Battle.AttackContext.Current;
                float previousIgnore = attack?.ArmorIgnore ?? 0f;
                if (attack != null && CurrentCast != null && CurrentCast.MatchedSecondUse && effect.matchedArmorIgnorePercent > 0f)
                    attack.ArmorIgnore = effect.matchedArmorIgnorePercent * 0.01f;
                float hitValue = calculatedValue;
                if (CurrentCast != null && CurrentCast.FirstHitBonus > 0f && !CurrentCast.FirstHitDone && attack != null && target == attack.PrimaryTarget)
                {
                    // 배반자: one bonus on the primary target's first hit (defense and crit applied once with it).
                    hitValue += CurrentCast.FirstHitBonus * critScale;
                    CurrentCast.FirstHitDone = true;
                }
                DamageResult damageResult = target.ReceiveDamageDetailed(hitValue, actor, effect.damageType);
                if (attack != null) attack.ArmorIgnore = previousIgnore;
                lastCalculatedValue = damageResult.HealthDamage;
                if (CurrentCast != null)
                {
                    CurrentCast.HealthDamageDealt += damageResult.HealthDamage;
                    var receiver = damageResult.FinalTarget != null ? damageResult.FinalTarget : target;
                    if (effect.damageType == DamageType.Magical) TheLastArk.Skills.SkillCast.Add(CurrentCast.MagicHealthDamage, receiver, damageResult.HealthDamage);
                    if (effect.damageType == DamageType.Physical) TheLastArk.Skills.SkillCast.Add(CurrentCast.PhysicalHealthDamage, receiver, damageResult.HealthDamage);
                    // [날카로운 못] 물리 피해 대상에게 고정 피해 1, 대상별 스킬당 1회 (방어·치명 미적용, 보호막 흡수 가능)
                    if (effect.damageType == DamageType.Physical && TheLastArk.Battle.RelicRules.IsAlly(actor)
                        && TheLastArk.Battle.RelicRules.Has(RelicEffectType.SharpNail) && receiver.status.currentHp > 0f
                        && CurrentCast.Once.Add((receiver, "nail")))
                        CurrentCast.HealthDamageDealt += receiver.ReceiveDamage(SharpNailDamage, null, DamageType.True, false);
                }
                onDamage?.Invoke(damageResult);
                if (effect.splashAdjacentPercent > 0f) SplashAdjacent(effect, actor, target, calculatedValue * effect.splashAdjacentPercent * 0.01f);

                break;

            case EffectType.Heal:
                bool isPlayerActor = actor.status.origin != null && !actor.status.origin.isEnemy;
                // [기도실] 주고 받는 치유량 증가 배율
                float prayerRoomBonus = isPlayerActor && TrainManager.IsInitialized
                    ? TrainManager.Instance.GetPrayerRoomHealMultiplier()
                    : 0f;
                float finalHealValue = calculatedValue * (1f + prayerRoomBonus);

                // [타락 모듈] 적 대상 치유 시 치유량의 50%만큼 마법 피해
                bool convertsEnemyHealToDamage = isPlayerActor
                    && target?.status?.origin != null
                    && target.status.origin.isEnemy
                    && TrainManager.IsInitialized
                    && TrainManager.Instance.HasPartEffectInAnyCar(TrainPartEffectType.CorruptionModule);
                if (convertsEnemyHealToDamage)
                {
                    float corruptDmg = finalHealValue * 0.5f;
                    lastCalculatedValue = target.ReceiveDamage(corruptDmg, actor, DamageType.Magical);
                    NotificationManager.Instance?.ShowMessage($"[타락 모듈] {target.characterName}에게 마법 피해 {corruptDmg:F0}!", Color.magenta);
                    break;
                }

                lastCalculatedValue = target.ReceiveHeal(calculatedValue, actor, prayerRoomBonus);
                SupportRecipient(actor, target);

                // [하늘 가호 프로토콜] 치유로 아군 체력 100% 달성 시 해당 턴 공/주 +20%
                if (isPlayerActor && TrainManager.IsInitialized && TrainManager.Instance.HasPartEffectInAnyCar(TrainPartEffectType.SkyBlessingProtocol))
                {
                    if (target != null && target.status != null && target.status.currentHp >= target.status.FinalMaxHp)
                    {
                        target.status.bonusAttack += target.status.FinalAttack * 0.20f;
                        if (target.view != null) target.view.UpdateVisual(target.status);
                        NotificationManager.Instance?.ShowMessage($"[하늘 가호] {target.characterName} 풀피 달성! 이번 턴 공/주 +20%!", Color.yellow);
                    }
                }

                // [빛의 분수대] 단일 치유 시 50%만큼 최저 체력 아군 치유
                if (isPlayerActor && TrainManager.IsInitialized && TrainManager.Instance.HasPartEffectInAnyCar(TrainPartEffectType.FountainOfLight))
                {
                    var bm = Object.FindObjectOfType<BattleManager>();
                    if (bm != null && bm.playerParty != null)
                    {
                        BattleCharacter lowestAlly = null;
                        float lowestHpRatio = float.MaxValue;
                        foreach (var ally in bm.playerParty)
                        {
                            if (ally != null && ally.status != null && ally.status.currentHp > 0 && ally != target)
                            {
                                float ratio = ally.status.currentHp / ally.status.FinalMaxHp;
                                if (ratio < lowestHpRatio)
                                {
                                    lowestHpRatio = ratio;
                                    lowestAlly = ally;
                                }
                            }
                        }
                        if (lowestAlly != null)
                        {
                            float splashHeal = finalHealValue * 0.5f;
                            lowestAlly.ReceiveHeal(splashHeal, actor);
                            NotificationManager.Instance?.ShowMessage($"[빛의 분수대] {lowestAlly.characterName} 연쇄 치유 {splashHeal:F0}!", Color.cyan);
                        }
                    }
                }

                // [물고기와 빵 모듈] 시전자 본인 치유 시 20%만큼 모든 아군 광역 치유
                if (isPlayerActor && TrainManager.IsInitialized && TrainManager.Instance.HasPartEffectInAnyCar(TrainPartEffectType.LoavesAndFishesModule))
                {
                    if (actor != null && target == actor)
                    {
                        var bm = Object.FindObjectOfType<BattleManager>();
                        if (bm != null && bm.playerParty != null)
                        {
                            float aoeHeal = finalHealValue * 0.20f;
                            foreach (var ally in bm.playerParty)
                            {
                                if (ally != null && ally != actor && ally.status != null && ally.status.currentHp > 0)
                                {
                                    ally.ReceiveHeal(aoeHeal, actor);
                                }
                            }
                            NotificationManager.Instance?.ShowMessage($"[물고기와 빵] 전 아군 추가 광역 치유 {aoeHeal:F0}!", Color.cyan);
                        }
                    }
                }
                break;

            case EffectType.MentalDamage:
                calculatedValue = new TheLastArk.Battle.ValueModifier()
                    .AddIncrease(TheLastArk.Character.SynergyEffects.LowSanityMentalBonus(actor.status, target.status)).Raw(calculatedValue);
                lastCalculatedValue = target.TakeMentalDamage(calculatedValue, TheLastArk.Battle.RelicRules.IsAlly(actor));
                break;

            case EffectType.MentalHeal:
                lastCalculatedValue = target.ReceiveMentalHeal(calculatedValue);
                break;

            case EffectType.Bleed:
                int bleedTurns = Mathf.Max(1, effect.duration > 0 ? effect.duration : 3);
                float bleedDmg = TheLastArk.Battle.ValueModifier.Floor((effect.value > 0 ? effect.value : baseValue * effect.multiplier) * DotScale(actor, target));
                ApplyBleed(target, bleedDmg, bleedTurns, actor, effect.durationType);
                CheckMageRuneOfCycle(actor, target);
                lastCalculatedValue = bleedDmg;
                break;

            case EffectType.Poison:
                int poisonTurns = Mathf.Max(1, effect.duration > 0 ? effect.duration : 3);
                float poisonDmg = TheLastArk.Battle.ValueModifier.Floor((effect.value > 0 ? effect.value : baseValue * effect.multiplier) * DotScale(actor, target));
                ApplyPoison(target, poisonDmg, poisonTurns, actor, effect.durationType);
                CheckMageRuneOfCycle(actor, target);
                lastCalculatedValue = poisonDmg;
                break;

            case EffectType.Burn:
                int burnTurns = Mathf.Max(1, effect.duration > 0 ? effect.duration : 3);
                float burnDmg = TheLastArk.Battle.ValueModifier.Floor((effect.value > 0 ? effect.value : effect.multiplier * 100f) * DotScale(actor, target));
                ApplyBurn(target, burnDmg, burnTurns, actor, effect.durationType);
                CheckMageRuneOfCycle(actor, target);
                lastCalculatedValue = burnDmg;
                break;

            case EffectType.Stun:
                int stunTurns = Mathf.Max(1, effect.duration > 0 ? effect.duration : 3);
                target.status.ApplyStatusEffect(effect.type, 0f, stunTurns,
                    0f, 0, -1, actor, effect.durationType);
                CheckMageRuneOfCycle(actor, target);
                lastCalculatedValue = 0f;
                break;

            case EffectType.Strength:
            case EffectType.Weakness:
            case EffectType.Amplification:
            case EffectType.Frailty:
            case EffectType.Protection:
            case EffectType.Vulnerable:
            case EffectType.MagicGuard:
            case EffectType.Corrosion:
                float statValue = ScaledStatusValue(!Mathf.Approximately(effect.value, 0f)
                    ? effect.value
                    : !Mathf.Approximately(effect.fixedValue, 0f) ? effect.fixedValue : effect.multiplier, effect, dice);
                if (IsDebuffStat(effect.type)) statValue = TheLastArk.Battle.ValueModifier.Floor(statValue * DebuffScale(actor, target));
                else SupportRecipient(actor, target);
                target.status.ApplyStatusEffect(effect.type, statValue,
                    effect.duration > 0 ? effect.duration : TheLastArk.Battle.StatusRules.DefaultStatTurns,
                    0f, 0, -1, actor, effect.durationType);
                lastCalculatedValue = statValue;
                break;

            case EffectType.Focus:
                int focusTurns = Mathf.Max(1, effect.duration);
                actor.status.ApplyStatusEffect(effect.type, 0f, focusTurns, 0f, 0, -1, actor, effect.durationType);
                lastCalculatedValue = 0f;
                break;

            case EffectType.Taunt:
                int tauntCharges = Mathf.Max(1, effect.charges);
                RemoveOtherPartyTaunts(target);
                target.status.ApplyStatusEffect(effect.type, 0f, 0, 0f, tauntCharges, -1, actor, StatusDurationType.Charges);
                lastCalculatedValue = tauntCharges;
                break;

            case EffectType.Counter:
                int counterCharges = Mathf.Max(1, effect.charges);
                target.status.ApplyStatusEffect(effect.type, effect.value, 0, 0f, counterCharges, -1, actor,
                    effect.durationType == StatusDurationType.Default ? StatusDurationType.Charges : effect.durationType);
                lastCalculatedValue = counterCharges;
                break;

            case EffectType.Reflect:
                int reflectCharges = Mathf.Max(1, effect.charges);
                float reflectPercent = Mathf.Max(0f, effect.value);
                target.status.ApplyStatusEffect(effect.type, reflectPercent, 0, 0f,
                    reflectCharges, -1, actor, effect.durationType);
                lastCalculatedValue = reflectCharges;
                break;

            case EffectType.Shield:
                int shieldTurns = Mathf.Max(1, effect.duration);
                calculatedValue = TheLastArk.Battle.ValueModifier.Floor(calculatedValue);
                calculatedValue = target.GainShield(calculatedValue, actor, shieldTurns, effect.durationType);
                SupportRecipient(actor, target);
                lastCalculatedValue = calculatedValue;
                Debug.Log($"[보호막] {target.characterName} 보호막 {calculatedValue} 획득!");

                // [연인] 보호(보호막) 효과 아군 전체 동시 공유
                if (target.status.origin != null && !target.status.origin.isEnemy)
                {
                    var bm = Object.FindObjectOfType<BattleManager>();
                    if (bm != null && bm._arcanaState != null && bm._arcanaState.isLoversActiveThisTurn && bm.playerParty != null)
                    {
                        foreach (var ally in bm.playerParty)
                        {
                            if (ally != null && ally != target && ally.status != null && ally.status.currentHp > 0)
                            {
                                ally.status.ApplyStatusEffect(effect.type, calculatedValue, shieldTurns,
                                    0f, 0, -1, actor, effect.durationType);
                                if (ally.view != null) ally.view.UpdateVisual(ally.status);
                            }
                        }
                    }
                }
                break;

            case EffectType.Resurrection:
                target.TryTriggerResurrectionTrait();
                lastCalculatedValue = 1f;
                break;

            case EffectType.Blockade:
            case EffectType.Fatigue:
            case EffectType.Confusion:
            case EffectType.Frost:
            case EffectType.Fear:
            case EffectType.Pressure:
            case EffectType.Despair:
            case EffectType.Pierce:
                // 봉쇄·피로·냉기 are player-only limits.
                if (TheLastArk.Battle.StatusRules.IsPlayerOnly(effect.type) && target.status.origin != null && target.status.origin.isEnemy)
                {
                    lastCalculatedValue = 0f;
                    break;
                }
                float limitValue = ScaledStatusValue(effect.value, effect, dice);
                if (effect.type == EffectType.Fear || effect.type == EffectType.Pressure || effect.type == EffectType.Despair)
                    limitValue = TheLastArk.Battle.ValueModifier.Floor(limitValue * DebuffScale(actor, target));
                target.status.ApplyStatusEffect(effect.type, limitValue,
                    Mathf.Max(1, effect.duration > 0 ? effect.duration : 3),
                    effect.secondaryValue, 0, effect.skillSlot, actor, effect.durationType);
                if (effect.type == EffectType.Blockade)
                    Object.FindObjectOfType<BattleManager>()?.RefreshDiceBlockade();
                lastCalculatedValue = effect.value;
                break;

            case EffectType.HuntMark:
                Object.FindObjectOfType<BattleManager>()?.ApplyHuntMark(actor, target);
                lastCalculatedValue = 0f;
                break;

            case EffectType.DiceBoost:
                // Bonus to this ally's next dice uses this turn; each instance covers `charges` dice.
                target.status.ApplyStatusEffect(EffectType.DiceBoost, effect.value, 1, 0f, Mathf.Max(1, effect.charges), -1, actor, StatusDurationType.Turns);
                SupportRecipient(actor, target);
                lastCalculatedValue = effect.value;
                break;

            case EffectType.Guard:
                // secondaryValue = protect damage reduction (%) applied after the protector's defense.
                target.status.ApplyStatusEffect(effect.type, 0f, 0, effect.secondaryValue,
                    Mathf.Max(1, effect.charges), -1, actor, StatusDurationType.Charges);
                lastCalculatedValue = effect.charges;
                break;
        }

        if (CurrentCast != null && TheLastArk.Battle.RelicRules.IsEnemy(target) && IsStatusEffectType(effect.type)
            && target.status.GetStatus(effect.type) != null)
            CurrentCast.AppliedStatusToEnemy = true;

        if (effect.statusIcon != null)
        {
            ActiveStatusEffect activeEffect = target.status.GetStatus(effect.type);
            if (activeEffect != null) activeEffect.displayIcon = effect.statusIcon;
        }

        if (target?.view != null)
            target.view.UpdateVisual(target.status);
    }

    private const float SharpNailDamage = 1f;

    private static bool IsStatusEffectType(EffectType type)
        => type != EffectType.Damage && type != EffectType.Heal && type != EffectType.Shield && type != EffectType.MentalDamage
           && type != EffectType.MentalHeal && type != EffectType.Resurrection && type != EffectType.DiceBoost && type != EffectType.HuntMark;

    private const float OdachiCritDamage = 0.25f;
    private const float ElementalAmplify = 1.5f;
    private const float FortressResistChance = 0.25f;

    private static float DebuffScale(BattleCharacter actor, BattleCharacter target)
        => TheLastArk.Character.SynergyEffects.DebuffMultiplier(actor?.status, target?.status);

    /// <summary>원소 증폭: the wearer's skill DoTs on enemies +50% (relic / gear auto effects excluded).</summary>
    private static float DotScale(BattleCharacter actor, BattleCharacter target)
        => DebuffScale(actor, target) * (CurrentCast != null && TheLastArk.Battle.RelicRules.IsEnemy(target)
            && actor.status.HasEquipment("ElementalStaff") ? ElementalAmplify : 1f);

    private static bool IsNegativeStatus(EffectType type)
        => type == EffectType.Stun || type == EffectType.Bleed || type == EffectType.Poison || type == EffectType.Burn
           || type == EffectType.Weakness || type == EffectType.Frailty || type == EffectType.Vulnerable || type == EffectType.Corrosion
           || type == EffectType.Blockade || type == EffectType.Fatigue || type == EffectType.Confusion || type == EffectType.Frost
           || type == EffectType.Fear || type == EffectType.Pressure || type == EffectType.Despair;

    /// <summary>요새의 반지: an enemy's negative status on the wearer is resisted 25% (first roll per type per attack is kept).</summary>
    private static bool ResistedByFortress(EffectType type, BattleCharacter actor, BattleCharacter target)
    {
        if (!IsNegativeStatus(type) || !TheLastArk.Battle.RelicRules.IsEnemy(actor) || !TheLastArk.Battle.RelicRules.IsAlly(target)
            || !target.status.HasEquipment("FortressRing")) return false;
        var attack = TheLastArk.Battle.AttackContext.Current;
        if (attack == null) return Random.value < FortressResistChance;
        if (!attack.ResistRolls.TryGetValue((target, type), out bool resisted))
            attack.ResistRolls[(target, type)] = resisted = Random.value < FortressResistChance;
        return resisted;
    }

    private static bool IsDebuffStat(EffectType type)
        => type == EffectType.Weakness || type == EffectType.Frailty || type == EffectType.Vulnerable || type == EffectType.Corrosion;

    /// <summary>서포터: an ally that actually received heal / shield / buff / dice bonus from a 서포터's active.</summary>
    private static void SupportRecipient(BattleCharacter actor, BattleCharacter target)
    {
        if (CurrentCast == null || actor?.status == null || target?.status?.origin == null || target.status.origin.isEnemy) return;
        if (!TheLastArk.Character.SynergyEffects.IsMember(actor.status, SynergyType.Support)) return;
        Object.FindObjectOfType<BattleManager>()?.GrantSupportMental(target);
    }

    /// <summary>Base stat for a scaled effect: explicit source, or ATK / SP by damage type.</summary>
    private static float StatFor(EffectEntry effect, BattleCharacter actor, bool usesSpellPower)
    {
        switch (effect.statSource)
        {
            case EffectStatSource.Attack: return actor.status.FinalAttack;
            case EffectStatSource.SpellPower: return actor.status.FinalSpellPower;
            case EffectStatSource.SelfShield: return CurrentCast?.SelfShield ?? actor.status.GetStatusValue(EffectType.Shield);
            case EffectStatSource.AllyShieldTotal: return CurrentCast?.AllyShieldTotal ?? 0f;
            default: return usesSpellPower ? actor.status.FinalSpellPower : actor.status.FinalAttack;
        }
    }

    /// <summary>Extra attack on each living neighbour of the target (each takes its own defense).</summary>
    private static void SplashAdjacent(EffectEntry effect, BattleCharacter actor, BattleCharacter target, float amount)
    {
        var bm = Object.FindObjectOfType<BattleManager>();
        if (bm == null || amount <= 0f || target?.status?.origin == null) return;
        var party = target.status.origin.isEnemy ? bm.enemyParty : bm.playerParty;
        int index = party.IndexOf(target);
        if (index < 0) return;
        foreach (int neighbour in new[] { index - 1, index + 1 })
        {
            if (neighbour < 0 || neighbour >= party.Count) continue;
            BattleCharacter side = party[neighbour];
            if (side?.status == null || side.status.currentHp <= 0f) continue;
            DamageResult result = side.ReceiveDamageDetailed(amount, actor, effect.damageType);
            if (CurrentCast != null) CurrentCast.HealthDamageDealt += result.HealthDamage;
        }
    }

    private static void RemoveOtherPartyTaunts(BattleCharacter target)
    {
        var bm = Object.FindObjectOfType<BattleManager>();
        if (bm == null || target?.status?.origin == null) return;
        var party = target.status.origin.isEnemy ? bm.enemyParty : bm.playerParty;
        if (party == null) return;

        foreach (var character in party)
        {
            if (character != null && character != target && character.status != null)
                character.status.activeStatusEffects.RemoveAll(effect => effect.effectType == EffectType.Taunt);
        }
    }

    private static void ApplyBleed(BattleCharacter target, float dmg, int turns, BattleCharacter actor, StatusDurationType durationType = StatusDurationType.Default)
    {
        target.status.ApplyStatusEffect(EffectType.Bleed, dmg, turns,
            0f, 0, -1, actor, durationType);

        // [피안개] 아군 스킬로 출혈 부여·갱신 후 출혈 6 이상이면 현재 출혈만큼 즉시 피해, 대상별 아군 턴당 1회
        if (TheLastArk.Battle.RelicRules.IsAlly(actor) && TheLastArk.Battle.RelicRules.Has(RelicEffectType.BloodMist))
        {
            float bleedValue = target.status.GetStatusValue(EffectType.Bleed);
            var bm = Object.FindObjectOfType<BattleManager>();
            if (bleedValue >= BattleManager.BloodMistMinimum && bm != null && bm.TryBloodMist(target))
                target.ReceiveDamage(TheLastArk.Battle.ValueModifier.Floor(bleedValue), null, DamageType.True, false);
        }
    }

    private static void ApplyPoison(BattleCharacter target, float dmg, int turns, BattleCharacter actor, StatusDurationType durationType = StatusDurationType.Default)
    {
        bool isActorAlly = actor != null && actor.status.origin != null && !actor.status.origin.isEnemy;

        // [맹독 버섯] 아군 스킬 독 부여량 +1, 같은 스킬·대상 1회
        if (isActorAlly && CurrentCast != null && TheLastArk.Battle.RelicRules.Has(RelicEffectType.PoisonMushroom) && CurrentCast.Once.Add((target, "mushroom")))
        {
            dmg += TheLastArk.Battle.RelicRules.Value(RelicEffectType.PoisonMushroom);
        }

        // [늪지의 액체] 중독된 적에게 독을 부여할 때, 무작위 적 1명에게 대상 독 수치의 20%만큼 독을 부여
        bool wasPoisoned = target.status.activeStatusEffects.Exists(e => e.effectType == EffectType.Poison);
        target.status.ApplyStatusEffect(EffectType.Poison, dmg, turns,
            0f, 0, -1, actor, durationType);

        // [늪지의 액체] 이미 중독된 적에게 아군 스킬로 독 → 부여 후 독 20%(버림)를 다른 무작위 적에게 3라운드 전파, 스킬당 1회 (전파 독은 재발동 없음)
        if (wasPoisoned && isActorAlly && CurrentCast != null && TheLastArk.Battle.RelicRules.Has(RelicEffectType.SwampLiquid) && CurrentCast.Once.Add((null, "swamp")))
        {
            var bm = Object.FindObjectOfType<BattleManager>();
            if (bm != null && bm.enemyParty != null)
            {
                List<BattleCharacter> otherEnemies = new List<BattleCharacter>();
                foreach (var enemy in bm.enemyParty)
                {
                    if (enemy != null && enemy != target && enemy.status != null && enemy.status.currentHp > 0)
                        otherEnemies.Add(enemy);
                }
                if (otherEnemies.Count > 0)
                {
                    var extraTarget = otherEnemies[Random.Range(0, otherEnemies.Count)];
                    float spreadPoison = Mathf.Floor(target.status.GetStatusValue(EffectType.Poison) * TheLastArk.Battle.RelicRules.Value(RelicEffectType.SwampLiquid));
                    if (spreadPoison >= 1f) extraTarget.status.ApplyStatusEffect(EffectType.Poison, spreadPoison, 3);
                    Debug.Log($"[늪지의 액체] {extraTarget.characterName}에게 독 {spreadPoison} 전이!");
                }
            }
        }
    }

    private static void ApplyBurn(BattleCharacter target, float dmg, int turns, BattleCharacter actor, StatusDurationType durationType = StatusDurationType.Default)
    {
        bool isActorAlly = actor != null && actor.status.origin != null && !actor.status.origin.isEnemy;

        // [불나방] 화상이 시전자에게도 부여됨. 적에게 부여하는 화상 수치가 100% 증가
        if (isActorAlly && TheLastArk.Battle.RelicRules.IsEnemy(target) && TheLastArk.Battle.RelicRules.Has(RelicEffectType.FireMoth))
        {
            dmg = Mathf.Floor(dmg * (1f + TheLastArk.Battle.RelicRules.Value(RelicEffectType.FireMoth)));
            if (CurrentCast == null || CurrentCast.Once.Add((actor, "firemoth")))
                actor.status.ApplyStatusEffect(EffectType.Burn, 3f, 3);
        }

        target.status.ApplyStatusEffect(EffectType.Burn, dmg, turns,
            0f, 0, -1, actor, durationType);

        // [화염 망치] 이번 턴에 화상을 3번 이상 같은 적에게 부여하면 즉시 화상이 발동(수치 미감소)
        // [화염 망치] 같은 적에게 아군 턴 동안 스킬 3회로 화상 부여 시 현재 화상 피해 즉시 1회 (대상별 턴당 1회, 기간 차감 없음)
        if (isActorAlly && CurrentCast != null && TheLastArk.Battle.RelicRules.Has(RelicEffectType.FlameHammer) && CurrentCast.Once.Add((target, "flamehammer")))
        {
            var bm = Object.FindObjectOfType<BattleManager>();
            if (bm != null && bm.RegisterBurnSkill(target))
                target.ReceiveDamage(Mathf.Floor(target.status.FinalMaxHp * target.status.GetStatusValue(EffectType.Burn) * 0.01f), null, DamageType.True, false);
        }
    }

    private static void CheckMageRuneOfCycle(BattleCharacter actor, BattleCharacter target)
    {
        // 순환의 룬 resolves once per skill in BattleManager (AppliedStatusToEnemy).
    }

    public static void RestoreRandomAllyMental(float amount)
    {
        var bm = Object.FindObjectOfType<BattleManager>();
        if (bm == null || bm.playerParty == null || bm.playerParty.Count == 0) return;

        List<BattleCharacter> aliveAllies = new List<BattleCharacter>();
        foreach (var ally in bm.playerParty)
        {
            if (ally != null && ally.status != null && ally.status.currentHp > 0)
            {
                aliveAllies.Add(ally);
            }
        }

        if (aliveAllies.Count > 0)
        {
            var target = aliveAllies[Random.Range(0, aliveAllies.Count)];
            target.ReceiveMentalHeal(amount);
            Debug.Log($"[정신력 회복] {target.characterName} 정신력 {amount} 회복! (현재: {target.status.currentMental})");
        }
    }
}
