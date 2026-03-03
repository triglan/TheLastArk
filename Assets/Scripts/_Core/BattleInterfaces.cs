// 데미지를 입을 수 있는 대상이 가져야 할 규칙입니다.
public interface IDamageable
{
    void ReceiveDamage(float amount, BattleCharacter attacker);
}

// 회복을 받을 수 있는 대상이 가져야 할 규칙입니다. (미래 확장용)
public interface IHealable
{
    void ReceiveHeal(float amount, BattleCharacter actor);
}