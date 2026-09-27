using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TheLastArk.UI
{
    // Shared by the saved scene preview and the runtime character selection flow.
    public partial class MainMenuUI
    {
        [Header("Character selection icons")]
        [SerializeField] private Sprite[] crewStatIcons = new Sprite[6];
        [SerializeField] private Sprite crewCostCircle;
        private static readonly Color CrewInk = new Color(.025f, .045f, .065f, .985f);
        private static readonly Color CrewWhite = new Color(.96f, .98f, 1f, 1f);
        private static readonly Color CrewMuted = new Color(.72f, .80f, .85f, 1f);
        private static readonly Color CrewGold = new Color(.91f, .75f, .43f, 1f);
        private static readonly Color CrewCyan = new Color(.49f, .77f, .81f, 1f);

        private static void CrewRect(Transform target, float x0, float y0, float x1, float y1)
        {
            if (target == null) return;
            var rect = target as RectTransform;
            if (rect == null) return;
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static GameObject CrewChild(Transform parent, string name)
        {
            var found = parent.Find(name);
            if (found != null) return found.gameObject;
            var result = new GameObject(name, typeof(RectTransform));
            result.transform.SetParent(parent, false);
            result.layer = parent.gameObject.layer;
            return result;
        }

        private static UnityEngine.UI.Image CrewPanel(Transform parent, string name, Color color,
            float x0, float y0, float x1, float y1)
        {
            var go = CrewChild(parent, name);
            CrewRect(go.transform, x0, y0, x1, y1);
            var image = go.GetComponent<UnityEngine.UI.Image>();
            if (image == null) image = go.AddComponent<UnityEngine.UI.Image>();
            image.sprite = null;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private TextMeshProUGUI CrewLabel(Transform parent, string name, string text, float size,
            Color color, float x0, float y0, float x1, float y1,
            TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var go = CrewChild(parent, name);
            CrewRect(go.transform, x0, y0, x1, y1);
            var label = go.GetComponent<TextMeshProUGUI>();
            if (label == null) label = go.AddComponent<TextMeshProUGUI>();
            if (mainFont != null) label.font = mainFont;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.fontStyle = FontStyles.Normal;
            label.alignment = alignment;
            label.enableWordWrapping = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = size * .78f;
            label.fontSizeMax = size;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            return label;
        }

        private static void CrewSurface(Transform target, Color color)
        {
            if (target == null) return;
            var image = target.GetComponent<UnityEngine.UI.Image>();
            if (image != null) { image.sprite = null; image.color = color; image.raycastTarget = false; }
            var outline = target.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null) outline.enabled = false;
        }

        [ContextMenu("Style Character Selection")]
        public void ApplyCharacterSelectionPresentation()
        {
            if (characterSelectionPanel == null || characterListContainer == null) return;
            Transform root = characterSelectionPanel.transform;
            Transform roster = root.Find("CharacterGridPanel");
            Transform detail = root.Find("CharacterDetailPanel");
            if (roster == null || detail == null) return;

            var shade = CrewPanel(root, "CrewContentShade", new Color(.015f,.025f,.04f,.82f), .04f,.135f,.96f,.755f);
            int rosterIndex = roster.GetSiblingIndex();
            shade.transform.SetSiblingIndex(rosterIndex - (shade.transform.GetSiblingIndex() < rosterIndex ? 1 : 0));

            CrewRect(roster, .04f, .135f, .335f, .755f);
            CrewSurface(roster, CrewInk);
            CrewPanel(roster, "HeaderRule", new Color(.5f,.65f,.72f,.22f), .04f,.849f,.96f,.851f);
            CrewLabel(roster, "GridHeader", "승무원 선택", 27, CrewWhite, .05f,.91f,.74f,.98f).fontStyle = FontStyles.Bold;
            CrewLabel(roster, "RosterCaption", "이번 여정을 이끌 리더를 선택하세요", 18, CrewMuted, .05f,.859f,.95f,.914f);
            var rosterIndexLabel = roster.Find("RosterIndex");
            if (rosterIndexLabel != null) rosterIndexLabel.gameObject.SetActive(false);
            CrewRect(characterListContainer, .045f,.04f,.955f,.825f);
            var grid = characterListContainer.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (grid != null) grid.enabled = true;
            var portraitGrid = characterListContainer.GetComponent<CrewPortraitGridLayout>();
            if (portraitGrid == null) portraitGrid = characterListContainer.gameObject.AddComponent<CrewPortraitGridLayout>();
            portraitGrid.ApplyLayout();

            var crew = new List<CharacterData>();
            foreach (var data in Resources.LoadAll<CharacterData>("Characters"))
                if (data != null && !data.isEnemy && characterListContainer.Find("Card_" + data.DataId) != null) crew.Add(data);
            for (int i = 0; i < crew.Count; i++)
            {
                var data = crew[i];
                var card = characterListContainer.Find("Card_" + data.DataId);
                StyleCrewCard(card, data, data == selectedCharacterData);
            }

            // Full-height hero illustration in the center; a compact dossier to its right.
            CrewRect(detail, .352f,.135f,.96f,.755f);
            CrewSurface(detail, Color.clear);
            var dossier = CrewPanel(detail, "DossierSurface", CrewInk, .54f,0,1,1);
            dossier.transform.SetAsFirstSibling();
            var portrait = detail.Find("DetailPortrait");
            CrewRect(portrait, 0,.015f,.52f,.965f);
            CrewSurface(portrait, Color.clear);
            if (detailPortraitImageComponent != null)
            {
                CrewRect(detailPortraitImageComponent.transform, -.04f,.015f,1.04f,1.035f);
                detailPortraitImageComponent.color = detailPortraitImageComponent.sprite != null ? Color.white : Color.clear;
            }
            var emptyHero = CrewLabel(detail,"HeroPlaceholder","초상화 준비 중",21,CrewMuted,.04f,.38f,.50f,.58f,TextAlignmentOptions.Center);
            emptyHero.gameObject.SetActive(detailPortraitImageComponent == null || detailPortraitImageComponent.sprite == null);
            var heroCaption = detail.Find("HeroCaption");
            if (heroCaption != null) heroCaption.gameObject.SetActive(false);
            CrewPanel(detail, "HeroRule", new Color(.91f,.75f,.43f,.5f), .035f,.055f,.50f,.057f);
            CrewLabel(detail, "HeroName", selectedCharacterData != null ? selectedCharacterData.DisplayName : "", 19,
                CrewWhite, .035f,0,.5f,.05f, TextAlignmentOptions.Center);

            var info = detail.Find("InfoContainer");
            CrewRect(info, .575f,.36f,.965f,.965f);
            CrewLabel(info, "DossierCaption", "선택한 리더", 16, CrewMuted, 0,.94f,1,1);
            if (detailNameTextComponent != null)
            {
                CrewRect(detailNameTextComponent.transform, 0,.755f,1,.95f);
                detailNameTextComponent.fontSize = 37;
                detailNameTextComponent.enableAutoSizing = true;
                detailNameTextComponent.fontSizeMin = 25;
                detailNameTextComponent.fontSizeMax = 37;
                detailNameTextComponent.color = CrewWhite;
                detailNameTextComponent.alignment = TextAlignmentOptions.MidlineLeft;
            }
            if (detailJobTextComponent != null)
            {
                CrewRect(detailJobTextComponent.transform, 0,.675f,1,.78f);
                detailJobTextComponent.text = selectedCharacterData != null ? selectedCharacterData.jobName : "";
                detailJobTextComponent.fontSize = 18;
                detailJobTextComponent.color = CrewCyan;
                detailJobTextComponent.alignment = TextAlignmentOptions.MidlineLeft;
            }
            if (detailStatsTextComponent != null) detailStatsTextComponent.gameObject.SetActive(false);
            CrewPanel(info, "InfoRule", new Color(.5f,.65f,.72f,.22f),0,.635f,1,.638f);
            CrewLabel(info, "StatsHeading", "기본 능력치", 18, CrewMuted, 0,.545f,1,.635f);
            StyleCrewStats(info);

            Transform syn = detail.Find("SynergiesPanel");
            CrewRect(syn, .575f,.235f,.965f,.355f);
            var oldHeading = syn.Find("SynergiesHeader");
            if (oldHeading != null) oldHeading.gameObject.SetActive(false);
            CrewLabel(syn, "SectionLabel", "보유 시너지", 18, CrewMuted,0,.61f,1,1);
            CrewRect(detailSynergiesContainer, 0,.02f,1,.58f);
            if (detailSynergiesContainer != null)
            {
                foreach (Transform badge in detailSynergiesContainer)
                {
                    // Destroy() is deferred in play mode; ignore slots removed this frame.
                    if (!badge.gameObject.activeSelf) continue;
                    var rect = badge as RectTransform;
                    if (rect != null) rect.sizeDelta = new Vector2(125,30);
                    var bg = badge.GetComponent<UnityEngine.UI.Image>();
                    if (bg != null) bg.color = new Color(.10f,.19f,.23f,.96f);
                    var edge = badge.GetComponent<UnityEngine.UI.Outline>();
                    if (edge != null) { edge.effectColor = new Color(.38f,.60f,.66f,.4f); edge.effectDistance = new Vector2(1,-1); }
                    var text = badge.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null) { text.fontSize = 14; text.color = CrewCyan; }
                }
            }
            StyleCrewSkills(detail);
            StyleCrewActions(root);
        }

        private void StyleCrewCard(Transform card, CharacterData data, bool selected)
        {
            var layout = card.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layout != null) layout.enabled = false;
            var image = card.GetComponent<UnityEngine.UI.Image>();
            image.sprite = null;
            image.color = selected ? new Color(.17f,.20f,.20f,.96f) : new Color(.065f,.10f,.13f,.96f);
            image.raycastTarget = true;
            var edge = card.GetComponent<UnityEngine.UI.Outline>();
            if (edge != null)
            {
                edge.enabled = true;
                edge.effectDistance = Vector2.one;
                edge.effectColor = selected ? CrewGold : new Color(.33f,.46f,.53f,.32f);
            }
            var button = card.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.targetGraphic = image;
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.3f,1.3f,1.3f,1);
                colors.pressedColor = new Color(.8f,.85f,.9f,1);
                colors.selectedColor = Color.white;
                colors.fadeDuration = .12f;
                button.colors = colors;
            }
            var portrait = card.Find("Portrait");
            CrewRect(portrait,0,0,1,1);
            var portraitImage = portrait.GetComponent<UnityEngine.UI.Image>();
            portraitImage.color = portraitImage.sprite != null ? Color.white : Color.clear;
            portraitImage.preserveAspect = false;
            var missing = CrewLabel(card,"PortraitPlaceholder","준비 중",18,CrewMuted,.08f,.25f,.92f,.9f,TextAlignmentOptions.Center);
            missing.enableWordWrapping = true;
            missing.gameObject.SetActive(portraitImage.sprite == null);
            CrewPanel(card,"NameShade",new Color(.015f,.025f,.04f,.94f),0,0,1,.23f).transform.SetAsLastSibling();
            var name=CrewLabel(card,"NameText",data.DisplayName,18,selected?CrewGold:CrewWhite,.035f,.015f,.965f,.225f,TextAlignmentOptions.Center);
            name.transform.SetAsLastSibling();
            name.enableWordWrapping = true;
            name.fontSizeMin=13;
            CrewPanel(card,"SelectionBar",selected?CrewGold:new Color(.34f,.49f,.55f,.3f),0,0,1,.023f).transform.SetAsLastSibling();
            var number=card.Find("CrewNumber"); if(number!=null)number.gameObject.SetActive(false);
            var marker=CrewLabel(card,"SelectedMarker","선택",13,CrewGold,.63f,.79f,.96f,.98f,TextAlignmentOptions.MidlineRight);
            marker.gameObject.SetActive(selected);
            var markerBg=CrewPanel(card,"SelectedMarkerBg",new Color(.025f,.04f,.06f,.95f),.61f,.79f,1,1);
            markerBg.gameObject.SetActive(selected);
            marker.transform.SetAsLastSibling();
        }

        private void StyleCrewStats(Transform info)
        {
            if (selectedCharacterData == null) return;
            var d = selectedCharacterData;
            string[] names = { "체력", "정신력", "공격력", "주문력", "방어력", "마법 저항" };
            float[] values = { d.maxHp,d.maxMental,d.baseAttack,d.spellPower,d.armor,d.magicResist };
            for (int i=0;i<6;i++)
            {
                float x=(i%2)*.515f;
                float y=.36f-(i/2)*.18f;
                var tile=CrewPanel(info,"Stat"+i,new Color(.075f,.12f,.16f,1),x,y,x+.485f,y+.16f);
                var oldLabel=tile.transform.Find("Label"); if(oldLabel!=null)oldLabel.gameObject.SetActive(false);
                // Source icons have transparent padding; a generous image box keeps the glyph legible.
                var icon=CrewPanel(tile.transform,"Icon",Color.white,.00f,-.27f,.56f,1.27f);
                icon.sprite=crewStatIcons!=null && i<crewStatIcons.Length?crewStatIcons[i]:null;
                icon.preserveAspect=true;
                if(icon.sprite==null) icon.color=Color.clear;
                CrewLabel(tile.transform,"Value",values[i].ToString("0.##"),30,CrewWhite,.52f,.05f,.9f,.95f,TextAlignmentOptions.MidlineRight);
                tile.raycastTarget=true;
                var hover=tile.GetComponent<UIHoverTrigger>(); if(hover==null)hover=tile.gameObject.AddComponent<UIHoverTrigger>();
                hover.title=names[i]; hover.category="기본 능력치"; hover.description=names[i]+"  "+values[i].ToString("0.##");
                hover.onHoverEnter=ShowHoverTooltip;hover.onHoverExit=HideHoverTooltip;
            }
        }

        private void StyleCrewSkills(Transform detail)
        {
            if (detailSkillsContainer == null) return;
            CrewLabel(detail,"SkillsHeading","전투 스킬",18,CrewMuted,.575f,.175f,.80f,.225f);
            var skillsHint=detail.Find("SkillsHint"); if(skillsHint!=null)skillsHint.gameObject.SetActive(false);
            CrewRect(detailSkillsContainer,.575f,.037f,.965f,.16f);
            var passive=detailSkillsContainer.Find("PassiveRow");
            var active=detailSkillsContainer.Find("ActiveRow");
            CrewRect(passive,0,0,.18f,1);
            CrewRect(active,.22f,0,1,1);
            foreach(var row in new[]{passive,active})
            {
                if(row==null) continue;
                var layout=row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                if(layout!=null) layout.enabled=false;
                var slots=new List<Transform>();
                foreach(Transform slot in row) if(slot.gameObject.activeSelf) slots.Add(slot);
                for(int i=0;i<slots.Count;i++)
                {
                    var slot=slots[i];
                    float width=1f/Mathf.Max(row==passive?1:4,slots.Count);
                    CrewRect(slot,i*width,.05f,(i+1)*width-.035f,.95f);
                    var bg=slot.GetComponent<UnityEngine.UI.Image>();
                    if(bg!=null) bg.color=Color.clear;
                    var edge=slot.GetComponent<UnityEngine.UI.Outline>();
                    if(edge!=null) edge.enabled=false;
                    var square=slot.GetComponent<UnityEngine.UI.AspectRatioFitter>();
                    if(square==null)square=slot.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
                    square.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.WidthControlsHeight; square.aspectRatio=1;
                    var icon=slot.Find("Icon");
                    CrewRect(icon,0,0,1,1);
                    if(icon!=null)icon.GetComponent<UnityEngine.UI.Image>().preserveAspect=false;
                    var cost=slot.Find("CostBadge");
                    if(cost!=null)
                    {
                        cost.gameObject.SetActive(row!=passive);
                        var costRect=(RectTransform)cost;
                        costRect.anchorMin=costRect.anchorMax=new Vector2(0,1);
                        costRect.pivot=new Vector2(.5f,.5f);costRect.sizeDelta=new Vector2(32,32);
                        costRect.anchoredPosition=new Vector2(9,-9);
                        var costImage=cost.GetComponent<UnityEngine.UI.Image>();costImage.sprite=crewCostCircle;
                        costImage.type=UnityEngine.UI.Image.Type.Simple;costImage.color=new Color(.025f,.34f,.83f,1);
                        costImage.preserveAspect=true;
                        var costEdge=cost.GetComponent<UnityEngine.UI.Outline>();if(costEdge!=null)costEdge.enabled=false;
                        var number=cost.GetComponentInChildren<TextMeshProUGUI>(true);
                        if(number!=null){number.color=Color.white;number.fontSize=20;number.alignment=TextAlignmentOptions.Center;}
                        cost.SetAsLastSibling();
                    }
                    if(row==passive)CrewLabel(slot,"PassiveLabel","패시브",12,CrewMuted,0,-.32f,1,-.02f,TextAlignmentOptions.Center);
                }
            }
        }

        private void StyleCrewActions(Transform root)
        {
            StyleCrewAction(root.Find("BackButton"),"메인 화면",new Color(.055f,.10f,.14f,.96f),CrewWhite,.04f,.035f,.19f,.102f);
            StyleCrewAction(root.Find("RelicGachaButton"),"특전 유물 소환",new Color(.10f,.16f,.20f,.98f),CrewWhite,.545f,.035f,.73f,.102f);
            StyleCrewAction(root.Find("ConfirmStartButton"),"여정 시작  ›",CrewGold,new Color(.07f,.09f,.105f,1),.745f,.035f,.96f,.102f);
            var relic=root.Find("DrawnRelicSlot");
            CrewRect(relic,.493f,.035f,.535f,.102f);
            CrewSurface(relic,new Color(.055f,.10f,.14f,.96f));
            if(relic!=null)
            {
                var relicBg=relic.GetComponent<UnityEngine.UI.Image>();
                if(relicBg!=null) relicBg.raycastTarget=true;
                var empty=CrewLabel(relic,"EmptyRelic","?",25,CrewMuted,.1f,.1f,.9f,.9f,TextAlignmentOptions.Center);
                empty.gameObject.SetActive(selectedStartingRelic==null);
                if(relicPreviewImageComponent!=null)
                {
                    if(relicPreviewImageComponent.sprite==null) relicPreviewImageComponent.color=Color.clear;
                    relicPreviewImageComponent.transform.SetAsLastSibling();
                }
            }
            CrewLabel(root,"DepartureCaption","출발 준비",19,CrewGold,.365f,.035f,.48f,.102f);
            var departureHint=root.Find("DepartureHint");if(departureHint!=null)departureHint.gameObject.SetActive(false);
        }

        private void StyleCrewAction(Transform target,string title,Color background,Color foreground,float x0,float y0,float x1,float y1)
        {
            if(target==null) return;
            CrewRect(target,x0,y0,x1,y1);
            CrewSurface(target,background);
            var image=target.GetComponent<UnityEngine.UI.Image>();
            image.raycastTarget=true;
            var button=target.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic=image;
            var colors=button.colors;
            colors.normalColor=Color.white;colors.highlightedColor=new Color(1.15f,1.15f,1.15f,1);
            colors.pressedColor=new Color(.8f,.8f,.8f,1);colors.selectedColor=Color.white;button.colors=colors;
            CrewLabel(target,"Text",title,23,foreground,.06f,.08f,.94f,.92f,TextAlignmentOptions.Center).fontStyle=FontStyles.Bold;
        }
    }
}
