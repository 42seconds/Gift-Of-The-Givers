using System.Globalization;
using System.Text;
using gift_of_the_givers.Models;

namespace gift_of_the_givers.Services;

public static class DonationCertificatePdfBuilder
{
    // Builds a one-page placeholder certificate PDF so every successful donation can produce a downloadable receipt without adding a third-party PDF package.
    public static byte[] Build(Donation donation, string certificateNumber, string donorDisplayName, DateTime issuedUtc)
    {
        var lines = new List<string>
        {
            "Gift of the Givers Foundation",
            "Placeholder Tax Certificate",
            $"Certificate No: {certificateNumber}",
            $"Donor Name: {donorDisplayName}",
            $"Donation Date: {donation.DonationDate.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture)} UTC",
            $"Amount: {donation.Currency} {donation.Amount.ToString("N2", CultureInfo.InvariantCulture)}",
            $"Issued On: {issuedUtc.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture)} UTC",
            "This certificate is a placeholder for prototype use only.",
            "It is not an official tax receipt."
        };

        return BuildPdf(lines);
    }

    // Converts the certificate text into a minimal but valid single-page PDF using plain PDF syntax.
    private static byte[] BuildPdf(IReadOnlyList<string> lines)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            BuildContentStreamObject(lines)
        };

        return AssemblePdf(objects);
    }

    // Writes the text stream for the certificate page, keeping the title large and the remaining fields readable and compact.
    private static string BuildContentStreamObject(IReadOnlyList<string> lines)
    {
        var content = BuildContentStream(lines);
        var contentLength = Encoding.ASCII.GetByteCount(content);

        return $"<< /Length {contentLength} >>\nstream\n{content}\nendstream";
    }

    // Places each certificate line on the page with a simple text layout so the output stays dependable and easy to inspect.
    private static string BuildContentStream(IReadOnlyList<string> lines)
    {
        var builder = new StringBuilder();
        builder.AppendLine("BT");
        builder.AppendLine("/F1 20 Tf");
        builder.AppendLine("72 770 Td");
        builder.AppendLine($"({EscapePdfText(lines[0])}) Tj");
        builder.AppendLine("/F1 12 Tf");

        for (var i = 1; i < lines.Count; i++)
        {
            builder.AppendLine("0 -24 Td");
            builder.AppendLine($"({EscapePdfText(lines[i])}) Tj");
        }

        builder.AppendLine("ET");
        return builder.ToString();
    }

    // Escapes characters that are special inside PDF text literals so the generated PDF stays valid.
    private static string EscapePdfText(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (ch == '\\' || ch == '(' || ch == ')')
            {
                builder.Append('\\');
            }

            builder.Append(ch is >= '\u0020' and <= '\u007e' ? ch : '?');
        }

        return builder.ToString();
    }

    // Assembles the final PDF bytes by calculating object offsets and emitting the cross-reference table.
    private static byte[] AssemblePdf(IReadOnlyList<string> objects)
    {
        var encoding = Encoding.ASCII;
        var buffer = new List<byte>();
        var offsets = new List<int>(objects.Count);

        buffer.AddRange(encoding.GetBytes("%PDF-1.4\n"));

        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(buffer.Count);
            var obj = $"{i + 1} 0 obj\n{objects[i]}\nendobj\n";
            buffer.AddRange(encoding.GetBytes(obj));
        }

        var xrefOffset = buffer.Count;
        var xref = new StringBuilder();
        xref.AppendLine("xref");
        xref.AppendLine($"0 {objects.Count + 1}");
        xref.AppendLine("0000000000 65535 f ");

        foreach (var offset in offsets)
        {
            xref.AppendLine($"{offset:0000000000} 00000 n ");
        }

        xref.AppendLine("trailer");
        xref.AppendLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        xref.AppendLine("startxref");
        xref.AppendLine(xrefOffset.ToString(CultureInfo.InvariantCulture));
        xref.AppendLine("%%EOF");

        buffer.AddRange(encoding.GetBytes(xref.ToString()));
        return buffer.ToArray();
    }
}
