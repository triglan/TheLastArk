using UnityEditor;
using UnityEngine;
using TheLastArk.Character;

namespace TheLastArk.Editor
{
    public static class EnemyIllustrationPreview
    {
        private static CharacterView prefabView;

        public static void Draw(CharacterData[] slots)
        {
            if (prefabView == null)
                prefabView = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Enemy Character.prefab")?.GetComponent<CharacterView>();
            if (prefabView == null || prefabView.displayImage == null) return;
            Rect area = GUILayoutUtility.GetRect(100f, 240f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(area, new Color(0.13f, 0.14f, 0.16f));
            GUI.BeginGroup(area);
            float floor = area.height - 36f;
            EditorGUI.DrawRect(new Rect(0f, floor, area.width, 1f), new Color(0.55f, 0.58f, 0.6f));
            var source = prefabView.displayImage;
            var rect = source.rectTransform;
            float largestHeight = EnemyIllustrationLayout.ReferenceHeightUI;
            foreach (CharacterData slot in slots)
                if (slot != null) largestHeight = Mathf.Max(largestHeight, EnemyIllustrationLayout.HeightUI(slot));
            float zoom = 140f / largestHeight;
            float width = area.width / Mathf.Max(1, slots.Length);
            for (int i = 0; i < slots.Length; i++)
            {
                CharacterData data = slots[i];
                float x = width * (i + 0.5f);
                GUI.Label(new Rect(width * i, floor + 8f, width, 22f), data != null ? $"{data.DisplayName} · {data.EnemyIllustrationHeight:0.#}cm" : "빈 슬롯", EditorStyles.centeredGreyMiniLabel);
                if (data == null) continue;
                Vector2 size = EnemyIllustrationLayout.FittedSize(rect.rect.size, data.standingSprite, true);
                Vector2 offset = EnemyIllustrationLayout.PositionOffset(data, size, rect.rect.size, rect.pivot, rect.localScale);
                float scale = EnemyIllustrationLayout.ScaleFor(data, size, rect.localScale);
                Vector2 drawn = Vector2.Scale(size, (Vector2)rect.localScale) * zoom;
                float pivotY = floor - rect.rect.height * rect.localScale.y * zoom * rect.pivot.y;
                // A fixed 180cm guide keeps relative height readable when the preview zooms out.
                float referenceHeight = EnemyIllustrationLayout.ReferenceHeightUI * zoom;
                float referenceWidth = drawn.y > 0f ? drawn.x / drawn.y * referenceHeight : 0f;
                Rect original = new Rect(x - referenceWidth * rect.pivot.x, floor - referenceHeight, referenceWidth, referenceHeight);
                Color guide = new Color(0.35f, 0.38f, 0.42f);
                EditorGUI.DrawRect(new Rect(original.x, original.y, original.width, 1f), guide);
                EditorGUI.DrawRect(new Rect(original.x, original.y, 1f, original.height), guide);
                EditorGUI.DrawRect(new Rect(original.xMax, original.y, 1f, original.height), guide);
                Rect target = new Rect(x + offset.x * zoom - drawn.x * scale * rect.pivot.x,
                    pivotY - offset.y * zoom - drawn.y * scale * (1f - rect.pivot.y), drawn.x * scale, drawn.y * scale);
                if (data.standingSprite != null)
                {
                    Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(data.standingSprite);
                    GUI.DrawTextureWithTexCoords(target, data.standingSprite.texture, new Rect(uv.x, uv.y, uv.z - uv.x, uv.w - uv.y));
                }
                else GUI.Box(target, "이미지 없음");
            }
            GUI.EndGroup();
            EditorGUILayout.LabelField("테두리 = 기준 키 180cm · 선 = 발밑 기준", EditorStyles.centeredGreyMiniLabel);
        }
    }
}
