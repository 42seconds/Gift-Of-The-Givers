namespace GiftOfTheGivers.Functions.Models;

// Dummy Section 18A-style tax certificate returned by the GenerateTaxCertificate function.
public class TaxCertificate
{
    public string CertificateNumber { get; set; } = string.Empty;

    public int DonationId { get; set; }

    public string DonorName { get; set; } = string.Empty;

    public string? DonorEmail { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "ZAR";

    public string Frequency { get; set; } = "OneTime";

    public DateTime DonationDateUtc { get; set; }

    public DateTime IssuedOnUtc { get; set; }

    public string TaxYear { get; set; } = string.Empty;

    public string Organisation { get; set; } = "Gift of the Givers Foundation";

    public string Section18AReference { get; set; } = "PLACEHOLDER-18A-0000";

    public bool IsPlaceholder { get; set; } = true;

    public string CertificateText { get; set; } = string.Empty;

    public string GeneratedBy { get; set; } = "Azure Function: GenerateTaxCertificate";
}
