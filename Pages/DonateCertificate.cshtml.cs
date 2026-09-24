using GiftOfTheGivers.Data;
using GiftOfTheGivers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GiftOfTheGivers.Pages
{
    public class DonateCertificateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public DonateCertificateModel(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var donation = await _db.Donations.FindAsync(id);
            if (donation is null) return NotFound();

            var pdfBytes = TaxCertificateGenerator.Generate(donation);
            return File(pdfBytes, "application/pdf", $"{donation.CertificateReference}.pdf");
        }
    }
}
