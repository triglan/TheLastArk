using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UI;

public class GameManager : MonoBehaviour
{
    [Header("Combatants")]
    public List<BattleCharacter> allCharacters;

    [Header("Managers")]
    public BattleSkillManager skillManager;
    public BattleManager battleManager; // 배틀 매니저 참조

    private BattleEndButton _battleEndButton;

    public void BeginBattleSetup()
    {
        if (battleManager != null)
        {
            // 전투 결과 화면이 없으면 자동으로 붙입니다.
            if (FindObjectOfType<BattleResultUIManager>() == null)
                battleManager.gameObject.AddComponent<BattleResultUIManager>();
        }

        // Step 0.5: Hierarchy의 TurnEndButton 찾기 및 연결
        Debug.Log("[GameManager] 🎬 TurnEndButton 버튼 연결");
        GameObject turnEndButtonObj = GameObject.Find("TurnEndButton");
        if (turnEndButtonObj != null)
        {
            Button turnEndButton = turnEndButtonObj.GetComponent<Button>();
            if (turnEndButton != null)
            {
                battleManager.turnEndButton = turnEndButton;
                turnEndButton.onClick.RemoveAllListeners();
                turnEndButton.onClick.AddListener(battleManager.EndPlayerTurn);
                
                // ✅ 추가: 버튼을 명시적으로 활성화
                turnEndButton.interactable = true;
                turnEndButtonObj.SetActive(true);
                
                Debug.Log("[GameManager] ✓ TurnEndButton 버튼 연결 완료");
            }
            else
            {
                Debug.LogError("[GameManager] ❌ TurnEndButton GameObject에 Button 컴포넌트가 없습니다!");
            }
        }
        else
        {
            Debug.LogError("[GameManager] ❌ Hierarchy에서 'TurnEndButton'을 찾을 수 없습니다!");
        }

        // Step 1: 전투 캐릭터 데이터를 준비합니다.
        Debug.Log("[GameManager] 📊 캐릭터 데이터 초기화");
        foreach (var character in allCharacters)
        {
            if (character == null) continue;

            EnemyBattleCharacter enemyBC = character.GetComponent<EnemyBattleCharacter>();
            if (enemyBC != null)
            {
                enemyBC.InitializeForBattle();
                continue;
            }

            character.PrepareCharacterData();
        }

        // Step 2: 공용 행동력 시스템 초기화
        Debug.Log("[GameManager] ⚡ BattleManager 행동력 초기화");
        if (battleManager != null)
        {
            battleManager.InitializeAP();
        }

        // Step 3: 준비된 데이터를 UI에 출력합니다.
        Debug.Log("[GameManager] 🎨 스킬 UI 연결");
        if (skillManager != null)
        {
            skillManager.LinkSkillsToUI();
        }

        Debug.Log("[GameManager] ✓ BeginBattleSetup() 완료");
    }

    private void Start()
    {
        BeginBattleSetup();
    }
}