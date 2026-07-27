using UnityEngine;
using TMPro;

namespace TheLastArk.UI
{
    public static class TMPFontManager
    {
        private static TMP_FontAsset mainKoreanFont;

        public static TMP_FontAsset MainKoreanFont
        {
            get
            {
                if (mainKoreanFont == null)
                {
                    mainKoreanFont = Resources.Load<TMP_FontAsset>("Fonts/Main_Fonts");
                    if (mainKoreanFont == null)
                    {
                        mainKoreanFont = Resources.Load<TMP_FontAsset>("Fonts/LiberationSans SDF");
                    }
                }
                return mainKoreanFont;
            }
        }

        public static void ApplyFont(TMP_Text textComponent)
        {
            if (textComponent == null) return;
            if (MainKoreanFont != null)
            {
                textComponent.font = MainKoreanFont;
            }
        }

        public static void ApplyFontToAll(Transform parent)
        {
            if (parent == null) return;
            TMP_Text[] texts = parent.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                ApplyFont(t);
            }
        }
    }
}
