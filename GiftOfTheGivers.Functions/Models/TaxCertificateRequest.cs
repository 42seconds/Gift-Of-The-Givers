namespace GiftOfTheGivers.Functions.Models;

// Donation details sent by the web app (or Postman) when a donor completes the donation form.
public class TaxCertificateRequest
{
    public int DonationId { get; set; }

    public string? DonorName { get; set; }

    public string? DonorEmail { get; set; }

    public bool IsAnonymous { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "ZAR";

    public string Frequency { get; set; } = "OneTime";

    public DateTime? DonationDate { get; set; }
}
