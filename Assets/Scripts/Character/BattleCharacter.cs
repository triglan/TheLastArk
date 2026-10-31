using UnityEngine;
using System.Collections.Generic;
using TheLastArk.Battle;

public readonly struct DamageResult
{
    public float RequestedDamage { get; }
    public float AppliedDefense { get; }
    public float ResolvedDamage { get; }
    public float ShieldDamage { get; }
    public float HealthDamage { get; }
    public BattleCharacter FinalTarget { get; }
    public bool Killed { get; }

    public DamageResult(
        float requestedDamage,
        float appliedDefense,
        float resolvedDamage,
        float shieldDamage,
        float healthDamage,
        BattleCharacter finalTarget,
        bool killed)
    {
        RequestedDamage = requestedDamage;
        AppliedDefense = appliedDefense;
        ResolvedDamage = resolvedDamage;
        ShieldDamage = shieldDamage;
        HealthDamage = healthDamage;
        FinalTarget = finalTarget;
        Killed = killed;
    }
}

public class BattleCharacter : MonoBehaviour, IDamageable
{
    public CharacterStatus status;
    public CharacterView view;
    public bool isLeader;

    public string characterName => (status != null && status.origin != null) ? status.origin.characterName : gameObject.name;

    public void Init(CharacterData data, bool leaderStatus)
    {
        isLeader = leaderStatus;
        status = new CharacterStatus(data);
        DraftSkills();
        if (view == null) view = GetComponent<CharacterView>();
        if (view != null) view.UpdateVisual(status);
    }

    private void DraftSkills()
    {
        if (status?.origin == null || status.origin.isEnemy || status.origin.activeSkills == null) return;

        status.dynamicActiveSkill.Clear();

        List<int> selectedIndices = status.selectedActiveSkillIndices;
        if (selectedIndices == null || selectedIndices.Count < 2)
        {
            selectedIndices = new List<int>() { 0, 1 };
        }

        // 1. 선택된 2개의 액티브 스킬 탑재
        foreach (int idx in selectedIndices)
        {
            if (idx >= 0 && idx < status.origin.activeSkills.Length && status.origin.activeSkills[idx] != null)
            {
                status.dynamicActiveSkill.Add(status.origin.activeSkills[idx]);
            }
        }

        // 2. 리더인 경우: 권위자의 지팡이 보유 시 4개 전부 해금, 아니면 3번째 스킬 탑재
        if (isLeader)
        {
            bool allUnlocked = TheLastArk.Managers.ResourceManager.Instance != null &&
                               TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.LeaderAllSkillsUnlocked);
            if (allUnlocked)
            {
                foreach (var sk in status.origin.activeSkills)
                {
                    if (sk != null && !status.dynamicActiveSkill.Contains(sk))
                    {
                        status.dynamicActiveSkill.Add(sk);
                    }
                }
            }
            else
            {
                int extraIdx = status.EnsureLeaderExtraSkill();
                if (extraIdx >= 0 && extraIdx < status.origin.activeSkills.Length && status.origin.activeSkills[extraIdx] != null)
                {
                    if (!status.dynamicActiveSkill.Contains(status.origin.activeSkills[extraIdx]))
                    {
                        status.dynamicActiveSkill.Add(status.origin.activeSkills[extraIdx]);
                    }
                }
            }
        }

        // [끊임없는 전투] 유물: 알렉스 바스티온의 재정비 스킬이 끊임없는 전투 스킬로 변경
        bool hasEndlessBattle = TheLastArk.Managers.ResourceManager.Instance != null &&
                                TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.EndlessBattle);
        if (hasEndlessBattle)
        {
            for (int i = 0; i < status.dynamicActiveSkill.Count; i++)
            {
                var sk = status.dynamicActiveSkill[i];
                if (sk != null && !string.IsNullOrEmpty(sk.skillName) && sk.skillName.Contains("재정비"))
                {
                    status.dynamicActiveSkill[i] = CreateEndlessBattleSkill(sk);
                    Debug.Log($"[끊임없는 전투] {characterName}의 재정비 스킬이 끊임없는 전투로 변환되었습니다.");
                }
            }
        }
    }

    // 끊임없는 전투 per level (0 / +1 / +2): charge target and total 가르기 uses that turn.
    private static readonly int[] EndlessChargeTargets = { 10, 10, 8 };
    private static readonly int[] EndlessCleaveUses = { 2, 2, 3 };

    /// <summary>재정비 → 끊임없는 전투: same heal / shield, shorter charge, and 가르기 uses granted on fire.</summary>
    private SkillInfo CreateEndlessBattleSkill(SkillInfo baseSkill)
    {
        var endless = new SkillInfo
        {
            skillName = BattleManager.EndlessBattleSkillName,
            skillIcon = baseSkill?.skillIcon,
            baseCost = baseSkill?.baseCost ?? 0,
            levels = new SkillLevelData[3]
        };
        for (int l = 0; l < endless.levels.Length; l++)
        {
            SkillLevelData source = baseSkill?.levels != null && baseSkill.levels.Length > l ? baseSkill.levels[l] : null;
            endless.levels[l] = new SkillLevelData
            {
                overrideCost = source?.overrideCost ?? -1,
                targetType = source?.targetType ?? TargetType.Self,
                diceInput = new TheLastArk.Skills.DiceInputRule
                {
                    diceCount = source?.diceInput?.diceCount ?? 1,
                    condition = source?.diceInput?.condition ?? TheLastArk.Skills.DiceCondition.Any,
                    value = source?.diceInput?.value ?? 1,
                    maxValue = source?.diceInput?.maxValue ?? 6,
                    chargeTarget = EndlessChargeTargets[l]
                },
                usesPerTurn = 1,
                specialCount = EndlessCleaveUses[l],
                effects = source?.effects != null ? new List<EffectEntry>(source.effects) : new List<EffectEntry>()
            };
        }
        return endless;
    }

    [HideInInspector] public bool hasResurrectedThisStage = false;
    /// <summary>마법장 갑주 already negated a hit this battle.</summary>
    [HideInInspector] public bool magicShellUsed;
    public event System.Action<BattleCharacter> MentalDepleted;
    public event System.Action<BattleCharacter> PanicChanged;

    /// <summary>
    /// Shared panic for players and enemies: sanity 0 starts one of the six panics (uniform); real healing that lifts
    /// sanity above 0 ends it. While panicking, more sanity damage never stacks or rerolls.
    /// </summary>
    public void SynchronizePanic()
    {
        if (status?.origin == null || status.currentHp <= 0f) return;
        if (status.panic == null) status.panic = new TheLastArk.Battle.PanicState();
        if (status.currentMental <= 0f && !status.panic.IsActive)
        {
            status.panic.Begin(TheLastArk.Battle.PanicState.Roll());
            PanicChanged?.Invoke(this);
        }
        else if (status.currentMental > 0f && status.panic.IsActive)
        {
            status.panic.Clear();
            PanicChanged?.Invoke(this);
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // Debug only: allow repeatable panic tests without changing normal battle-end recovery.
    internal bool DebugSetPanic(TheLastArk.Battle.PanicType type)
    {
        if (status?.origin == null || status.origin.isEnemy || status.currentHp <= 0f
            || !gameObject.activeInHierarchy || (int)type < 0 || (int)type > TheLastArk.Battle.PanicState.TypeCount) return false;
        float mentalBefore = status.currentMental;
        if (status.panic == null) status.panic = new TheLastArk.Battle.PanicState();
        status.panic.Begin(type);
        status.currentMental = type == TheLastArk.Battle.PanicType.None ? status.FinalMaxMental : 0f;
        PanicChanged?.Invoke(this);
        view?.UpdateVisual(status);
        if (mentalBefore > 0f && status.currentMental <= 0f) MentalDepleted?.Invoke(this);
        return true;
    }
#endif
    public event System.Action<DamageResult> DamageResolved;

    public float ReceiveDamage(float amount, BattleCharacter attacker)
    {
        return ReceiveDamage(amount, attacker, DamageType.Physical);
    }

    public float ReceiveDamage(float amount, BattleCharacter attacker, DamageType damageType, bool triggerResponses = true)
    {
        return ReceiveDamageDetailed(amount, attacker, damageType, triggerResponses).HealthDamage;
    }

    public DamageResult ReceiveDamageDetailed(float amount, BattleCharacter attacker, DamageType damageType, bool triggerResponses = true)
    {
        return ReceiveDamageInternal(amount, attacker, damageType, triggerResponses, triggerResponses);
    }

    private DamageResult ReceiveDamageInternal(float amount, BattleCharacter attacker, DamageType damageType, bool allowRedirect, bool allowReactions)
    {
        if (status == null || amount <= 0f)
            return new DamageResult(amount, 0f, 0f, 0f, 0f, this, false);

        // Direct hits outside a skill still resolve responses, as a one-hit attack of their own.
        AttackContext context = allowReactions ? AttackContext.Current : null;
        bool ownContext = false;
        if (allowReactions && context == null && attacker != null && attacker != this)
        {
            context = AttackContext.Begin(attacker, this, TargetType.SingleEnemy);
            ownContext = true;
        }
        try
        {
            BattleCharacter receiver = this;
            bool redirected = false;
            if (allowRedirect && context != null)
            {
                // Order: 도발 target fixed → 보호.
                if (context.TauntAffected && this == context.PrimaryTarget)
                {
                    BattleCharacter taunter = context.LockedTaunter;
                    if (taunter == null && attacker?.status?.GetStatus(EffectType.Focus) == null) taunter = FindTaunter();
                    if (taunter != null && taunter.status != null && taunter.status.currentHp > 0f)
                    {
                        // One count per hit; once exhausted mid-attack the attack stays on the taunter.
                        if (taunter.status.GetStatus(EffectType.Taunt) != null) taunter.status.ConsumeStatusCharge(EffectType.Taunt);
                        context.LockedTaunter = taunter;
                        receiver = taunter;
                        redirected = true;
                        taunter.view?.UpdateVisual(taunter.status);
                    }
                }

                BattleCharacter protector = receiver.FindProtector(context);
                if (protector != null)
                {
                    float reduction = Mathf.Clamp01(context.GuardReduction(receiver));
                    return protector.ApplyHit(amount, attacker, damageType, reduction, allowReactions ? context : null, true);
                }
            }
            return receiver.ApplyHit(amount, attacker, damageType, 0f, allowReactions ? context : null, redirected);
        }
        finally
        {
            if (ownContext) context.End();
        }
    }

    /// <summary>보호: the living caster of this ally's protect takes direct hits. One count per attack skill;
    /// later hits of the same skill keep going to that protector. Works while the protector is stunned.</summary>
    private BattleCharacter FindProtector(AttackContext context)
    {
        BattleCharacter known = context.GetGuardian(this);
        if (known != null) return known.status != null && known.status.currentHp > 0f ? known : null;
        var guard = status.GetStatus(EffectType.Guard);
        if (guard == null || guard.source == null || guard.source == this || guard.source.status == null || guard.source.status.currentHp <= 0f)
            return null;
        // 수호자의 맹세: the protector's passive adds its own reduction (each decrease multiplies in turn).
        float reduction = guard.secondaryValue * 0.01f;
        EffectEntry oath = guard.source.status.GetPassiveEntry(PassiveTrait.GuardianOath);
        if (oath != null) reduction = 1f - (1f - reduction) * (1f - Mathf.Clamp01(oath.value * 0.01f));
        context.SetGuardian(this, guard.source, reduction);
        status.ConsumeStatusCharge(EffectType.Guard);
        view?.UpdateVisual(status);
        return guard.source;
    }

    /// <summary>Defense → protect reduction → floor → shield → HP. Responses are deferred to the attack's end.</summary>
    private DamageResult ApplyHit(float amount, BattleCharacter attacker, DamageType damageType, float protectReduction, AttackContext context, bool viaProtectOrTaunt = false)
    {
        float defense = damageType switch
        {
            DamageType.Physical => Mathf.Max(0f, status.FinalArmor),
            DamageType.Magical => Mathf.Max(0f, status.FinalMagicResist),
            _ => 0f
        };
        if (attacker?.status != null)
            defense *= Mathf.Clamp01(1f - attacker.status.GetStatusPercent(EffectType.Pierce));
        defense *= Mathf.Clamp01(1f - (AttackContext.Current?.ArmorIgnore ?? 0f));
        float damageAfterDefense = damageType == DamageType.True ? amount : Mathf.Max(0f, amount - defense);
        // 사냥 표식: allies' direct HP damage on a marked enemy is increased.
        float markBonus = context != null && attacker?.status?.origin != null && !attacker.status.origin.isEnemy
            ? status.GetStatusValue(EffectType.HuntMark) * 0.01f : 0f;
        var damageModifier = new ValueModifier().AddIncrease(markBonus).AddDecrease(protectReduction);
        if (status.panic != null && status.panic.Type == PanicType.Agitation) damageModifier.AddMultiplier(PanicState.AgitationDamageMultiplier);
        // 거석 방패 부동: below 20% HP right before the hit, physical damage −50% (protected hits included).
        if (damageType == DamageType.Physical && status.HasEquipment("MegalithShield") && status.currentHp < status.FinalMaxHp * 0.2f)
            damageModifier.AddDecrease(0.5f);
        // 수호자: team reduction, plus extra while protecting / taunting (applied before shields).
        if (context != null) TheLastArk.Character.SynergyEffects.AddGuardianReductions(status, viaProtectOrTaunt, damageModifier);
        float resolvedDamage = damageModifier.Apply(damageAfterDefense);
        // 마법장 갑주: the battle's first direct-attack HP damage hit is negated (after defense, before shields).
        if (context != null && attacker != null && resolvedDamage > 0f && !magicShellUsed && status.HasEquipment("AegisArmor"))
        {
            magicShellUsed = true;
            resolvedDamage = 0f;
        }
        float healthDamage = resolvedDamage;
        float shieldDamage = 0f;

        // [보호막 (Shield)] 상태이상 흡수 처리
        float totalShield = status.GetStatusValue(EffectType.Shield);
        if (totalShield > 0f && resolvedDamage > 0f)
        {
            float shieldMultiplier = 1f;
            // [마나분쇄자] 유물: 적 보호막에 주는 피해 +50%
            if (status.origin != null && status.origin.isEnemy && attacker?.status?.origin != null && !attacker.status.origin.isEnemy)
            {
                if (Application.isPlaying && TheLastArk.Managers.ResourceManager.Instance != null &&
                    TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.ManaCrusher))
                {
                    shieldMultiplier += TheLastArk.Managers.ResourceManager.Instance.GetRelicBonus(TheLastArk.Data.RelicEffectType.ManaCrusher);
                }
            }

            shieldDamage = Mathf.Min(totalShield, resolvedDamage * shieldMultiplier);
            healthDamage = Mathf.Max(0f, resolvedDamage - totalShield);

            float remainingShieldDamage = shieldDamage;
            for (int i = 0; i < status.activeStatusEffects.Count && remainingShieldDamage > 0f; i++)
            {
                ActiveStatusEffect shield = status.activeStatusEffects[i];
                if (shield == null || shield.effectType != EffectType.Shield || !shield.IsActive) continue;

                float absorbed = Mathf.Min(shield.damagePerTurn, remainingShieldDamage);
                shield.damagePerTurn -= absorbed;
                remainingShieldDamage -= absorbed;
                if (shield.damagePerTurn <= 0f)
                {
                    status.activeStatusEffects.RemoveAt(i);
                    i--;
                }
            }
        }

        float hpBefore = status.currentHp;
        status.currentHp = Mathf.Max(0f, status.currentHp - healthDamage);
        float appliedHealthDamage = hpBefore - status.currentHp;

        // [복수의 인장] 유물: 직접 공격으로 실제 체력 피해를 받은 아군 힘 1 (1턴, 진영 턴당 3회)
        if (appliedHealthDamage > 0f && context != null && attacker != null)
            (BattleManager.Active != null ? BattleManager.Active : FindObjectOfType<BattleManager>())?.TryVengeance(this);

        // 특성 [회생] / [배반자] 발동 검사 (체력 1 이하일 때 발동)
        if (appliedHealthDamage > 0f && status.currentHp <= 1f)
        {
            TryTriggerResurrectionTrait();
        }

        Debug.Log($"{gameObject.name} 피격! 남은 체력: {status.currentHp}");
        // 반격/반사 resolve once after every hit of the attack skill, even for 0 HP damage.
        if (context != null && attacker != null && attacker != this) context.RecordHit(this, damageType);

        if (view != null) view.UpdateVisual(status);

        if (status.currentHp <= 0f)
            GetComponent<EnemyBattleCharacter>()?.ClaimDefeatReward();

        var result = new DamageResult(
            amount,
            defense,
            resolvedDamage,
            shieldDamage,
            appliedHealthDamage,
            this,
            status.currentHp <= 0f);
        DamageResolved?.Invoke(result);
        return result;
    }

    /// <summary>
    /// After an attack skill: 반격 (own ATK, physical) first, then 반사 (attacker ATK × summed ratio, same damage type)
    /// if the attacker still lives. Each at most once per attack skill; needs this character alive and not stunned.
    /// </summary>
    public void ResolveAttackResponses(BattleCharacter attacker, DamageType receivedType)
    {
        if (attacker == null || attacker == this || status == null || attacker.status == null) return;
        if (status.currentHp <= 0f || status.GetStatus(EffectType.Stun) != null) return;

        var counter = status.GetStatus(EffectType.Counter);
        if (counter != null && attacker.status.currentHp > 0f)
        {
            // Own ATK; an authored counter value is added on top (0 in the default data).
            float counterDamage = status.FinalAttack + Mathf.Max(0f, counter.damagePerTurn);
            status.ConsumeStatusCharge(EffectType.Counter);
            attacker.ReceiveDamage(counterDamage, this, DamageType.Physical, false);
        }

        var reflect = status.GetStatus(EffectType.Reflect);
        if (reflect != null && attacker.status.currentHp > 0f)
        {
            float ratio = Mathf.Max(0f, status.GetStatusValue(EffectType.Reflect)) * 0.01f;
            status.ConsumeStatusCharge(EffectType.Reflect);
            float reflected = ValueModifier.Floor(attacker.status.FinalAttack * ratio);
            if (reflected > 0f) attacker.ReceiveDamage(reflected, this, receivedType, false);
        }
        view?.UpdateVisual(status);
    }

    // Ritual costs consume HP directly; shields, redirection and retaliation do not pay a cost.
    public float SpendHealth(float amount)
    {
        if (status == null) return 0f;
        float spent = Mathf.Min(status.currentHp, Mathf.Max(0f, amount));
        status.currentHp -= spent;
        view?.UpdateVisual(status);
        if (status.currentHp <= 0f) GetComponent<EnemyBattleCharacter>()?.ClaimDefeatReward();
        return spent;
    }

    private BattleCharacter FindTaunter()
    {
        var battleManager = FindObjectOfType<BattleManager>();
        if (battleManager == null || status?.origin == null) return null;
        var party = status.origin.isEnemy ? battleManager.enemyParty : battleManager.playerParty;
        return TargetResolver.FindTauntTarget(party);
    }

    public void ResolveActionStatusEffects()
    {
        if (status == null) return;
        float bleed = status.GetStatusValue(EffectType.Bleed);
        if (bleed > 0f) ReceiveDamage(ValueModifier.Floor(bleed * 0.5f), null, DamageType.True, false);

        float pressure = status.GetStatusValue(EffectType.Pressure);
        if (pressure > 0f) TakeMentalDamage(ValueModifier.Floor(pressure));
    }

    /// <param name="fromAlly">Sanity damage dealt by the player's side (파쇄 주문서 checks only these).</param>
    public float TakeMentalDamage(float amount, bool fromAlly = false)
    {
        if (status == null || amount <= 0f) return 0f;

        float mentalBefore = status.currentMental;
        float resolvedDamage = ValueModifier.Floor(amount);
        status.currentMental = Mathf.Max(0f, status.currentMental - resolvedDamage);

        // [파쇄 주문서] 유물: 정신 피해를 줄 때 대상의 정신력이 10% 미만이면 즉시 패닉
        // 아군 정신 피해 후 10% 미만이면 0 → 기존 패닉 진입 경로. 이미 0이거나 패닉 중이면 중복 없음.
        if (fromAlly && RelicRules.Has(TheLastArk.Data.RelicEffectType.ShatterScroll)
            && status.currentMental > 0f && status.currentMental < status.FinalMaxMental * 0.10f
            && (status.panic == null || !status.panic.IsActive))
        {
            status.currentMental = 0f;
            Debug.Log($"[파쇄 주문서] {characterName} 정신력 10% 미만 → 0");
        }

        float appliedDamage = mentalBefore - status.currentMental;
        SynchronizePanic();
        if (view != null) view.UpdateVisual(status);
        if (mentalBefore > 0f && status.currentMental <= 0f)
            MentalDepleted?.Invoke(this);

        return appliedDamage;
    }

    public float ReceiveMentalHeal(float amount)
    {
        if (status == null || amount <= 0f) return 0f;
        amount = new ValueModifier().AddIncrease(status.HasEquipment("SaintNecklace") ? SaintNecklaceBonus : 0f)
            .AddDecrease(status.GetStatusValue(EffectType.Despair) * 0.01f).Apply(amount);
        if (amount <= 0f) return 0f;
        float before = status.currentMental;
        status.currentMental = Mathf.Min(status.FinalMaxMental, status.currentMental + amount);
        SynchronizePanic();
        if (view != null) view.UpdateVisual(status);
        return status.currentMental - before;
    }

    public void TryTriggerResurrectionTrait()
    {
        if (status == null || status.origin == null) return;
        if (hasResurrectedThisStage) return;

        bool canResurrect = false;

        // 1. 본인이 회생 특성 보유 & 1강 이상(개방) 상태인 경우
        if (status.IsTraitUnlocked && status.origin.passiveSkill != null &&
            !string.IsNullOrEmpty(status.origin.passiveSkill.skillName) && status.origin.passiveSkill.skillName.Contains("회생"))
        {
            canResurrect = true;
        }
        // 2. 본인이 개방되지 않았더라도 파티원 중 4강(각성 개화) 회생 특성을 가진 아군이 있는 경우
        else
        {
            var bm = FindObjectOfType<BattleManager>();
            if (bm != null && bm.playerParty != null)
            {
                foreach (var ally in bm.playerParty)
                {
                    if (ally != null && ally.status != null && ally.status.IsTraitAwakened && ally.status.origin != null &&
                        ally.status.origin.passiveSkill != null && !string.IsNullOrEmpty(ally.status.origin.passiveSkill.skillName) &&
                        ally.status.origin.passiveSkill.skillName.Contains("회생"))
                    {
                        canResurrect = true;
                        break;
                    }
                }
            }
        }

        if (canResurrect && status.currentMental > 1f)
        {
            hasResurrectedThisStage = true;
            status.currentHp = 1f;

            float consumedMental = status.currentMental - 1f;
            status.currentMental = 1f;

            float healAmount = consumedMental * 2f; // 소모한 정신력의 200%만큼 체력 회복
            ReceiveHeal(healAmount, this);

            Debug.Log($"[{characterName}] 특성 [회생] 발동! 정신력 {consumedMental} 소모 -> 체력 {healAmount} 회복!");
        }
    }

    public void CastSlashOnRandomEnemy(int repeatCount = 1)
    {
        var bm = FindObjectOfType<BattleManager>();
        if (bm == null || bm.enemyParty == null || bm.enemyParty.Count == 0) return;

        SkillInfo slashSkill = null;
        if (status.origin != null && status.origin.activeSkills != null)
        {
            foreach (var sk in status.origin.activeSkills)
            {
                if (sk != null && !string.IsNullOrEmpty(sk.skillName) && sk.skillName.Contains("가르기"))
                {
                    slashSkill = sk;
                    break;
                }
            }
        }

        int skillIdx = status.SkillLevelIndex;
        SkillLevelData levelData = null;
        if (slashSkill != null && slashSkill.levels != null && slashSkill.levels.Length > 0)
        {
            levelData = slashSkill.levels[Mathf.Clamp(skillIdx, 0, slashSkill.levels.Length - 1)];
        }
        else
        {
            levelData = new SkillLevelData
            {
                targetType = TargetType.SingleEnemy,
                effects = new List<EffectEntry>
                {
                    new EffectEntry
                    {
                        type = EffectType.Damage,
                        damageType = DamageType.Physical,
                        multiplier = 1.5f
                    }
                }
            };
        }

        for (int r = 0; r < repeatCount; r++)
        {
            List<BattleCharacter> aliveEnemies = new List<BattleCharacter>();
            foreach (var e in bm.enemyParty)
            {
                if (e != null && e.status != null && e.status.currentHp > 0)
                    aliveEnemies.Add(e);
            }

            if (aliveEnemies.Count > 0)
            {
                BattleCharacter targetEnemy = aliveEnemies[Random.Range(0, aliveEnemies.Count)];
                EffectEngine.ProcessSkill(this, new List<BattleCharacter> { targetEnemy }, levelData);
                Debug.Log($"[{characterName}] 무작위 적 {targetEnemy.characterName}에게 [가르기] 시전! (반복 {r + 1}/{repeatCount})");
            }
        }
    }

    private const float SaintNecklaceBonus = 0.2f;

    public float ReceiveHeal(float amount, BattleCharacter healer) => ReceiveHeal(amount, healer, 0f);

    /// <summary>
    /// Grants a shield through the shared rule: received-shield bonuses (서포터), then floor. Default duration lasts
    /// until this side's next turn start. Returns the amount granted.
    /// </summary>
    public float GainShield(float amount, BattleCharacter source, int turns = 1, StatusDurationType duration = StatusDurationType.Default)
    {
        if (status == null || amount <= 0f) return 0f;
        float granted = new ValueModifier().AddIncrease(TheLastArk.Character.SynergyEffects.SupportReceiveBonus(status)).Apply(amount);
        if (granted <= 0f) return 0f;
        status.ApplyStatusEffect(EffectType.Shield, granted, Mathf.Max(1, turns), 0f, 0, -1, source, duration);
        view?.UpdateVisual(status);
        return granted;
    }

    /// <param name="healIncrease">Caller-side % increase (e.g. 기도실), summed with other increases.</param>
    public float ReceiveHeal(float amount, BattleCharacter healer, float healIncrease)
    {
        if (status == null || amount <= 0f) return 0f;
        var heal = new ValueModifier().AddIncrease(healIncrease).AddIncrease(TheLastArk.Character.SynergyEffects.SupportReceiveBonus(status));
        if (status.HasEquipment("SaintNecklace")) heal.AddIncrease(SaintNecklaceBonus);
        else if (healer != null && healer != this && healer.status != null && healer.status.HasEquipment("SaintNecklace")) heal.AddIncrease(SaintNecklaceBonus);

        // [회광반조] 유물: 잃은 체력에 비례해 치유량이 최대 50%까지 증가 (체력 10%에서 최대)
        // 회광반조: the target's and (for another ally's heal) the healer's lost-HP bonus, each up to +50%.
        if (RelicRules.Has(TheLastArk.Data.RelicEffectType.FlashOfTwilight) && RelicRules.IsAlly(this))
        {
            heal.AddIncrease(RelicRules.TwilightBonus(status));
            if (healer != null && healer != this && RelicRules.IsAlly(healer)) heal.AddIncrease(RelicRules.TwilightBonus(healer.status));
        }

        if (status.GetStatus(EffectType.Burn) != null) heal.AddDecrease(StatusRules.BurnHealDecrease);
        amount = heal.Apply(amount);
        if (amount <= 0f) return 0f;
        float beforeHp = status.currentHp;
        status.currentHp += amount;
        if (status.currentHp > status.FinalMaxHp)
            status.currentHp = status.FinalMaxHp;

        float actualHeal = status.currentHp - beforeHp;
        if (view != null) view.UpdateVisual(status);

        string healerName = healer != null ? healer.characterName : "System";
        Debug.Log($"{characterName}이(가) {healerName}에게 {amount}만큼 회복받음! (현재 HP: {status.currentHp})");
        return actualHeal;
    }

    [Header("Testing")]
    public CharacterData testData;

    public void PrepareCharacterData()
    {
        if (testData == null) return;
        Init(testData, isLeader);
    }

    public void ChangeLevel(int newLevel)
    {
        if (status == null || status.origin == null || status.origin.isEnemy) return;

        float oldMaxHp = status.FinalMaxHp;
        status.charLevel = Mathf.Clamp(newLevel, 0, 4);
        float newMaxHp = status.FinalMaxHp;

        float diff = newMaxHp - oldMaxHp;
        if (diff > 0) status.currentHp += diff;

        if (view != null) view.UpdateVisual(status);
        Debug.Log($"{characterName} 강화 단계 변경: {status.charLevel}강. 체력 {diff} 증가.");
    }

    [ContextMenu("Debug: Take 50 Damage")]
    public void DebugTakeDamage()
    {
        if (status == null) return;

        ReceiveDamage(50f, null, DamageType.True);
        Debug.Log($"{characterName}이(가) 테스트를 위해 자해했습니다. 현재 HP: {status.currentHp}");
    }
}
