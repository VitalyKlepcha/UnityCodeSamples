public class BannerListener
{
    public ReadyCallback OnReady { get; set; }
    public ErrorCallback OnError { get; set; }

    public delegate void ReadyCallback(IronSourceAdInfo info);
    public delegate void ErrorCallback(string message);
}