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

        List<SkillInfo> pool = new List<SkillInfo>(status.origin.activeSkills);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            SkillInfo temp = pool[i];
            pool[i] = pool[rnd];
            pool[rnd] = temp;
        }

        int countToExtract = 2 + (isLeader ? 1 : 0);
        status.dynamicActiveSkill.Clear();
        for (int i = 0; i < countToExtract; i++)
        {
            if (i < pool.Count) status.dynamicActiveSkill.Add(pool[i]);
        }
    }

    public float ReceiveDamage(float amount, BattleCharacter attacker)
    {
        if (status == null) return 0;

        float actualDamage = amount;
        status.currentHp -= actualDamage;
        if (status.currentHp < 0) status.currentHp = 0;

        Debug.Log($"{gameObject.name} 피격! 남은 체력: {status.currentHp}");
        if (view != null) view.UpdateVisual(status);
        return actualDamage;
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

        status.currentHp -= 50;
        if (status.currentHp < 0) status.currentHp = 0;

        if (view != null) view.UpdateVisual(status);
        Debug.Log($"{characterName}이(가) 테스트를 위해 자해했습니다. 현재 HP: {status.currentHp}");
    }
}
