using UnityEngine;

namespace TheLastArk.UI
{
    public static class HudCanvasUtility
    {
        public static Canvas FindOverlayCanvas()
        {
            var battleHud = GameObject.Find("TopBarCanvas");
            return battleHud != null ? battleHud.GetComponent<Canvas>() : Object.FindObjectOfType<Canvas>();
        }
    }
}
