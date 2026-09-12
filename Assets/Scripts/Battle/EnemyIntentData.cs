using System.Collections.Generic;
using UnityEngine;

public enum EnemyIntentVisualType { Attack, Defend, Support, Special }

public sealed class EnemyIntentData
{
    public string PatternName;
    public EnemyIntentVisualType VisualType;
    public TargetType TargetType;
    public EnemyTargetSelection TargetSelection;
    public List<BattleCharacter> Targets = new List<BattleCharacter>();
    public List<EnemyIntentEffectData> Effects = new List<EnemyIntentEffectData>();
}

public sealed class EnemyIntentEffectData
{
    public Sprite Icon;
    public EnemyPatternActionType ActionType;
    public EffectType EffectType;
    public DamageType DamageType;
    public TargetType TargetType;
    public EnemyTargetSelection TargetSelection;
    public float Value;
    public int HitCount = 1;
    public int Duration;
    public string RuntimeVariableId;
    public string RuntimeDisplayName;
    public int RuntimeValue;
    public bool SetRuntimeValue;
}
