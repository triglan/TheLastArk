using System.Collections.Generic;

[System.Serializable]
public class CharacterStatus
{
    public CharacterData origin; // 어떤 캐릭터인지 원본 참조

    // 실시간 변동 데이터
    public float currentHp;
    public float currentMental;
    public float bonusAttack; // 유물 등으로 추가된 공격력

    // 캐릭터 통합 레벨, 이번판에서 사용할 스킬들
    public int charLevel = 1;
    public List<SkillInfo> dynamicActiveSkill = new List<SkillInfo>();

    // 생성자: 원본 데이터를 복사하여 초기 상태를 만듭니다.
    public CharacterStatus(CharacterData data)
    {
        origin = data;
        currentHp = data.maxHp;
        currentMental = data.maxMental;
        bonusAttack = 0;
        charLevel = 1;

        dynamicActiveSkill = new List<SkillInfo>();
    }
}