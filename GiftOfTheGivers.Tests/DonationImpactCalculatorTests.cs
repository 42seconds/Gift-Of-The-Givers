using gift_of_the_givers.Models;
using gift_of_the_givers.Services;

namespace GiftOfTheGivers.Tests;

// Checks the donation impact calculation shown in the "Your impact" box on the donation form.
public class DonationImpactCalculatorTests
{
    [Fact]
    public void OneTimeZarDonation_SplitsIntoReliefItems()
    {
        var lines = DonationImpactCalculator.Calculate(500m, Currency.ZAR, DonationFrequency.OneTime);

        Assert.Collection(lines,
            line => { Assert.Equal(1, line.Quantity); Assert.Equal("food parcel for a family", line.Label); },
            line => { Assert.Equal(4, line.Quantity); Assert.Equal("hot meals", line.Label); });
    }

    [Theory]
    [InlineData(100, Currency.USD, 1800)]
    [InlineData(100, Currency.EUR, 2000)]
    [InlineData(100, Currency.ZAR, 100)]
    public void ToZar_ConvertsUsingPrototypeRates(decimal amount, Currency currency, decimal expectedZar)
    {
        Assert.Equal(expectedZar, DonationImpactCalculator.ToZar(amount, currency));
    }

    [Fact]
    public void RecurringDonation_IsAnnualisedOverTwelveMonths()
    {
        var total = DonationImpactCalculator.AnnualisedZar(250m, Currency.ZAR, DonationFrequency.Recurring);

        Assert.Equal(3000m, total);
    }

    [Fact]
    public void CalculatedItems_NeverCostMoreThanTheDonation()
    {
        var lines = DonationImpactCalculator.Calculate(5000m, Currency.ZAR, DonationFrequency.OneTime);
        var costs = DonationImpactCalculator.Items.ToDictionary(i => i.Plural, i => i.CostZar);
        var singular = DonationImpactCalculator.Items.ToDictionary(i => i.Singular, i => i.CostZar);

        var spent = lines.Sum(l => l.Quantity * (costs.TryGetValue(l.Label, out var c) ? c : singular[l.Label]));

        Assert.True(spent <= 5000m);
        Assert.True(spent > 5000m - 25m, "Less than one hot meal should be left unallocated.");
    }

    [Fact]
    public void TinyDonation_ProducesNoWholeItems()
    {
        var lines = DonationImpactCalculator.Calculate(10m, Currency.ZAR, DonationFrequency.OneTime);

        Assert.Empty(lines);
    }
}
