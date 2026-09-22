using System.Threading.Tasks;

public delegate void WalletCurrencyIncreaseHandler(InGameCurrency currency, int value);
public delegate void WalletCurrencyDecreaseHandler(InGameCurrency currency, int value);

public interface IWalletService
{
    event WalletCurrencyIncreaseHandler OnWalletCurrencyIncrease;
    event WalletCurrencyDecreaseHandler OnWalletCurrencyDecrease;

    bool IsInitialized { get; }

    bool HasCurrency(InGameCurrency currency, int value);
    int GetCurrencyValue(InGameCurrency currency);

    Task<bool> IncreaseValue(InGameCurrency currency, int value);
    Task<bool> DecreaseValue(InGameCurrency currency, int value, string purchaseName);
}