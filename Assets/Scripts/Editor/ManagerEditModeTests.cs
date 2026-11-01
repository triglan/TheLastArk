using System.Reflection;
using NUnit.Framework;
using TheLastArk.UI;
using UnityEngine;

public class ManagerEditModeTests
{
    [TestCase(typeof(RunManager), "_instance")]
    [TestCase(typeof(NotificationManager), "instance")]
    public void EditorInitializationDoesNotUsePlayModePersistence(System.Type type, string fieldName)
    {
        var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
        var prior = field.GetValue(null);
        var go = new GameObject("ManagerEditModeTest");
        go.SetActive(false);
        try
        {
            field.SetValue(null, null);
            var manager = go.AddComponent(type);
            Assert.DoesNotThrow(() => type.GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null));
            Assert.AreSame(manager, field.GetValue(null));
            Assert.AreNotEqual("DontDestroyOnLoad", go.scene.name);
            var notifications = manager as NotificationManager;
            if (notifications != null)
            {
                notifications.ShowMessage("Edit mode validation", Color.white);
                Assert.IsNull(type.GetField("notificationCanvasObj", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager));
            }
        }
        finally { Object.DestroyImmediate(go); field.SetValue(null, prior); }
    }
}
