using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Battle/Character")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string characterName;
    public Sprite standingSprite; // 리더용 전신 일러스트
    public Sprite portraitSprite; // 동료용 작은 초상화

    [Header("Base Stats")]
    public float maxHp = 200f;
    public float maxMental = 200f;
    public float baseAttack = 25f;

    [Header("Skills")]
    // 각 캐릭터가 가진 4개의 스킬 데이터 (나중에 구현할 SkillData 에셋 연결)
    public SkillData[] skills = new SkillData[4];
}