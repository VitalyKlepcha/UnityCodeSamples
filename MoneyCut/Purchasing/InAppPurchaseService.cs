using System;
using System.Linq;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

public class InAppPurchaseService : IInAppPurchaseService, IStoreListener
{
    private static IExtensionProvider _storeExtensionProvider;
    private static IStoreController _storeController;

    public PurchaseIdSuccessHandler PurchaseSuccessHandler { get; set; }
    public PurchaseFailHandlerWithReason PurchaseFailHandler { get; set; }

    public async void InitializePurchasing(InAppPurchase[] products)
    {
        if (IsInitialized())
        {
            return;
        }
        //Initialize UGS
        var options = new InitializationOptions()
               .SetEnvironmentName("production");

        await UnityServices.InitializeAsync(options);

#if UNITY_EDITOR
        StandardPurchasingModule.Instance().useFakeStoreAlways = true;
        StandardPurchasingModule.Instance().useFakeStoreUIMode = FakeStoreUIMode.Default;
#endif
        // Create a builder, first passing in a suite of Unity provided stores.
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        foreach (var product in products)
        {
            var unityId = product.GetProductId();
            var appStoreId = product.GetIosProductId();
            var googlePlayId = product.GetAndroidProductId();
            var type = product.GetProductType();

            if (googlePlayId == null && appStoreId == null)
            {
                builder.AddProduct(unityId, type);
                continue;
            }

            var ids = new IDs() {
                { appStoreId ?? unityId, AppleAppStore.Name },
                { googlePlayId ?? unityId, GooglePlay.Name }
            };
            builder.AddProduct(unityId, type, ids);
        }
        UnityPurchasing.Initialize(this, builder);
    }

    public bool IsInitialized()
    {
        return _storeController != null && _storeExtensionProvider != null;
    }

    public void BuyProduct(string productId)
    {
        if (!IsInitialized()) { return; }

        Product product = _storeController.products.WithID(productId);

        if (product == null || !product.availableToPurchase) { return; }

        Debug.Log(string.Format("Purchasing product: '{0}'", product.definition.id));
        _storeController.InitiatePurchase(product);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        // Overall Purchasing system, configured with products for this application.
        _storeController = controller;
        // Store specific subsystem, for accessing device-specific store features.
        _storeExtensionProvider = extensions;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log($"In-App Purchasing initialize failed: {error}");
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.Log($"In-App Purchasing initialize failed: {error}" + " " +  "with message:" + message);
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.Log($"Purchase failed - Product: '{product.definition.id}', PurchaseFailureReason: {failureReason}");
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEventArgs)
    {
        bool isValidPurchase = ValidatePurchase(purchaseEventArgs.purchasedProduct);
        var purchasedProductStoreId = purchaseEventArgs.purchasedProduct.definition.storeSpecificId;
        if (isValidPurchase)
        {

            PurchaseSuccessHandler?.Invoke(purchasedProductStoreId);
        }
        else
        {
            PurchaseFailHandler?.Invoke(purchasedProductStoreId, PurchaseFailureReason.Unknown);
        }


        return PurchaseProcessingResult.Complete;
    }
    #region Validation
    private bool ValidatePurchase(Product product)
    {
#if UNITY_EDITOR
        return true;
#else
        return ValidateAppleAndGooglePlayProduct(product);
#endif
    }

    private bool ValidateAppleAndGooglePlayProduct(Product product)
    {
        try
        {
            var validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);
            var purchaseReceipts = validator.Validate(product.receipt);
            return purchaseReceipts.Any(purchaseReceipt =>
                purchaseReceipt.productID == product.definition.storeSpecificId
                && IsPurchaseReceiptValid(purchaseReceipt, product.definition.type));
        }
        catch (IAPSecurityException)
        {
            Debug.Log("Invalid receipt, not unlocking content");
            return false;
        }
    }
    private bool IsPurchaseReceiptValid(IPurchaseReceipt productReceipt, ProductType productType)
    {
        switch (productReceipt)
        {
            case AppleInAppPurchaseReceipt appleReceipt:
                return appleReceipt.IsPurchaseValid(productType);
            case GooglePlayReceipt googleReceipt:
                return googleReceipt.IsPurchaseValid();
            default:
                return false;
        }
    }
    #endregion

    public string GetProductName(string productId)
    {
        var productMetadata = GetProductMetadata(productId);
        if (productMetadata == null)
            return null;

        return productMetadata.localizedTitle;
    }


    public decimal GetProductPriceFromStore(string productId)
    {
        var productMetadata = GetProductMetadata(productId);
        if (productMetadata == null)
            return 0;

        return productMetadata.localizedPrice;
    }


    public string GetProductLocalizedPriceFromStore(string productId)
    {
        var productMetadata = GetProductMetadata(productId);
        if (productMetadata == null)
            return "";

        return productMetadata.localizedPriceString;
    }


    private ProductMetadata GetProductMetadata(string productId)
    {
        if (_storeController != null && _storeController.products != null
            && _storeController.products.WithID(productId) != null)
            return _storeController.products.WithID(productId).metadata;

        return null;
    }

}
