using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TheLastArk.UI
{
    /// <summary>Links an enemy intent hover to its ally warning icons without persistent text or lines.</summary>
    public sealed class EnemyIntentTargetHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private readonly List<CharacterView> targets = new List<CharacterView>();
        private bool hovered;

        public void Bind(IEnumerable<BattleCharacter> recipients)
        {
            SetHighlighted(false);
            targets.Clear();
            if (recipients != null)
                foreach (var actor in recipients)
                {
                    if (actor == null || actor.status == null || actor.status.currentHp <= 0f
                        || actor.status.origin == null || actor.status.origin.isEnemy || actor.view == null) continue;
                    if (!targets.Contains(actor.view)) targets.Add(actor.view);
                }
            if (hovered) SetHighlighted(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData != null && eventData.dragging) return;
            hovered = true;
            SetHighlighted(true);
        }

        public void OnPointerExit(PointerEventData eventData) { Clear(); }
        private void OnDisable() { Clear(); }
        private void Clear() { hovered = false; SetHighlighted(false); }
        private void SetHighlighted(bool value)
        {
            foreach (var view in targets)
                if (view != null) view.SetEnemyTargetHighlighted(value);
        }
    }
}
