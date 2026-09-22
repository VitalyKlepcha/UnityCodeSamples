using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum InGameCurrency
{
    Coin = 1,
    Diamond = 2
}

public static class InGameCurrencyExtensions
{
    public static string GetCurrencyId(this InGameCurrency currency)
    {
        switch (currency)
        {
            case InGameCurrency.Coin: return "coin";
            case InGameCurrency.Diamond: return "diamond";
            default: throw new ArgumentException($"Can't find currency id for {currency}");
        }
    }
}

public class Purchase
{
    public InGameCurrency Currency { get; set; } = InGameCurrency.Diamond;
    public int Price { get; set; }
}