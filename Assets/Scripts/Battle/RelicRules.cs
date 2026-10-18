using TheLastArk.Data;
using TheLastArk.Managers;

namespace TheLastArk.Battle
{
    /// <summary>Relic lookups that never create a ResourceManager (safe in edit-mode tests).</summary>
    public static class RelicRules
    {
        public static bool Has(RelicEffectType type) => ResourceManager.IsInitialized && ResourceManager.Instance.HasRelicEffect(type);

        /// <summary>The relic's data value (RelicData.effectValue), summed over copies; 0 without it.</summary>
        public static float Value(RelicEffectType type) => Has(type) ? ResourceManager.Instance.GetRelicBonus(type) : 0f;

        public static bool IsAlly(BattleCharacter character) => character?.status?.origin != null && !character.status.origin.isEnemy;
        public static bool IsEnemy(BattleCharacter character) => character?.status?.origin != null && character.status.origin.isEnemy;

        /// <summary>회광반조: min(50%, lost HP ratio × 50% ÷ 90%) for one character.</summary>
        public static float TwilightBonus(CharacterStatus status)
        {
            if (status == null || status.FinalMaxHp <= 0f) return 0f;
            float lost = UnityEngine.Mathf.Clamp01(1f - status.currentHp / status.FinalMaxHp);
            return UnityEngine.Mathf.Min(0.5f, lost * 0.5f / 0.9f);
        }
    }
}
