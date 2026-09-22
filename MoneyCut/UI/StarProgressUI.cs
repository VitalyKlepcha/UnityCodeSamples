using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Security.AccessControl;

public class StarProgressUI : MonoBehaviour
{
    [Header("Star References")]
    [SerializeField] private Image[] starIcons = new Image[3];
    [SerializeField] private Sprite emptyStar;
    [SerializeField] private Sprite filledStar;
    [SerializeField] private GameObject starFlyPrefab;
    [SerializeField] private Transform starFlyOrigin;
    [SerializeField] private AudioClip starEarnedSound;

    [Header("Animation Settings")]
    [SerializeField] private float flyDuration = 1f;
    [SerializeField] private Ease flyEase = Ease.OutBack;
    [SerializeField] private float showDuration = 2f; // How long to stay visible after animation
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float soundVolume = 0.7f;

    [Header("UI Components")]
    [SerializeField] private CanvasGroup panelCanvasGroup;

    private int _currentStars = 0;
    private bool _initialized = false;
    private Coroutine _hideCoroutine;
    private Sequence _showSequence;

    private void Awake()
    {
        // Start hidden
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.blocksRaycasts = false;
            panelCanvasGroup.interactable = false;
        }
    }

    public void Initialize()
    {
        if (_initialized) return;

        // Set all stars to empty initially
        foreach (Image star in starIcons)
        {
            if (star != null) star.sprite = emptyStar;
        }

        _initialized = true;
    }

    public void UpdateStars(int starCount, bool animate = true)
    {
        if (!_initialized) Initialize();
        if (starCount == _currentStars) return;

        // Show the panel
        ShowPanel();

        // Only animate new stars that were earned
        for (int i = _currentStars; i < starCount && i < starIcons.Length; i++)
        {
            if (starIcons[i] != null)
            {
                if (animate)
                {
                    AnimateStarEarned(i);
                }
                else
                {
                    starIcons[i].sprite = filledStar;
                    starIcons[i].transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 2, 0.5f);
                }
            }
        }

        _currentStars = starCount;

        // Restart the hide timer
        RestartHideTimer();
    }

    private void AnimateStarEarned(int starIndex)
    {
        if (starFlyPrefab == null || starFlyOrigin == null)
        {
            // Fallback: just fill the star with animation
            starIcons[starIndex].sprite = filledStar;
            starIcons[starIndex].transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 2, 0.5f);
            return;
        }

        // Create a flying star animation
        GameObject flyingStar = Instantiate(starFlyPrefab, starFlyOrigin.position, Quaternion.identity, transform);
        Image starImage = flyingStar.GetComponent<Image>();

        if (starImage != null)
        {
            // Set initial state
            flyingStar.transform.localScale = Vector3.zero;

            // Animate the star flying to its position
            flyingStar.transform.DOMove(starIcons[starIndex].transform.position, flyDuration)
                .SetEase(flyEase)
                .OnStart(() => {
                    // Scale up at start
                    flyingStar.transform.DOScale(1f, flyDuration * 0.3f).SetEase(Ease.OutBack);
                })
                .OnComplete(() => {
                    // Fill the actual star
                    starIcons[starIndex].sprite = filledStar;
                    starIcons[starIndex].transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 2, 0.5f);

                    // Remove the flying star
                    Destroy(flyingStar);

                    // Restart hide timer after animation completes
                    RestartHideTimer();
                });
        }
    }

    private void ShowPanel()
    {
        // Cancel any ongoing hide operations
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }

        if (_showSequence != null && _showSequence.IsActive())
        {
            _showSequence.Kill();
        }

        // Show panel with animation
        _showSequence = DOTween.Sequence();
        _showSequence.Append(panelCanvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad))
                     .OnStart(() => {
                         PlayStarSound();
                         panelCanvasGroup.blocksRaycasts = true;
                         panelCanvasGroup.interactable = true;
                     });
    }

    private void HidePanel()
    {
        if (_showSequence != null && _showSequence.IsActive())
        {
            _showSequence.Kill();
        }

        Sequence hideSequence = DOTween.Sequence();
        hideSequence.Append(panelCanvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad))
                    .OnComplete(() => {
                        panelCanvasGroup.blocksRaycasts = false;
                        panelCanvasGroup.interactable = false;
                    });
    }

    private void RestartHideTimer()
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
        }
        _hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(showDuration);
        HidePanel();
        _hideCoroutine = null;
    }

    public void ResetStars()
    {
        _currentStars = 0;
        foreach (Image star in starIcons)
        {
            if (star != null) star.sprite = emptyStar;
        }

        // Hide immediately when resetting
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }
        HidePanel();
    }

    // Manual control methods
    public void ShowPanelTemporarily(float duration = 3f)
    {
        ShowPanel();
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
        }
        _hideCoroutine = StartCoroutine(HideAfterDelay(duration));
    }

    private IEnumerator HideAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        HidePanel();
        _hideCoroutine = null;
    }

    public void ForceShowPanel()
    {
        ShowPanel();
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }
    }

    public void ForceHidePanel()
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }
        HidePanel();
    }

    private void OnDisable()
    {
        // Clean up any running coroutines or tweens
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }

        if (_showSequence != null && _showSequence.IsActive())
        {
            _showSequence.Kill();
            _showSequence = null;
        }

        DOTween.Kill(panelCanvasGroup);
        foreach (Image star in starIcons)
        {
            if (star != null) DOTween.Kill(star.transform);
        }
    }

    private void PlayStarSound()
    {
        if (starEarnedSound != null)
        {
            AudioSource.PlayClipAtPoint(starEarnedSound, Camera.main.transform.position, soundVolume);
        }
    }
}