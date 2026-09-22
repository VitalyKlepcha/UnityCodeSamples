public class InterstitialAdsListener
{
    public ReadyCallback OnReady { get; set; }
    public FinishCallback OnFinish { get; set; }
    public ErrorCallback OnError { get; set; }
    public StartCallback OnStart { get; set; }

    public delegate void ReadyCallback(string placementId);
    public delegate void FinishCallback(string placementId);
    public delegate void ErrorCallback(string message);
    public delegate void StartCallback(string placementId);
}