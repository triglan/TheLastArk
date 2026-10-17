using System.Collections.Generic;
using UnityEngine;

namespace TheLastArk.UI
{
    public sealed class BattleEnemyFormationLayout : MonoBehaviour
    {
        private const float CenterX = 0.60f;
        private const float FrontBaseline = 0.225f;
        private const float BackRowDepth = 0.045f;
        private const float LeftBoundary = 0.28f;
        private const float RightBoundary = 0.92f;
        private const float HudCeiling = 0.90f;
        private EnemyBattleCharacter[] slots;
        private RectTransform canvas;
        private int previousHash;
        private readonly List<CharacterView> visible = new List<CharacterView>(4);
        public IReadOnlyList<EnemyBattleCharacter> Slots => slots;

        public void Initialize(Transform enemyParent)
        {
            slots = enemyParent.GetComponentsInChildren<EnemyBattleCharacter>(true);
            canvas = enemyParent.GetComponentInParent<Canvas>()?.rootCanvas.transform as RectTransform;
            if (canvas != null)
            {
                int order = canvas.GetComponent<Canvas>().sortingOrder;
                var allies = enemyParent.parent?.Find("Player Character");
                if (allies != null) LiftGraphics(allies, order + 20);
                var controls = GameObject.Find("TopBarCanvas")?.GetComponent<Canvas>();
                if (controls != null) controls.sortingOrder = Mathf.Max(controls.sortingOrder, order + 30);
            }
            Refresh();
        }

        private void LateUpdate()
        {
            if (slots == null || canvas == null) return;
            int hash = canvas.rect.size.GetHashCode();
            foreach (var slot in slots)
            {
                if (slot == null) continue;
                var data = slot.CurrentStatus?.origin;
                unchecked
                {
                    hash = hash * 31 + (IsVisible(slot) ? slot.GetInstanceID() : 0);
                    hash = hash * 31 + (slot.CurrentStatus?.GetHashCode() ?? 0);
                    hash = hash * 31 + (data != null ? data.GetInstanceID() ^ (int)data.EnemyFormationType : 0);
                    var sprite = slot.GetComponent<CharacterView>()?.displayImage?.sprite;
                    hash = hash * 31 + (sprite != null ? sprite.GetInstanceID() : 0);
                    hash = hash * 31 + (data != null ? data.EnemyIllustrationHeight.GetHashCode() ^ data.EnemyIllustrationOffset.GetHashCode() : 0);
                    hash = hash * 31 + (slot.CurrentStatus?.activeStatusEffects.Count ?? 0);
                    hash = hash * 31 + (slot.GetComponent<CharacterView>()?.EnemyHUDHeight.GetHashCode() ?? 0);
                }
            }
            if (hash == previousHash) return;
            previousHash = hash;
            Refresh();
        }

        private static bool IsVisible(EnemyBattleCharacter slot) => slot != null && slot.gameObject.activeInHierarchy && slot.HasRuntimeStatus && slot.CurrentHp > 0f;

        // Slot order stays stable; drawing order is independently sorted back-to-front.
        public static float NormalizedX(int index, int count)
        {
            if (count <= 1) return CenterX;
            float span = count == 2 ? 0.18f : count == 3 ? 0.24f : 0.28f;
            return CenterX - span * 0.5f + span * index / (count - 1);
        }

        public static float NormalizedBaseline(int index, int count) => FrontBaseline
            + (count > 1 && index % 2 == 1 ? BackRowDepth : 0f);

        public static float CreatureX(int index, int count)
        {
            if (count <= 1) return CenterX;
            if (count == 2) return index == 0 ? 0.48f : 0.72f;
            if (count == 3) return index == 0 ? 0.46f : index == 1 ? 0.62f : 0.78f;
            return (index < 2 ? 0.46f : 0.76f) + (index % 2 == 1 ? 0.03f : 0f);
        }

        public static float CreatureBaseline(int index, int count) => FrontBaseline
            + (count > 1 && index % 2 == 1 ? 0.215f : 0f);

        public void Refresh()
        {
            if (slots == null || canvas == null) return;
            visible.Clear();
            foreach (var slot in slots)
                if (IsVisible(slot))
                {
                    var view = slot.GetComponent<CharacterView>();
                    if (view != null) { view.SetPresentationScale(1f); visible.Add(view); }
                }
            int creatureCount = 0;
            foreach (var view in visible)
                if (TheLastArk.Character.EnemyIllustrationLayout.UsesCreatureLayout(view.GetComponent<EnemyBattleCharacter>().CurrentStatus.origin)) creatureCount++;
            bool mixedFormation = creatureCount > 0 && creatureCount < visible.Count;
            for (int i = 0; i < visible.Count; i++)
            {
                var view = visible[i];
                bool creature = TheLastArk.Character.EnemyIllustrationLayout.UsesCreatureLayout(view.GetComponent<EnemyBattleCharacter>().CurrentStatus.origin);
                // A tall boss must not shrink every other combatant in the formation.
                float fit = 1.12f;
                Rect bounds = view.IllustrationBounds;
                float sx = Mathf.Abs(view.transform.lossyScale.x / canvas.lossyScale.x);
                float sy = Mathf.Abs(view.transform.lossyScale.y / canvas.lossyScale.y);
                float x = creature && !mixedFormation ? CreatureX(i, visible.Count) : NormalizedX(i, visible.Count);
                if (mixedFormation) x = CenterX - 0.18f + 0.36f * i / (visible.Count - 1);
                // Overlap between rows is intentional. Fit the battlefield, not four narrow cells.
                // Every slot uses the same envelope: edge slots must not shrink identical art.
                float width = visible.Count == 1 ? canvas.rect.width * 0.60f * 0.88f
                    : canvas.rect.width * 2f * Mathf.Min(NormalizedX(0, visible.Count) - LeftBoundary,
                        RightBoundary - NormalizedX(visible.Count - 1, visible.Count));
                float height = canvas.rect.height * (HudCeiling - NormalizedBaseline(visible.Count > 1 ? 1 : 0, visible.Count));
                float baseline = NormalizedBaseline(i, visible.Count);
                if (creature && mixedFormation)
                {
                    // A landscape illustration (horse, cape, long weapon) is not a small creature.
                    // Mixed parties share the humanoid height/row envelope; allow wide art to
                    // overlap within the battlefield instead of squeezing it into a creature cell.
                    width = canvas.rect.width * (RightBoundary - LeftBoundary) * 0.94f;
                }
                else if (creature && visible.Count > 1)
                {
                    width = canvas.rect.width * (visible.Count == 2 ? 0.32f : visible.Count == 3 ? 0.28f : 0.26f);
                    baseline = CreatureBaseline(i, visible.Count);
                    height = canvas.rect.height * (HudCeiling - CreatureBaseline(1, visible.Count));
                }
                if (bounds.width > 0f) fit = Mathf.Min(fit, width / (bounds.width * sx));
                if (bounds.height > 0f) fit = Mathf.Min(fit, (height - view.EnemyHUDHeight * sy) / (bounds.height * sy));
                view.SetPresentationScale(Mathf.Clamp(fit, 0.1f, 1.12f));
                x = canvas.rect.xMin + canvas.rect.width * x;
                float y = canvas.rect.yMin + canvas.rect.height * baseline - view.IllustrationBaseline * sy;
                // Authored sprite offsets must not move artwork into the combat controls.
                bounds = view.IllustrationBounds;
                x = Mathf.Clamp(x, canvas.rect.xMin + canvas.rect.width * LeftBoundary - bounds.xMin * sx,
                    canvas.rect.xMin + canvas.rect.width * RightBoundary - bounds.xMax * sx);
                y = Mathf.Clamp(y, canvas.rect.yMin + canvas.rect.height * FrontBaseline - bounds.yMin * sy,
                    canvas.rect.yMin + canvas.rect.height * HudCeiling - (bounds.yMax + view.EnemyHUDHeight) * sy);
                view.transform.position = canvas.TransformPoint(new Vector3(x, y, 0f));
                if (view.displayImage != null && view.displayImage.GetComponent<EnemyIllustrationRaycastFilter>() == null)
                    view.displayImage.gameObject.AddComponent<EnemyIllustrationRaycastFilter>();
                // These UI combatants use their drawn artwork and bars for both clicks and hover.
                // Legacy full-height colliders otherwise steal EventSystem pointer-enter events.
                foreach (var collider in view.GetComponents<Collider2D>()) collider.enabled = false;
                KeepHudAboveIllustrations(view);
            }
            // Later siblings are drawn in front. Do not reorder the cached logical slots.
            for (int row = 1; row >= 0; row--)
                for (int i = 0; i < visible.Count; i++)
                    if (i % 2 == row) visible[i].transform.SetAsLastSibling();
        }

        private void KeepHudAboveIllustrations(CharacterView view)
        {
            // Back-row bodies may be occluded, but their bars and intent remain selectable.
            int order = canvas.GetComponent<Canvas>().sortingOrder + 10;
            foreach (Transform child in view.transform)
            {
                if (view.displayImage != null && child == view.displayImage.transform) continue;
                if (child.GetComponentInChildren<UnityEngine.UI.Graphic>(true) == null) continue;
                LiftGraphics(child, order);
            }
        }

        private static void LiftGraphics(Transform target, int order)
        {
            var overlay = target.GetComponent<Canvas>();
            if (overlay == null) overlay = target.gameObject.AddComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingOrder = order;
            if (target.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                target.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
    }
}
