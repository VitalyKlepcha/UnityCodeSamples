using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading;
using Zenject;
using DG.Tweening;

public class FinishLevelPopUp : MonoBehaviour
{
    IAdsManager _adsManager;


    [Inject(Id = "PopUpCanvas")]
    private Canvas _popUpCanvas;



    InterstitialAdsListener _interstitialListener;
    private CancellationTokenSource _timeoutCancellationTokenSource;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float elementDelay = 0.2f;
    [SerializeField] private float scaleStart = 0.5f;
    [SerializeField] private float scaleEnd = 1f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    [Header("UI Components")]
    [SerializeField] private RectTransform _starPanel;
    [SerializeField] private RectTransform _result;
    [SerializeField] private Button _continue;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private Image[] _stars;
    [SerializeField] private Sprite _fillStarSprite;
    private CanvasGroup canvasGroup;

    [Inject]
    void Setup(IAdsManager adsManager)
    {
        _adsManager = adsManager;
    }

    public void Initialize(int starNum, string resultText = null)
    {
        if (resultText != null)
        {
            _resultText.text = resultText;
        }
        for(int i = 0; i < starNum; i++)
        {
            _stars[i].sprite = _fillStarSprite;
        }

        _continue.onClick.AddListener(ClosePopup);
    }

    #region Interstitial
    private void SetInterstitialListener()
    {
        if (_interstitialListener != null)
            return;

        _interstitialListener = new InterstitialAdsListener()
        {
            OnFinish = HandleInterstitialFinish,
            OnStart = HandleInterstitialOpen,
            OnError = HandleInterstitialError
        };
        _adsManager.SetInterstitialListener(_interstitialListener);
    }

    private void RemoveInterstitialListener()
    {
        _interstitialListener = null;
        _adsManager.RemoveInterstitialListener();
    }

    private void ShowInterstitial()
    {
        SetInterstitialListener();

        this.ShowActivityIndicator(_popUpCanvas.transform);

        _timeoutCancellationTokenSource = new CancellationTokenSource();
        _adsManager.ShowInterstitial(_timeoutCancellationTokenSource.Token);
    }

    private void HandleInterstitialOpen(string placementId)
    {
        _timeoutCancellationTokenSource?.CancelAndDispose();
    }

    private void HandleInterstitialFinish(string placementId)
    {
        this.HideActivityIndicator();

        RemoveInterstitialListener();

        PerformActionAfterInterstitial();
    }

    private void HandleInterstitialError(string message)
    {
        this.HideActivityIndicator();

        RemoveInterstitialListener();

        _timeoutCancellationTokenSource?.CancelAndDispose();

        Log.Warning(message);

        PerformActionAfterInterstitial();
    }

    private void PerformActionAfterInterstitial()
    {
        SceneManager.LoadScene("Game");
    }
    #endregion

    #region Animation
    private void OnEnable()
    {
        SetupCanvasGroup();
        InitializeAnimation();
        PlayEntranceAnimation();
    }

    private void SetupCanvasGroup()
    {
        if (canvasGroup == null)
        {
            // Method 1: Get from this object first
            canvasGroup = GetComponent<CanvasGroup>();

            // Method 2: Get from parent (the singleton canvas)
            if (canvasGroup == null && transform.parent != null)
            {
                canvasGroup = transform.parent.GetComponent<CanvasGroup>();
            }

            // Method 3: Get from any parent in hierarchy (in case of nested objects)
            if (canvasGroup == null)
            {
                canvasGroup = GetComponentInParent<CanvasGroup>();
            }

            // Method 4: Create new one on this object as fallback
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
                Debug.Log("Added CanvasGroup to popup object", this);
            }
        }
    }

    private void InitializeAnimation()
    {
        // Set initial state for main canvas group
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Set initial states for individual elements
        if (_starPanel != null)
        {
            _starPanel.gameObject.SetActive(false); // Hide initially
        }

        if (_result != null)
        {
            _result.localScale = Vector3.one * scaleStart;
            _result.gameObject.SetActive(false);
        }

        if (_continue != null)
        {
            _continue.transform.localScale = Vector3.one * scaleStart;
            _continue.gameObject.SetActive(false);
            _continue.interactable = false;
        }
    }

    private void PlayEntranceAnimation()
    {
        // Clean up any existing animations
        DOTween.Kill(this);
        DOTween.Kill(_starPanel);
        DOTween.Kill(_result);
        DOTween.Kill(_continue);
        DOTween.Kill(canvasGroup);

        // Main fade-in animation
        canvasGroup.DOFade(1f, animationDuration)
            .SetEase(Ease.OutQuad)
            .OnStart(() =>
            {
                // Show all elements at start of animation
                if (_starPanel != null) _starPanel.gameObject.SetActive(true);
                if (_continue != null) _continue.gameObject.SetActive(true);
                if (_result != null) _result.gameObject.SetActive(true);
            });

        // Staggered element animations using sequence
        Sequence sequence = DOTween.Sequence();


        // 1. Animate star panel
        if (_starPanel != null)
        {
            AnimateIndividualStars();
            sequence.AppendInterval(elementDelay);
        }
        else
        {
            sequence.AppendInterval(animationDuration);
        }

        // 2. Animate result text 
        if (_result != null)
        {

            sequence.AppendCallback(() => AnimateResultText());
            sequence.AppendInterval(elementDelay);
        }
        else
        {
            sequence.AppendInterval(animationDuration);
        }

        // 3. Animate continue button last
        if (_continue != null)
        {
            sequence.Append(_continue.transform.DOScale(scaleEnd, animationDuration)
                .SetEase(easeType)
                .OnStart(() => _continue.interactable = false))
                .OnComplete(EnableInteractions);
        }
        else
        {
            sequence.OnComplete(EnableInteractions);
        }

        sequence.Play();
    }

    private void AnimateResultText()
    {
        if (_result == null) return;

        // Reset to initial state
        _result.localScale = Vector3.one * scaleStart;

        // Create a sequence of multiple animations
        Sequence resultSequence = DOTween.Sequence();

        // 1. Scale up with bounce
        resultSequence.Append(_result.DOScale(scaleEnd * 1.2f, animationDuration * 0.4f)
            .SetEase(Ease.OutBack));

        // 2. Slight overshoot then settle
        resultSequence.Append(_result.DOScale(scaleEnd, animationDuration * 0.3f)
            .SetEase(Ease.OutQuad));

        // 3. Add rotation effect
        resultSequence.Join(_result.DORotate(new Vector3(0, 0, -5f), animationDuration * 0.4f)
            .SetEase(Ease.OutQuad)
            .SetLoops(2, LoopType.Yoyo));

        if (_resultText != null)
        {
            Color originalColor = _resultText.color;
            resultSequence.Join(_resultText.DOColor(new Color(1.2f, 1.2f, 1.2f, 1f), animationDuration * 0.7f)
                .SetEase(Ease.OutQuad)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => { _resultText.color = originalColor; }));
        }

        resultSequence.Play();
    }

    private void AnimateIndividualStars()
    {
        // Animate each star individually for a more dynamic effect
        RectTransform[] starTransforms = _starPanel.GetComponentsInChildren<RectTransform>(true);

        // Filter out the panel itself and only get actual star transforms
        foreach (RectTransform starTransform in starTransforms)
        {
            // Skip the panel itself and any non-star containers
            if (starTransform == _starPanel) continue;
            if (starTransform.parent != _starPanel) continue; // Only direct children

            // Reset scale
            starTransform.localScale = Vector3.zero;
            starTransform.gameObject.SetActive(true);

            // Get index among actual stars (direct children of the panel)
            int starIndex = 0;
            for (int i = 0; i < _starPanel.childCount; i++)
            {
                if (_starPanel.GetChild(i) == starTransform)
                {
                    starIndex = i;
                    break;
                }
            }

            // Staggered animation for each star - scale up with bounce effect
            starTransform.DOScale(1f, 0.4f)
                .SetEase(Ease.OutBack)
                .SetDelay(0.15f * starIndex)
                .OnStart(() => {
                    // Optional: Add a little rotation effect for extra polish
                    starTransform.DOPunchRotation(new Vector3(0, 0, 15f), 0.3f, 2, 0.5f);
                });
        }
    }

    private void EnableInteractions()
    {
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (_continue != null)
        {
            _continue.interactable = true;
        }
    }

    public void ClosePopup()
    {
        if (_continue != null) _continue.interactable = false;

        // Reverse animation sequence
        Sequence exitSequence = DOTween.Sequence();

        // 1. Animate continue button first (reverse order)
        if (_continue != null)
        {
            exitSequence.Append(_continue.transform.DOScale(scaleStart, animationDuration * 0.6f)
                .SetEase(Ease.InBack));
        }

        // 2. Animate star panel
        if (_starPanel != null)
        {
            exitSequence.Join(_starPanel.DOScale(scaleStart, animationDuration * 0.7f)
                .SetEase(Ease.InBack));
        }

        // 3. Animate result text
        if (_result != null)
        {
            exitSequence.Join(_result.DOScale(scaleStart, animationDuration * 0.8f)
                .SetEase(Ease.InBack));
        }

        // 4. Fade out entire canvas group
        exitSequence.Join(canvasGroup.DOFade(0f, animationDuration)
            .SetEase(Ease.InQuad))
            .OnComplete(ShowInterstitial);

        exitSequence.Play();

    }

    private void OnDisable()
    {
        // Clean up all tweens
        DOTween.Kill(_starPanel);
        DOTween.Kill(_result);
        DOTween.Kill(_continue);
        DOTween.Kill(canvasGroup);
        DOTween.Kill(this);
    }
    #endregion
}
