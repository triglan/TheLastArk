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
        [System.Serializable]
        private sealed class CrewBackgroundFrame
        {
            public CharacterData character;
            public Sprite illustration;
            [Tooltip("배경 그림에서 얼굴 중심. 왼쪽 아래 (0,0), 오른쪽 위 (1,1).")]
            public Vector2 faceFocus = new Vector2(.5f, .75f);
            [Range(1f, 1.5f)] public float zoom = 1f;
        }
        [Header("Character selection background framing")]
        [SerializeField] private List<CrewBackgroundFrame> crewBackgroundFrames = new List<CrewBackgroundFrame>();
        private static readonly Color CrewInk = new Color(.025f, .045f, .065f, .91f);
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

            var shade = root.Find("CrewContentShade");
            if (shade != null) shade.gameObject.SetActive(false);
            StyleCrewTrain(root);

            CrewRect(roster, .62f, .39f, .96f, .765f);
            CrewSurface(roster, CrewInk);
            CrewPanel(roster, "HeaderRule", new Color(.5f,.65f,.72f,.22f), .055f,.875f,.945f,.879f);
            CrewLabel(roster, "GridHeader", "승무원 선택", 25, CrewWhite, .055f,.895f,.47f,.99f).fontStyle = FontStyles.Bold;
            CrewLabel(roster, "RosterCaption", "리더 1명 선택", 15, CrewMuted, .47f,.895f,.73f,.99f,TextAlignmentOptions.MidlineRight);
            var portraitGrid = ConfigureCrewRoster(roster);

            var crew = new List<CharacterData>();
            foreach (var data in Resources.LoadAll<CharacterData>("Characters"))
                if (data != null && !data.isEnemy && characterListContainer.Find("Card_" + data.DataId) != null) crew.Add(data);
            for (int i = 0; i < crew.Count; i++)
            {
                var data = crew[i];
                var card = characterListContainer.Find("Card_" + data.DataId);
                StyleCrewCard(card, data, data == selectedCharacterData);
            }
            if (selectedCharacterData != null)
                portraitGrid.EnsureVisible(characterListContainer.Find("Card_" + selectedCharacterData.DataId) as RectTransform);

            // The dedicated background is the hero image; the dossier occupies only the right edge.
            CrewRect(detail, .62f,.13f,.96f,.39f);
            CrewSurface(detail, Color.clear);
            var dossier = CrewPanel(detail, "DossierSurface", CrewInk, 0,0,1,1);
            dossier.transform.SetAsFirstSibling();
            foreach (string name in new[] { "DetailPortrait", "HeroPlaceholder", "HeroCaption", "HeroRule", "HeroName" })
            { var old = detail.Find(name); if (old != null) old.gameObject.SetActive(false); }
            if (detailPortraitImageComponent != null) detailPortraitImageComponent.gameObject.SetActive(false);

            var info = detail.Find("InfoContainer");
            CrewRect(info, .055f,.60f,.945f,.98f);
            foreach (string oldName in new[] { "DossierCaption", "InfoRule", "StatsHeading" })
            { var old = info.Find(oldName); if (old != null) old.gameObject.SetActive(false); }
            if (detailStatsTextComponent != null) detailStatsTextComponent.gameObject.SetActive(false);
            StyleCrewIdentity(detail, info);
            StyleCrewStats(info);
            StyleCrewSkills(detail);
            StyleCrewActions(root);
            StyleCrewBackground(root);
            if (!Application.isPlaying) ApplyTrainCustomizationState(false);
            RefreshArkFeatureVisibility();
        }

        private CrewPortraitGridLayout ConfigureCrewRoster(Transform roster)
        {
            var viewport = CrewChild(roster, "RosterViewport").GetComponent<RectTransform>();
            CrewRect(viewport, .055f,.025f,.945f,.85f);
            if (viewport.GetComponent<UnityEngine.UI.RectMask2D>() == null) viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var hit = viewport.GetComponent<UnityEngine.UI.Image>();
            if (hit == null) hit = viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
            hit.color = Color.clear; hit.raycastTarget = true;
            if (characterListContainer.parent != viewport) characterListContainer.SetParent(viewport, false);
            var scroll = viewport.GetComponent<UnityEngine.UI.ScrollRect>();
            if (scroll == null) scroll = viewport.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.viewport = viewport; scroll.content = characterListContainer as RectTransform;
            scroll.horizontal = true; scroll.vertical = false;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35; scroll.inertia = true;
            // Navigation uses complete eight-card pages, so dragging cannot leave half a row visible.
            scroll.enabled = false;
            var grid = characterListContainer.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (grid != null) grid.enabled = true;
            var fitter = characterListContainer.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            var portraitGrid = characterListContainer.GetComponent<CrewPortraitGridLayout>();
            if (portraitGrid == null) portraitGrid = characterListContainer.gameObject.AddComponent<CrewPortraitGridLayout>();
            var previous = CrewRosterArrow(roster, "PreviousCrew", "‹", .755f, .815f);
            var next = CrewRosterArrow(roster, "NextCrew", "›", .89f, .95f);
            var pageLabel = CrewLabel(roster, "RosterIndex", "", 14, CrewMuted, .815f,.895f,.89f,.99f,TextAlignmentOptions.Center);
            pageLabel.gameObject.SetActive(true);
            portraitGrid.Configure(viewport, scroll, previous, next, pageLabel);
            return portraitGrid;
        }

        private UnityEngine.UI.Button CrewRosterArrow(Transform roster, string name, string label, float x0, float x1)
        {
            var image = CrewPanel(roster, name, new Color(.10f,.17f,.20f,1), x0,.895f,x1,.99f);
            image.raycastTarget = true;
            var button = image.GetComponent<UnityEngine.UI.Button>();
            if (button == null) button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            CrewLabel(image.transform,"Text",label,30,CrewWhite,0,0,1,1,TextAlignmentOptions.Center);
            return button;
        }

        private CrewBackgroundFrame FindCrewBackgroundFrame(CharacterData data)
        {
            return crewBackgroundFrames.Find(frame => frame != null && frame.character == data);
        }

        private void StyleCrewBackground(Transform root)
        {
            var layer = root.Find("CharacterBackgroundLayer");
            if (layer == null) return;
            // Reserve real space for the train and footer instead of drawing them over a face.
            CrewRect(layer, 0,.10f,.65f,.795f);
            if (layer.GetComponent<UnityEngine.UI.RectMask2D>() == null)
                layer.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            layer.GetComponent<UnityEngine.UI.RectMask2D>().softness = new Vector2Int(120,50);
            var backdropObject = CrewChild(root, "CrewBlurredBackdrop");
            CrewRect(backdropObject.transform, 0,0,1,1);
            backdropObject.transform.SetAsFirstSibling();
            var backdrop = backdropObject.GetComponent<CrewSelectionBackdrop>();
            if (backdrop == null) backdrop = backdropObject.AddComponent<CrewSelectionBackdrop>();
            backdrop.SetIllustration(characterBackgroundImageComponent != null ? characterBackgroundImageComponent.sprite : null);
            var vignette = layer.Find("VignetteOverlay")?.GetComponent<UnityEngine.UI.Image>();
            if (vignette != null) { vignette.color = new Color(.025f,.04f,.06f,.14f); vignette.raycastTarget = false; }
            if (characterAliveEffect != null)
            {
                // Keep the illustration alive without pushing a high-placed face beyond the frame.
                characterAliveEffect.parallaxStrength = new Vector2(16,6);
                characterAliveEffect.floatAmplitude = 2;
                characterAliveEffect.swayAmplitude = 3;
                characterAliveEffect.breatheScaleAmount = .006f;
                characterAliveEffect.tiltAngle = .08f;
                var frame = FindCrewBackgroundFrame(selectedCharacterData);
                characterAliveEffect.SetFraming(frame != null ? frame.faceFocus : new Vector2(.5f,.75f), frame != null ? frame.zoom : 1f);
            }
        }

        private void StyleCrewTrain(Transform root)
        {
            var header = root.Find("TrainCustomizationHeader");
            if (header == null) return;
            header.gameObject.SetActive(true);
            CrewRect(header,.04f,.785f,.96f,.985f);
            CrewSurface(header,CrewInk);
            var title = header.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
            CrewRect(title != null ? title.transform : null,.015f,.82f,.60f,.98f);
            if (title != null) { title.text = "방주 열차"; title.fontSize = 22; title.color = CrewWhite; }
            var hint = header.Find("TrainHint");
            CrewRect(hint,.61f,.82f,.985f,.98f);
            CrewRect(header.Find("TrackLine"),.015f,.035f,.985f,.045f);
            var train = header.Find("TrainImage");
            CrewRect(train,.015f,.02f,.985f,.82f);
            if (train != null) foreach (Transform slot in train)
            {
                var trainArt = slot.GetComponent<UnityEngine.UI.Image>();
                if (trainArt != null) trainArt.preserveAspect = true;
                if (!slot.name.StartsWith("CarSlot_")) continue;
                var name = slot.Find("Text")?.GetComponent<TextMeshProUGUI>();
                var status = slot.Find("CarStatus")?.GetComponent<TextMeshProUGUI>();
                if (name != null)
                {
                    CrewRect(name.transform,.025f,.25f,.975f,.47f);
                    name.fontSize = 20; name.enableAutoSizing = true; name.fontSizeMin = 15; name.fontSizeMax = 20;
                }
                if (status != null)
                {
                    CrewRect(status.transform,.025f,.08f,.975f,.24f);
                    status.fontSize = 15; status.enableAutoSizing = true; status.fontSizeMin = 12; status.fontSizeMax = 15;
                }
            }
        }

        public void ApplyTrainCustomizationState(bool unlocked)
        {
            if (characterSelectionPanel == null) return;
            var header = characterSelectionPanel.transform.Find("TrainCustomizationHeader");
            if (header == null) return;
            header.gameObject.SetActive(true);
            var hint = header.Find("TrainHint")?.GetComponent<TextMeshProUGUI>();
            if (hint != null)
            {
                hint.text = unlocked ? "각 칸을 선택하여 커스텀" : "기차 커스텀 · 방주에서 해금";
                hint.fontSize = 16; hint.color = unlocked ? CrewCyan : CrewMuted;
            }
            var train = header.Find("TrainImage");
            if (train == null) return;
            Sprite lockSprite = Resources.Load<Sprite>("Icons/lock");
            foreach (Transform slot in train)
            {
                // Only existing customizable slots are locked; the locomotive keeps its original art/color.
                if (!slot.name.StartsWith("CarSlot_") || slot.GetComponent<UnityEngine.UI.Button>() == null) continue;
                var state = slot.GetComponent<CrewTrainSlotLock>();
                if (state == null) state = slot.gameObject.AddComponent<CrewTrainSlotLock>();
                state.Apply(unlocked, lockSprite);
                var hover = slot.GetComponent<UIHoverTrigger>();
                if (hover == null) hover = slot.gameObject.AddComponent<UIHoverTrigger>();
                hover.title = "기차 커스텀"; hover.category = unlocked ? "해금 완료" : "잠김";
                hover.description = unlocked ? "이 칸을 선택해 커스텀합니다." : "방주에서 기차 커스텀을 해금하면 사용할 수 있습니다.";
                hover.onHoverEnter = ShowHoverTooltip; hover.onHoverExit = HideHoverTooltip;
            }
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
            CrewRect(portrait,0,.16f,1,1);
            var portraitImage = portrait.GetComponent<UnityEngine.UI.Image>();
            portraitImage.sprite = data.PortraitSprite;
            portraitImage.color = portraitImage.sprite != null ? Color.white : Color.clear;
            portraitImage.preserveAspect = true;
            var missing = CrewLabel(card,"PortraitPlaceholder","준비 중",18,CrewMuted,.08f,.25f,.92f,.9f,TextAlignmentOptions.Center);
            missing.enableWordWrapping = true;
            missing.gameObject.SetActive(portraitImage.sprite == null);
            CrewPanel(card,"NameShade",new Color(.015f,.025f,.04f,.94f),0,0,1,.16f).transform.SetAsLastSibling();
            var name=CrewLabel(card,"NameText",data.DisplayName,16,selected?CrewGold:CrewWhite,.035f,.005f,.965f,.155f,TextAlignmentOptions.Center);
            name.transform.SetAsLastSibling();
            name.enableWordWrapping = false;
            name.fontSizeMin=11;
            CrewPanel(card,"SelectionBar",selected?CrewGold:new Color(.34f,.49f,.55f,.3f),0,0,1,.023f).transform.SetAsLastSibling();
            var number=card.Find("CrewNumber"); if(number!=null)number.gameObject.SetActive(false);
            var marker=CrewLabel(card,"SelectedMarker","선택",13,CrewGold,.63f,.79f,.96f,.98f,TextAlignmentOptions.MidlineRight);
            marker.gameObject.SetActive(selected);
            var markerBg=CrewPanel(card,"SelectedMarkerBg",new Color(.025f,.04f,.06f,.95f),.61f,.79f,1,1);
            markerBg.gameObject.SetActive(selected);
            marker.transform.SetAsLastSibling();
        }

        private void StyleCrewIdentity(Transform detail, Transform info)
        {
            var syn = detail.Find("SynergiesPanel");
            if (syn == null || detailSynergiesContainer == null) return;
            foreach (string oldName in new[] { "SynergiesHeader", "SectionLabel" })
            { var old = syn.Find(oldName); if (old != null) old.gameObject.SetActive(false); }
            int count = selectedCharacterData != null && selectedCharacterData.synergies != null ? selectedCharacterData.synergies.Count : 0;
            float infoWidth = Mathf.Max(1, ((RectTransform)info).rect.width);
            const float side = 40, gap = 7;
            float badgeWidth = count * side + Mathf.Max(0,count-1)*gap;
            float badgeFraction = Mathf.Min(.4f, badgeWidth/infoWidth);
            CrewRect(syn,.945f-.89f*badgeFraction,.79f,.945f,.98f);
            CrewRect(detailSynergiesContainer,0,0,1,1);
            var layout = detailSynergiesContainer.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            if (layout != null) { layout.spacing=gap; layout.childAlignment=TextAnchor.MiddleRight; }
            var customIcons = Resources.LoadAll<TheLastArk.Data.SynergyData>("Synergies");
            foreach (Transform badge in detailSynergiesContainer)
            {
                if (!badge.gameObject.activeSelf) continue;
                ((RectTransform)badge).sizeDelta=Vector2.one*side;
                var element=badge.GetComponent<UnityEngine.UI.LayoutElement>();
                if(element!=null){element.minWidth=element.preferredWidth=side;element.minHeight=element.preferredHeight=side;}
                var bg=badge.GetComponent<UnityEngine.UI.Image>();
                if(bg!=null) bg.color=new Color(.10f,.19f,.23f,.9f);
                var edge=badge.GetComponent<UnityEngine.UI.Outline>();
                if(edge!=null){edge.effectColor=new Color(.49f,.77f,.81f,.45f);edge.effectDistance=new Vector2(1,-1);}
                var oldText=badge.Find("Text");if(oldText!=null)oldText.gameObject.SetActive(false);
                TheLastArk.Data.SynergyType type;
                if(!System.Enum.TryParse(badge.name.Substring("Synergy_".Length),out type))continue;
                var iconObject=CrewChild(badge,"SynergyIcon");CrewRect(iconObject.transform,.12f,.12f,.88f,.88f);
                if(iconObject.GetComponent<CanvasRenderer>()==null)iconObject.AddComponent<CanvasRenderer>();
                var iconData=System.Array.Find(customIcons,entry=>entry.synergyType==type&&entry.icon!=null);
                if(iconData!=null)
                {
                    var image=iconObject.GetComponent<UnityEngine.UI.Image>();if(image==null)image=iconObject.AddComponent<UnityEngine.UI.Image>();
                    image.sprite=iconData.icon;image.preserveAspect=true;image.raycastTarget=false;
                }
                else
                {
                    var glyph=iconObject.GetComponent<CrewSynergyGlyph>();if(glyph==null)glyph=iconObject.AddComponent<CrewSynergyGlyph>();
                    glyph.color=CrewCyan;glyph.raycastTarget=false;glyph.SetSynergy(type);
                }
            }
            float available=infoWidth-badgeWidth-16;
            float jobWidth=Mathf.Min(120,available*.36f);
            if(detailJobTextComponent!=null)
            {
                detailJobTextComponent.text=selectedCharacterData!=null?selectedCharacterData.JobDisplayName:"";
                detailJobTextComponent.fontSize=17;detailJobTextComponent.enableAutoSizing=true;
                detailJobTextComponent.fontSizeMin=12;detailJobTextComponent.fontSizeMax=17;
                detailJobTextComponent.color=new Color(.85f,.91f,.96f,.44f);
                detailJobTextComponent.enableWordWrapping=false;detailJobTextComponent.alignment=TextAlignmentOptions.MidlineLeft;
            }
            float nameWidth=available-jobWidth-10;
            if(detailNameTextComponent!=null)
            {
                detailNameTextComponent.fontSize=28;detailNameTextComponent.enableAutoSizing=false;
                nameWidth=Mathf.Min(nameWidth,detailNameTextComponent.GetPreferredValues(detailNameTextComponent.text).x+4);
                detailNameTextComponent.enableAutoSizing=true;detailNameTextComponent.fontSizeMin=20;detailNameTextComponent.fontSizeMax=28;
                detailNameTextComponent.enableWordWrapping=false;detailNameTextComponent.color=CrewWhite;
                detailNameTextComponent.alignment=TextAlignmentOptions.MidlineLeft;
                CrewRect(detailNameTextComponent.transform,0,.50f,nameWidth/infoWidth,1);
            }
            if(detailJobTextComponent!=null)CrewRect(detailJobTextComponent.transform,(nameWidth+10)/infoWidth,.50f,available/infoWidth,1);
        }

        private void StyleCrewStats(Transform info)
        {
            if (selectedCharacterData == null) return;
            var d = selectedCharacterData;
            string[] names = { "체력", "정신력", "공격력", "주문력", "방어력", "마법 저항" };
            float[] values = { d.maxHp,d.maxMental,d.baseAttack,d.spellPower,d.armor,d.magicResist };
            for (int i=0;i<6;i++)
            {
                float x=i/6f;
                var tile=CrewPanel(info,"Stat"+i,new Color(.075f,.12f,.16f,.55f),x,0,x+1f/6f-.008f,.38f);
                var oldLabel=tile.transform.Find("Label"); if(oldLabel!=null)oldLabel.gameObject.SetActive(false);
                // Source icons have transparent padding; a generous image box keeps the glyph legible.
                var icon=CrewPanel(tile.transform,"Icon",Color.white,.00f,-.27f,.56f,1.27f);
                icon.sprite=crewStatIcons!=null && i<crewStatIcons.Length?crewStatIcons[i]:null;
                icon.preserveAspect=true;
                if(icon.sprite==null) icon.color=Color.clear;
                CrewLabel(tile.transform,"Value",values[i].ToString("0.##"),24,CrewWhite,.52f,.05f,.9f,.95f,TextAlignmentOptions.MidlineRight);
                tile.raycastTarget=true;
                var hover=tile.GetComponent<UIHoverTrigger>(); if(hover==null)hover=tile.gameObject.AddComponent<UIHoverTrigger>();
                hover.title=names[i]; hover.category="기본 능력치"; hover.description=names[i]+"  "+values[i].ToString("0.##");
                hover.onHoverEnter=ShowHoverTooltip;hover.onHoverExit=HideHoverTooltip;
            }
        }

        private void StyleCrewSkills(Transform detail)
        {
            if (detailSkillsContainer == null) return;
            CrewLabel(detail,"SkillsHeading","전투 스킬",16,CrewMuted,.055f,.51f,.945f,.58f);
            var skillsHint=detail.Find("SkillsHint"); if(skillsHint!=null)skillsHint.gameObject.SetActive(false);
            CrewRect(detailSkillsContainer,.055f,.07f,.945f,.49f);
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
                    if(square!=null) square.enabled=false;
                    var slotRect=(RectTransform)slot;
                    float side=Mathf.Min(((RectTransform)row).rect.height*.9f, ((RectTransform)row).rect.width*width-8f);
                    slotRect.anchorMin=slotRect.anchorMax=new Vector2((i+.5f)*width,.5f);
                    slotRect.pivot=new Vector2(.5f,.5f); slotRect.sizeDelta=Vector2.one*Mathf.Max(1,side); slotRect.anchoredPosition=Vector2.zero;
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
                    if(row==passive)CrewLabel(slot,"PassiveLabel","패시브",12,CrewMuted,0,-.2f,1,-.015f,TextAlignmentOptions.Center);
                }
            }
        }

        private void StyleCrewActions(Transform root)
        {
            var footer = CrewPanel(root,"CrewFooterSurface",CrewInk,0,0,1,.105f);
            var back = root.Find("BackButton");
            if (back != null) footer.transform.SetSiblingIndex(back.GetSiblingIndex()
                - (footer.transform.GetSiblingIndex() < back.GetSiblingIndex() ? 1 : 0));
            StyleCrewAction(root.Find("BackButton"),"‹  메인 화면",Color.clear,CrewWhite,.04f,.022f,.19f,.084f);
            StyleCrewAction(root.Find("RelicGachaButton"),"방주 유물 소환",new Color(.10f,.16f,.20f,.98f),CrewWhite,.40f,.022f,.585f,.084f);
            StyleCrewAction(root.Find("ConfirmStartButton"),"여정 시작  ›",CrewGold,new Color(.07f,.09f,.105f,1),.62f,.022f,.96f,.084f);
            var relic=root.Find("DrawnRelicSlot");
            CrewRect(relic,.343f,.022f,.385f,.084f);
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
            var caption=root.Find("DepartureCaption");if(caption!=null)caption.gameObject.SetActive(false);
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
