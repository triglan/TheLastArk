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

    public void Init(CharacterData data)
    {
        status = new CharacterStatus(data);

        DraftSkills();

        if (view == null) view = GetComponent<CharacterView>();

        // 초기 시각적 상태 반영
        view.UpdateVisual(status);
    }

    private void DraftSkills()
    {
        if (status.origin == null || status.origin.activeSkills == null) return;

        // 원본 4개 스킬을 복사해서 리스트로 만듭니다.
        List<SkillInfo> pool = new List<SkillInfo>(status.origin.activeSkills);

        // 셔플 (Fisher-Yates 알고리즘)
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            SkillInfo temp = pool[i];
            pool[i] = pool[rnd];
            pool[rnd] = temp;
        }

        // 리더면 3개, 아니면 2개 추출
        int countToExtract = 2;
        if(isLeader) countToExtract += 1;

        // 주머니(dynamicActiveSkill)에 담기
        for (int i = 0; i < countToExtract; i++)
        {
            if (i < pool.Count)
            {
                status.dynamicActiveSkill.Add(pool[i]);
            }
        }

        Debug.Log($"{characterName} 드래프트 완료: {countToExtract}개 스킬 배정됨.");
    }

    public void ReceiveDamage(float amount, BattleCharacter attacker)
    {
        if (status == null) return;

        status.currentHp -= amount;
        if (status.currentHp < 0) status.currentHp = 0;

        // 피격 후 화면 갱신을 View에게 요청합니다.
        view.UpdateVisual(status);
        Debug.Log($"{gameObject.name} 피격! 남은 체력: {status.currentHp}");
    }

    [Header("Testing")]
    public CharacterData testData;

    public void PrepareCharacterData()
    {
        if (testData == null) return;

        // 1. 설계도에서 실시간 데이터 생성
        status = new CharacterStatus(testData);

        DraftSkills();

        // 2. 시각적 요소 연결 및 초기화
        if (view == null) view = GetComponent<CharacterView>();
        view.UpdateVisual(status);

        Debug.Log($"[테스트] {characterName} 초기화 완료. 리더여부: {isLeader}, 스킬개수: {status.dynamicActiveSkill.Count}");
    }
}