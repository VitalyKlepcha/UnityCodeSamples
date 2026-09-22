using Firebase.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.GameFoundation;
using UnityEngine.GameFoundation.DefaultLayers;
using UnityEngine.GameFoundation.DefaultLayers.Persistence;
using UnityEngine.Promise;

public class GameFoundationWalletService : IWalletService
{
    private const string localPersistenceKey = "MoneyCutDatabase";
    private static PersistenceDataLayer dataLayer;

    private bool isInitialized;
    public bool IsInitialized { get => isInitialized; }

    public event WalletCurrencyIncreaseHandler OnWalletCurrencyIncrease;
    public event WalletCurrencyDecreaseHandler OnWalletCurrencyDecrease;

    public GameFoundationWalletService()
    {
        SetupGameFoundation();
    }

    private void SetupGameFoundation()
    {
        if (dataLayer != null) return;

        var dataSerializer = new JsonDataSerializer();
        var localPersistence = new LocalPersistence(localPersistenceKey, dataSerializer);
        dataLayer = new PersistenceDataLayer(localPersistence);

        using (Deferred initDeferred = GameFoundationSdk.Initialize(dataLayer))
        {
            initDeferred.Wait();

            if (initDeferred.isFulfilled)
            {
                OnInitSucceeded();
            }
            else
                OnInitFailed(initDeferred.error);
        }
    }

    private string GetCurrencyDefinitionKey(InGameCurrency currency) => currency.GetCurrencyId();
    private Currency GetCurrencyDefinition(InGameCurrency currency)
    {
        var definitionKey = GetCurrencyDefinitionKey(currency);
        Currency definition = GameFoundationSdk.catalog.Find<Currency>(definitionKey);
        if (definition is null)
        {
            Debug.Log($"WalletService: Definition {definitionKey} not found");
            return null;
        }
        return definition;
    }

    public int GetCurrencyValue(InGameCurrency currency)
    {
        long balance = GameFoundationSdk.wallet.Get(GetCurrencyDefinition(currency));
        return Convert.ToInt32(balance);
    }

    public bool HasCurrency(InGameCurrency currency, int value)
    {
        var currencyBalance = GetCurrencyValue(currency);
        return currencyBalance >= value;
    }

    public async Task<bool> IncreaseValue(InGameCurrency currency, int value)
    {
        if (Thread.CurrentThread.IsBackground)
        {
            return await Task.CompletedTask.ContinueWithOnMainThread((t) => IncreaseValueOnMainThread(currency, value));
        }

        return IncreaseValueOnMainThread(currency, value);
    }

    private bool IncreaseValueOnMainThread(InGameCurrency currency, int value)
    {
        var isSuccessful = ChangeValue(currency, value);

        if (isSuccessful)
        {
            OnWalletCurrencyIncrease?.Invoke(currency, value);
        }

        return isSuccessful;
    }

    public async Task<bool> DecreaseValue(InGameCurrency currency, int value, string purchaseName)
    {
        //string purchaseName will be used for analytics
        if (Thread.CurrentThread.IsBackground)
        {
            return await Task.CompletedTask.ContinueWithOnMainThread((t) => DecreaseValueOnMainThread(currency, value, purchaseName));
        }

        return DecreaseValueOnMainThread(currency, value, purchaseName);
    }

    public bool DecreaseValueOnMainThread(InGameCurrency currency, int value, string purchaseName)
    {
        var isSuccessful = ChangeValue(currency, -value);

        if (isSuccessful)
        {
            OnWalletCurrencyDecrease?.Invoke(currency, value);
        }

        return isSuccessful;
    }

    private bool ChangeValue(InGameCurrency currency, int value)
    {
        var currencyDefinition = GetCurrencyDefinition(currency);
        var currencyBalance = GetCurrencyValue(currency);
        // Return false if there are insufficient funds
        if (value < 0 && currencyBalance < Math.Abs(value))
        {
            return false;
        }
        var newValue = currencyBalance + value;
        bool success = GameFoundationSdk.wallet.Set(currencyDefinition, newValue);
        if (!success)
        {
            Debug.LogError($"WalletService: Failed in setting a new value for '{currencyDefinition.displayName}'");
            return false;
        }

        Save();

        return true;
    }
    private void Save() => dataLayer.Save();


    // Called when Game Foundation is successfully initialized.
    void OnInitSucceeded()
    {
        isInitialized = true;
        Debug.Log("Game Foundation is successfully initialized");
    }

    // Called if Game Foundation initialization fails 
    void OnInitFailed(Exception error)
    {
        Debug.LogException(error);
    }
}