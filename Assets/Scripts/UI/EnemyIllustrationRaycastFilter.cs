using UnityEngine;

namespace TheLastArk.UI
{
    // Image.preserveAspect only fits the artwork; its RectTransform still includes empty padding.
    public sealed class EnemyIllustrationRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        private CharacterView view;

        private void Awake() => view = GetComponentInParent<CharacterView>();

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (view == null) view = GetComponentInParent<CharacterView>();
            if (view == null) return false;
            var enemy = view.GetComponent<EnemyBattleCharacter>();
            if (enemy == null || !enemy.HasRuntimeStatus || enemy.CurrentHp <= 0f) return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)view.transform,
                screenPoint, eventCamera, out var localPoint) && view.IllustrationBounds.Contains(localPoint);
        }
    }
}
