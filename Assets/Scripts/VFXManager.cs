using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    private static VFXManager _instance;
    public static VFXManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<VFXManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("VFXManager");
                    _instance = go.AddComponent<VFXManager>();
                }
            }
            return _instance;
        }
    }

    [System.Serializable]
    public class VFXData
    {
        public string name;
        public GameObject prefab;
    }

    [Header("VFX Settings")]
    [Tooltip("Register your VFX prefabs here with a unique name.")]
    public List<VFXData> vfxList = new List<VFXData>();

    [Header("2D UI Scale & Rendering")]
    [Tooltip("2D 화면(Orthographic Size 400 기준)에 맞추기 위한 기본 스케일 배율")]
    [SerializeField] private float defaultVfxScale = 140f;

    [Tooltip("UI 위에 이펙트가 가려지지 않도록 강제 적용할 Sorting Order")]
    [SerializeField] private int defaultSortingOrder = 5000;

    private Dictionary<string, GameObject> _vfxCache = new Dictionary<string, GameObject>();

    // Resources 경로 프리픽스
    private const string ResourceBasePath = "TestEffefct/FreeQuickEffectsVol1/Prefabs/";

    // 기본 추천 프리팹 명칭 상수들
    public const string VfxImpact = "vfx_Impact_01";
    public const string VfxSparks = "vfx_Sparks_01";
    public const string VfxShockwave = "vfx_Shockwave_01";
    public const string VfxExplosion = "vfx_Explosion_01";
    public const string VfxHeal = "vfx_Heal_01";
    public const string VfxHeal2 = "vfx_Heal_02";
    public const string VfxShield = "vfx_Shield_01";
    public const string VfxFlames = "vfx_Flames_01";
    public const string VfxFlamethrower = "vfx_Flamethrower_01";
    public const string VfxLightning = "vfx_Lightning_01";
    public const string VfxElectricity = "vfx_Electricity_01";
    public const string VfxSmoke = "vfx_Smoke_01";
    public const string VfxMeteorRain = "vfx_MeteorRain_01";
    public const string VfxProjectile = "vfx_Projectile_01";
    public const string VfxMuzzleFlash = "vfx_MuzzleFlash_01";

    private Shader _urpParticleShader;
    private Shader _spritesDefaultShader;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // URP 셰이더 캐싱
        _urpParticleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (_urpParticleShader == null) _urpParticleShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (_urpParticleShader == null) _urpParticleShader = Shader.Find("Universal Render Pipeline/Unlit");
        _spritesDefaultShader = Shader.Find("Sprites/Default");

        // 인스펙터에 사전 등록된 프리팹 캐싱
        foreach (var vfx in vfxList)
        {
            if (vfx.prefab != null && !string.IsNullOrEmpty(vfx.name))
            {
                if (!_vfxCache.ContainsKey(vfx.name))
                {
                    _vfxCache.Add(vfx.name, vfx.prefab);
                }
            }
        }
    }

    /// <summary>
    /// Resources 또는 캐시에서 프리팹을 가져옵니다.
    /// </summary>
    public GameObject GetVFXPrefab(string vfxName)
    {
        if (string.IsNullOrEmpty(vfxName)) return null;

        if (_vfxCache.TryGetValue(vfxName, out GameObject cachedPrefab) && cachedPrefab != null)
        {
            return cachedPrefab;
        }

        // Resources 로드 시도
        string fullPath = ResourceBasePath + vfxName;
        GameObject loaded = Resources.Load<GameObject>(fullPath);
        if (loaded == null)
        {
            loaded = Resources.Load<GameObject>(vfxName);
        }

        if (loaded != null)
        {
            _vfxCache[vfxName] = loaded;
            return loaded;
        }

        Debug.LogWarning($"[VFXManager] VFX 프리팹을 Resources에서 찾을 수 없습니다: {fullPath}");
        return null;
    }

    /// <summary>
    /// 대상 위치에 지정된 VFX를 스폰하고 2D 화면에 맞게 스케일링/정렬/셰이더를 보정합니다.
    /// </summary>
    public GameObject SpawnVFX(string vfxName, Vector3 position, Quaternion rotation, float? customScale = null, float defaultLifetime = 2.0f)
    {
        GameObject prefab = GetVFXPrefab(vfxName);
        if (prefab == null)
        {
            Debug.LogWarning($"[VFXManager] '{vfxName}' 프리팹을 로드하지 못해 VFX 재생에 실패했습니다.");
            return null;
        }

        // 카메라(Z=-100)와 UI/캐릭터(Z=0) 사이에 위치하도록 Z를 -15f로 배치
        Vector3 spawnPos = new Vector3(position.x, position.y, -15f);
        GameObject vfxInstance = Instantiate(prefab, spawnPos, rotation);

        // 2D 씬 픽셀 단위(Orthographic 400)에 맞게 140배 스케일링
        float scaleVal = customScale ?? defaultVfxScale;
        vfxInstance.transform.localScale = Vector3.one * scaleVal;

        // URP 셰이더 및 투명 알파 블렌딩 완벽 적용 (노란 네모 현상 제거)
        FixShadersForURP(vfxInstance);

        // UI Canvas 뒤에 묻히지 않도록 모든 ParticleSystemRenderer의 Sorting Order를 5000으로 강제 설정
        var renderers = vfxInstance.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r != null)
            {
                r.sortingOrder = defaultSortingOrder;
                r.sortingLayerName = "Default";
            }
        }

        // 파티클 시스템 재생 트리거 및 지속 시간 계산
        float duration = defaultLifetime;
        var allParticles = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in allParticles)
        {
            // 너무 순식간에 지나가지 않도록 시뮬레이션 속도 안정화
            var main = ps.main;
            main.simulationSpeed = Mathf.Clamp(main.simulationSpeed, 0.8f, 1.2f);

            ps.Play(true);
            float pDur = main.duration + main.startLifetime.constantMax;
            if (pDur > duration) duration = pDur;
        }

        Debug.Log($"<color=#00FFCC>[VFXManager] 스킬 이펙트 재생: {vfxName}</color> (위치: {spawnPos}, 스케일: {scaleVal}배, SortingOrder: {defaultSortingOrder})");

        Destroy(vfxInstance, Mathf.Clamp(duration, 1.5f, 5.0f));
        return vfxInstance;
    }

    public GameObject SpawnVFX(string vfxName, Vector3 position, float? customScale = null, float defaultLifetime = 2.0f)
    {
        return SpawnVFX(vfxName, position, Quaternion.identity, customScale, defaultLifetime);
    }

    /// <summary>
    /// URP 렌더 파이프라인에서 파티클이 노란 네모(불투명 쿼드)로 나오는 문제를 방지하고 완벽한 알파 블렌딩을 적용합니다.
    /// </summary>
    private void FixShadersForURP(GameObject vfxInstance)
    {
        Shader targetShader = _urpParticleShader ?? _spritesDefaultShader;

        var renderers = vfxInstance.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r == null) continue;
            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;

                // 1. 텍스처 추출 (_MainTex 또는 _BaseMap)
                Texture tex = null;
                if (mats[i].HasProperty("_MainTex")) tex = mats[i].GetTexture("_MainTex");
                if (tex == null && mats[i].HasProperty("_BaseMap")) tex = mats[i].GetTexture("_BaseMap");
                if (tex == null) tex = mats[i].mainTexture;

                // 2. 색상 추출
                Color col = Color.white;
                if (mats[i].HasProperty("_Color")) col = mats[i].GetColor("_Color");
                else if (mats[i].HasProperty("_BaseColor")) col = mats[i].GetColor("_BaseColor");

                // 3. URP 파티클 셰이더 설정
                if (targetShader != null)
                {
                    mats[i].shader = targetShader;
                }

                // 4. 텍스처 및 색상 바인딩
                if (tex != null)
                {
                    if (mats[i].HasProperty("_BaseMap")) mats[i].SetTexture("_BaseMap", tex);
                    if (mats[i].HasProperty("_MainTex")) mats[i].SetTexture("_MainTex", tex);
                }
                if (mats[i].HasProperty("_BaseColor")) mats[i].SetColor("_BaseColor", col);
                if (mats[i].HasProperty("_Color")) mats[i].SetColor("_Color", col);

                // 5. 투명 알파 블렌딩 / Additive 설정 (노란 네모 쿼드 제거의 핵심)
                if (mats[i].HasProperty("_Surface")) mats[i].SetFloat("_Surface", 1f); // 1 = Transparent
                if (mats[i].HasProperty("_Blend")) mats[i].SetFloat("_Blend", 1f);     // 1 = Premultiply / Additive
                if (mats[i].HasProperty("_SrcBlend")) mats[i].SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (mats[i].HasProperty("_DstBlend")) mats[i].SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                if (mats[i].HasProperty("_ZWrite")) mats[i].SetFloat("_ZWrite", 0f);
                if (mats[i].HasProperty("_Cull")) mats[i].SetFloat("_Cull", 0f);
                if (mats[i].HasProperty("_AlphaClip")) mats[i].SetFloat("_AlphaClip", 0f);

                mats[i].EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mats[i].EnableKeyword("_ALPHABLEND_ON");
                mats[i].EnableKeyword("_ALPHAPREMULTIPLY_ON");
                mats[i].DisableKeyword("_ALPHATEST_ON");

                mats[i].renderQueue = 3000;
                mats[i].SetOverrideTag("RenderType", "Transparent");
            }
            r.materials = mats;
        }
    }

    /// <summary>
    /// 기본 VFX 재생 (스킬명 없을 때)
    /// </summary>
    public void PlayDefaultEffect(EffectType effectType, DamageType damageType, BattleCharacter target)
    {
        if (target == null) return;
        PlaySmartSkillEffect("", effectType, damageType, target);
    }

    public void PlayDefaultEffect(EffectType effectType, DamageType damageType, Vector3 position)
    {
        string vfxName = GetDefaultVFXName(effectType, damageType);
        if (!string.IsNullOrEmpty(vfxName))
        {
            SpawnVFX(vfxName, position);
        }
    }

    /// <summary>
    /// 스킬 이름 및 효과/대미지 타입에 따라 가장 어울리는 최적의 VFX를 대상 위치에 자동 재생합니다.
    /// </summary>
    public void PlaySmartSkillEffect(string skillName, EffectType effectType, DamageType damageType, BattleCharacter target)
    {
        if (target == null) return;
        string vfxName = GetSmartVFXName(skillName, effectType, damageType);
        if (!string.IsNullOrEmpty(vfxName))
        {
            SpawnVFX(vfxName, target.transform.position);
        }
    }

    /// <summary>
    /// 스킬 이름 키워드 및 효과 유형을 분석하여 가장 타격감 있고 어울리는 VFX 프리팹을 결정합니다.
    /// </summary>
    public string GetSmartVFXName(string skillName, EffectType effectType, DamageType damageType)
    {
        if (!string.IsNullOrEmpty(skillName))
        {
            string lowerName = skillName.ToLower();

            // 1. 방패 / 치기 / 강타 / 돌진 -> 충격파 (vfx_Shockwave_01)
            if (lowerName.Contains("방패") || lowerName.Contains("치기") || lowerName.Contains("강타") || lowerName.Contains("충격") || lowerName.Contains("smash") || lowerName.Contains("bash"))
            {
                return VfxShockwave;
            }

            // 2. 가르기 / 베기 / 참격 / 검격 / 연속 -> 호쾌한 타격 임팩트 (vfx_Impact_01)
            if (lowerName.Contains("가르기") || lowerName.Contains("베기") || lowerName.Contains("참격") || lowerName.Contains("slash") || lowerName.Contains("cleave"))
            {
                return VfxImpact;
            }

            // 3. 사격 / 저격 / 화살 / 투척 -> 머즐플래시 / 투사체
            if (lowerName.Contains("사격") || lowerName.Contains("저격") || lowerName.Contains("화살") || lowerName.Contains("shot") || lowerName.Contains("arrow"))
            {
                return VfxMuzzleFlash;
            }

            // 4. 폭발 / 화염 / 메테오 -> 폭발
            if (lowerName.Contains("폭발") || lowerName.Contains("메테오") || lowerName.Contains("운석") || lowerName.Contains("explosion") || lowerName.Contains("meteor"))
            {
                return VfxExplosion;
            }

            // 5. 회복 / 힐 / 은총 / 정화 -> 치유 빛무리
            if (lowerName.Contains("회복") || lowerName.Contains("치유") || lowerName.Contains("힐") || lowerName.Contains("재정비") || lowerName.Contains("heal"))
            {
                return VfxHeal;
            }

            // 6. 보호막 / 철벽 / 방어 -> 배리어 구체
            if (lowerName.Contains("보호") || lowerName.Contains("실드") || lowerName.Contains("철벽") || lowerName.Contains("barrier") || lowerName.Contains("shield"))
            {
                return VfxShield;
            }
        }

        // 스킬 이름 매칭이 없을 때 효과 타입 및 대미지 타입 기반 기본 선택
        return GetDefaultVFXName(effectType, damageType);
    }

    /// <summary>
    /// 상황에 맞는 기본 VFX 이름 결정
    /// </summary>
    public string GetDefaultVFXName(EffectType effectType, DamageType damageType)
    {
        switch (effectType)
        {
            case EffectType.Damage:
                return damageType switch
                {
                    DamageType.Magical => VfxLightning,     // 마법 공격 -> 번개/마법 이펙트
                    DamageType.True => VfxShockwave,       // 관통/고정 피해 -> 충격파 이펙트
                    _ => VfxImpact                         // 물리 공격 -> 검격/타격 충격 이펙트
                };

            case EffectType.Heal:
            case EffectType.MentalHeal:
                return VfxHeal;                            // 치유 -> 힐링 빛무리 이펙트

            case EffectType.MentalDamage:
                return VfxShockwave;                       // 정신 피해 -> 충격파 이펙트

            case EffectType.Shield:
                return VfxShield;                          // 보호막 -> 배리어 구체 이펙트

            case EffectType.Strength:
            case EffectType.Focus:
                return VfxSparks;                          // 버프/힘 -> 스파크 광휘 이펙트

            case EffectType.Burn:
                return VfxFlames;                          // 화상 -> 불꽃 이펙트

            case EffectType.Bleed:
                return VfxImpact;                          // 출혈 -> 강한 타격 이펙트

            case EffectType.Poison:
                return VfxSmoke;                           // 중독 -> 독연기 이펙트

            case EffectType.Stun:
                return VfxShockwave;                       // 기절 -> 충격파 이펙트

            case EffectType.Taunt:
            case EffectType.Counter:
                return VfxShockwave;                       // 도발/반격 -> 충격파 이펙트

            case EffectType.Resurrection:
                return VfxHeal2;                           // 부활 -> 강력한 성스러운 빛 이펙트

            default:
                return VfxImpact;                          // 기본 기본값: 충격 타격
        }
    }
}
