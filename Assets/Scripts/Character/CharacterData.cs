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
    public Sprite closedEyesSprite;
    [Tooltip("패닉 발생 시 이 목록에서 무작위 일러스트를 표시합니다. 비어 있으면 기존 일러스트를 사용합니다.")]
    public List<Sprite> panicIllustrations = new List<Sprite>();

    [Header("적 일러스트 배치")]
    [SerializeField, Tooltip("Auto는 일러스트 종횡비로 판정합니다. 가로로 넓은 크리쳐는 별도의 두 줄 배치를 사용합니다.")]
    private TheLastArk.Character.EnemyFormationType enemyFormationType;
    public TheLastArk.Character.EnemyFormationType EnemyFormationType => enemyFormationType;
    // Retained only to preserve old serialized data; height now controls rendering.
    [SerializeField, HideInInspector] private float enemyIllustrationScale = 1f;
    [SerializeField, Min(1f)] private float enemyIllustrationHeight = 180f;
    [SerializeField] private Vector2 enemyIllustrationOffset;
    [SerializeField] private bool enemyIllustrationKeepFeet = true;
    public float EnemyIllustrationHeight => Mathf.Max(1f, enemyIllustrationHeight);
    public Vector2 EnemyIllustrationOffset => enemyIllustrationOffset;
    public bool EnemyIllustrationKeepFeet => enemyIllustrationKeepFeet;

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
    [TextArea(2, 4)]
    public string enemyRoleDescription;
    [Tooltip("레거시 데이터 보존용입니다. 전투에는 더 이상 사용하지 않습니다.")]
    [Min(0f)] public float damageBonusPerCycle;
    [Tooltip("레거시 데이터 보존용입니다. 전투에는 더 이상 사용하지 않습니다.")]
    [Min(0f)] public float bleedBonusPerCycle;
    public List<EnemyPatternData> enemyPatterns = new List<EnemyPatternData>();
    public List<EnemyRuntimeVariableDefinition> enemyRuntimeVariables = new List<EnemyRuntimeVariableDefinition>();
    public Sprite enemyEnhancementIcon;
    [Min(0)] public int defeatGoldBonus;
    public bool returnStolenGoldOnDefeat;
    public string stolenGoldVariableId;

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
        if (string.IsNullOrWhiteSpace(regionId)) regionId = EnemyEncounterPool.DefaultRegionId;
        else regionId = regionId.Trim();

        if (isEnemy)
        {
            damageBonusPerCycle = Mathf.Max(0f, damageBonusPerCycle);
            bleedBonusPerCycle = Mathf.Max(0f, bleedBonusPerCycle);
            return;
        }

        characterId = NormalizeCharacterId(characterId);
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
    [Tooltip("패시브 전용: 어떤 특성 규칙을 쓰는지. 수치는 levels[0](기본)·levels[1](개화)의 첫 효과 value/charges에 둡니다.")]
    public PassiveTrait passiveTrait;
    public SkillLevelData[] levels = new SkillLevelData[3];
}

/// <summary>Passive rules implemented in code; their numbers live in the passive's level data.</summary>
public enum PassiveTrait
{
    None,
    /// <summary>공방일체: value = % of HP damage as shield, charges = attacks per own turn (0 = every attack).</summary>
    OffenseDefense,
    /// <summary>수호자의 맹세: value = % damage reduction while protecting.</summary>
    GuardianOath,
    /// <summary>흑사: charges = actual attack fires per generated 0 die.</summary>
    BlackDeath,
    /// <summary>가자 댕댕아!: value = % direct HP damage increase on marked enemies, charges = marks kept.</summary>
    HuntingMark
}

/// <summary>Skill mechanics that can't be expressed as plain effects.</summary>
public enum SkillSpecial
{
    None,
    /// <summary>Split one die into specialCount dice whose values the player chooses (same total).</summary>
    SplitDie,
    /// <summary>A second use this turn whose input value matches the first use counts as "matched".</summary>
    MatchSecondUse
}

/// <summary>Stat a scaled effect multiplies.</summary>
public enum EffectStatSource { Auto, Attack, SpellPower, SelfShield, AllyShieldTotal }

/// <summary>Who an effect lands on, relative to the skill's targets.</summary>
public enum EffectTargetScope { SkillTargets, Self, AllAllies }

[System.Serializable]
public class SkillLevelData
{
    // 스킬의 한 단계가 어떤 대상을 공격하고 어떤 효과를 주는지 정합니다.
    public int overrideCost = -1;
    public TheLastArk.Skills.DiceInputRule diceInput = new TheLastArk.Skills.DiceInputRule();
    public TargetType targetType;
    public string customVfxName; // 커스텀 VFX 이름 (비어있을 시 효과 타입별 기본 VFX 자동 재생)
    [Tooltip("같은 스킬을 한 턴에 사용할 수 있는 횟수")]
    [Min(1)] public int usesPerTurn = 1;
    [Tooltip("아군 대상 스킬에서 시전자 자신을 대상에서 제외")]
    public bool excludeSelf;
    [Tooltip("발동 시 시전자 보호막을 전부 소비 (소비 전 값이 S)")]
    public bool consumeSelfShield;
    public SkillSpecial special;
    [Tooltip("분할 개수 등 특수 규칙의 수치")]
    public int specialCount;
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
    public float value = 0f;
    public float secondaryValue = 0f;
    [Min(1)] public int hitCount = 1;
    public int duration = 3;
    public int charges = 3;
    public StatusDurationType durationType = StatusDurationType.Default;
    public int skillSlot = -1;
    public Sprite statusIcon;
    public string customVfxName; // 개별 효과 커스텀 VFX 이름 (비어있을 시 효과 타입별 기본 VFX 자동 재생)

    [Header("주사위 계수 (D = 효과 눈금, 주사위 여러 개면 합)")]
    [Tooltip("배율에 D마다 더함: (multiplier + multiplierPerDie × D)")]
    public float multiplierPerDie;
    [Tooltip("고정값에 D마다 더함: fixedValue + fixedPerDie × D")]
    public float fixedPerDie;
    [Tooltip("0보다 크면 상태 수치에 ⌊D ÷ 이 값⌋을 더함")]
    [Min(0)] public int valuePerDieDivisor;
    [Tooltip("발동 확률(%) = chance + chancePerDie × D, 최대 100")]
    public float chance = 100f;
    public float chancePerDie;
    [Tooltip("D가 이 값 이상일 때만 적용 (0 = 항상)")]
    [Min(0)] public int minDie;
    public EffectStatSource statSource;
    public EffectTargetScope targetScope;
    [Tooltip("주 대상 양옆 적에게 표기 피해의 이 비율(%)로 추가 공격")]
    public float splashAdjacentPercent;
    [Tooltip("대상 출혈 1당 피해 증가(%) — 공격 직전 출혈 수치, 소비 안 함")]
    public float bonusPercentPerTargetBleed;
    [Tooltip("일치한 두 번째 사용일 때 타격 횟수 (0 = hitCount 그대로)")]
    [Min(0)] public int matchedHitCount;
    [Tooltip("일치한 두 번째 사용일 때 대상 방어력 무시 비율(%)")]
    public float matchedArmorIgnorePercent;
    [Tooltip("집중: 이번 스킬 동안만 유지")]
    public bool focusOnlyThisSkill;
}

[System.Serializable]
public class EnemyPatternData
{
    [Min(0)] public int forecastPatternOffset;
    // 적이 자기 턴마다 순서대로 실행할 행동 패턴입니다.
    public string patternName = "패턴";
    public TargetType targetType = TargetType.SingleEnemy;
    public EnemyTargetSelection targetSelection = EnemyTargetSelection.Random;
    public string customVfxName; // 패턴 커스텀 VFX 이름 (비어있을 시 효과 타입별 기본 VFX 자동 재생)
    public List<EffectEntry> effects = new List<EffectEntry>();
    public List<EnemyPatternAction> actions = new List<EnemyPatternAction>();
}

[System.Serializable]
public class EnemyRuntimeVariableDefinition
{
    public string id;
    public string displayName;
    [TextArea] public string displayDescription;
    public Sprite displayIcon;
    public int initialValue;
    public bool hasMaxValue;
    public int maxValue;
    public bool showInUi;
}

[System.Serializable]
public class EnemyPatternAction
{
    public EnemyPatternActionType actionType = EnemyPatternActionType.Effect;
    public Sprite intentIcon;
    public TargetType targetType = TargetType.SingleEnemy;
    public EnemyTargetSelection targetSelection = EnemyTargetSelection.Random;
    public bool usePreparedTarget;
    public bool excludeSelf;
    public bool uniqueSummon;
    public bool requireLastHit;
    public string valueMultiplierVariableId;
    public EffectEntry effect = new EffectEntry();
    [Tooltip("효과의 고정 수치에 더할 전투 변수 ID입니다.")]
    public string valueVariableId;
    [Tooltip("피해 또는 정신 피해의 타격 횟수에 더할 전투 변수 ID입니다.")]
    public string hitCountVariableId;
    public bool useHitCountVariableAsTotal;
    public string runtimeVariableId;
    public int runtimeValue;
    public bool setRuntimeValue;
    public string runtimeValueVariableId;
    [Header("특수 행동")]
    public string enemyName;
    public string[] enemyNames;
    [Min(0)] public int limit;
    [Min(0f)] public float percent;
    [Min(0f)] public float divisor = 1f;
    public int maxBonus;
    public string chargesVariableId;
    public string additionalValueVariableId;
    public string conditionVariableId;
    public EnemyRuntimeComparison condition = EnemyRuntimeComparison.Always;
    public int conditionValue;
}

public enum TargetType
{
    SingleEnemy,
    LeftEnemy,
    RightEnemy,
    AdjacentEnemy,
    AllEnemy,
    Friendly,
    AllFriendly,
    FriendlyLeft,
    FriendlyRight,
    FriendlyAdjacent,
    Self
}
public enum EnemyTargetSide { User, Enemy, Self }
public enum EnemyTargetSelection { Random, Leader, LowestHp, HighestHp, Remembered }
public enum EnemyTargetRange { Single, Left, Right, Adjacent, All }
public enum EnemyPatternActionType
{
    Effect, ModifyRuntimeVariable, RememberTarget, StealGold, Flee,
    BreakShield, RemoveStatus, TriggerStatus, Summon, SacrificeAllies,
    BeginDamageCheck, ResolveDamageCheck, SetArmor
}
public enum EnemyRuntimeComparison { Always, Less, LessOrEqual, Equal, GreaterOrEqual, Greater }
public enum EffectType
{
    Damage = 0, Heal = 1,
    Stun = 3, Bleed = 4, Taunt = 5, Counter = 6, Shield = 7, Resurrection = 8,
    Poison = 9, Burn = 10, Strength = 11, Blockade = 12, Fatigue = 13,
    Confusion = 14, Frost = 15, Fear = 16, Pressure = 17, Despair = 18,
    Weakness = 19, Protection = 20, Vulnerable = 21, Pierce = 22, Guard = 23,
    Focus = 24, MentalDamage = 25, MentalHeal = 26,
    Amplification = 27, Frailty = 28, MagicGuard = 29, Corrosion = 30,
    Reflect = 31,
    // 사냥 표식: 아군의 직접 체력 피해 증가 (value %)
    HuntMark = 32,
    // 다음 주사위 사용 시 눈금 보너스 (value = 보너스, charges = 적용 개수)
    DiceBoost = 33
}
public enum DamageType { Physical, Magical, True }
public enum StatusDurationType
{
    Default = 0,
    Turns = 1,
    Permanent = 2,
    Charges = 3,
    UntilOwnerPhase = 4,
    Marker = 5
}
