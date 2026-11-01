using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TheLastArk.UI;

public class MirageBattlePresentationTests
{
    private readonly List<Object> owned = new List<Object>();
    [TearDown] public void Cleanup() { for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]); owned.Clear(); }
    private T Own<T>(T item) where T : Object { owned.Add(item); return item; }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void FormationCentersAndKeepsFeetAboveControls(int count)
    {
        var canvas = Own(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        ((RectTransform)canvas.transform).sizeDelta = new Vector2(1000f, 600f);
        var parent = new GameObject("Enemies", typeof(RectTransform)); parent.transform.SetParent(canvas.transform, false);
        var enemies = new List<EnemyBattleCharacter>();
        for (int i = 0; i < count; i++)
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Enemy Character.prefab"), parent.transform);
            root.transform.localScale = Vector3.one * 0.5f;
            var enemy = root.GetComponent<EnemyBattleCharacter>();
            enemy.AssignAndInitialize(Resources.Load<CharacterData>("Characters/Enemy/Data/무장 상인_Data"));
            enemies.Add(enemy);
        }
        var layout = parent.AddComponent<BattleEnemyFormationLayout>(); layout.Initialize(parent.transform);
        float average = 0f;
        Vector2 firstSize = enemies[0].GetComponent<CharacterView>().IllustrationBounds.size;
        for (int i = 0; i < count; i++)
        {
            var view = enemies[i].GetComponent<CharacterView>();
            Assert.That(Vector2.Distance(firstSize, view.IllustrationBounds.size), Is.LessThan(0.01f),
                "Identical enemies must not change size by slot or row.");
            Vector3 position = canvas.transform.InverseTransformPoint(view.transform.position);
            average += position.x;
            Assert.That(position.x, Is.EqualTo(-500f + 1000f * BattleEnemyFormationLayout.NormalizedX(i, count)).Within(0.01f));
            Assert.That(position.y + view.IllustrationBaseline * 0.5f,
                Is.EqualTo(-300f + 600f * BattleEnemyFormationLayout.NormalizedBaseline(i, count)).Within(0.01f));
            Assert.That(position.y + (view.IllustrationBounds.yMax + view.EnemyHUDHeight) * 0.5f, Is.LessThanOrEqualTo(240.1f));
        }
        Assert.That(average / count, Is.EqualTo(120f).Within(0.01f));
        if (count > 1)
        {
            Assert.Greater(enemies[1].transform.position.y, enemies[0].transform.position.y);
            Assert.Less(enemies[1].transform.GetSiblingIndex(), enemies[0].transform.GetSiblingIndex());
            Vector3 firstPosition = enemies[0].transform.position;
            layout.Refresh();
            Assert.That(Vector3.Distance(firstPosition, enemies[0].transform.position), Is.LessThan(0.01f), "Drawing order must not reshuffle slots.");
            for (int i = 1; i < count; i++) enemies[i].gameObject.SetActive(false);
            layout.Refresh();
            Assert.That(canvas.transform.InverseTransformPoint(enemies[0].transform.position).x, Is.EqualTo(120f).Within(0.01f));
        }
    }

    [Test] public void LargeEnemyDoesNotShrinkItsHumanoidNeighbor()
    {
        var canvas = Own(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        ((RectTransform)canvas.transform).sizeDelta = new Vector2(1000f, 600f);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Enemy Character.prefab");
        var front = Object.Instantiate(prefab, canvas.transform);
        var back = Object.Instantiate(prefab, canvas.transform);
        front.transform.localScale = back.transform.localScale = Vector3.one * 0.5f;
        var humanoid = Resources.Load<CharacterData>("Characters/Enemy/Data/흑철 방패병_Data");
        front.GetComponent<EnemyBattleCharacter>().AssignAndInitialize(humanoid);
        back.GetComponent<EnemyBattleCharacter>().AssignAndInitialize(humanoid);
        var layout = canvas.AddComponent<BattleEnemyFormationLayout>();
        layout.Initialize(canvas.transform);
        float heightBefore = front.GetComponent<CharacterView>().IllustrationBounds.height;
        back.GetComponent<EnemyBattleCharacter>().AssignAndInitialize(
            Resources.Load<CharacterData>("Characters/Enemy/Data/사막을 삼키는 자_Data"));
        layout.Refresh();
        Assert.That(front.GetComponent<CharacterView>().IllustrationBounds.height, Is.EqualTo(heightBefore).Within(0.01f));
    }

    [Test] public void TurnClockInitializesWithoutTweenAndKeepsLargeNumbersInItsFace()
    {
        var canvas = Own(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
        var label = new GameObject("TurnCountText", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
        label.transform.SetParent(canvas.transform, false);
        var clock = BattleTurnClock.Create(canvas.transform, label);
        clock.SetTurn(1);
        Assert.AreEqual(1, clock.DisplayedTurn);
        Assert.AreEqual("1", label.text);
        Assert.IsFalse(clock.IsAnimating);
        clock.SetTurn(120);
        Assert.AreEqual("120", label.text);
        Assert.IsTrue(label.enableAutoSizing);
        Assert.IsFalse(label.raycastTarget);
        Assert.AreEqual(clock, BattleTurnClock.Create(canvas.transform, label));
    }

    [Test] public void IntentIsCompactAndDetailsAreHoverOnly()
    {
        var root = Own(new GameObject("Intent"));
        var ui = root.AddComponent<EnemyIntentUI>();
        var effect = new EnemyIntentEffectData { ActionType = EnemyPatternActionType.Effect, EffectType = EffectType.Damage, Value = 3, HitCount = 4 };
        ui.Show(new EnemyIntentData { PatternName = "연타", Effects = new List<EnemyIntentEffectData> { effect } });
        Assert.AreEqual("3×4", EnemyIntentUI.CompactValue(effect));
        Assert.AreEqual(30f, ui.Panel.rect.height);
        Assert.AreEqual(204f, ui.Panel.rect.width);
        Assert.NotNull(ui.Panel.GetComponent<BattleHoverTooltip>());
        Assert.IsNull(ui.Panel.Find("Detail"));
        ui.Hide(); Assert.IsFalse(ui.Panel.gameObject.activeSelf);
    }

    [Test] public void DiceDockIsInsideTrayAndFacesDoNotStretch()
    {
        var canvas = Own(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
        var tray = new GameObject("BattleSkillTray", typeof(RectTransform)); tray.transform.SetParent(canvas.transform, false);
        ((RectTransform)tray.transform).sizeDelta = new Vector2(BattleHudLayout.TrayWidth, BattleHudLayout.TrayHeight);
        var ui = canvas.AddComponent<BattleDiceUI>(); ui.Initialize(null, canvas.transform);
        var dock = tray.transform.Find("BattleDicePool") as RectTransform;
        Assert.NotNull(dock);
        var layout = dock.GetComponent<GridLayoutGroup>();
        Assert.AreEqual(10, layout.constraintCount);
        Assert.IsFalse(layout.enabled, "The row uses persistent die slots instead of packing active children.");
        Assert.Greater(BattleDiceUI.DieSize, 48f);
        Assert.LessOrEqual(10 * BattleDiceUI.DieSize + 9 * layout.spacing.x, dock.rect.width);
        Assert.LessOrEqual(BattleDiceUI.DieSize, dock.rect.height);
        var sources=dock.GetComponentsInChildren<DiceDragSource>(true);
        Assert.AreEqual(10,sources.Length);
        foreach(var source in sources)
            Assert.AreEqual(Vector2.one*BattleDiceUI.DieSize,((RectTransform)source.transform).sizeDelta);
        Assert.NotNull(dock.GetComponent<ScrollRect>().content);
    }

    [Test] public void StatusCountAndHoverTooltipKeepFullDetails()
    {
        var canvas = Own(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        ((RectTransform)canvas.transform).sizeDelta = new Vector2(1000f, 600f);
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Enemy Character.prefab"), canvas.transform);
        var enemy = root.GetComponent<EnemyBattleCharacter>();
        enemy.AssignAndInitialize(Resources.Load<CharacterData>("Characters/Enemy/Data/무장 상인_Data"));
        enemy.CurrentStatus.activeStatusEffects.Add(new ActiveStatusEffect(EffectType.Stun, 0f, 3));
        root.GetComponent<CharacterView>().UpdateVisual(enemy.CurrentStatus);
        var icon = root.transform.Find("StatusIcons/Status Icon");
        Assert.NotNull(icon);
        StringAssert.Contains("3", icon.Find("Count").GetComponent<TMPro.TextMeshProUGUI>().text);
        var hover = icon.GetComponent<BattleHoverTooltip>(); hover.OnPointerEnter(null);
        var panel = canvas.transform.Find("Battle Detail Tooltip"); Assert.NotNull(panel);
        StringAssert.Contains("3턴", panel.GetComponentInChildren<TMPro.TextMeshProUGUI>().text);
        Assert.IsFalse(panel.GetComponent<Image>().raycastTarget);
        hover.OnPointerExit(null); Assert.IsFalse(panel.gameObject.activeSelf);
    }

    [Test] public void EnhancementReservesSpaceAndKeepsDetailsOnHover()
    {
        var canvas = Own(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        ((RectTransform)canvas.transform).sizeDelta = new Vector2(1000f, 600f);
        var data = Own(Object.Instantiate(Resources.Load<CharacterData>("Characters/Enemy/Data/무장 상인_Data")));
        data.enemyRuntimeVariables = new List<EnemyRuntimeVariableDefinition> {
            new EnemyRuntimeVariableDefinition { id = "power", displayName = "공격 강화", initialValue = 4, showInUi = true }
        };
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Enemy Character.prefab"), canvas.transform);
        root.transform.localScale = Vector3.one * 0.5f;
        root.GetComponent<EnemyBattleCharacter>().AssignAndInitialize(data);
        var view = root.GetComponent<CharacterView>();
        var enhancement = root.GetComponent<EnemyEnhancementUI>() ?? root.AddComponent<EnemyEnhancementUI>();
        enhancement.Refresh();
        Assert.AreEqual(36f, enhancement.LayoutHeight);
        var layout = canvas.AddComponent<BattleEnemyFormationLayout>(); layout.Initialize(canvas.transform);
        var panel = root.transform.Find("Enemy Enhancements") as RectTransform;
        Assert.Less(panel.anchoredPosition.y + panel.rect.height * 0.5f, view.IllustrationBounds.yMin);
        Assert.LessOrEqual(canvas.transform.InverseTransformPoint(root.transform.position).y + (view.IllustrationBounds.yMax + view.EnemyHUDHeight) * 0.5f, 240.1f);
        var hover = panel.GetComponent<BattleHoverTooltip>(); hover.OnPointerEnter(null);
        StringAssert.Contains("공격 강화 +4", canvas.transform.Find("Battle Detail Tooltip").GetComponentInChildren<TMPro.TextMeshProUGUI>().text);
        root.GetComponent<EnemyAI>().RuntimeState.Set("power", 0); enhancement.Refresh();
        Assert.AreEqual(0f, enhancement.LayoutHeight);
        Assert.IsFalse(panel.gameObject.activeSelf);
    }

    [Test] public void SpentDiceKeepSurvivorsInTheirSlotsAndGenerationReusesHoles()
    {
        var root = Own(new GameObject("Dice slot test"));
        var ui = root.AddComponent<BattleDiceUI>();
        var pool = new TheLastArk.Battle.ActionDicePool(_ => 4);
        pool.BeginTurn(10);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var update = typeof(BattleDiceUI).GetMethod("UpdateDieSlots", flags);
        var field = typeof(BattleDiceUI).GetField("_dieSlots", flags);
        update.Invoke(ui, new object[] { pool });
        var slots = (Dictionary<int, int>)field.GetValue(ui);
        var before = new Dictionary<int, int>(slots);
        int first = pool.Dice[0].Id, middle = pool.Dice[4].Id;
        Assert.IsTrue(pool.TrySpend(new[] { first, middle }));
        update.Invoke(ui, new object[] { pool });
        foreach (var die in pool.Dice) Assert.AreEqual(before[die.Id], slots[die.Id]);
        Assert.IsFalse(slots.ContainsKey(first));
        Assert.IsFalse(slots.ContainsKey(middle));
        pool.Roll(2);
        update.Invoke(ui, new object[] { pool });
        foreach (var die in pool.Dice)
            if (before.ContainsKey(die.Id)) Assert.AreEqual(before[die.Id], slots[die.Id]);
        Assert.AreEqual(10, new HashSet<int>(slots.Values).Count);
        pool.BeginTurn(3);
        update.Invoke(ui, new object[] { pool });
        Assert.AreEqual(3, slots.Count);
        foreach (var die in pool.Dice) Assert.That(slots[die.Id], Is.InRange(0, 9));
    }

    [Test] public void StatusOverflowDoesNotMoveHeaderAndRetainsHiddenDetails()
    {
        var root = Own(Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Enemy Character.prefab")));
        var enemy = root.GetComponent<EnemyBattleCharacter>();
        enemy.AssignAndInitialize(Resources.Load<CharacterData>("Characters/Enemy/Data/무장 상인_Data"));
        var view = root.GetComponent<CharacterView>();
        var bar = view.barUI.hpSlider.transform.parent as RectTransform;
        Vector2 before = bar.anchoredPosition;
        float height = view.EnemyHUDHeight;
        foreach (var type in new[] { EffectType.Stun, EffectType.Bleed, EffectType.Poison, EffectType.Weakness, EffectType.Frailty, EffectType.Corrosion })
            enemy.CurrentStatus.ApplyStatusEffect(type, 3f, 2);
        view.UpdateVisual(enemy.CurrentStatus);
        Assert.AreEqual(before, bar.anchoredPosition);
        Assert.AreEqual(height, view.EnemyHUDHeight);
        var icons = root.transform.Find("StatusIcons");
        Assert.AreEqual(5, icons.childCount);
        StringAssert.Contains("+2", icons.GetChild(4).GetComponentInChildren<TMPro.TextMeshProUGUI>().text);
        var content = typeof(BattleHoverTooltip).GetField("content", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        string detail = (string)content.GetValue(icons.GetChild(4).GetComponent<BattleHoverTooltip>());
        Assert.IsTrue(detail.Contains("\n"), "Both hidden status descriptions must remain accessible.");
        Assert.Less(((RectTransform)icons).anchoredPosition.y, view.IllustrationBounds.yMin);
    }

    public void VerifyDockedDiceDragInPlayMode()
    {
        var canvas = Own(new GameObject("Drag Canvas", typeof(RectTransform), typeof(Canvas)));
        var tray = new GameObject("BattleSkillTray", typeof(RectTransform)); tray.transform.SetParent(canvas.transform, false);
        var ui = canvas.AddComponent<BattleDiceUI>(); ui.Initialize(null, canvas.transform);
        var source = tray.GetComponentInChildren<DiceDragSource>();
        source.Configure(7, (RectTransform)canvas.transform);
        var pointer = new UnityEngine.EventSystems.PointerEventData(null) { position = new Vector2(120f, 80f) };
        source.OnBeginDrag(pointer);
        Assert.AreEqual(7, source.DieId);
        Assert.NotNull(canvas.transform.Find("DraggedDie"));
        Assert.IsFalse(source.GetComponent<CanvasGroup>().blocksRaycasts);
        pointer.position = new Vector2(250f, 120f); source.OnDrag(pointer);
        source.OnEndDrag(pointer);
        Assert.IsTrue(source.GetComponent<CanvasGroup>().blocksRaycasts);
    }
}
