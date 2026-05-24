using UnityEngine;
using System.Collections.Generic;

// 실제 전투를 수행하고 수치를 관리하는 주체입니다.
public class BattleCharacter : MonoBehaviour, IDamageable
{
    public CharacterStatus status; // 실시간 수치 데이터
    public CharacterView view;     // 연결된 시각적 컴포넌트
    public bool isLeader;

    // 캐릭터 이름 쉽게 불러오기
    public string characterName => (status != null && status.origin != null) ? status.origin.characterName : gameObject.name;

    public void Init(CharacterData data, bool leaderStatus)
    {
        isLeader = leaderStatus;
        status = new CharacterStatus(data);
        DraftSkills();
        if (view == null) view = GetComponent<CharacterView>();
        view.UpdateVisual(status); // 초기 시각적 상태 반영
    }

    private void DraftSkills()
    { 
        if (status.origin == null || status.origin.activeSkills == null) return;
        List<SkillInfo> pool = new List<SkillInfo>(status.origin.activeSkills);// 원본 4개 스킬을 복사해서 리스트

        // 셔플 (Fisher-Yates 알고리즘)
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            SkillInfo temp = pool[i];
            pool[i] = pool[rnd];
            pool[rnd] = temp;
        }

        // 기본 2칸 + 리더 1칸 + (추후 유물 보너스)
        int countToExtract = 2 + (isLeader ? 1 : 0);
        //countToExtract = pool.Count; // 디버깅용 스킬칸 4개 쓰기

        status.dynamicActiveSkill.Clear();
        for (int i = 0; i < countToExtract; i++)
        {
            if (i < pool.Count) status.dynamicActiveSkill.Add(pool[i]);
        }
    }

    public float ReceiveDamage(float amount, BattleCharacter attacker)
    {
        if (status == null) return 0;

        // TODO: 여기에 방어력(Defense)이나 저항력 계산 로직이 들어감
        float actualDamage = amount;

        status.currentHp -= actualDamage;
        if (status.currentHp < 0) status.currentHp = 0;

        Debug.Log($"{gameObject.name} 피격! 남은 체력: {status.currentHp}");

        view.UpdateVisual(status);// 피격 후 화면 갱신을 View에게 요청
        return actualDamage;
    }

    public float ReceiveHeal(float amount, BattleCharacter healer)
    {
        if (status == null) return 0f;

        float beforeHp = status.currentHp;
        status.currentHp += amount;
        if (status.currentHp > status.FinalMaxHp)
            status.currentHp = status.FinalMaxHp;

        float actualHeal = status.currentHp - beforeHp; // 오버힐 제외 실제 회복량
        view.UpdateVisual(status);

        Debug.Log($"{characterName}이(가) {healer.characterName}에게 {amount}만큼 회복받음! (현재 HP: {status.currentHp})");
        return actualHeal;
    }

    [Header("Testing")]
    public CharacterData testData;

    public void PrepareCharacterData()
    {
        if (testData == null) return;
        Init(testData, isLeader);
    }

    public void ChangeLevel(int newLevel)
    {
        float oldMaxHp = status.FinalMaxHp;
        status.charLevel = Mathf.Clamp(newLevel, 0, 4);
        float newMaxHp = status.FinalMaxHp;

        // 증가한 체력만큼 회복
        float diff = newMaxHp - oldMaxHp;
        if (diff > 0) status.currentHp += diff;

        view.UpdateVisual(status);
        Debug.Log($"{characterName} 강화 단계 변경: {status.charLevel}강. 체력 {diff} 증가.");
    }

    [ContextMenu("Debug: Take 50 Damage")] // 컴포넌트 우클릭으로 실행
    public void DebugTakeDamage()
    {
        if (status == null) return;

        status.currentHp -= 50;
        if (status.currentHp < 0) status.currentHp = 0;

        view.UpdateVisual(status); // UI 갱신
        Debug.Log($"{characterName}이(가) 테스트를 위해 자해했습니다. 현재 HP: {status.currentHp}");
    }
}