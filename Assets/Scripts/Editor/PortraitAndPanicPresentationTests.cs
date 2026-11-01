using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TheLastArk.Battle;
using TheLastArk.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PortraitAndPanicPresentationTests
{
    private readonly List<Object> owned = new List<Object>();
    private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
    [TearDown] public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }

    private CharacterData PortraitData()
    {
        return Own(Object.Instantiate(Resources.Load<CharacterData>("Characters/Player/Data/Pathfinder_Data")));
    }

    [Test] public void PortraitReusesSourceTextureAndCachedSprite()
    {
        var data = PortraitData();
        var portrait = data.PortraitSprite;
        Assert.AreSame(data.standingSprite.texture, portrait.texture);
        Assert.AreEqual(portrait.rect.width, portrait.rect.height);
        Assert.Less(portrait.rect.width, data.standingSprite.rect.width);
        Assert.AreSame(portrait, data.PortraitSprite);
        data.portraitFocus = new Vector2(.25f, .5f);
        Assert.AreNotSame(portrait, data.PortraitSprite);
        Assert.IsTrue(portrait == null, "The obsolete crop must be released.");
    }

    [TestCase(-3f, -2f, 0f)]
    [TestCase(4f, 3f, 100f)]
    public void CropStaysInsideSpriteEvenWithInvalidSettings(float x, float y, float zoom)
    {
        var data = PortraitData();
        data.portraitFocus = new Vector2(x, y);
        data.portraitZoom = zoom;
        Rect source = data.standingSprite.rect, crop = data.PortraitSprite.rect;
        Assert.GreaterOrEqual(crop.xMin, source.xMin);
        Assert.GreaterOrEqual(crop.yMin, source.yMin);
        Assert.LessOrEqual(crop.xMax, source.xMax);
        Assert.LessOrEqual(crop.yMax, source.yMax);
        Assert.Greater(crop.width, 0);
    }

    [Test] public void ManualPortraitAndMissingArtKeepTheirFallbacks()
    {
        var data = PortraitData();
        data.useStandingPortrait = false;
        Assert.AreSame(data.portraitSprite, data.PortraitSprite);
        data.useStandingPortrait = true;
        data.standingSprite = null;
        Assert.AreSame(data.portraitSprite, data.PortraitSprite);
        data.portraitSprite = null;
        Assert.IsNull(data.PortraitSprite);
    }

    [Test] public void NewStandingArtInvalidatesTheCachedPortrait()
    {
        var data = PortraitData();
        var previous = data.PortraitSprite;
        data.standingSprite = data.panicIllustrations[0];
        var current = data.PortraitSprite;
        Assert.AreNotSame(previous, current);
        Assert.AreSame(data.standingSprite.texture, current.texture);
    }

    [Test] public void FeatheredArtHasRenderableMeshWithSoftEdgesAndOpaqueCenter()
    {
        var go = Own(new GameObject("Feathered art", typeof(RectTransform)));
        var graphic = go.AddComponent<FeatheredIllustration>();
        graphic.rectTransform.sizeDelta = Vector2.one * 500;
        graphic.SetSprite(Resources.Load<CharacterData>("Characters/Player/Data/Pathfinder_Data").panicIllustrations[0]);
        Assert.NotNull(graphic.GetComponent<CanvasRenderer>());
        using (var vh = new VertexHelper())
        {
            typeof(FeatheredIllustration).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(VertexHelper) }, null).Invoke(graphic, new object[] { vh });
            var mesh = Own(new Mesh()); vh.FillMesh(mesh);
            Assert.Greater(mesh.vertexCount, 4);
            var colors = mesh.colors32;
            Assert.AreEqual(0, colors[0].a);
            bool soft = false, opaque = false;
            foreach (var color in colors) { soft |= color.a > 0 && color.a < 255; opaque |= color.a == 255; }
            Assert.IsTrue(soft && opaque);
        }
    }

    [TestCase(PanicType.Hallucination)] [TestCase(PanicType.Depression)]
    [TestCase(PanicType.Compulsion)] [TestCase(PanicType.Lethargy)]
    [TestCase(PanicType.Seizure)] [TestCase(PanicType.Agitation)]
    public void PanicIconShowsEffectOnHoverAndDisappearsAfterHealing(PanicType type)
    {
        var canvas = Own(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        ((RectTransform)canvas.transform).sizeDelta = new Vector2(1000, 600);
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Enemy Character.prefab"), canvas.transform);
        var enemy = root.GetComponent<EnemyBattleCharacter>();
        enemy.AssignAndInitialize(Resources.Load<CharacterData>("Characters/Enemy/Data/무장 상인_Data"));
        var status = enemy.CurrentStatus;
        status.currentMental = 0;
        status.panic.Begin(type);
        var view = root.GetComponent<CharacterView>();
        view.UpdateVisual(status);
        var icon = root.transform.Find("StatusIcons/Panic Icon");
        Assert.NotNull(icon);
        Assert.IsTrue(icon.gameObject.activeInHierarchy);
        Assert.NotNull(icon.GetComponentInChildren<PanicIconGraphic>().GetComponent<CanvasRenderer>());
        Assert.IsNull(root.transform.Find("Enemy Panic"));
        var hover = icon.GetComponent<BattleHoverTooltip>();
        hover.OnPointerEnter(null);
        var panel = canvas.transform.Find("Battle Detail Tooltip");
        string text = panel.GetComponentInChildren<TextMeshProUGUI>().text;
        StringAssert.Contains(PanicState.Name(type), text);
        StringAssert.Contains(PanicState.Description(type), text);
        Assert.Greater(enemy.BattleCharacterComponent.ReceiveMentalHeal(1), 0);
        Assert.IsFalse(status.panic.IsActive);
        Assert.IsFalse(icon.gameObject.activeInHierarchy);
        // MonoBehaviour.OnDisable is dispatched in Play Mode; pointer exit can be checked here.
        hover.OnPointerExit(null);
        Assert.IsFalse(panel.gameObject.activeSelf);
    }

    [Test] public void PanicRemainsVisibleWhenOtherStatusesOverflow()
    {
        var root = Own(Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Enemy Character.prefab")));
        var enemy = root.GetComponent<EnemyBattleCharacter>();
        enemy.AssignAndInitialize(Resources.Load<CharacterData>("Characters/Enemy/Data/무장 상인_Data"));
        enemy.CurrentStatus.panic.Begin(PanicType.Agitation);
        foreach (var type in new[] { EffectType.Stun, EffectType.Bleed, EffectType.Poison, EffectType.Weakness, EffectType.Frailty, EffectType.Corrosion })
            enemy.CurrentStatus.ApplyStatusEffect(type, 3f, 2);
        root.GetComponent<CharacterView>().UpdateVisual(enemy.CurrentStatus);
        var icons = root.transform.Find("StatusIcons");
        Assert.IsTrue(icons.Find("Panic Icon").gameObject.activeSelf);
        Assert.AreEqual(5, icons.childCount, "Panic + three statuses + overflow.");
        StringAssert.Contains("+3", icons.GetChild(4).GetComponentInChildren<TextMeshProUGUI>().text);
        var content = typeof(BattleHoverTooltip).GetField("content", BindingFlags.Instance | BindingFlags.NonPublic);
        string detail = (string)content.GetValue(icons.GetChild(4).GetComponent<BattleHoverTooltip>());
        Assert.GreaterOrEqual(detail.Split('\n').Length, 3);
    }
}
