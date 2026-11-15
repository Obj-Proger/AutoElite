using System.Globalization;
using System.Text;

namespace AutoElite.API.DemoData;

/// <summary>
/// The files of the demo documents: one-page PDF documents with a line of text, made here so that the demo data
/// needs no files of its own.
/// </summary>
internal static class DemoFiles
{
    /// <summary>Makes a one-page A4 PDF document with a title in Helvetica.</summary>
    /// <param name="title">The title, in ASCII, without parentheses or backslashes.</param>
    /// <returns>The bytes of the document.</returns>
    public static byte[] Pdf(string title)
    {
        var content = $"BT /F1 20 Tf 72 760 Td ({title}) Tj ET\nBT /F1 11 Tf 72 730 Td (AutoElite demo document) Tj ET\n";
        string[] objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            string.Create(CultureInfo.InvariantCulture, $"<< /Length {content.Length} >>\nstream\n{content}endstream"),
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        ];

        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(pdf.Length);
            pdf.Append(CultureInfo.InvariantCulture, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var xref = pdf.Length;
        pdf.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
            pdf.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");

        pdf.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
