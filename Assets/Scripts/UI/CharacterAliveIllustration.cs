using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace TheLastArk.UI
{
    /// <summary>
    /// Assets/Sprites/Backgrounds/ 및 Resources에서 캐릭터 이름 기반 배경 일러스트를 자동 검색 및 로드하는 헬퍼
    /// </summary>
    public static class CharacterBackgroundLoader
    {
        private static readonly Dictionary<string, Sprite> runtimeCache = new Dictionary<string, Sprite>();

        public static Sprite LoadCharacterBackground(CharacterData data)
        {
            if (data == null) return null;

            List<string> candidates = new List<string>();

            void AddCandidate(string val)
            {
                if (string.IsNullOrWhiteSpace(val)) return;
                string clean = val.Trim();
                if (!candidates.Contains(clean)) candidates.Add(clean);

                string noSpace = clean.Replace(" ", "");
                if (!candidates.Contains(noSpace)) candidates.Add(noSpace);

                string[] parts = clean.Split(new char[] { ' ', '_', '-' }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    if (p.Length >= 2 && !candidates.Contains(p))
                    {
                        candidates.Add(p);
                    }
                }
            }

            // 캐릭터 이름 및 식별자 후보군 등록 (예: "카에데", "이지스", "세레스", "알렉스 바스티온" -> "알렉스")
            AddCandidate(data.characterName);
            AddCandidate(data.DisplayName);
            AddCandidate(data.characterId);
            AddCandidate(data.jobName);
            if (!string.IsNullOrEmpty(data.name))
            {
                AddCandidate(data.name.Replace("_Data", ""));
            }

            // 1. 캐시 및 Resources.Load 우선 검색
            foreach (var key in candidates)
            {
                if (runtimeCache.TryGetValue(key, out Sprite cached) && cached != null)
                {
                    return cached;
                }

                Sprite resSprite = Resources.Load<Sprite>($"Sprites/Backgrounds/{key}");
                if (resSprite != null)
                {
                    runtimeCache[key] = resSprite;
                    return resSprite;
                }

                resSprite = Resources.Load<Sprite>($"Backgrounds/{key}");
                if (resSprite != null)
                {
                    runtimeCache[key] = resSprite;
                    return resSprite;
                }
            }

            // 2. 디스크 상의 디렉토리 검색 (Assets/Sprites/Backgrounds 최우선)
            string[] searchRelativePaths = new string[]
            {
                "Assets/Sprites/Backgrounds",
                "Assets/Backgrounds",
                "Assets/Resources/Backgrounds",
                "Assets/Resources/Sprites/Backgrounds"
            };

#if UNITY_EDITOR
            foreach (string backgroundsDir in searchRelativePaths)
            {
                if (!Directory.Exists(backgroundsDir)) continue;

                string[] files = Directory.GetFiles(backgroundsDir);
                foreach (string file in files)
                {
                    if (file.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase)) continue;
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file).Trim();
                    string fileNameNoSpace = fileNameWithoutExt.Replace(" ", "");

                    bool matched = false;
                    foreach (var key in candidates)
                    {
                        string keyNoSpace = key.Replace(" ", "");
                        if (string.Equals(fileNameWithoutExt, key, System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(fileNameNoSpace, keyNoSpace, System.StringComparison.OrdinalIgnoreCase) ||
                            (key.Length >= 2 && fileNameWithoutExt.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (fileNameWithoutExt.Length >= 2 && key.IndexOf(fileNameWithoutExt, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (keyNoSpace.Length >= 2 && fileNameNoSpace.IndexOf(keyNoSpace, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (fileNameNoSpace.Length >= 2 && keyNoSpace.IndexOf(fileNameNoSpace, System.StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            matched = true;
                            break;
                        }
                    }

                    if (matched)
                    {
                        string normalizedPath = file.Replace('\\', '/');
                        Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(normalizedPath);
                        if (s == null)
                        {
                            // TextureImporter가 Sprite가 아니면 자동 변환
                            var importer = UnityEditor.AssetImporter.GetAtPath(normalizedPath) as UnityEditor.TextureImporter;
                            if (importer != null && importer.textureType != UnityEditor.TextureImporterType.Sprite)
                            {
                                importer.textureType = UnityEditor.TextureImporterType.Sprite;
                                importer.SaveAndReimport();
                                s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(normalizedPath);
                            }
                        }

                        if (s != null)
                        {
                            runtimeCache[data.characterName ?? data.name] = s;
                            return s;
                        }
                    }
                }
            }
#else
            List<string> runtimeDirs = new List<string>();
            foreach (var rel in searchRelativePaths)
            {
                string sub = rel.StartsWith("Assets/") ? rel.Substring(7) : rel;
                runtimeDirs.Add(Path.Combine(Application.dataPath, sub));
                try
                {
                    string parent = Directory.GetParent(Application.dataPath)?.FullName;
                    if (!string.IsNullOrEmpty(parent))
                    {
                        runtimeDirs.Add(Path.Combine(parent, rel.Replace('/', Path.DirectorySeparatorChar)));
                    }
                }
                catch {}
            }

            foreach (string runtimeBgDir in runtimeDirs)
            {
                if (!Directory.Exists(runtimeBgDir)) continue;

                string[] files = Directory.GetFiles(runtimeBgDir);
                foreach (string file in files)
                {
                    if (file.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase)) continue;
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file).Trim();
                    string fileNameNoSpace = fileNameWithoutExt.Replace(" ", "");

                    bool matched = false;
                    foreach (var key in candidates)
                    {
                        string keyNoSpace = key.Replace(" ", "");
                        if (string.Equals(fileNameWithoutExt, key, System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(fileNameNoSpace, keyNoSpace, System.StringComparison.OrdinalIgnoreCase) ||
                            (key.Length >= 2 && fileNameWithoutExt.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (fileNameWithoutExt.Length >= 2 && key.IndexOf(fileNameWithoutExt, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (keyNoSpace.Length >= 2 && fileNameNoSpace.IndexOf(keyNoSpace, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (fileNameNoSpace.Length >= 2 && keyNoSpace.IndexOf(fileNameNoSpace, System.StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            matched = true;
                            break;
                        }
                    }

                    if (matched)
                    {
                        try
                        {
                            byte[] bytes = File.ReadAllBytes(file);
                            Texture2D tex = new Texture2D(2, 2);
                            if (tex.LoadImage(bytes))
                            {
                                Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                                runtimeCache[data.characterName ?? data.name] = s;
                                return s;
                            }
                        }
                        catch {}
                    }
                }
            }
#endif

            return null;
        }

        public static void ClearCache()
        {
            runtimeCache.Clear();
        }
    }

    /// <summary>
    /// 캐릭터 배경 일러스트에 살아있는 숨쉬기(Breathing), 미세 유영(Sway), 마우스 시선 반응(Parallax)을 부여하는 컴포넌트
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class CharacterAliveIllustration : MonoBehaviour
    {
        [Header("Target Image & CanvasGroup")]
        public Image targetImage;
        public CanvasGroup canvasGroup;

        [Header("1. Breathing Motion (호흡 / 팽창 수축)")]
        [Tooltip("숨쉬기 움직임 사용 여부")]
        public bool enableBreathing = true;
        [Tooltip("호흡 주기 속도")]
        public float breatheSpeed = 1.15f;
        [Tooltip("호흡 크기 확대/축소 비율")]
        public float breatheScaleAmount = 0.025f;

        [Header("2. Floating & Gentle Sway (상하 유영 및 바람 흔들림)")]
        [Tooltip("유영 움직임 사용 여부")]
        public bool enableFloating = true;
        [Tooltip("상하 부유 이동 거리(px)")]
        public float floatAmplitude = 8f;
        [Tooltip("상하 부유 속도")]
        public float floatSpeed = 0.95f;
        [Tooltip("좌우 미세 흔들림 거리(px)")]
        public float swayAmplitude = 5f;
        [Tooltip("미세 회전 기울기 각도(Deg)")]
        public float tiltAngle = 0.35f;

        [Header("3. Mouse Parallax (마우스 시선 반응 깊이감)")]
        [Tooltip("마우스 위치에 반응하는 2.5D 패럴랙스 사용 여부")]
        public bool enableParallax = true;
        [Tooltip("마우스 최대 반응 이동 거리")]
        public Vector2 parallaxStrength = new Vector2(28f, 16f);
        [Tooltip("마우스 반응 부드러움 (Lerp 속도)")]
        public float parallaxSmoothness = 5f;

        [Header("4. Transition Settings (전환 연출)")]
        [Tooltip("새로운 캐릭터 선택 시 페이드인 전환 시간")]
        public float transitionDuration = 0.35f;

        private RectTransform rectTransform;
        private Vector2 baseAnchoredPosition = Vector2.zero;
        private Vector3 baseScale = new Vector3(1.08f, 1.08f, 1f);
        private Vector2 currentParallaxOffset = Vector2.zero;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (targetImage == null) targetImage = GetComponent<Image>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            baseAnchoredPosition = Vector2.zero;
            baseScale = new Vector3(1.08f, 1.08f, 1f);
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = baseAnchoredPosition;
                rectTransform.localScale = baseScale;
                rectTransform.localEulerAngles = Vector3.zero;
            }
        }

        private void OnEnable()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (targetImage == null) targetImage = GetComponent<Image>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            baseAnchoredPosition = Vector2.zero;
            baseScale = new Vector3(1.08f, 1.08f, 1f);
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = baseAnchoredPosition;
                rectTransform.localScale = baseScale;
            }

            if (targetImage != null && targetImage.sprite != null && canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        private void Start()
        {
            if (rectTransform != null)
            {
                baseScale = new Vector3(1.08f, 1.08f, 1f);
                rectTransform.localScale = baseScale;
            }
        }

        public void SetIllustration(Sprite newSprite)
        {
            if (targetImage == null) targetImage = GetComponent<Image>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (newSprite == null)
            {
                if (canvasGroup != null) canvasGroup.alpha = 0f;
                if (targetImage != null) targetImage.sprite = null;
                return;
            }

            targetImage.sprite = newSprite;
            targetImage.color = Color.white;

            if (Application.isPlaying && gameObject.activeInHierarchy)
            {
                StopAllCoroutines();
                StartCoroutine(TransitionInRoutine());
            }
            else
            {
                if (canvasGroup != null) canvasGroup.alpha = 1f;
            }
        }

        private IEnumerator TransitionInRoutine()
        {
            if (canvasGroup == null) yield break;

            canvasGroup.alpha = 0.2f;
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                // Smooth Step
                t = t * t * (3f - 2f * t);
                canvasGroup.alpha = Mathf.Lerp(0.2f, 1f, t);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        private void Update()
        {
            if (rectTransform == null) return;
            if (targetImage != null && targetImage.sprite == null) return;

            float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            float deltaTime = Application.isPlaying ? Time.deltaTime : 0.016f;

            // 1. Breathing Scale (호흡 팽창 수축)
            if (enableBreathing)
            {
                float bWave = Mathf.Sin(time * breatheSpeed) * 0.7f + Mathf.Sin(time * (breatheSpeed * 0.48f)) * 0.3f;
                float currentScaleMultiplier = 1f + bWave * breatheScaleAmount;
                rectTransform.localScale = baseScale * currentScaleMultiplier;
            }

            // 2. Floating & Sway (상하 유영 및 미세 흔들림)
            float floatY = 0f;
            float swayX = 0f;
            float rotZ = 0f;

            if (enableFloating)
            {
                floatY = Mathf.Sin(time * floatSpeed) * floatAmplitude;
                swayX = Mathf.Cos(time * (floatSpeed * 0.65f)) * swayAmplitude;
                rotZ = Mathf.Sin(time * (floatSpeed * 0.5f)) * tiltAngle;
                rectTransform.localEulerAngles = new Vector3(0, 0, rotZ);
            }

            // 3. Mouse Parallax (마우스 시선 반응)
            if (enableParallax && Application.isPlaying)
            {
                float mouseX = Mathf.Clamp01(Input.mousePosition.x / Mathf.Max(1f, (float)Screen.width)) - 0.5f;
                float mouseY = Mathf.Clamp01(Input.mousePosition.y / Mathf.Max(1f, (float)Screen.height)) - 0.5f;

                Vector2 targetParallax = new Vector2(-mouseX * parallaxStrength.x, -mouseY * parallaxStrength.y);
                currentParallaxOffset = Vector2.Lerp(currentParallaxOffset, targetParallax, deltaTime * parallaxSmoothness);
            }
            else
            {
                currentParallaxOffset = Vector2.Lerp(currentParallaxOffset, Vector2.zero, deltaTime * parallaxSmoothness);
            }

            // 종합 위치 적용
            rectTransform.anchoredPosition = baseAnchoredPosition + new Vector2(swayX, floatY) + currentParallaxOffset;
        }
    }
}
