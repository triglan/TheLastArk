using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public TargetArrow targetHandler;
    public BattleCharacter attacker; // CharacterView에서 BattleCharacter로 변경

    public void PerformAttack()
    {
        if (targetHandler == null || attacker == null) return;

        // 데이터 참조 경로 수정 (status.origin)
        if (attacker.status == null || attacker.status.origin == null) return;

        GameObject targetObj = targetHandler.target;
        if (targetObj != null)
        {
            IDamageable damageable = targetObj.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float finalDamage = attacker.status.origin.baseAttack + attacker.status.bonusAttack;
                damageable.ReceiveDamage(finalDamage, attacker);
            }
        }
    }
}