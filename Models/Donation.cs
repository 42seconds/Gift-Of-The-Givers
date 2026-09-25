using System.ComponentModel.DataAnnotations;

namespace gift_of_the_givers.Models;

public enum Currency
{
    ZAR,
    USD,
    EUR
}

public enum DonationFrequency
{
    OneTime,
    Recurring
}

public class Donation
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string DonorName { get; set; } = "Anonymous Donor";

    [EmailAddress]
    public string? DonorEmail { get; set; }

    public bool IsAnonymous { get; set; }

    public string? UserId { get; set; }

    // Compare as decimal: the int overload rounds, which let amounts like 9.99 through.
    [Range(typeof(decimal), "10", "1000000", ParseLimitsInInvariantCulture = true, ErrorMessage = "Please enter a donation amount of at least 10.")]
    public decimal Amount { get; set; }

    public Currency Currency { get; set; } = Currency.ZAR;

    public DonationFrequency Frequency { get; set; } = DonationFrequency.OneTime;

    public DateTime DonationDate { get; set; } = DateTime.UtcNow;
}
