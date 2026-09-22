using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IAdsService
{
    bool IsInitialized { get; }

    void Init();
    void OnApplicationPause(bool isPaused);


    string BannerPlacementId { get; }
    string InterstitialPlacementId { get; }
    string RewardedVideoPlacementId { get; }

    #region Banner
    bool IsBannerAvailable { get; }
    void LoadBanner();
    void ShowBanner();
    void HideBanner();
    void SetBannerListener(BannerListener listener);
    void RemoveBannerListener();
    #endregion

    #region Interstitial

    bool IsInterstitialAvailable { get; }
    void ShowInterstitial();
    void LoadInterstitial();
    void SetInterstitialListener(InterstitialAdsListener listener);
    void RemoveInterstitialListener();
    #endregion

    #region Rewarded
    bool IsRewardedVideoAvailable { get; }
    void LoadRewardedVideo();
    void ShowRewardedVideo();
    void SetRewardedVideoListener(RewardedVideoListener listener);
    void RemoveRewardedVideoListener();
    #endregion
}