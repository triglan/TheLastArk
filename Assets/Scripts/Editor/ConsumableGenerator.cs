using UnityEngine;
using UnityEditor;
using TheLastArk.Data;

public class ConsumableGenerator
{
    [MenuItem("TheLastArk/Generate Test Consumables")]
    public static void GenerateConsumables()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Consumables"))
            AssetDatabase.CreateFolder("Assets/Resources", "Consumables");

        CreateConsumable("Consumables/FlameBurstTome", "불꽃 폭발의 마법서", "대상 적 하나에게 피해를 20 줍니다.", ConsumableEffectType.DamageSingle, 20f);
        CreateConsumable("Consumables/FlameWallTome", "화염 장벽의 마법서", "모든 적에게 피해를 10 줍니다.", ConsumableEffectType.DamageAll, 10f);
        CreateConsumable("Consumables/LeafOfLife", "생명의 나뭇잎", "대상의 체력을 10 회복합니다.", ConsumableEffectType.HealHP, 10f);
        CreateConsumable("Consumables/NerveStabilizer", "신경 안정제", "대상의 정신력을 10 회복합니다.", ConsumableEffectType.HealMental, 10f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Test Consumables Generated.");
    }

    private static void CreateConsumable(string path, string name, string desc, ConsumableEffectType type, float value)
    {
        ConsumableData data = ScriptableObject.CreateInstance<ConsumableData>();
        data.consumableID = path.Replace("Consumables/", "");
        data.consumableName = name;
        data.description = desc;
        data.effectType = type;
        data.effectValue = value;
        // Icon is null for now, UI handles null icons (shows fallback color).

        AssetDatabase.CreateAsset(data, $"Assets/Resources/{path}.asset");
    }
}
