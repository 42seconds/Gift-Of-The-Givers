using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using gift_of_the_givers.Data;
using gift_of_the_givers.Models;
using gift_of_the_givers.Services;

namespace gift_of_the_givers.Pages.Donation;

public class CertificateModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public CertificateModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public global::gift_of_the_givers.Models.Donation? Donation { get; private set; }

    public string CertificateNumber => Donation is null ? string.Empty : $"GOTG-{Donation.Id:000000}";

    public string DonorDisplayName => Donation is null
        ? string.Empty
        : (Donation.IsAnonymous || string.IsNullOrWhiteSpace(Donation.DonorName) ? "Anonymous Donor" : Donation.DonorName);

    // Loads the saved donation so the certificate page can display the recorded gift details on screen.
    public async Task<IActionResult> OnGetAsync(int id)
    {
        Donation = await _db.GetDonationByIdAsync(id);
        if (Donation is null)
        {
            return NotFound();
        }

        return Page();
    }

    // Creates a downloadable PDF version of the same placeholder certificate so the user can keep a copy.
    public async Task<IActionResult> OnGetPdfAsync(int id)
    {
        var donation = await _db.GetDonationByIdAsync(id);
        if (donation is null)
        {
            return NotFound();
        }

        var donorDisplayName = donation.IsAnonymous || string.IsNullOrWhiteSpace(donation.DonorName)
            ? "Anonymous Donor"
            : donation.DonorName;

        var pdfBytes = DonationCertificatePdfBuilder.Build(
            donation,
            $"GOTG-{donation.Id:000000}",
            donorDisplayName,
            DateTime.UtcNow);

        return File(pdfBytes, "application/pdf", $"tax-certificate-{donation.Id:000000}.pdf");
    }
}
