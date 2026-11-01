using System;

namespace TheLastArk.Battle
{
    /// <summary>Separate battle-long charges; Origin parts retain their own per-turn uses.</summary>
    public sealed class ArkDiceBenefits
    {
        public int ExtraFirstTurnDice { get; }
        public int EnhanceCapacity { get; }
        public int RerollCapacity { get; }
        public int EnhancesLeft { get; private set; }
        public int RerollsLeft { get; private set; }
        public ArkDiceBenefits(int extra, int enhance, int reroll)
        {
            ExtraFirstTurnDice=Math.Max(0,extra);
            EnhanceCapacity=EnhancesLeft=Math.Max(0,enhance);
            RerollCapacity=RerollsLeft=Math.Max(0,reroll);
        }
        public int ExtraDiceForTurn(int turn)=>turn==1?ExtraFirstTurnDice:0;
        public bool TryEnhance(ActionDicePool pool,int id)
        {
            if(EnhancesLeft<=0 || pool==null || !pool.TryAdjust(id,1))return false;
            EnhancesLeft--;return true;
        }
        public bool TryReroll(ActionDicePool pool,int id)
        {
            var die=pool?.Find(id);
            if(RerollsLeft<=0 || die==null || !die.IsAvailable || die.HasFixedRoll)return false;
            if(!pool.TryReroll(new[]{id}))return false;
            RerollsLeft--;return true;
        }
    }
}
