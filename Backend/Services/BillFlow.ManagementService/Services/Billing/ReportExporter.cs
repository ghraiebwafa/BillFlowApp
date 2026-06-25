using BillFlow.Models.Shared.Enums;
using System.Text;

namespace BillFlow.ManagementService.Services.Billing;

public static class ReportExporter
{
    public static ReportExportResult Export(
        string baseFileName,
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        ReportFormat format) =>
        format switch
        {
            ReportFormat.Csv => ExportCsv(baseFileName, headers, rows),
            ReportFormat.Xlsx => ExportXlsx(baseFileName, sheetName, headers, rows),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format."),
        };

    private static ReportExportResult ExportCsv(
        string baseFileName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', headers.Select(EscapeCsv)));

        foreach (var row in rows)
            builder.AppendLine(string.Join(',', row.Select(EscapeCsv)));

        return new ReportExportResult
        {
            Content = Encoding.UTF8.GetBytes(builder.ToString()),
            FileName = $"{baseFileName}.csv",
            ContentType = "text/csv",
        };
    }

    private static ReportExportResult ExportXlsx(
        string baseFileName,
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        for (var column = 0; column < headers.Count; column++)
            worksheet.Cell(1, column + 1).Value = headers[column];

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (var column = 0; column < row.Count; column++)
                worksheet.Cell(rowIndex + 2, column + 1).Value = row[column];
        }

        worksheet.Row(1).Style.Font.Bold = true;
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ReportExportResult
        {
            Content = stream.ToArray(),
            FileName = $"{baseFileName}.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        };
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}

public sealed class ReportExportResult
{
    public byte[] Content { get; init; } = [];

    public string FileName { get; init; } = null!;

    public string ContentType { get; init; } = null!;
}
