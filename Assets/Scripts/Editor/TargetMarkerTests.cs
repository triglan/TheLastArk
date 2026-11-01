using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TheLastArk.UI;

public class TargetMarkerTests
{
    private GameObject root;

    [SetUp] public void Setup()
    {
        root = new GameObject("Target marker test", typeof(RectTransform));
        root.SetActive(false);
    }

    [TearDown] public void Cleanup()
    {
        if (root != null) Object.DestroyImmediate(root);
    }

    private CharacterView Ally(string prefab)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/" + prefab + ".prefab");
        return Object.Instantiate(asset, root.transform, false).GetComponent<CharacterView>();
    }

    private static RectTransform Badge(CharacterView view) => (RectTransform)view.barUI.hpSlider.transform.Find("Enemy Target Marker");

    [TestCase("Main Character")]
    [TestCase("Sub Character")]
    public void EnemyIntentUsesOneHealthBarBadgeWithoutSelectionArrow(string prefab)
    {
        var ally = Ally(prefab);
        var hp = (RectTransform)ally.barUI.hpSlider.transform;
        float rightEdge = hp.anchoredPosition.x + hp.rect.xMax;
        ally.SetEnemyTargeted(true);
        float reservedWidth = hp.rect.width;
        ally.SetEnemyTargeted(true);
        var badge = Badge(ally);
        Assert.NotNull(badge);
        Assert.AreSame(ally.barUI.hpSlider.transform, badge.parent);
        Assert.AreEqual(new Vector2(0, .5f), badge.anchorMax);
        Assert.Less(badge.anchoredPosition.x, 0);
        Assert.AreEqual(rightEdge, hp.anchoredPosition.x + hp.rect.xMax, .01f, "Reserve warning space without extending into the skill panel.");
        Assert.AreEqual(reservedWidth, hp.rect.width, .01f, "Repeated intent refresh must not shrink the bars again.");
        Assert.AreSame(Resources.Load<Sprite>("UI/BattleMarkers/EnemyThreat"), badge.GetComponent<Image>().sprite);
        Assert.NotNull(badge.GetComponent<Image>().sprite, "The painted warning must be included in the project.");
        Assert.NotNull(badge.GetComponent<BattleHoverTooltip>());
        Assert.IsTrue(badge.gameObject.activeSelf);
        ally.SetEnemyTargeted(false);
        Assert.IsFalse(badge.gameObject.activeSelf);
        ally.SetEnemyTargeted(true);
        Assert.AreSame(badge, Badge(ally));
        Assert.IsTrue(badge.gameObject.activeSelf);
    }

    [Test]
    public void ChangingPlayerSelectionReusesArrowAndKeepsEnemyIntentSeparate()
    {
        var ally = Ally("Main Character");
        var other = Ally("Sub Character");
        ally.SetEnemyTargeted(true);
        other.SetEnemyTargeted(true);
        var handler = root.AddComponent<TargetArrow>();
        var arrow = new GameObject("Player selection", typeof(RectTransform), typeof(Image));
        arrow.transform.SetParent(root.transform, false);
        handler.arrowUI = arrow.GetComponent<RectTransform>();
        var select = typeof(TargetArrow).GetMethod("SelectTarget", BindingFlags.Instance | BindingFlags.NonPublic);
        select.Invoke(handler, new object[] { ally.gameObject });
        select.Invoke(handler, new object[] { other.gameObject });
        Assert.AreSame(other.gameObject, handler.target);
        Assert.AreSame(arrow.transform, handler.arrowUI);
        Assert.IsTrue(arrow.activeSelf);
        Assert.IsTrue(arrow.GetComponent<Image>().enabled);
        Assert.IsFalse(arrow.GetComponent<Image>().raycastTarget);
        Assert.AreSame(Resources.Load<Sprite>("UI/BattleMarkers/SelectionArrow"), arrow.GetComponent<Image>().sprite);
        Assert.NotNull(arrow.GetComponent<Image>().sprite);
        Assert.AreEqual(0, arrow.GetComponentsInChildren<BattleHudGlyph>().Length);
        ally.SetEnemyTargeted(false);
        Assert.IsTrue(arrow.activeSelf, "Clearing enemy intent must not hide the player selection.");
        handler.Deselect(false);
        Assert.IsFalse(arrow.activeSelf);
        Assert.IsTrue(Badge(other).gameObject.activeSelf,
            "Deselecting must not clear an enemy's intended target.");
    }

    [Test]
    public void IntentHoverHighlightsOnlyItsTargetsAndClearsOnRebindOrHide()
    {
        var first = Ally("Main Character");
        var second = Ally("Sub Character");
        var data = ScriptableObject.CreateInstance<CharacterData>();
        try
        {
            data.maxHp = 30;
            var a = first.GetComponent<BattleCharacter>(); a.status = new CharacterStatus(data); a.view = first;
            var b = second.GetComponent<BattleCharacter>(); b.status = new CharacterStatus(data); b.view = second;
            first.SetEnemyTargeted(true); second.SetEnemyTargeted(true);
            var aBadge = Badge(first);
            var bBadge = Badge(second);
            var hover = root.AddComponent<EnemyIntentTargetHighlight>();
            hover.Bind(new[] { a }); hover.OnPointerEnter(null);
            Assert.Greater(aBadge.localScale.x, 1f); Assert.AreEqual(Vector3.one, bBadge.localScale);
            hover.Bind(new[] { b });
            Assert.AreEqual(Vector3.one, aBadge.localScale); Assert.Greater(bBadge.localScale.x, 1f);
            typeof(EnemyIntentTargetHighlight).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hover, null);
            Assert.AreEqual(Vector3.one, bBadge.localScale);
            Assert.IsTrue(aBadge.gameObject.activeSelf); Assert.IsTrue(bBadge.gameObject.activeSelf);
        }
        finally { Object.DestroyImmediate(data); }
    }

    [Test]
    public void InitiallyHiddenArrowRendersAboveArtAfterFirstActivationAndReselection()
    {
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        root.SetActive(true);
        var manager = new GameObject("GameManager");
        manager.transform.SetParent(root.transform, false);
        var handler = manager.AddComponent<TargetArrow>();
        var arrow = new GameObject("Initially hidden arrow", typeof(RectTransform), typeof(Image));
        arrow.transform.SetParent(manager.transform, false);
        arrow.SetActive(false);
        handler.arrowUI = (RectTransform)arrow.transform;
        var enemyArt = new GameObject("Enemy art", typeof(RectTransform), typeof(Image), typeof(Canvas));
        enemyArt.transform.SetParent(root.transform, false);
        var artCanvas = enemyArt.GetComponent<Canvas>();
        artCanvas.overrideSorting = true;
        artCanvas.sortingOrder = 1;
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(TargetArrow).GetMethod("Start", flags).Invoke(handler, null);
        var select = typeof(TargetArrow).GetMethod("SelectTarget", flags);
        select.Invoke(handler, new object[] { enemyArt });
        var markerCanvas = arrow.GetComponent<Canvas>();
        Assert.IsTrue(markerCanvas.overrideSorting, "The first activation must not reset independent sorting.");
        Assert.Greater(markerCanvas.sortingOrder, artCanvas.sortingOrder);
        Assert.AreEqual(canvas.sortingLayerID, markerCanvas.sortingLayerID);
        Assert.Less(markerCanvas.sortingOrder, canvas.sortingOrder + 30, "Keep command overlays above the marker.");
        handler.Deselect(false);
        canvas.sortingOrder = 50;
        artCanvas.sortingOrder = 51;
        select.Invoke(handler, new object[] { enemyArt });
        Assert.IsTrue(markerCanvas.overrideSorting);
        Assert.Greater(markerCanvas.sortingOrder, artCanvas.sortingOrder);
        Assert.IsFalse(arrow.GetComponent<Image>().raycastTarget, "The overlapping marker must not steal target clicks.");
    }

    [Test]
    public void FloatingArrowTracksIllustrationRatherThanHudAndBobsWithoutDrift()
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Enemy Character.prefab");
        var enemy = Object.Instantiate(asset, root.transform, false).GetComponent<EnemyBattleCharacter>();
        enemy.AssignAndInitialize(Resources.Load<CharacterData>("Characters/Enemy/Data/흑철 호위대장_Data"));
        var view = enemy.GetComponent<CharacterView>();
        Assert.AreEqual(100f, view.EnemyHUDHeight, "Selection artwork must retain the original formation envelope.");
        Assert.AreEqual(56f, ((RectTransform)view.barUI.hpSlider.transform.parent).anchoredPosition.y - view.IllustrationBounds.yMax, .01f);
        var originalBounds = view.IllustrationBounds;
        var handler = root.AddComponent<TargetArrow>();
        var arrow = new GameObject("Painted selection", typeof(RectTransform), typeof(Image));
        arrow.transform.SetParent(root.transform, false); handler.arrowUI = (RectTransform)arrow.transform;
        typeof(TargetArrow).GetMethod("SelectTarget", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(handler, new object[] { enemy.gameObject });
        var position = typeof(TargetArrow).GetMethod("PositionMarker", BindingFlags.Instance | BindingFlags.NonPublic);
        position.Invoke(handler, new object[] { 0f });
        Vector3 start = arrow.transform.position;
        Rect bounds = view.IllustrationBounds;
        var head = view.transform.TransformPoint(new Vector3(bounds.center.x, bounds.yMax));
        Assert.AreEqual(head.x, start.x, .01f); Assert.Less(start.y, head.y);
        Assert.AreEqual(originalBounds, view.IllustrationBounds, "Selecting an enemy must not resize its illustration.");
        view.barUI.hpSlider.transform.parent.position += new Vector3(300, 200, 0);
        position.Invoke(handler, new object[] { 0f }); Assert.AreEqual(start, arrow.transform.position);
        position.Invoke(handler, new object[] { Mathf.PI / (2f * handler.floatSpeed) });
        Assert.AreEqual(handler.floatAmplitude, arrow.transform.position.y - start.y, .01f);
        for (int i = 0; i < 10; i++) position.Invoke(handler, new object[] { 0f });
        Assert.AreEqual(start, arrow.transform.position, "Floating must not accumulate a position offset.");
        view.displayImage.rectTransform.anchoredPosition += new Vector2(40, 15);
        position.Invoke(handler, new object[] { 0f });
        Assert.Less(Vector3.Distance(start + view.transform.TransformVector(new Vector3(40, 15)), arrow.transform.position), .01f);
    }
}
