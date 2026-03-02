using UnityEngine;

// 모든 스킬 효과의 기반이 되는 추상 클래스입니다.
public abstract class SkillEffects : ScriptableObject
{
    [Header("Base Settings")]
    public string effectName;

    // 효과가 실제로 실행될 로직을 정의합니다. (2단계에서 구체화)
    public abstract void Execute(CharacterView actor, GameObject target, int skillLevel);
}