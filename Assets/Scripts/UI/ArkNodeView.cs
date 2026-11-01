using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using TheLastArk.Data;
using TheLastArk.Run;

namespace TheLastArk.UI
{
    public sealed class ArkNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public ArkNodeDefinition Definition { get; private set; }
        public RectTransform star;
        public ArkConstellationGraphic glow, icon;
        public TextMeshProUGUI caption;
        public TextMeshProUGUI title;
        private bool hovered, selected;
        public static readonly Color Blue = new Color(.28f,.73f,1f);
        public static readonly Color Gold = new Color(1f,.78f,.30f);
        public void Bind(ArkNodeDefinition definition) { Definition=definition; Refresh(); }
        public void Select(bool value) { selected=value; Refresh(); }
        public void Refresh()
        {
            if(Definition==null)return;
            int level=ArkProgression.Data.Level(Definition.id);
            Color tint=level>=Definition.MaxLevel && !Definition.placeholder ? (Definition.major?Gold:Blue) : new Color(.89f,.95f,1f);
            bool available=ArkProgression.Data.PrerequisitesMet(ArkProgressionConfig.Load(),Definition,out _);
            if(Definition.placeholder || !available)tint.a=.38f;
            glow.color=icon.color=tint;
            glow.highlighted=hovered||selected;glow.SetVerticesDirty();
            caption.text=Definition.placeholder ? "미공개" : available || level>0 ? $"{level} / {Definition.MaxLevel}" : "·";
            caption.color=tint;
            if(title!=null){title.gameObject.SetActive(Definition.major||hovered||selected);title.color=selected||hovered?Color.white:new Color(.70f,.79f,.9f);}
        }
        private void Update()
        {
            float target=selected?1.4f:hovered?1.18f:1f;
            star.localScale=Vector3.Lerp(star.localScale,Vector3.one*target,1-Mathf.Exp(-12f*Time.unscaledDeltaTime));
        }
        public void OnPointerEnter(PointerEventData data){hovered=true;Refresh();}
        public void OnPointerExit(PointerEventData data){hovered=false;Refresh();}
        private void OnDisable(){hovered=false;}
    }
}
