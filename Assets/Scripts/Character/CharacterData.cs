using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Battle/Character")]
public class CharacterData : ScriptableObject
{
    // 캐릭터 공통 정보입니다. 적 여부가 켜져 있으면 적 데이터로 취급합니다.
    public string characterId;
    public string characterName;
    public string jobName;
    [Tooltip("화면에 표시할 직업명. 비어 있으면 Job을 표시합니다.")]
    public string jobDisplayName;
    public List<TheLastArk.Data.SynergyType> synergies = new List<TheLastArk.Data.SynergyType>();
    public bool isEnemy = false;
    [Tooltip("캐릭터 카드의 같은 지역 보상 후보를 판정할 때 사용합니다.")]
    public string regionId = EnemyEncounterPool.DefaultRegionId;

    public string DataId => isEnemy ? characterName : GetFirstValidText(characterId, jobName, characterName);
    public string DisplayName => GetFirstValidText(characterName, jobName, characterId);
    public string JobDisplayName => GetFirstValidText(jobDisplayName, jobName, "방주 승무원");
    public string DataName => isEnemy ? characterName : GetFirstValidText(jobName, characterName, characterId);

    // 아군은 초상화와 스탠딩 이미지를 모두 쓰고, 적은 스탠딩 이미지만 씁니다.
    public Sprite standingSprite;
    public Sprite portraitSprite;
    [Tooltip("별도 초상화 대신 기본 일러스트의 얼굴 영역을 사용합니다.")]
    public bool useStandingPortrait = true;
    public Vector2 portraitFocus = new Vector2(.53f,.85f);
    [Range(1f,8f)] public float portraitZoom = 3f;
    [System.NonSerialized] private Sprite croppedPortrait, portraitSource;
    [System.NonSerialized] private Rect cachedPortraitRect;

    public Rect PortraitCropRect
    {
        get
        {
            if (standingSprite==null) return Rect.zero;
            Rect source=standingSprite.rect;
            float side=Mathf.Max(1,Mathf.Floor(Mathf.Min(source.width,source.height)/Mathf.Clamp(portraitZoom,1,8)));
            Vector2 center=source.min+Vector2.Scale(source.size,portraitFocus);
            return new Rect(Mathf.Floor(Mathf.Clamp(center.x-side*.5f,source.xMin,source.xMax-side)),
                Mathf.Floor(Mathf.Clamp(center.y-side*.5f,source.yMin,source.yMax-side)),side,side);
        }
    }
    public Sprite PortraitSprite
    {
        get
        {
            if (isEnemy || !useStandingPortrait || standingSprite==null) return portraitSprite!=null?portraitSprite:standingSprite;
            Rect crop=PortraitCropRect;
            if (croppedPortrait!=null && portraitSource==standingSprite && cachedPortraitRect==crop) return croppedPortrait;
            ReleasePortrait();
            croppedPortrait=Sprite.Create(standingSprite.texture,crop,Vector2.one*.5f,standingSprite.pixelsPerUnit,0,SpriteMeshType.FullRect);
            croppedPortrait.name=DataId+"_FacePortrait";croppedPortrait.hideFlags=HideFlags.HideAndDontSave;
            portraitSource=standingSprite;cachedPortraitRect=crop;
            return croppedPortrait;
        }
    }
    private void ReleasePortrait()
    {
        if (croppedPortrait!=null)
        {
            if (Application.isPlaying) Destroy(croppedPortrait);else DestroyImmediate(croppedPortrait);
        }
        croppedPortrait=null;portraitSource=null;
    }
    private void OnDisable(){ReleasePortrait();}
    public Sprite closedEyesSprite;
    [Tooltip("1번: 강박·발작 / 2번: 우울·무기력 / 3번: 동요 / 4번: 환각. 해당 그림이 없으면 기존 일러스트를 사용합니다.")]
    public List<Sprite> panicIllustrations = new List<Sprite>();

    public Sprite GetPanicIllustration(TheLastArk.Battle.PanicType type)
    {
        int index=type switch
        {
            TheLastArk.Battle.PanicType.Compulsion => 0,
            TheLastArk.Battle.PanicType.Seizure => 0,
            TheLastArk.Battle.PanicType.Depression => 1,
            TheLastArk.Battle.PanicType.Lethargy => 1,
            TheLastArk.Battle.PanicType.Agitation => 2,
            TheLastArk.Battle.PanicType.Hallucination => 3,
            _ => -1
        };
        if (index >= 0 && panicIllustrations != null && index < panicIllustrations.Count && panicIllustrations[index] != null)
            return panicIllustrations[index];
        return standingSprite != null ? standingSprite : portraitSprite;
    }

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
    HuntingMark,
    /// <summary>명사수의 눈: value = crit chance %p, secondaryValue = crit damage %p. Excess crit converts to attack %.</summary>
    MarksmanEye,
    /// <summary>태초의 마력: value = primordial mana gained after each actual skill use.</summary>
    PrimordialMana,
    /// <summary>철의 축전로: value = own-turn gain, charges = capacity, secondaryValue = shield absorption threshold.</summary>
    IronCapacitor
}

/// <summary>Skill mechanics that can't be expressed as plain effects.</summary>
public enum SkillSpecial
{
    None,
    /// <summary>Split one die into specialCount dice whose values the player chooses (same total).</summary>
    SplitDie,
    /// <summary>A second use this turn whose input value matches the first use counts as "matched".</summary>
    MatchSecondUse,
    /// <summary>마술의 땅: below 20 mana, shield; at 20 or more, spend 5 and attack all enemies.</summary>
    MagicLand,
    PreparedShot,
    ElectromagneticField
}

/// <summary>Stat a scaled effect multiplies.</summary>
public enum EffectStatSource { Auto, Attack, SpellPower, SelfShield, AllyShieldTotal, CurrentSelfShield }

/// <summary>Who an effect lands on, relative to the skill's targets.</summary>
public enum EffectTargetScope { SkillTargets, Self, AllAllies }

[System.Serializable]
public class SkillLevelData
{
    [TextArea(2, 5)] public string description;
    // 스킬의 한 단계가 어떤 대상을 공격하고 어떤 효과를 주는지 정합니다.
    public int overrideCost = -1;
    public TheLastArk.Skills.DiceInputRule diceInput = new TheLastArk.Skills.DiceInputRule();
    public TargetType targetType;
    public string customVfxName; // 커스텀 VFX 이름 (비어있을 시 효과 타입별 기본 VFX 자동 재생)
    public TheLastArk.Skills.SkillVfxSettings visualEffect = new TheLastArk.Skills.SkillVfxSettings();
    [Tooltip("같은 스킬을 한 턴에 사용할 수 있는 횟수")]
    [Min(1)] public int usesPerTurn = 1;
    [Tooltip("아군 대상 스킬에서 시전자 자신을 대상에서 제외")]
    public bool excludeSelf;
    [Tooltip("발동 시 시전자 보호막을 전부 소비 (소비 전 값이 S)")]
    public bool consumeSelfShield;
    public SkillSpecial special;
    [Tooltip("분할 개수 등 특수 규칙의 수치")]
    public int specialCount;
    [Tooltip("분할 후 보관함 개수 제한 (0 = 제한 없음)")] public int splitPoolLimit;
    [Tooltip("사용한 슬롯 복구에 필요한 마력 (0 = 복구 없음)")] public int primalManaRestoreCost;
    [Tooltip("필요 충전량에서 현재 축전을 차감")] public bool chargeReducedByCapacitor;
    [Tooltip("마지막 충전 투입의 효과 눈금을 발동에 사용")] public bool chargeUsesLastDie;
    [Header("원초 마력")]
    [Min(0)] public int primalManaRequired;
    [Min(0)] public int primalManaCost;
    public bool consumeAllPrimalMana;
    [Min(0)] public int primalManaGain;
    public bool primalManaGainFromDie;
    [Tooltip("효과 눈금 2 이하일 때 추가 획득")]
    [Min(0)] public int primalManaLowDieBonus;
    public bool primalManaGainBeforeEffects;
    [Tooltip("사용 직전 마력이 이 값 이상이면 마력을 소모해 최대 1회 추가 시전 (0 = 없음)")]
    [Min(0)] public int primalManaRepeatThreshold;
    [Min(0)] public int primalManaRepeatCost;
    [Tooltip("첫 발사와 추가 발사 사이 간격 (초)")]
    [Min(0)] public float primalManaRepeatDelay = .3f;
    [Tooltip("마술의 땅: 이 마력 이상에서 보호막과 공격 동시 발동 (0 = 없음)")]
    [Min(0)] public int primalManaDualEffectThreshold;
    [System.NonSerialized] public bool primalManaCostAfterShield;
    [Header("축전")]
    [Min(0)] public int capacitorGain;
    [Tooltip("이 수치 미만이면 소모 없이 기본 효과만 발동")]
    [Min(0)] public int capacitorSpendThreshold;
    [Min(0)] public int capacitorMaxSpend;
    public bool consumeAllCapacitor;
    [Range(0, 100)] public int capacitorRefundPercent;
    public List<EffectEntry> effects = new List<EffectEntry>();

    // Runtime variants never mutate the shared character asset.
    public SkillLevelData RuntimeCopy() => (SkillLevelData)MemberwiseClone();
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
    public TheLastArk.Skills.SkillVfxSettings visualEffect = new TheLastArk.Skills.SkillVfxSettings();

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
    [Tooltip("대상의 잃은 체력 중 피해에 더할 비율(%)")]
    public float targetMissingHealthPercent;
    [Tooltip("체력이 최대인 대상에게 피해 증가(%)")]
    public float fullHealthDamageBonusPercent;
    [Tooltip("체력이 절반 미만인 대상에게 피해 증가(%)")]
    public float belowHalfHealthDamageBonusPercent;
    [Tooltip("이번 피해만 방어력·마법 저항력 무시(%)")]
    public float armorIgnorePercent;
    [Tooltip("양옆 피해를 첫 대상의 실제 체력 피해 기준으로 계산")]
    public bool splashUsesActualDamage;
    [Tooltip("첫 대상의 실제 체력 피해 중 모든 적에게 추가로 줄 비율(%)")]
    public float splashAllPercent;
    [Header("원초 마력 계수")]
    public float primalManaScaling;
    public float spentPrimalManaScaling;
    [Min(0)] public int spentPrimalManaBonusThreshold;
    public float spentPrimalManaDamageBonusPercent;
    [Tooltip("주 대상을 제외한 모든 적에게 방어 적용 전 피해의 비율(%)")]
    public float splashOtherEnemiesPercent;
    public float loneTargetDamageBonusPercent;
    [Header("혼합 능력치 / 축전 계수")]
    public float additionalSpellPowerMultiplier;
    public float capacitorScaling;
    public float spentCapacitorScaling;
    public float damageBonusPercentPerSpentCapacitor;
    [Tooltip("이 효과에 필요한 이번 스킬의 축전 소모량 (0 = 항상)")]
    [Min(0)] public int minSpentCapacitor;
    [Min(0)] public int minCurrentCapacitor;
    public float belowQuarterHealthDamageBonusPercent;
    public float noAdjacentFinalDamageBonusPercent;
    public bool spentManaBonusIsFinalDamage;
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
    DiceBoost = 33,
    // 회피: charges + floor(D/valuePerDieDivisor), secondaryValue: 0 = HP damage, 1 = all damage, 2 = physical only.
    Evasion = 34
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
