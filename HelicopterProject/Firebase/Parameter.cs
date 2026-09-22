using System;
using System.Collections.Generic;

public enum Parameter
{
    CollectionItemGenerationInterval,
    CollectionItemLongLifeTime,
    CollectionItemNormalLifeTime,
    QREncodedName,
    ScanUI,
    SecondsBeforeRotation,
    ShopUI,
    Slides,
    TopRightUI,
    PilotLevelConfig,
    ForceGenerationItemCount
}

public static class Parameters
{
    public static string GetKey(this Parameter param)
    {
        switch (param)
        {
            case Parameter.CollectionItemGenerationInterval: return "collectionitem_generation_interval";
            case Parameter.CollectionItemLongLifeTime: return "collectionitem_long_life_time";
            case Parameter.CollectionItemNormalLifeTime: return "collectionitem_normal_life_time";
            case Parameter.QREncodedName: return "qr_encoded_name";
            case Parameter.ScanUI: return "scan_ui";
            case Parameter.SecondsBeforeRotation: return "seconds_before_rotation";
            case Parameter.ShopUI: return "shop_ui";
            case Parameter.Slides: return "slides";
            case Parameter.TopRightUI: return "top_right_ui";
            case Parameter.PilotLevelConfig: return "pilot_level_config";
            case Parameter.ForceGenerationItemCount: return "force_generation_item_count";

            default: throw new ArgumentException($"Parameter {param:f} not supported by RemoteConfig");
        }
    }

    public static Dictionary<string, object> GetDefaults()
    {
        return new Dictionary<string, object>
        {
            [Parameter.CollectionItemGenerationInterval.GetKey()] = 10,
            [Parameter.CollectionItemLongLifeTime.GetKey()] = 300,
            [Parameter.CollectionItemNormalLifeTime.GetKey()] = 30,
            [Parameter.QREncodedName.GetKey()] = "MVM.Kids.5DHelicopter",
            [Parameter.ScanUI.GetKey()] = true,
            [Parameter.SecondsBeforeRotation.GetKey()] = 7,
            [Parameter.ShopUI.GetKey()] = false,
            [Parameter.Slides.GetKey()] = true,
            [Parameter.TopRightUI.GetKey()] = "education",
            [Parameter.PilotLevelConfig.GetKey()] = "{\"MutualScoreDivider\":0.5,\"TrickScoreDivider\":20,\"InitialXpToLevelUp\":100," +
            "\"XpToLevelUpMultiplier\":1.5,\"ShowLevelUpPopUp\":true,\"DecreaseLevel\":false}",
            [Parameter.ForceGenerationItemCount.GetKey()] = 3,
        };
    }
}
