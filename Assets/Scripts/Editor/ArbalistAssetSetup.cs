using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TheLastArk.Data;
using TheLastArk.Skills;

namespace TheLastArk.Editor
{
    public static class ArbalistAssetSetup
    {
        public const string AssetPath="Assets/Resources/Characters/Player/Data/Arbalist_Data.asset";

        // Explicit, create-only setup: later edits made in the character editor are preserved.
        public static CharacterData CreateIfMissing()
        {
            var existing=AssetDatabase.LoadAssetAtPath<CharacterData>(AssetPath);
            if (existing!=null) return existing;
            var data=ScriptableObject.CreateInstance<CharacterData>();
            data.characterId="03"; data.characterName="마커스 블랙쏜"; data.jobName="Arbalist";
            data.synergies=new List<SynergyType>{SynergyType.Mirage,SynergyType.Ranger};
            data.maxHp=30; data.maxMental=30; data.baseAttack=7; data.spellPower=0;
            data.armor=1; data.magicResist=1; data.critRate=50;
            data.standingSprite=Resources.Load<Sprite>("Characters/Player/Illust/_TempIllust/_TempIllust");
            data.portraitSprite=Resources.Load<Sprite>("Characters/Player/Illust/_TempIllust/_TempPortrait");
            data.passiveSkill=new SkillInfo{skillName="명사수의 눈",baseCost=0,passiveTrait=PassiveTrait.MarksmanEye,levels=new SkillLevelData[3]};
            data.activeSkills=new[]{Skill("꿰뚫어 쏘기"),Skill("폭발 화살"),Skill("후퇴 사격"),Skill("정조준: 필중")};
            for(int rank=0;rank<3;rank++)
            {
                data.passiveSkill.levels[rank]=new SkillLevelData
                {
                    targetType=TargetType.Self,
                    description="치명타 확률 +"+(rank==0?25:50)+"%p.\n치명타 확률이 100%를 초과하면 초과 1%p마다 공격력 +1%."+(rank==0?"\n개화: 치명타 확률 +25%p, 치명타 피해 +25%p 추가.":"\n치명타 피해 +25%p."),
                    effects=new List<EffectEntry>{new EffectEntry{value=rank==0?25:50,secondaryValue=rank==0?0:25}}
                };
                float pierce=rank==0?1.5f:1.8f;
                data.activeSkills[0].levels[rank]=Level(DiceCondition.AtLeast,4,0,
                    "4 이상 · 적 1명\n눈금 + 공격력 "+(pierce*100).ToString("0")+"% 물리 피해.\n집중: 이번 스킬은 도발 무시."+(rank==2?"\n체력이 최대인 대상에게 피해 +50%.":""),
                    new EffectEntry{type=EffectType.Focus,focusOnlyThisSkill=true,duration=1,targetScope=EffectTargetScope.Self},
                    new EffectEntry{type=EffectType.Damage,multiplier=pierce,fixedPerDie=1,fullHealthDamageBonusPercent=rank==2?50:0});
                data.activeSkills[1].levels[rank]=Level(DiceCondition.Any,1,20,
                    "충전 20 · 적 1명\n공격력 "+(pierce*100).ToString("0")+"% 물리 피해.\n첫 대상에게 준 체력 피해의 50%만큼 양옆 적에게 물리 피해."+(rank==2?"\n첫 대상에게 준 체력 피해의 25%만큼 모든 적에게 추가 물리 피해.":""),
                    new EffectEntry{type=EffectType.Damage,multiplier=pierce,splashAdjacentPercent=50,splashUsesActualDamage=true,splashAllPercent=rank==2?25:0});
                data.activeSkills[2].levels[rank]=Level(DiceCondition.Even,1,0,
                    "짝수 · 적 1명\n공격력 "+(rank==0?60:80)+"% 물리 피해.\n다음 자기 턴까지 "+(rank==2?"모든 피해":"체력 피해")+"를 ⌊눈금 ÷ 2⌋회 회피.",
                    new EffectEntry{type=EffectType.Damage,multiplier=rank==0?.6f:.8f},
                    new EffectEntry{type=EffectType.Evasion,multiplier=0,charges=0,valuePerDieDivisor=2,secondaryValue=rank==2?1:0,targetScope=EffectTargetScope.Self});
                data.activeSkills[3].levels[rank]=Level(DiceCondition.AtLeast,rank==0?5:4,40,
                    (rank==0?"5":"4")+" 이상 · 충전 40 · 적 1명\n공격력 300% + 대상이 잃은 체력의 30% 물리 피해.\n집중·관통: 이번 스킬은 도발·방어력 무시."+(rank==2?"\n체력 50% 미만인 대상에게 피해 +50%.":""),
                    new EffectEntry{type=EffectType.Focus,focusOnlyThisSkill=true,duration=1,targetScope=EffectTargetScope.Self},
                    new EffectEntry{type=EffectType.Damage,multiplier=3,targetMissingHealthPercent=30,armorIgnorePercent=100,belowHalfHealthDamageBonusPercent=rank==2?50:0});
            }
            AssetDatabase.CreateAsset(data,AssetPath);
            AssetDatabase.SaveAssetIfDirty(data);
            return data;
        }
        private static SkillInfo Skill(string name)=>new SkillInfo{skillName=name,baseCost=0,levels=new SkillLevelData[3]};
        private static SkillLevelData Level(DiceCondition condition,int value,int charge,string description,params EffectEntry[] effects)
            =>new SkillLevelData{description=description,targetType=TargetType.SingleEnemy,
                diceInput=new DiceInputRule{condition=condition,value=value,chargeTarget=charge},effects=new List<EffectEntry>(effects)};
    }
}
