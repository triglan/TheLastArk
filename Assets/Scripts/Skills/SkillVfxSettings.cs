using System;
using UnityEngine;

namespace TheLastArk.Skills
{
    public enum SkillVfxPlacement { EffectRecipient, Caster }

    [Serializable]
    public sealed class SkillVfxSettings
    {
        public GameObject prefab;
        public SkillVfxPlacement placement;
        [Min(.01f)] public float scale = 1f;
        [Min(.1f)] public float lifetime = 2f;
        public Vector2 offset;
        public Vector3 rotation;
        public bool preservePrefabMaterials;
        public bool IsConfigured => prefab != null;
        public SkillVfxSettings Copy() => (SkillVfxSettings)MemberwiseClone();

        public BattleCharacter Recipient(BattleCharacter caster, BattleCharacter target)
            => placement == SkillVfxPlacement.Caster ? caster : target;

        public static Vector3 Anchor(BattleCharacter target)
        {
            if (target == null) return Vector3.zero;
            var view = target.view != null ? target.view : target.GetComponent<CharacterView>();
            if (view?.displayImage != null)
            {
                var rect = view.displayImage.rectTransform;
                return rect.TransformPoint(rect.rect.center);
            }
            return target.transform.position;
        }
    }
}
