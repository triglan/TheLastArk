using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class RemainingEnemyAssetBootstrap
{
    private const string Folder = "Assets/Resources/Characters/Enemy/Data";

    [MenuItem("Tools/Battle/Create Remaining Seventeen Enemies")]
    public static void CreateMissing()
    {
        WriteDefinitions(false);
    }

    [MenuItem("Tools/Battle/Update Remaining Seventeen Enemies")]
    public static void UpdateAll()
    {
        WriteDefinitions(true);
    }

    private static void WriteDefinitions(bool overwrite)
    {
        if (!AssetDatabase.IsValidFolder(Folder)) EnsureFolder(Folder);
        int created = 0;
        foreach (CharacterData data in Definitions())
        {
            string path = $"{Folder}/{data.characterName}_Data.asset";
            CharacterData existing = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (existing != null)
            {
                if (overwrite)
                {
                    string assetName = System.IO.Path.GetFileNameWithoutExtension(path);
                    EditorUtility.CopySerialized(data, existing);
                    existing.name = assetName;
                    EditorUtility.SetDirty(existing);
                }
                Object.DestroyImmediate(data);
                continue;
            }
            AssetDatabase.CreateAsset(data, path);
            created++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Battle] Created {created} remaining enemy assets.");
    }

    [MenuItem("Tools/Battle/Validate Remaining Seventeen Enemies")]
    public static void ValidateAll()
    {
        int count = 0;
        foreach (CharacterData definition in Definitions())
        {
            CharacterData asset = AssetDatabase.LoadAssetAtPath<CharacterData>($"{Folder}/{definition.characterName}_Data.asset");
            if (asset == null || !asset.isEnemy || asset.maxHp <= 0 || asset.maxMental < 0 || asset.enemyPatterns == null || asset.enemyPatterns.Count == 0)
                throw new System.InvalidOperationException($"Invalid enemy data: {definition.characterName}");
            count++;
            Object.DestroyImmediate(definition);
        }
        if (count != 17) throw new System.InvalidOperationException($"Expected 17 enemies, found {count}.");
        Debug.Log("[Battle] Remaining 17 enemy assets validated.");
    }

    [MenuItem("Tools/Battle/Create Approved Enemy Formations And Pools")]
    public static void CreateApprovedFormationsAndPools()
    {
        var enemies = new Dictionary<string, CharacterData>();
        foreach (CharacterData enemy in Resources.LoadAll<CharacterData>("Characters/Enemy"))
            if (enemy != null && enemy.isEnemy && !string.IsNullOrWhiteSpace(enemy.characterName)) enemies[enemy.characterName] = enemy;

        var formations = new Dictionary<string, EnemyEncounterData>();
        foreach (FormationDefinition definition in FormationDefinitions())
        {
            CharacterData[] slots = new CharacterData[EnemyEncounterData.SlotCount];
            for (int i = 0; i < definition.enemies.Length; i++)
            {
                if (!enemies.TryGetValue(definition.enemies[i], out slots[i]))
                    throw new System.InvalidOperationException($"Missing enemy data: {definition.enemies[i]}");
            }
            formations[definition.id] = WriteFormation(definition, slots);
        }

        BattleEncounterTable table = AssetDatabase.LoadAssetAtPath<BattleEncounterTable>("Assets/Resources/Battle/BattleEncounterTable.asset");
        if (table == null) throw new System.InvalidOperationException("BattleEncounterTable asset is missing.");
        foreach (PoolDefinition definition in PoolDefinitions())
        {
            var entries = new List<EnemyEncounterData>();
            foreach (string id in definition.formationIds) entries.Add(formations[id]);
            EnemyEncounterPool pool = WritePool(definition, entries);
            table.RegisterPool(pool);
        }
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateApprovedFormationsAndPools();
    }

    [MenuItem("Tools/Battle/Validate Approved Enemy Formations And Pools")]
    public static void ValidateApprovedFormationsAndPools()
    {
        int formationCount = 0;
        foreach (FormationDefinition definition in FormationDefinitions())
        {
            EnemyEncounterData formation = AssetDatabase.LoadAssetAtPath<EnemyEncounterData>(FormationPath(definition.id));
            if (formation == null || formation.EnemySlots == null || formation.EnemySlots.Length != 4 || !formation.HasAnyEnemy)
                throw new System.InvalidOperationException($"Invalid formation: {definition.id}");
            int enemyCount = 0;
            foreach (CharacterData enemy in formation.EnemySlots) if (enemy != null) enemyCount++;
            if (System.Array.Exists(definition.enemies, name => name == "보급 마차꾼") && enemyCount < 2)
                throw new System.InvalidOperationException($"Supply driver cannot appear alone: {definition.id}");
            if (System.Array.Exists(definition.enemies, name => name == "재의 인도자") && enemyCount > 2)
                throw new System.InvalidOperationException($"Ash Guide needs two summon slots: {definition.id}");
            formationCount++;
        }

        int poolCount = 0;
        foreach (PoolDefinition definition in PoolDefinitions())
        {
            EnemyEncounterPool pool = AssetDatabase.LoadAssetAtPath<EnemyEncounterPool>(PoolPath(definition.id));
            if (pool == null || pool.Encounters.Count == 0 || pool.MinFloor != definition.minFloor || pool.MaxFloor != definition.maxFloor || pool.NodeType != definition.nodeType)
                throw new System.InvalidOperationException($"Invalid pool: {definition.id}");
            poolCount++;
        }
        if (formationCount != 33 || poolCount != 6)
            throw new System.InvalidOperationException($"Expected 33 formations and 6 pools, got {formationCount} and {poolCount}.");
        Debug.Log("[Battle] Approved 33 formations and 6 pools validated.");
    }

    private static EnemyEncounterData WriteFormation(FormationDefinition definition, CharacterData[] slots)
    {
        string path = FormationPath(definition.id);
        EnemyEncounterData asset = AssetDatabase.LoadAssetAtPath<EnemyEncounterData>(path);
        if (asset == null) { asset = ScriptableObject.CreateInstance<EnemyEncounterData>(); AssetDatabase.CreateAsset(asset, path); }
        SerializedObject serialized = new SerializedObject(asset);
        serialized.FindProperty("encounterId").stringValue = "content_" + definition.id;
        serialized.FindProperty("displayName").stringValue = definition.displayName;
        SerializedProperty enemySlots = serialized.FindProperty("enemySlots");
        enemySlots.arraySize = EnemyEncounterData.SlotCount;
        for (int i = 0; i < EnemyEncounterData.SlotCount; i++) enemySlots.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static EnemyEncounterPool WritePool(PoolDefinition definition, List<EnemyEncounterData> formations)
    {
        string path = PoolPath(definition.id);
        EnemyEncounterPool asset = AssetDatabase.LoadAssetAtPath<EnemyEncounterPool>(path);
        if (asset == null) { asset = ScriptableObject.CreateInstance<EnemyEncounterPool>(); AssetDatabase.CreateAsset(asset, path); }
        SerializedObject serialized = new SerializedObject(asset);
        serialized.FindProperty("poolId").stringValue = "content_" + definition.id;
        serialized.FindProperty("displayName").stringValue = definition.displayName;
        serialized.FindProperty("regionId").stringValue = EnemyEncounterPool.DefaultRegionId;
        serialized.FindProperty("nodeType").enumValueIndex = (int)definition.nodeType;
        serialized.FindProperty("minFloor").intValue = definition.minFloor;
        serialized.FindProperty("maxFloor").intValue = definition.maxFloor;
        serialized.FindProperty("minCombatCount").intValue = 0;
        serialized.FindProperty("maxCombatCount").intValue = 99;
        serialized.FindProperty("priority").intValue = 100;
        serialized.FindProperty("rewardStageId").stringValue = definition.rewardStageId;
        SerializedProperty encounters = serialized.FindProperty("encounters");
        encounters.arraySize = formations.Count;
        for (int i = 0; i < formations.Count; i++) encounters.GetArrayElementAtIndex(i).objectReferenceValue = formations[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static IEnumerable<FormationDefinition> FormationDefinitions()
    {
        yield return F("early_merchant", "초반 - 무장 상인", "무장 상인");
        yield return F("early_dot", "초반 - 도적과 전갈", "부랑자 도적", "사막 전갈");
        yield return F("early_blades", "초반 - 쌍칼과 방패", "쌍칼잡이", "흑철 방패병");
        yield return F("early_bell", "초반 - 종지기와 도적", "종말의 종지기", "부랑자 도적");
        yield return F("early_caravan", "초반 - 상인 호위", "무장 상인", "흑철 방패병");

        yield return F("intro_collector", "중간 입문 - 징수자", "징수자", "부랑자 도적");
        yield return F("intro_hook", "중간 입문 - 약탈자", "갈고리 약탈자", "사막 전갈");
        yield return F("intro_beetle", "중간 입문 - 딱정벌레", "탈피 딱정벌레", "종말의 종지기");
        yield return F("intro_duelist", "중간 입문 - 결투사", "계약 결투사", "흑철 방패병");
        yield return F("intro_crossbow", "중간 입문 - 쇠뇌수", "흑철 쇠뇌수", "흑철 방패병");
        yield return F("intro_witness", "중간 입문 - 목격자", "마지막 목격자", "쌍칼잡이");

        yield return F("mid_collection", "중반 - 연타 징수대", "징수자", "쌍칼잡이", "부랑자 도적");
        yield return F("mid_iron", "중반 - 흑철 사격대", "갈고리 약탈자", "흑철 쇠뇌수", "흑철 방패병");
        yield return F("mid_shell", "중반 - 탈피의 증언", "탈피 딱정벌레", "마지막 목격자", "종말의 종지기");
        yield return F("mid_contract", "중반 - 독과 계약", "계약 결투사", "사막 전갈", "부랑자 도적");
        yield return F("mid_crossfire", "중반 - 쇠뇌 교차사격", "흑철 쇠뇌수", "흑철 쇠뇌수", "흑철 방패병");
        yield return F("mid_testimony", "중반 - 마지막 타종", "마지막 목격자", "징수자", "종말의 종지기");

        yield return F("late_powder", "후반 - 화약 호위대", "화약 행상인", "흑철 방패병", "부랑자 도적");
        yield return F("late_supply", "후반 - 보급 사격대", "보급 마차꾼", "징수자", "흑철 쇠뇌수");
        yield return F("late_blood", "후반 - 피와 독", "피칼잡이", "부랑자 도적", "사막 전갈");
        yield return F("late_thorns", "후반 - 가시의 목격자", "가시 선인장", "마지막 목격자");
        yield return F("late_souls", "후반 - 영혼의 타종", "영혼등불지기", "종말의 종지기", "탈피 딱정벌레");
        yield return F("late_fortress", "후반 - 움직이는 요새", "보급 마차꾼", "가시 선인장", "쌍칼잡이");
        yield return F("late_barrage", "후반 - 피의 포격", "화약 행상인", "피칼잡이");

        yield return F("elite_rider", "엘리트 - 흑철 기수", "흑철 기수");
        yield return F("elite_rider_guard", "엘리트 - 기수 호위", "흑철 기수", "흑철 방패병");
        yield return F("elite_captain_crossbow", "엘리트 - 호위대 사격", "흑철 호위대장", "흑철 쇠뇌수");
        yield return F("elite_captain_duelist", "엘리트 - 호위대 계약", "흑철 호위대장", "계약 결투사");
        yield return F("elite_ash", "엘리트 - 재의 인도자", "재의 인도자");
        yield return F("elite_ash_bell", "엘리트 - 재의 타종", "재의 인도자", "종말의 종지기");

        yield return F("boss_camel", "보스 - 황금낙타 대상주", "황금낙타 대상주");
        yield return F("boss_glaive", "보스 - 쌍월도 대장", "쌍월도 대장");
        yield return F("boss_devourer", "보스 - 사막을 삼키는 자", "사막을 삼키는 자");
    }

    private static IEnumerable<PoolDefinition> PoolDefinitions()
    {
        yield return Pool("combat_01_03", "일반 1~3층", NodeType.Combat, 1, 3, "easy", "early_merchant", "early_dot", "early_blades", "early_bell", "early_caravan");
        yield return Pool("combat_04_05", "일반 4~5층", NodeType.Combat, 4, 5, "normal", "intro_collector", "intro_hook", "intro_beetle", "intro_duelist", "intro_crossbow", "intro_witness");
        yield return Pool("combat_06_10", "일반 6~10층", NodeType.Combat, 6, 10, "normal", "mid_collection", "mid_iron", "mid_shell", "mid_contract", "mid_crossfire", "mid_testimony");
        yield return Pool("combat_11_15", "일반 11~15층", NodeType.Combat, 11, 15, "hard", "late_powder", "late_supply", "late_blood", "late_thorns", "late_souls", "late_fortress", "late_barrage");
        yield return Pool("elite_01_15", "엘리트 전투", NodeType.Elite, 1, 15, "hard", "elite_rider", "elite_rider_guard", "elite_captain_crossbow", "elite_captain_duelist", "elite_ash", "elite_ash_bell");
        yield return Pool("boss_01_15", "보스 전투", NodeType.Boss, 1, 15, "very-hard", "boss_camel", "boss_glaive", "boss_devourer");
    }

    private static FormationDefinition F(string id, string displayName, params string[] enemies) => new FormationDefinition(id, displayName, enemies);
    private static PoolDefinition Pool(string id, string displayName, NodeType nodeType, int minFloor, int maxFloor, string rewardStageId, params string[] formations) => new PoolDefinition(id, displayName, nodeType, minFloor, maxFloor, rewardStageId, formations);
    private static string FormationPath(string id) => $"Assets/Resources/Battle/Encounters/Content_{id}_Formation.asset";
    private static string PoolPath(string id) => $"Assets/Resources/Battle/EncounterPools/Content_{id}_Pool.asset";

    private sealed class FormationDefinition
    {
        public readonly string id, displayName; public readonly string[] enemies;
        public FormationDefinition(string id, string displayName, string[] enemies) { this.id = id; this.displayName = displayName; this.enemies = enemies; }
    }

    private sealed class PoolDefinition
    {
        public readonly string id, displayName, rewardStageId; public readonly NodeType nodeType; public readonly int minFloor, maxFloor; public readonly string[] formationIds;
        public PoolDefinition(string id, string displayName, NodeType nodeType, int minFloor, int maxFloor, string rewardStageId, string[] formationIds) { this.id = id; this.displayName = displayName; this.nodeType = nodeType; this.minFloor = minFloor; this.maxFloor = maxFloor; this.rewardStageId = rewardStageId; this.formationIds = formationIds; }
    }

    private static IEnumerable<CharacterData> Definitions()
    {
        yield return Collector(); yield return HookRaider(); yield return Beetle(); yield return Duelist();
        yield return Crossbowman(); yield return Witness(); yield return PowderMerchant(); yield return SupplyDriver();
        yield return BloodBlade(); yield return Cactus(); yield return SoulLantern(); yield return Rider();
        yield return Captain(); yield return AshGuide(); yield return CamelBoss(); yield return TwinGlaive(); yield return Devourer();
    }

    private static CharacterData Collector()
    {
        var d = Enemy("징수자", 18, 10, 0, 0, "힘을 빠르게 누적하는 연타 공격수.");
        d.enemyPatterns.Add(P("징수", Self(EffectType.Strength, 1, permanent: true)));
        d.enemyPatterns.Add(P("삼연타", E(TargetType.SingleEnemy, EffectType.Damage, 3, hits: 3), Self(EffectType.Strength, 1, permanent: true)));
        return d;
    }

    private static CharacterData HookRaider()
    {
        var d = Enemy("갈고리 약탈자", 27, 8, 1, 0, "보호막을 걷어낸 뒤 같은 대상을 강타한다.");
        d.enemyPatterns.Add(P("갈고리", E(TargetType.SingleEnemy, EffectType.Damage, 4), Special(EnemyPatternActionType.BreakShield, TargetType.SingleEnemy, 6), Remember()));
        d.enemyPatterns.Add(P("약탈 강타", E(TargetType.SingleEnemy, EffectType.Damage, 8, selection: EnemyTargetSelection.Remembered), Self(EffectType.Strength, 1, permanent: true)));
        return d;
    }

    private static CharacterData Beetle()
    {
        var d = Enemy("탈피 딱정벌레", 30, 5, 2, 0, "두 번 탈피하며 방어를 힘으로 바꾼다.");
        d.enemyRuntimeVariables.Add(Var("molt", "탈피", 0, 2));
        d.enemyPatterns.Add(P("박치기", E(TargetType.SingleEnemy, EffectType.Damage, 5)));
        d.enemyPatterns.Add(P("탈피", Self(EffectType.Shield, 4), Conditional(Self(EffectType.Vulnerable, 2, permanent: true), "molt", EnemyRuntimeComparison.Less, 1),
            Conditional(Self(EffectType.Strength, 2, permanent: true), "molt", EnemyRuntimeComparison.Less, 2),
            Conditional(Self(EffectType.Strength, 1, permanent: true), "molt", EnemyRuntimeComparison.GreaterOrEqual, 2), M("molt", 1)));
        return d;
    }

    private static CharacterData Duelist()
    {
        var d = Enemy("계약 결투사", 18, 14, 0, 1, "행동 사이마다 반격을 준비하는 결투가.");
        d.enemyRuntimeVariables.Add(Var("counter_bonus", "반격 피해", 0));
        d.enemyPatterns.Add(P("반격 준비", Self(EffectType.Counter, valueVariable: "counter_bonus", charges: 2, ownerPhase: true)));
        d.enemyPatterns.Add(P("응수", E(TargetType.SingleEnemy, EffectType.Damage, 5), Self(EffectType.Counter, valueVariable: "counter_bonus", charges: 2, ownerPhase: true)));
        d.enemyPatterns.Add(P("계약의 일격", E(TargetType.SingleEnemy, EffectType.Damage, 8), Self(EffectType.Counter, valueVariable: "counter_bonus", charges: 2, ownerPhase: true), M("counter_bonus", 1)));
        return d;
    }

    private static CharacterData Crossbowman()
    {
        var d = Enemy("흑철 쇠뇌수", 14, 8, 0, 0, "한 대상을 조준해 관통 사격한다.");
        d.enemyRuntimeVariables.Add(Var("shot_bonus", "사격 피해", 0));
        d.enemyPatterns.Add(P("조준", Remember()));
        d.enemyPatterns.Add(P("관통 사격", Self(EffectType.Pierce, 50, marker: true), E(TargetType.SingleEnemy, EffectType.Damage, 9, "shot_bonus", EnemyTargetSelection.Remembered),
            Remove(TargetType.Self, EffectType.Pierce), M("shot_bonus", 2)));
        d.enemyPatterns.Add(P("재장전", M("shot_bonus", 0)));
        return d;
    }

    private static CharacterData Witness()
    {
        var d = Enemy("마지막 목격자", 14, 24, 0, 2, "방어할 때 다음 정신 공격 대상을 지정한다.");
        d.enemyRuntimeVariables.Add(Var("mental_bonus", "정신 피해", 0));
        d.enemyPatterns.Add(P("주시", Remember(), Self(EffectType.Shield, 6)));
        d.enemyPatterns.Add(P("증언", E(TargetType.SingleEnemy, EffectType.MentalDamage, 5, "mental_bonus", EnemyTargetSelection.Remembered), M("mental_bonus", 1)));
        return d;
    }

    private static CharacterData PowderMerchant()
    {
        var d = Enemy("화약 행상인", 24, 12, 0, 0, "장전 수만큼 포격을 반복한다.");
        d.enemyRuntimeVariables.Add(Var("base_load", "기본 장전", 0)); d.enemyRuntimeVariables.Add(Var("load", "현재 장전", 0));
        d.enemyPatterns.Add(P("포격 장전", M("load", 1))); d.enemyPatterns.Add(P("포격 장전", M("load", 1)));
        d.enemyPatterns.Add(P("연속 포격", E(TargetType.SingleEnemy, EffectType.Damage, 5, hitVariable: "load", hitVariableIsTotal: true), M("base_load", 1), MSetFrom("load", "base_load")));
        return d;
    }

    private static CharacterData SupplyDriver()
    {
        var d = Enemy("보급 마차꾼", 38, 8, 0, 0, "아군 전체를 순환 지원하며 단독 출현하지 않는다.");
        d.enemyRuntimeVariables.Add(Var("heal_bonus", "회복량", 0)); d.enemyRuntimeVariables.Add(Var("shield_bonus", "보호막", 0));
        d.enemyPatterns.Add(P("전투 보급", E(TargetType.AllFriendly, EffectType.Heal, 6, "heal_bonus"), M("heal_bonus", 2)));
        d.enemyPatterns.Add(P("무기 보급", E(TargetType.AllFriendly, EffectType.Strength, value: 1, permanent: true)));
        d.enemyPatterns.Add(P("방패 보급", E(TargetType.AllFriendly, EffectType.Shield, 5, "shield_bonus"), M("shield_bonus", 2)));
        return d;
    }

    private static CharacterData BloodBlade()
    {
        var d = Enemy("피칼잡이", 24, 13, 0, 0, "출혈을 누적하고 즉시 폭발시킨다.");
        d.enemyRuntimeVariables.Add(Var("bleed_bonus", "출혈 수치", 0));
        d.enemyPatterns.Add(P("피의 연격", E(TargetType.SingleEnemy, EffectType.Damage, 6, hits: 2), E(TargetType.SingleEnemy, EffectType.Bleed, value: 2, duration: 3, valueVariable: "bleed_bonus", usePreparedTarget: true)));
        d.enemyPatterns.Add(P("상처 폭발", Special(EnemyPatternActionType.TriggerStatus, TargetType.AllEnemy, effectType: EffectType.Bleed), M("bleed_bonus", 1)));
        return d;
    }

    private static CharacterData Cactus()
    {
        var d = Enemy("가시 선인장", 42, 6, 3, 0, "공격력을 비례 반사하며 반사 횟수가 성장한다.");
        d.enemyRuntimeVariables.Add(Var("reflect_bonus", "반사 횟수", 0, 2));
        d.enemyPatterns.Add(P("가시 갑옷", Self(EffectType.Reflect, value: 50, charges: 3, ownerPhase: true, chargesVariable: "reflect_bonus")));
        d.enemyPatterns.Add(P("가시 폭발", E(TargetType.AllEnemy, EffectType.Damage, 5), M("reflect_bonus", 1)));
        return d;
    }

    private static CharacterData SoulLantern()
    {
        var d = Enemy("영혼등불지기", 20, 35, 0, 3, "충전 수만큼 전체 정신 피해를 반복한다.");
        d.enemyRuntimeVariables.Add(Var("charge", "등불 충전", 0)); d.enemyRuntimeVariables.Add(Var("damage_bonus", "충전당 피해", 0));
        d.enemyPatterns.Add(P("등불 충전", M("charge", 1), E(TargetType.AllFriendly, EffectType.MentalHeal, 3)));
        d.enemyPatterns.Add(P("등불 충전", M("charge", 1), E(TargetType.AllFriendly, EffectType.MentalHeal, 3)));
        d.enemyPatterns.Add(P("영혼 방출", E(TargetType.AllEnemy, EffectType.MentalDamage, 2, "damage_bonus", hitVariable: "charge", hitVariableIsTotal: true), M("damage_bonus", 1), MSet("charge", 0)));
        return d;
    }

    private static CharacterData Rider()
    {
        var d = Enemy("흑철 기수", 70, 18, 2, 0, "돌진 후 하마하고 다시 무장한다.");
        d.enemyPatterns.Add(P("돌진 준비", M("mounted", 0))); d.enemyRuntimeVariables.Add(Var("mounted", "기마", 1));
        d.enemyPatterns.Add(P("흑철 돌진", Self(EffectType.Pierce, 50, marker: true), E(TargetType.AllEnemy, EffectType.Damage, 7), Remove(TargetType.Self, EffectType.Pierce)));
        d.enemyPatterns.Add(P("하마", Self(EffectType.Vulnerable, 2, permanent: true)));
        d.enemyPatterns.Add(P("보행", M("mounted", 0)));
        d.enemyPatterns.Add(P("재탑승", Remove(TargetType.Self, EffectType.Vulnerable), Self(EffectType.Strength, 1, permanent: true)));
        return d;
    }

    private static CharacterData Captain()
    {
        var d = Enemy("흑철 호위대장", 70, 18, 5, 0, "도발과 보호로 다른 적을 지휘한다.");
        d.enemyPatterns.Add(P("방진", Remove(TargetType.Self, EffectType.Vulnerable), Self(EffectType.Taunt, charges: 3), Self(EffectType.Shield, 12)));
        d.enemyPatterns.Add(P("호위", E(TargetType.SingleEnemy, EffectType.Damage, 7), E(TargetType.AllFriendly, EffectType.Guard, charges: 1)));
        d.enemyPatterns.Add(P("지휘관의 연격", E(TargetType.SingleEnemy, EffectType.Damage, 5, hits: 3), Self(EffectType.Strength, 1, permanent: true)));
        d.enemyPatterns.Add(P("방어 하강", Self(EffectType.Vulnerable, 5, ownerPhase: true)));
        return d;
    }

    private static CharacterData AshGuide()
    {
        var d = Enemy("재의 인도자", 42, 60, 0, 4, "종지기를 소환하고 아군 체력을 의식으로 바꾼다.");
        d.enemyRuntimeVariables.Add(Var("summoned", "소환 성공", 0)); d.enemyRuntimeVariables.Add(Var("ritual", "의식", 0)); d.enemyRuntimeVariables.Add(Var("mental_bonus", "정신 피해", 0));
        d.enemyPatterns.Add(P("종지기 소환", Summon(new[] { "종말의 종지기" }, 2, "summoned"),
            Conditional(E(TargetType.AllFriendly, EffectType.Strength, value: 1, permanent: true, enemyNames: new[] { "종말의 종지기" }), "summoned", EnemyRuntimeComparison.Equal, 0)));
        d.enemyPatterns.Add(P("재의 의식", Sacrifice(20, "ritual")));
        var release = E(TargetType.AllEnemy, EffectType.MentalDamage, 3, "ritual", divisor: 3, maxBonus: 5); release.additionalValueVariableId = "mental_bonus";
        d.enemyPatterns.Add(P("의식 방출", release, M("mental_bonus", 1), MSet("ritual", 0)));
        return d;
    }

    private static CharacterData CamelBoss()
    {
        var d = Enemy("황금낙타 대상주", 100, 80, 5, 6, "골드를 훔치고 용병을 고용하는 보스.");
        d.defeatGoldBonus = 50; d.returnStolenGoldOnDefeat = true; d.stolenGoldVariableId = "stolen_gold";
        d.enemyRuntimeVariables.Add(Var("stolen_gold", "탈취 골드", 0)); d.enemyRuntimeVariables.Add(Var("hired", "고용 성공", 0)); d.enemyRuntimeVariables.Add(Var("magic_bonus", "마법 피해", 0));
        d.enemyPatterns.Add(P("황금 압류", new EnemyPatternAction { actionType = EnemyPatternActionType.StealGold, runtimeVariableId = "stolen_gold", runtimeValue = 20 }, Self(EffectType.Shield, 12)));
        string[] mercs = { "흑철 방패병", "계약 결투사", "흑철 쇠뇌수" };
        d.enemyPatterns.Add(P("용병 고용", Summon(mercs, 4, "hired"), Conditional(E(TargetType.AllFriendly, EffectType.Heal, 10, enemyNames: mercs), "hired", EnemyRuntimeComparison.Equal, 0)));
        d.enemyPatterns.Add(P("대상단 지원", E(TargetType.AllFriendly, EffectType.Shield, 8), E(TargetType.AllFriendly, EffectType.Strength, value: 1, permanent: true)));
        d.enemyPatterns.Add(P("황금 마법", E(TargetType.SingleEnemy, EffectType.Damage, 10, "magic_bonus", damageType: DamageType.Magical), M("magic_bonus", 1)));
        return d;
    }

    private static CharacterData TwinGlaive()
    {
        var d = Enemy("쌍월도 대장", 105, 45, 6, 2, "연타와 출혈을 연마하는 보스.");
        d.enemyRuntimeVariables.Add(Var("attack_bonus", "타격 피해", 0, 3)); d.enemyRuntimeVariables.Add(Var("bleed_bonus", "출혈 수치", 0));
        d.enemyPatterns.Add(P("초승달", Remove(TargetType.Self, EffectType.Vulnerable), E(TargetType.SingleEnemy, EffectType.Damage, 8, "attack_bonus"), E(TargetType.SingleEnemy, EffectType.Bleed, value: 1, duration: 3, valueVariable: "bleed_bonus", usePreparedTarget: true)));
        d.enemyPatterns.Add(P("쌍월", E(TargetType.SingleEnemy, EffectType.Damage, 5, "attack_bonus", hits: 2)));
        d.enemyPatterns.Add(P("만월 연격", E(TargetType.SingleEnemy, EffectType.Damage, 4, "attack_bonus", hits: 4), E(TargetType.SingleEnemy, EffectType.Bleed, value: 2, duration: 3, valueVariable: "bleed_bonus", usePreparedTarget: true)));
        d.enemyPatterns.Add(P("연마", M("attack_bonus", 1), M("bleed_bonus", 1), Self(EffectType.Vulnerable, 6, ownerPhase: true)));
        return d;
    }

    private static CharacterData Devourer()
    {
        var d = Enemy("사막을 삼키는 자", 140, 20, 10, 0, "경고 대상을 공격해 포식을 저지하게 만드는 보스.");
        d.enemyRuntimeVariables.Add(Var("threshold", "포식 저지", 6, 10));
        d.enemyPatterns.Add(P("물어뜯기", E(TargetType.SingleEnemy, EffectType.Damage, 10)));
        d.enemyPatterns.Add(P("모래 휩쓸기", E(TargetType.AllEnemy, EffectType.Damage, 6)));
        d.enemyPatterns.Add(P("포식 경고", Remember(), Self(EffectType.Vulnerable, 10, permanent: true), Special(EnemyPatternActionType.BeginDamageCheck, TargetType.SingleEnemy, selection: EnemyTargetSelection.Remembered)));
        d.enemyPatterns.Add(P("포식", new EnemyPatternAction { actionType = EnemyPatternActionType.ResolveDamageCheck, targetType = TargetType.SingleEnemy, targetSelection = EnemyTargetSelection.Remembered,
            conditionVariableId = "threshold", effect = Entry(EffectType.Damage, 15), percent = 15, runtimeValue = 1 }));
        d.enemyPatterns.Add(P("껍질 재생", Remove(TargetType.Self, EffectType.Vulnerable), Self(EffectType.Shield, 10), M("threshold", 2)));
        return d;
    }

    private static CharacterData Enemy(string name, int hp, int mental, int armor, int resist, string role) => Create(name, hp, mental, armor, resist, role);
    private static CharacterData Create(string name, int hp, int mental, int armor, int resist, string role)
    {
        var d = ScriptableObject.CreateInstance<CharacterData>(); d.characterName = name; d.isEnemy = true; d.maxHp = hp; d.maxMental = mental; d.armor = armor; d.magicResist = resist; d.enemyRoleDescription = role; return d;
    }
    private static EnemyRuntimeVariableDefinition Var(string id, string label, int initial, int max = 0) => new EnemyRuntimeVariableDefinition { id = id, displayName = label, initialValue = initial, showInUi = true, hasMaxValue = max > 0, maxValue = max };
    private static EnemyPatternData P(string name, params EnemyPatternAction[] actions) => new EnemyPatternData { patternName = name, targetType = actions.Length > 0 ? actions[0].targetType : TargetType.Self, actions = new List<EnemyPatternAction>(actions) };
    private static EffectEntry Entry(EffectType type, float fixedValue = 0, float value = 0, int duration = 3, int charges = 3, DamageType damageType = DamageType.Physical, StatusDurationType durationType = StatusDurationType.Default) => new EffectEntry { type = type, fixedValue = fixedValue, value = value, duration = duration, charges = charges, damageType = damageType, multiplier = 0, durationType = durationType };
    private static EnemyPatternAction E(TargetType target, EffectType type, float fixedValue = 0, string valueVariable = null, EnemyTargetSelection selection = EnemyTargetSelection.Random, int hits = 1, float value = 0, int duration = 3, int charges = 3, bool permanent = false, string hitVariable = null, bool hitVariableIsTotal = false, float divisor = 1, int maxBonus = 0, DamageType damageType = DamageType.Physical, string[] enemyNames = null, bool usePreparedTarget = false) => new EnemyPatternAction { actionType = EnemyPatternActionType.Effect, targetType = target, targetSelection = selection, usePreparedTarget = usePreparedTarget, effect = Entry(type, fixedValue, value, duration, charges, damageType, permanent ? StatusDurationType.Permanent : StatusDurationType.Default), valueVariableId = valueVariable, hitCountVariableId = hitVariable, useHitCountVariableAsTotal = hitVariableIsTotal, divisor = divisor, maxBonus = maxBonus, enemyNames = enemyNames };
    private static EnemyPatternAction Self(EffectType type, float fixedValue = 0, float value = 0, string valueVariable = null, int charges = 3, bool permanent = false, bool ownerPhase = false, bool marker = false, string chargesVariable = null) { var a = E(TargetType.Self, type, fixedValue, valueVariable, value: value); a.effect.charges = charges; a.chargesVariableId = chargesVariable; a.effect.durationType = permanent ? StatusDurationType.Permanent : ownerPhase ? StatusDurationType.UntilOwnerPhase : marker ? StatusDurationType.Marker : StatusDurationType.Default; return a; }
    private static EnemyPatternAction M(string id, int amount) => new EnemyPatternAction { actionType = EnemyPatternActionType.ModifyRuntimeVariable, runtimeVariableId = id, runtimeValue = amount };
    private static EnemyPatternAction MSet(string id, int value) { var a = M(id, value); a.setRuntimeValue = true; return a; }
    private static EnemyPatternAction MSetFrom(string id, string source) => new EnemyPatternAction { actionType = EnemyPatternActionType.ModifyRuntimeVariable, runtimeVariableId = id, runtimeValueVariableId = source, setRuntimeValue = true };
    private static EnemyPatternAction Remember() => new EnemyPatternAction { actionType = EnemyPatternActionType.RememberTarget, targetType = TargetType.SingleEnemy, targetSelection = EnemyTargetSelection.Random, usePreparedTarget = true };
    private static EnemyPatternAction Remove(TargetType target, EffectType type) => new EnemyPatternAction { actionType = EnemyPatternActionType.RemoveStatus, targetType = target, effect = Entry(type) };
    private static EnemyPatternAction Special(EnemyPatternActionType type, TargetType target, float fixedValue = 0, EffectType effectType = EffectType.Damage, EnemyTargetSelection selection = EnemyTargetSelection.Random) => new EnemyPatternAction { actionType = type, targetType = target, targetSelection = selection, usePreparedTarget = true, effect = Entry(effectType, fixedValue) };
    private static EnemyPatternAction Conditional(EnemyPatternAction action, string id, EnemyRuntimeComparison comparison, int value) { action.conditionVariableId = id; action.condition = comparison; action.conditionValue = value; return action; }
    private static EnemyPatternAction Summon(string[] names, int limit, string resultVariable) => new EnemyPatternAction { actionType = EnemyPatternActionType.Summon, enemyNames = names, limit = limit, runtimeVariableId = resultVariable };
    private static EnemyPatternAction Sacrifice(float percent, string variable) => new EnemyPatternAction { actionType = EnemyPatternActionType.SacrificeAllies, targetType = TargetType.AllFriendly, percent = percent, runtimeVariableId = variable };

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/'); string current = parts[0];
        for (int i = 1; i < parts.Length; i++) { string next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; }
    }
}
