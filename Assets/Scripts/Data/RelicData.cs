using UnityEngine;

namespace TheLastArk.Data
{
    public enum RelicEffectType
    {
        RestBonusHeal,
        BonusAttack,
        BonusMaxHP,
        BonusMaxMental,
        BonusAP
    }

    [CreateAssetMenu(fileName = "NewRelic", menuName = "TheLastArk/Relic Data")]
    public class RelicData : ScriptableObject
    {
        public string relicID;
        public string relicName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        public RelicEffectType effectType;
        public float effectValue;
    }
}
