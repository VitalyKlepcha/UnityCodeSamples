using Zenject;
using UnityEngine;
using System.Threading;
using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
#if UNITY_IOS
using Balaso;
#endif

public class AdsManager : DontDestroyOnLoadSingletone<AdsManager>, IAdsManager
{
    private bool IsShowAds { get; set; } = false;
    private bool IsShowBanner { get; set; } = false;
    public bool IsShowBannerContainer { get; private set; } = false;
    public bool IsLoadingScreen { get; set; }

    private IAdsService _adsService;
    private IRemoteConfig _remoteConfig;

    private bool _isRemoteShowAds;

    private int _timeout = 5000;

    [Inject]
    private void Setup(IAdsService adsService, IRemoteConfig remoteConfig)
    {
        _adsService = adsService;
        _remoteConfig = remoteConfig;

        if (_remoteConfig.IsRemoteDataFetched)
            _isRemoteShowAds = _remoteConfig.GetBool(Parameter.IsShowAds);
        else
        {
            _remoteConfig.OnRemoteDataFetched += () =>
            {
                _isRemoteShowAds = _remoteConfig.GetBool(Parameter.IsShowAds);
            };
        }

    }

    public override void Awake()
    {
        base.Awake();

#if UNITY_IOS
		AppTrackingTransparency.RegisterAppForAdNetworkAttribution();
#endif
    }

    private void Update()
    {
        IsShowAds = CheckIsShowAds();
#if UNITY_EDITOR
        return;
#endif
        var newIsShowBannerContainer = CheckIsShowBannerContainer();
        if (newIsShowBannerContainer != IsShowBannerContainer)
        {
            IsShowBannerContainer = newIsShowBannerContainer;
        }

        var newIsShowBanner = CheckIsShowBanner();
        if (newIsShowBanner != IsShowBanner)
        {
            IsShowBanner = newIsShowBanner;
            UpdateBanner(newIsShowBanner);
        }
    }

    private void Start()
    {
#if UNITY_IOS
		RequestTrackingAuthorization();
#endif
        SetUpAdsService();
    }

    private void SetUpAdsService()
    {
        _adsService.Init();
        UpdateBanner(false);
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        return;
#endif
        _adsService?.RemoveBannerListener();
    }

    private void OnApplicationPause(bool isPaused)
    {
        _adsService?.OnApplicationPause(isPaused);
    }

    #region Banner
    private bool CheckIsShowAds() => _isRemoteShowAds;

    private bool CheckIsShowBannerContainer() => IsShowAds && _adsService.IsBannerAvailable;

    //Add constaints on demand(loading screen, etc.)
    private bool CheckIsShowBanner() => IsShowBannerContainer && !IsLoadingScreen;

    private void UpdateBanner(bool isShowBanner)
    {
        if (isShowBanner)
            ShowBanner();
        else
            HideBanner();
    }

    private void ShowBanner()
    {
        if (!_adsService.IsInitialized)
            _adsService.Init();

        _adsService.SetBannerListener(new BannerListener()
        {
            OnReady = (_) =>
            {
                if (IsShowBanner)
                    _adsService.ShowBanner();

                _adsService.RemoveBannerListener();
            }
        });
        _adsService.LoadBanner();

    }

    private void HideBanner()
    {
        _adsService.HideBanner();
    }
    #endregion

    #region Interstitial
    private InterstitialAdsListener _interstitialAdsListener;

    public async void ShowInterstitial(CancellationToken cancellationToken)
    {
#if UNITY_EDITOR
        _interstitialAdsListener?.OnError("Can't show interstitial in editor");
        return;
#endif
        if (!CheckIsShowAds())
        {
            _interstitialAdsListener?.OnError("Show ads is disabled");
            return;
        }

        if (!_adsService.IsInitialized)
            _adsService.Init();

        bool isTimeout = false;

        if (_interstitialAdsListener != null)
            _interstitialAdsListener.OnReady += ShowInterstitial;

        _adsService.LoadInterstitial();

        try
        {
            await UniTask
                .WaitUntilCanceled(cancellationToken)
                .Timeout(TimeSpan.FromMilliseconds(_timeout));
        }
        catch (TimeoutException)
        {
            isTimeout = true;
        }
        catch (OperationCanceledException) { }


        if (isTimeout && _interstitialAdsListener != null)
        {
            _interstitialAdsListener?.OnError("The request is timed out");
        }
    }
    public void SetInterstitialListener(InterstitialAdsListener listener)
    {
        _interstitialAdsListener = listener;
        _adsService.SetInterstitialListener(listener);
    }

    public void RemoveInterstitialListener()
    {
        _interstitialAdsListener = null;
        if (_adsService is null) return;
        _adsService.RemoveInterstitialListener();
    }

    private void ShowInterstitial(string placement)
    {
        if (placement != _adsService.InterstitialPlacementId)
            return;

        _adsService.ShowInterstitial();
    }
    #endregion

    #region RewardedVideo
    private RewardedVideoListener _rewardedListener;

    public void RemoveRewardedOnReadyListener()
    {
        if (_rewardedListener == null)
            return;
        _rewardedListener.OnReady = null;
    }

    public async void ShowRewardedVideo(CancellationToken cancellationToken)
    {
#if UNITY_EDITOR
_rewardedListener?.OnError("Can't show rewarded in editor");
        return;
#endif
        if (!CheckIsShowAds())
        {
            _rewardedListener?.OnError("Show ads is disabled");
            return;
        }
        if (!_adsService.IsInitialized)
            _adsService.Init();


        bool isTimeout = false;
        if (_rewardedListener != null)
            _rewardedListener.OnReady += ShowRewardedVideo;

        _adsService.LoadRewardedVideo();

        try
        {
            await UniTask
                .WaitUntilCanceled(cancellationToken)
                .Timeout(TimeSpan.FromMilliseconds(_timeout));
        }
        catch (TimeoutException)
        {
            isTimeout = true;
        }
        catch (OperationCanceledException) { }



        if (isTimeout && _rewardedListener != null)
        {
            _rewardedListener?.OnError("The request is timed out");
        }
    }

    public void SetRewardedVideoListener(RewardedVideoListener listener)
    {
        if (_adsService is null)
        {
            return;
        }
        _rewardedListener = listener;
        _adsService.SetRewardedVideoListener(listener);
    }

    public void RemoveRewardedVideoListener()
    {
        _rewardedListener = null;
        if (_adsService is null)
            return;
        _adsService.RemoveRewardedVideoListener();
    }

    public bool IsRewardedVideoAvailable()
    {
        return !(_adsService is null) && _adsService.IsRewardedVideoAvailable;
    }
    private void ShowRewardedVideo(string placement)
    {
        if (placement != _adsService.RewardedVideoPlacementId)
            return;
        _adsService.ShowRewardedVideo();
    }
    #endregion

#if UNITY_IOS
    private void RequestTrackingAuthorization()
    {
		AppTrackingTransparency.OnAuthorizationRequestDone += OnAuthorizationRequestDone;

		var currentStatus = AppTrackingTransparency.TrackingAuthorizationStatus;
		if (currentStatus != AppTrackingTransparency.AuthorizationStatus.AUTHORIZED)
		{
			AppTrackingTransparency.RequestTrackingAuthorization();
		}
	}

	private void OnAuthorizationRequestDone(AppTrackingTransparency.AuthorizationStatus status)
	{
		Log.Debug("ATT request done with status " + status);
	}
#endif
}