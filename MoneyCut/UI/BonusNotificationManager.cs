using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class BonusNotificationManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform notificationPanel;
    [SerializeField] private Image bonusIcon;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float showDuration = 3f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float slideDistance = 100f;
    [SerializeField] private Ease slideEase = Ease.OutBack;
    [SerializeField] private Ease fadeEase = Ease.InOutQuad;

    private Vector2 originalPosition;
    private Sequence currentAnimation;
    private Queue<BonusNotification> notificationQueue = new Queue<BonusNotification>();
    private bool isShowingNotification = false;

    private struct BonusNotification
    {
        public BonusItem.BonusTypes bonusType;
        public Sprite icon;
    }

    private void Awake()
    {
        // Store original position
        originalPosition = notificationPanel.anchoredPosition;

        // Start hidden
        canvasGroup.alpha = 0;
        notificationPanel.anchoredPosition = originalPosition - new Vector2(slideDistance, 0);
    }

    public void ShowBonusNotification(BonusItem.BonusTypes bonusType, BonusSpriteDatabase spriteDatabase)
    {
        // Get sprite from database
        Sprite icon = spriteDatabase.GetSprite(bonusType);

        // Create notification
        BonusNotification notification = new BonusNotification
        {
            bonusType = bonusType,
            icon = icon
        };

        // Add to queue
        notificationQueue.Enqueue(notification);

        // Show if not already showing
        if (!isShowingNotification)
        {
            ShowNextNotification();
        }
    }

    private void ShowNextNotification()
    {
        if (notificationQueue.Count == 0)
        {
            isShowingNotification = false;
            return;
        }

        isShowingNotification = true;
        BonusNotification notification = notificationQueue.Dequeue();

        // Set up notification
        bonusIcon.sprite = notification.icon;

        // Start animation
        AnimateNotification();
    }

    private void AnimateNotification()
    {
        // Kill any existing animation
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
        }

        // Reset position and alpha
        notificationPanel.anchoredPosition = originalPosition - new Vector2(slideDistance, 0);
        canvasGroup.alpha = 0;

        // Create animation sequence
        currentAnimation = DOTween.Sequence();

        // Slide in and fade in
        currentAnimation.Append(notificationPanel.DOAnchorPos(originalPosition, fadeDuration).SetEase(slideEase));
        currentAnimation.Join(canvasGroup.DOFade(1, fadeDuration).SetEase(fadeEase));

        // Stay visible
        currentAnimation.AppendInterval(showDuration);

        // Slide out and fade out
        currentAnimation.Append(notificationPanel.DOAnchorPos(originalPosition + new Vector2(slideDistance, 0), fadeDuration).SetEase(slideEase));
        currentAnimation.Join(canvasGroup.DOFade(0, fadeDuration).SetEase(fadeEase));

        // Show next notification or complete
        currentAnimation.OnComplete(() => {
            ShowNextNotification();
        });

        currentAnimation.Play();
    }

    public void ClearAllNotifications()
    {
        notificationQueue.Clear();

        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
        }

        // Immediately hide
        canvasGroup.alpha = 0;
        notificationPanel.anchoredPosition = originalPosition - new Vector2(slideDistance, 0);
        isShowingNotification = false;
    }
}