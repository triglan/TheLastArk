using UnityEngine;
using System.Collections.Generic;

public class BattleCharacter : MonoBehaviour, IDamageable
{
    public CharacterStatus status;
    public CharacterView view;
    public bool isLeader;

    public string characterName => (status != null && status.origin != null) ? status.origin.characterName : gameObject.name;

    public void Init(CharacterData data, bool leaderStatus)
    {
        isLeader = leaderStatus;
        status = new CharacterStatus(data);
        DraftSkills();
        if (view == null) view = GetComponent<CharacterView>();
        if (view != null) view.UpdateVisual(status);
    }

    private void DraftSkills()
    {
        if (status?.origin == null || status.origin.isEnemy || status.origin.activeSkills == null) return;

        status.dynamicActiveSkill.Clear();

        List<int> selectedIndices = status.selectedActiveSkillIndices;
        if (selectedIndices == null || selectedIndices.Count < 2)
        {
            selectedIndices = new List<int>() { 0, 1 };
        }

        // 1. 선택된 2개의 액티브 스킬 탑재
        foreach (int idx in selectedIndices)
        {
            if (idx >= 0 && idx < status.origin.activeSkills.Length && status.origin.activeSkills[idx] != null)
            {
                status.dynamicActiveSkill.Add(status.origin.activeSkills[idx]);
            }
        }

        // 2. 리더인 경우 고정 추가 3번째 스킬 추가 탑재
        if (isLeader)
        {
            int extraIdx = status.EnsureLeaderExtraSkill();
            if (extraIdx >= 0 && extraIdx < status.origin.activeSkills.Length && status.origin.activeSkills[extraIdx] != null)
            {
                if (!status.dynamicActiveSkill.Contains(status.origin.activeSkills[extraIdx]))
                {
                    status.dynamicActiveSkill.Add(status.origin.activeSkills[extraIdx]);
                }
            }
        }
    }

    [HideInInspector] public bool hasResurrectedThisStage = false;

    public float ReceiveDamage(float amount, BattleCharacter attacker)
    {
        return ReceiveDamage(amount, attacker, DamageType.Physical);
    }

    public float ReceiveDamage(float amount, BattleCharacter attacker, DamageType damageType)
    {
        if (status == null || amount <= 0f) return 0f;

        float defense = damageType switch
        {
            DamageType.Physical => Mathf.Max(0f, status.FinalArmor),
            DamageType.Magical => Mathf.Max(0f, status.FinalMagicResist),
            _ => 0f
        };
        float actualDamage = damageType == DamageType.True
            ? amount
            : Mathf.Max(1f, amount - defense);
        status.currentHp -= actualDamage;

        // 특성 [회생] 발동 검사 (체력 1 이하일 때 발동)
        if (status.currentHp <= 1f)
        {
            TryTriggerResurrectionTrait();
        }

        if (status.currentHp < 0) status.currentHp = 0;

        Debug.Log($"{gameObject.name} 피격! 남은 체력: {status.currentHp}");
        if (view != null) view.UpdateVisual(status);
        return actualDamage;
    }

    public void TryTriggerResurrectionTrait()
    {
        if (status == null || status.origin == null) return;

        bool canResurrect = false;

        // 1. 본인이 회생 특성 보유 & 1강 이상(개방) 상태인 경우
        if (!hasResurrectedThisStage && status.IsTraitUnlocked && status.origin.passiveSkill != null &&
            !string.IsNullOrEmpty(status.origin.passiveSkill.skillName) && status.origin.passiveSkill.skillName.Contains("회생"))
        {
            canResurrect = true;
        }
        // 2. 본인이 개방되지 않았더라도 파티원 중 4강(각성 개화) 회생 특성을 가진 아군이 있는 경우
        else if (!hasResurrectedThisStage)
        {
            var bm = FindObjectOfType<BattleManager>();
            if (bm != null && bm.playerParty != null)
            {
                foreach (var ally in bm.playerParty)
                {
                    if (ally != null && ally.status != null && ally.status.IsTraitAwakened && ally.status.origin != null &&
                        ally.status.origin.passiveSkill != null && !string.IsNullOrEmpty(ally.status.origin.passiveSkill.skillName) &&
                        ally.status.origin.passiveSkill.skillName.Contains("회생"))
                    {
                        canResurrect = true;
                        break;
                    }
                }
            }
        }

        if (canResurrect && status.currentMental > 1f)
        {
            hasResurrectedThisStage = true;
            status.currentHp = 1f;

            float consumedMental = status.currentMental - 1f;
            status.currentMental = 1f;

            float healAmount = consumedMental * 2f; // 소모한 정신력의 200%만큼 체력 회복
            ReceiveHeal(healAmount, this);

            Debug.Log($"✨ [{characterName}] 특성 [회생] 발동! 정신력 {consumedMental} 소모 -> 체력 {healAmount} 회복!");
        }
    }

    public float ReceiveHeal(float amount, BattleCharacter healer)
    {
        if (status == null) return 0f;

        float beforeHp = status.currentHp;
        status.currentHp += amount;
        if (status.currentHp > status.FinalMaxHp)
            status.currentHp = status.FinalMaxHp;

        float actualHeal = status.currentHp - beforeHp;
        if (view != null) view.UpdateVisual(status);

        string healerName = healer != null ? healer.characterName : "System";
        Debug.Log($"{characterName}이(가) {healerName}에게 {amount}만큼 회복받음! (현재 HP: {status.currentHp})");
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
        if (status == null || status.origin == null || status.origin.isEnemy) return;

        float oldMaxHp = status.FinalMaxHp;
        status.charLevel = Mathf.Clamp(newLevel, 0, 4);
        float newMaxHp = status.FinalMaxHp;

        float diff = newMaxHp - oldMaxHp;
        if (diff > 0) status.currentHp += diff;

        if (view != null) view.UpdateVisual(status);
        Debug.Log($"{characterName} 강화 단계 변경: {status.charLevel}강. 체력 {diff} 증가.");
    }

    [ContextMenu("Debug: Take 50 Damage")]
    public void DebugTakeDamage()
    {
        if (status == null) return;

        ReceiveDamage(50f, null, DamageType.True);
        Debug.Log($"{characterName}이(가) 테스트를 위해 자해했습니다. 현재 HP: {status.currentHp}");
    }
}
