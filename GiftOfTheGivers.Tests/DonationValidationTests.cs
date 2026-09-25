using System.ComponentModel.DataAnnotations;
using gift_of_the_givers.Models;

namespace GiftOfTheGivers.Tests;

// Checks the validation rules on the Donation model that protect the donation form.
public class DonationValidationTests
{
    private static List<ValidationResult> Validate(Donation donation)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(donation, new ValidationContext(donation), results, validateAllProperties: true);
        return results;
    }

    private static Donation ValidDonation() => new()
    {
        DonorName = "Thandi Mokoena",
        DonorEmail = "thandi@example.com",
        Amount = 500m,
        Currency = Currency.ZAR,
        Frequency = DonationFrequency.OneTime
    };

    [Fact]
    public void ValidDonation_PassesValidation()
    {
        var results = Validate(ValidDonation());

        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9.99)]
    [InlineData(-50)]
    public void AmountBelowMinimum_FailsValidation(decimal amount)
    {
        var donation = ValidDonation();
        donation.Amount = amount;

        var results = Validate(donation);

        var error = Assert.Single(results);
        Assert.Contains(nameof(Donation.Amount), error.MemberNames);
        Assert.Equal("Please enter a donation amount of at least 10.", error.ErrorMessage);
    }

    [Fact]
    public void MissingDonorName_FailsValidation()
    {
        var donation = ValidDonation();
        donation.DonorName = "";

        var results = Validate(donation);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Donation.DonorName)));
    }

    [Fact]
    public void InvalidEmail_FailsValidation()
    {
        var donation = ValidDonation();
        donation.DonorEmail = "not-an-email";

        var results = Validate(donation);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Donation.DonorEmail)));
    }

    [Fact]
    public void NewDonation_DefaultsToZarOneTime()
    {
        var donation = new Donation();

        Assert.Equal(Currency.ZAR, donation.Currency);
        Assert.Equal(DonationFrequency.OneTime, donation.Frequency);
    }
}
