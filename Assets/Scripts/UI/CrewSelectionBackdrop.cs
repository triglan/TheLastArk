using UnityEngine;

namespace TheLastArk.UI
{
    /// <summary>Blurs a small copy once per selection, retaining the original illustration assets.</summary>
    [ExecuteAlways, RequireComponent(typeof(UnityEngine.UI.RawImage))]
    public sealed class CrewSelectionBackdrop : MonoBehaviour
    {
        [SerializeField] private Sprite source;
        private Sprite renderedSource;
        private RenderTexture blurred;
        private UnityEngine.UI.RawImage image;

        private void OnEnable() => SetIllustration(source);
        public void SetIllustration(Sprite sprite)
        {
            source = sprite;
            if (image == null) image = GetComponent<UnityEngine.UI.RawImage>();
            image.raycastTarget = false;
            image.color = new Color(.52f,.56f,.62f,1);
            if (sprite == renderedSource && blurred != null) { FitCover(); return; }
            Release();
            if (source == null) { image.texture = Texture2D.blackTexture; return; }
            var shader = Resources.Load<Shader>("UI/CrewBackdropBlur");
            if (shader == null) { image.texture = source.texture; FitCover(); return; }
            int width = 320, height = Mathf.Max(1, Mathf.RoundToInt(width*source.rect.height/source.rect.width));
            blurred = new RenderTexture(width,height,0,RenderTextureFormat.ARGB32)
                { name="Crew blurred backdrop",hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp };
            var scratch = RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32);
            scratch.filterMode = FilterMode.Bilinear; scratch.wrapMode = TextureWrapMode.Clamp;
            var material = new Material(shader) { hideFlags=HideFlags.HideAndDontSave };
            var previous = RenderTexture.active;
            try
            {
                Rect rect = source.textureRect;
                Graphics.Blit(source.texture, blurred, new Vector2(rect.width/source.texture.width, rect.height/source.texture.height),
                    new Vector2(rect.x/source.texture.width, rect.y/source.texture.height));
                for (int i=0;i<3;i++)
                {
                    material.SetVector("_Direction", new Vector4(2,0,0,0)); Graphics.Blit(blurred,scratch,material);
                    material.SetVector("_Direction", new Vector4(0,2,0,0)); Graphics.Blit(scratch,blurred,material);
                }
                renderedSource=source; image.texture=blurred;
            }
            finally
            {
                RenderTexture.active=previous;
                RenderTexture.ReleaseTemporary(scratch);
                if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
            }
            FitCover();
        }
        private void OnRectTransformDimensionsChange() => FitCover();
        private void FitCover()
        {
            if (image==null || source==null) return;
            Rect rect=image.rectTransform.rect;
            if(rect.height<=0 || rect.width<=0)return;
            float ratio=(rect.width/rect.height)/(source.rect.width/source.rect.height);
            image.uvRect=ratio>1 ? new Rect(0,(1-1/ratio)*.5f,1,1/ratio) : new Rect((1-ratio)*.5f,0,ratio,1);
        }
        private void OnDisable() => Release();
        private void Release()
        {
            if (image!=null) image.texture=null;
            if (blurred!=null) { blurred.Release(); if(Application.isPlaying)Destroy(blurred);else DestroyImmediate(blurred); }
            blurred=null;renderedSource=null;
        }
    }
}
