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
            CrewSkillRevision.ApplyArbalist(data);
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
