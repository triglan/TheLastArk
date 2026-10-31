using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI;
using UnityEngine;
using TheLastArk.UI;
using TheLastArk.Managers;
using TheLastArk.Battle;
using TheLastArk.Data;
using TheLastArk.Skills;
using TheLastArk.Character;

public partial class BattleManager : MonoBehaviour
{
    private const string SkillFirstTargetingKey = "Battle.SkillFirstTargeting";
    private const string AutoEndDiceTurnKey = "Battle.AutoEndDiceTurn";
    public const string DiceReasonUsed = "사용 완료";
    public const string DiceReasonNotEnoughDice = "주사위 부족";
    public const string DiceReasonConditionMismatch = "눈 조건 불충족";
    public const string DiceReasonStorageFull = "보관함 부족";

    public static bool AutoEndDiceTurn
    {
        get => PlayerPrefs.GetInt(AutoEndDiceTurnKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(AutoEndDiceTurnKey, value ? 1 : 0);
            PlayerPrefs.Save();
            FindObjectOfType<BattleManager>()?.TryAutoEndDiceTurn();
        }
    }

    public static bool SkillFirstTargeting
    {
        get => PlayerPrefs.GetInt(SkillFirstTargetingKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(SkillFirstTargetingKey, value ? 1 : 0);
            PlayerPrefs.Save();
            BattleManager manager = FindObjectOfType<BattleManager>();
            if (manager != null) manager.CancelPendingSelection();
        }
    }

    [Header("Config")]
    [SerializeField] private BattleConfig battleConfig;

    [Header("Party")]
    public List<BattleCharacter> playerParty;
    public List<BattleCharacter> enemyParty;

    [Header("References")]
    public TargetArrow targetHandler;

    [Header("Action Points")]
    public int currentAP;
    public TextMeshProUGUI apText;

    [Header("Turn UI")]
    [Tooltip("현재 턴 수를 표시합니다.")]
    public TextMeshProUGUI turnCountText;
    private TheLastArk.UI.BattleTurnClock _turnClock;
    [Tooltip("플레이어 턴을 종료하는 버튼입니다. 비워두면 TurnEndButton 이름으로 자동 검색합니다.")]
    public UnityEngine.UI.Button turnEndButton;

    private BattlePhase _currentPhase = BattlePhase.None;
    private bool _hasResurrectedThisBattle = false;
    private int _turnCount = 1;
    private int _lastTurnEndClickFrame = -1;
    private bool _hasUsedFirstSkillInBattle = false;
    private bool _turnEndButtonListenerBound;
    private int _carriedOverAP = 0;
    private int _nextTurnLockedAP = 0;
    private bool _overloadedThisTurn = false;
    private bool _canOverloadThisTurn = true;
    private int _skillsUsedThisBattle = 0;
    private readonly BattleSelectionState _selection = new BattleSelectionState();
    private readonly ActionDicePool _dicePool = new ActionDicePool();
    private readonly List<int> _selectedDiceIds = new List<int>();
    private SkillInfo _pendingDiceSkill;
    private BattleCharacter _pendingDiceActor;
    private readonly Dictionary<(BattleCharacter, SkillInfo), int> _diceSkillUses = new Dictionary<(BattleCharacter, SkillInfo), int>();
    private readonly Dictionary<(BattleCharacter, SkillInfo), int> _firstUseInput = new Dictionary<(BattleCharacter, SkillInfo), int>();
    private readonly HashSet<BattleCharacter> _offenseDefenseUsed = new HashSet<BattleCharacter>();
    private readonly Dictionary<BattleCharacter, int> _attackFires = new Dictionary<BattleCharacter, int>();
    private readonly Dictionary<BattleCharacter, List<BattleCharacter>> _huntMarks = new Dictionary<BattleCharacter, List<BattleCharacter>>();
    private SkillCast _pendingCast;
    private const int SkillExhausted = int.MaxValue / 2;
    private const int UnityBannerDice = 2;
    private readonly Dictionary<(BattleCharacter, SkillInfo), int> _diceCharge = new Dictionary<(BattleCharacter, SkillInfo), int>();
    private BattleDiceUI _diceUI;
    private OriginPartBalanceConfig _originBalance;
    private int _parasiticStacks;
    private int _manipulateUsesLeft;
    private int _rerollUsesLeft;

    private int _gambleRolledAP = -1;
    private int _limitRolledAP = -1;
    private int _clusterRolledAP = -1;
    private int _arcanaRolledAP = -1;
    private int _sinRolledAP = -1;
    public TheLastArk.Battle.ArcanaBattleState _arcanaState = new TheLastArk.Battle.ArcanaBattleState();
    public TheLastArk.Battle.SinActiveState _sinState = new TheLastArk.Battle.SinActiveState();
    private bool _limitFirstSkillFreeThisTurn = false;
    private bool _limitRetainAllSkillsThisTurn = false;

    public bool IsPlayerTurn => _currentPhase == BattlePhase.PlayerTurn && !DebugActionRunning
        && (_executingPanicSkill || (!_resolvingPanicTurn && (_panicPresentation == null || !_panicPresentation.IsPresenting)));
    public bool UsesDice => TheLastArk.Managers.TrainManager.IsInitialized
        && TheLastArk.Managers.TrainManager.Instance.nexusCar != null
        && TheLastArk.Data.NexusModuleDatabase.GetModule(TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId)?.moduleId
            == TheLastArk.Data.NexusModuleDatabase.OriginId;
    public BattleConfig Config => battleConfig;
    /// <summary>The running battle (set in Start, cleared on destroy).</summary>
    public static BattleManager Active { get; private set; }

    /// <summary>True from battle scene setup until the battle ends; train edits are blocked meanwhile.</summary>
    public bool IsBattleOngoing => _currentPhase != BattlePhase.BattleEnd;

    public bool CanUseDiceSkill(BattleCharacter actor, SkillInfo skill)
    {
        return GetDiceSkillUnavailableReason(actor, skill) == null;
    }

    public string GetDiceSkillUnavailableReason(BattleCharacter actor, SkillInfo skill)
    {
        string panicReason = GetPanicSkillUnavailableReason(actor, skill);
        if (panicReason != null) return panicReason;
        if (!UsesDice) return null;
        if (actor?.status == null || skill == null) return "사용 불가";
        if (actor.status.currentHp <= 0f) return "전투 불능";
        if (IsSkillUsedUp(actor, skill)) return DiceReasonUsed;
        if (actor.status.GetStatus(EffectType.Stun) != null) return "기절";
        if (actor.status.GetStatus(EffectType.Fatigue) != null && actor.status.lastUsedSkill == skill)
            return "피로";
        int slot = actor.status.dynamicActiveSkill.IndexOf(skill);
        if (actor.status.activeStatusEffects.Exists(e => e.effectType == EffectType.Blockade
            && e.IsActive && (e.skillSlot < 0 || e.skillSlot == slot))) return "봉쇄";

        DiceInputRule rule = GetDiceRule(skill, actor);
        if (rule.IsCharge && IsChargeReady(actor, skill)) return null;
        SkillLevelData skillLevel = GetSkillLevel(skill, actor);
        if (skillLevel != null && skillLevel.special == SkillSpecial.SplitDie
            && _dicePool.Dice.Count - 1 + Mathf.Max(1, skillLevel.specialCount) > ActionDicePool.MaxStoredDice)
            return DiceReasonStorageFull;
        int matching = 0;
        int available = 0;
        foreach (ActionDie die in _dicePool.Dice)
        {
            if (!die.IsAvailable) continue;
            available++;
            if (rule.AcceptsValue(GetDieInputValue(actor, die))) matching++;
        }
        int needed = Mathf.Max(1, rule.diceCount);
        if (available < needed) return DiceReasonNotEnoughDice;
        if (matching < needed) return DiceReasonConditionMismatch;
        return null;
    }

    public int GetDiceCharge(BattleCharacter actor, SkillInfo skill)
    {
        return _diceCharge.TryGetValue((actor, skill), out int charge) ? charge : 0;
    }

    /// <summary>Charge reached its target and waits for a manual fire. Persists across turns.</summary>
    public bool IsChargeReady(BattleCharacter actor, SkillInfo skill)
    {
        DiceInputRule rule = actor?.status != null ? GetDiceRule(skill, actor) : null;
        return rule != null && rule.IsCharge && GetDiceCharge(actor, skill) >= rule.chargeTarget;
    }

    /// <summary>냉기 on a player character (largest instance, floored). Enemies are never affected.</summary>
    public int GetFrost(BattleCharacter actor)
    {
        if (actor?.status?.origin == null || actor.status.origin.isEnemy) return 0;
        return Mathf.FloorToInt(actor.status.GetStatusValue(EffectType.Frost) + 1e-4f);
    }

    /// <summary>Value used for input conditions: original 0 stays 0, otherwise max(1, actual − 냉기). On-use bonus never applies.</summary>
    public int GetDieInputValue(BattleCharacter actor, ActionDie die)
    {
        if (die == null) return 0;
        int frost = GetFrost(actor);
        return die.Value == 0 || frost <= 0 ? die.Value : Mathf.Max(1, die.Value - frost);
    }

    /// <summary>Effect / sum-charge value: base (actual value, or face count for max-value effects) + on-use bonus − 냉기,
    /// floored at 0 for an original 0 and at 1 otherwise.</summary>
    public int GetDieEffectValue(BattleCharacter actor, ActionDie die, int onUseBonus, bool useMaxValue = false)
    {
        if (die == null) return 0;
        int raw = (useMaxValue ? die.Faces : die.Value) + onUseBonus + SynergyEffects.AssassinDiceBonus(actor?.status) - GetFrost(actor);
        if (PanicOf(actor) == PanicType.Depression) return Mathf.Max(0, raw) / 2;
        return Mathf.Max(die.Value == 0 ? 0 : 1, raw);
    }

    /// <summary>Held-die preview for a frosted character's slot: storage value → input value.</summary>
    public bool TryGetFrostPreview(int dieId, BattleCharacter actor, out int original, out int input)
    {
        ActionDie die = _dicePool.Find(dieId);
        original = die?.Value ?? 0;
        input = GetDieInputValue(actor, die);
        return die != null && GetFrost(actor) > 0;
    }

    /// <summary>Slot preview text while a die is held: 냉기 (input value) and 우울 (halved effect value).</summary>
    public string GetDiePreviewText(int dieId, BattleCharacter actor)
    {
        ActionDie die = _dicePool.Find(dieId);
        if (die == null || actor?.status == null) return null;
        if (PanicOf(actor) == PanicType.Depression)
            return $"우울  {die.Value} → {GetDieEffectValue(actor, die, GetDiceOnUseBonus(actor, null))}";
        return GetFrost(actor) > 0 ? $"냉기  {die.Value} → {GetDieInputValue(actor, die)}" : null;
    }

    private List<ActionDie> SelectedDice()
    {
        var dice = new List<ActionDie>(_selectedDiceIds.Count);
        foreach (int id in _selectedDiceIds) dice.Add(_dicePool.Find(id));
        return dice;
    }

    private int SumEffectValues(BattleCharacter actor, List<ActionDie> dice)
    {
        int total = 0;
        foreach (ActionDie die in dice) total += GetDieEffectValue(actor, die, ConsumeDiceBoost(actor));
        return total;
    }

    /// <summary>
    /// Character on-use dice bonus for the next die (저쪽을 공격해!). Applies to effect calc and sum-type charge,
    /// never to input conditions.
    /// </summary>
    public int GetDiceOnUseBonus(BattleCharacter actor, SkillInfo skill)
    {
        ActiveStatusEffect boost = actor?.status?.activeStatusEffects.Find(e => e != null && e.effectType == EffectType.DiceBoost
            && e.IsActive && e.remainingCharges > 0);
        return boost != null ? Mathf.FloorToInt(boost.damagePerTurn) : 0;
    }

    /// <summary>Uses one die's worth of the oldest on-use bonus; returns the bonus (0 when none).</summary>
    private int ConsumeDiceBoost(BattleCharacter actor)
    {
        ActiveStatusEffect boost = actor?.status?.activeStatusEffects.Find(e => e != null && e.effectType == EffectType.DiceBoost
            && e.IsActive && e.remainingCharges > 0);
        if (boost == null) return 0;
        boost.remainingCharges--;
        if (boost.remainingCharges <= 0) actor.status.activeStatusEffects.Remove(boost);
        return Mathf.FloorToInt(boost.damagePerTurn);
    }

    private SkillLevelData GetSkillLevel(SkillInfo skill, BattleCharacter actor)
    {
        if (skill?.levels == null || skill.levels.Length == 0 || actor?.status == null) return null;
        return skill.levels[Mathf.Clamp(actor.status.SkillLevelIndex, 0, skill.levels.Length - 1)];
    }

    private int UsesOf(BattleCharacter actor, SkillInfo skill) => _diceSkillUses.TryGetValue((actor, skill), out int uses) ? uses : 0;
    public int GetSkillUsesThisTurn(BattleCharacter actor, SkillInfo skill) => Mathf.Min(UsesOf(actor, skill), GetSkillUsesPerTurn(actor, skill));
    public int GetSkillUsesPerTurn(BattleCharacter actor, SkillInfo skill)
        => Mathf.Max(Mathf.Max(1, GetSkillLevel(skill, actor)?.usesPerTurn ?? 1), EndlessCleaveUses(actor, skill));
    private bool IsSkillUsedUp(BattleCharacter actor, SkillInfo skill) => UsesOf(actor, skill) >= GetSkillUsesPerTurn(actor, skill);
    private void MarkSkillUsed(BattleCharacter actor, SkillInfo skill) => _diceSkillUses[(actor, skill)] = UsesOf(actor, skill) + 1;

    /// <summary>
    /// Dice and shield inputs for one actual fire: per die the input value (냉기) and effect value
    /// (one on-use bonus consumed per die), S / T snapshots, and whether a 2nd use matches the 1st.
    /// </summary>
    private SkillCast BuildCast(BattleCharacter actor, SkillInfo skill, List<ActionDie> dice)
    {
        var cast = new SkillCast { SelfShield = actor.status.GetStatusValue(EffectType.Shield) };
        foreach (BattleCharacter ally in playerParty)
            if (ally?.status != null && ally.status.currentHp > 0f) cast.AllyShieldTotal += ally.status.GetStatusValue(EffectType.Shield);
        bool maxValue = actor == _arcadiaMaxActor;
        foreach (ActionDie die in dice)
        {
            cast.InputValues.Add(GetDieInputValue(actor, die));
            int effectValue = GetDieEffectValue(actor, die, ConsumeDiceBoost(actor), maxValue);
            cast.EffectValues.Add(effectValue);
            if (die.Kind == DieKind.North) cast.NorthValues.Add(effectValue);
        }
        SkillLevelData level = GetSkillLevel(skill, actor);
        cast.PhysicalDamageBonus = WarriorBonusFor(actor, level) + EndlessCleaveBonus(actor, skill) + GearPhysicalBonus(actor);
        cast.FirstDie = dice.Count > 0 ? dice[0] : null;
        ApplyTraitorBeforeAttack(actor, level, cast);
        if (level != null && level.special == SkillSpecial.MatchSecondUse && cast.InputValues.Count > 0)
        {
            if (_firstUseInput.TryGetValue((actor, skill), out int first)) cast.MatchedSecondUse = cast.InputValues[0] == first;
            else _firstUseInput[(actor, skill)] = cast.InputValues[0];
        }
        if (level != null && level.consumeSelfShield)
            actor.status.activeStatusEffects.RemoveAll(e => e != null && e.effectType == EffectType.Shield);
        return cast;
    }

    /// <summary>Self / all-target skills need no picked target.</summary>
    private BattleCharacter AutoTargetFor(SkillInfo skill, BattleCharacter actor)
    {
        SkillLevelData level = GetSkillLevel(skill, actor);
        if (level == null) return null;
        switch (level.targetType)
        {
            case TargetType.Self:
            case TargetType.AllFriendly:
                return actor;
            case TargetType.AllEnemy:
                return enemyParty.Find(e => e != null && e.status != null && e.status.currentHp > 0f);
            default:
                return null;
        }
    }

    /// <summary>
    /// [연운·안개] Splits one die into the player's chosen values (same actual total, each ≥ 0). The original's kept
    /// modifier moves to the chosen result die. Fails without consuming anything if storage would overflow.
    /// </summary>
    public bool TrySplitDie(BattleCharacter actor, SkillInfo skill, int dieId, IReadOnlyList<int> values, int modifierIndex)
    {
        if (!UsesDice || !IsPlayerTurn || actor?.status == null) return false;
        SkillLevelData level = GetSkillLevel(skill, actor);
        if (level == null || level.special != SkillSpecial.SplitDie || values == null || values.Count != Mathf.Max(2, level.specialCount)) return false;
        if (GetDiceSkillUnavailableReason(actor, skill) != null) return false;
        ActionDie die = _dicePool.Find(dieId);
        if (die == null || !die.IsAvailable || !GetDiceRule(skill, actor).AcceptsValue(GetDieInputValue(actor, die))) return false;
        if (!_dicePool.TrySplit(dieId, values, modifierIndex)) return false;
        MarkSkillUsed(actor, skill);
        CountActualSkillUse(actor);
        LockCompulsion(actor, skill);
        actor.status.lastUsedSkill = skill;
        actor.ResolveActionStatusEffects();
        _selectedDiceIds.Clear();
        RefreshDiceUI();
        RefreshSkillSlots();
        return true;
    }

    /// <summary>
    /// 사냥 표식 from a 가자 댕댕아! caster: keeps at most `charges` marks, replacing the oldest; marked enemies take
    /// `value`% more direct HP damage from allies.
    /// </summary>
    public void ApplyHuntMark(BattleCharacter caster, BattleCharacter target)
    {
        EffectEntry passive = caster?.status?.GetPassiveEntry(PassiveTrait.HuntingMark);
        if (passive == null || target?.status == null || target.status.currentHp <= 0f) return;
        if (!_huntMarks.TryGetValue(caster, out var marks)) _huntMarks[caster] = marks = new List<BattleCharacter>();
        marks.RemoveAll(m => m == null || m.status == null || m.status.currentHp <= 0f || m.status.GetStatus(EffectType.HuntMark) == null);
        if (marks.Contains(target)) return;
        while (marks.Count >= Mathf.Max(1, passive.charges))
        {
            BattleCharacter oldest = marks[0];
            marks.RemoveAt(0);
            oldest.status.activeStatusEffects.RemoveAll(e => e != null && e.effectType == EffectType.HuntMark && e.source == caster);
            oldest.view?.UpdateVisual(oldest.status);
        }
        target.status.ApplyStatusEffect(EffectType.HuntMark, passive.value, 0, 0f, 0, -1, caster, StatusDurationType.Marker);
        marks.Add(target);
        target.view?.UpdateVisual(target.status);
    }

    /// <summary>Ally turn start: each 가자 댕댕아! caster marks a random unmarked enemy.</summary>
    private void RefreshHuntMarks()
    {
        foreach (BattleCharacter ally in playerParty)
        {
            if (ally?.status == null || ally.status.currentHp <= 0f || ally.status.GetPassiveEntry(PassiveTrait.HuntingMark) == null) continue;
            _huntMarks.TryGetValue(ally, out var marks);
            var candidates = enemyParty.FindAll(e => e != null && e.status != null && e.status.currentHp > 0f && (marks == null || !marks.Contains(e)));
            if (candidates.Count > 0) ApplyHuntMark(ally, candidates[UnityEngine.Random.Range(0, candidates.Count)]);
        }
    }

    /// <summary>After an actual fire: 공방일체 shield and 흑사 die generation (attack skills only).</summary>
    private void AfterActualSkillFire(BattleCharacter actor, SkillInfo skill, SkillLevelData level, SkillCast cast)
    {
        LockCompulsion(actor, skill);
        AfterSynergySkillFire(actor, skill, level, cast);
        bool isAttack = level?.effects != null && level.special != SkillSpecial.SplitDie
            && level.effects.Exists(e => e != null && e.type == EffectType.Damage);
        if (!isAttack || actor?.status == null) return;

        EffectEntry offense = actor.status.GetPassiveEntry(PassiveTrait.OffenseDefense);
        if (offense != null && (offense.charges <= 0 || _offenseDefenseUsed.Add(actor)))
        {
            float shield = ValueModifier.Floor((cast?.HealthDamageDealt ?? 0f) * offense.value * 0.01f);
            if (shield > 0f) actor.status.ApplyStatusEffect(EffectType.Shield, shield, 1, 0f, 0, -1, actor);
        }

        EffectEntry blackDeath = actor.status.GetPassiveEntry(PassiveTrait.BlackDeath);
        if (blackDeath != null && UsesDice)
        {
            _attackFires.TryGetValue(actor, out int fires);
            _attackFires[actor] = ++fires;
            if (fires % Mathf.Max(1, blackDeath.charges) == 0) _dicePool.AddFixedDie(ActionDicePool.DefaultFaces, 0);
        }
        actor.view?.UpdateVisual(actor.status);
    }

    public void DropDieOnSkill(int dieId, SkillInfo skill, BattleCharacter actor)
    {
        if (!UsesDice || !IsPlayerTurn || skill == null || actor?.status == null || targetHandler == null) return;
        string unavailableReason = GetDiceSkillUnavailableReason(actor, skill);
        if (unavailableReason != null)
        {
            Notify($"스킬 사용 불가: {unavailableReason}", Color.red);
            return;
        }
        ActionDie die = _dicePool.Find(dieId);
        if (die == null || !die.IsAvailable) return;
        DiceInputRule dropRule = GetDiceRule(skill, actor);
        if (!dropRule.AcceptsValue(GetDieInputValue(actor, die))) return;
        if (dropRule.IsCharge)
        {
            if (IsChargeReady(actor, skill))
            {
                Notify("충전 완료 — 대상을 지정하고 스킬을 눌러 발동하세요.", Color.cyan);
                return;
            }
            if (_pendingDiceSkill != skill || _pendingDiceActor != actor)
                _selectedDiceIds.Clear();
            _pendingDiceSkill = skill;
            _pendingDiceActor = actor;
            if (_selectedDiceIds.Contains(dieId)) return;
            _selectedDiceIds.Add(dieId);
            if (_selectedDiceIds.Count >= Mathf.Max(1, dropRule.diceCount))
            {
                TryInsertChargeDice(actor, skill);
                _pendingDiceSkill = null;
                _pendingDiceActor = null;
            }
            RefreshDiceUI();
            return;
        }

        if (skill.levels == null || skill.levels.Length == 0) return;
        SkillLevelData level = skill.levels[Mathf.Clamp(actor.status.SkillLevelIndex, 0, skill.levels.Length - 1)];
        if (level != null && level.special == SkillSpecial.SplitDie)
        {
            EnsureDiceUI();
            _diceUI?.ShowSplitDialog(die, Mathf.Max(2, level.specialCount),
                (values, modifierIndex) => TrySplitDie(actor, skill, dieId, values, modifierIndex));
            return;
        }
        BattleCharacter target = AutoTargetFor(skill, actor) ?? (targetHandler.target != null
            ? targetHandler.target.GetComponent<BattleCharacter>() : null);
        if (target == null)
        {
            Notify("먼저 대상을 지정하세요.", Color.yellow);
            return;
        }
        if (level == null || (level.excludeSelf && target == actor) || !(level.targetType == TargetType.Self
            ? target == actor
            : TargetResolver.IsValid(target, level.targetType, playerParty, enemyParty, actor)))
        {
            Notify("이 스킬에 맞는 대상이 아닙니다.", Color.red);
            return;
        }

        if (_pendingDiceSkill != skill || _pendingDiceActor != actor)
            _selectedDiceIds.Clear();
        _pendingDiceSkill = skill;
        _pendingDiceActor = actor;
        if (_selectedDiceIds.Contains(dieId)) return;
        _selectedDiceIds.Add(dieId);
        DiceInputRule rule = GetDiceRule(skill, actor);
        if (_selectedDiceIds.Count < Mathf.Max(1, rule.diceCount))
        {
            RefreshDiceUI();
            return;
        }

        SelectSkill(skill, actor);
        _selection.Clear();
        _selectedDiceIds.Clear();
        _pendingDiceSkill = null;
        _pendingDiceActor = null;
        RefreshDiceUI();
    }

    /// <summary>Inserts the selected dice into a charge skill. Consumes dice only on a valid insert;
    /// overflow above the target is discarded. Charging alone is not a use.</summary>
    private bool TryInsertChargeDice(BattleCharacter actor, SkillInfo skill)
    {
        DiceInputRule rule = GetDiceRule(skill, actor);
        if (!rule.IsCharge || IsChargeReady(actor, skill) || IsSkillUsedUp(actor, skill)) return false;
        if (!TryGetSelectedDice(actor, rule, out List<int> values))
        {
            Notify($"주사위를 선택하세요: {rule.Describe()}", Color.yellow);
            return false;
        }
        List<ActionDie> inserted = SelectedDice();
        if (!_dicePool.TrySpend(_selectedDiceIds)) return false;
        // Sum-type charge uses effect values (on-use bonuses consumed per die); count-type counts dice only.
        int contribution = rule.chargeByDiceCount ? values.Count : SumEffectValues(actor, inserted);
        var northValues = new List<int>();
        foreach (ActionDie die in inserted)
            if (die.Kind == DieKind.North) northValues.Add(GetDieEffectValue(actor, die, 0));
        GrantNorthShields(actor, northValues);
        int nextCharge = Mathf.Min(rule.chargeTarget, GetDiceCharge(actor, skill) + contribution);
        _diceCharge[(actor, skill)] = nextCharge;
        _selectedDiceIds.Clear();
        RefreshDiceUI();
        RefreshSkillSlots();
        Notify(nextCharge >= rule.chargeTarget
            ? $"{skill.skillName} 준비 완료 — 대상을 지정해 발동하세요"
            : $"{skill.skillName} 충전 {nextCharge}/{rule.chargeTarget}", Color.cyan);
        TryAutoEndDiceTurn();
        return true;
    }

    /// <summary>Origin part numbers snapshotted at battle start; Inspector edits apply from the next battle.</summary>
    private OriginPartBalanceConfig BattleOriginBalance
    {
        get
        {
            if (_originBalance == null && TheLastArk.Managers.TrainManager.IsInitialized)
            {
                OriginPartBalanceConfig source = TheLastArk.Managers.TrainManager.Instance.OriginPartBalance;
                if (source != null) _originBalance = Instantiate(source);
            }
            return _originBalance;
        }
    }

    private int OriginPartLevel(TrainPartEffectType effect)
    {
        var car = TheLastArk.Managers.TrainManager.IsInitialized ? TheLastArk.Managers.TrainManager.Instance.nexusCar : null;
        return car != null ? car.GetEquippedPartLevel(effect) : -1;
    }

    public bool HasNumberManipulator => UsesDice && OriginPartLevel(TrainPartEffectType.NumberManipulator) >= 0;
    public bool HasChanceProtocol => UsesDice && OriginPartLevel(TrainPartEffectType.ChanceProtocol) >= 0;
    public int ManipulateUsesLeft => _manipulateUsesLeft;
    public int RerollUsesLeft => _rerollUsesLeft;
    public int ParasiticStacks => _parasiticStacks;

    /// <summary>기회 프로토콜 기본형 rerolls every available die; upgraded levels reroll the selected dice.</summary>
    public bool RerollTargetsAll
    {
        get
        {
            int level = OriginPartLevel(TrainPartEffectType.ChanceProtocol);
            return level >= 0 && BattleOriginBalance != null && OriginPartBalanceConfig.At(BattleOriginBalance.chanceRerollsAll, level);
        }
    }

    /// <summary>[숫자 조작 장치] ±1 on one available die. Invalid input consumes no use.</summary>
    public bool TryManipulateDie(int dieId, int delta)
    {
        if (!HasNumberManipulator || !IsPlayerTurn || _manipulateUsesLeft <= 0 || Mathf.Abs(delta) != 1) return false;
        if (!_dicePool.TryAdjust(dieId, delta))
        {
            Notify("조정할 수 없는 주사위입니다.", Color.red);
            return false;
        }
        _manipulateUsesLeft--;
        _selectedDiceIds.Clear();
        RefreshDiceUI();
        RefreshSkillSlots();
        return true;
    }

    /// <summary>[기회 프로토콜] One use rerolls all available dice (기본형) or the given dice together.</summary>
    public bool TryRerollDice(IReadOnlyList<int> dieIds)
    {
        if (!HasChanceProtocol || !IsPlayerTurn || _rerollUsesLeft <= 0) return false;
        bool rerolled = RerollTargetsAll ? _dicePool.TryRerollAll() : _dicePool.TryReroll(dieIds);
        if (!rerolled)
        {
            Notify("리롤할 주사위를 선택하세요.", Color.yellow);
            return false;
        }
        _rerollUsesLeft--;
        _selectedDiceIds.Clear();
        RefreshDiceUI();
        RefreshSkillSlots();
        return true;
    }

    /// <summary>
    /// Origin turn start: reset turn counters → update 기생 accumulation and count → roll the first batch →
    /// apply 고에너지 to one die of that batch.
    /// </summary>
    private void BeginOriginDiceTurn()
    {
        var car = TheLastArk.Managers.TrainManager.Instance.nexusCar;
        OriginPartBalanceConfig balance = BattleOriginBalance;
        int manipulator = car.GetEquippedPartLevel(TrainPartEffectType.NumberManipulator);
        int chance = car.GetEquippedPartLevel(TrainPartEffectType.ChanceProtocol);
        _manipulateUsesLeft = manipulator >= 0 && balance != null ? OriginPartBalanceConfig.At(balance.manipulatorUses, manipulator) : 0;
        _rerollUsesLeft = chance >= 0 && balance != null ? OriginPartBalanceConfig.At(balance.chanceUses, chance) : 0;

        int parasitic = car.GetEquippedPartLevel(TrainPartEffectType.ParasiticOrgan);
        if (parasitic >= 0 && balance != null)
            _parasiticStacks = Mathf.Min(OriginPartBalanceConfig.At(balance.parasiticStackCap, parasitic), _parasiticStacks + 1);

        int baseNormal = TheLastArk.Managers.TrainManager.OriginNormalDiceRequest(car, balance, _parasiticStacks);
        int archiumExtra = SynergyExtraNormalDice();
        int bannerDice = RelicRules.Has(RelicEffectType.UnityBanner) && SynergyEffects.ActiveSynergyCount() == 0 ? UnityBannerDice : 0;
        _dicePool.BeginTurn(baseNormal + archiumExtra + bannerDice, ActionDicePool.DefaultFaces, SynergySpecialDice(), TakeCarriedDice());
        ApplyArchiumTopBonus();
        int placedNormal = 0;
        foreach (ActionDie die in _dicePool.Dice) if (!die.IsCarried && !die.IsSpecial) placedNormal++;
        ApplyMagitech(Mathf.Clamp(placedNormal - baseNormal, 0, archiumExtra));

        int highEnergy = car.GetEquippedPartLevel(TrainPartEffectType.HighEnergyModule);
        var firstBatch = new List<ActionDie>();
        foreach (ActionDie die in _dicePool.Dice) if (!die.IsCarried) firstBatch.Add(die);
        if (highEnergy >= 0 && balance != null && firstBatch.Count > 0)
        {
            ActionDie target = firstBatch[_dicePool.PickIndex(firstBatch.Count)];
            _dicePool.AddModifier(target.Id, OriginPartBalanceConfig.At(balance.highEnergyBonus, highEnergy));
        }
    }

    public int RollAdditionalDice(int count, int sides = ActionDicePool.DefaultFaces)
    {
        if (!UsesDice || !IsPlayerTurn) return 0;
        int rolled = _dicePool.Roll(count, sides);
        if (rolled > 0)
        {
            _selectedDiceIds.RemoveAll(id => _dicePool.Find(id)?.IsAvailable != true);
            RefreshDiceUI();
            RefreshSkillSlots();
        }
        return rolled;
    }

    public void RefreshDiceBlockade()
    {
        if (!UsesDice || !IsPlayerTurn) return;
        RefreshSkillSlots();
    }

    private DiceInputRule GetDiceRule(SkillInfo skill, BattleCharacter actor)
    {
        if (skill?.levels == null || skill.levels.Length == 0) return new DiceInputRule();
        int index = Mathf.Clamp(actor.status.SkillLevelIndex, 0, skill.levels.Length - 1);
        return skill.levels[index]?.diceInput ?? new DiceInputRule();
    }

    private bool TryGetSelectedDice(BattleCharacter actor, DiceInputRule rule, out List<int> values)
    {
        values = new List<int>(_selectedDiceIds.Count);
        if (_selectedDiceIds.Count != Mathf.Max(1, rule.diceCount)) return false;
        foreach (int id in _selectedDiceIds)
        {
            ActionDie die = _dicePool.Find(id);
            if (die == null || !die.IsAvailable) return false;
            values.Add(GetDieInputValue(actor, die));
        }
        return rule.Accepts(values);
    }

    private void EnsureDiceUI()
    {
        if (_diceUI != null) return;
        GameObject canvas = GameObject.Find("TopBarCanvas");
        if (canvas == null) return;
        _diceUI = gameObject.GetComponent<BattleDiceUI>();
        if (_diceUI == null) _diceUI = gameObject.AddComponent<BattleDiceUI>();
        _diceUI.Initialize(this, canvas.transform);
    }

    private void RefreshDiceUI()
    {
        EnsureDiceUI();
        _diceUI?.Refresh(_dicePool, _selectedDiceIds, DicePreviewHint());
        UpdateAPUI();
    }

    private string DicePreviewHint()
    {
        if (_pendingDiceSkill == null || _pendingDiceActor == null) return null;
        int bonus = GetDiceOnUseBonus(_pendingDiceActor, _pendingDiceSkill);
        return bonus != 0 ? $"{_pendingDiceSkill.skillName} · 사용 시 {bonus:+0;-0}" : null;
    }

    private void RefreshSkillSlots()
    {
        FindObjectOfType<BattleSkillManager>()?.LinkSkillsToUI();
    }

    private bool TryCommitDiceSkill(BattleCharacter actor, SkillInfo skill)
    {
        if (!CanUseDiceSkill(actor, skill)) return false;
        DiceInputRule rule = GetDiceRule(skill, actor);
        if (rule.IsCharge)
        {
            if (!IsChargeReady(actor, skill)) return false;
            // Firing consumes no dice; afterwards the skill can't be charged or fired again this turn.
            _pendingCast = BuildCast(actor, skill, new List<ActionDie>());
            _diceCharge.Remove((actor, skill));
            _diceSkillUses[(actor, skill)] = SkillExhausted;
            _selectedDiceIds.Clear();
            RefreshDiceUI();
            RefreshSkillSlots();
            return true;
        }
        if (!TryGetSelectedDice(actor, rule, out List<int> values))
        {
            Notify($"주사위 조건을 확인하세요: {rule.Describe()}", Color.red);
            return false;
        }
        List<ActionDie> spent = SelectedDice();
        if (!_dicePool.TrySpend(_selectedDiceIds)) return false;
        _pendingCast = BuildCast(actor, skill, spent);
        MarkSkillUsed(actor, skill);
        _selectedDiceIds.Clear();
        RefreshDiceUI();
        RefreshSkillSlots();
        return true;
    }

    public int MaxAP
    {
        get
        {
            int baseAP = 4;
            if (TheLastArk.Managers.TrainManager.IsInitialized)
            {
                if (TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
                    TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.GambleId)
                {
                    baseAP = _gambleRolledAP > 0 ? _gambleRolledAP : TheLastArk.Managers.TrainManager.Instance.GetNexusBaseAP();
                }
                else if (TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
                    TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.LimitId)
                {
                    baseAP = _limitRolledAP > 0 ? _limitRolledAP : TheLastArk.Managers.TrainManager.Instance.GetNexusBaseAP();
                }
                else if (TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
                    TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.ClusterId)
                {
                    baseAP = _clusterRolledAP > 0 ? _clusterRolledAP : TheLastArk.Managers.TrainManager.Instance.GetNexusBaseAP();
                }
                else if (TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
                    TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.ArcanaId)
                {
                    baseAP = _arcanaRolledAP > 0 ? _arcanaRolledAP : TheLastArk.Managers.TrainManager.Instance.GetNexusBaseAP();
                }
                else if (TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
                    TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.SinId)
                {
                    baseAP = _sinRolledAP > 0 ? _sinRolledAP : TheLastArk.Managers.TrainManager.Instance.GetNexusBaseAP();
                }
                else
                {
                    baseAP = TheLastArk.Managers.TrainManager.Instance.GetNexusTurnAP(_turnCount);
                }
            }
            else if (battleConfig != null)
            {
                baseAP = battleConfig.MaxAP;
            }

            int relicAP = TheLastArk.Managers.ResourceManager.Instance != null ? (int)TheLastArk.Managers.ResourceManager.Instance.GetRelicBonus(TheLastArk.Data.RelicEffectType.BonusAP) : 0;
            int synergyAP = TheLastArk.Character.SynergyCalculator.GetTotalSynergyBonusAP();
            return baseAP + relicAP + synergyAP;
        }
    }

    private float EnemyActionDelay => battleConfig != null ? battleConfig.EnemyActionDelay : BattleConfig.DefaultEnemyActionDelay;
    private float StatusEffectDelay => battleConfig != null ? battleConfig.StatusEffectDelay : BattleConfig.DefaultStatusEffectDelay;
    private int VictoryGold => battleConfig != null ? battleConfig.VictoryGold : BattleConfig.DefaultVictoryGold;
    private string MapSceneName => battleConfig != null ? battleConfig.MapSceneName : BattleConfig.DefaultMapSceneName;

    private void Start()
    {
        Active = this;
        Debug.Log("[Battle] BattleManager.Start() - BeginBattleSetup()은 GameManager에서 호출되어야 합니다.");
    }

    public void InitializeAP()
    {
        Debug.Log("[Battle] InitializeAP() 호출 - 전투 시작.");
        if (UsesDice)
        {
            currentAP = 0;
        }
        else if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.GambleId)
        {
            TheLastArk.UI.GambleDiceUI.Instance.ShowDiceRoll(TheLastArk.Managers.TrainManager.Instance.nexusCar, (rolledAP) =>
            {
                _gambleRolledAP = rolledAP;
                _limitRolledAP = -1;
                _clusterRolledAP = -1;
                currentAP = MaxAP;
                UpdateAPUI();
            });
        }
        else if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.LimitId)
        {
            TheLastArk.UI.LimitCardUI.Instance.ShowCardDraw(TheLastArk.Managers.TrainManager.Instance.nexusCar, (rolledAP, freeSkill, retainAll) =>
            {
                _limitRolledAP = rolledAP;
                _gambleRolledAP = -1;
                _clusterRolledAP = -1;
                _limitFirstSkillFreeThisTurn = freeSkill;
                _limitRetainAllSkillsThisTurn = retainAll;
                currentAP = MaxAP;
                UpdateAPUI();
            });
        }
        else if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.ClusterId)
        {
            TheLastArk.UI.ClusterCardUI.Instance.ShowCardDraw(TheLastArk.Managers.TrainManager.Instance.nexusCar, (rolledAP, result) =>
            {
                _clusterRolledAP = rolledAP;
                _gambleRolledAP = -1;
                _limitRolledAP = -1;
                _arcanaRolledAP = -1;
                currentAP = MaxAP;
                UpdateAPUI();
                ApplyClusterSuitEffects(result);
            });
        }
        else if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.ArcanaId)
        {
            TheLastArk.UI.ArcanaCardUI.Instance.ShowTarotDraw(TheLastArk.Managers.TrainManager.Instance.nexusCar, _arcanaState, (rolledAP, result) =>
            {
                _arcanaRolledAP = rolledAP;
                _gambleRolledAP = -1;
                _limitRolledAP = -1;
                _clusterRolledAP = -1;
                _sinRolledAP = -1;
                currentAP = MaxAP;
                UpdateAPUI();
                ApplyArcanaCardEffects(result);
            });
        }
        else if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.SinId)
        {
            _sinState.ClearCurrentSin();
            var newSin = TheLastArk.Battle.SinModuleManager.DrawNextSin(TheLastArk.Managers.TrainManager.Instance.nexusCar, _sinState);
            _sinState.currentSin = newSin;
            _sinState.remainingTurns = 3;

            TheLastArk.UI.SinModuleUI.Instance.ShowSinManifestation(newSin, TheLastArk.Managers.TrainManager.Instance.nexusCar, _sinState, (rolledAP) =>
            {
                _sinRolledAP = rolledAP;
                _gambleRolledAP = -1;
                _limitRolledAP = -1;
                _clusterRolledAP = -1;
                _arcanaRolledAP = -1;
                currentAP = MaxAP;
                UpdateAPUI();
                ApplySinStartOfTurnEffects();
            });
        }
        else
        {
            _gambleRolledAP = -1;
            _limitRolledAP = -1;
            _clusterRolledAP = -1;
            _arcanaRolledAP = -1;
            _sinRolledAP = -1;
            _limitFirstSkillFreeThisTurn = false;
            _limitRetainAllSkillsThisTurn = false;
            currentAP = MaxAP;
            UpdateAPUI();
        }

        UpdateTurnUI();

        foreach (var enemy in enemyParty)
        {
            if (enemy == null) continue;
            var ai = enemy.GetComponent<EnemyAI>();
            if (ai == null) continue;

            ai.targetParty = playerParty;
            ai.allyParty = enemyParty;
        }

        EnsureTopResourceUI();

        _hasUsedFirstSkillInBattle = false;

        OnBattleStartRelics();
        OnBattleStartEquipment();

        // [적응형 보호막 생성기] 전투 시작 시 모든 아군이 잃은 체력의 20%만큼 1턴간 보호막 생성
        if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.HasPartEffectInAnyCar(TheLastArk.Data.TrainPartEffectType.AdaptiveShieldGenerator))
        {
            foreach (var ally in playerParty)
            {
                if (ally != null && ally.status != null && ally.status.currentHp > 0)
                {
                    float lostHp = Mathf.Max(0f, ally.status.FinalMaxHp - ally.status.currentHp);
                    if (lostHp > 0f)
                    {
                        float shieldAmount = lostHp * 0.20f;
                        ally.status.ApplyStatusEffect(EffectType.Shield, shieldAmount, 1, StatusDurationType.Turns);
                        Debug.Log($"[적응형 보호막 생성기] {ally.characterName}에게 잃은 체력 20% 보호막({shieldAmount:F0}) 생성");
                    }
                }
            }
        }

        if (targetHandler != null)
        {
            targetHandler.TargetSelected -= OnTargetSelected;
            targetHandler.TargetSelected += OnTargetSelected;
            targetHandler.TargetCanceled -= CancelPendingSelection;
            targetHandler.TargetCanceled += CancelPendingSelection;
        }
        InitializePanic();
        EnterPhase(BattlePhase.PlayerTurn);
    }

    private void OnDestroy()
    {
        if (Active == this) Active = null;
        ClearBattlePanic(false);
        if (_originBalance != null) DestroyImmediate(_originBalance);
        if (targetHandler == null) return;
        targetHandler.TargetSelected -= OnTargetSelected;
        targetHandler.TargetCanceled -= CancelPendingSelection;
    }

    private void EnsureTopResourceUI()
    {
        GameObject topBarCanvasObj = GameObject.Find("TopBarCanvas");
        if (topBarCanvasObj == null)
        {
            topBarCanvasObj = new GameObject("TopBarCanvas");

            Canvas canvas = topBarCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = topBarCanvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            topBarCanvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        {
            var resourceUI = gameObject.GetComponent<TheLastArk.UI.ExplorationResourceUI>();
            if (resourceUI == null)
                resourceUI = gameObject.AddComponent<TheLastArk.UI.ExplorationResourceUI>();

            resourceUI.Initialize(topBarCanvasObj.transform);
        }

        EnsureTurnCountUI(topBarCanvasObj.transform);
    }

    private void EnsureTurnCountUI(Transform topBarCanvasTransform)
    {
        if (_turnClock != null) return;
        if (turnCountText == null)
        {
            GameObject existingTurnObj = GameObject.Find("TurnCountText");
            if (existingTurnObj != null)
            {
                turnCountText = existingTurnObj.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject turnObj = new GameObject("TurnCountText");
                turnCountText = turnObj.AddComponent<TextMeshProUGUI>();
            }
        }

        if (turnCountText != null)
        {
            turnCountText.transform.SetParent(topBarCanvasTransform, false);
            turnCountText.transform.SetAsLastSibling();

            RectTransform rect = turnCountText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -16f);
            rect.sizeDelta = new Vector2(140f, 40f);

            turnCountText.fontSize = 24;
            turnCountText.raycastTarget = false;
            turnCountText.alignment = TextAlignmentOptions.Center;
            turnCountText.color = Color.white;
            turnCountText.fontStyle = FontStyles.Bold;

            TMPFontManager.ApplyFont(turnCountText);
            _turnClock = TheLastArk.UI.BattleTurnClock.Create(topBarCanvasTransform, turnCountText);
            _turnClock.SetTurn(_turnCount);
        }
    }

    private void EnterPhase(BattlePhase next)
    {
        Debug.Log($"[Battle] 페이즈 전환: {_currentPhase} -> {next}");
        _currentPhase = next;
        UpdateTurnEndButtonState();
        if (_diceUI != null) _diceUI.Refresh(_dicePool, _selectedDiceIds, DicePreviewHint());

        switch (next)
        {
            case BattlePhase.PlayerTurn:
                Debug.Log("[Battle] OnEnterPlayerTurn() 호출");
                OnEnterPlayerTurn();
                break;
            case BattlePhase.EnemyTurn:
                Debug.Log("[Battle] RunEnemyTurn() 코루틴 시작");
                StartCoroutine(RunEnemyTurn());
                break;
            case BattlePhase.StatusEffect:
                Debug.Log("[Battle] RunStatusEffectPhase() 코루틴 시작");
                StartCoroutine(RunStatusEffectPhase());
                break;
            case BattlePhase.TurnEnd:
                Debug.Log("[Battle] OnEnterTurnEnd() 호출");
                OnEnterTurnEnd();
                break;
            case BattlePhase.BattleEnd:
                Debug.Log("[Battle] OnEnterBattleEnd() 호출");
                OnEnterBattleEnd();
                break;
        }
    }

    public void SelectSkill(SkillInfo skill, BattleCharacter actor)
    {
        if (!IsPlayerTurn) return;
        string panicReason = GetPanicSkillUnavailableReason(actor, skill);
        if (panicReason != null)
        {
            Notify(panicReason, Color.red);
            return;
        }

        if (UsesDice)
        {
            if (actor.status.GetStatus(EffectType.Stun) != null)
            {
                Notify("[기절] 행동할 수 없습니다.", Color.red);
                return;
            }
            if (actor.status.GetStatus(EffectType.Fatigue) != null && actor.status.lastUsedSkill == skill)
            {
                Notify("[피로] 같은 스킬을 연속으로 사용할 수 없습니다.", Color.red);
                return;
            }
            string diceReason = GetDiceSkillUnavailableReason(actor, skill);
            if (diceReason != null)
            {
                Notify($"스킬 사용 불가: {diceReason}", Color.red);
                return;
            }
            DiceInputRule rule = GetDiceRule(skill, actor);
            if (rule.IsCharge && !IsChargeReady(actor, skill))
            {
                TryInsertChargeDice(actor, skill);
                return;
            }
            if (rule.IsCharge)
            {
                // Ready charge: the user picks a target and fires manually.
                if (!_executingPanicSkill && AutoTargetFor(skill, actor) == null && (targetHandler == null || targetHandler.target == null))
                {
                    Notify("먼저 대상을 지정한 뒤 발동하세요.", Color.yellow);
                    return;
                }
            }
            else if (!TryGetSelectedDice(actor, rule, out _))
            {
                Notify($"주사위를 선택하세요: {rule.Describe()}", Color.yellow);
                return;
            }
        }

        if (SkillFirstTargeting && _selection.Skill == skill && _selection.Actor == actor)
        {
            CancelPendingSelection();
            return;
        }

        _selection.Clear();
        _selection.Set(skill, actor);

        if (UsesDice)
        {
            BattleCharacter autoTarget = AutoTargetFor(skill, actor);
            if (autoTarget != null) PerformSkillOnTarget(autoTarget);
            else PerformSkill();
            return;
        }

        if (SkillFirstTargeting)
        {
            if (targetHandler != null) targetHandler.Deselect(false);
            Notify("대상을 선택하세요. (우클릭/Esc: 취소)", Color.yellow);
            return;
        }

        if (targetHandler != null && targetHandler.target != null)
            PerformSkill();
        else
            Notify("대상을 선택하세요.", Color.yellow);
    }

    public void SelectConsumable(int consumableIndex)
    {
        if (!IsPlayerTurn) return;

        var resMgr = TheLastArk.Managers.ResourceManager.Instance;
        if (resMgr == null || consumableIndex < 0 || consumableIndex >= resMgr.Consumables.Count) return;

        var consumable = resMgr.Consumables[consumableIndex];
        _selection.Clear();
        _selection.SetConsumable(consumable, consumableIndex);

        if (consumable.effectType == TheLastArk.Data.ConsumableEffectType.DamageAll)
        {
            PerformConsumable();
            return;
        }

        if (SkillFirstTargeting)
        {
            if (targetHandler != null) targetHandler.Deselect(false);
            Notify("대상을 선택하세요. (우클릭/Esc: 취소)", Color.yellow);
            return;
        }

        if (targetHandler != null && targetHandler.target != null)
            PerformConsumable();
        else
            Notify("대상을 선택하세요.", Color.yellow);
    }

    public void PerformSkill() => PerformSkillOnTarget(null);

    private void PerformSkillOnTarget(BattleCharacter forcedTarget)
    {
        if (!IsPlayerTurn) return;
        if (!_selection.IsReady || (forcedTarget == null && (targetHandler == null || targetHandler.target == null))) return;

        BattleCharacter primaryTarget = forcedTarget != null ? forcedTarget : targetHandler.target.GetComponent<BattleCharacter>();
        if (primaryTarget == null) return;

        SkillInfo skill = _selection.Skill;
        BattleCharacter actor = _selection.Actor;
        if (actor?.status == null || actor.status.currentHp <= 0f || primaryTarget.status == null || primaryTarget.status.currentHp <= 0f) return;
        string panicReason = GetPanicSkillUnavailableReason(actor, skill);
        if (panicReason != null)
        {
            Notify(panicReason, Color.red);
            return;
        }
        if (actor.status.GetStatus(EffectType.Stun) != null)
        {
            Notify("[기절] 행동할 수 없습니다.", Color.red);
            return;
        }
        int selectedSkillSlot = actor.status.dynamicActiveSkill.IndexOf(skill);
        bool isBlocked = actor.status.activeStatusEffects.Exists(e => e.effectType == EffectType.Blockade
            && e.IsActive && (e.skillSlot < 0 || e.skillSlot == selectedSkillSlot));
        if (isBlocked)
        {
            Notify("[봉쇄] 해당 스킬칸을 사용할 수 없습니다.", Color.red);
            return;
        }
        if (actor.status.GetStatus(EffectType.Fatigue) != null && actor.status.lastUsedSkill == skill)
        {
            Notify("[피로] 같은 스킬을 연속으로 사용할 수 없습니다.", Color.red);
            return;
        }
        int skillIdx = actor.status.SkillLevelIndex;
        SkillLevelData levelData = skill.levels[Mathf.Clamp(skillIdx, 0, skill.levels.Length - 1)];

        bool isValidTarget = levelData.targetType == TargetType.Self
            ? primaryTarget == actor
            : !(levelData.excludeSelf && primaryTarget == actor)
                && TargetResolver.IsValid(primaryTarget, levelData.targetType, playerParty, enemyParty, actor);
        if (!isValidTarget)
        {
            string message = TargetResolver.IsTauntAffected(levelData.targetType)
                && TargetResolver.FindTauntTarget(enemyParty) != null
                ? "[도발] 도발 상태의 적만 공격할 수 있습니다."
                : $"{skill.skillName}에 맞는 대상이 아닙니다.";
            Notify(message, Color.red);
            if (!SkillFirstTargeting) _selection.Clear();
            return;
        }

        // [색욕] 매혹된 캐릭터 직접 조종 제한
        if (_sinState.charmedCharacters != null && _sinState.charmedCharacters.Contains(actor))
        {
            Notify($"[매혹] {actor.characterName}(은)는 매혹되어 이번 턴에 직접 조종할 수 없습니다!", Color.magenta);
            if (!SkillFirstTargeting) _selection.Clear();
            return;
        }

        // [나태] 스킬 최대 3회 사용 제한 (순교자의 서약 제외)
        bool hasMartyrVow = TheLastArk.Managers.TrainManager.IsInitialized && TheLastArk.Managers.TrainManager.Instance.nexusCar != null && TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TrainPartEffectType.SinMartyrVow);
        if (!hasMartyrVow && _sinState.currentSin == TheLastArk.Battle.SinType.Sloth && !_sinState.isIndulgedCurrentSin)
        {
            if (_sinState.skillsUsedThisTurn >= 3)
            {
                Notify("[나태의 죄] 이번 턴에는 스킬을 최대 3회까지만 사용할 수 있습니다!", Color.red);
                if (!SkillFirstTargeting) _selection.Clear();
                return;
            }
        }

        // [황제] 이전 턴 사용 스킬 금지 체크
        if (_arcanaState.emperorBannedSkillsNextTurn != null && _arcanaState.emperorBannedSkillsNextTurn.Contains(skill))
        {
            Notify("[황제] 이전 턴에 사용했던 스킬은 이번 턴에 사용할 수 없습니다!", Color.red);
            if (!SkillFirstTargeting) _selection.Clear();
            return;
        }

        // [황제] 이번 턴 이미 사용한 스킬 중복 시전 금지 체크
        if (_arcanaState.isEmperorActiveThisTurn && _arcanaState.emperorUsedSkillsThisTurn.Contains(skill))
        {
            Notify("[황제] 이번 턴에 이미 사용한 스킬은 다시 사용할 수 없습니다!", Color.red);
            if (!SkillFirstTargeting) _selection.Clear();
            return;
        }

        int cost = 0;
        if (UsesDice)
        {
            if (!TryCommitDiceSkill(actor, skill)) return;
        }
        else
        {
        cost = levelData.overrideCost != -1 ? levelData.overrideCost : skill.baseCost;

        // [황제 / 오만] 모든 스킬 비용 0 고정
        if (_arcanaState.isEmperorActiveThisTurn || (_sinState.currentSin == TheLastArk.Battle.SinType.Pride && !_sinState.isIndulgedCurrentSin))
        {
            cost = 0;
        }
        else
        {
            // [광대] 첫 2개 스킬 비용 0 소모
            if (_arcanaState.foolFreeSkillsRemaining > 0)
            {
                cost = 0;
                _arcanaState.foolFreeSkillsRemaining--;
                Notify($"[광대] 스킬 비용 0 소모! (남은 횟수: {_arcanaState.foolFreeSkillsRemaining})", Color.yellow);
            }
            // [완전수 모듈] 수치가 6 또는 28일 때 이번 턴 첫 스킬 비용 0 소모
            else if (_limitFirstSkillFreeThisTurn)
            {
                cost = 0;
                _limitFirstSkillFreeThisTurn = false;
                Notify("[완전수 모듈] 첫 스킬 비용 0 소모!", Color.green);
            }

            // [교황] 3코스트 이상 스킬 비용 -1
            if (_arcanaState.isHierophantActiveThisTurn && cost >= 3)
            {
                cost = Mathf.Max(0, cost - 1);
            }

            // [힘] 공격 스킬 비용 +1
            bool isAttackSkill = levelData.targetType == TargetType.SingleEnemy ||
                                 levelData.targetType == TargetType.LeftEnemy ||
                                 levelData.targetType == TargetType.RightEnemy ||
                                 levelData.targetType == TargetType.AdjacentEnemy ||
                                 levelData.targetType == TargetType.AllEnemy ||
                                 (levelData.effects != null && levelData.effects.Exists(e => e.type == EffectType.Damage));

            if (_arcanaState.isStrengthActiveThisTurn && isAttackSkill)
            {
                cost += 1;
            }
        }

        cost += Mathf.Max(0, Mathf.RoundToInt(actor.status.GetStatusValue(EffectType.Frost)));

        if (_arcanaState.isEmperorActiveThisTurn)
        {
            _arcanaState.emperorUsedSkillsThisTurn.Add(skill);
        }

        if (currentAP < cost)
        {
            bool canOverload = TheLastArk.Managers.TrainManager.IsInitialized &&
                               TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
                               TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TheLastArk.Data.TrainPartEffectType.OverloadModule) &&
                               _canOverloadThisTurn && !_overloadedThisTurn;

            int needed = cost - currentAP;
            if (canOverload && needed <= 5)
            {
                _nextTurnLockedAP = needed;
                _overloadedThisTurn = true;
                currentAP += needed;
                Notify($"[과부하 모듈] 다음 턴 행동력 {needed}를 미리 사용합니다! (다음 턴 {needed} 잠김)", Color.yellow);
            }
            else
            {
                Notify(canOverload ? "행동력이 부족합니다. (과부하는 최대 5까지 가능)" : "행동력이 부족합니다.", Color.red);
                return;
            }
        }

        currentAP -= cost;
        UpdateAPUI();
        }

        _sinState.skillsUsedThisTurn++;

        // [오만] 스킬 사용 시마다 무작위 아군 정신력 -6 감소
        if (_sinState.currentSin == TheLastArk.Battle.SinType.Pride && !_sinState.isIndulgedCurrentSin)
        {
            var livingAllies = new List<BattleCharacter>();
            foreach (var a in playerParty) { if (a != null && a.status != null && a.status.currentHp > 0) livingAllies.Add(a); }
            if (livingAllies.Count > 0)
            {
                var victim = livingAllies[UnityEngine.Random.Range(0, livingAllies.Count)];
                victim.TakeMentalDamage(6f);
                if (victim.view != null) victim.view.UpdateVisual(victim.status);
                Notify($"[오만의 죄] 스킬 사용으로 {victim.characterName} 정신력 -6 감소!", Color.magenta);
            }
        }

        CountActualSkillUse(actor);

        {
            // Order: 혼란 (at fire) → 도발 → 보호 (per hit in the damage path).
            // 환각: random final target among all living combatants (self included); 혼란 / 도발 skipped, 보호 still applies.
            bool hallucinating = IsHallucinatingAttack(actor, levelData);
            if (hallucinating) primaryTarget = HallucinationTarget(actor) ?? primaryTarget;
            else primaryTarget = ApplyConfusion(actor, primaryTarget, levelData.targetType);
            BattleCharacter resolvedPrimary = hallucinating ? primaryTarget : TargetResolver.ResolvePrimary(primaryTarget, levelData.targetType, enemyParty, actor);
            List<BattleCharacter> targets = TargetResolver.Resolve(primaryTarget, levelData.targetType, playerParty, enemyParty, actor, hallucinating);
            SkillCast cast = _pendingCast ?? new SkillCast();
            cast.IgnoreTaunt = hallucinating;
            cast.DamageBonus += TwinStarBonus(actor, levelData, resolvedPrimary);
            EffectEngine.CurrentCast = cast;
            try { EffectEngine.ProcessSkill(actor, targets, levelData, resolvedPrimary, skill.skillName); }
            finally { EffectEngine.CurrentCast = null; }
            ResolveEquipmentAfterSkill(actor, levelData, cast);
            ResolveRelicsAfterSkill(actor, skill, levelData, cast);
            AfterActualSkillFire(actor, skill, levelData, cast);

            // [탑] 이번 턴 처음 사용하는 스킬 2번 발동 (더블 캐스트)
            if (_arcanaState.isTowerDoubleCastActive)
            {
                _arcanaState.isTowerDoubleCastActive = false;
                List<BattleCharacter> doubleTargets = TargetResolver.Resolve(primaryTarget, levelData.targetType, playerParty, enemyParty, actor);
                EffectEngine.CurrentCast = cast;
                try { EffectEngine.ProcessSkill(actor, doubleTargets, levelData, TargetResolver.ResolvePrimary(primaryTarget, levelData.targetType, enemyParty, actor), skill.skillName); }
                finally { EffectEngine.CurrentCast = null; }
                Notify("[탑] 첫 스킬이 2번 연속 발동되었습니다!", Color.red);
            }

            // [식탐] 적 처치 시 추가 행동력 획득
            if (_sinState.currentSin == TheLastArk.Battle.SinType.Gluttony && !_sinState.isIndulgedCurrentSin)
            {
                bool anyEnemyKilled = false;
                foreach (var t in targets)
                {
                    if (t != null && t.status != null && t.status.origin != null && t.status.origin.isEnemy && t.status.currentHp <= 0)
                    {
                        anyEnemyKilled = true;
                        break;
                    }
                }

                if (anyEnemyKilled)
                {
                    _sinState.enemyKilledThisTurn = true;
                    bool hasMartyr = TheLastArk.Managers.TrainManager.IsInitialized && TheLastArk.Managers.TrainManager.Instance.nexusCar != null && TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TrainPartEffectType.SinMartyrVow);
                    bool hasGevil = TheLastArk.Managers.TrainManager.IsInitialized && TheLastArk.Managers.TrainManager.Instance.nexusCar != null && TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TrainPartEffectType.SinGreaterEvil);
                    int bonus = hasMartyr ? 6 : (hasGevil ? 6 : 3);

                    currentAP = Mathf.Min(MaxAP + 10, currentAP + bonus);
                    UpdateAPUI();
                    Notify($"[식탐의 죄] 적 처치 달성! 행동력 +{bonus} 획득!", Color.yellow);
                }
            }
        }

        _pendingCast = null;
        actor.status.lastUsedSkill = skill;
        actor.ResolveActionStatusEffects();

        // [별자리] 스킬 3회 사용 시마다 행동력 +1 획득
        if (_arcanaState.isConstellationActive)
        {
            _arcanaState.constellationSkillCount++;
            if (_arcanaState.constellationSkillCount % 3 == 0)
            {
                currentAP = Mathf.Min(MaxAP + 5, currentAP + 1);
                UpdateAPUI();
                Notify("[별자리] 스킬 3회 사용 달성! 행동력 +1 획득!", Color.cyan);
            }
        }

        _selection.Clear();
        // Dice mode keeps a living target selected so the player can keep acting on it.
        if (targetHandler != null && !(UsesDice && IsKeptTargetAlive())) targetHandler.Deselect(false);
        if (TryEndBattleIfPartyWiped()) return;
        RefreshSkillSlots();

        if (UsesDice)
        {
            TryAutoEndDiceTurn();
        }
        else if (currentAP <= 0 && !_resolvingPanicTurn) EndPlayerTurn();
    }

    /// <summary>Player-facing message; skipped outside Play Mode (edit-mode tests).</summary>
    private static void Notify(string message, Color color)
    {
        if (Application.isPlaying) NotificationManager.Instance?.ShowMessage(message, color);
    }

    private bool IsKeptTargetAlive()
    {
        BattleCharacter kept = targetHandler != null && targetHandler.target != null
            ? targetHandler.target.GetComponent<BattleCharacter>() : null;
        return kept != null && kept.status != null && kept.status.currentHp > 0f;
    }

    private void TryAutoEndDiceTurn()
    {
        if (_resolvingPanicTurn) return;
        if (!UsesDice || !IsPlayerTurn || !AutoEndDiceTurn || _dicePool.GeneratedThisTurn == 0) return;
        // Remaining dice that only fail conditions still block auto end; so does a fireable ready charge.
        foreach (ActionDie die in _dicePool.Dice)
            if (die.IsAvailable) return;
        if (HasFireableReadyCharge()) return;
        EndPlayerTurn();
    }

    private bool HasFireableReadyCharge()
    {
        foreach (BattleCharacter actor in playerParty)
        {
            if (actor?.status == null) continue;
            foreach (SkillInfo skill in actor.status.dynamicActiveSkill)
                if (IsChargeReady(actor, skill) && CanUseDiceSkill(actor, skill)) return true;
        }
        return false;
    }

    /// <summary>
    /// 혼란: on an actual fire, the summed % chance moves a single-primary skill to a random target among the
    /// skill's original valid targets (taunt not yet applied). Self and all-target skills are unaffected.
    /// </summary>
    private BattleCharacter ApplyConfusion(BattleCharacter actor, BattleCharacter primaryTarget, TargetType type)
    {
        float chance = actor.status.GetStatusValue(EffectType.Confusion) * 0.01f;
        if (chance <= 0f || type == TargetType.Self || type == TargetType.AllEnemy || type == TargetType.AllFriendly) return primaryTarget;
        if (UnityEngine.Random.value >= chance) return primaryTarget;
        var pool = type == TargetType.Friendly || type == TargetType.FriendlyLeft || type == TargetType.FriendlyRight
            || type == TargetType.FriendlyAdjacent ? playerParty : enemyParty;
        var candidates = pool.FindAll(c => c != null && c.status != null && c.status.currentHp > 0f
            && TargetResolver.IsValid(c, type, playerParty, enemyParty, actor, true));
        if (candidates.Count == 0) return primaryTarget;
        Notify("[혼란] 스킬 대상이 무작위로 변경되었습니다!", Color.magenta);
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    public void PerformConsumable()
    {
        if (!IsPlayerTurn) return;
        if (!_selection.IsReady || _selection.Consumable == null) return;

        var consumable = _selection.Consumable;
        var targets = new List<BattleCharacter>();

        BattleCharacter primaryTarget = null;
        if (targetHandler.target != null)
            primaryTarget = targetHandler.target.GetComponent<BattleCharacter>();

        switch (consumable.effectType)
        {
            case TheLastArk.Data.ConsumableEffectType.DamageSingle:
                if (primaryTarget == null || playerParty.Contains(primaryTarget))
                {
                    Notify("적을 선택하세요.", Color.red);
                    if (!SkillFirstTargeting) _selection.Clear();
                    return;
                }
                targets.Add(primaryTarget);
                break;

            case TheLastArk.Data.ConsumableEffectType.HealHP:
            case TheLastArk.Data.ConsumableEffectType.HealMental:
                if (primaryTarget == null || enemyParty.Contains(primaryTarget))
                {
                    Notify("아군을 선택하세요.", Color.red);
                    if (!SkillFirstTargeting) _selection.Clear();
                    return;
                }
                targets.Add(primaryTarget);
                break;

            case TheLastArk.Data.ConsumableEffectType.DamageAll:
                targets.AddRange(enemyParty);
                break;
        }

        foreach (BattleCharacter target in targets)
        {
            if (target == null || target.status.currentHp <= 0) continue;

            if (consumable.effectType == TheLastArk.Data.ConsumableEffectType.DamageSingle ||
                consumable.effectType == TheLastArk.Data.ConsumableEffectType.DamageAll)
            {
                target.ReceiveDamage(consumable.effectValue, null, DamageType.True);
            }
            else if (consumable.effectType == TheLastArk.Data.ConsumableEffectType.HealHP)
            {
                target.ReceiveHeal(consumable.effectValue, null);
            }
            else if (consumable.effectType == TheLastArk.Data.ConsumableEffectType.HealMental)
            {
                Debug.Log($"[BattleManager] 정신력 회복: {consumable.effectValue}");
            }
        }

        Notify($"{consumable.consumableName} 사용.", Color.cyan);

        var resMgr = TheLastArk.Managers.ResourceManager.Instance;
        if (resMgr != null) resMgr.RemoveConsumable(_selection.ConsumableIndex);

        _selection.Clear();
        if (targetHandler != null) targetHandler.Deselect(false);
    }

    private void OnTargetSelected(GameObject selectedTarget)
    {
        if (!SkillFirstTargeting || !_selection.IsReady || selectedTarget == null) return;
        if (_selection.Skill != null) PerformSkill();
        else if (_selection.Consumable != null) PerformConsumable();
    }

    public void CancelPendingSelection()
    {
        _selection.Clear();
        if (UsesDice)
        {
            _selectedDiceIds.Clear();
            _pendingDiceSkill = null;
            _pendingDiceActor = null;
            RefreshDiceUI();
        }
        if (targetHandler != null) targetHandler.Deselect(false);
    }

    public void EndPlayerTurn()
    {
        if (_lastTurnEndClickFrame == Time.frameCount) return;
        _lastTurnEndClickFrame = Time.frameCount;

        Debug.Log($"[Battle] EndPlayerTurn() 호출. 현재 페이즈: {_currentPhase}");

        if (!IsPlayerTurn)
        {
            Debug.LogWarning($"[Battle] 턴 종료 입력 무시: 현재 페이즈가 플레이어 턴이 아닙니다. CurrentPhase: {_currentPhase}");
            return;
        }
        if (UsesDice)
        {
            _selectedDiceIds.Clear();
            _pendingDiceSkill = null;
            _pendingDiceActor = null;
            PrepareCarryOver();
            ResetTwinStar();
            _dicePool.EndTurn();
            RefreshDiceUI();
            if (targetHandler != null) targetHandler.Deselect(false);
        }
        ApplySeizure(playerParty);
        TickSideTurnEnd(playerParty);

        // [에너지 저장소] 미사용 행동력 최대 3까지 다음 턴으로 이월 저장
        if (!UsesDice && TheLastArk.Managers.TrainManager.IsInitialized && TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TheLastArk.Data.TrainPartEffectType.EnergyStorage))
        {
            _carriedOverAP = Mathf.Clamp(currentAP, 0, 3);
            if (_carriedOverAP > 0)
            {
                Debug.Log($"[에너지 저장소] 미사용 행동력 {_carriedOverAP} 다음 턴 이월 저장.");
            }
        }
        else
        {
            _carriedOverAP = 0;
        }

        // [죽음] 턴 종료 시 모든 아군 체력/정신력 4 감소
        if (_arcanaState.isDeathPendingThisTurn)
        {
            foreach (var a in playerParty)
            {
                if (a != null && a.status != null && a.status.currentHp > 0)
                {
                    a.status.currentHp = Mathf.Max(1f, a.status.currentHp - 4f);
                    a.TakeMentalDamage(4f);
                    if (a.view != null) a.view.UpdateVisual(a.status);
                }
            }
            Notify("[죽음] 턴 종료로 모든 아군 체력/정신력 -4 감소", Color.black);
        }

        // [악마 계약] 턴 종료 시 지속 효과 처리
        if (_arcanaState.activeDevilContract == TheLastArk.Battle.DevilContractType.Option2_EveryTurnAP_LoseMental_NoMoreDevil)
        {
            foreach (var a in playerParty)
            {
                if (a != null && a.status != null && a.status.currentHp > 0)
                {
                    a.TakeMentalDamage(6f);
                    if (a.view != null) a.view.UpdateVisual(a.status);
                }
            }
            Notify("[악마 계약] 턴 종료로 모든 아군 정신력 -6 감소", Color.red);
        }
        else if (_arcanaState.activeDevilContract == TheLastArk.Battle.DevilContractType.Option3_EveryTurnAP_GainDebuffs)
        {
            foreach (var a in playerParty)
            {
                if (a != null && a.status != null && a.status.currentHp > 0)
                {
                    a.status.ApplyStatusEffect(EffectType.Bleed, 1f, 2);
                }
            }
            Notify("[악마 계약] 턴 종료로 모든 아군 약화/취약 획득", Color.red);
        }

        // [황제] 다음 턴 스킬 밴 목록 갱신 및 턴 플래그 리셋
        _arcanaState.emperorBannedSkillsNextTurn = new HashSet<SkillInfo>(_arcanaState.emperorUsedSkillsThisTurn);
        _arcanaState.ResetTurnFlags();

        // [식탐] 적을 처치하지 못했을 시 다음 턴 행동력 감소 예약
        if (_sinState.currentSin == TheLastArk.Battle.SinType.Gluttony && !_sinState.isIndulgedCurrentSin)
        {
            if (!_sinState.enemyKilledThisTurn)
            {
                bool hasMartyr = TheLastArk.Managers.TrainManager.IsInitialized && TheLastArk.Managers.TrainManager.Instance.nexusCar != null && TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TrainPartEffectType.SinMartyrVow);
                bool hasGevil = TheLastArk.Managers.TrainManager.IsInitialized && TheLastArk.Managers.TrainManager.Instance.nexusCar != null && TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TrainPartEffectType.SinGreaterEvil);
                _sinState.pendingGluttonyPenaltyNextTurn = hasMartyr ? 9 : (hasGevil ? 6 : 3);
                Notify($"[식탐의 죄] 적 미처치로 다음 턴 행동력 -{_sinState.pendingGluttonyPenaltyNextTurn} 감소 예정", Color.red);
            }
        }

        // [탐욕] 턴 종료 시 남은(미사용) 행동력 비례 모든 아군 정신력 피해
        if (_sinState.currentSin == TheLastArk.Battle.SinType.Greed && !_sinState.isIndulgedCurrentSin && currentAP > 0)
        {
            bool hasMartyr = TheLastArk.Managers.TrainManager.IsInitialized && TheLastArk.Managers.TrainManager.Instance.nexusCar != null && TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TrainPartEffectType.SinMartyrVow);
            bool hasGevil = TheLastArk.Managers.TrainManager.IsInitialized && TheLastArk.Managers.TrainManager.Instance.nexusCar != null && TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TrainPartEffectType.SinGreaterEvil);

            foreach (var a in playerParty)
            {
                if (a != null && a.status != null && a.status.currentHp > 0)
                {
                    if (hasMartyr)
                    {
                        float mDmg = a.status.FinalMaxMental * 0.20f * currentAP;
                        a.TakeMentalDamage(mDmg);
                    }
                    else
                    {
                        int pPerAp = hasGevil ? 6 : 3;
                        a.TakeMentalDamage(pPerAp * currentAP);
                    }
                    if (a.view != null) a.view.UpdateVisual(a.status);
                }
            }
            Notify($"[탐욕의 죄] 미사용 행동력({currentAP}) 비례 모든 아군 정신력 감소!", Color.red);
        }

        // 씬 턴 카운터 리셋
        _sinState.ResetTurnCounters();

        CancelPendingSelection();
        Debug.Log($"[Battle] 적 턴으로 전환합니다. EnemyParty 수: {enemyParty.Count}");
        EnterPhase(BattlePhase.EnemyTurn);
    }

    public void DebugWinBattle()
    {
        if (_currentPhase == BattlePhase.BattleEnd) return;
        if (DebugActionRunning || !IsPlayerTurn) return;

        Debug.LogWarning("[Battle] DebugWinBattle() 호출. 적 파티를 즉시 전멸 처리합니다.");
        if (enemyParty != null)
        {
            foreach (BattleCharacter enemy in enemyParty)
            {
                if (enemy == null || enemy.status == null) continue;
                enemy.status.currentHp = 0f;
                if (enemy.view != null) enemy.view.UpdateVisual(enemy.status);
            }
        }

        EnterPhase(BattlePhase.BattleEnd);
    }

    private void OnEnterPlayerTurn()
    {
        RemoveOwnerPhaseEffects(playerParty);
        ResetSynergyTurnState();
        ResetRelicTurnState();
        ResetEquipmentTurnState();
        RefreshHuntMarks();
        ApplySynergyTurnStart();
        ApplyRelicTurnStart();
        ApplyEquipmentTurnStart();
        ClearEnemyTargetMarkers();
        PrepareEnemyIntents();
        Debug.Log($"[Battle] {_turnCount}턴: 아군 턴 시작");

        _limitFirstSkillFreeThisTurn = false;
        _limitRetainAllSkillsThisTurn = false;

        if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.GambleId)
        {
            TheLastArk.UI.GambleDiceUI.Instance.ShowDiceRoll(TheLastArk.Managers.TrainManager.Instance.nexusCar, (rolledAP) =>
            {
                _gambleRolledAP = rolledAP;
                _limitRolledAP = -1;
                _clusterRolledAP = -1;
                ApplyPlayerTurnStartAP();
            });
        }
        else if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.LimitId)
        {
            TheLastArk.UI.LimitCardUI.Instance.ShowCardDraw(TheLastArk.Managers.TrainManager.Instance.nexusCar, (rolledAP, freeSkill, retainAll) =>
            {
                _limitRolledAP = rolledAP;
                _gambleRolledAP = -1;
                _clusterRolledAP = -1;
                _limitFirstSkillFreeThisTurn = freeSkill;
                _limitRetainAllSkillsThisTurn = retainAll;
                ApplyPlayerTurnStartAP();
            });
        }
        else if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.ClusterId)
        {
            TheLastArk.UI.ClusterCardUI.Instance.ShowCardDraw(TheLastArk.Managers.TrainManager.Instance.nexusCar, (rolledAP, result) =>
            {
                _clusterRolledAP = rolledAP;
                _gambleRolledAP = -1;
                _limitRolledAP = -1;
                _arcanaRolledAP = -1;
                ApplyPlayerTurnStartAP();
                ApplyClusterSuitEffects(result);
            });
        }
        else if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.ArcanaId)
        {
            TheLastArk.UI.ArcanaCardUI.Instance.ShowTarotDraw(TheLastArk.Managers.TrainManager.Instance.nexusCar, _arcanaState, (rolledAP, result) =>
            {
                _arcanaRolledAP = rolledAP;
                _gambleRolledAP = -1;
                _limitRolledAP = -1;
                _clusterRolledAP = -1;
                _sinRolledAP = -1;
                ApplyPlayerTurnStartAP();
                ApplyArcanaCardEffects(result);
            });
        }
        else if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.SinId)
        {
            var nexusCar = TheLastArk.Managers.TrainManager.Instance.nexusCar;
            bool shouldDrawNewSin = _sinState.rerollSinNextTurn || !_sinState.currentSin.HasValue || _sinState.remainingTurns <= 0;

            if (shouldDrawNewSin)
            {
                _sinState.rerollSinNextTurn = false;
                _sinState.isIndulgedCurrentSin = false;
                var newSin = TheLastArk.Battle.SinModuleManager.DrawNextSin(nexusCar, _sinState);
                _sinState.currentSin = newSin;
                _sinState.remainingTurns = 3;

                TheLastArk.UI.SinModuleUI.Instance.ShowSinManifestation(newSin, nexusCar, _sinState, (rolledAP) =>
                {
                    _sinRolledAP = rolledAP;
                    _gambleRolledAP = -1;
                    _limitRolledAP = -1;
                    _clusterRolledAP = -1;
                    _arcanaRolledAP = -1;
                    ApplyPlayerTurnStartAP();
                    ApplySinStartOfTurnEffects();
                });
            }
            else
            {
                _sinState.remainingTurns--;
                _sinRolledAP = TheLastArk.Battle.SinModuleManager.CalculateSinAP(_sinState.currentSin.Value, nexusCar);
                _gambleRolledAP = -1;
                _limitRolledAP = -1;
                _clusterRolledAP = -1;
                _arcanaRolledAP = -1;
                ApplyPlayerTurnStartAP();
                ApplySinStartOfTurnEffects();
                TheLastArk.UI.SinModuleUI.Instance.UpdateHUD(_sinState, nexusCar);
            }
        }
        else
        {
            _gambleRolledAP = -1;
            _limitRolledAP = -1;
            _clusterRolledAP = -1;
            _arcanaRolledAP = -1;
            _sinRolledAP = -1;
            ApplyPlayerTurnStartAP();
        }
    }

    private void ApplyArcanaCardEffects(TheLastArk.Battle.ArcanaDrawResult result)
    {
        if (result == null || result.drawnCard == null) return;
        if (!TheLastArk.Managers.TrainManager.IsInitialized || TheLastArk.Managers.TrainManager.Instance.nexusCar == null) return;

        ExecuteSingleTarotEffect(result.drawnCard, result.devilChoice);

        if (result.hermitChainedCard != null)
        {
            ExecuteSingleTarotEffect(result.hermitChainedCard, result.devilChoice);
        }

        if (!string.IsNullOrEmpty(result.summary))
        {
            Notify($"[아르카나 개방] {result.summary}", Color.yellow);
        }
    }

    private void ExecuteSingleTarotEffect(TheLastArk.Battle.ArcanaCardInfo card, TheLastArk.Battle.DevilContractType devilChoice)
    {
        if (card == null) return;

        var livingAllies = new List<BattleCharacter>();
        if (playerParty != null)
        {
            foreach (var a in playerParty)
            {
                if (a != null && a.status != null && a.status.currentHp > 0)
                    livingAllies.Add(a);
            }
        }

        var livingEnemies = new List<BattleCharacter>();
        if (enemyParty != null)
        {
            foreach (var e in enemyParty)
            {
                if (e != null && e.status != null && e.status.currentHp > 0)
                    livingEnemies.Add(e);
            }
        }

        switch (card.cardType)
        {
            case TarotCardType.Fool:
                _arcanaState.foolFreeSkillsRemaining = 2;
                Notify("[광대] 이번 턴 사용하는 첫 스킬 2개의 비용이 0이 됩니다!", Color.yellow);
                break;

            case TarotCardType.HighPriestess:
                foreach (var a in livingAllies)
                {
                    a.status.ApplyStatusEffect(EffectType.Shield, 1f, 1);
                }
                Notify("[여사제] 모든 아군에게 보호(방어막) 1 부여!", Color.cyan);
                break;

            case TarotCardType.Lovers:
                _arcanaState.isLoversActiveThisTurn = true;
                Notify("[연인] 이번 턴 아군이 얻는 힘/보호 효과가 전체에게 공유됩니다!", Color.magenta);
                break;

            case TarotCardType.Chariot:
                foreach (var a in livingAllies)
                {
                    a.status.bonusAttack += 1;
                    if (a.view != null) a.view.UpdateVisual(a.status);
                }
                Notify("[전차] 모든 아군 힘(공격력) +1 증가!", Color.cyan);
                break;

            case TarotCardType.Strength:
                _arcanaState.isStrengthActiveThisTurn = true;
                foreach (var a in livingAllies)
                {
                    a.status.bonusAttack += 3;
                    if (a.view != null) a.view.UpdateVisual(a.status);
                }
                Notify("[힘] 모든 아군 힘 +3 증가! (이번 턴 공격 스킬 비용 +1)", Color.red);
                break;

            case TarotCardType.WheelOfFortune:
                _arcanaState.isWheelOfFortunePending = true;
                Notify("[운명] 다음 턴에 얻는 행동력이 2배로 증가합니다!", Color.yellow);
                break;

            case TarotCardType.Justice:
                foreach (var e in livingEnemies)
                {
                    e.status.ApplyStatusEffect(EffectType.Bleed, 3f, 3);
                }
                Notify("[정의] 모든 적에게 약화 3 / 취약 3(출혈) 부여!", Color.cyan);
                break;

            case TarotCardType.Hierophant:
                _arcanaState.isHierophantActiveThisTurn = true;
                Notify("[교황] 비용 3 이상인 스킬들의 비용이 -1 감소합니다!", Color.yellow);
                break;

            case TarotCardType.HangedMan:
                _arcanaState.hangedManPenaltyPending = 4;
                Notify("[매달린 사람] 대량 AP 획득! (다음 턴 행동력 -4)", Color.magenta);
                break;

            case TarotCardType.Temperance:
                _arcanaState.temperanceAccumulatedAP += 1;
                Notify($"[절제] 매 턴 행동력 +1 영구 누적! (현재 누적: +{_arcanaState.temperanceAccumulatedAP} AP)", Color.green);
                break;

            case TarotCardType.Tower:
                _arcanaState.isTowerDoubleCastActive = true;
                Notify("[탑] 이번 턴에 처음 사용하는 스킬이 2번 발동(더블 캐스트)됩니다!", Color.red);
                break;

            case TarotCardType.Empress:
                foreach (var a in livingAllies)
                {
                    a.status.currentHp = Mathf.Min(a.status.FinalMaxHp, a.status.currentHp + 5);
                    a.ReceiveMentalHeal(5f);
                    if (a.view != null) a.view.UpdateVisual(a.status);
                }
                Notify("[여제] 모든 아군 체력 +5, 정신력 +5 회복!", Color.green);
                break;

            case TarotCardType.Emperor:
                _arcanaState.isEmperorActiveThisTurn = true;
                Notify("[황제] 이번 턴 모든 스킬 비용 0 고정! (스킬당 1회 제한, 사용 스킬 다음 턴 사용 불가)", Color.red);
                break;

            case TarotCardType.Death:
                _arcanaState.isDeathPendingThisTurn = true;
                Notify("[죽음] 막대한 AP 획득! (턴 종료 시 모든 아군 체력/정신력 -4 감소)", Color.black);
                break;

            case TarotCardType.Devil:
                if (devilChoice == DevilContractType.Option1_MoreAP_MoreDevil)
                {
                    _arcanaState.devilOption1PickCount++;
                    Notify("[악마] 탐욕 계약 체결! (악마 AP 및 등장 확률 증가)", Color.red);
                }
                else if (devilChoice == DevilContractType.Option2_EveryTurnAP_LoseMental_NoMoreDevil)
                {
                    _arcanaState.activeDevilContract = DevilContractType.Option2_EveryTurnAP_LoseMental_NoMoreDevil;
                    Notify("[악마] 광기 계약 체결! (매 턴 +6 AP, 매 턴 전원 정신력 -6, 악마 등장 제외)", Color.red);
                }
                else if (devilChoice == DevilContractType.Option3_EveryTurnAP_GainDebuffs)
                {
                    _arcanaState.activeDevilContract = DevilContractType.Option3_EveryTurnAP_GainDebuffs;
                    Notify("[악마] 고통 계약 체결! (매 턴 +6 AP, 매 턴 전원 약화/취약 획득)", Color.red);
                }
                break;

            case TarotCardType.Judgement:
                if (livingEnemies.Count > 0)
                {
                    BattleCharacter highestHpEnemy = livingEnemies[0];
                    foreach (var e in livingEnemies)
                    {
                        if (e.status.currentHp > highestHpEnemy.status.currentHp) highestHpEnemy = e;
                    }
                    highestHpEnemy.status.ApplyStatusEffect(EffectType.Stun, 0f, 1);
                    Notify($"[심판] 최고 체력 적 {highestHpEnemy.characterName} 1턴 기절!", Color.yellow);
                }
                break;

            case TarotCardType.Sun:
                _arcanaState.isSunRetainActiveThisTurn = true;
                Notify("[태양] 이번 턴 모든 스킬에 유지(Retain) 효과 부여!", Color.yellow);
                break;

            case TarotCardType.Moon:
                int highMentalCount = 0;
                foreach (var a in livingAllies)
                {
                    if (a.status.currentMental >= a.status.FinalMaxMental * 0.5f)
                    {
                        highMentalCount++;
                    }
                    else
                    {
                        a.ReceiveMentalHeal(a.status.FinalMaxMental * 0.15f);
                        if (a.view != null) a.view.UpdateVisual(a.status);
                    }
                }
                if (highMentalCount > 0)
                {
                    currentAP = Mathf.Min(MaxAP + 10, currentAP + highMentalCount);
                    UpdateAPUI();
                    Notify($"[달] 정신력 50% 이상 아군({highMentalCount}명) 비례 행동력 +{highMentalCount} 추가 획득 & 회복!", Color.cyan);
                }
                break;

            case TarotCardType.Star:
                _arcanaState.isConstellationActive = true;
                Notify("[별] 별자리 효과 획득! (스킬 3회 사용 시마다 행동력 +1 충전)", Color.cyan);
                break;

            case TarotCardType.World:
                foreach (var a in livingAllies)
                {
                    a.status.RemoveAllStatusEffects();
                    if (a.view != null) a.view.UpdateVisual(a.status);
                }
                Notify("[세계] 모든 아군의 모든 상태이상이 완전히 정화되었습니다!", Color.green);
                break;
        }
    }

    private void ApplySinStartOfTurnEffects()
    {
        if (!TheLastArk.Managers.TrainManager.IsInitialized || TheLastArk.Managers.TrainManager.Instance.nexusCar == null) return;
        var nexusCar = TheLastArk.Managers.TrainManager.Instance.nexusCar;

        // 식탐 페널티 적용
        if (_sinState.pendingGluttonyPenaltyNextTurn > 0)
        {
            currentAP = Mathf.Max(1, currentAP - _sinState.pendingGluttonyPenaltyNextTurn);
            UpdateAPUI();
            Notify($"<color=#FF5555>[식탐 페널티] 이전 턴 적 미처치로 행동력 -{_sinState.pendingGluttonyPenaltyNextTurn} 감소</color>", Color.red);
            _sinState.pendingGluttonyPenaltyNextTurn = 0;
        }

        // 면죄된 경우 부가효과 스킵
        if (_sinState.isIndulgedCurrentSin || !_sinState.currentSin.HasValue) return;

        var sin = _sinState.currentSin.Value;
        bool hasGreaterEvil = nexusCar.HasPartEffect(TrainPartEffectType.SinGreaterEvil);
        bool hasMartyrVow = nexusCar.HasPartEffect(TrainPartEffectType.SinMartyrVow);

        var livingAllies = new List<BattleCharacter>();
        if (playerParty != null)
        {
            foreach (var a in playerParty) { if (a != null && a.status != null && a.status.currentHp > 0) livingAllies.Add(a); }
        }

        var livingEnemies = new List<BattleCharacter>();
        if (enemyParty != null)
        {
            foreach (var e in enemyParty) { if (e != null && e.status != null && e.status.currentHp > 0) livingEnemies.Add(e); }
        }

        switch (sin)
        {
            case SinType.Sloth:
                if (hasMartyrVow)
                {
                    ExecuteSlothMartyrAutoSkills(livingAllies, livingEnemies);
                }
                break;

            case SinType.Lust:
                ExecuteLustCharm(livingAllies, livingEnemies, hasGreaterEvil, hasMartyrVow);
                break;

            case SinType.Wrath:
                int wrathBuff = hasMartyrVow ? 12 : (hasGreaterEvil ? 6 : 3);
                foreach (var a in livingAllies)
                {
                    a.status.bonusAttack += wrathBuff;
                    a.status.ApplyStatusEffect(EffectType.Bleed, wrathBuff, 2);
                    if (a.view != null) a.view.UpdateVisual(a.status);
                }
                foreach (var e in livingEnemies)
                {
                    e.status.bonusAttack += wrathBuff;
                    e.status.ApplyStatusEffect(EffectType.Bleed, wrathBuff, 2);
                    if (e.view != null) e.view.UpdateVisual(e.status);
                }
                Notify($"[분노의 죄] 모든 아군과 적에게 힘 +{wrathBuff}, 취약 +{wrathBuff} 동시 부여!", Color.red);
                break;

            case SinType.Envy:
                if (hasMartyrVow)
                {
                    ExecuteEnvyMartyrLifeDrain(livingAllies, livingEnemies);
                }
                else
                {
                    ExecuteEnvyBuffCopy(livingAllies, livingEnemies, hasGreaterEvil);
                }
                break;
        }
    }

    private void ExecuteSlothMartyrAutoSkills(List<BattleCharacter> livingAllies, List<BattleCharacter> livingEnemies)
    {
        if (livingAllies.Count == 0) return;
        Notify("[순교자 나태] 턴 시작 시 무작위 스킬 3개 자동 시전!", Color.cyan);

        for (int i = 0; i < 3; i++)
        {
            var actor = livingAllies[UnityEngine.Random.Range(0, livingAllies.Count)];
            if (actor.status == null || actor.status.dynamicActiveSkill == null || actor.status.dynamicActiveSkill.Count == 0) continue;
            var skill = actor.status.dynamicActiveSkill[UnityEngine.Random.Range(0, actor.status.dynamicActiveSkill.Count)];
            if (skill == null || skill.levels == null || skill.levels.Length == 0) continue;

            int sIdx = Mathf.Clamp(actor.status.SkillLevelIndex, 0, skill.levels.Length - 1);
            var levelData = skill.levels[sIdx];

            BattleCharacter primaryTarget = (livingEnemies.Count > 0) ? livingEnemies[UnityEngine.Random.Range(0, livingEnemies.Count)] : actor;
            var targets = TargetResolver.Resolve(primaryTarget, levelData.targetType, playerParty, enemyParty);
            EffectEngine.ProcessSkill(actor, targets, levelData);
        }
    }

    private void ExecuteLustCharm(List<BattleCharacter> livingAllies, List<BattleCharacter> livingEnemies, bool greaterEvil, bool martyrVow)
    {
        List<BattleCharacter> charmedList = new List<BattleCharacter>();

        if (martyrVow)
        {
            charmedList.AddRange(livingAllies);
            charmedList.AddRange(livingEnemies);
        }
        else
        {
            int count = greaterEvil ? 2 : 1;
            List<BattleCharacter> aPool = new List<BattleCharacter>(livingAllies);
            for (int i = 0; i < count && aPool.Count > 0; i++)
            {
                var chosen = aPool[UnityEngine.Random.Range(0, aPool.Count)];
                charmedList.Add(chosen);
                aPool.Remove(chosen);
            }
            List<BattleCharacter> ePool = new List<BattleCharacter>(livingEnemies);
            for (int i = 0; i < count && ePool.Count > 0; i++)
            {
                var chosen = ePool[UnityEngine.Random.Range(0, ePool.Count)];
                charmedList.Add(chosen);
                ePool.Remove(chosen);
            }
        }

        _sinState.charmedCharacters = new List<BattleCharacter>(charmedList);

        foreach (var c in charmedList)
        {
            ExecuteCharmedAutoCast(c, livingAllies, livingEnemies);
        }

        Notify($"[색욕의 죄] {charmedList.Count}명이 '매혹'되어 자동 행동합니다!", Color.magenta);
    }

    private void ExecuteCharmedAutoCast(BattleCharacter actor, List<BattleCharacter> allies, List<BattleCharacter> enemies)
    {
        if (actor == null || actor.status == null || actor.status.currentHp <= 0) return;

        List<BattleCharacter> allTargets = new List<BattleCharacter>();
        allTargets.AddRange(allies);
        allTargets.AddRange(enemies);
        BattleCharacter target = allTargets.Count > 0 ? allTargets[UnityEngine.Random.Range(0, allTargets.Count)] : actor;

        // 아군인 경우
        if (actor.status.dynamicActiveSkill != null && actor.status.dynamicActiveSkill.Count > 0)
        {
            var skill = actor.status.dynamicActiveSkill[UnityEngine.Random.Range(0, actor.status.dynamicActiveSkill.Count)];
            if (skill != null && skill.levels != null && skill.levels.Length > 0)
            {
                int sIdx = Mathf.Clamp(actor.status.SkillLevelIndex, 0, skill.levels.Length - 1);
                var levelData = skill.levels[sIdx];

                var resolved = TargetResolver.Resolve(target, levelData.targetType, playerParty, enemyParty);
                EffectEngine.ProcessSkill(actor, resolved, levelData);
            }
        }
        // 적군인 경우
        else if (actor.status.origin != null && actor.status.origin.enemyPatterns != null && actor.status.origin.enemyPatterns.Count > 0)
        {
            var pattern = actor.status.origin.enemyPatterns[UnityEngine.Random.Range(0, actor.status.origin.enemyPatterns.Count)];
            var resolved = TargetResolver.Resolve(target, pattern.targetType, playerParty, enemyParty);
            SkillLevelData runtimePattern = new SkillLevelData
            {
                overrideCost = -1,
                targetType = pattern.targetType,
                effects = pattern.effects
            };
            EffectEngine.ProcessSkill(actor, resolved, runtimePattern);
        }
    }

    private void ExecuteEnvyMartyrLifeDrain(List<BattleCharacter> livingAllies, List<BattleCharacter> livingEnemies)
    {
        if (livingAllies.Count == 0 || livingEnemies.Count == 0) return;

        BattleCharacter lowestAlly = livingAllies[0];
        foreach (var a in livingAllies) { if (a.status.currentHp < lowestAlly.status.currentHp) lowestAlly = a; }

        BattleCharacter highestEnemy = livingEnemies[0];
        foreach (var e in livingEnemies) { if (e.status.currentHp > highestEnemy.status.currentHp) highestEnemy = e; }

        float drainAmount = highestEnemy.status.FinalMaxHp * 0.12f;
        float actualDrain = highestEnemy.ReceiveDamage(drainAmount, lowestAlly, DamageType.True);
        lowestAlly.ReceiveHeal(actualDrain, lowestAlly);

        Notify($"[순교자 질투] {lowestAlly.characterName}이(가) {highestEnemy.characterName}의 체력 {actualDrain:F0} 강탈!", Color.green);
    }

    private void ExecuteEnvyBuffCopy(List<BattleCharacter> livingAllies, List<BattleCharacter> livingEnemies, bool greaterEvil)
    {
        if (livingAllies.Count == 0 || livingEnemies.Count == 0) return;

        float totalBonusAtk = 0f;
        float totalShield = 0f;
        foreach (var e in livingEnemies)
        {
            totalBonusAtk += e.status.bonusAttack;
            var shieldEffect = e.status.activeStatusEffects.Find(eff => eff.effectType == EffectType.Shield);
            if (shieldEffect != null) totalShield += shieldEffect.damagePerTurn;
        }

        if (greaterEvil)
        {
            totalBonusAtk *= 2f;
            totalShield *= 2f;
        }

        if (totalBonusAtk > 0f || totalShield > 0f)
        {
            var chosenAlly = livingAllies[UnityEngine.Random.Range(0, livingAllies.Count)];
            if (totalBonusAtk > 0f) chosenAlly.status.bonusAttack += totalBonusAtk;
            if (totalShield > 0f) chosenAlly.status.ApplyStatusEffect(EffectType.Shield, totalShield, 1);
            if (chosenAlly.view != null) chosenAlly.view.UpdateVisual(chosenAlly.status);

            Notify($"[질투의 죄] 적 버프 복사 -> {chosenAlly.characterName}에게 공격력 +{totalBonusAtk:F0}, 보호막 +{totalShield:F0} 부여!", Color.green);
        }
    }

    private void ApplyClusterSuitEffects(TheLastArk.Battle.ClusterHandResult result)
    {
        if (result == null || result.cards == null) return;
        if (!TheLastArk.Managers.TrainManager.IsInitialized || TheLastArk.Managers.TrainManager.Instance.nexusCar == null) return;

        int level = TheLastArk.Managers.TrainManager.Instance.nexusCar.level;

        var livingEnemies = new List<BattleCharacter>();
        if (enemyParty != null)
        {
            foreach (var e in enemyParty)
            {
                if (e != null && e.status != null && e.status.currentHp > 0)
                    livingEnemies.Add(e);
            }
        }

        var livingAllies = new List<BattleCharacter>();
        if (playerParty != null)
        {
            foreach (var a in playerParty)
            {
                if (a != null && a.status != null && a.status.currentHp > 0)
                    livingAllies.Add(a);
            }
        }

        foreach (var card in result.cards)
        {
            int activeNum = card.resolvedNumber;
            CardSuit activeSuit = card.resolvedSuit;
            int suitVal = TheLastArk.Battle.ClusterCardManager.CalculateSuitValue(activeNum, level);

            if (suitVal <= 0) continue;

            switch (activeSuit)
            {
                case CardSuit.Spade:
                    if (livingEnemies.Count > 0)
                    {
                        var targetEnemy = livingEnemies[UnityEngine.Random.Range(0, livingEnemies.Count)];
                        targetEnemy.ReceiveDamage(suitVal, null, DamageType.True);
                        Debug.Log($"[클러스터 ♠] {targetEnemy.characterName}에게 ♠{activeNum} 효과로 관통 피해 {suitVal} 입힘!");
                    }
                    break;

                case CardSuit.Heart:
                    if (livingAllies.Count > 0)
                    {
                        var targetAlly = livingAllies[UnityEngine.Random.Range(0, livingAllies.Count)];
                        targetAlly.status.ApplyStatusEffect(EffectType.Shield, suitVal, 1);
                        Debug.Log($"[클러스터 ♥] {targetAlly.characterName}에게 ♥{activeNum} 효과로 방어막 {suitVal} 부여!");
                    }
                    break;

                case CardSuit.Diamond:
                    if (livingAllies.Count > 0)
                    {
                        var targetAlly = livingAllies[UnityEngine.Random.Range(0, livingAllies.Count)];
                        targetAlly.status.ApplyStatusEffect(EffectType.Shield, suitVal, 2, StatusDurationType.Turns); // 보호: 2턴 유지 보호막
                        Debug.Log($"[클러스터 ♦] {targetAlly.characterName}에게 ♦{activeNum} 효과로 보호 {suitVal} 부여!");
                    }
                    break;
            }
        }

        if (!string.IsNullOrEmpty(result.summary))
        {
            Notify($"[클러스터 덱 완성] {result.summary}", Color.cyan);
        }
    }

    private void ApplyPlayerTurnStartAP()
    {
        if (UsesDice)
        {
            BeginOriginDiceTurn();
            _selectedDiceIds.Clear();
            _pendingDiceSkill = null;
            _pendingDiceActor = null;
            _diceSkillUses.Clear();
            _firstUseInput.Clear();
            _offenseDefenseUsed.Clear();
            currentAP = 0;
            RefreshDiceUI();
            RefreshSkillSlots();
        }
        else
        {
            currentAP = MaxAP;

            // [에너지 저장소] 이월 행동력 추가
            if (_carriedOverAP > 0)
            {
                currentAP += _carriedOverAP;
                Notify($"[에너지 저장소] 이월 행동력 +{_carriedOverAP} 추가!", Color.cyan);
                _carriedOverAP = 0;
            }

            // [과부하 모듈] 잠금 처리
            if (_nextTurnLockedAP > 0)
            {
                int locked = _nextTurnLockedAP;
                currentAP = Mathf.Max(0, currentAP - locked);
                Notify($"[과부하 모듈] 이전 턴 과부하로 행동력 -{locked} 잠김! (이번 턴 과부하 불가)", Color.red);
                _nextTurnLockedAP = 0;
                _canOverloadThisTurn = false;
            }
            else
            {
                _canOverloadThisTurn = true;
            }
            _overloadedThisTurn = false;
        }

        UpdateAPUI();
        CancelPendingSelection();

        EffectEngine.ResetTurnBurnCounts(_turnCount);

        // [빨간 구두] 유물: 턴 시작 시 출혈을 보유하지 않은 모든 적에게 출혈 2 부여
        if (TheLastArk.Managers.ResourceManager.Instance != null &&
            TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.RedShoes))
        {
            foreach (var enemy in enemyParty)
            {
                if (enemy != null && enemy.status != null && enemy.status.currentHp > 0)
                {
                    bool hasBleed = enemy.status.activeStatusEffects.Exists(e => e.effectType == EffectType.Bleed);
                    if (!hasBleed)
                    {
                        enemy.status.ApplyStatusEffect(EffectType.Bleed, RelicRules.Value(TheLastArk.Data.RelicEffectType.RedShoes), 3);
                        Debug.Log($"[빨간 구두] {enemy.characterName}에게 출혈 2 부여!");
                    }
                }
            }
        }

        StartCoroutine(ResolvePanicTurnStart());
    }

    private IEnumerator RunEnemyTurn()
    {
        RemoveOwnerPhaseEffects(enemyParty);
        _vengeanceCounts.Clear();
        ClearEnemyTargetMarkers();
        Debug.Log($"[Battle] {_turnCount}턴: 적 턴 시작. 생존 적 수 확인 중...");
        Debug.Log($"[Battle] enemyParty 총 수: {enemyParty.Count}");

        int aliveEnemies = 0;
        foreach (BattleCharacter enemy in enemyParty)
        {
            if (enemy != null && enemy.status.currentHp > 0)
                aliveEnemies++;
        }
        Debug.Log($"[Battle] 생존 적: {aliveEnemies}명");

        if (aliveEnemies == 0)
        {
            Debug.LogWarning("[Battle] 생존 적이 없습니다. 상태이상 처리로 넘어갑니다.");
            TickSideTurnEnd(enemyParty);
            EnterPhase(BattlePhase.StatusEffect);
            yield break;
        }

        // Summons join the next phase, and cannot invalidate this phase's enumeration.
        foreach (BattleCharacter enemy in enemyParty.FindAll(member => member != null && member.status != null && member.status.currentHp > 0f))
        {
            if (enemy == null)
            {
                Debug.LogWarning("[Battle] Enemy가 null입니다.");
                continue;
            }

            if (enemy.status.currentHp <= 0)
            {
                Debug.Log($"[Battle] {enemy.characterName}은 이미 사망했습니다.");
                continue;
            }

            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai == null)
            {
                Debug.LogError($"[Battle] {enemy.characterName}에 EnemyAI 컴포넌트가 없습니다.");
                continue;
            }

            Debug.Log($"[Battle] {enemy.characterName}이 행동합니다.");
            if (!ai.HasPreparedTurn && !ai.PrepareTurn()) continue;

            var preparedTargets = new List<BattleCharacter>(ai.PreparedTargets);
            SetEnemyTargetMarkers(preparedTargets, true);
            yield return new WaitForSeconds(EnemyActionDelay);
            ai.ExecuteTurn();
            SetEnemyTargetMarkers(preparedTargets, false);
            enemy.GetComponent<EnemyIntentUI>()?.Hide();

            if (!DebugHoldsBattleOpen && IsPartyWiped(playerParty))
            {
                Debug.Log("[Battle] 플레이어 파티 전멸. 전투 종료로 넘어갑니다.");
                EnterPhase(BattlePhase.BattleEnd);
                yield break;
            }
        }

        Debug.Log("[Battle] 적 턴 완료. 상태이상 처리로 넘어갑니다.");
        ApplySeizure(enemyParty);
        TickSideTurnEnd(enemyParty);
        EnterPhase(BattlePhase.StatusEffect);
    }

    private IEnumerator RunStatusEffectPhase()
    {
        Debug.Log($"[Battle] {_turnCount}턴: 상태이상 처리 시작");

        var allCombatants = new List<BattleCharacter>(playerParty);
        allCombatants.AddRange(enemyParty);
        StatusEffectPhaseHandler.ProcessAll(allCombatants);

        yield return new WaitForSeconds(StatusEffectDelay);
        Debug.Log("[Battle] 상태이상 처리 완료. 턴 종료 단계로 넘어갑니다.");
        EnterPhase(BattlePhase.TurnEnd);
    }

    private void OnEnterTurnEnd()
    {
        Debug.Log($"[Battle] {_turnCount}턴: 턴 종료 단계");
        RemoveDeadCharacters();

        if (TryEndBattleIfPartyWiped()) return;

        _turnCount++;
        Debug.Log($"[Battle] 턴 카운트 증가: {_turnCount}턴으로 진입");
        UpdateTurnUI();
        EnterPhase(BattlePhase.PlayerTurn);
    }

    /// <summary>Turn effects (other than round-end DoTs) decrement at their own side's turn end.</summary>
    private static void TickSideTurnEnd(List<BattleCharacter> party)
    {
        if (party == null) return;
        foreach (BattleCharacter character in party)
        {
            if (character?.status == null) continue;
            character.status.TickSideTurnEnd();
            character.view?.UpdateVisual(character.status);
        }
    }

    private static void RemoveOwnerPhaseEffects(List<BattleCharacter> party)
    {
        if (party == null) return;

        foreach (BattleCharacter character in party)
        {
            if (character?.status == null) continue;
            int removed = character.status.RemoveEffectsExpiringAtOwnerPhaseStart();
            if (removed <= 0) continue;

            Debug.Log($"[StatusEffect] {character.characterName}의 다음 자기 턴 지속 효과 {removed}개 종료");
            if (character.view != null)
                character.view.UpdateVisual(character.status);
        }
    }

    private void OnEnterBattleEnd()
    {
        _diceCharge.Clear();
        _parasiticStacks = 0;
        _carriedDice.Clear();
        _blueTowerSkills.Clear();
        // Battle statuses (permanent ones included) never carry into the next battle.
        foreach (BattleCharacter ally in playerParty)
            ally?.status?.RemoveAllStatusEffects();
        ClearBattlePanic();
        CancelPendingSelection();
        ClearEnemyTargetMarkers();
        ClearEnemyIntents();
        bool playerWin = IsPartyWiped(enemyParty);
        Debug.Log($"[Battle] 전투 종료: {(playerWin ? "승리" : "패배")}");

        // [의무실] 전투 승리 시 체력/정신력 회복 및 소생 적용
        if (playerWin && TheLastArk.Managers.TrainManager.IsInitialized)
        {
            TheLastArk.Managers.TrainManager.Instance.ApplyInfirmaryPostBattleEffect(playerParty);
        }

        if (BattleResultUIManager.Instance != null)
        {
            if (playerWin)
            {
                BattleResultUIManager.Instance.ShowVictoryScreen(
                    RunManager.Instance.CurrentEncounterPool,
                    VictoryGold,
                    LoadMapScene);
            }
            else
                BattleResultUIManager.Instance.ShowDefeatScreen(LoadMapScene);
            return;
        }

        if (playerWin) GiveVictoryRewards();
        Notify(playerWin ? "승리!" : "패배...", playerWin ? Color.cyan : Color.red);
        Invoke(nameof(LoadMapScene), 2f);
    }

    private void GiveVictoryRewards()
    {
        if (VictoryGold <= 0) return;

        ResourceManager.Instance.AddGold(VictoryGold);
        Debug.Log($"[Battle] 승리 보상 지급: 골드 +{VictoryGold}");
    }

    private void LoadMapScene()
    {
        Debug.Log($"[Battle] {MapSceneName}으로 이동합니다.");
        UnityEngine.SceneManagement.SceneManager.LoadScene(MapSceneName);
    }

    public void UpdateAPUI()
    {
        if (apText == null) return;

        TMPFontManager.ApplyFont(apText);
        if (UsesDice)
        {
            int available = 0;
            foreach (ActionDie die in _dicePool.Dice)
                if (die.IsAvailable) available++;
            apText.text = $"주사위 {available}<size=55%><color=#8CAFC8> / {ActionDicePool.MaxStoredDice}</color></size>";
            return;
        }
        apText.text = $"{currentAP}<size=55%><color=#8CAFC8> / {MaxAP}</color></size>";
    }

    private void SetEnemyTargetMarkers(IEnumerable<BattleCharacter> targets, bool visible)
    {
        if (targets == null || playerParty == null) return;

        foreach (BattleCharacter target in targets)
        {
            if (target == null || target.view == null || !playerParty.Contains(target)) continue;
            target.view.SetEnemyTargeted(visible);
        }
    }

    private void ClearEnemyTargetMarkers()
    {
        SetEnemyTargetMarkers(playerParty, false);
    }

    private void PrepareEnemyIntents()
    {
        if (enemyParty == null) return;

        foreach (BattleCharacter enemy in enemyParty)
        {
            if (enemy == null || enemy.status == null || enemy.status.currentHp <= 0f) continue;
            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai == null) continue;

            ai.targetParty = playerParty;
            ai.allyParty = enemyParty;
            EnemyIntentUI ui = enemy.GetComponent<EnemyIntentUI>();
            if (ui == null) ui = enemy.gameObject.AddComponent<EnemyIntentUI>();
            if (enemy.GetComponent<EnemyEnhancementUI>() == null) enemy.gameObject.AddComponent<EnemyEnhancementUI>();

            if (ai.PrepareTurn())
            {
                ui.Show(ai.PreparedIntent);
                SetEnemyTargetMarkers(ai.PreparedTargets, true);
            }
            else ui.Hide();
        }
    }

    private void ClearEnemyIntents()
    {
        if (enemyParty == null) return;
        foreach (BattleCharacter enemy in enemyParty)
            enemy?.GetComponent<EnemyIntentUI>()?.Hide();
    }

    private void CacheTurnEndButton()
    {
        Debug.Log("[Battle] CacheTurnEndButton() 호출");

        if (turnEndButton == null)
        {
            Debug.Log("[Battle] turnEndButton이 null입니다. TurnEndButton 오브젝트를 검색합니다.");
            GameObject buttonObject = GameObject.Find("TurnEndButton");
            if (buttonObject == null)
            {
                Debug.LogWarning("[Battle] TurnEndButton 오브젝트를 찾지 못했습니다.");
                return;
            }

            Debug.Log("[Battle] TurnEndButton GameObject 찾음");
            turnEndButton = buttonObject.GetComponent<UnityEngine.UI.Button>();
        }

        if (turnEndButton == null)
        {
            Debug.LogWarning("[Battle] TurnEndButton 오브젝트에 Button 컴포넌트가 없습니다.");
            return;
        }

        Debug.Log("[Battle] TurnEndButton Button 컴포넌트 획득");

        if (_turnEndButtonListenerBound)
        {
            Debug.Log("[Battle] 턴 종료 버튼 리스너가 이미 등록되어 있습니다.");
            return;
        }

        turnEndButton.onClick.RemoveListener(EndPlayerTurn);
        turnEndButton.onClick.AddListener(EndPlayerTurn);
        _turnEndButtonListenerBound = true;
        Debug.Log("[Battle] EndPlayerTurn 리스너 등록 완료");
    }

    public void UpdateTurnEndButtonState()
    {
        if (turnEndButton == null)
        {
            Debug.LogWarning("[Battle] turnEndButton이 null이어서 Interactable을 설정할 수 없습니다.");
            return;
        }

        bool shouldBeInteractable = IsPlayerTurn;
        if (turnEndButton.interactable != shouldBeInteractable)
        {
            Debug.Log($"[Battle] TurnEndButton Interactable 변경: {turnEndButton.interactable} -> {shouldBeInteractable}");
            turnEndButton.interactable = shouldBeInteractable;
        }
    }

    private void UpdateTurnUI()
    {
        if (turnCountText == null) return;

        TMPFontManager.ApplyFont(turnCountText);
        if (_turnClock != null) _turnClock.SetTurn(_turnCount);
        else turnCountText.text = $"{_turnCount}턴";
    }

    private bool IsPartyWiped(List<BattleCharacter> party)
    {
        return party.TrueForAll(c => c == null || c.status.currentHp <= 0);
    }

    private bool TryEndBattleIfPartyWiped()
    {
        if (DebugHoldsBattleOpen) return false;
        if (IsPartyWiped(playerParty))
        {
            Debug.LogWarning("[Battle] 플레이어 파티 전멸.");
            EnterPhase(BattlePhase.BattleEnd);
            return true;
        }

        if (IsPartyWiped(enemyParty))
        {
            Debug.LogWarning("[Battle] 적 파티 전멸.");
            EnterPhase(BattlePhase.BattleEnd);
            return true;
        }

        return false;
    }

    private void RemoveDeadCharacters()
    {
        foreach (BattleCharacter character in playerParty)
        {
            if (character != null && character.status.currentHp <= 0)
                Debug.Log($"[Battle] {character.characterName} is down.");
        }

        foreach (BattleCharacter character in enemyParty)
        {
            if (character != null && character.status.currentHp <= 0)
                Debug.Log($"[Battle] {character.characterName} is down.");
        }
    }
}
