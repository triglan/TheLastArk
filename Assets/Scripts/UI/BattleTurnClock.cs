using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    public sealed class BattleTurnClock : MonoBehaviour
    {
        private const float Duration = 0.48f;
        private RectTransform gear;
        private TextMeshProUGUI number;
        private TextMeshProUGUI outgoing;
        private Coroutine transition;
        private int targetTurn = -1;
        public int DisplayedTurn { get; private set; } = -1;
        public bool IsAnimating => transition != null;

        public static BattleTurnClock Create(Transform parent, TextMeshProUGUI label)
        {
            var existing = parent.Find("BattleTurnClock");
            if (existing != null) return existing.GetComponent<BattleTurnClock>();
            var root = new GameObject("BattleTurnClock", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -61f);
            rect.sizeDelta = new Vector2(112f, 112f);
            var clock = root.AddComponent<BattleTurnClock>();
            clock.Build(label);
            return clock;
        }

        private void Build(TextMeshProUGUI label)
        {
            MakeGear("Clock Back", 116f, new Color(0.06f, 0.075f, 0.09f), true);
            gear = MakeGear("Rotating Gear", 108f, new Color(0.72f, 0.84f, 0.91f), false);
            var hand = new GameObject("Clock Hand", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            hand.transform.SetParent(gear, false);
            hand.color = new Color(1f, 0.77f, 0.34f);
            hand.raycastTarget = false;
            hand.rectTransform.sizeDelta = new Vector2(4f, 16f);
            hand.rectTransform.anchoredPosition = new Vector2(0f, 41f);
            number = label;
            number.transform.SetParent(transform, false);
            number.rectTransform.anchorMin = number.rectTransform.anchorMax = Vector2.one * 0.5f;
            number.rectTransform.pivot = Vector2.one * 0.5f;
            number.rectTransform.sizeDelta = new Vector2(74f, 46f);
            number.rectTransform.anchoredPosition = new Vector2(0f, 3f);
            number.fontSize = 30f;
            number.enableAutoSizing = true;
            number.fontSizeMin = 16f;
            number.fontSizeMax = 30f;
            number.raycastTarget = false;
            outgoing = Instantiate(number, transform);
            outgoing.name = "Previous Turn";
            outgoing.alpha = 0f;
            var caption = Instantiate(number, transform);
            caption.name = "Turn Caption";
            caption.enableAutoSizing = false;
            caption.fontSize = 10f;
            caption.characterSpacing = 2f;
            caption.text = "TURN";
            caption.color = new Color(0.65f, 0.72f, 0.78f);
            caption.rectTransform.anchoredPosition = new Vector2(0f, -23f);
            caption.rectTransform.sizeDelta = new Vector2(64f, 18f);
        }

        private RectTransform MakeGear(string name, float size, Color tint, bool face)
        {
            var graphic = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer)).AddComponent<BattleTurnGearGraphic>();
            graphic.transform.SetParent(transform, false);
            graphic.rectTransform.sizeDelta = Vector2.one * size;
            graphic.Face = face;
            graphic.color = tint;
            graphic.raycastTarget = false;
            return graphic.rectTransform;
        }

        public void SetTurn(int turn)
        {
            turn = Mathf.Max(1, turn);
            if (targetTurn == turn) return;
            if (transition != null) StopCoroutine(transition);
            targetTurn = turn;
            if (DisplayedTurn < 0 || !Application.isPlaying || !isActiveAndEnabled)
            {
                Finish();
                return;
            }
            transition = StartCoroutine(AnimateTurn());
        }

        private IEnumerator AnimateTurn()
        {
            outgoing.text = DisplayedTurn.ToString();
            number.text = targetTurn.ToString();
            float startAngle = gear.localEulerAngles.z;
            float endAngle = -((targetTurn - 1) % 12) * 30f;
            for (float elapsed = 0f; elapsed < Duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / Duration);
                gear.localRotation = Quaternion.Euler(0f, 0f, startAngle + Mathf.DeltaAngle(startAngle, endAngle) * t);
                outgoing.alpha = 1f - t;
                outgoing.rectTransform.anchoredPosition = new Vector2(0f, 3f + 20f * t);
                number.alpha = t;
                number.rectTransform.anchoredPosition = new Vector2(0f, 3f - 20f * (1f - t));
                yield return null;
            }
            Finish();
        }

        private void Finish()
        {
            DisplayedTurn = targetTurn;
            number.text = targetTurn.ToString();
            number.alpha = 1f;
            number.rectTransform.anchoredPosition = new Vector2(0f, 3f);
            outgoing.alpha = 0f;
            gear.localRotation = Quaternion.Euler(0f, 0f, -((targetTurn - 1) % 12) * 30f);
            transition = null;
        }

        private void OnDisable()
        {
            if (transition != null) StopCoroutine(transition);
            if (number != null && targetTurn >= 1) Finish();
        }
    }
}
