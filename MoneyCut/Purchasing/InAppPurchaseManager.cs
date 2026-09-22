using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;

public delegate void PurchaseIdSuccessHandler(string purchaseId);
public delegate void PurchaseSuccessHandler(InAppPurchase purchase);
public delegate void PurchaseFailHandlerWithReason(string purchaseId, PurchaseFailureReason failureReason);

public class InAppPurchaseManager : IInAppPurchaseManager
{
    IInAppPurchaseService _purchaseService;
    IWalletService _walletService;

    private readonly InAppPurchase[] products = {
        InAppPurchase.Coins,
        InAppPurchase.Coins100,
        InAppPurchase.Diamonds,
        InAppPurchase.Diamonds10
    };

    public PurchaseSuccessHandler PurchaseSuccessHandler { get; set; }
    public Action OnCurrencyPurchased { get; set; }
    public InAppPurchase[] Products { get => products;  }

    public InAppPurchaseManager(IInAppPurchaseService purchaseService, IWalletService walletService)
    {
        _purchaseService = purchaseService;
        _walletService = walletService;

        if (IsInitialized() && _purchaseService != null)
            return;

        _purchaseService.PurchaseSuccessHandler = HandlePurchaseSuccess;
        _purchaseService.PurchaseFailHandler = HandlePurchaseFail;
        _purchaseService.InitializePurchasing(products);
    }

    public void BuyProduct(InAppPurchase purchase)
    {
        var productId = purchase.GetProductId();
        _purchaseService.BuyProduct(productId);
    }

    public bool IsInitialized()
    {
        if (_purchaseService == null)
            return false;

        return _purchaseService.IsInitialized();
    }

    private void HandlePurchaseSuccess(string purchaseId)
    {
        Debug.Log("Successfully purchased product with id: " + purchaseId);
        var inAppPurchase = GetPurchase(purchaseId);
        if (!(inAppPurchase is InAppPurchase purchase))
            return;
        HandleSuccessfulCurrencyPurchase(purchase);
        PurchaseSuccessHandler?.Invoke(purchase);
    }

    private void HandlePurchaseFail(string purchaseId, PurchaseFailureReason failureReason)
    {
        var inAppPurchase = GetPurchase(purchaseId);
        Debug.Log("Purchase for: " + purchaseId + " failed with " + failureReason);
    }

    private void HandleSuccessfulCurrencyPurchase(InAppPurchase inAppPurchase)
    {
        var purchase = GetCurrencyPurchase(inAppPurchase);
        if (purchase == null)
            return;

        var currency = purchase.Currency;
        _walletService.IncreaseValue(currency, purchase.Price);
        OnCurrencyPurchased?.Invoke();
    }

    public Purchase GetCurrencyPurchase(InAppPurchase product)
    {
        switch (product)
        {
            case InAppPurchase.Coins:
                return new Purchase()
                {
                    Currency = InGameCurrency.Coin,
                    Price = 1
                };    
            case InAppPurchase.Diamonds:
                return new Purchase()
                {
                    Currency = InGameCurrency.Diamond,
                    Price = 1
                };
            case InAppPurchase.Diamonds10:
                var numberString = product.ToString().Replace("Diamonds", string.Empty);
                var number = int.Parse(numberString);
                return new Purchase()
                {
                    Currency = InGameCurrency.Diamond,
                    Price = number
                };
            case InAppPurchase.Coins100:
                var numberStringCoins = product.ToString().Replace("Coins", string.Empty);
                var numberCoins = int.Parse(numberStringCoins);
                return new Purchase()
                {
                    Currency = InGameCurrency.Coin,
                    Price = numberCoins
                };
            default:
                return null;
        }
    }

    private InAppPurchase? GetPurchase(string id)
    {
#if UNITY_EDITOR
        return products.FirstOrDefault(p => p.GetProductId() == id);
#elif UNITY_IOS
        return products.FirstOrDefault(p => { 
            var iosId = p.GetIosProductId() ?? p.GetProductId();

            return id == iosId;
        });
#elif UNITY_ANDROID
            return products.FirstOrDefault(p => {
                var androidId = p.GetAndroidProductId() ?? p.GetProductId();

                return id == androidId;
            });
#else
        return null;
#endif
    }

    public string GetProductName(string productId) => _purchaseService.GetProductName(productId);


    public decimal GetProductPriceFromStore(string productId) => _purchaseService.GetProductPriceFromStore(productId);

    public string GetProductLocalizedPriceFromStore(string productId) => _purchaseService.GetProductLocalizedPriceFromStore(productId);

}