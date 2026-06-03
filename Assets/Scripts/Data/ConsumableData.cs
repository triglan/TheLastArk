using UnityEngine;

namespace TheLastArk.Data
{
    public enum ConsumableEffectType
    {
        DamageSingle,
        DamageAll,
        HealHP,
        HealMental
    }

    [CreateAssetMenu(fileName = "NewConsumable", menuName = "TheLastArk/Consumable Data")]
    public class ConsumableData : ScriptableObject
    {
        public string consumableID;
        public string consumableName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        public ConsumableEffectType effectType;
        public float effectValue;
    }
}
