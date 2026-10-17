using System.Collections;
using UnityEngine.TestTools;

public class MirageEnemyPlayModeSmokeTests
{
    // Enter/ExitPlayMode reloads the domain. Re-register to retain a verifiable result
    // even when an external test runner loses its in-memory callback during the reload.
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterResults()
    {
        var api = UnityEngine.ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();
        api.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
        api.RegisterCallbacks(new ResultRecorder());
    }

    private sealed class ResultRecorder : UnityEditor.TestTools.TestRunner.Api.ICallbacks
    {
        public void RunStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor tests) { }
        public void TestStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor test) { }
        public void TestFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result) { }
        public void RunFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result)
        {
            var text = new System.Text.StringBuilder();
            Collect(result, text);
            if (text.Length > 0) System.IO.File.WriteAllText("Library/MirageEnemyTestResults.txt", text.ToString());
        }
        private static void Collect(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result, System.Text.StringBuilder text)
        {
            if (result.HasChildren) foreach (var child in result.Children) Collect(child, text);
            else if (result.Test.FullName.StartsWith("Mirage") || result.Test.FullName.StartsWith("EnemyPatternCombatTests"))
                text.AppendLine(result.ResultState + " | " + result.Test.FullName + (string.IsNullOrEmpty(result.Message) ? "" : " | " + result.Message));
        }
    }

    [UnityTest]
    public IEnumerator BattlePresentationWorksInPlayMode()
    {
        yield return new EnterPlayMode();
        System.Exception failure = null;
        var tests = new MirageBattlePresentationTests();
        try
        {
            tests.FormationCentersAndKeepsFeetAboveControls(1); tests.Cleanup();
            tests.FormationCentersAndKeepsFeetAboveControls(4); tests.Cleanup();
            tests.IntentIsCompactAndDetailsAreHoverOnly(); tests.Cleanup();
            tests.DiceDockIsInsideTrayAndFacesDoNotStretch(); tests.Cleanup();
            tests.StatusCountAndHoverTooltipKeepFullDetails(); tests.Cleanup();
            tests.EnhancementReservesSpaceAndKeepsDetailsOnHover(); tests.Cleanup();
            tests.VerifyDockedDiceDragInPlayMode();
        }
        catch (System.Exception exception) { failure = exception; }
        finally { tests.Cleanup(); }
        yield return new ExitPlayMode();
        if (failure != null) throw failure;
    }

    [UnityTest]
    public IEnumerator EncounterLayoutWorksInPlayMode()
    {
        yield return new EnterPlayMode();
        System.Exception failure = null;
        var tests = new MirageEncounterLayoutTests();
        tests.Setup();
        try
        {
            NUnit.Framework.Assert.AreEqual("mirage", RunManager.Instance.CurrentRegionId);
            var prepare = typeof(RunManager).GetMethod("TryPrepareBattleEncounter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            bool prepared = (bool)prepare.Invoke(RunManager.Instance, new object[] { new MapNode(4, 4, 0, NodeType.Combat) });
            NUnit.Framework.Assert.IsTrue(prepared);
            NUnit.Framework.Assert.AreEqual(4, RunManager.Instance.CurrentEncounterPool.MinFloor);
            NUnit.Framework.Assert.Contains(RunManager.Instance.CurrentEncounter.EncounterId, RunManager.Instance.State.appearedEncounterIDs);
            tests.AllBattleNodeTypesAreCoveredAtEveryFloorAndAfterHistoryExhaustion();
            tests.PrefabInitializationUpdatesOnlyIllustrationAndResetsOnReuse();
        }
        catch (System.Exception exception) { failure = exception; }
        finally { tests.Cleanup(); }
        yield return new ExitPlayMode();
        if (failure != null) throw failure;
    }

    [UnityTest]
    public IEnumerator EnemyPatternsWorkInPlayMode()
    {
        yield return new EnterPlayMode();
        System.Exception failure = null;
        var tests = new MirageEnemySpecificationTests();
        try
        {
            tests.RiderDropsArmorOnChargeAndRemountsOnFourthAction();
            tests.Cleanup();
            tests.DevourerTracksItsOwnShieldDamageAndRejectsVictimDamage();
            tests.Cleanup();
            tests.LastHitBleedIsSkippedWhenCounterInterruptsTheCombo();
        }
        catch (System.Exception exception) { failure = exception; }
        finally { tests.Cleanup(); }
        yield return new ExitPlayMode();
        if (failure != null) throw failure;
    }
}
