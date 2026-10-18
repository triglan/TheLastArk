namespace TheLastArk.Battle
{
    /// <summary>Shared panic list (players and enemies). One panic = one penalty.</summary>
    public enum PanicType { None, Hallucination, Depression, Compulsion, Lethargy, Seizure, Agitation }

    [System.Serializable]
    public sealed class PanicState
    {
        public const int TypeCount = 6;
        public const int SeizureSanityLoss = 5;
        public const float AgitationDamageMultiplier = 1.5f;
        public const float DepressionEnemyDamageDecrease = 0.5f;

        public PanicType Type { get; private set; }
        /// <summary>강박: the first skill actually fired after the panic began (players).</summary>
        public SkillInfo LockedSkill { get; set; }
        /// <summary>강박: the first pattern used after the panic began (enemies), -1 until then.</summary>
        public int LockedPatternIndex { get; set; } = -1;
        public bool IsActive => Type != PanicType.None;

        public void Begin(PanicType type)
        {
            Type = type;
            LockedSkill = null;
            LockedPatternIndex = -1;
        }

        public void Clear() => Begin(PanicType.None);

        /// <summary>Uniform pick among the six panics (1/6 each).</summary>
        public static PanicType Roll() => (PanicType)UnityEngine.Random.Range(1, TypeCount + 1);

        public static string Name(PanicType type) => type switch
        {
            PanicType.Hallucination => "환각",
            PanicType.Depression => "우울",
            PanicType.Compulsion => "강박",
            PanicType.Lethargy => "무기력",
            PanicType.Seizure => "발작",
            PanicType.Agitation => "동요",
            _ => ""
        };

        public static string Description(PanicType type) => type switch
        {
            PanicType.Hallucination => "단일 대상 공격의 대상이 생존한 적·아군 전체(자신 포함) 중 무작위가 됩니다.",
            PanicType.Depression => "사용하는 주사위의 효과 눈금이 절반(버림)이 됩니다. 적은 주는 피해 −50%.",
            PanicType.Compulsion => "패닉 후 처음 실제로 쓴 스킬(적은 패턴)만 사용할 수 있습니다.",
            PanicType.Lethargy => "공격 스킬(패턴)을 사용할 수 없습니다. 방어·지원만 가능합니다.",
            PanicType.Seizure => "자기 진영 턴 종료 시 다른 생존 동료 정신력 −5.",
            PanicType.Agitation => "받는 체력 피해 +50%.",
            _ => ""
        };
    }
}
