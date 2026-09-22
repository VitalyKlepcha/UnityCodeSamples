using System.Threading;

public interface IAdsManager
{
    bool IsShowBannerContainer { get; }
    bool IsLoadingScreen { get; set; }
    void ShowInterstitial(CancellationToken cancellationToken);
    void SetInterstitialListener(InterstitialAdsListener listener);
    void RemoveInterstitialListener();

    void ShowRewardedVideo(CancellationToken cancellationToken);
    void SetRewardedVideoListener(RewardedVideoListener listener);
    void RemoveRewardedVideoListener();
    bool IsRewardedVideoAvailable();
}