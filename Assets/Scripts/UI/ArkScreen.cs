using System;
using System.Collections.Generic;
using TheLastArk.Data;
using TheLastArk.Run;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    public sealed class ArkScreen : MonoBehaviour
    {
        private static readonly Color White=new Color(.92f,.96f,1f), Muted=new Color(.53f,.64f,.76f);
        private RectTransform nebula,farStars,nearStars,board;
        private readonly List<ArkNodeView> nodes=new List<ArkNodeView>();
        private TextMeshProUGUI memory,title,category,level,effect,cost,buttonText,feedback,requirement;
        private ArkConstellationGraphic detailGlow,detailIcon,connections;
        private Button purchase,previous;
        private ArkNodeDefinition selected;
        private Action onClose;
        private Vector2 pointer;
        private ArkMapViewport navigation;
        public string SelectedNodeId => selected?.id;

        public static ArkScreen Create(Transform parent)
        {
            var root=new GameObject("ArkScreen",typeof(RectTransform),typeof(Image),typeof(ArkScreen));
            root.transform.SetParent(parent,false);Stretch(root.GetComponent<RectTransform>());
            root.GetComponent<Image>().color=new Color(.012f,.020f,.045f,1f);
            var screen=root.GetComponent<ArkScreen>();screen.Build();root.SetActive(false);return screen;
        }
        public void Open(Action close){onClose=close;gameObject.SetActive(true);transform.SetAsLastSibling();BindActions();Refresh();if(selected==null && nodes.Count>0)Select(nodes[0].Definition.id);}
        public void Close(){gameObject.SetActive(false);if(onClose!=null)onClose();else FindObjectOfType<MainMenuUI>()?.ShowTitlePanel();}
        private void OnEnable(){ArkProgression.Changed+=Refresh;if(purchase!=null){BindActions();Refresh();}}
        private void OnDisable(){ArkProgression.Changed-=Refresh;}
        private void BindActions()
        {
            // Runtime listeners are not serialized across an Editor script reload.
            nodes.Clear();
            foreach(var view in GetComponentsInChildren<ArkNodeView>(true))
            {
                string id=view.name.Substring("Node_".Length);
                var definition=ArkProgressionConfig.Load().Find(id);if(definition==null)continue;
                view.Bind(definition);nodes.Add(view);
                var button=view.GetComponent<Button>();button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>Select(id));
            }
            purchase.onClick.RemoveAllListeners();purchase.onClick.AddListener(Buy);
            previous.onClick.RemoveAllListeners();previous.onClick.AddListener(SelectPrevious);
            var back=transform.Find("Back").GetComponent<Button>();back.onClick.RemoveAllListeners();back.onClick.AddListener(Close);
        }
        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape)){Close();return;}
            Vector2 target=new Vector2(Mathf.Clamp(Input.mousePosition.x/Mathf.Max(1,Screen.width)-.5f,-.5f,.5f),Mathf.Clamp(Input.mousePosition.y/Mathf.Max(1,Screen.height)-.5f,-.5f,.5f));
            pointer=Vector2.Lerp(pointer,target,1-Mathf.Exp(-4f*Time.unscaledDeltaTime));
            nebula.anchoredPosition=-pointer*18f;farStars.anchoredPosition=-pointer*32f;nearStars.anchoredPosition=-pointer*60f;navigation.SetParallax(-pointer*10f);
        }
        private void Build()
        {
            nebula=Layer("Nebula",ArkVisual.Nebula);farStars=Layer("DistantStars",ArkVisual.DistantStars);nearStars=Layer("NearStars",ArkVisual.NearStars);
            var back=ButtonAt(transform,"Back","‹  메인 화면",Close);Place(back.transform,.03f,.90f,.14f,.96f);
            Label(transform,"PageTitle","방주",52,White,.18f,.88f,.42f,.965f);
            memory=Label(transform,"Memory","",30,White,.76f,.905f,.96f,.965f,TextAlignmentOptions.MidlineRight);
            Panel(transform,"TopRule",new Color(.5f,.7f,.9f,.18f),.03f,.835f,.96f,.836f);
            var viewport=Rect("MapViewport",transform);Place(viewport,.035f,.125f,.705f,.775f);
            var background=viewport.gameObject.AddComponent<Image>();background.color=Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            board=Rect("Constellation",viewport);
            navigation=viewport.gameObject.AddComponent<ArkMapViewport>();navigation.Initialize(board);
            connections=Visual("Connections",board,ArkVisual.Constellation);Stretch(connections.rectTransform);
            Label(board,"OriginName","회귀의 중심",13,Muted,.395f,.42f,.565f,.46f,TextAlignmentOptions.Center);
            foreach(var node in ArkProgressionConfig.Load().nodes)
            {
                var root=Rect("Node_"+node.id,board);root.anchorMin=root.anchorMax=node.position;root.sizeDelta=Vector2.one*(node.major?78:58);
                var hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;
                var button=root.gameObject.AddComponent<Button>();button.targetGraphic=hit;
                var view=root.gameObject.AddComponent<ArkNodeView>();
                var star=Rect("Star",root);star.sizeDelta=Vector2.one*(node.major?74:48);view.star=star;
                view.glow=Visual("Glow",star,ArkVisual.Star);Stretch(view.glow.rectTransform);view.glow.major=node.major;view.glow.compact=true;
                view.icon=Visual("Icon",star,ArkVisual.Symbol);view.icon.symbol=node.kind;view.icon.rectTransform.sizeDelta=Vector2.one*(node.major?24:17);
                view.title=Label(root,"Name",node.title,node.major?15:14,White,.5f,node.major?0:1,.5f,node.major?0:1,TextAlignmentOptions.Center);
                view.title.rectTransform.sizeDelta=new Vector2(190,24);view.title.rectTransform.anchoredPosition=new Vector2(0,node.major?-8:14);
                view.caption=Label(root,"Level","",12,Muted,.5f,0,.5f,0,TextAlignmentOptions.Center);view.caption.rectTransform.sizeDelta=new Vector2(100,20);view.caption.rectTransform.anchoredPosition=new Vector2(0,node.major?-29:2);
                view.Bind(node);nodes.Add(view);string id=node.id;button.onClick.AddListener(()=>Select(id));
            }
            Panel(transform,"DetailSurface",new Color(.016f,.029f,.059f,.88f),.725f,.155f,.965f,.795f);
            Panel(transform,"DetailRule",new Color(.5f,.7f,.9f,.22f),.725f,.155f,.726f,.795f);
            var detail=Rect("Detail",transform);Place(detail,.75f,.17f,.94f,.775f);
            category=Label(detail,"Category","",14,Muted,0,.93f,1,1);
            detailGlow=Visual("SelectedStar",detail,ArkVisual.Star);Place(detailGlow.transform,.25f,.73f,.75f,.94f);
            detailIcon=Visual("SelectedIcon",detail,ArkVisual.Symbol);Place(detailIcon.transform,.44f,.795f,.56f,.875f);
            title=Label(detail,"Title","",27,White,0,.64f,1,.73f);
            level=Label(detail,"Level","",17,Muted,0,.59f,1,.65f);
            effect=Label(detail,"Effect","",16,ArkNodeView.Blue,0,.47f,1,.56f);effect.enableWordWrapping=true;
            requirement=Label(detail,"Requirement","",16,Muted,0,.28f,1,.40f);requirement.enableWordWrapping=true;requirement.alignment=TextAlignmentOptions.TopLeft;
            previous=ButtonAt(detail,"Previous","선행 별 보기  ‹",SelectPrevious);Place(previous.transform,0,.21f,1,.27f);
            cost=Label(detail,"Cost","",21,White,0,.08f,1,.14f);
            purchase=ButtonAt(detail,"Purchase","",Buy);Place(purchase.transform,0,0,1,.075f);buttonText=purchase.GetComponentInChildren<TextMeshProUGUI>();
            feedback=Label(detail,"Feedback","",14,Muted,0,-.065f,1,.005f,TextAlignmentOptions.Center);
        }
        public void Select(string id)
        {
            var definition=ArkProgressionConfig.Load().Find(id);if(definition==null)return;selected=definition;
            foreach(var view in nodes)view.Select(view.Definition==selected);
            feedback.text="";Refresh();
        }
        private void SelectPrevious()
        {
            if(selected?.prerequisites==null || selected.prerequisites.Length==0)return;
            ArkProgression.Data.PrerequisitesMet(ArkProgressionConfig.Load(),selected,out string missing);
            Select(missing??selected.prerequisites[0]);
            navigation.Focus(selected.position);
        }
        private void Buy(){if(selected==null)return;string message;if(!ArkProgression.TryPurchase(selected.id,out message))feedback.text=message;else feedback.text=selected.major?"새로운 가능성이 열렸습니다.":"기억을 새겼습니다.";}
        private void Refresh()
        {
            if(memory==null)return;
            memory.text=$"기억  {ArkProgression.Data.memory:N0}";
            foreach(var view in nodes)view.Refresh();
            if(selected==null)return;
            connections.selectedId=selected.id;connections.SetVerticesDirty();
            int rank=Mathf.Min(ArkProgression.Data.Level(selected.id),selected.MaxLevel);bool max=rank>=selected.MaxLevel;
            var config=ArkProgressionConfig.Load();var data=ArkProgression.Data;
            bool available=data.PrerequisitesMet(config,selected,out _);
            Color tint=max?(selected.major?ArkNodeView.Gold:ArkNodeView.Blue):White;
            detailGlow.major=selected.major;detailGlow.color=detailIcon.color=tint;detailIcon.symbol=selected.kind;detailIcon.SetVerticesDirty();detailGlow.SetVerticesDirty();
            category.text=selected.branch+(selected.major?"  /  주요 별":"");
            title.text=selected.title;level.text=selected.placeholder?"미공개 · 캐릭터 준비 중":$"Lv. {rank} / {selected.MaxLevel}"+(max?"  ·  완성":available?"  ·  강화 가능":"  ·  잠김");
            bool hasParent=selected.prerequisites!=null && selected.prerequisites.Length>0;
            previous.gameObject.SetActive(hasParent);
            requirement.text="";
            if(hasParent)foreach(string id in selected.prerequisites){var parent=config.Find(id);requirement.text+=$"{parent?.title??id}  {data.Level(id)} / {parent?.MaxLevel??0}\n";}
            requirement.color=available?Muted:new Color(.85f,.75f,.55f);
            int total=data.Amount(config,selected.kind);
            string effectName=EffectName(selected.kind);
            effect.text=selected.placeholder?"":selected.major?effectName+(max?" 해금 완료":" 해금"):max?$"{effectName}  +{total}":$"{effectName}  +{total} → +{total+selected.amountPerLevel}";
            int price=selected.CostAt(rank);
            cost.text=selected.placeholder?"아직 발견되지 않은 기억":max?"최대 단계 달성":$"필요한 기억   {price:N0}";
            purchase.interactable=!selected.placeholder&&!max&&available&&ArkProgression.Data.memory>=price;
            buttonText.text=selected.placeholder?"준비 중":max?"완성한 별":!available?"선행 별 완성 필요":ArkProgression.Data.memory<price?$"기억 {price-ArkProgression.Data.memory} 부족":selected.major?"기억으로 해금":"기억으로 강화";
        }
        private static string EffectName(ArkNodeKind kind)
        {
            switch(kind)
            {
                case ArkNodeKind.LeaderHealth:return "리더 최대 체력";
                case ArkNodeKind.LeaderMental:return "리더 최대 정신력";
                case ArkNodeKind.LeaderArmor:return "리더 방어력";
                case ArkNodeKind.LeaderMagicResist:return "리더 마법 저항력";
                case ArkNodeKind.LeaderAttack:return "리더 공격력";
                case ArkNodeKind.LeaderSpellPower:return "리더 주문력";
                case ArkNodeKind.ExtraDie:return "첫 턴 주사위";
                case ArkNodeKind.EnhanceDie:return "전투당 주사위 강화";
                case ArkNodeKind.Reroll:return "전투당 재굴림";
                case ArkNodeKind.StartingGold:return "시작 골드";
                case ArkNodeKind.VictoryMemory:return "승리당 기억";
                case ArkNodeKind.RelicSummon:return "유물 소환";
                case ArkNodeKind.TrainCustomization:return "열차 커스텀";
                case ArkNodeKind.Character:return "승무원";
                default:return "총 보너스";
            }
        }
        private RectTransform Layer(string name,ArkVisual type){var g=Visual(name,transform,type);Stretch(g.rectTransform);g.rectTransform.sizeDelta=new Vector2(160,160);return g.rectTransform;}
        private static RectTransform Rect(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return go.GetComponent<RectTransform>();}
        private static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        private static void Place(Transform t,float x0,float y0,float x1,float y1){var r=(RectTransform)t;r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;}
        private static Image Panel(Transform parent,string name,Color c,float x0,float y0,float x1,float y1){var r=Rect(name,parent);Place(r,x0,y0,x1,y1);var im=r.gameObject.AddComponent<Image>();im.color=c;im.raycastTarget=false;return im;}
        private static ArkConstellationGraphic Visual(string name,Transform parent,ArkVisual type){var r=Rect(name,parent);var g=r.gameObject.AddComponent<ArkConstellationGraphic>();g.visual=type;g.raycastTarget=false;return g;}
        private static TextMeshProUGUI Label(Transform parent,string name,string text,float size,Color c,float x0,float y0,float x1,float y1,TextAlignmentOptions align=TextAlignmentOptions.MidlineLeft)
        {
            var r=Rect(name,parent);Place(r,x0,y0,x1,y1);var label=r.gameObject.AddComponent<TextMeshProUGUI>();TMPFontManager.ApplyFont(label);label.text=text;label.color=c;label.fontSize=size;label.alignment=align;label.enableAutoSizing=true;label.fontSizeMin=size*.82f;label.fontSizeMax=size;label.enableWordWrapping=false;label.raycastTarget=false;return label;
        }
        private static Button ButtonAt(Transform parent,string name,string text,Action action)
        {
            var r=Rect(name,parent);var im=r.gameObject.AddComponent<Image>();im.color=new Color(.10f,.19f,.29f,.8f);var button=r.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>action());var colors=button.colors;colors.highlightedColor=new Color(1.2f,1.2f,1.2f);colors.disabledColor=new Color(.55f,.60f,.65f,.7f);button.colors=colors;Label(r,"Text",text,19,White,0,0,1,1,TextAlignmentOptions.Center);return button;
        }
    }
}
