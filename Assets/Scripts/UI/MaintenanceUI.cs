using UnityEngine;

namespace TheLastArk.UI
{
    /// <summary>Village workspace using the same train controls as the management panel.</summary>
    public sealed class MaintenanceUI : MonoBehaviour
    {
        private static MaintenanceUI instance;

        public static void Show()
        {
            if (instance == null)
            {
                var canvas = HudCanvasUtility.FindOverlayCanvas();
                if (canvas == null) return;
                GameObject root = null;
                root = ManagementDialog.Create(canvas, "MaintenancePopup", "마을 정비소", "WORKSHOP  /  기차 강화 · 건설 · 파츠", () => root.SetActive(false), 0.08f, out var window);
                instance = root.AddComponent<MaintenanceUI>();
                var body = ManagementTheme.Rect("MaintenanceBody", window);
                ManagementTheme.Place(body, Vector2.zero, Vector2.one, new Vector2(28, 38), new Vector2(-28, -108));
                body.gameObject.AddComponent<TrainManagementUI>().BuildEmbedded(body, true);
            }
            instance.gameObject.SetActive(true);
            instance.transform.SetAsLastSibling();
        }

        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
