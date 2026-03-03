using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Battle/Skill")]
public class SkillData : ScriptableObject
{
    public string skillName;
    public Sprite skillIcon;
    public int baseCost; // 생성 시 기본값 2로 할당될 예정입니다.

    [Header("Targeting")]
    public TargetType targetType;

    [Header("Effects")]
    public List<SkillEffects> effects = new List<SkillEffects>();

    public int GetCost(int level)
    {
        // 예시: 특정 조건에서 코스트 감소 로직
        if (skillName == "재정비" && level >= 2) return baseCost - 2;
        return baseCost;
    }
}

public enum TargetType { SingleEnemy, AllEnemies, SingleAlly, AllAllies, Self, AdjacentRight }