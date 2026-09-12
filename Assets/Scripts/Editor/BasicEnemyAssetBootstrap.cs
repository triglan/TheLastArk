using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class BasicEnemyAssetBootstrap
{
    private const string Folder = "Assets/Resources/Characters/Enemy/Data";

    [MenuItem("Tools/Battle/Create Basic Six Enemies")]
    public static void CreateMissing()
    {
        bool created = false;
        foreach (CharacterData data in CreateDefinitions())
        {
            string path = $"{Folder}/{data.characterName}_Data.asset";
            if (AssetDatabase.LoadAssetAtPath<CharacterData>(path) != null)
            {
                Object.DestroyImmediate(data);
                continue;
            }

            AssetDatabase.CreateAsset(data, path);
            created = true;
        }

        if (!created) return;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Battle] Created the six basic enemy assets.");
    }

    private static IEnumerable<CharacterData> CreateDefinitions()
    {
        yield return CreateMerchant();
        yield return CreateRogue();
        yield return CreateDualBlade();
        yield return CreateScorpion();
        yield return CreateShieldSoldier();
        yield return CreateBellRinger();
    }

    private static CharacterData CreateMerchant()
    {
        CharacterData data = CreateEnemy("무장 상인", 12, 8, 0, 0,
            "제한 시간 안에 처치해야 하는 위험·보상형 적.");
        data.defeatGoldBonus = 10;
        data.returnStolenGoldOnDefeat = true;
        data.stolenGoldVariableId = "stolen_gold";
        data.enemyRuntimeVariables.Add(new EnemyRuntimeVariableDefinition
        {
            id = "stolen_gold", displayName = "탈취 골드", displayDescription = "처치 시 반환되는 탈취 골드", showInUi = false
        });
        data.enemyPatterns.Add(Pattern("골드 탈취", TargetType.Self,
            new EnemyPatternAction { actionType = EnemyPatternActionType.StealGold, runtimeVariableId = "stolen_gold", runtimeValue = 10 }));
        data.enemyPatterns.Add(Pattern("방어막", TargetType.Self, Effect(TargetType.Self, EffectType.Shield, fixedValue: 5f)));
        data.enemyPatterns.Add(Pattern("물리 공격", TargetType.SingleEnemy, Effect(TargetType.SingleEnemy, EffectType.Damage, fixedValue: 6f)));
        data.enemyPatterns.Add(Pattern("도주", TargetType.Self, new EnemyPatternAction { actionType = EnemyPatternActionType.Flee }));
        return data;
    }

    private static CharacterData CreateRogue()
    {
        CharacterData data = CreateEnemy("부랑자 도적", 15, 8, 0, 0,
            "직접 피해와 출혈을 함께 성장시키는 기본 근접형 적.");
        data.enemyRuntimeVariables.Add(Variable("bleed_bonus", "출혈 강화", "출혈 부여 수치", 0));
        data.enemyPatterns.Add(Pattern("물리 공격", TargetType.SingleEnemy, Effect(TargetType.SingleEnemy, EffectType.Damage, fixedValue: 5f)));
        data.enemyPatterns.Add(Pattern("출혈 공격", TargetType.SingleEnemy,
            Effect(TargetType.SingleEnemy, EffectType.Damage, fixedValue: 4f),
            Effect(TargetType.SingleEnemy, EffectType.Bleed, value: 1f, duration: 2, usePreparedTarget: true)));
        data.enemyPatterns.Add(Pattern("방어와 강화", TargetType.Self,
            Effect(TargetType.Self, EffectType.Shield, fixedValue: 4f),
            Effect(TargetType.Self, EffectType.Strength, value: 1f, durationType: StatusDurationType.Permanent),
            Modify("bleed_bonus", 1)));
        data.enemyPatterns[1].actions[1].valueVariableId = "bleed_bonus";
        return data;
    }

    private static CharacterData CreateDualBlade()
    {
        CharacterData data = CreateEnemy("쌍칼잡이", 10, 6, 0, 0,
            "매턴 2연타 피해가 증가하는 유리대포 적.");
        data.enemyPatterns.Add(Pattern("연속 베기", TargetType.SingleEnemy,
            Effect(TargetType.SingleEnemy, EffectType.Damage, fixedValue: 3f, hitCount: 2),
            Effect(TargetType.Self, EffectType.Strength, value: 1f, durationType: StatusDurationType.Permanent)));
        return data;
    }

    private static CharacterData CreateScorpion()
    {
        CharacterData data = CreateEnemy("사막 전갈", 12, 5, 0, 0,
            "직접 피해와 독을 함께 성장시키는 지속 피해형 적.");
        data.enemyRuntimeVariables.Add(Variable("poison_bonus", "독 강화", "독 부여 수치", 0));
        data.enemyPatterns.Add(Pattern("물리 공격", TargetType.SingleEnemy, Effect(TargetType.SingleEnemy, EffectType.Damage, fixedValue: 5f)));
        data.enemyPatterns.Add(Pattern("독침", TargetType.SingleEnemy,
            Effect(TargetType.SingleEnemy, EffectType.Damage, fixedValue: 4f),
            Effect(TargetType.SingleEnemy, EffectType.Poison, value: 2f, duration: 3, usePreparedTarget: true)));
        data.enemyPatterns.Add(Pattern("방어와 강화", TargetType.Self,
            Effect(TargetType.Self, EffectType.Shield, fixedValue: 5f),
            Effect(TargetType.Self, EffectType.Strength, value: 1f, durationType: StatusDurationType.Permanent),
            Modify("poison_bonus", 1)));
        data.enemyPatterns[1].actions[1].valueVariableId = "poison_bonus";
        return data;
    }

    private static CharacterData CreateShieldSoldier()
    {
        CharacterData data = CreateEnemy("흑철 방패병", 25, 12, 1, 0,
            "성장하는 방어막으로 다른 적을 지키는 저화력 탱커 적.");
        data.enemyRuntimeVariables.Add(Variable("shield_bonus", "보호막 강화", "이후 보호막 획득량", 0));
        data.enemyPatterns.Add(Pattern("도발과 방어", TargetType.Self,
            Effect(TargetType.Self, EffectType.Taunt, charges: 2),
            Effect(TargetType.Self, EffectType.Shield, fixedValue: 3f)));
        data.enemyPatterns.Add(Pattern("방패 강타", TargetType.SingleEnemy,
            Effect(TargetType.SingleEnemy, EffectType.Damage, fixedValue: 5f),
            Modify("shield_bonus", 1)));
        data.enemyPatterns[0].actions[1].valueVariableId = "shield_bonus";
        return data;
    }

    private static CharacterData CreateBellRinger()
    {
        CharacterData data = CreateEnemy("종말의 종지기", 10, 18, 0, 2,
            "단일 정신 공격 뒤 전체 정신 피해와 함께 성장하는 정신 공격형 적.");
        data.enemyRuntimeVariables.Add(Variable("mental_damage_bonus", "정신 피해 강화", "정신 피해량", 0));
        data.enemyPatterns.Add(Pattern("첫 번째 타종", TargetType.SingleEnemy,
            Effect(TargetType.SingleEnemy, EffectType.MentalDamage, fixedValue: 2f)));
        data.enemyPatterns.Add(Pattern("두 번째 타종", TargetType.SingleEnemy,
            Effect(TargetType.SingleEnemy, EffectType.MentalDamage, fixedValue: 2f)));
        data.enemyPatterns.Add(Pattern("전체 타종", TargetType.AllEnemy,
            Effect(TargetType.AllEnemy, EffectType.MentalDamage, fixedValue: 2f, valueVariableId: "mental_damage_bonus"),
            Modify("mental_damage_bonus", 1)));
        return data;
    }

    private static CharacterData CreateEnemy(string name, int hp, int mental, int armor, int magicResist, string role)
    {
        return new CharacterData
        {
            characterName = name,
            isEnemy = true,
            maxHp = hp,
            maxMental = mental,
            armor = armor,
            magicResist = magicResist,
            enemyRoleDescription = role,
            enemyPatterns = new List<EnemyPatternData>(),
            enemyRuntimeVariables = new List<EnemyRuntimeVariableDefinition>()
        };
    }

    private static EnemyRuntimeVariableDefinition Variable(string id, string name, string description, int initialValue)
    {
        return new EnemyRuntimeVariableDefinition
        {
            id = id,
            displayName = name,
            displayDescription = description,
            initialValue = initialValue,
            showInUi = true
        };
    }

    private static EnemyPatternData Pattern(string name, TargetType targetType, params EnemyPatternAction[] actions)
    {
        return new EnemyPatternData
        {
            patternName = name,
            targetType = targetType,
            targetSelection = EnemyTargetSelection.Random,
            actions = new List<EnemyPatternAction>(actions),
            effects = new List<EffectEntry>()
        };
    }

    private static EnemyPatternAction Effect(TargetType targetType, EffectType type, float fixedValue = 0f,
        float value = 0f, int duration = 3, int charges = 3, int hitCount = 1,
        StatusDurationType durationType = StatusDurationType.Default, string valueVariableId = "", bool usePreparedTarget = false)
    {
        return new EnemyPatternAction
        {
            actionType = EnemyPatternActionType.Effect,
            targetType = targetType,
            targetSelection = EnemyTargetSelection.Random,
            usePreparedTarget = usePreparedTarget,
            valueVariableId = valueVariableId,
            effect = new EffectEntry
            {
                type = type,
                damageType = DamageType.Physical,
                multiplier = 0f,
                fixedValue = fixedValue,
                value = value,
                duration = duration,
                charges = charges,
                hitCount = hitCount,
                durationType = durationType
            }
        };
    }

    private static EnemyPatternAction Modify(string id, int amount)
    {
        return new EnemyPatternAction
        {
            actionType = EnemyPatternActionType.ModifyRuntimeVariable,
            runtimeVariableId = id,
            runtimeValue = amount
        };
    }
}
