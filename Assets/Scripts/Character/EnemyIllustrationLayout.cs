using UnityEngine;

namespace TheLastArk.Character
{
    public enum EnemyFormationType { Auto, Humanoid, Creature }

    public static class EnemyIllustrationLayout
    {
        public static bool UsesCreatureLayout(CharacterData data)
        {
            if (data == null) return false;
            if (data.EnemyFormationType != EnemyFormationType.Auto)
                return data.EnemyFormationType == EnemyFormationType.Creature;
            var sprite = data.standingSprite;
            return sprite != null && sprite.rect.width >= sprite.rect.height * 0.9f;
        }

        public const float ReferenceHeightCm = 180f;
        public const float ReferenceHeightUI = 525f;

        public static float HeightUI(CharacterData data)
        {
            return data.EnemyIllustrationHeight / ReferenceHeightCm * ReferenceHeightUI;
        }

        public static float ScaleFor(CharacterData data, Vector2 drawnSize, Vector3 baseScale)
        {
            if (data == null || !data.isEnemy) return 1f;
            float baseHeight = drawnSize.y * Mathf.Abs(baseScale.y);
            return baseHeight > 0.001f ? HeightUI(data) / baseHeight : 1f;
        }

        public static Vector2 FittedSize(Vector2 frame, Sprite sprite, bool preserveAspect)
        {
            if (!preserveAspect || sprite == null || sprite.rect.height <= 0f) return frame;
            float ratio = sprite.rect.width / sprite.rect.height;
            return frame.x / frame.y > ratio
                ? new Vector2(frame.y * ratio, frame.y)
                : new Vector2(frame.x, frame.x / ratio);
        }

        // Unity Image positions its aspect-fitted rectangle around the RectTransform pivot.
        public static Vector2 PositionOffset(CharacterData data, Vector2 drawnSize, Vector2 frameSize, Vector2 pivot, Vector3 baseScale, float presentationScale = 1f)
        {
            if (data == null || !data.isEnemy) return Vector2.zero;
            Vector2 offset = data.EnemyIllustrationOffset;
            if (data.EnemyIllustrationKeepFeet)
                offset.y += (drawnSize.y * ScaleFor(data, drawnSize, baseScale) * presentationScale - frameSize.y) * pivot.y * baseScale.y;
            return offset;
        }
    }
}
