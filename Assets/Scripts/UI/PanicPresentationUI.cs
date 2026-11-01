using System.Collections;
using System.Collections.Generic;
using TheLastArk.Battle;
using TMPro;
using UnityEngine;

namespace TheLastArk.UI
{
    public sealed class PanicPresentationUI : MonoBehaviour
    {
        private readonly Queue<(string name, Sprite art, PanicType type, CharacterView view)> _queue
            = new Queue<(string, Sprite, PanicType, CharacterView)>();
        private readonly HashSet<CharacterView> _pendingPulses = new HashSet<CharacterView>();
        private CanvasGroup _group;
        private RectTransform _composition;
        private RectTransform _artRect;
        private FeatheredIllustration _art;
        private CanvasGroup _artGroup, _frameGroup, _stampGroup;
        private UnityEngine.UI.Image _frame;
        private UnityEngine.UI.Image _flash;
        private PanicImpactAudio _audio;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _panicName;
        private const float ImpactAt = .12f, StampAt = .26f;
        private const float Duration = 2.45f, FadeOutDuration = .48f;
        private float _shakeStrength;
        public bool IsPresenting { get; private set; }

        public void Enqueue(BattleCharacter actor)
        {
            var data = actor.status.origin;
            Sprite sprite = data.GetPanicIllustration(actor.status.panic.Type);
            _queue.Enqueue((actor.characterName, sprite, actor.status.panic.Type,
                actor.view != null ? actor.view : actor.GetComponent<CharacterView>()));
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
                _art.SetSprite(entry.art);
                _art.enabled = entry.art != null;
                _title.text = entry.name;
                _panicName.text = PanicState.Name(entry.type);
                int shake = Mathf.Clamp(PlayerPrefs.GetInt("Setting.ScreenShake", 1), 0, 2);
                _shakeStrength = shake == 0 ? 0 : shake == 1 ? 7 : 13;
                if (entry.view != null) _pendingPulses.Add(entry.view);
                bool playedImpact = false;
                float startedAt = Time.realtimeSinceStartup;
                for (float elapsed = 0f; elapsed < Duration; elapsed = Time.realtimeSinceStartup - startedAt)
                {
                    ApplyFrame(elapsed);
                    if (!playedImpact && elapsed >= ImpactAt)
                    {
                        _audio.Play();
                        playedImpact = true;
                    }
                    yield return null;
                }
            }
            _group.alpha=0;
            _group.gameObject.SetActive(false);
            IsPresenting = false;
            // Wait until the entire queue is gone, so chained panics don't hide the icon pulse.
            foreach (var view in _pendingPulses)
                if (view != null) view.PulsePanicIcon();
            _pendingPulses.Clear();
        }

        private void ApplyFrame(float elapsed)
        {
            _group.alpha = Ease(elapsed / .10f) * Ease((Duration - elapsed) / FadeOutDuration);
            float impact = Mathf.Clamp01((elapsed - ImpactAt) / .26f);
            float settle = 1 - Mathf.Pow(1 - impact, 3);
            _artGroup.alpha = Ease((elapsed - ImpactAt) / .07f);
            _artRect.localScale = Vector3.one * Mathf.Lerp(1.14f, 1f, settle);
            _frameGroup.alpha = Ease((elapsed - ImpactAt) / .12f);
            _frame.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.32f, 1.18f, settle);
            _frame.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(-3, 0, settle));

            float shock = elapsed >= ImpactAt ? Mathf.Pow(1 - impact, 2) : 0;
            _composition.anchoredPosition = new Vector2(Mathf.Sin(impact * 49), Mathf.Sin(impact * 71))
                * (shock * _shakeStrength);
            float flash = elapsed >= ImpactAt ? Mathf.Clamp01(1 - (elapsed - ImpactAt) / .14f) : 0;
            _flash.color = new Color(.65f, .025f, .075f, flash * .16f);
            _title.alpha = Ease((elapsed - .18f) / .20f);
            float stamp = Ease((elapsed - StampAt) / .16f);
            _stampGroup.alpha = Ease((elapsed - StampAt) / .04f);
            _panicName.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.65f, 1f, stamp);
            _panicName.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(-5, 0, stamp));
        }

        private static float Ease(float value) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(value));

        private void EnsureUI()
        {
            if (_group != null) return;
            var root = new GameObject("PanicPresentation", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(CanvasGroup));
            // BattleManager may itself be inside the battle Canvas. Keep this a root
            // canvas so screen anchors use the viewport rather than a 100x100 child rect.
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            _group = root.GetComponent<CanvasGroup>();
            _audio = root.AddComponent<PanicImpactAudio>();
            var veil = Rect("Veil", root.transform, Vector2.zero, Vector2.one).gameObject.AddComponent<UnityEngine.UI.Image>();
            veil.color = new Color(0.018f, 0.012f, 0.025f, 0.96f);
            var vignette = Rect("PanicVignette", root.transform, Vector2.zero, Vector2.one).gameObject.AddComponent<PanicVignetteGraphic>();
            vignette.raycastTarget = false;
            _flash = Rect("ImpactFlash", root.transform, Vector2.zero, Vector2.one).gameObject.AddComponent<UnityEngine.UI.Image>();
            _flash.raycastTarget = false;
            _composition = Rect("FracturedIllustration", root.transform, Vector2.zero, Vector2.one);
            Vector2 artMin = new Vector2(.22f, .16f), artMax = new Vector2(.78f, .84f);
            _artRect = Rect("CharacterIllustration", _composition, artMin, artMax);
            _art = _artRect.gameObject.AddComponent<FeatheredIllustration>();
            _art.raycastTarget = false;
            _artGroup = _artRect.gameObject.AddComponent<CanvasGroup>();
            _frame = Rect("PanicIllustratedFrame", _composition, artMin, artMax).gameObject.AddComponent<UnityEngine.UI.Image>();
            _frame.sprite = Resources.Load<Sprite>("UI/Panic/PanicFrame");
            _frame.enabled = _frame.sprite != null;
            _frame.preserveAspect = true;
            _frame.raycastTarget = false;
            _frameGroup = _frame.gameObject.AddComponent<CanvasGroup>();
            // Keep the illustration readable; only its feathered edges reveal the surrounding thorns.
            _frame.transform.SetAsFirstSibling();
            _title = Label("CharacterName", root.transform, new Vector2(0.15f, 0.86f), new Vector2(0.85f, 0.95f), 34, new Color(.9f,.86f,.84f));
            _panicName = Label("PanicName", root.transform, new Vector2(0.15f, 0.045f), new Vector2(0.85f, 0.145f), 64, new Color(1f, 0.52f, 0.48f));
            _panicName.fontStyle = FontStyles.Bold;
            _panicName.characterSpacing = 5;
            _stampGroup = _panicName.gameObject.AddComponent<CanvasGroup>();
            ApplyFrame(0);
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
            _pendingPulses.Clear();
            IsPresenting = false;
            if (_group != null) {_group.alpha=0;_group.gameObject.SetActive(false);}
        }

        private void OnDestroy()
        {
            if (_group != null) Destroy(_group.gameObject);
        }
    }
}
