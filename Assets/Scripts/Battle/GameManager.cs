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

        // Step 1: 전투 캐릭터 데이터를 준비합니다 (덱에 편성된 캐릭터만 스폰)
        Debug.Log("[GameManager] 📊 캐릭터 데이터 초기화 및 덱 스폰 연동");
        ApplySelectedEnemyEncounter();

        var partyIDs = RunManager.Instance != null ? RunManager.Instance.State.partyDataIDs : new List<string>();

        // 최소 1명 필수 및 1명일 때는 무조건 리더 지정
        if (partyIDs == null || partyIDs.Count == 0)
        {
            partyIDs = new List<string>();
            // Fallback: 씬에 테스트 데이터가 있으면 기본 등록
            foreach (var c in allCharacters)
            {
                if (c != null && c.GetComponent<EnemyBattleCharacter>() == null && c.testData != null)
                {
                    partyIDs.Add(c.testData.DataId);
                }
            }
            if (RunManager.Instance != null && partyIDs.Count > 0)
            {
                RunManager.Instance.State.partyDataIDs = partyIDs;
            }
        }

        if (partyIDs.Count == 1 && RunManager.Instance != null)
        {
            RunManager.Instance.State.leaderCharacterID = partyIDs[0];
        }

        string leaderID = RunManager.Instance != null ? RunManager.Instance.State.leaderCharacterID : "";

        // 씬 내 아군 캐릭터 오브젝트와 적 캐릭터 오브젝트 구분
        List<BattleCharacter> playerSlots = new List<BattleCharacter>();
        foreach (var character in allCharacters)
        {
            if (character == null) continue;

            EnemyBattleCharacter enemyBC = character.GetComponent<EnemyBattleCharacter>();
            if (enemyBC != null)
            {
                enemyBC.InitializeForBattle();
            }
            else
            {
                playerSlots.Add(character);
            }
        }

        // battleManager의 playerParty 리스트 갱신
        if (battleManager != null)
        {
            battleManager.playerParty.Clear();
        }

        CharacterData[] allResCharacters = Resources.LoadAll<CharacterData>("Characters");

        for (int i = 0; i < playerSlots.Count; i++)
        {
            BattleCharacter pChar = playerSlots[i];
            if (i < partyIDs.Count)
            {
                string charId = partyIDs[i];
                CharacterData data = null;
                if (allResCharacters != null)
                {
                    foreach (var cd in allResCharacters)
                    {
                        if (cd != null && cd.DataId == charId)
                        {
                            data = cd;
                            break;
                        }
                    }
                }
                if (data == null) data = pChar.testData;

                bool isLeader = (charId == leaderID);
                pChar.gameObject.SetActive(true);

                if (data != null)
                {
                    pChar.Init(data, isLeader);
                }
                else
                {
                    pChar.isLeader = isLeader;
                    pChar.PrepareCharacterData();
                }

                if (battleManager != null)
                {
                    battleManager.playerParty.Add(pChar);
                }
            }
            else
            {
                // 덱 인원수보다 많은 씬 슬롯은 완벽히 비활성화하여 초상화 및 UI 숨김
                pChar.gameObject.SetActive(false);
            }
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

    private void ApplySelectedEnemyEncounter()
    {
        if (battleManager == null || battleManager.enemyParty == null) return;

        EnemyEncounterData encounter = RunManager.Instance.CurrentEncounter;
        if (encounter == null)
        {
            Debug.LogWarning("[GameManager] No runtime encounter was selected. Scene enemy setup will be used.");
            return;
        }

        CharacterData[] enemyDataSlots = encounter.EnemySlots;
        if (enemyDataSlots == null || enemyDataSlots.Length != EnemyEncounterData.SlotCount)
        {
            Debug.LogError($"[GameManager] Encounter '{encounter.DisplayName}' must contain exactly {EnemyEncounterData.SlotCount} slots.");
            return;
        }

        List<BattleCharacter> sceneSlots = new List<BattleCharacter>(battleManager.enemyParty);
        List<BattleCharacter> activeEnemies = new List<BattleCharacter>(EnemyEncounterData.SlotCount);

        if (sceneSlots.Count < EnemyEncounterData.SlotCount)
        {
            Debug.LogError($"[GameManager] BattleScene requires {EnemyEncounterData.SlotCount} enemy slots, but only {sceneSlots.Count} are assigned.");
            return;
        }

        for (int i = 0; i < EnemyEncounterData.SlotCount; i++)
        {
            BattleCharacter slot = sceneSlots[i];
            CharacterData enemyData = enemyDataSlots[i];
            if (slot == null) continue;

            bool shouldActivate = enemyData != null;
            slot.gameObject.SetActive(shouldActivate);
            if (!shouldActivate) continue;

            EnemyBattleCharacter enemy = slot.GetComponent<EnemyBattleCharacter>();
            if (enemy == null)
            {
                Debug.LogError($"[GameManager] Enemy slot {i} has no EnemyBattleCharacter component.", slot);
                slot.gameObject.SetActive(false);
                continue;
            }

            enemy.enemyData = enemyData;
            enemy.ApplyDataReference();
            activeEnemies.Add(slot);
        }

        battleManager.enemyParty = activeEnemies;
        allCharacters.RemoveAll(character => character != null && character.GetComponent<EnemyBattleCharacter>() != null);
        allCharacters.AddRange(activeEnemies);

        Debug.Log($"[GameManager] Applied encounter '{encounter.DisplayName}' with {activeEnemies.Count} enemies.");
    }

    private void Start()
    {
        BeginBattleSetup();
    }
}
