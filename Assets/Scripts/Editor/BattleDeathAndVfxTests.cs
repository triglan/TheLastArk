using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TheLastArk.Skills;

public class BattleDeathAndVfxTests
{
    private readonly List<Object> owned = new List<Object>();
    private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
    [TearDown] public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); TheLastArk.Battle.AttackContext.ResetAll(); EffectEngine.CurrentCast = null;
    }
    private EnemyBattleCharacter Enemy()
    {
        var data = Own(ScriptableObject.CreateInstance<CharacterData>());
        data.isEnemy = true; data.maxHp = 10; data.maxMental = 10;
        var root = Own(new GameObject("DeathTestEnemy", typeof(RectTransform)));
        var enemy = root.AddComponent<EnemyBattleCharacter>();
        enemy.AssignAndInitialize(data);
        return enemy;
    }
    private BattleCharacter Ally()
    {
        var data = Own(ScriptableObject.CreateInstance<CharacterData>());data.maxHp=100;
        var actor = Own(new GameObject("VfxRecipient")).AddComponent<BattleCharacter>();
        actor.status = new CharacterStatus(data);return actor;
    }
    [Test] public void LethalHitHidesRootAndChildrenButRetainsSlotData()
    {
        var enemy = Enemy(); var ui = Own(new GameObject("HP and status UI"));ui.transform.SetParent(enemy.transform);
        var status = enemy.CurrentStatus;
        enemy.BattleCharacterComponent.ReceiveDamage(20, null, DamageType.True);
        Assert.AreEqual(0,status.currentHp); Assert.IsFalse(enemy.gameObject.activeSelf);Assert.IsFalse(ui.activeInHierarchy);
        Assert.AreSame(status,enemy.CurrentStatus);Assert.NotNull(enemy.GetComponent<EnemyAI>());
    }
    [Test] public void HealthCostAndDirectZeroAlsoHideWithoutRepeatedRewards()
    {
        var enemy=Enemy();enemy.BattleCharacterComponent.SpendHealth(10);Assert.IsFalse(enemy.gameObject.activeSelf);
        enemy.ClaimDefeatReward();Assert.IsTrue((bool)typeof(EnemyBattleCharacter).GetField("_defeatRewardClaimed",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(enemy));
        enemy.AssignAndInitialize(enemy.enemyData);enemy.CurrentStatus.currentHp=0;enemy.ClaimDefeatReward();Assert.IsFalse(enemy.gameObject.activeSelf);
    }
    [Test] public void DefeatedSlotCanBeReusedAndItsAiStateResets()
    {
        var enemy=Enemy();enemy.BattleCharacterComponent.ReceiveDamage(20,null,DamageType.True);
        enemy.AssignAndInitialize(enemy.enemyData);
        Assert.IsTrue(enemy.gameObject.activeSelf);Assert.AreEqual(10,enemy.CurrentHp);
        Assert.IsFalse((bool)typeof(EnemyBattleCharacter).GetField("_defeatRewardClaimed",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(enemy));
    }
    [Test] public void ShieldAbsorptionDoesNotHideLivingEnemy()
    {
        var enemy=Enemy();enemy.CurrentStatus.ApplyStatusEffect(EffectType.Shield,30,1);
        enemy.BattleCharacterComponent.ReceiveDamage(20,null,DamageType.True);
        Assert.IsTrue(enemy.gameObject.activeSelf);Assert.AreEqual(10,enemy.CurrentHp);
    }
    [Test] public void ConfiguredVisualUsesActualRecipientAndDeduplicatesDamageAndDebuff()
    {
        var caster=Ally();var target=Ally();var settings=new SkillVfxSettings{prefab=Own(new GameObject("TestEffect"))};
        var playback=new SkillVfxPlayback(settings);var damage=new EffectEntry{type=EffectType.Damage};
        Assert.IsTrue(playback.TryResolve(damage,caster,target,out var chosen,out var recipient));Assert.AreSame(target,recipient);Assert.AreSame(settings,chosen);
        Assert.IsFalse(playback.TryResolve(new EffectEntry{type=EffectType.Vulnerable},caster,target,out chosen,out recipient));
        Assert.IsTrue(playback.TryResolve(damage,caster,Ally(),out chosen,out recipient),"AOE gets one visual on each recipient");
    }
    [Test] public void PerEffectOverrideAndCasterPlacementAreIndependent()
    {
        var caster=Ally();var target=Ally();var prefab=Own(new GameObject("TestEffect"));
        var shared=new SkillVfxSettings{prefab=prefab};var self=new SkillVfxSettings{prefab=prefab,placement=SkillVfxPlacement.Caster};
        var effect=new EffectEntry{type=EffectType.Shield,visualEffect=self};var playback=new SkillVfxPlayback(shared);
        Assert.IsTrue(playback.TryResolve(effect,caster,target,out var chosen,out var recipient));Assert.AreSame(caster,recipient);Assert.AreSame(self,chosen);
        Assert.IsFalse(playback.TryResolve(effect,caster,Ally(),out chosen,out recipient));
        Assert.IsTrue(playback.TryResolve(new EffectEntry{type=EffectType.Damage},caster,target,out chosen,out recipient));Assert.AreSame(target,recipient);
    }
    [Test] public void VisualAnchorUsesIllustrationCenterAndCopiedSettingsDoNotShareValues()
    {
        var actor=Ally();
        var image=Own(new GameObject("Standing Illust",typeof(RectTransform),typeof(UnityEngine.UI.Image))).GetComponent<UnityEngine.UI.Image>();
        image.transform.SetParent(actor.transform,false);image.rectTransform.sizeDelta=new Vector2(200,300);image.rectTransform.pivot=Vector2.zero;
        var view=actor.gameObject.AddComponent<CharacterView>();actor.view=view;
        view.displayImage=image;Assert.AreEqual(image.rectTransform.TransformPoint(new Vector3(100,150)),SkillVfxSettings.Anchor(actor));
        var settings=new SkillVfxSettings{prefab=Own(new GameObject("TestEffect")),scale=2,offset=new Vector2(3,4)};
        var copy=settings.Copy();copy.scale=5;Assert.AreEqual(2,settings.scale);Assert.AreSame(settings.prefab,copy.prefab);
    }
    [Test] public void AttackVisualDoesNotAlsoPlayOnCasterForItsFocusStatus()
    {
        var caster=Ally();var target=Ally();var settings=new SkillVfxSettings{prefab=Own(new GameObject("ArrowEffect"))};
        var focus=new EffectEntry{type=EffectType.Focus};var damage=new EffectEntry{type=EffectType.Damage};
        var playback=new SkillVfxPlayback(settings,new List<EffectEntry>{focus,damage});
        Assert.IsFalse(playback.TryResolve(focus,caster,caster,out var chosen,out var recipient));
        Assert.IsTrue(playback.TryResolve(damage,caster,target,out chosen,out recipient));Assert.AreSame(target,recipient);
        playback=new SkillVfxPlayback(settings,new List<EffectEntry>{focus});
        Assert.IsTrue(playback.TryResolve(focus,caster,caster,out chosen,out recipient),"Pure preparation skill still has a visual");
    }
}
