using UnityEngine;

public abstract class SkillEffects : ScriptableObject
{
    [Header("Base Settings")]
    public string effectName;

    // 🔥 CharacterView 대신 BattleCharacter를 전달받아 로직 처리를 용이하게 합니다.
    public abstract void Execute(BattleCharacter actor, BattleCharacter target, int skillLevel);
}