using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Battle/Skill")]
public class SkillData : ScriptableObject
{
    public string skillName;
    public Sprite skillIcon;
    public int baseCost;
    public bool isOneTime; // 일회성 여부

    [Header("Targeting")]
    public TargetType targetType; // 단일, 전체, 자신 등

    [Header("Effects")]
    // 이 스킬이 가진 효과 리스트입니다. (예: 데미지 + 기절)
    public List<SkillEffects> effects = new List<SkillEffects>();

    // 강화 단계에 따른 비용 변화 등을 위해 사용합니다.
    public int GetCost(int level)
    {
        // 재정비 스킬처럼 레벨에 따라 코스트가 줄어드는 로직 등을 구현합니다.
        if (skillName == "재정비" && level >= 2) return baseCost - 2;
        return baseCost;
    }
}

public enum TargetType { SingleEnemy, AllEnemies, SingleAlly, AllAllies, Self, AdjacentRight }