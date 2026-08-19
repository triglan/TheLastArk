using UnityEngine;
using System.Collections.Generic;
using TheLastArk.Data;
using TheLastArk.Managers;
using TheLastArk.UI;

public static class EffectEngine
{
    // 직전 효과가 실제로 처리한 값을 저장합니다.
    private static float _lastCalculatedValue = 0f;

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

    public static void ProcessSkill(BattleCharacter actor, List<BattleCharacter> targets, SkillLevelData data, string skillName = "")
    {
        // 스킬 또는 패턴의 효과를 대상들에게 순서대로 적용합니다.
        if (actor == null || actor.status == null || targets == null || data?.effects == null) return;

        _lastCalculatedValue = 0f;

        // 스킬 레벨에 지정된 커스텀 VFX가 있으면 대상들에게 재생
        if (!string.IsNullOrEmpty(data.customVfxName))
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
            foreach (BattleCharacter target in targets)
            {
                if (target == null || target.status == null) continue;
                ExecuteEffect(effect, actor, target, skillName);
            }
        }
    }

    private static void ExecuteEffect(EffectEntry effect, BattleCharacter actor, BattleCharacter target, string skillName = "")
    {
        // 1. VFX 재생: 커스텀 지정이 있으면 그것을 재생, 없으면 스킬명/효과 타입별 스마트 VFX 자동 재생
        if (!string.IsNullOrEmpty(effect.customVfxName))
        {
            VFXManager.Instance?.SpawnVFX(effect.customVfxName, target.transform.position);
        }
        else
        {
            VFXManager.Instance?.PlaySmartSkillEffect(skillName, effect.type, effect.damageType, target);
        }

        // 기본값은 시전자의 최종 공격력이고, 옵션에 따라 직전 결과를 다시 씁니다.
        float baseValue = effect.useActualResult
            ? _lastCalculatedValue
            : effect.type == EffectType.Damage && effect.damageType == DamageType.Magical
                ? actor.status.FinalSpellPower
                : actor.status.FinalAttack;
        float calculatedValue = (baseValue * effect.multiplier) + effect.fixedValue;

        switch (effect.type)
        {
            case EffectType.Damage:
                // 치명타 판정
                bool isCrit = Random.value < (actor.status.FinalCritRate * 0.01f);
                if (isCrit)
                {
                    float critMultiplier = 1.5f;
                    // [저격수의 눈] 사수 아군의 치명타 피해량 +25%
                    if (actor.status.HasSynergy(SynergyType.Ranger) &&
                        ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.SniperEye))
                    {
                        critMultiplier += 0.25f;
                    }
                    calculatedValue *= critMultiplier;
                    Debug.Log($"[치명타!] {actor.characterName}의 공격이 치명타로 적중! (배율: {critMultiplier}x)");

                    // [유리 칼날] 치명타 발동 시 대상에게 출혈 3 부여
                    if (actor.status.origin != null && !actor.status.origin.isEnemy &&
                        ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.GlassBlade))
                    {
                        ApplyBleed(target, 3f, 3, actor);
                    }
                }

                _lastCalculatedValue = target.ReceiveDamage(calculatedValue, actor, effect.damageType);

                // [광전사의 도끼] 전사 아군이 적에게 주는 체력 피해의 15%만큼 체력 회복
                if (actor.status.origin != null && !actor.status.origin.isEnemy &&
                    target.status.origin != null && target.status.origin.isEnemy &&
                    actor.status.HasSynergy(SynergyType.Warrior) &&
                    ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.BerserkerAxe))
                {
                    if (_lastCalculatedValue > 0)
                    {
                        float lifesteal = _lastCalculatedValue * 0.15f;
                        actor.ReceiveHeal(lifesteal, actor);
                        Debug.Log($"[광전사의 도끼] {actor.characterName} 체력 {lifesteal} 흡혈 회복!");
                    }
                }

                // [그림자 베일] 암살자 아군이 적을 처치하면 행동력 3 회복
                if (actor.status.origin != null && !actor.status.origin.isEnemy &&
                    target.status.origin != null && target.status.origin.isEnemy &&
                    target.status.currentHp <= 0 &&
                    actor.status.HasSynergy(SynergyType.Assassin) &&
                    ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.ShadowVeil))
                {
                    var bm = Object.FindObjectOfType<BattleManager>();
                    if (bm != null)
                    {
                        bm.currentAP = Mathf.Min(bm.MaxAP, bm.currentAP + 3);
                        bm.UpdateAPUI();
                        NotificationManager.Instance?.ShowMessage($"[그림자 베일] {actor.characterName} 적 처치! 행동력 +3 회복!", Color.cyan);
                    }
                }
                break;

            case EffectType.Heal:
                // [기도실] 주고 받는 치유량 증가 배율
                float prayerRoomBonus = TrainManager.IsInitialized ? TrainManager.Instance.GetPrayerRoomHealMultiplier() : 0f;
                float finalHealValue = calculatedValue * (1f + prayerRoomBonus);

                // [타락 모듈] 적 대상 치유 시 치유량의 50%만큼 마법 피해
                if (target != null && target.status != null && target.status.origin != null && target.status.origin.isEnemy)
                {
                    float corruptDmg = finalHealValue * 0.5f;
                    _lastCalculatedValue = target.ReceiveDamage(corruptDmg, actor, DamageType.Magical);
                    NotificationManager.Instance?.ShowMessage($"[타락 모듈] {target.characterName}에게 마법 피해 {corruptDmg:F0}!", Color.magenta);
                    break;
                }

                _lastCalculatedValue = target.ReceiveHeal(finalHealValue, actor);

                // [하늘 가호 프로토콜] 치유로 아군 체력 100% 달성 시 해당 턴 공/주 +20%
                if (TrainManager.IsInitialized && TrainManager.Instance.HasPartEffectInAnyCar(TrainPartEffectType.SkyBlessingProtocol))
                {
                    if (target != null && target.status != null && target.status.currentHp >= target.status.FinalMaxHp)
                    {
                        target.status.bonusAttack += target.status.FinalAttack * 0.20f;
                        if (target.view != null) target.view.UpdateVisual(target.status);
                        NotificationManager.Instance?.ShowMessage($"[하늘 가호] {target.characterName} 풀피 달성! 이번 턴 공/주 +20%!", Color.yellow);
                    }
                }

                // [빛의 분수대] 단일 치유 시 50%만큼 최저 체력 아군 치유
                if (TrainManager.IsInitialized && TrainManager.Instance.HasPartEffectInAnyCar(TrainPartEffectType.FountainOfLight))
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
                if (TrainManager.IsInitialized && TrainManager.Instance.HasPartEffectInAnyCar(TrainPartEffectType.LoavesAndFishesModule))
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

            case EffectType.Bleed:
                int bleedTurns = Mathf.Max(1, effect.duration > 0 ? effect.duration : 3);
                float bleedDmg = effect.value > 0 ? effect.value : baseValue * effect.multiplier;
                ApplyBleed(target, bleedDmg, bleedTurns, actor);
                CheckMageRuneOfCycle(actor, target);
                _lastCalculatedValue = bleedDmg;
                break;

            case EffectType.Poison:
                int poisonTurns = Mathf.Max(1, effect.duration > 0 ? effect.duration : 3);
                float poisonDmg = effect.value > 0 ? effect.value : baseValue * effect.multiplier;
                ApplyPoison(target, poisonDmg, poisonTurns, actor);
                CheckMageRuneOfCycle(actor, target);
                _lastCalculatedValue = poisonDmg;
                break;

            case EffectType.Burn:
                int burnTurns = Mathf.Max(1, effect.duration > 0 ? effect.duration : 3);
                float burnDmg = effect.value > 0 ? effect.value : effect.multiplier * 100f;
                ApplyBurn(target, burnDmg, burnTurns, actor);
                CheckMageRuneOfCycle(actor, target);
                _lastCalculatedValue = burnDmg;
                break;

            case EffectType.Stun:
                int stunTurns = Mathf.Max(1, effect.duration > 0 ? effect.duration : 3);
                target.status.ApplyStatusEffect(effect.type, 0f, stunTurns);
                CheckMageRuneOfCycle(actor, target);
                _lastCalculatedValue = 0f;
                break;

            case EffectType.Strength:
                float strength = effect.value > 0 ? effect.value : effect.multiplier * 100f;
                target.status.ApplyStatusEffect(effect.type, strength, Mathf.Max(1, effect.duration > 0 ? effect.duration : 3), 0f, 0, -1, actor);
                _lastCalculatedValue = strength;
                if (target.view != null) target.view.UpdateVisual(target.status);
                break;

            case EffectType.Taunt:
                int tauntCharges = Mathf.Max(1, effect.charges);
                target.status.ApplyStatusEffect(effect.type, 0f, 0, 0f, tauntCharges, -1, actor);
                _lastCalculatedValue = tauntCharges;
                break;

            case EffectType.Counter:
                int counterCharges = Mathf.Max(1, effect.charges);
                target.status.ApplyStatusEffect(effect.type, 0f, 0, 0f, counterCharges, -1, actor);
                _lastCalculatedValue = counterCharges;
                break;

            case EffectType.Shield:
                int shieldTurns = Mathf.Max(1, Mathf.RoundToInt(effect.fixedValue > 0 ? effect.fixedValue : 1));
                target.status.ApplyStatusEffect(effect.type, calculatedValue, shieldTurns);
                _lastCalculatedValue = calculatedValue;
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
                                ally.status.ApplyStatusEffect(effect.type, calculatedValue, shieldTurns);
                                if (ally.view != null) ally.view.UpdateVisual(ally.status);
                            }
                        }
                    }
                }
                break;

            case EffectType.Resurrection:
                target.TryTriggerResurrectionTrait();
                _lastCalculatedValue = 1f;
                break;

            case EffectType.Blockade:
            case EffectType.Fatigue:
            case EffectType.Confusion:
            case EffectType.Frost:
            case EffectType.Fear:
            case EffectType.Pressure:
            case EffectType.Despair:
            case EffectType.Weakness:
            case EffectType.Protection:
            case EffectType.Vulnerable:
            case EffectType.Pierce:
                target.status.ApplyStatusEffect(effect.type, effect.value, Mathf.Max(1, effect.duration > 0 ? effect.duration : 3), effect.secondaryValue, 0, effect.skillSlot, actor);
                _lastCalculatedValue = effect.value;
                break;

            case EffectType.Guard:
                target.status.ApplyStatusEffect(effect.type, 0f, 0, 0f, Mathf.Max(1, effect.charges), -1, actor);
                _lastCalculatedValue = effect.charges;
                break;
        }
    }

    private static void ApplyBleed(BattleCharacter target, float dmg, int turns, BattleCharacter actor)
    {
        target.status.ApplyStatusEffect(EffectType.Bleed, dmg, turns);

        // [피안개] 출혈이 10 이상 중첩된 적은 즉시 출혈이 1회 발동
        if (actor != null && actor.status.origin != null && !actor.status.origin.isEnemy &&
            ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.BloodMist))
        {
            var bleed = target.status.activeStatusEffects.Find(e => e.effectType == EffectType.Bleed);
            if (bleed != null && (bleed.damagePerTurn >= 10f || bleed.remainingTurns >= 10))
            {
                target.ReceiveDamage(bleed.damagePerTurn, null, DamageType.True);
                Debug.Log($"[피안개] {target.characterName} 출혈 10 이상 중첩으로 즉시 출혈 1회 발동!");
            }
        }
    }

    private static void ApplyPoison(BattleCharacter target, float dmg, int turns, BattleCharacter actor)
    {
        bool isActorAlly = actor != null && actor.status.origin != null && !actor.status.origin.isEnemy;

        // [맹독 버섯] 독을 부여할 때 추가로 +1 부여
        if (isActorAlly && ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.PoisonMushroom))
        {
            dmg += 1f;
        }

        // [늪지의 액체] 중독된 적에게 독을 부여할 때, 무작위 적 1명에게 대상 독 수치의 20%만큼 독을 부여
        bool wasPoisoned = target.status.activeStatusEffects.Exists(e => e.effectType == EffectType.Poison);
        target.status.ApplyStatusEffect(EffectType.Poison, dmg, turns);

        if (wasPoisoned && isActorAlly && ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.SwampLiquid))
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
                    float spreadPoison = Mathf.Max(1f, dmg * 0.20f);
                    extraTarget.status.ApplyStatusEffect(EffectType.Poison, spreadPoison, turns);
                    Debug.Log($"[늪지의 액체] {extraTarget.characterName}에게 독 {spreadPoison} 전이!");
                }
            }
        }
    }

    private static void ApplyBurn(BattleCharacter target, float dmg, int turns, BattleCharacter actor)
    {
        bool isActorAlly = actor != null && actor.status.origin != null && !actor.status.origin.isEnemy;

        // [불나방] 화상이 시전자에게도 부여됨. 적에게 부여하는 화상 수치가 100% 증가
        if (isActorAlly && ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.FireMoth))
        {
            dmg *= 2f;
            actor.status.ApplyStatusEffect(EffectType.Burn, dmg * 0.5f, turns);
            Debug.Log($"[불나방] 시전자 {actor.characterName}에게도 화상 부여! 적 화상 피해 100% 증가!");
        }

        target.status.ApplyStatusEffect(EffectType.Burn, dmg, turns);

        // [화염 망치] 이번 턴에 화상을 3번 이상 같은 적에게 부여하면 즉시 화상이 발동(수치 미감소)
        if (isActorAlly && ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.FlameHammer))
        {
            if (!_turnBurnCounts.ContainsKey(target)) _turnBurnCounts[target] = 0;
            _turnBurnCounts[target]++;

            if (_turnBurnCounts[target] >= 3)
            {
                target.ReceiveDamage(dmg, null, DamageType.True);
                Debug.Log($"[화염 망치] {target.characterName}에게 3회 이상 화상 부여로 즉시 화상 피해 발동!");
            }
        }
    }

    private static void CheckMageRuneOfCycle(BattleCharacter actor, BattleCharacter target)
    {
        if (actor != null && actor.status != null && actor.status.origin != null && !actor.status.origin.isEnemy &&
            target != null && target.status != null && target.status.origin != null && target.status.origin.isEnemy &&
            actor.status.HasSynergy(SynergyType.Mage) &&
            ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.RuneOfCycle))
        {
            RestoreRandomAllyMental(1f);
            Debug.Log($"[순환의 룬] 마술사 {actor.characterName}의 상태이상 부여로 아군 정신력 1 회복!");
        }
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
