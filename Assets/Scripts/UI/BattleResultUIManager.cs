using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 전투 종료(승리/패배) 결과를 고품격 글래스모피즘 스타일의 UI 패널로 동적 생성하여 표시하는 매니저입니다.
    /// </summary>
    public class BattleResultUIManager : MonoBehaviour
    {
        public static BattleResultUIManager Instance { get; private set; }

        private Canvas _targetCanvas;
        private Sprite _whiteSprite;
        private TMP_FontAsset _cachedFont;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // 1픽셀 흰색 텍스처를 활용해 동적 스프라이트 생성 (배경 및 테두리용)
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// 현재 씬 내에 존재하는 활성화된 Canvas를 검색하거나 캐싱합니다.
        /// </summary>
        private Canvas GetCanvas()
        {
            if (_targetCanvas == null)
            {
                _targetCanvas = FindObjectOfType<Canvas>();
                if (_targetCanvas == null)
                {
                    GameObject canvasObj = new GameObject("BattleResultCanvas");
                    _targetCanvas = canvasObj.AddComponent<Canvas>();
                    _targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasObj.AddComponent<CanvasScaler>();
                    canvasObj.AddComponent<GraphicRaycaster>();
                }
            }
            return _targetCanvas;
        }

        /// <summary>
        /// 한글 폰트가 깨지지 않도록 기존 씬 내 텍스트의 폰트 에셋을 동적으로 수집 및 캐싱합니다.
        /// </summary>
        private TMP_FontAsset GetFontAsset()
        {
            if (_cachedFont == null)
            {
                var allTMPs = FindObjectsOfType<TextMeshProUGUI>(true);
                foreach (var tmp in allTMPs)
                {
                    if (tmp.font != null)
                    {
                        _cachedFont = tmp.font;
                        break;
                    }
                }
            }
            return _cachedFont;
        }

        /// <summary>
        /// 승리 보상 화면을 띄웁니다.
        /// </summary>
        public void ShowVictoryScreen(int gold, int exp, Action onExit)
        {
            Canvas canvas = GetCanvas();
            TMP_FontAsset font = GetFontAsset();

            // 1. 전체 화면을 덮는 블러용 오버레이 배경 생성 (글래스모피즘 스타일)
            GameObject overlayObj = new GameObject("VictoryOverlay");
            overlayObj.transform.SetParent(canvas.transform, false);
            var rectOverlay = overlayObj.AddComponent<RectTransform>();
            rectOverlay.anchorMin = Vector2.zero;
            rectOverlay.anchorMax = Vector2.one;
            rectOverlay.sizeDelta = Vector2.zero;

            var imgOverlay = overlayObj.AddComponent<Image>();
            imgOverlay.sprite = _whiteSprite;
            // 딥 네이비 / 차콜 계열의 투명한 글래스 배경
            imgOverlay.color = new Color(0.07f, 0.09f, 0.15f, 0.88f);

            // 2. 중앙의 메인 카드 팝업 생성
            GameObject cardObj = new GameObject("VictoryCard");
            cardObj.transform.SetParent(overlayObj.transform, false);
            var rectCard = cardObj.AddComponent<RectTransform>();
            rectCard.sizeDelta = new Vector2(480, 520);
            rectCard.anchoredPosition = Vector2.zero;

            var imgCard = cardObj.AddComponent<Image>();
            imgCard.sprite = _whiteSprite;
            imgCard.color = new Color(0.12f, 0.16f, 0.26f, 0.95f);

            // 골드/메탈릭 테두리 추가 (부모 크기보다 3px 크게 설정)
            GameObject outlineObj = new GameObject("Outline");
            outlineObj.transform.SetParent(cardObj.transform, false);
            var rectOutline = outlineObj.AddComponent<RectTransform>();
            rectOutline.anchorMin = Vector2.zero;
            rectOutline.anchorMax = Vector2.one;
            rectOutline.sizeDelta = new Vector2(6, 6); // 테두리 두께 3px
            rectOutline.anchoredPosition = Vector2.zero;
            var imgOutline = outlineObj.AddComponent<Image>();
            imgOutline.sprite = _whiteSprite;
            imgOutline.color = new Color(0.85f, 0.65f, 0.2f, 0.6f); // 연골드톤 테두리
            outlineObj.transform.SetAsFirstSibling();

            // 3. 내부 요소 배치를 위한 Vertical Layout Group 설정
            var layout = cardObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 40, 40);
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            // 4. 헤더 텍스트: "전 투 승 리" (VICTORY)
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(cardObj.transform, false);
            var rectTitle = titleObj.AddComponent<RectTransform>();
            rectTitle.sizeDelta = new Vector2(420, 60);
            var txtTitle = titleObj.AddComponent<TextMeshProUGUI>();
            txtTitle.font = font;
            txtTitle.text = "BATTLE VICTORY";
            txtTitle.fontSize = 32;
            txtTitle.fontStyle = FontStyles.Bold;
            txtTitle.alignment = TextAlignmentOptions.Center;
            txtTitle.color = new Color(0.95f, 0.8f, 0.3f, 1f); // 찬란한 골드 톤

            // 데코 라인
            GameObject lineObj = new GameObject("DecoLine");
            lineObj.transform.SetParent(cardObj.transform, false);
            var rectLine = lineObj.AddComponent<RectTransform>();
            rectLine.sizeDelta = new Vector2(300, 2);
            var imgLine = lineObj.AddComponent<Image>();
            imgLine.sprite = _whiteSprite;
            imgLine.color = new Color(0.85f, 0.65f, 0.2f, 0.3f);

            // 5. 보상 박스 타이틀 및 리스트 컨테이너
            GameObject rewardHeaderObj = new GameObject("RewardHeader");
            rewardHeaderObj.transform.SetParent(cardObj.transform, false);
            var rectRewHead = rewardHeaderObj.AddComponent<RectTransform>();
            rectRewHead.sizeDelta = new Vector2(420, 30);
            var txtRewHead = rewardHeaderObj.AddComponent<TextMeshProUGUI>();
            txtRewHead.font = font;
            txtRewHead.text = "전투 획득 보상";
            txtRewHead.fontSize = 18;
            txtRewHead.alignment = TextAlignmentOptions.Center;
            txtRewHead.color = new Color(0.8f, 0.9f, 1f, 0.8f);

            // 보상 항목 1: 골드
            GameObject goldItem = CreateRewardItem(cardObj.transform, "GoldItem", font, "획득 골드", $"+ {gold} G", new Color(0.9f, 0.75f, 0.2f));
            // 보상 항목 2: 경험치
            GameObject expItem = CreateRewardItem(cardObj.transform, "ExpItem", font, "획득 경험치", $"+ {exp} EXP", new Color(0.2f, 0.8f, 0.6f));

            // 여백 컴포넌트
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(cardObj.transform, false);
            var rectSpacer = spacer.AddComponent<RectTransform>();
            rectSpacer.sizeDelta = new Vector2(420, 20);

            // 6. 맵 화면 이동 버튼
            GameObject buttonObj = new GameObject("ExitButton");
            buttonObj.transform.SetParent(cardObj.transform, false);
            var rectBtn = buttonObj.AddComponent<RectTransform>();
            rectBtn.sizeDelta = new Vector2(280, 50);

            var imgBtn = buttonObj.AddComponent<Image>();
            imgBtn.sprite = _whiteSprite;
            imgBtn.color = new Color(0.18f, 0.44f, 0.35f, 1f); // 에메랄드/그린 계열

            var btn = buttonObj.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                onExit?.Invoke();
            });

            // 호버 인터랙션 컴포넌트 추가
            var hover = buttonObj.AddComponent<UIHoverInteraction>();
            hover.targetImage = imgBtn;
            hover.normalColor = new Color(0.18f, 0.44f, 0.35f, 1f);
            hover.hoverColor = new Color(0.22f, 0.55f, 0.44f, 1f);

            // 버튼 내부 텍스트
            GameObject btnTextObj = new GameObject("BtnText");
            btnTextObj.transform.SetParent(buttonObj.transform, false);
            var rectBtnTxt = btnTextObj.AddComponent<RectTransform>();
            rectBtnTxt.anchorMin = Vector2.zero;
            rectBtnTxt.anchorMax = Vector2.one;
            rectBtnTxt.sizeDelta = Vector2.zero;

            var txtBtn = btnTextObj.AddComponent<TextMeshProUGUI>();
            txtBtn.font = font;
            txtBtn.text = "전리품 챙기고 이동";
            txtBtn.fontSize = 18;
            txtBtn.fontStyle = FontStyles.Bold;
            txtBtn.alignment = TextAlignmentOptions.Center;
            txtBtn.color = Color.white;
        }

        /// <summary>
        /// 패배(게임 오버) 화면을 띄웁니다.
        /// </summary>
        public void ShowDefeatScreen(Action onExit)
        {
            Canvas canvas = GetCanvas();
            TMP_FontAsset font = GetFontAsset();

            // 1. 전체 화면을 덮는 블러용 오버레이 배경 생성 (절망적인 다크 톤)
            GameObject overlayObj = new GameObject("DefeatOverlay");
            overlayObj.transform.SetParent(canvas.transform, false);
            var rectOverlay = overlayObj.AddComponent<RectTransform>();
            rectOverlay.anchorMin = Vector2.zero;
            rectOverlay.anchorMax = Vector2.one;
            rectOverlay.sizeDelta = Vector2.zero;

            var imgOverlay = overlayObj.AddComponent<Image>();
            imgOverlay.sprite = _whiteSprite;
            imgOverlay.color = new Color(0.08f, 0.04f, 0.04f, 0.94f); // 매우 어둡고 불그스름한 검정

            // 2. 중앙의 메인 카드 팝업 생성
            GameObject cardObj = new GameObject("DefeatCard");
            cardObj.transform.SetParent(overlayObj.transform, false);
            var rectCard = cardObj.AddComponent<RectTransform>();
            rectCard.sizeDelta = new Vector2(480, 420);
            rectCard.anchoredPosition = Vector2.zero;

            var imgCard = cardObj.AddComponent<Image>();
            imgCard.sprite = _whiteSprite;
            imgCard.color = new Color(0.18f, 0.10f, 0.10f, 0.96f);

            // 소프트 크림슨 테두리 추가 (부모 크기보다 4px 크게 설정)
            GameObject outlineObj = new GameObject("Outline");
            outlineObj.transform.SetParent(cardObj.transform, false);
            var rectOutline = outlineObj.AddComponent<RectTransform>();
            rectOutline.anchorMin = Vector2.zero;
            rectOutline.anchorMax = Vector2.one;
            rectOutline.sizeDelta = new Vector2(6, 6);
            rectOutline.anchoredPosition = Vector2.zero;
            var imgOutline = outlineObj.AddComponent<Image>();
            imgOutline.sprite = _whiteSprite;
            imgOutline.color = new Color(0.7f, 0.15f, 0.15f, 0.5f); // 다크 레드톤 테두리
            outlineObj.transform.SetAsFirstSibling();

            // 3. 내부 요소 배치를 위한 Vertical Layout Group 설정
            var layout = cardObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 45, 45);
            layout.spacing = 25;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            // 4. 헤더 텍스트: "게 임 오 버" (DEFEAT / GAME OVER)
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(cardObj.transform, false);
            var rectTitle = titleObj.AddComponent<RectTransform>();
            rectTitle.sizeDelta = new Vector2(420, 60);
            var txtTitle = titleObj.AddComponent<TextMeshProUGUI>();
            txtTitle.font = font;
            txtTitle.text = "DEFEAT";
            txtTitle.fontSize = 36;
            txtTitle.fontStyle = FontStyles.Bold;
            txtTitle.alignment = TextAlignmentOptions.Center;
            txtTitle.color = new Color(0.85f, 0.2f, 0.2f, 1f); // 강렬한 레드 톤

            // 데코 라인
            GameObject lineObj = new GameObject("DecoLine");
            lineObj.transform.SetParent(cardObj.transform, false);
            var rectLine = lineObj.AddComponent<RectTransform>();
            rectLine.sizeDelta = new Vector2(260, 2);
            var imgLine = lineObj.AddComponent<Image>();
            imgLine.sprite = _whiteSprite;
            imgLine.color = new Color(0.85f, 0.2f, 0.2f, 0.3f);

            // 5. 설명 텍스트
            GameObject descObj = new GameObject("DescText");
            descObj.transform.SetParent(cardObj.transform, false);
            var rectDesc = descObj.AddComponent<RectTransform>();
            rectDesc.sizeDelta = new Vector2(400, 50);
            var txtDesc = descObj.AddComponent<TextMeshProUGUI>();
            txtDesc.font = font;
            txtDesc.text = "모든 아군 캐릭터가 쓰러졌습니다.\n전력을 가다듬어 다시 도전해보세요.";
            txtDesc.fontSize = 16;
            txtDesc.alignment = TextAlignmentOptions.Center;
            txtDesc.color = new Color(0.85f, 0.85f, 0.85f, 0.8f);

            // 여백
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(cardObj.transform, false);
            var rectSpacer = spacer.AddComponent<RectTransform>();
            rectSpacer.sizeDelta = new Vector2(420, 10);

            // 6. 맵 화면 이동 버튼
            GameObject buttonObj = new GameObject("ExitButton");
            buttonObj.transform.SetParent(cardObj.transform, false);
            var rectBtn = buttonObj.AddComponent<RectTransform>();
            rectBtn.sizeDelta = new Vector2(280, 50);

            var imgBtn = buttonObj.AddComponent<Image>();
            imgBtn.sprite = _whiteSprite;
            imgBtn.color = new Color(0.48f, 0.16f, 0.16f, 1f); // 다크 레드 계열 버튼

            var btn = buttonObj.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                onExit?.Invoke();
            });

            // 호버 인터랙션 컴포넌트 추가
            var hover = buttonObj.AddComponent<UIHoverInteraction>();
            hover.targetImage = imgBtn;
            hover.normalColor = new Color(0.48f, 0.16f, 0.16f, 1f);
            hover.hoverColor = new Color(0.6f, 0.2f, 0.2f, 1f);

            // 버튼 내부 텍스트
            GameObject btnTextObj = new GameObject("BtnText");
            btnTextObj.transform.SetParent(buttonObj.transform, false);
            var rectBtnTxt = btnTextObj.AddComponent<RectTransform>();
            rectBtnTxt.anchorMin = Vector2.zero;
            rectBtnTxt.anchorMax = Vector2.one;
            rectBtnTxt.sizeDelta = Vector2.zero;

            var txtBtn = btnTextObj.AddComponent<TextMeshProUGUI>();
            txtBtn.font = font;
            txtBtn.text = "피해를 수습하고 퇴각";
            txtBtn.fontSize = 18;
            txtBtn.fontStyle = FontStyles.Bold;
            txtBtn.alignment = TextAlignmentOptions.Center;
            txtBtn.color = Color.white;
        }

        /// <summary>
        /// 보상 항목을 담은 한 줄의 예쁜 리스트 컴포넌트를 동적 생성합니다.
        /// </summary>
        private GameObject CreateRewardItem(Transform parent, string objName, TMP_FontAsset font, string label, string val, Color valColor)
        {
            GameObject container = new GameObject(objName);
            container.transform.SetParent(parent, false);
            var rectContainer = container.AddComponent<RectTransform>();
            rectContainer.sizeDelta = new Vector2(400, 45);

            var imgBg = container.AddComponent<Image>();
            imgBg.sprite = _whiteSprite;
            imgBg.color = new Color(1f, 1f, 1f, 0.05f); // 매우 은은한 반투명 한 줄 배경

            // 라벨 텍스트
            GameObject labelObj = new GameObject("LabelText");
            labelObj.transform.SetParent(container.transform, false);
            var rectLabel = labelObj.AddComponent<RectTransform>();
            rectLabel.anchorMin = new Vector2(0f, 0f);
            rectLabel.anchorMax = new Vector2(0.5f, 1f);
            rectLabel.pivot = new Vector2(0f, 0.5f);
            rectLabel.anchoredPosition = new Vector2(15, 0);

            var txtLabel = labelObj.AddComponent<TextMeshProUGUI>();
            txtLabel.font = font;
            txtLabel.text = label;
            txtLabel.fontSize = 16;
            txtLabel.alignment = TextAlignmentOptions.MidlineLeft;
            txtLabel.color = Color.white;

            // 값 텍스트
            GameObject valObj = new GameObject("ValText");
            valObj.transform.SetParent(container.transform, false);
            var rectVal = valObj.AddComponent<RectTransform>();
            rectVal.anchorMin = new Vector2(0.5f, 0f);
            rectVal.anchorMax = new Vector2(1f, 1f);
            rectVal.pivot = new Vector2(1f, 0.5f);
            rectVal.anchoredPosition = new Vector2(-15, 0);

            var txtVal = valObj.AddComponent<TextMeshProUGUI>();
            txtVal.font = font;
            txtVal.text = val;
            txtVal.fontSize = 18;
            txtVal.fontStyle = FontStyles.Bold;
            txtVal.alignment = TextAlignmentOptions.MidlineRight;
            txtVal.color = valColor;

            return container;
        }
    }

    /// <summary>
    /// 마우스 호버 및 클릭 액션 시 프리미엄 마이크로 인터랙션을 제공하는 동적 스크립트입니다.
    /// </summary>
    public class UIHoverInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Image targetImage;
        public Color normalColor;
        public Color hoverColor;

        private Vector3 _originalScale = Vector3.one;
        private Vector3 _targetScale = Vector3.one;
        private Color _targetColor;

        private void Start()
        {
            _originalScale = transform.localScale;
            _targetScale = _originalScale;
            _targetColor = normalColor;
            if (targetImage != null) targetImage.color = normalColor;
        }

        private void Update()
        {
            // 크기 Lerp 애니메이션
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.unscaledDeltaTime * 12f);
            // 색상 Lerp 애니메이션
            if (targetImage != null)
            {
                targetImage.color = Color.Lerp(targetImage.color, _targetColor, Time.unscaledDeltaTime * 12f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _targetScale = _originalScale * 1.05f; // 부드럽게 1.05배 확대
            _targetColor = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _targetScale = _originalScale;
            _targetColor = normalColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _targetScale = _originalScale * 0.96f; // 클릭 시 부드럽게 수축
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _targetScale = _originalScale * 1.05f;
        }
    }
}
