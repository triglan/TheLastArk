using UnityEngine;

namespace TheLastArk.Battle
{
    public enum PanicType { None, SelfLoathing, Impulse, Delirium, ContagiousFear, Despair, Compulsion }

    [System.Serializable]
    public sealed class PanicState
    {
        public PanicType Type { get; private set; }
        public SkillInfo RequiredSkill { get; set; }
        public bool IsActive => Type != PanicType.None;

        public void Begin(PanicType type) { Type = type; RequiredSkill = null; }
        public void Clear() => Begin(PanicType.None);
        public void SkillUsed(SkillInfo skill)
        {
            if (RequiredSkill == skill) RequiredSkill = null;
        }

        public static string Name(PanicType type) => type switch
        {
            PanicType.SelfLoathing => "자기혐오",
            PanicType.Impulse => "충동",
            PanicType.Delirium => "정신착란",
            PanicType.ContagiousFear => "전염성 공포",
            PanicType.Despair => "절망",
            PanicType.Compulsion => "강박증",
            _ => ""
        };

        public static string Description(PanicType type) => type switch
        {
            PanicType.SelfLoathing => "턴 시작 시 최대 체력의 10% 피해 · 체력 1 미만으로 감소하지 않음",
            PanicType.Impulse => "턴 시작 시 주사위로 무작위 스킬과 유효 대상을 선택해 사용",
            PanicType.Delirium => "턴 시작 시 다른 아군 1명에게 공격력 30% + 주문력 30% 물리 피해",
            PanicType.ContagiousFear => "턴 시작 시 다른 아군 1명의 최대 정신력의 10%만큼 정신력 감소",
            PanicType.Despair => "스킬 사용 불가 · 기본 공격, 이동, 방어는 가능",
            PanicType.Compulsion => "지정된 스킬을 먼저 사용해야 다른 스킬 사용 가능",
            _ => ""
        };

        public static float SelfDamage(float hp, float maxHp) => Mathf.Min(Mathf.Max(0f, hp - 1f), Mathf.Max(0f, maxHp * 0.1f));
    }
}
