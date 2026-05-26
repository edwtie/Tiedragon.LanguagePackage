namespace Tiedragon.NodSystem.Core;

/// <summary>
/// Bouwt previewrapporten op basis van in-memory rows.
/// Later kan dit worden uitgebreid naar CSV/JSON/PDF.
/// </summary>
public static class ReportBuilder
{
    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildPreviewReport.
    public static NodPreviewReport BuildPreviewReport(
        NodDocument doc,
        IEnumerable<IDictionary<string, string>> rows,
        NodRunContext? context = null)
    {
        if (doc.Data is null)
            throw new InvalidOperationException("No data block found.");

        context ??= new NodRunContext(doc.Name);

        var report = new NodPreviewReport
        {
            RunId = context.RunId,
            RuleSetName = doc.Name,
            TableName = doc.Data.TableName
        };

        foreach (var row in rows)
        {
            report.ScannedRows++;

            var results = NodEngine.TransformRow(doc, row);

            foreach (var r in results)
            {
                if (!string.Equals(r.Original, r.Result, StringComparison.Ordinal))
                {
                    report.ChangedFields++;
                    report.Lines.Add($"{r.FieldName} -> {r.OutputField}: {r.Original} -> {r.Normalized} -> {r.Result}");
                }
            }
        }

        if (context.Safety.PreviewRequired)
            report.Lines.Insert(0, "Safety: preview required = true");

        if (context.Safety.BackupRequired)
            report.Lines.Insert(1, "Safety: backup required = true");

        if (context.Safety.ApprovalRequired && string.IsNullOrWhiteSpace(context.Safety.ApprovedBy))
        {
            report.Warnings++;
            report.Lines.Insert(2, "Warning: approval required but ApprovedBy is empty");
        }

        return report;
    }
}

