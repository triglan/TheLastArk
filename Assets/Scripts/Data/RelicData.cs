using UnityEngine;

namespace TheLastArk.Data
{
    public enum RelicRarity
    {
        Common,
        Legendary
    }

    public enum RelicEffectType
    {
        RestBonusHeal,
        BonusAttack,
        BonusMaxHP,
        BonusMaxMental,
        BonusAP,
        FreeRest,
        CommLevelBonus,
        ShopDiscount,
        TavernDiscount,
        TavernExtraMerc,
        ShopFirstLegendary,
        ExtraRefresh
    }

    [CreateAssetMenu(fileName = "NewRelic", menuName = "TheLastArk/Relic Data")]
    public class RelicData : ScriptableObject
    {
        public string relicID;
        public string relicName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        public RelicRarity rarity = RelicRarity.Common;
        public RelicEffectType effectType;
        public float effectValue;
    }
}
