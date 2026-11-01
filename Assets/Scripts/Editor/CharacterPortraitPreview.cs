using UnityEditor;
using UnityEngine;

namespace TheLastArk.Editor
{
    public static class CharacterPortraitPreview
    {
        public static void Draw(CharacterData data)
        {
            if (data==null || !data.useStandingPortrait || data.standingSprite==null) return;
            Rect area=GUILayoutUtility.GetRect(100,210,GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(area,new Color(.075f,.09f,.12f));
            Sprite source=data.standingSprite;
            float scale=Mathf.Min(area.width*.52f/source.rect.width,(area.height-12)/source.rect.height);
            Vector2 size=source.rect.size*scale;
            Rect full=new Rect(area.x+area.width*.27f-size.x*.5f,area.y+6,size.x,size.y);
            DrawSprite(full,source);
            Rect crop=data.PortraitCropRect;
            Rect border=new Rect(full.x+(crop.x-source.rect.x)*scale,full.y+(source.rect.yMax-crop.yMax)*scale,crop.width*scale,crop.height*scale);
            Color gold=new Color(.95f,.75f,.4f);
            EditorGUI.DrawRect(new Rect(border.x,border.y,border.width,2),gold);
            EditorGUI.DrawRect(new Rect(border.x,border.yMax-2,border.width,2),gold);
            EditorGUI.DrawRect(new Rect(border.x,border.y,2,border.height),gold);
            EditorGUI.DrawRect(new Rect(border.xMax-2,border.y,2,border.height),gold);
            float thumbSize=Mathf.Min(140,area.width*.38f);
            Rect thumb=new Rect(area.x+area.width*.74f-thumbSize*.5f,area.center.y-thumbSize*.5f,thumbSize,thumbSize);
            DrawSprite(thumb,data.PortraitSprite);
            var e=Event.current;
            if ((e.type==EventType.MouseDown || e.type==EventType.MouseDrag) && e.button==0 && full.Contains(e.mousePosition))
            {
                Undo.RecordObject(data,"초상화 얼굴 위치");
                data.portraitFocus=new Vector2(Mathf.Clamp01((e.mousePosition.x-full.x)/full.width),Mathf.Clamp01(1-(e.mousePosition.y-full.y)/full.height));
                EditorUtility.SetDirty(data);GUI.changed=true;e.Use();
            }
            EditorGUILayout.LabelField("왼쪽 그림의 얼굴을 클릭·드래그해 위치를 조절하세요.",EditorStyles.wordWrappedMiniLabel);
        }
        private static void DrawSprite(Rect target,Sprite sprite)
        {
            Vector4 uv=UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            GUI.DrawTextureWithTexCoords(target,sprite.texture,new Rect(uv.x,uv.y,uv.z-uv.x,uv.w-uv.y));
        }
    }
}
