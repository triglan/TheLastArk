using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Battle/Character")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string characterName;
    public Sprite standingSprite;
    public Sprite portraitSprite;

    [Header("Base Stats")]
    public float maxHp = 200f;
    public float maxMental = 200f;
    public float baseAttack = 25f;

    [Header("Skills")]
    // 0번 스킬용 패시브 칸을 따로 만듭니다.
    public SkillData passiveSkill;

    // 1~4번 액티브 스킬 배열입니다.
    public SkillData[] activeSkills = new SkillData[4];
}