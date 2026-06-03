/// <summary>
/// 현재 플레이어가 선택한 스킬과 시전자를 관리합니다.
/// 소모품 선택 상태도 함께 관리합니다.
/// </summary>
public class BattleSelectionState
{
    public SkillInfo      Skill  { get; private set; }
    public BattleCharacter Actor  { get; private set; }
    
    public TheLastArk.Data.ConsumableData Consumable { get; private set; }
    public int ConsumableIndex { get; private set; } = -1;

    /// <summary>스킬과 시전자가 모두 설정되어 있거나, 소모품이 설정되어 있을 때 true</summary>
    public bool IsReady => (Skill != null && Actor != null) || Consumable != null;

    /// <summary>스킬과 시전자를 동시에 설정합니다.</summary>
    public void Set(SkillInfo skill, BattleCharacter actor)
    {
        Skill = skill;
        Actor = actor;
    }

    /// <summary>소모품을 설정합니다.</summary>
    public void SetConsumable(TheLastArk.Data.ConsumableData consumable, int index)
    {
        Consumable = consumable;
        ConsumableIndex = index;
    }

    /// <summary>선택을 초기화합니다. 스킬 사용 완료 또는 취소 시 호출하세요.</summary>
    public void Clear()
    {
        Skill = null;
        Actor  = null;
        Consumable = null;
        ConsumableIndex = -1;
    }
}
