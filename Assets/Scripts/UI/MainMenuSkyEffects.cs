using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TheLastArk.UI
{
    /// <summary>
    /// 개별 별빛의 불규칙한(Irregular) 서정적 점멸 및 샤인 애니메이션을 전담하는 컴포넌트.
    /// </summary>
    public class IndividualStarTwinkle : MonoBehaviour
    {
        private Material mat;
        private Image img;
        private float baseAlpha;
        private float phaseOffset;
        private float speed;
        private float shineSpeed;
        private float shinePos;

        public void Initialize(Material instanceMat, Image targetImg, float baseAlphaVal)
        {
            this.mat = instanceMat;
            this.img = targetImg;
            this.baseAlpha = baseAlphaVal;

            // 개별 별마다 완전히 다른 무작위 위상 및 극도로 완만한 속도 부여
            this.phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            this.speed = Random.Range(0.25f, 0.65f); // 이전보다 훨씬 천천히 서서히 지는 속도
            this.shineSpeed = Random.Range(0.003f, 0.015f); // 림 샤인 스위핑 극저속화
            this.shinePos = Random.Range(0f, 1f);
        }

        private void Update()
        {
            float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            float deltaTime = Application.isPlaying ? Time.deltaTime : 0.016f;

            // 2개의 비조화 사인파(Non-harmonic Sines)를 합성하여 완벽히 불규칙적인(Irregular) 점멸 곡선 생성
            float sine1 = Mathf.Sin(time * speed + phaseOffset);
            float sine2 = Mathf.Sin(time * (speed * 0.43f) + phaseOffset * 1.73f);
            float blended = (sine1 + sine2 * 0.5f) / 1.5f;

            float currentAlpha = baseAlpha * (0.55f + 0.45f * blended);

            if (img != null)
            {
                Color c = img.color;
                img.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(currentAlpha));
            }

            if (mat != null)
            {
                shinePos += deltaTime * shineSpeed;
                if (shinePos > 1f) shinePos -= 1f;
                mat.SetFloat("_ShineLocation", shinePos);
            }
        }
    }

    /// <summary>
    /// 메인 메뉴 배경의 광원 위치(탑 전등, 기차 헤드라이트, 푸른 영혼불, 야경 별빛 등) 및 
    /// 하늘 영역 한정 별똥별(Shooting Star) 연출을 담당하는 스크립트.
    /// </summary>
    [ExecuteAlways]
    public class MainMenuSkyEffects : MonoBehaviour
    {
        [Header("1. 광원 앵커 설정 (유니티 씬 뷰에서 직관적 배치 가능)")]
        [Tooltip("자식 빈 GameObject들을 유니티 씬 뷰에서 이동 도구(W)로 드래그하여 광원 위치를 설정하세요.")]
        public Transform lightPointsContainer;
        public Vector2 starSizeRange = new Vector2(16f, 32f);

        [Header("2. 유성/별똥별 하늘 한계선 (Horizon Mask)")]
        [Range(0.3f, 0.95f)]
        [Tooltip("별똥별이 소멸하는 지평선/땅 높이 (0: 화면 맨아래, 1: 화면 맨위). 씬 뷰의 하늘색 선 아래로 내려가면 서서히 사라집니다.")]
        public float skyHorizonY = 0.55f;
        [Range(0.5f, 0.98f)]
        public float skyMinSpawnY = 0.75f;
        [Range(0.6f, 1.00f)]
        public float skyMaxSpawnY = 0.96f;
        public float minSpawnInterval = 3.5f;
        public float maxSpawnInterval = 7.0f;
        public float shootingStarSpeed = 1200f;

        [Header("3. 반짝임 속도 & 셰이더 조절 (All In 1 Sprite Shader)")]
        [Range(0.001f, 0.2f)]
        [Tooltip("림 샤인(Shine) 스위핑 반짝임 속도 (작을수록 부드럽고 천천히 반짝임)")]
        public float shineSpeed = 0.015f;
        [Range(0.05f, 2.0f)]
        public float flickerFrequency = 0.35f;
        [Range(0.0f, 1.0f)]
        public float flickerDepth = 0.35f;

        private Transform skyContainer;
        private Sprite starSprite;
        private Sprite shootingStarSprite;
        private Material baseStarMaterial;
        private Material shootingStarMaterial;

        private float nextSpawnTime;
        private List<GameObject> activeShootingStars = new List<GameObject>();
        private List<float> shootingStarLifespan = new List<float>();
        private List<Vector3> shootingStarDir = new List<Vector3>();

        public static void SafeDestroy(GameObject obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        public void Initialize(Transform parent)
        {
            if (skyContainer == null)
            {
                Transform existingSky = parent.Find("SkyEffectsContainer");
                if (existingSky != null)
                {
                    skyContainer = existingSky;
                }
                else
                {
                    GameObject skyObj = new GameObject("SkyEffectsContainer");
                    skyObj.transform.SetParent(parent, false);
                    RectTransform sRect = skyObj.AddComponent<RectTransform>();
                    sRect.anchorMin = Vector2.zero;
                    sRect.anchorMax = Vector2.one;
                    sRect.sizeDelta = Vector2.zero;
                    skyContainer = skyObj.transform;
                }
            }

            Transform existingContainer = skyContainer.Find("LightPointsContainer");
            if (existingContainer != null)
            {
                lightPointsContainer = existingContainer;
            }
            else if (lightPointsContainer == null)
            {
                GameObject lpContainerObj = new GameObject("LightPointsContainer");
                lpContainerObj.transform.SetParent(skyContainer, false);
                RectTransform lpRect = lpContainerObj.AddComponent<RectTransform>();
                lpRect.anchorMin = Vector2.zero;
                lpRect.anchorMax = Vector2.one;
                lpRect.sizeDelta = Vector2.zero;
                lightPointsContainer = lpContainerObj.transform;

                CreateDefaultLightAnchors(lightPointsContainer);
            }

            starSprite = CreateStarSprite();
            shootingStarSprite = CreateShootingStarSprite();

            baseStarMaterial = CreateAllIn1Material(isShootingStar: false);
            shootingStarMaterial = CreateAllIn1Material(isShootingStar: true);

            SpawnStarfieldAtAnchors();
            ScheduleNextShootingStar();
        }

        private void Start()
        {
            if (skyContainer == null && transform.parent != null)
            {
                Initialize(transform);
            }
            else if (skyContainer == null)
            {
                Initialize(transform);
            }
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                UpdateShootingStars();

                if (Time.time >= nextSpawnTime)
                {
                    SpawnShootingStar();
                    ScheduleNextShootingStar();
                }
            }
        }

        [ContextMenu("Rebuild Light Anchors & Stars")]
        public void RebuildLightAnchorsAndStars()
        {
            if (lightPointsContainer != null)
            {
                for (int i = lightPointsContainer.childCount - 1; i >= 0; i--)
                {
                    SafeDestroy(lightPointsContainer.GetChild(i).gameObject);
                }
                CreateDefaultLightAnchors(lightPointsContainer);
                SpawnStarfieldAtAnchors();
            }
            else if (transform != null)
            {
                Initialize(transform);
            }
        }

        [ContextMenu("Spawn Test Shooting Star")]
        public void SpawnTestShootingStar()
        {
            if (skyContainer == null) skyContainer = transform;
            if (shootingStarSprite == null) shootingStarSprite = CreateShootingStarSprite();
            if (shootingStarMaterial == null) shootingStarMaterial = CreateAllIn1Material(isShootingStar: true);
            SpawnShootingStar();
        }

        private void CreateDefaultLightAnchors(Transform container)
        {
            AddLightAnchor(container, "01_TowerTop_Spire", new Vector2(0.245f, 0.770f));      // 좌측 시계탑 붉은 노을 빛
            AddLightAnchor(container, "02_TrainHeadlight_Eye", new Vector2(0.535f, 0.775f)); // 중앙 기차 헤드라이트 푸른 눈
            AddLightAnchor(container, "03_Magical_SoulFlame", new Vector2(0.380f, 0.385f));  // 중앙 인물 손 끝 푸른 영혼불
            AddLightAnchor(container, "04_Train_Engine_Spark", new Vector2(0.720f, 0.710f)); // 우측 대형 열차 차체 톱니 광원
            AddLightAnchor(container, "05_City_FarLight_1", new Vector2(0.120f, 0.580f));   // 좌측 원경 도심 불빛
            AddLightAnchor(container, "06_SkyStar_HighLeft", new Vector2(0.150f, 0.920f));  // 하늘 좌상단 별빛
            AddLightAnchor(container, "07_SkyStar_HighCenter", new Vector2(0.420f, 0.880f));// 하늘 중앙 별빛
            AddLightAnchor(container, "08_SkyStar_HighRight", new Vector2(0.850f, 0.940f)); // 하늘 우상단 별빛
        }

        private void AddLightAnchor(Transform parent, string name, Vector2 normPos)
        {
            GameObject anchor = new GameObject(name);
            anchor.transform.SetParent(parent, false);
            RectTransform rect = anchor.AddComponent<RectTransform>();
            rect.anchorMin = normPos;
            rect.anchorMax = normPos;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(24, 24);
        }

        private void SpawnStarfieldAtAnchors()
        {
            if (lightPointsContainer == null) return;

            int index = 0;
            foreach (Transform child in lightPointsContainer)
            {
                if (child == null) continue;

                if (child.childCount > 0)
                {
                    index++;
                    continue;
                }

                GameObject starObj = new GameObject($"StarParticle_{index++}");
                starObj.transform.SetParent(child, false);

                RectTransform rect = starObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;

                Image img = starObj.AddComponent<Image>();
                img.sprite = starSprite;
                float startAlpha = Random.Range(0.65f, 1.0f);
                img.color = new Color(1f, 1f, 1f, startAlpha);
                img.raycastTarget = false;

                // 개별 독립 인스턴스 마테리얼 생성 (동기화 점멸 완전 방지)
                if (baseStarMaterial != null)
                {
                    Material instanceMat = new Material(baseStarMaterial);
                    instanceMat.SetFloat("_FlickerFreq", Random.Range(0.1f, 0.35f)); // 불규칙한 극저속 플리커
                    instanceMat.SetFloat("_FlickerPercent", flickerDepth);
                    instanceMat.SetFloat("_ShineLocation", Random.Range(0f, 1f));
                    img.material = instanceMat;

                    // 개별 서정적 점멸 컨트롤러 부착
                    IndividualStarTwinkle twinkle = starObj.AddComponent<IndividualStarTwinkle>();
                    twinkle.Initialize(instanceMat, img, startAlpha);
                }
            }
        }

        private void ScheduleNextShootingStar()
        {
            float currentTime = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            nextSpawnTime = currentTime + Random.Range(minSpawnInterval, maxSpawnInterval);
        }

        private void SpawnShootingStar()
        {
            if (skyContainer == null) skyContainer = transform;

            GameObject sObj = new GameObject("ShootingStar");
            sObj.transform.SetParent(skyContainer, false);

            RectTransform rect = sObj.AddComponent<RectTransform>();

            float startX = Random.Range(0.05f, 0.70f);
            float startY = Random.Range(skyMinSpawnY, skyMaxSpawnY);
            rect.anchorMin = new Vector2(startX, startY);
            rect.anchorMax = new Vector2(startX, startY);

            float width = Random.Range(180f, 260f);
            float height = Random.Range(20f, 32f);
            rect.sizeDelta = new Vector2(width, height);

            float angle = Random.Range(-25f, -40f);
            rect.localRotation = Quaternion.Euler(0, 0, angle);

            Image img = sObj.AddComponent<Image>();
            if (shootingStarSprite == null) shootingStarSprite = CreateShootingStarSprite();
            img.sprite = shootingStarSprite;
            img.color = new Color(1f, 0.95f, 0.8f, 1f);
            if (shootingStarMaterial == null) shootingStarMaterial = CreateAllIn1Material(isShootingStar: true);
            if (shootingStarMaterial != null) img.material = shootingStarMaterial;
            img.raycastTarget = false;

            activeShootingStars.Add(sObj);
            shootingStarLifespan.Add(Random.Range(0.8f, 1.3f));

            float rad = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0).normalized;
            shootingStarDir.Add(dir);
        }

        private void UpdateShootingStars()
        {
            float bgHeight = Screen.height > 0 ? Screen.height : 1080f;
            float deltaTime = Application.isPlaying ? Time.deltaTime : 0.016f;

            for (int i = activeShootingStars.Count - 1; i >= 0; i--)
            {
                GameObject sObj = activeShootingStars[i];
                if (sObj == null)
                {
                    RemoveShootingStarAt(i);
                    continue;
                }

                shootingStarLifespan[i] -= deltaTime;
                float remainingRatio = shootingStarLifespan[i] / 1.0f;

                RectTransform rect = sObj.GetComponent<RectTransform>();
                rect.anchoredPosition += (Vector2)(shootingStarDir[i] * shootingStarSpeed * deltaTime);

                float currentNormalizedY = rect.anchorMin.y + (rect.anchoredPosition.y / bgHeight);
                float distanceToHorizon = currentNormalizedY - skyHorizonY;
                float horizonFade = Mathf.Clamp01(distanceToHorizon / 0.12f);

                float lifeAlpha = Mathf.Clamp01(remainingRatio);
                float finalAlpha = lifeAlpha * horizonFade;

                Image img = sObj.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(1f, 0.95f, 0.8f, finalAlpha);
                }

                if (finalAlpha <= 0.02f || currentNormalizedY <= skyHorizonY - 0.03f)
                {
                    SafeDestroy(sObj);
                    RemoveShootingStarAt(i);
                }
            }
        }

        private void RemoveShootingStarAt(int index)
        {
            activeShootingStars.RemoveAt(index);
            shootingStarLifespan.RemoveAt(index);
            shootingStarDir.RemoveAt(index);
        }

        private Material CreateAllIn1Material(bool isShootingStar)
        {
            Shader shader = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShaderUiMask");
            if (shader == null) shader = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShader");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            Material mat = new Material(shader);

            if (isShootingStar)
            {
                mat.EnableKeyword("GLOW_ON");
                mat.EnableKeyword("SHINE_ON");

                mat.SetColor("_GlowColor", new Color(1.0f, 0.92f, 0.65f, 1f));
                mat.SetFloat("_Glow", 3.5f);
                mat.SetColor("_ShineColor", new Color(1.0f, 1.0f, 0.9f, 1f));
                mat.SetFloat("_ShineGlow", 2.0f);
                mat.SetFloat("_ShineWidth", 0.3f);
            }
            else
            {
                mat.EnableKeyword("GLOW_ON");
                mat.EnableKeyword("FLICKER_ON");
                mat.EnableKeyword("SHINE_ON");

                mat.SetColor("_GlowColor", new Color(0.85f, 0.92f, 1.0f, 1f));
                mat.SetFloat("_Glow", 2.2f);

                mat.SetFloat("_FlickerFreq", flickerFrequency);
                mat.SetFloat("_FlickerPercent", flickerDepth);
                mat.SetFloat("_FlickerAlpha", 0.25f);

                mat.SetColor("_ShineColor", new Color(1.0f, 0.95f, 0.75f, 1f));
                mat.SetFloat("_ShineWidth", 0.25f);
                mat.SetFloat("_ShineGlow", 1.8f);
            }

            return mat;
        }

        private Sprite CreateStarSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float center = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float circular = Mathf.Clamp01(1f - dist * 1.5f);
                    circular = Mathf.Pow(circular, 2.5f);

                    float rayX = Mathf.Clamp01(1f - Mathf.Abs(dx) * 4f) * Mathf.Clamp01(1f - Mathf.Abs(dy) * 0.8f);
                    float rayY = Mathf.Clamp01(1f - Mathf.Abs(dy) * 4f) * Mathf.Clamp01(1f - Mathf.Abs(dx) * 0.8f);
                    float rays = Mathf.Max(rayX, rayY) * 0.8f;

                    float alpha = Mathf.Clamp01(circular + rays);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateShootingStarSprite()
        {
            int width = 160;
            int height = 32;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float centerY = (height - 1) / 2f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normX = (float)x / (width - 1);
                    float normY = (y - centerY) / centerY;

                    float widthFade = Mathf.Pow(normX, 1.8f);
                    float thickness = Mathf.Clamp01(1f - Mathf.Abs(normY));
                    thickness = Mathf.Pow(thickness, 2.0f);

                    float alpha = widthFade * thickness;
                    tex.SetPixel(x, y, new Color(1f, 0.95f, 0.8f, alpha));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(1.0f, 0.5f));
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.85f);
            float screenH = Screen.height > 0 ? Screen.height : 1080f;
            float worldY = (skyHorizonY - 0.5f) * screenH;

            Vector3 pLeft = transform.TransformPoint(new Vector3(-960, worldY, 0));
            Vector3 pRight = transform.TransformPoint(new Vector3(960, worldY, 0));
            Gizmos.DrawLine(pLeft, pRight);

            UnityEditor.Handles.Label(pLeft + Vector3.up * 15f, $"[별똥별 소멸 지평선 SkyHorizonY: {skyHorizonY:F2}]");

            if (lightPointsContainer != null)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.25f, 0.9f);
                foreach (Transform child in lightPointsContainer)
                {
                    if (child == null) continue;
                    Gizmos.DrawWireSphere(child.position, 18f);
                    UnityEditor.Handles.Label(child.position + Vector3.up * 20f, $"✦ {child.name}");
                }
            }
        }
#endif
    }
}
