using UnityEngine;

namespace TheLastArk.Data
{
    /// <summary>
    /// Synergy numbers. Arrays are indexed by active tier (index 0 = first tier). Only the highest tier applies.
    /// </summary>
    [CreateAssetMenu(fileName = "SynergyBalanceConfig", menuName = "TheLastArk/Synergy Balance")]
    public class SynergyBalanceConfig : ScriptableObject
    {
        public const string ResourcePath = "Synergies/SynergyBalanceConfig";
        private static SynergyBalanceConfig _cached;

        /// <summary>Default asset from Resources; logs an error when missing (no hidden fallback numbers).</summary>
        public static SynergyBalanceConfig Instance
        {
            get
            {
                if (_cached == null)
                {
                    _cached = Resources.Load<SynergyBalanceConfig>(ResourcePath);
                    if (_cached == null) Debug.LogError($"[Synergy] SynergyBalanceConfig 에셋이 없습니다: Resources/{ResourcePath}");
                }
                return _cached;
            }
        }

        [Header("아르키움 유니온 2/4/6/8")]
        public int[] archiumExtraDice = { 1, 1, 2, 3 };
        public int[] archiumTopBonus = { 0, 2, 2, 4 };

        [Header("라이언하트 3/5/7")]
        public int[] lionheartNorthDice = { 1, 2, 3 };
        public int[] lionheartShieldMultiplier = { 1, 1, 2 };
        public int northDieFaces = 6;
        [Tooltip("마수살해자 보유 시 북방 주사위 면 수")]
        public int beastSlayerNorthFaces = 8;

        [Header("성도 엘리시움 2/4/6/8")]
        public int[] elysiumCelestialDice = { 1, 2, 2, 3 };
        public int[] elysiumOverhealShieldPercent = { 0, 0, 50, 100 };

        [Header("푸른 마탑 2/4/6")]
        public float[] blueTowerSpellPowerPercent = { 10, 20, 30 };
        public int[] blueTowerDistinctSkills = { 3, 3, 2 };
        [Tooltip("별의 그리모어 보유 시 필요 종류 수")]
        public int[] grimoireDistinctSkills = { 2, 2, 1 };
        public int[] blueTowerMental = { 2, 3, 4 };
        public int[] blueTowerPerTurn = { 1, 1, 2 };

        [Header("엘븐우드 대삼림 2/4/6 (단계 n = 앞에서 n개)")]
        public int[] elvenwoodFaces = { 12, 18, 24 };

        [Header("미라주 2/4/6/8")]
        public float[] mirageGoldPercent = { 0, 25, 50, 100 };
        public int mirageGoldDiceCost = 30;
        public float[] mirageGuardianShieldPercent = { 4, 6, 8, 12 };
        public float[] mirageWarriorAttackPercent = { 8, 12, 16, 24 };
        public float[] mirageAssassinCrit = { 5, 7.5f, 10, 15 };
        public float[] mirageRangerCritDamage = { 10, 15, 20, 30 };
        public float[] mirageMageSpellPercent = { 8, 12, 16, 24 };
        public int[] mirageSupportMental = { 1, 1, 2, 3 };

        [Header("속삭이는 교단 3/5/7")]
        public int[] whisperDice = { 1, 2, 2 };
        public int[] whisperFear = { 1, 2, 3 };
        public float[] whisperLowMentalBonusPercent = { 25, 40, 50 };
        public int whisperFearRounds = 3;

        [Header("청운 3/5/7")]
        public int[] cheongwoonCloudCount = { 2, 2, 4 };
        public int[] cheongwoonCloudValue = { 0, 1, 1 };
        public int[] cheongwoonFrailty = { 1, 2, 3 };

        [Header("침묵의 아르카디아 1/2/3")]
        public int[] arcadiaCarry = { 1, 2, 2 };
        [Tooltip("3단계 최대 눈금 효과가 시작되는 플레이어 턴 (첫 턴 = 1)")]
        public int arcadiaMaxValueFromTurn = 6;

        [Header("수호자 2/4")]
        public float[] guardianTeamReductionPercent = { 10, 15 };
        public float[] guardianProtectReductionPercent = { 10, 20 };

        [Header("전사 2/4")]
        public float[] warriorFirstSkillPercent = { 35, 50 };
        public float[] warriorSecondSkillPercent = { 0, 25 };

        [Header("암살자 2/4")]
        public float[] assassinStatPercent = { 10, 20 };
        public int[] assassinDiceBonus = { 1, 2 };

        [Header("사수 2/4")]
        public float[] rangerCrit = { 10, 20 };
        public float[] rangerCritDamage = { 15, 25 };
        [Tooltip("기본 치명 피해 (%)")]
        public float baseCritDamagePercent = 180;

        [Header("마법사 2/4")]
        public float[] mageDebuffMultiplier = { 1.5f, 2f };

        [Header("서포터 1/2/3")]
        public float[] supportReceivePercent = { 15, 25, 35 };
        public int[] supportMental = { 1, 2, 3 };

        public static int At(int[] values, int tier) => values == null || values.Length == 0 || tier <= 0 ? 0 : values[Mathf.Min(tier, values.Length) - 1];
        public static float At(float[] values, int tier) => values == null || values.Length == 0 || tier <= 0 ? 0f : values[Mathf.Min(tier, values.Length) - 1];
    }
}
