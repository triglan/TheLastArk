using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Combatants")]
    public List<BattleCharacter> allCharacters;

    [Header("Managers")]
    public BattleSkillManager skillManager;
    public BattleManager battleManager; // 배틀 매니저 참조 추가

    public void BeginBattleSetup()
    {
        // Step 1: 선수 입장 및 데이터 준비
        foreach (var character in allCharacters)
        {
            if (character != null) character.PrepareCharacterData();
        }

        // Step 2: 공용 행동력 시스템 초기화 (실행 순서 보장)
        if (battleManager != null)
        {
            battleManager.InitializeAP();
        }

        // Step 3: 준비된 데이터를 UI에 출력
        if (skillManager != null)
        {
            skillManager.LinkSkillsToUI();
        }

        Debug.Log("=== 배틀 셋업 프로세스 종료 ===");
    }

    private void Start()
    {
        BeginBattleSetup();
    }
}