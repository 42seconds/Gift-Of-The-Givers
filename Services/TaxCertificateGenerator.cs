using GiftOfTheGivers.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GiftOfTheGivers.Services
{
    // Produces a simple, clearly-labelled PLACEHOLDER tax certificate.
    // Not a legally valid tax document - for prototype demonstration only.
    public static class TaxCertificateGenerator
    {
        public static byte[] Generate(Donation donation)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Gift of the Givers Foundation")
                            .FontSize(20).Bold().FontColor(Colors.Orange.Darken2);
                        col.Item().Text("Section 18A Donation Tax Certificate (PLACEHOLDER - PROTOTYPE ONLY)")
                            .FontSize(11).Italic().FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Spacing(10);
                        col.Item().Text($"Certificate Reference: {donation.CertificateReference}");
                        col.Item().Text($"Date Issued: {donation.DonationDate:dd MMMM yyyy}");
                        col.Item().Text($"Donor: {(donation.IsAnonymous ? "Anonymous Donor" : donation.DonorName)}");
                        col.Item().Text($"Amount: {donation.Currency} {donation.Amount:N2}");
                        col.Item().Text($"Frequency: {(donation.Frequency == DonationFrequency.OneTime ? "One-Time Donation" : "Recurring Donation")}");
                        col.Item().PaddingTop(15).Text(
                            "This certificate confirms a donation made to the Gift of the Givers Foundation. " +
                            "This is a prototype placeholder document and does not constitute a valid tax deduction receipt.")
                            .FontSize(10).FontColor(Colors.Grey.Darken1);
                    });

                    page.Footer().AlignCenter().Text(
                        "Gift of the Givers Foundation - giftofthegivers.org (Prototype System)")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });

            return document.GeneratePdf();
        }
    }
}
