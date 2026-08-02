using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Combatants")]
    public List<BattleCharacter> allCharacters;

    [Header("Managers")]
    public BattleSkillManager skillManager;
    public BattleManager battleManager;

    [Header("Debug")]
    [SerializeField] private bool showDebugVictoryButton = true;

    private BattleDebugVictoryButton _debugVictoryButton;

    public void BeginBattleSetup()
    {
        if (battleManager != null)
        {
            if (FindObjectOfType<BattleResultUIManager>() == null)
                battleManager.gameObject.AddComponent<BattleResultUIManager>();

            if (showDebugVictoryButton)
                EnsureDebugVictoryButton();
        }

        Debug.Log("[GameManager] TurnEndButton 버튼 연결");
        GameObject turnEndButtonObj = GameObject.Find("TurnEndButton");
        if (turnEndButtonObj != null)
        {
            Button turnEndButton = turnEndButtonObj.GetComponent<Button>();
            if (turnEndButton != null)
            {
                battleManager.turnEndButton = turnEndButton;
                turnEndButton.onClick.RemoveAllListeners();
                turnEndButton.onClick.AddListener(battleManager.EndPlayerTurn);
                turnEndButton.interactable = true;
                turnEndButtonObj.SetActive(true);

                Debug.Log("[GameManager] TurnEndButton 버튼 연결 완료");
            }
            else
            {
                Debug.LogError("[GameManager] TurnEndButton GameObject에 Button 컴포넌트가 없습니다.");
            }
        }
        else
        {
            Debug.LogError("[GameManager] Hierarchy에서 'TurnEndButton'을 찾을 수 없습니다.");
        }

        Debug.Log("[GameManager] 캐릭터 데이터 초기화 및 슬롯 연동");
        ApplySelectedEnemyEncounter();

        var partyIDs = RunManager.Instance != null ? RunManager.Instance.State.partyDataIDs : new List<string>();

        if (partyIDs == null || partyIDs.Count == 0)
        {
            partyIDs = new List<string>();
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

        if (RunManager.Instance != null)
            RunManager.Instance.SynchronizeLeaderWithPartyOrder();

        string leaderID = RunManager.Instance != null ? RunManager.Instance.State.leaderCharacterID : "";

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

                bool isLeader = charId == leaderID;
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
                pChar.gameObject.SetActive(false);
            }
        }

        Debug.Log("[GameManager] BattleManager 행동력 초기화");
        if (battleManager != null)
        {
            battleManager.InitializeAP();
        }

        Debug.Log("[GameManager] 스킬 UI 연결");
        if (skillManager != null)
        {
            skillManager.LinkSkillsToUI();
        }

        Debug.Log("[GameManager] BeginBattleSetup() 완료");
    }

    private void EnsureDebugVictoryButton()
    {
        if (battleManager == null) return;

        _debugVictoryButton = battleManager.GetComponent<BattleDebugVictoryButton>();
        if (_debugVictoryButton == null)
            _debugVictoryButton = battleManager.gameObject.AddComponent<BattleDebugVictoryButton>();

        _debugVictoryButton.Initialize(battleManager);
    }

    private void ApplySelectedEnemyEncounter()
    {
        if (battleManager == null || battleManager.enemyParty == null) return;

        EnemyEncounterData encounter = RunManager.Instance.CurrentEncounter;
        if (encounter == null)
        {
            Debug.LogWarning("[GameManager] 선택된 런타임 인카운터가 없습니다. 씬에 배치된 적 설정을 사용합니다.");
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
