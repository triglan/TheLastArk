using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Battle/Character")]
public class CharacterData : ScriptableObject
{
    // 캐릭터 공통 정보입니다. 적 여부가 켜져 있으면 적 데이터로 취급합니다.
    public string characterId;
    public string characterName;
    public string jobName;
    public List<TheLastArk.Data.SynergyType> synergies = new List<TheLastArk.Data.SynergyType>();
    public bool isEnemy = false;
    [Tooltip("캐릭터 카드의 같은 지역 보상 후보를 판정할 때 사용합니다.")]
    public string regionId = EnemyEncounterPool.DefaultRegionId;

    public string DataId => isEnemy ? characterName : GetFirstValidText(characterId, jobName, characterName);
    public string DisplayName => GetFirstValidText(characterName, jobName, characterId);
    public string DataName => isEnemy ? characterName : GetFirstValidText(jobName, characterName, characterId);

    // 아군은 초상화와 스탠딩 이미지를 모두 쓰고, 적은 스탠딩 이미지만 씁니다.
    public Sprite standingSprite;
    public Sprite portraitSprite;

    [Header("기본 능력치")]
    public float maxHp = 35f;
    public float maxMental = 30f;
    public float baseAttack = 5f;
    public float spellPower = 0f;
    public float armor = 3f;
    public float magicResist = 3f;
    public float critRate = 0f;

    [Header("아군 강화 배율 (0강:0%, 1강:20%, 2강:50%, 3강:100%, 4강:200%)")]
    public float[] levelStatMultipliers = new float[5] { 0f, 0.2f, 0.5f, 1.0f, 2.0f };

    [Header("아군 스킬")]
    public SkillInfo passiveSkill;
    public SkillInfo[] activeSkills = new SkillInfo[4];

    [Header("적 행동 패턴")]
    public List<EnemyPatternData> enemyPatterns = new List<EnemyPatternData>();

    public static string FormatCharacterId(int id)
    {
        return Mathf.Clamp(id, 0, 99).ToString("00");
    }

    public static bool IsValidCharacterId(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length != 2) return false;
        return int.TryParse(id, out int value) && value >= 0 && value <= 99;
    }

    public static string NormalizeCharacterId(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "";
        return int.TryParse(id.Trim(), out int value) ? FormatCharacterId(value) : id.Trim();
    }

    private static string GetFirstValidText(params string[] values)
    {
        foreach (string value in values)
        {
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
        }
        return "";
    }

    private void OnValidate()
    {
        if (isEnemy) return;

        characterId = NormalizeCharacterId(characterId);
        if (string.IsNullOrWhiteSpace(regionId)) regionId = EnemyEncounterPool.DefaultRegionId;
        else regionId = regionId.Trim();
        if (string.IsNullOrWhiteSpace(jobName))
            jobName = characterName;
    }
}

[System.Serializable]
public class SkillInfo
{
    // 스킬 이름, 아이콘, 기본 행동력 비용, 강화 단계별 효과를 담습니다.
    public string skillName;
    public Sprite skillIcon;
    public int baseCost = 3;
    public SkillLevelData[] levels = new SkillLevelData[3];
}

[System.Serializable]
public class SkillLevelData
{
    // 스킬의 한 단계가 어떤 대상을 공격하고 어떤 효과를 주는지 정합니다.
    public int overrideCost = -1;
    public TargetType targetType;
    public List<EffectEntry> effects = new List<EffectEntry>();
}

[System.Serializable]
public class EffectEntry
{
    // 하나의 효과입니다. 배율은 공격력에 곱하고, 고정값은 추가 수치나 턴 수로 씁니다.
    public EffectType type;
    public DamageType damageType = DamageType.Physical;
    public float multiplier = 1.0f;
    public float fixedValue = 0f;
    public bool useActualResult = false;
}

[System.Serializable]
public class EnemyPatternData
{
    // 적이 자기 턴마다 순서대로 실행할 행동 패턴입니다.
    public string patternName = "패턴";
    public TargetType targetType = TargetType.SingleEnemy;
    public List<EffectEntry> effects = new List<EffectEntry>();
}

public enum TargetType { SingleEnemy, LeftEnemy, RightEnemy, AdjacentEnemy, AllEnemy, Friendly, AllFriendly }
public enum EffectType { Damage, Heal, Buff, Stun, Bleed, Taunt, Counter, Shield, Resurrection }
public enum DamageType { Physical, Magical, True }
