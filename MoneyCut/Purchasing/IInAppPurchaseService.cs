
public interface IInAppPurchaseService 
{
    PurchaseIdSuccessHandler PurchaseSuccessHandler { get; set; }
    PurchaseFailHandlerWithReason PurchaseFailHandler { get; set; }

    void InitializePurchasing(InAppPurchase[] products);

    bool IsInitialized();

    void BuyProduct(string productId);

    string GetProductName(string productId);
    decimal GetProductPriceFromStore(string productId);
    string GetProductLocalizedPriceFromStore(string productId);
}
