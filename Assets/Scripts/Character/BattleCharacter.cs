using UnityEngine;

// 실제 전투를 수행하고 수치를 관리하는 주체입니다.
public class BattleCharacter : MonoBehaviour, IDamageable
{
    public CharacterStatus status; // 실시간 수치 데이터
    public CharacterView view;     // 연결된 시각적 컴포넌트

    // 캐릭터 이름 쉽게 불러오기
    public string characterName => (status != null && status.origin != null) ? status.origin.characterName : gameObject.name;

    public void Init(CharacterData data)
    {
        status = new CharacterStatus(data);
        if (view == null) view = GetComponent<CharacterView>();

        // 초기 시각적 상태 반영
        view.UpdateVisual(status);
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

        // 2. 시각적 요소 연결 및 초기화
        if (view == null) view = GetComponent<CharacterView>();
        view.UpdateVisual(status);
    }
}