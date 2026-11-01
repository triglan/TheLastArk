using System.Collections;
using UnityEngine;

namespace TheLastArk.UI
{
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public sealed class PanicIconPulse : MonoBehaviour
    {
        private UnityEngine.UI.Image icon;
        private Color restingColor;
        private Coroutine pulse;

        public void Play()
        {
            if (!isActiveAndEnabled || !Application.isPlaying) return;
            ResetPulse();
            icon = GetComponent<UnityEngine.UI.Image>();
            restingColor = icon.color;
            pulse = StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            const float duration = .65f;
            float startedAt = Time.realtimeSinceStartup;
            for (float t = 0; t < duration; t = Time.realtimeSinceStartup - startedAt)
            {
                float amount = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / duration));
                transform.localScale = Vector3.one * (1 + amount * .3f);
                icon.color = Color.Lerp(restingColor, new Color(.95f, .16f, .22f, 1), amount);
                yield return null;
            }
            Restore();
            pulse = null;
        }

        private void Restore()
        {
            transform.localScale = Vector3.one;
            if (icon != null) icon.color = restingColor;
        }
        private void ResetPulse()
        {
            if (pulse != null) StopCoroutine(pulse);
            pulse = null;
            Restore();
        }
        private void OnDisable() { ResetPulse(); }
    }
}
