using System.Text;
using gift_of_the_givers.Models;
using gift_of_the_givers.Services;

namespace GiftOfTheGivers.Tests;

// Checks the downloadable PDF certificate produced by the web app.
public class CertificatePdfTests
{
    private static readonly Donation SampleDonation = new()
    {
        Id = 42,
        DonorName = "Thandi Mokoena",
        Amount = 1250.5m,
        Currency = Currency.ZAR,
        DonationDate = new DateTime(2026, 9, 20, 8, 0, 0, DateTimeKind.Utc)
    };

    private static string BuildPdfText() => Encoding.ASCII.GetString(
        DonationCertificatePdfBuilder.Build(SampleDonation, "GOTG-000042", "Thandi Mokoena", new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc)));

    [Fact]
    public void Build_ProducesValidPdfFile()
    {
        var pdf = BuildPdfText();

        Assert.StartsWith("%PDF-1.4", pdf);
        Assert.EndsWith("%%EOF" + Environment.NewLine, pdf);
    }

    [Fact]
    public void Build_IncludesCertificateNumberDonorAndFormattedAmount()
    {
        var pdf = BuildPdfText();

        Assert.Contains("Certificate No: GOTG-000042", pdf);
        Assert.Contains("Donor Name: Thandi Mokoena", pdf);
        Assert.Contains("Amount: ZAR 1,250.50", pdf);
        Assert.Contains("Donation Date: 20 Sep 2026 08:00 UTC", pdf);
    }

    [Fact]
    public void Build_EscapesSpecialCharactersInDonorName()
    {
        var pdf = Encoding.ASCII.GetString(
            DonationCertificatePdfBuilder.Build(SampleDonation, "GOTG-000042", "Smith (Pty) Ltd", DateTime.UtcNow));

        Assert.Contains(@"Donor Name: Smith \(Pty\) Ltd", pdf);
    }
}
