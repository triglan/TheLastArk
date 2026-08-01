using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyBattleCharacter))]
public class EnemyBattleCharacterEditor : Editor
{
    private SerializedProperty enemyDataProperty;
    private SerializedProperty initializeOnStartProperty;

    private void OnEnable()
    {
        enemyDataProperty = serializedObject.FindProperty("enemyData");
        initializeOnStartProperty = serializedObject.FindProperty("initializeOnStart");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(enemyDataProperty, new GUIContent("적 캐릭터 데이터"));
        EditorGUILayout.PropertyField(initializeOnStartProperty, new GUIContent("시작 시 초기화"));
        serializedObject.ApplyModifiedProperties();

        EnemyBattleCharacter enemy = (EnemyBattleCharacter)target;
        EditorGUILayout.Space(6);
        DrawActionButtons(enemy);
        DrawStatusPreview(enemy);
        DrawPatternPreview(enemy.Patterns);
    }

    public override bool RequiresConstantRepaint()
    {
        return Application.isPlaying;
    }

    private void DrawActionButtons(EnemyBattleCharacter enemy)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("적 데이터 적용"))
        {
            enemy.ApplyDataReference();
            EditorUtility.SetDirty(enemy);
        }

        if (GUILayout.Button("전투 데이터 초기화"))
        {
            enemy.InitializeForBattle();
            EditorUtility.SetDirty(enemy);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawStatusPreview(EnemyBattleCharacter enemy)
    {
        CharacterStatus status = Application.isPlaying && enemy.HasRuntimeStatus ? enemy.CurrentStatus : null;

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("현재 전투 상태", EditorStyles.boldLabel);

        if (enemy.enemyData == null)
        {
            EditorGUILayout.HelpBox("적 캐릭터 데이터가 비어 있습니다.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        if (status == null)
            EditorGUILayout.HelpBox("아직 전투 상태가 만들어지지 않았습니다. 전투 시작 후에는 현재 값이 표시됩니다.", MessageType.Info);

        DrawReadOnlyText("이름", enemy.enemyData.characterName);
        float currentHp = status != null ? status.currentHp : enemy.enemyData.maxHp;
        float maxHp = status != null ? status.FinalMaxHp : enemy.enemyData.maxHp;
        float currentMental = status != null ? status.currentMental : enemy.enemyData.maxMental;
        float maxMental = status != null ? status.FinalMaxMental : enemy.enemyData.maxMental;
        DrawReadOnlyText("체력", $"{currentHp:0.##} / {maxHp:0.##}");
        DrawReadOnlyText("정신력", $"{currentMental:0.##} / {maxMental:0.##}");
        DrawReadOnlyText("기본 공격력", enemy.BaseAttack.ToString("0.##"));
        DrawReadOnlyText("추가 공격력", (status != null ? status.bonusAttack : 0f).ToString("0.##"));
        DrawReadOnlyText("총 공격력", (status != null ? status.FinalAttack : enemy.BaseAttack).ToString("0.##"));

        if (status != null && status.activeStatusEffects != null && status.activeStatusEffects.Count > 0)
            DrawActiveEffects(status.activeStatusEffects);

        EditorGUILayout.EndVertical();
    }

    private void DrawActiveEffects(List<ActiveStatusEffect> effects)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("적용 중인 상태", EditorStyles.miniBoldLabel);
        foreach (ActiveStatusEffect effect in effects)
        {
            if (effect == null) continue;
            DrawReadOnlyText(GetEffectName(effect.effectType), $"{effect.remainingTurns}턴 / 수치 {effect.damagePerTurn:0.##}");
        }
    }

    private void DrawPatternPreview(List<EnemyPatternData> patterns)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("행동 패턴", EditorStyles.boldLabel);

        if (patterns == null || patterns.Count == 0)
        {
            EditorGUILayout.HelpBox("등록된 패턴이 없습니다. 전투 중 기본 공격을 사용합니다.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        for (int i = 0; i < patterns.Count; i++)
        {
            EnemyPatternData pattern = patterns[i];
            if (pattern == null) continue;

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField($"{i + 1}. {GetPatternName(pattern, i)}", EditorStyles.miniBoldLabel);
            DrawReadOnlyText("대상", GetTargetName(pattern.targetType));

            if (pattern.effects == null || pattern.effects.Count == 0)
            {
                EditorGUILayout.LabelField("효과 없음");
            }
            else
            {
                for (int effectIndex = 0; effectIndex < pattern.effects.Count; effectIndex++)
                    DrawReadOnlyText($"효과 {effectIndex + 1}", GetEffectSummary(pattern.effects[effectIndex]));
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawReadOnlyText(string label, string value)
    {
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField(label, value);
        EditorGUI.EndDisabledGroup();
    }

    private string GetPatternName(EnemyPatternData pattern, int index)
    {
        return string.IsNullOrWhiteSpace(pattern.patternName) ? $"패턴 {index + 1}" : pattern.patternName;
    }

    private string GetEffectSummary(EffectEntry effect)
    {
        if (effect == null) return "비어 있음";
        string resultText = effect.useActualResult ? ", 직전 결과 사용" : "";
        return $"{GetEffectName(effect.type)} / 배율 {effect.multiplier:0.##} / 고정 {effect.fixedValue:0.##}{resultText}";
    }

    private string GetTargetName(TargetType targetType)
    {
        return targetType switch
        {
            TargetType.SingleEnemy => "플레이어 1명",
            TargetType.LeftEnemy => "대상 왼쪽",
            TargetType.RightEnemy => "대상 오른쪽",
            TargetType.AdjacentEnemy => "대상과 양옆",
            TargetType.AllEnemy => "플레이어 전체",
            TargetType.Friendly => "적 아군 1명",
            TargetType.AllFriendly => "적 전체",
            _ => targetType.ToString()
        };
    }

    private string GetEffectName(EffectType effectType)
    {
        return effectType switch
        {
            EffectType.Damage => "피해",
            EffectType.Heal => "회복",
            EffectType.Buff => "버프",
            EffectType.Stun => "기절",
            EffectType.Bleed => "출혈",
            _ => effectType.ToString()
        };
    }
}
