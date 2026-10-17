using System.Collections;
using System.Collections.Generic;
using TheLastArk.Battle;
using TMPro;
using UnityEngine;

namespace TheLastArk.UI
{
    public sealed class PanicPresentationUI : MonoBehaviour
    {
        private readonly Queue<(string name, Sprite art, PanicType type)> _queue = new Queue<(string, Sprite, PanicType)>();
        private CanvasGroup _group;
        private RectTransform _artRect;
        private UnityEngine.UI.Image _art;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _description;
        public bool IsPresenting { get; private set; }

        public void Enqueue(BattleCharacter actor)
        {
            var data = actor.status.origin;
            var illustrations = data.panicIllustrations?.FindAll(s => s != null);
            Sprite sprite = illustrations != null && illustrations.Count > 0
                ? illustrations[Random.Range(0, illustrations.Count)]
                : data.standingSprite != null ? data.standingSprite : data.portraitSprite;
            _queue.Enqueue((actor.characterName, sprite, actor.status.panic.Type));
            if (!IsPresenting) StartCoroutine(Present());
        }

        private IEnumerator Present()
        {
            IsPresenting = true;
            EnsureUI();
            _group.gameObject.SetActive(true);
            while (_queue.Count > 0)
            {
                var entry = _queue.Dequeue();
                _art.sprite = entry.art;
                _art.enabled = entry.art != null;
                _title.text = $"<size=28>{entry.name}</size>\n{PanicState.Name(entry.type)}";
                _description.text = PanicState.Description(entry.type) + "\n<size=18>전투 종료까지 유지</size>";
                const float duration = 2.2f;
                for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
                {
                    _group.alpha = Mathf.Min(Mathf.Clamp01(elapsed / 0.16f), Mathf.Clamp01((duration - elapsed) / 0.3f));
                    float scale = Mathf.Lerp(1.08f, 1f, Mathf.Clamp01(elapsed / 0.45f));
                    _artRect.localScale = Vector3.one * scale;
                    yield return null;
                }
            }
            _group.gameObject.SetActive(false);
            IsPresenting = false;
        }

        private void EnsureUI()
        {
            if (_group != null) return;
            var root = new GameObject("PanicPresentation", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(CanvasGroup));
            // BattleManager may itself be inside the battle Canvas. Keep this a root
            // canvas so screen anchors use the viewport rather than a 100x100 child rect.
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            _group = root.GetComponent<CanvasGroup>();
            var veil = Rect("Veil", root.transform, Vector2.zero, Vector2.one).gameObject.AddComponent<UnityEngine.UI.Image>();
            veil.color = new Color(0.035f, 0.01f, 0.025f, 0.88f);
            var stripe = Rect("CrimsonBand", root.transform, new Vector2(0, 0.16f), new Vector2(1, 0.84f)).gameObject.AddComponent<UnityEngine.UI.Image>();
            stripe.color = new Color(0.22f, 0.025f, 0.045f, 0.8f);
            stripe.raycastTarget = false;
            _artRect = Rect("CharacterIllustration", root.transform, new Vector2(0.26f, 0.24f), new Vector2(0.74f, 0.94f));
            _art = _artRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            _art.preserveAspect = true;
            _art.raycastTarget = false;
            _title = Label("PanicName", root.transform, new Vector2(0.15f, 0.14f), new Vector2(0.85f, 0.34f), 58, new Color(1f, 0.73f, 0.65f));
            _description = Label("PanicEffect", root.transform, new Vector2(0.12f, 0.035f), new Vector2(0.88f, 0.15f), 25, Color.white);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static TextMeshProUGUI Label(string name, Transform parent, Vector2 min, Vector2 max, int size, Color color)
        {
            var label = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            TMPFontManager.ApplyFont(label);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = size; label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _queue.Clear();
            IsPresenting = false;
            if (_group != null) _group.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_group != null) Destroy(_group.gameObject);
        }
    }
}
