using UnityEngine;

// [수정 사항]: SkillEffects를 상속받아 실제 데미지 로직을 구현합니다.
// [수정 사항]: CreateAssetMenu를 통해 유니티 에디터에서 에셋으로 생성 가능하게 합니다.
[CreateAssetMenu(fileName = "NewDamageEffect", menuName = "Battle/Effects/Damage")]
public class DamageEffect : SkillEffects
{
    [Header("Damage Settings")]
    public float damageMultiplier = 1.0f; // 공격력 배율 (1.0 = 100%)
    public DamageType damageType = DamageType.Physical;

    // [수정 사항]: 추상 메서드 Execute를 오버라이드하여 실제 전투 로직을 작성합니다.
    public override void Execute(BattleCharacter actor, BattleCharacter target, int skillLevel)
    {
        if (actor == null || target == null) return;

        // 1. 데미지 계산: (시전자 기본 공격력 * 배율) + (스킬 레벨 보너스)
        float basePower = damageType == DamageType.Magical
            ? actor.status.FinalSpellPower
            : actor.status.FinalAttack;
        float finalDamage = (basePower * damageMultiplier) + (skillLevel * 5);

        // 2. 타겟에게 데미지 전달
        // BattleCharacter에 구현된 ReceiveDamage를 호출하여 HP를 깎습니다.
        target.ReceiveDamage(finalDamage, actor, damageType);


        Debug.Log($"{actor.characterName}이(가) {target.characterName}에게 {finalDamage}의 피해를 입혔습니다!");
    }
}
