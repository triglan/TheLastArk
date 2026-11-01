using System.Collections.Generic;
using UnityEngine;

namespace TheLastArk.Skills
{
    /// <summary>One visual per setting and recipient per cast, even for a damage+debuff skill.</summary>
    public sealed class SkillVfxPlayback
    {
        private readonly SkillVfxSettings skill;
        private readonly bool preferImpacts;
        private readonly HashSet<(SkillVfxSettings, BattleCharacter)> played = new HashSet<(SkillVfxSettings, BattleCharacter)>();
        public SkillVfxPlayback(SkillVfxSettings settings, List<EffectEntry> effects = null)
        {
            skill = settings;
            preferImpacts = effects != null && effects.Exists(e => e != null && IsImpact(e.type));
        }
        private static bool IsImpact(EffectType type) => type == EffectType.Damage || type == EffectType.Shield
            || type == EffectType.Heal || type == EffectType.MentalDamage || type == EffectType.MentalHeal || type == EffectType.Resurrection;

        public bool TryResolve(EffectEntry effect, BattleCharacter actor, BattleCharacter target,
            out SkillVfxSettings settings, out BattleCharacter recipient)
        {
            settings = effect?.visualEffect?.IsConfigured == true ? effect.visualEffect : skill;
            recipient = settings?.Recipient(actor, target);
            if (effect?.visualEffect?.IsConfigured != true && preferImpacts && !IsImpact(effect.type)) return false;
            return settings?.IsConfigured == true && recipient != null && played.Add((settings, recipient));
        }

        public void Play(EffectEntry effect, BattleCharacter actor, BattleCharacter target, string skillName)
        {
            if (!Application.isPlaying) return;
            if (effect?.visualEffect?.IsConfigured == true || skill?.IsConfigured == true)
            {
                if (TryResolve(effect, actor, target, out var settings, out var recipient))
                    VFXManager.Instance.PlayConfigured(settings, recipient);
                return;
            }
            if (!string.IsNullOrEmpty(effect.customVfxName))
                VFXManager.Instance.SpawnVFX(effect.customVfxName, SkillVfxSettings.Anchor(target));
            else VFXManager.Instance.PlaySmartSkillEffect(skillName, effect.type, effect.damageType, target);
        }
    }
}
