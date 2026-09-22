using System;
using System.Collections.Generic;

public enum Parameter
{
    IsShowAds,
    LevelsLoadingConfig
}

public static class Parameters
{
    public static string GetKey(this Parameter param)
    {
        switch (param)
        {
            case Parameter.IsShowAds: return "shows_ads";
            case Parameter.LevelsLoadingConfig: return "levels_loading_config";

            default: throw new ArgumentException($"Parameter {param:f} not supported by RemoteConfig");
        }
    }

    public static Dictionary<string, object> GetDefaults()
    {
        return new Dictionary<string, object>
        {
            [Parameter.IsShowAds.GetKey()] = true,
            [Parameter.LevelsLoadingConfig.GetKey()] = "{\"loadNumber\":5,\"leftBeforeLoadNumber\":1,\"batchSize\":0,\"isLoadAllRemote\":false,\"isUpdatePrevious\":false,\"isUpdateNext\":false}",
        };
    }
}
