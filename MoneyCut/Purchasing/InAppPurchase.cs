using UnityEngine;
using UnityEngine.Purchasing;

public enum InAppPurchase 
{
    Coins,
    Coins100,
    Diamonds,
    Diamonds10
}

static class InAppPurchaseMethods
{
    public static ProductType GetProductType(this InAppPurchase purchase)
    {
        switch (purchase)
        {
            default:
                return ProductType.Consumable;
        }
    }

    public static string GetProductId(this InAppPurchase purchase)
    {
        return purchase.ToString();
    }

    public static string GetIosProductId(this InAppPurchase purchase)
    {
        switch (purchase)
        {
            default:
                return purchase.ToString();
        }
    }

    public static string GetAndroidProductId(this InAppPurchase purchase)
    {
        switch (purchase)
        {
            default:
            return purchase.ToString();
    }
    }

    /// <returns>null if sprite is not set</returns>
    public static Sprite GetSprite(this InAppPurchase purchase, ShopSpritePreset spritePreset)
    {
        return spritePreset.items[purchase];
    }
    public static string GetShopName(this InAppPurchase purchase)
    {
        switch (purchase)
        {
            case InAppPurchase.Coins:
                return "1 coin";
            case InAppPurchase.Coins100:
                return "100 coins";
            case InAppPurchase.Diamonds:
                return "1 diamond";
            case InAppPurchase.Diamonds10:
                return "10 diamonds";
            default:return "";
        }
    }

}