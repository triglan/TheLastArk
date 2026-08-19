using UnityEngine;
using System.Collections.Generic;

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

        // [힐링 말랑이] 유물 / 지원가 4단계: 모든 아군 지원가의 스킬칸 +1
        bool hasHealingMallangi = TheLastArk.Managers.ResourceManager.Instance != null &&
                                  TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.HealingMallangi);
        if (status.HasSynergy(TheLastArk.Data.SynergyType.Support) && (hasHealingMallangi || TheLastArk.Character.SynergyCalculator.CalculateActiveSynergies().GetValueOrDefault(TheLastArk.Data.SynergyType.Support) >= 4))
        {
            for (int i = 0; i < status.origin.activeSkills.Length; i++)
            {
                var sk = status.origin.activeSkills[i];
                if (sk != null && !status.dynamicActiveSkill.Contains(sk))
                {
                    status.dynamicActiveSkill.Add(sk);
                    Debug.Log($"[힐링 말랑이] 지원가 {characterName} 스킬칸 +1 해금 ({sk.skillName})");
                    break;
                }
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

    private SkillInfo CreateEndlessBattleSkill(SkillInfo baseSkill)
    {
        SkillInfo endless = new SkillInfo
        {
            skillName = "끊임없는 전투",
            skillIcon = baseSkill != null ? baseSkill.skillIcon : null,
            baseCost = 2,
            levels = new SkillLevelData[3]
        };

        for (int l = 0; l < 3; l++)
        {
            endless.levels[l] = new SkillLevelData
            {
                overrideCost = 2,
                targetType = TargetType.Friendly,
                effects = new List<EffectEntry>
                {
                    new EffectEntry
                    {
                        type = EffectType.Strength,
                        damageType = DamageType.Physical,
                        value = 20f,
                        duration = 3
                    }
                }
            };
        }

        return endless;
    }

    [HideInInspector] public bool hasResurrectedThisStage = false;

    public float ReceiveDamage(float amount, BattleCharacter attacker)
    {
        return ReceiveDamage(amount, attacker, DamageType.Physical);
    }

    public float ReceiveDamage(float amount, BattleCharacter attacker, DamageType damageType, bool triggerResponses = true)
    {
        if (status == null || amount <= 0f) return 0f;

        if (triggerResponses)
        {
            var guard = status.GetStatus(EffectType.Guard);
            if (guard != null && guard.source != null && guard.source != this && guard.source.status != null && guard.source.status.currentHp > 0f)
            {
                ConsumeCharge(guard);
                return guard.source.ReceiveDamage(amount, attacker, damageType, false);
            }

            var taunter = FindTaunter();
            if (taunter != null && taunter != this)
            {
                var taunt = taunter.status.GetStatus(EffectType.Taunt);
                taunter.ConsumeCharge(taunt);
                return taunter.ReceiveDamage(amount, attacker, damageType, false);
            }
        }

        // [날카로운 못] 유물: 아군 공격자가 물리 피해를 입힐 때 +1 고정 피해
        if (damageType == DamageType.Physical && attacker != null && attacker.status != null && !attacker.status.origin.isEnemy)
        {
            if (TheLastArk.Managers.ResourceManager.Instance != null &&
                TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.SharpNail))
            {
                amount += TheLastArk.Managers.ResourceManager.Instance.GetRelicBonus(TheLastArk.Data.RelicEffectType.SharpNail);
            }
        }

        float defense = damageType switch
        {
            DamageType.Physical => Mathf.Max(0f, status.FinalArmor),
            DamageType.Magical => Mathf.Max(0f, status.FinalMagicResist),
            _ => 0f
        };
        if (attacker?.status != null)
            defense *= Mathf.Clamp01(1f - attacker.status.GetStatusPercent(EffectType.Pierce));
        float actualDamage = damageType == DamageType.True
            ? amount
            : Mathf.Max(1f, amount - defense);

        // [보호막 (Shield)] 상태이상 흡수 처리
        var shieldEffect = status.activeStatusEffects.Find(e => e.effectType == EffectType.Shield);
        if (shieldEffect != null && shieldEffect.damagePerTurn > 0)
        {
            float shieldDmg = actualDamage;
            // [마나분쇄자] 유물: 적 보호막에 주는 피해 +50%
            if (status.origin != null && status.origin.isEnemy && attacker != null && attacker.status != null && !attacker.status.origin.isEnemy)
            {
                if (TheLastArk.Managers.ResourceManager.Instance != null &&
                    TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.ManaCrusher))
                {
                    shieldDmg *= (1f + TheLastArk.Managers.ResourceManager.Instance.GetRelicBonus(TheLastArk.Data.RelicEffectType.ManaCrusher));
                }
            }

            if (shieldEffect.damagePerTurn >= shieldDmg)
            {
                shieldEffect.damagePerTurn -= shieldDmg;
                actualDamage = 0f;
            }
            else
            {
                float absorbed = shieldEffect.damagePerTurn;
                shieldEffect.damagePerTurn = 0f;
                actualDamage = Mathf.Max(0f, actualDamage - absorbed);
            }
        }

        status.currentHp -= actualDamage;

        // [복수의 인장] 유물: 아군이 체력 피해를 입을 때마다 힘(공격력) +1 획득
        if (actualDamage > 0 && status.origin != null && !status.origin.isEnemy)
        {
            if (TheLastArk.Managers.ResourceManager.Instance != null &&
                TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.SealOfVengeance))
            {
                status.bonusAttack += 1f;
                Debug.Log($"[복수의 인장] {characterName} 피격으로 힘(공격력) +1 획득!");
            }
        }

        // [정신 분열의 룬] 유물: 마법 피해의 10%만큼 대상 정신력에 추가 피해
        if (damageType == DamageType.Magical && attacker != null && attacker.status != null && !attacker.status.origin.isEnemy)
        {
            if (TheLastArk.Managers.ResourceManager.Instance != null &&
                TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.MindFractureRune))
            {
                float mentalDmg = actualDamage * 0.10f;
                TakeMentalDamage(mentalDmg);
            }
        }

        // 특성 [회생] / [배반자] 발동 검사 (체력 1 이하일 때 발동)
        if (status.currentHp <= 1f)
        {
            TryTriggerResurrectionTrait();
        }

        if (status.currentHp < 0) status.currentHp = 0;

        Debug.Log($"{gameObject.name} 피격! 남은 체력: {status.currentHp}");
        if (view != null) view.UpdateVisual(status);

        if (triggerResponses && attacker != null && attacker != this)
        {
            var counter = status.GetStatus(EffectType.Counter);
            if (counter != null)
            {
                ConsumeCharge(counter);
                attacker.ReceiveDamage(status.FinalAttack, this, DamageType.Physical, false);
            }
        }
        return actualDamage;
    }

    private BattleCharacter FindTaunter()
    {
        var bm = FindObjectOfType<BattleManager>();
        if (bm == null) return null;
        var party = status.origin != null && status.origin.isEnemy ? bm.enemyParty : bm.playerParty;
        return party?.Find(c => c != null && c != this && c.status != null && c.status.currentHp > 0f && c.status.GetStatus(EffectType.Taunt) != null);
    }

    private void ConsumeCharge(ActiveStatusEffect effect)
    {
        if (effect == null) return;
        effect.remainingCharges--;
        if (effect.remainingCharges <= 0) status.activeStatusEffects.Remove(effect);
    }

    public void ResolveActionStatusEffects()
    {
        var bleed = status?.GetStatus(EffectType.Bleed);
        if (bleed != null) ReceiveDamage(bleed.damagePerTurn * 0.5f, null, DamageType.True, false);

        var pressure = status?.GetStatus(EffectType.Pressure);
        if (pressure != null) TakeMentalDamage(pressure.damagePerTurn);
    }

    public void TakeMentalDamage(float amount)
    {
        if (status == null || amount <= 0f) return;
        status.currentMental -= amount;

        // [속삭임 교단] 유물: 전용 스킬 정신 피해가 체력에도 동일한 피해를 줌
        if (status.origin != null && status.origin.isEnemy &&
            TheLastArk.Managers.ResourceManager.Instance != null &&
            TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.WhisperCultRelic))
        {
            ReceiveDamage(amount, null, DamageType.True);
            Debug.Log($"[속삭임 교단] {characterName} 체력에도 동일한 피해 {amount} 부여!");
        }

        // [파쇄 주문서] 유물: 정신 피해를 줄 때 대상의 정신력이 10% 미만이면 즉시 패닉
        if (TheLastArk.Managers.ResourceManager.Instance != null &&
            TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.ShatterScroll))
        {
            if (status.currentMental < status.FinalMaxMental * 0.10f)
            {
                status.currentMental = 0f;
                Debug.Log($"[파쇄 주문서] {characterName} 정신력 10% 미만으로 즉시 패닉!");
            }
        }

        if (status.currentMental < 0) status.currentMental = 0;
        if (view != null) view.UpdateVisual(status);
    }

    public float ReceiveMentalHeal(float amount)
    {
        if (status == null || amount <= 0f) return 0f;
        var despair = status.GetStatus(EffectType.Despair);
        if (despair != null) amount *= Mathf.Clamp01(1f - despair.damagePerTurn * 0.01f);
        float before = status.currentMental;
        status.currentMental = Mathf.Min(status.FinalMaxMental, status.currentMental + amount);
        if (view != null) view.UpdateVisual(status);
        return status.currentMental - before;
    }

    public void TryTriggerResurrectionTrait()
    {
        if (status == null || status.origin == null) return;
        if (hasResurrectedThisStage) return;

        bool hasTraitorRelic = TheLastArk.Managers.ResourceManager.Instance != null &&
                               TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.Traitor);

        // [배반자] 유물: 회생 특성이 배반자 특성으로 변경
        if (hasTraitorRelic && (characterName.Contains("알렉스") || (status.origin.passiveSkill != null && !string.IsNullOrEmpty(status.origin.passiveSkill.skillName) && status.origin.passiveSkill.skillName.Contains("회생"))))
        {
            hasResurrectedThisStage = true;
            status.currentHp = 1f;

            var bm = FindObjectOfType<BattleManager>();
            float totalAbsorbed = 0f;

            if (bm != null && bm.playerParty != null)
            {
                foreach (var ally in bm.playerParty)
                {
                    if (ally != null && ally != this && ally.status != null && ally.status.currentHp > 0)
                    {
                        float absorb = Mathf.Max(0f, ally.status.currentMental * 0.5f);
                        ally.status.currentMental = Mathf.Max(1f, ally.status.currentMental - absorb);
                        if (ally.view != null) ally.view.UpdateVisual(ally.status);
                        totalAbsorbed += absorb;
                    }
                }
            }

            float alexMental = Mathf.Max(0f, status.currentMental - 1f);
            totalAbsorbed += alexMental;
            status.currentMental = 1f;

            float healAmount = Mathf.Max(status.FinalMaxHp * 0.4f, totalAbsorbed * 2f);
            ReceiveHeal(healAmount, this);

            // 부활 후 공격력 50% 증가
            status.bonusAttack += status.FinalAttack * 0.50f;
            Debug.Log($"[{characterName}] 특성 [배반자] 발동! 아군 정신력 흡수({totalAbsorbed:F1}) -> 체력 {healAmount:F1} 회복 및 공격력 +50%!");

            // (개화 / 4강) 부활 시 무작위 적에게 가르기를 시전합니다. 2번 반복합니다.
            if (status.IsTraitAwakened)
            {
                CastSlashOnRandomEnemy(2);
            }

            TheLastArk.UI.NotificationManager.Instance?.ShowMessage($"[{characterName}] 특성 [배반자] 발동! 부활 & 공격력 +50%!", Color.red);
            return;
        }

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

    public float ReceiveHeal(float amount, BattleCharacter healer)
    {
        if (status == null || amount <= 0f) return 0f;

        // [회광반조] 유물: 잃은 체력에 비례해 치유량이 최대 50%까지 증가 (체력 10%에서 최대)
        if (TheLastArk.Managers.ResourceManager.Instance != null &&
            TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.FlashOfTwilight))
        {
            float maxHp = Mathf.Max(1f, status.FinalMaxHp);
            float hpRatio = status.currentHp / maxHp;
            float lostRatio = Mathf.Clamp01((1f - hpRatio) / 0.9f); // 100% -> 0, 10% 이하 -> 1.0
            float healBonus = lostRatio * 0.50f;
            amount *= (1f + healBonus);
        }

        if (status.GetStatus(EffectType.Burn) != null) amount *= 0.5f;
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
