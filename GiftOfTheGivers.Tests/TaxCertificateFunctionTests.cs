using System.Text;
using System.Text.Json;
using GiftOfTheGivers.Functions.Functions;
using GiftOfTheGivers.Functions.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace GiftOfTheGivers.Tests;

// Unit tests for the GenerateTaxCertificate Azure Function (certificate formatting and input checks).
public class TaxCertificateFunctionTests
{
    private readonly GenerateTaxCertificate _function = new(NullLogger<GenerateTaxCertificate>.Instance);

    private static HttpRequest PostJson(object body)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body)));
        return context.Request;
    }

    private async Task<TaxCertificate> GenerateAsync(object body)
    {
        var result = await _function.Run(PostJson(body));
        var ok = Assert.IsType<OkObjectResult>(result);
        return Assert.IsType<TaxCertificate>(ok.Value);
    }

    [Fact]
    public async Task ValidDonation_ReturnsFormattedCertificateNumberAndTaxYear()
    {
        var certificate = await GenerateAsync(new
        {
            DonationId = 12,
            DonorName = "Thandi Mokoena",
            Amount = 750m,
            Currency = "ZAR",
            DonationDate = new DateTime(2026, 9, 20, 10, 30, 0, DateTimeKind.Utc)
        });

        Assert.Equal("GOTG-TC-2026-000012", certificate.CertificateNumber);
        Assert.Equal("2026/2027", certificate.TaxYear);
        Assert.Equal("Thandi Mokoena", certificate.DonorName);
        Assert.Equal(750m, certificate.Amount);
        Assert.True(certificate.IsPlaceholder);
        Assert.Contains("ZAR 750.00", certificate.CertificateText);
    }

    [Theory]
    [InlineData(2026, 2, 28, "2025/2026")]
    [InlineData(2026, 3, 1, "2026/2027")]
    public async Task TaxYear_FollowsSouthAfricanMarchToFebruaryYear(int year, int month, int day, string expectedTaxYear)
    {
        var certificate = await GenerateAsync(new
        {
            DonationId = 1,
            DonorName = "Donor",
            Amount = 100m,
            DonationDate = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc)
        });

        Assert.Equal(expectedTaxYear, certificate.TaxYear);
    }

    [Fact]
    public async Task AnonymousDonation_HidesDonorNameAndEmail()
    {
        var certificate = await GenerateAsync(new
        {
            DonationId = 5,
            DonorName = "Real Name",
            DonorEmail = "real@example.com",
            IsAnonymous = true,
            Amount = 100m,
            Currency = "usd"
        });

        Assert.Equal("Anonymous Donor", certificate.DonorName);
        Assert.Null(certificate.DonorEmail);
        Assert.Equal("USD", certificate.Currency);
    }

    [Fact]
    public async Task ZeroAmount_ReturnsBadRequest()
    {
        var result = await _function.Run(PostJson(new { DonationId = 7, Amount = 0m }));

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
