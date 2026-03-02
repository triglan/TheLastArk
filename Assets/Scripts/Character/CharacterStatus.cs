[System.Serializable]
public class CharacterStatus
{
    public CharacterData origin; // 어떤 캐릭터인지 원본 참조

    // 실시간 변동 데이터
    public float currentHp;
    public float currentMental;
    public float bonusAttack; // 유물 등으로 추가된 공격력
    public int[] skillLevels = new int[4]; // 각 스킬의 강화 단계 (0, 1, 2)

    // 생성자: 원본 데이터를 복사하여 초기 상태를 만듭니다.
    public CharacterStatus(CharacterData data)
    {
        origin = data;
        currentHp = data.maxHp;
        currentMental = data.maxMental;
        bonusAttack = 0;
        for (int i = 0; i < skillLevels.Length; i++) skillLevels[i] = 0;
    }
}