using System.Globalization;
using System.Net;
using System.Text.Json;
using GiftOfTheGivers.Functions.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GiftOfTheGivers.Functions.Functions;

// HTTP-triggered function that turns a completed donation into a dummy tax certificate.
// The web app calls it with POST (JSON body) after a donation is saved; it also accepts GET with
// query-string values so it can be tested straight from a browser.
public class GenerateTaxCertificate
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ILogger<GenerateTaxCertificate> _logger;

    public GenerateTaxCertificate(ILogger<GenerateTaxCertificate> logger)
    {
        _logger = logger;
    }

    [Function(nameof(GenerateTaxCertificate))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "certificates")] HttpRequest req)
    {
        TaxCertificateRequest? request;
        try
        {
            request = HttpMethods.IsPost(req.Method)
                ? await JsonSerializer.DeserializeAsync<TaxCertificateRequest>(req.Body, JsonOptions)
                : FromQuery(req.Query);
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new { error = "The request body is not valid JSON." });
        }

        if (request is null)
        {
            return new BadRequestObjectResult(new { error = "Donation details are required." });
        }

        if (request.Amount <= 0)
        {
            return new BadRequestObjectResult(new { error = "Amount must be greater than zero." });
        }

        var certificate = BuildCertificate(request, DateTime.UtcNow);

        _logger.LogInformation(
            "Generated dummy tax certificate {CertificateNumber} for donation {DonationId} ({Currency} {Amount}).",
            certificate.CertificateNumber, certificate.DonationId, certificate.Currency, certificate.Amount);

        // ?format=html renders a printable certificate, which is handy when testing in the browser.
        if (string.Equals(req.Query["format"], "html", StringComparison.OrdinalIgnoreCase))
        {
            return new ContentResult
            {
                Content = RenderHtml(certificate),
                ContentType = "text/html; charset=utf-8",
                StatusCode = StatusCodes.Status200OK
            };
        }

        return new OkObjectResult(certificate);
    }

    // Reads donation values from the query string, e.g. ?donationId=12&donorName=Jane&amount=500&currency=ZAR
    private static TaxCertificateRequest FromQuery(IQueryCollection query)
    {
        var request = new TaxCertificateRequest
        {
            DonorName = query["donorName"],
            DonorEmail = query["donorEmail"],
            IsAnonymous = bool.TryParse(query["isAnonymous"], out var anonymous) && anonymous
        };

        if (int.TryParse(query["donationId"], out var donationId))
        {
            request.DonationId = donationId;
        }

        if (decimal.TryParse(query["amount"], NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            request.Amount = amount;
        }

        if (!string.IsNullOrWhiteSpace(query["currency"]))
        {
            request.Currency = query["currency"]!;
        }

        if (!string.IsNullOrWhiteSpace(query["frequency"]))
        {
            request.Frequency = query["frequency"]!;
        }

        if (DateTime.TryParse(query["donationDate"], CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var donationDate))
        {
            request.DonationDate = donationDate;
        }

        return request;
    }

    internal static TaxCertificate BuildCertificate(TaxCertificateRequest request, DateTime issuedOnUtc)
    {
        var donationDate = request.DonationDate ?? issuedOnUtc;

        // South African tax years run from 1 March to the end of February.
        var taxYearStart = donationDate.Month >= 3 ? donationDate.Year : donationDate.Year - 1;

        var donorName = request.IsAnonymous || string.IsNullOrWhiteSpace(request.DonorName)
            ? "Anonymous Donor"
            : request.DonorName.Trim();

        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "ZAR" : request.Currency.Trim().ToUpperInvariant();

        // Saved donations get a stable number; ad-hoc test calls (no donation id) get a unique demo number.
        var certificateNumber = request.DonationId > 0
            ? $"GOTG-TC-{taxYearStart}-{request.DonationId:000000}"
            : $"GOTG-TC-{taxYearStart}-DEMO-{issuedOnUtc:HHmmssfff}";

        var certificate = new TaxCertificate
        {
            CertificateNumber = certificateNumber,
            DonationId = request.DonationId,
            DonorName = donorName,
            DonorEmail = request.IsAnonymous ? null : request.DonorEmail,
            Amount = decimal.Round(request.Amount, 2),
            Currency = currency,
            Frequency = string.IsNullOrWhiteSpace(request.Frequency) ? "OneTime" : request.Frequency,
            DonationDateUtc = donationDate,
            IssuedOnUtc = issuedOnUtc,
            TaxYear = $"{taxYearStart}/{taxYearStart + 1}"
        };

        certificate.CertificateText =
            $"This certifies that {certificate.DonorName} donated {certificate.Currency} " +
            $"{certificate.Amount.ToString("N2", CultureInfo.InvariantCulture)} to the {certificate.Organisation} on " +
            $"{certificate.DonationDateUtc.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture)} " +
            $"(tax year {certificate.TaxYear}). This is a DUMMY certificate generated for prototype purposes " +
            "and is not a valid Section 18A receipt.";

        return certificate;
    }

    private static string RenderHtml(TaxCertificate c)
    {
        static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<title>Tax Certificate {{E(c.CertificateNumber)}}</title>
<style>
  body { font-family: Segoe UI, Arial, sans-serif; background:#f4f6ef; margin:0; padding:2rem; color:#1f2a10; }
  .cert { max-width:720px; margin:auto; background:#fff; border:6px double #6b8e23; padding:2rem 2.5rem; }
  h1 { margin:0; color:#4a6b12; } h2 { margin:.25rem 0 1.5rem; font-weight:400; }
  table { width:100%; border-collapse:collapse; } td { padding:.45rem 0; border-bottom:1px solid #e3e8d6; }
  td:first-child { color:#5b6650; width:40%; } .note { margin-top:1.5rem; font-size:.9rem; color:#8a3b12; }
</style>
</head>
<body>
<div class="cert">
  <h1>{{E(c.Organisation)}}</h1>
  <h2>Donation Tax Certificate (placeholder)</h2>
  <table>
    <tr><td>Certificate no.</td><td><strong>{{E(c.CertificateNumber)}}</strong></td></tr>
    <tr><td>Donor</td><td>{{E(c.DonorName)}}</td></tr>
    <tr><td>Amount</td><td>{{E(c.Currency)}} {{c.Amount.ToString("N2", CultureInfo.InvariantCulture)}}</td></tr>
    <tr><td>Frequency</td><td>{{E(c.Frequency)}}</td></tr>
    <tr><td>Donation date</td><td>{{c.DonationDateUtc.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture)}} UTC</td></tr>
    <tr><td>Tax year</td><td>{{E(c.TaxYear)}}</td></tr>
    <tr><td>Section 18A ref.</td><td>{{E(c.Section18AReference)}}</td></tr>
    <tr><td>Issued</td><td>{{c.IssuedOnUtc.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture)}} UTC</td></tr>
  </table>
  <p>{{E(c.CertificateText)}}</p>
  <p class="note">Generated by {{E(c.GeneratedBy)}}. Not an official tax receipt.</p>
</div>
</body>
</html>
""";
    }
}
