using System;
using UnityEngine;

public class IronSourceService : IAdsService
{

#if UNITY_ANDROID
    private const string appKey = "";
#elif UNITY_IOS 
    private const string appKey = "";
#else
    private const string appKey = "unexpected_platform";
#endif

    private bool _isInitialized;

    public bool IsInitialized { get => _isInitialized; }

    private const string rewardedVideoPlacementId = "rewardedVideo";
    private const string interstitialPlacementId = "video";
    private const string bannerPlacementId = "banner";

    public string BannerPlacementId => bannerPlacementId;
    public string InterstitialPlacementId => interstitialPlacementId;
    public string RewardedVideoPlacementId => rewardedVideoPlacementId;

    private BannerListener _bannerListener;
    private InterstitialAdsListener _interstitialListener;
    private RewardedVideoListener _rewardedListener;

    public void Init()
    {
        IronSource.Agent.validateIntegration();

        IronSource.Agent.init(appKey, IronSourceAdUnits.INTERSTITIAL, IronSourceAdUnits.BANNER,
            IronSourceAdUnits.REWARDED_VIDEO);
        IronSourceEvents.onSdkInitializationCompletedEvent += SdkInitializationCompletedEvent;
    }

    #region Common events
    private void SdkInitializationCompletedEvent()
    {
        Debug.Log("Ironsource initialized");
        _isInitialized = true;
    }

    public void OnApplicationPause(bool isPaused)
    {
        IronSource.Agent.onApplicationPause(isPaused);
    }
    #endregion

    #region Banner
    public bool IsBannerAvailable => true;


    public void ShowBanner()
    {
        IronSource.Agent.displayBanner();
    }

    public void HideBanner()
    {
        IronSource.Agent.hideBanner();
    }


    public void LoadBanner()
    {
        IronSource.Agent.loadBanner(IronSourceBannerSize.SMART, IronSourceBannerPosition.BOTTOM);
    }

    public void SetBannerListener(BannerListener listener)
    {
        RemoveBannerListener();

        _bannerListener = listener;
        IronSourceBannerEvents.onAdLoadedEvent += BannerAdLoadedEvent;
        IronSourceBannerEvents.onAdLoadFailedEvent += BannerAdLoadFailedEvent;
    }

    public void RemoveBannerListener()
    {
        if (_bannerListener == null)
            return;

        _bannerListener = null;
        IronSourceBannerEvents.onAdLoadedEvent -= BannerAdLoadedEvent;
        IronSourceBannerEvents.onAdLoadFailedEvent -= BannerAdLoadFailedEvent;
    }

    private void BannerAdLoadedEvent(IronSourceAdInfo adInfo) => _bannerListener?.OnReady(adInfo);
    private void BannerAdLoadFailedEvent(IronSourceError error) => _bannerListener?.OnError(error?.getDescription() ?? "Unknown");

    #endregion

    #region Interstitial
    public bool IsInterstitialAvailable => true;
    public void LoadInterstitial()
    {
#if UNITY_EDITOR
        _interstitialListener?.OnError("Can't load interstitial in the editor");
#else
        IronSource.Agent.loadInterstitial();
#endif
    }

    public void ShowInterstitial()
    {
        if (!IronSource.Agent.isInterstitialReady())
        {
            _interstitialListener?.OnError("The interstitial isn't loaded");
            return;
        }

        IronSource.Agent.showInterstitial(interstitialPlacementId);
    }

    public void SetInterstitialListener(InterstitialAdsListener listener)
    {
        RemoveInterstitialListener();

        _interstitialListener = listener;
        IronSourceInterstitialEvents.onAdReadyEvent += InterstitialAdReadyEvent;
        IronSourceInterstitialEvents.onAdClosedEvent += InterstitialAdClosedEvent;
        IronSourceInterstitialEvents.onAdLoadFailedEvent += InterstitialAdLoadFailedEvent;
        IronSourceInterstitialEvents.onAdShowFailedEvent += InterstitialAdShowFailedEvent;
        IronSourceInterstitialEvents.onAdOpenedEvent += InterstitialAdOpenedEvent;
    }

    public void RemoveInterstitialListener()
    {
        if (_interstitialListener == null)
            return;

        _interstitialListener = null;
        IronSourceInterstitialEvents.onAdReadyEvent -= InterstitialAdReadyEvent;
        IronSourceInterstitialEvents.onAdClosedEvent -= InterstitialAdClosedEvent;
        IronSourceInterstitialEvents.onAdLoadFailedEvent -= InterstitialAdLoadFailedEvent;
        IronSourceInterstitialEvents.onAdShowFailedEvent -= InterstitialAdShowFailedEvent;
        IronSourceInterstitialEvents.onAdOpenedEvent -= InterstitialAdOpenedEvent;
    }

    private void InterstitialAdReadyEvent(IronSourceAdInfo adInfo) => _interstitialListener?.OnReady(interstitialPlacementId);
    private void InterstitialAdClosedEvent(IronSourceAdInfo adInfo) => _interstitialListener?.OnFinish(interstitialPlacementId);
    private void InterstitialAdOpenedEvent(IronSourceAdInfo adInfo) => _interstitialListener?.OnStart(interstitialPlacementId);
    private void InterstitialAdShowFailedEvent(IronSourceError error, IronSourceAdInfo adInfo) => _interstitialListener?.OnError(error.getDescription());
    private void InterstitialAdLoadFailedEvent(IronSourceError error) => _interstitialListener?.OnError(error.getDescription());
    #endregion

    #region RewardedVideo
    public void LoadRewardedVideo()
    {
        if (IronSource.Agent.isRewardedVideoAvailable())
        {
            _rewardedListener?.OnReady(rewardedVideoPlacementId);
            return;
        }
        _rewardedListener?.OnError("A rewarded video isn't available");
    }
    public bool IsRewardedVideoAvailable => IronSource.Agent.isRewardedVideoAvailable();

    public void ShowRewardedVideo()
    {
        IronSource.Agent.showRewardedVideo(rewardedVideoPlacementId);
    }

    public void SetRewardedVideoListener(RewardedVideoListener listener)
    {
        RemoveRewardedVideoListener();
        _rewardedListener = listener;
        IronSourceRewardedVideoEvents.onAdAvailableEvent += RewardedVideoAvailableEvent;
        IronSourceRewardedVideoEvents.onAdUnavailableEvent += RewardedVideoUnavailableEvent;
        IronSourceRewardedVideoEvents.onAdRewardedEvent += RewardedVideoAdRewardedEvent;
        IronSourceRewardedVideoEvents.onAdShowFailedEvent += RewardedVideoAdShowFailedEvent;
        IronSourceRewardedVideoEvents.onAdClosedEvent += RewardedVideoAdClosedEvent;
        IronSourceRewardedVideoEvents.onAdOpenedEvent += RewardedVideoAdOpenedEvent;
    }

    public void RemoveRewardedVideoListener()
    {
        if (_rewardedListener == null)
            return;

        _rewardedListener = null;
        IronSourceRewardedVideoEvents.onAdAvailableEvent -= RewardedVideoAvailableEvent;
        IronSourceRewardedVideoEvents.onAdUnavailableEvent -= RewardedVideoUnavailableEvent;
        IronSourceRewardedVideoEvents.onAdRewardedEvent -= RewardedVideoAdRewardedEvent;
        IronSourceRewardedVideoEvents.onAdShowFailedEvent -= RewardedVideoAdShowFailedEvent;
        IronSourceRewardedVideoEvents.onAdClosedEvent -= RewardedVideoAdClosedEvent;
        IronSourceRewardedVideoEvents.onAdOpenedEvent -= RewardedVideoAdOpenedEvent;
    }

    private void RewardedVideoAvailableEvent(IronSourceAdInfo adInfo) => _rewardedListener?.OnRewardedVideoAvailabilityChange?.Invoke(true);
    private void RewardedVideoUnavailableEvent() => _rewardedListener?.OnRewardedVideoAvailabilityChange?.Invoke(false);
    private void RewardedVideoAdRewardedEvent(IronSourcePlacement placement, IronSourceAdInfo adInfo) => _rewardedListener?.OnReward(placement);
    private void RewardedVideoAdShowFailedEvent(IronSourceError error, IronSourceAdInfo adInfo) => _rewardedListener?.OnError(error.getDescription());
    private void RewardedVideoAdOpenedEvent(IronSourceAdInfo adInfo) => _rewardedListener?.OnStart(rewardedVideoPlacementId);
    private void RewardedVideoAdClosedEvent(IronSourceAdInfo adInfo) => _rewardedListener?.OnFinish(rewardedVideoPlacementId);
    #endregion

}