using UnityEngine;

public abstract class SkillEffects : ScriptableObject
{
    [Header("Base Settings")]
    public string effectName;

    public abstract void Execute(BattleCharacter actor, BattleCharacter target, int skillLevel);
}