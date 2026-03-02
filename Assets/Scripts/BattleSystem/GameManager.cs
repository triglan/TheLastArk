using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Combatants")]
    public List<BattleCharacter> allCharacters; // 아군 + 적군 모두 포함

    [Header("Managers")]
    public BattleSkillManager skillManager;

    public void BeginBattleSetup()
    {
        // Step 1: 선수 입장 및 데이터 준비
        foreach (var character in allCharacters)
        {
            if (character != null) character.PrepareCharacterData();
        }

        // Step 2: 준비된 데이터를 UI에 출력
        if (skillManager != null)
        {
            skillManager.LinkSkillsToUI();
        }

        Debug.Log("=== 배틀 셋업 프로세스 종료 ===");
    }

    private void Start()
    {
        BeginBattleSetup(); // 게임 시작 시 감독관이 지휘 시작
    }
}