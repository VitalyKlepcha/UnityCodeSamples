using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class RewardedVideoListener
{
    public ReadyCallback OnReady { get; set; }
    public RewardCallback OnReward { get; set; }
    public FinishCallback OnFinish { get; set; }
    public ErrorCallback OnError { get; set; }
    public StartCallback OnStart { get; set; }
    public Action<bool> OnRewardedVideoAvailabilityChange { get; set; }

    public delegate void ReadyCallback(string placementId);
    public delegate void RewardCallback(IronSourcePlacement placement);
    public delegate void FinishCallback(string placementId);
    public delegate void ErrorCallback(string message);
    public delegate void StartCallback(string placementId);
}