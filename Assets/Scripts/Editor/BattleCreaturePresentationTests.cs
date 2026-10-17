using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using TheLastArk.Character;
using TheLastArk.UI;

public class BattleCreaturePresentationTests
{
    private readonly List<Object> owned = new List<Object>();
    [TearDown] public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }

    private BattleEnemyFormationLayout Formation(string name, int count)
    {
        var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        owned.Add(canvas);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        ((RectTransform)canvas.transform).sizeDelta = new Vector2(1000f, 600f);
        var data = Resources.Load<CharacterData>("Characters/Enemy/Data/" + name + "_Data");
        for (int i = 0; i < count; i++)
        {
            var enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Enemy Character.prefab"), canvas.transform);
            enemy.transform.localScale = Vector3.one * 0.5f;
            enemy.GetComponent<EnemyBattleCharacter>().AssignAndInitialize(data);
        }
        var layout = canvas.AddComponent<BattleEnemyFormationLayout>();
        layout.Initialize(canvas.transform);
        return layout;
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void WideCreaturesFitCombatAreaAtEqualSize(int count)
    {
        var layout = Formation("가시 거북", count);
        var first = layout.Slots[0].GetComponent<CharacterView>().IllustrationBounds.size;
        for (int i = 0; i < count; i++)
        {
            var view = layout.Slots[i].GetComponent<CharacterView>();
            Rect b = view.IllustrationBounds;
            Vector3 p = layout.transform.InverseTransformPoint(view.transform.position);
            Assert.That(Vector2.Distance(first, b.size), Is.LessThan(0.01f));
            Assert.That(p.x + b.xMin * 0.5f, Is.GreaterThanOrEqualTo(-220.1f));
            Assert.That(p.x + b.xMax * 0.5f, Is.LessThanOrEqualTo(420.1f));
            Assert.That(p.y + b.yMin * 0.5f, Is.GreaterThanOrEqualTo(-165.1f));
            Assert.That(p.y + (b.yMax + view.EnemyHUDHeight) * 0.5f, Is.LessThanOrEqualTo(240.1f));
            if (count == 4) Assert.LessOrEqual(b.width * 0.5f, 260.1f);
        }
        if (count > 1)
        {
            float gap = layout.Slots[1].transform.localPosition.y - layout.Slots[0].transform.localPosition.y;
            Assert.Greater(gap, 120f, "Expose rear bodies instead of stacking all wide silhouettes in one row.");
        }
    }

    [Test] public void SpritePaddingDoesNotInterceptClicks()
    {
        var layout = Formation("가시 거북", 1);
        var view = layout.Slots[0].GetComponent<CharacterView>();
        var filter = view.displayImage.GetComponent<EnemyIllustrationRaycastFilter>();
        foreach (var collider in view.GetComponents<Collider2D>()) Assert.IsFalse(collider.enabled);
        Rect b = view.IllustrationBounds;
        Assert.IsTrue(filter.IsRaycastLocationValid(view.transform.TransformPoint(b.center), null));
        Assert.IsFalse(filter.IsRaycastLocationValid(view.transform.TransformPoint(new Vector2(b.center.x, b.yMax + 40f)), null));
        Assert.IsFalse(filter.IsRaycastLocationValid(view.transform.TransformPoint(new Vector2(b.center.x, b.yMin - 40f)), null));
    }

    [Test] public void ReplacingSameHeightHumanoidRefreshesCreatureLayout()
    {
        var layout = Formation("흑철 방패병", 4);
        var update = typeof(BattleEnemyFormationLayout).GetMethod("LateUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        update.Invoke(layout, null);
        var creature = Resources.Load<CharacterData>("Characters/Enemy/Data/가시 거북_Data");
        foreach (var slot in layout.Slots) slot.AssignAndInitialize(creature);
        update.Invoke(layout, null);
        Assert.Greater(layout.Slots[1].transform.localPosition.y - layout.Slots[0].transform.localPosition.y, 120f);
        foreach (var slot in layout.Slots)
            Assert.LessOrEqual(slot.GetComponent<CharacterView>().IllustrationBounds.width * 0.5f, 260.1f);
    }

    [Test] public void BodyTypeSupportsAutomaticAndExplicitClassification()
    {
        var data = Object.Instantiate(Resources.Load<CharacterData>("Characters/Enemy/Data/가시 거북_Data"));
        owned.Add(data);
        Assert.IsTrue(EnemyIllustrationLayout.UsesCreatureLayout(data));
        Assert.IsFalse(EnemyIllustrationLayout.UsesCreatureLayout(Resources.Load<CharacterData>("Characters/Enemy/Data/흑철 방패병_Data")));
        var serialized = new SerializedObject(data);
        serialized.FindProperty("enemyFormationType").enumValueIndex = (int)EnemyFormationType.Humanoid;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Assert.IsFalse(EnemyIllustrationLayout.UsesCreatureLayout(data));
    }

    [TestCase(2, 0)] [TestCase(2, 1)] [TestCase(3, 0)] [TestCase(3, 2)] [TestCase(4, 0)] [TestCase(4, 3)]
    public void MixedFormationDoesNotMiniaturizeLandscapeArt(int count, int riderIndex)
    {
        var layout = Formation("흑철 방패병", count);
        int neighborIndex = riderIndex == 0 ? 1 : 0;
        float neighborHeight = layout.Slots[neighborIndex].GetComponent<CharacterView>().IllustrationBounds.height;
        layout.Slots[riderIndex].AssignAndInitialize(Resources.Load<CharacterData>("Characters/Enemy/Data/흑철 기수_Data"));
        layout.Refresh();
        var rider = layout.Slots[riderIndex].GetComponent<CharacterView>();
        Assert.GreaterOrEqual(rider.IllustrationBounds.height, neighborHeight * 0.9f,
            "The taller authored rider must not be squeezed into a narrow creature cell.");
        Assert.That(layout.Slots[neighborIndex].GetComponent<CharacterView>().IllustrationBounds.height,
            Is.EqualTo(neighborHeight).Within(0.01f), "Do not shrink humanoids to match wide artwork.");
        foreach (var slot in layout.Slots)
        {
            var view = slot.GetComponent<CharacterView>();
            Rect b = view.IllustrationBounds;
            Vector3 p = layout.transform.InverseTransformPoint(view.transform.position);
            Assert.GreaterOrEqual(p.x + b.xMin * 0.5f, -220.1f);
            Assert.LessOrEqual(p.x + b.xMax * 0.5f, 420.1f);
            Assert.GreaterOrEqual(p.y + b.yMin * 0.5f, -165.1f);
            Assert.LessOrEqual(p.y + (b.yMax + view.EnemyHUDHeight) * 0.5f, 240.1f);
        }
        Rect first = rider.IllustrationBounds;
        layout.Refresh();
        Assert.That(Vector2.Distance(first.size, rider.IllustrationBounds.size), Is.LessThan(0.01f));
    }
}
