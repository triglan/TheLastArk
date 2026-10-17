using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MirageEncounterLayoutTests
{
    private readonly List<Object> owned = new List<Object>();
    private Random.State randomState;
    [SetUp] public void Setup() { randomState = Random.state; Random.InitState(917); }
    [TearDown] public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        Random.state = randomState;
    }
    private T Own<T>(T obj) where T : Object { owned.Add(obj); return obj; }
    private CharacterData Enemy(float height = 180f, float x = 0f, float y = 0f, bool feet = true)
    {
        var data = Own(ScriptableObject.CreateInstance<CharacterData>());
        data.isEnemy = true;
        var serialized = new SerializedObject(data);
        serialized.FindProperty("enemyIllustrationHeight").floatValue = height;
        serialized.FindProperty("enemyIllustrationOffset").vector2Value = new Vector2(x, y);
        serialized.FindProperty("enemyIllustrationKeepFeet").boolValue = feet;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return data;
    }
    private CharacterView View()
    {
        var root = Own(new GameObject("Layout test", typeof(RectTransform)));
        var child = new GameObject("Standing Illust", typeof(RectTransform), typeof(Image));
        child.transform.SetParent(root.transform, false);
        var image = child.GetComponent<Image>();
        image.rectTransform.sizeDelta = new Vector2(400f, 704f);
        image.rectTransform.anchoredPosition = new Vector2(12f, 30f);
        image.rectTransform.localScale = new Vector3(0.8f, 0.9f, 1f);
        return root.AddComponent<CharacterView>();
    }

    [Test] public void DefaultHeightUses525UIUnitsAndPreservesFeetAndSlot()
    {
        var view = View(); var data = Enemy();
        view.ApplyIllustrationLayout(data);
        var rect = view.displayImage.rectTransform;
        Assert.That(rect.rect.height * rect.localScale.y, Is.EqualTo(525f).Within(0.001f));
        Assert.That(rect.anchoredPosition.y - 262.5f, Is.EqualTo(30f - 352f * 0.9f).Within(0.001f));
        Assert.AreEqual(Vector3.one, view.transform.localScale);
    }

    [Test] public void ScalingKeepsFeetAndOffsetsWithoutAccumulating()
    {
        var view = View(); var data = Enemy(270f, 18f, -7f);
        var rect = view.displayImage.rectTransform;
        float bottom = rect.anchoredPosition.y - 352f * rect.localScale.y;
        for (int i = 0; i < 20; i++) view.ApplyIllustrationLayout(data);
        Assert.That(rect.rect.height * rect.localScale.y, Is.EqualTo(787.5f).Within(0.001f));
        Assert.That(rect.anchoredPosition.x, Is.EqualTo(30f).Within(0.001f));
        Assert.That(rect.anchoredPosition.y - 352f * rect.localScale.y, Is.EqualTo(bottom - 7f).Within(0.001f));
        view.ApplyIllustrationLayout(Enemy());
        Assert.That(rect.anchoredPosition.x, Is.EqualTo(12f).Within(0.001f));
        Assert.That(rect.rect.height * rect.localScale.y, Is.EqualTo(525f).Within(0.001f));
        Assert.That(rect.anchoredPosition.y - 262.5f, Is.EqualTo(bottom).Within(0.001f));
    }

    [Test] public void CenterScalingAndAspectFittedFeetUseCorrectOrigin()
    {
        var view = View();
        view.ApplyIllustrationLayout(Enemy(360f, 4f, 5f, false));
        Assert.AreEqual(new Vector2(16f, 35f), view.displayImage.rectTransform.anchoredPosition);
        var texture = Own(new Texture2D(200, 100));
        view.displayImage.sprite = Own(Sprite.Create(texture, new Rect(0, 0, 200, 100), Vector2.one * 0.5f));
        view.displayImage.preserveAspect = true;
        view.ApplyIllustrationLayout(Enemy(360f));
        // A wide image stays proportional and its feet align with the original frame bottom.
        var rect = view.displayImage.rectTransform;
        Assert.That(rect.anchoredPosition.y - 100f * rect.localScale.y, Is.EqualTo(30f - 352f * 0.9f).Within(0.001f));
        Assert.IsTrue(view.displayImage.preserveAspect);
        Assert.That(200f * rect.localScale.y, Is.EqualTo(1050f).Within(0.001f));
    }

    [Test] public void PrefabInitializationUpdatesOnlyIllustrationAndResetsOnReuse()
    {
        var root = Own(Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Enemy Character.prefab")));
        var view = root.GetComponent<CharacterView>();
        var character = root.GetComponent<BattleCharacter>();
        var imageRect = view.displayImage.rectTransform;
        Vector3 baseScale = imageRect.localScale;
        Vector2 basePosition = imageRect.anchoredPosition;
        var bar = view.barUI.transform;
        Vector3 barPosition = bar.localPosition, barScale = bar.localScale;
        var data = Enemy(117f, -12f, 15f);
        character.Init(data, false);
        character.status.currentHp -= 2f;
        view.UpdateVisual(character.status);
        var size = TheLastArk.Character.EnemyIllustrationLayout.FittedSize(imageRect.rect.size, view.displayImage.sprite, true);
        Assert.That(size.y * imageRect.localScale.y, Is.EqualTo(341.25f).Within(0.001f));
        Assert.AreEqual(barPosition, bar.localPosition);
        Assert.AreEqual(barScale, bar.localScale);
        character.Init(Enemy(), false);
        Assert.That(size.y * imageRect.localScale.y, Is.EqualTo(525f).Within(0.001f));
        Assert.That(imageRect.anchoredPosition.y - 262.5f,
            Is.EqualTo(basePosition.y - imageRect.rect.height * imageRect.pivot.y * baseScale.y).Within(0.001f));
    }

    [TestCase(170f, 200, 100)] [TestCase(180f, 200, 100)]
    [TestCase(180f, 100, 200)] [TestCase(200f, 200, 400)]
    public void EnteredHeightIsIndependentOfSpriteAspectAndResolution(float height, int width, int textureHeight)
    {
        var view = View();
        var texture = Own(new Texture2D(width, textureHeight));
        view.displayImage.sprite = Own(Sprite.Create(texture, new Rect(0, 0, width, textureHeight), Vector2.one * 0.5f));
        var data = Enemy(height);
        // Legacy scale must not multiply the new height.
        var serialized = new SerializedObject(data);
        serialized.FindProperty("enemyIllustrationScale").floatValue = 1.8f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        view.ApplyIllustrationLayout(data);
        var rect = view.displayImage.rectTransform;
        var size = TheLastArk.Character.EnemyIllustrationLayout.FittedSize(rect.rect.size, view.displayImage.sprite, true);
        Assert.That(size.y * rect.localScale.y, Is.EqualTo(height / 180f * 525f).Within(0.001f));
    }

    [Test] public void MirageRegionHasAllSixPoolsAndThirtyThreeFormations()
    {
        var table = BattleEncounterTable.LoadDefault();
        Assert.AreEqual("mirage", table.StartingRegionId);
        var pools = table.GetPoolsForRegion("mirage").ToArray();
        Assert.AreEqual(6, pools.Length);
        var formations = pools.SelectMany(p => p.Encounters).Distinct().ToArray();
        Assert.AreEqual(33, formations.Length);
        var enemies = formations.SelectMany(f => f.EnemySlots).Where(e => e != null).Distinct().ToArray();
        Assert.AreEqual(23, enemies.Length);
        foreach (var enemy in enemies) Assert.AreEqual("mirage", enemy.regionId, enemy.name);
        foreach (var formation in formations)
        {
            Assert.AreEqual(4, formation.EnemySlots.Length);
            Assert.IsTrue(formation.HasAnyEnemy);
            Assert.IsTrue(formation.EnemySlots.All(e => e == null || e.isEnemy));
        }
    }

    [TestCase(1, 1, 3)] [TestCase(3, 1, 3)]
    [TestCase(4, 4, 5)] [TestCase(5, 4, 5)]
    [TestCase(6, 6, 10)] [TestCase(10, 6, 10)]
    [TestCase(11, 11, 15)] [TestCase(15, 11, 15)]
    public void NormalFloorBoundariesSelectTheirOwnPools(int floor, int min, int max)
    {
        var encounter = BattleEncounterResolver.Resolve(BattleEncounterTable.LoadDefault(), "mirage", NodeType.Combat, floor, 0, null, out var pool);
        Assert.NotNull(encounter);
        Assert.AreEqual(min, pool.MinFloor); Assert.AreEqual(max, pool.MaxFloor);
    }

    [Test] public void AllBattleNodeTypesAreCoveredAtEveryFloorAndAfterHistoryExhaustion()
    {
        var table = BattleEncounterTable.LoadDefault();
        var history = table.GetPoolsForRegion("mirage").SelectMany(p => p.Encounters).Select(e => e.EncounterId).ToList();
        foreach (NodeType node in new[] { NodeType.Combat, NodeType.Elite, NodeType.Boss })
            for (int floor = 1; floor <= 15; floor++)
            {
                var encounter = BattleEncounterResolver.Resolve(table, "mirage", node, floor, 14, history, out var pool);
                Assert.NotNull(encounter, $"{node}, {floor}");
                Assert.IsTrue(pool.Matches("mirage", node, floor, 14));
                CollectionAssert.Contains(pool.Encounters, encounter);
            }
    }

    [Test] public void ResolverRejectsMissingRegionFloorNodeAndCombatCount()
    {
        var table = BattleEncounterTable.LoadDefault();
        Assert.IsNull(BattleEncounterResolver.Resolve(table, "missing-region", NodeType.Combat, 1, 0, null, out _));
        Assert.IsNull(BattleEncounterResolver.Resolve(table, "mirage", NodeType.Combat, 16, 0, null, out _));
        Assert.IsNull(BattleEncounterResolver.Resolve(table, "mirage", NodeType.Rest, 1, 0, null, out _));
        Assert.IsNull(BattleEncounterResolver.Resolve(table, "mirage", NodeType.Combat, 1, 100, null, out _));
    }

    [Test] public void UnseenFormationIsPreferredWithinPool()
    {
        var table = BattleEncounterTable.LoadDefault();
        var source = table.GetPoolsForRegion("mirage").Single(p => p.NodeType == NodeType.Combat && p.MinFloor == 1);
        var history = source.Encounters.Skip(1).Select(e => e.EncounterId).ToList();
        var encounter = BattleEncounterResolver.Resolve(table, "mirage", NodeType.Combat, 1, 2, history, out _);
        Assert.AreSame(source.Encounters[0], encounter);
    }

    [Test] public void EmptyHighPriorityPoolDoesNotHideValidMatchingPool()
    {
        var table = Own(ScriptableObject.CreateInstance<BattleEncounterTable>());
        var empty = Own(ScriptableObject.CreateInstance<EnemyEncounterPool>());
        var valid = Own(ScriptableObject.CreateInstance<EnemyEncounterPool>());
        var encounter = Own(EnemyEncounterData.CreateRuntimeInstance("layout-test", "Test", new[] { Enemy() }));
        var properties = new SerializedObject(empty);
        properties.FindProperty("priority").intValue = 1000;
        properties.ApplyModifiedPropertiesWithoutUndo();
        properties = new SerializedObject(valid);
        var entries = properties.FindProperty("encounters"); entries.arraySize = 1;
        entries.GetArrayElementAtIndex(0).objectReferenceValue = encounter;
        properties.ApplyModifiedPropertiesWithoutUndo();
        table.RegisterPool(empty); table.RegisterPool(valid);
        Assert.AreSame(encounter, BattleEncounterResolver.Resolve(table, "temp", NodeType.Combat, 1, 0, null, out var pool));
        Assert.AreSame(valid, pool);
    }

    [Test] public void RunRegionIsSerializedAndResetWithRunHistory()
    {
        var state = new RunState { currentRegionId = "mirage", combatCount = 8 };
        state.appearedEncounterIDs.Add("test");
        var restored = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(state));
        Assert.AreEqual("mirage", restored.currentRegionId);
        restored.Reset();
        Assert.IsEmpty(restored.currentRegionId);
        Assert.IsEmpty(restored.appearedEncounterIDs);
    }
}
