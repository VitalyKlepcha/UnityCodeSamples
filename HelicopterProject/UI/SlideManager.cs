using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization.Settings;
using System.Linq;

public class SlideManager : MonoBehaviour
{
    #region Variables

    private Slide[] slides;

    public Slide[] inspSlides;


    protected static int step;

    [SerializeField]
    protected TextMeshProUGUI m_title;

    [SerializeField]
    protected TextMeshProUGUI m_sentence;

    [SerializeField]
    protected GameObject m_imageLayout;

    [SerializeField]
    protected TextMeshProUGUI count;

    [SerializeField]
    protected GameObject leftArrow;

    [SerializeField]
    protected GameObject rightArrow;

    [SerializeField]
    protected GameObject _imagePrefab;

    [SerializeField]
    protected CardFlip _card;

    private Vector2 _swipeStartPos;

    private Vector2 _swipeEndPos;

    public static float SWIPE_MIN_DISTANCE = 450f;

    //If y delta of swipe more then this value, then swipe won't count
    private float _swipeYMaxDistance = 500f;

    public event Action<int,int> OnStepChange;

    //RightAwayCard
    [SerializeField]
    private Transform _awayCardParent;

    //LeftAwayCard
    [SerializeField]
    private Transform _leftAwayCardParent;

    protected bool _playAwayAnim = true;

    private bool _firstStep = true;

    [SerializeField]
    private GameObject _slideMenu;

    [SerializeField]
    private Transform _slideMenuGrid;

    [SerializeField]
    private GameObject _slideChoicePrefab;

    [SerializeField]
    private GameObject _slideMenuButton;

    private static bool touchOnGrid = false;

    public static bool TouchOnGrid { get => touchOnGrid; }

    [SerializeField]
    private List<Color> _cardColors;

    private int _colorStep;
    private int ColorStep { get => _colorStep; set => _colorStep = value > _cardColors.Count - 1 ? 0 : value; }

    private bool isSlideMenuInitialized = false;

    [SerializeField]
    private Transform _content;

    [SerializeField]
    private LocalizedStringTable _localizedTable;
    #endregion

    #region BuiltInMethods
    private void Update()
    {
        count.text = string.Format("{0}/{1}", step + 1, slides.Length);
        HandleSwipe();
    }

    private IEnumerator Start()
    {
        foreach (Slide slide in slides)
        {
           SetAudio(slide);
        }
        SetArrowsButtonsState();
        PlayAudio(slides[step]);
        if (_localizedTable.IsEmpty)
        {
            _playAwayAnim = false;
            UpdateSlide();
            _playAwayAnim = true;
            SetSentenceScale();
            InitSlideMenuGrid();
            yield break;
        }
        var getTableAsync =_localizedTable.GetTableAsync();
        yield return getTableAsync;
        var table = getTableAsync.Result;
        LocalizeSlides(table);
        SetSentenceScale();
        _playAwayAnim = false;
        UpdateSlide();
        _playAwayAnim = true;
        InitSlideMenuGrid();
        LocalizationSettings.SelectedLocaleChanged += LocaleChangeHandler;
    }
    private void OnEnable()
    {
        slides = inspSlides;
        foreach (Slide slide in slides)
        {
            StopAudio(slide);
        }
    }

    #endregion

    #region Methods

    private void LocaleChangeHandler(Locale locale)
    {
        var stringTable = _localizedTable.GetTable();
        LocalizeSlides(stringTable);
        SetSentenceScale();
        _playAwayAnim = false;
        UpdateSlide();
        _playAwayAnim = true;
        LocalizeSlideMenu(stringTable);
    }

    private void LocalizeSlides(StringTable stringTable)
    {
        for(int i = 0; i < inspSlides.Length; i++)
        {
            //Localize title
            var titleKey = "Title" + (i+1);
            var titleEntry = stringTable.GetEntry(titleKey);
            if (titleEntry != null && !string.IsNullOrEmpty(titleEntry.GetLocalizedString()))
            {
                slides[i].title = titleEntry.GetLocalizedString();
            }
            //Localize sentence
            var sentenceKey = "Sentence" + (i+1);
            var sentenceEntry = stringTable.GetEntry(sentenceKey);
            if(sentenceEntry != null && !string.IsNullOrEmpty(sentenceEntry.GetLocalizedString()))
            {
                slides[i].sentence = sentenceEntry.GetLocalizedString();
            }
        }
    }

    private void LocalizeSlideMenu(StringTable stringTable)
    {
        var slideMenuItems = _slideMenuGrid.GetComponentsInChildren<SlideMenuItem>().OrderBy(item => int.Parse(item.Num.text)).ToArray();
        if (slideMenuItems == null || slideMenuItems.Length == 0)
            return;
        for (int i = 0; i < slideMenuItems.Length; i++)
        {
            //Localize title
            var titleKey = "Title" + (i + 1);
            var titleEntry = stringTable.GetEntry(titleKey);
            if (titleEntry != null && !string.IsNullOrEmpty(titleEntry.GetLocalizedString()))
            {
                slideMenuItems[i].Title.text = titleEntry.GetLocalizedString();
            }
        }
    }

    /// <summary>
    /// Updates sentence scale using <size=value%> tag
    /// </summary>
    private void SetSentenceScale()
    {
        foreach(var slide in slides)
        {
            //Skip if sentenceScale is default or sentence size is set manually
            if (slide.sentenceScale == 100 || slide.sentence.Contains("<size="))
                continue;
            slide.sentence = string.Concat("<size=", slide.sentenceScale, "%>", slide.sentence);
        }
    }

    private void InitSlideMenuGrid()
    {
        //Avoid repeatable initialization
        if (isSlideMenuInitialized)
            return;
        for(int i  = 1; i < slides.Length + 1; i++)
        {
            SlideMenuItem slideChoice = Instantiate(_slideChoicePrefab, _slideMenuGrid).GetComponent<SlideMenuItem>();
            slideChoice.Num.text = i.ToString();
            slideChoice.Title.text = slides[i - 1].title;
            bool hasIcon = slides[i - 1].images.Length > 0;
            //Reduce title font size if title is too big and has icon
            if (slides[i - 1].title.Length > 60 && hasIcon)
            {
                slideChoice.Title.fontSizeMax = 80;
            }
            if(hasIcon)
                slideChoice.Icon.sprite = slides[i-1].images[0];
            else
            {
                Destroy(slideChoice.Icon.transform.parent.gameObject);
            }
            Button button = slideChoice.GetComponent<Button>();
            int cashedIndex = i - 1;
            button.onClick.AddListener(delegate {
                touchOnGrid = true;
                SetStep(cashedIndex);
                UpdateSlide();
                _slideMenu.SetActive(false);
                _slideMenuButton.SetActive(true);
                touchOnGrid = false;
                _card.SetCardSprite();
            });
            slideChoice.name = (i-1).ToString();
        }
        isSlideMenuInitialized = true;
    }

    private void HandleSwipe()
    {
        //Block swipe if any zoomed image active
        if (GetComponentInChildren<ZoomedImage>(false) != null)
            return;
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0)) {
            _swipeStartPos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0)) {
            _swipeEndPos = Input.mousePosition;
            DidSwipe();
        }
            
#endif
        if(Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if(touch.phase == TouchPhase.Began) {
                _swipeStartPos = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended) {
                _swipeEndPos = touch.position;
                DidSwipe();
            }
        }
    }

    private void DidSwipe() {
        if (Mathf.Abs(_swipeEndPos.y - _swipeStartPos.y) < _swipeYMaxDistance) {
            float swipeDistanseX = _swipeEndPos.x - _swipeStartPos.x;
            if (swipeDistanseX > SWIPE_MIN_DISTANCE) {
                //If slide isn't first
                if (step > 0) {
                    LeftArrowAction();
                }
            }
            else if (swipeDistanseX < -SWIPE_MIN_DISTANCE) {
                //If slide isn't last
                if (step < slides.Length - 1) {
                    RightArrowAction();
                }
            }
        }
    }

    public void DecrementStep()
    {
        SetStep(step-1);
    }

    public void SetStep(int newStep)
    {
        StopAudio(slides[step]);
        if (newStep < 0 && newStep > slides.Length - 1)
            return;
        step = newStep;
        SetArrowsButtonsState();
        PlayAudio(slides[step]);
        OnStepChange?.Invoke(step, slides.Length);
    }

    public void IncrementStep()
    {
        SetStep(step+1);
    }

    public void NulifyStep()
    {
        step = 0;
    }

    /// <summary>
    /// Controlls all aspects of slide, such as images size, scroll area, sentence, title
    /// </summary>
    public void UpdateSlide(bool rightAway = true)
    {
        if (_playAwayAnim)
        {
            HandleAwayCard(rightAway);
        }
        _card.Reset();
        ChangeCardColor();

        Slide slide = slides[step];
        m_title.text = slide.title;
        m_sentence.text = slide.sentence;
        m_sentence.transform.parent.gameObject.GetComponent<ScrollRect>().normalizedPosition = new Vector2(0, 1.1f);
        //Destroy previous images
        foreach (Transform child in m_imageLayout.transform)
        {
            Destroy(child.gameObject);
        }
        if (slide.images.Length > 0)
        {
            if (_imagePrefab == null)
            {
                Debug.LogError("ZP SlideManager _imagePrefab is not set");
            }
            //Instantiang images
            foreach (var sprite in slide.images)
            {
                var image = Instantiate(_imagePrefab, m_imageLayout.transform);
                image.GetComponent<Image>().sprite = sprite;
                image.GetComponent<SlideImage>().Container = _content;
            }
            m_imageLayout.SetActive(true);
            //Controll image size
            GridLayoutGroup grid = m_imageLayout.GetComponent<GridLayoutGroup>();
            SetGridCellSize(grid, slide.images.Length);
        }
        else if (slide.images.Length == 0)
        {
            //Makes vertical layout stretch scroll area on whole card
            m_imageLayout.SetActive(false);
        }
    }

    protected void ChangeCardColor()
    {
        _card.GetComponent<Image>().color = _cardColors[ColorStep];
        ColorStep++;
    }

    protected void HandleAwayCard(bool rightAway)
    {
        Transform awayCardParent;
        awayCardParent = rightAway ? _awayCardParent : _leftAwayCardParent;
        awayCardParent.gameObject.SetActive(false);
        foreach(Transform child in awayCardParent)
        {
            Destroy(child.gameObject);
        }
        var awayCard = Instantiate(_card.gameObject,_card.gameObject.transform.parent);
        Destroy(awayCard.GetComponent<CardFlip>());
        var rectTransform = awayCard.GetComponent<RectTransform>();
        var yRot = rectTransform.rotation.eulerAngles.y;
        awayCard.transform.SetParent(awayCardParent);
        rectTransform.position = awayCardParent.position;
        if (_firstStep)
        {
            rectTransform.rotation = Quaternion.Euler(0, yRot, 0);
            _firstStep = false;
        }
        else if ((!_card.IsFront && rightAway) || (_card.IsFront && !rightAway))
        {
            rectTransform.rotation = Quaternion.Euler(0, yRot, 35f);
        }
        else
        {
            rectTransform.rotation = Quaternion.Euler(0, yRot, -35f);
        }
        awayCardParent.gameObject.SetActive(true);


    }


    private void SetAudio(Slide slide) {
        var audio = slide.audio;
        if(audio != null)
        {
            slide.source = gameObject.AddComponent<AudioSource>();
            slide.source.clip = slide.audio;
        }
    }
    private void PlayAudio(Slide slide)
    {
        if (slide.source)
        {
            slide.source.Play();
        }
    }

    private void StopAudio(Slide slide)
    {
        if (slide!=null && slide.source)
        {
            slide.source.Stop();
        }
    }

    private void SetArrowsButtonsState() {
        if(step == 0)
        {
            // first slide
            leftArrow.GetComponent<Button>().interactable = false;
            rightArrow.GetComponent<Button>().interactable = true;
        }
        else if(step == slides.Length - 1)
        {
            // last slide
            leftArrow.GetComponent<Button>().interactable = true;
            rightArrow.GetComponent<Button>().interactable = false;
        }
        else
        {
            leftArrow.GetComponent<Button>().interactable = true;
            rightArrow.GetComponent<Button>().interactable = true;
        }
    }

    public void RightArrowAction()
    {
        if (!_card.IsRotating)
        {
            IncrementStep();
            UpdateSlide();
            _card.SetCardSprite();
        }
    }

    public void LeftArrowAction()
    {
        if (!_card.IsRotating)
        {
            DecrementStep();
            UpdateSlide(false);
            _card.SetCardSprite();
        }
    }
    #endregion


    public virtual void SetGridCellSize(GridLayoutGroup grid, int countOfImages) {
        if (countOfImages > 2) {
            grid.cellSize = new Vector2(500, 500);
        }
        else {
            grid.cellSize = new Vector2(700, 700);
        }
    }
}
