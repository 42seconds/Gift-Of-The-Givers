using gift_of_the_givers.Models;

namespace gift_of_the_givers.Services;

// One relief item a donation can pay for, e.g. a food parcel costing R400.
public record ImpactItem(string Key, string Singular, string Plural, decimal CostZar);

// One line of the calculated impact, e.g. "2 x food parcels for a family".
public record ImpactLine(string Label, int Quantity);

// Turns a donation amount into tangible relief outcomes so donors can see what their gift achieves.
// The same figures are sent to the browser so the donation form can update the estimate live as the donor types.
public static class DonationImpactCalculator
{
    // Prototype exchange rates to ZAR. A later phase could load live rates instead.
    public static readonly IReadOnlyDictionary<string, decimal> RatesToZar = new Dictionary<string, decimal>
    {
        ["ZAR"] = 1m,
        ["USD"] = 18m,
        ["EUR"] = 20m
    };

    // Indicative costs of common relief items, most expensive first so large gifts are allocated to bigger items first.
    public static readonly IReadOnlyList<ImpactItem> Items = new List<ImpactItem>
    {
        new("medical", "emergency medical kit", "emergency medical kits", 1000m),
        new("food", "food parcel for a family", "food parcels for families", 400m),
        new("water", "month of clean water for a family", "months of clean water for a family", 150m),
        new("meal", "hot meal", "hot meals", 25m)
    };

    public static decimal ToZar(decimal amount, Currency currency)
    {
        return amount * RatesToZar[currency.ToString()];
    }

    // Recurring donations are treated as monthly, so the estimate shows a full year of giving.
    public static decimal AnnualisedZar(decimal amount, Currency currency, DonationFrequency frequency)
    {
        var zar = ToZar(amount, currency);
        return frequency == DonationFrequency.Recurring ? zar * 12 : zar;
    }

    // Splits the amount across the relief items: at most half of the gift goes to any one item
    // (while it can still buy one), so the donor sees a mix of outcomes rather than a single number.
    public static IReadOnlyList<ImpactLine> Calculate(decimal amount, Currency currency, DonationFrequency frequency)
    {
        var remaining = AnnualisedZar(amount, currency, frequency);
        var total = remaining;
        var lines = new List<ImpactLine>();

        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            var isLast = i == Items.Count - 1;
            var budget = isLast ? remaining : Math.Max(total / 2, item.CostZar);
            var quantity = (int)Math.Floor(Math.Min(budget, remaining) / item.CostZar);

            if (quantity > 0)
            {
                lines.Add(new ImpactLine(quantity == 1 ? item.Singular : item.Plural, quantity));
                remaining -= quantity * item.CostZar;
            }
        }

        return lines;
    }
}
