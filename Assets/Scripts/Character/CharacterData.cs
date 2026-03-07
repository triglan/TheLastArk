using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Battle/Character")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public Sprite standingSprite; // [복구]: 오류 해결을 위해 추가
    public Sprite portraitSprite; // [복구]: 오류 해결을 위해 추가

    [Header("Base Stats")]
    public float maxHp = 200f;
    public float maxMental = 200f;
    public float baseAttack = 25f;

    [Header("Skills")]
    public SkillInfo passiveSkill;
    public SkillInfo[] activeSkills = new SkillInfo[4];
}

[System.Serializable]
public class SkillInfo
{
    public string skillName;
    public Sprite skillIcon;
    public int baseCost = 3;
    public SkillLevelData[] levels = new SkillLevelData[3];
}

[System.Serializable]
public class SkillLevelData
{
    public int overrideCost = -1;
    public TargetType targetType;
    public List<EffectEntry> effects = new List<EffectEntry>();
}

[System.Serializable]
public class EffectEntry
{
    public EffectType type;
    public float multiplier = 1.0f;
    public float fixedValue = 0f;
    public bool baseOnLastEffect = false;
}

// [수정]: 사용자님이 정의한 명칭 그대로 반영 (단수/복수 오타 방지)
public enum TargetType { SingleEnemy, LeftEnemy, RightEnemy, AdjacentEnemy, AllEnemy, Friendly, AllFriendly }
public enum EffectType { Damage, Heal, Buff, Stun, Bleed }