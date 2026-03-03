using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public TargetArrow targetHandler;

    [Header("Current Selection")]
    public SkillData selectedSkill;
    public BattleCharacter selectedActor;

    public void SelectSkill(SkillData skill, BattleCharacter actor)
    {
        selectedSkill = skill;
        selectedActor = actor;
        // 여기서 나중에 타겟팅 화살표를 켜주는 로직이 들어갑니다.
    }

    public void PerformSkill()
    {
        if (selectedSkill == null || selectedActor == null || targetHandler.target == null) return;

        BattleCharacter targetCharacter = targetHandler.target.GetComponent<BattleCharacter>();
        if (targetCharacter == null) return;

        // 🔥 현재 캐릭터의 스킬 레벨을 가져오는 로직 (임시로 1 설정)
        int currentLevel = 1;

        // 스킬에 담긴 모든 효과(Effects)를 순서대로 실행합니다.
        foreach (var effect in selectedSkill.effects)

        {
            // 🔥 수정된 매개변수 3개를 모두 전달하여 컴파일 에러를 해결합니다.
            effect.Execute(selectedActor, targetCharacter, currentLevel);
        }

        Debug.Log($"{selectedActor.name}이(가) {targetCharacter.name}에게 {selectedSkill.skillName} 사용!");
        selectedSkill = null;
    }
}