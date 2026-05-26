namespace Tiedragon.NodSystem.Core;

/// <summary>
/// Beschrijft hoe veilig een NOD-run uitgevoerd moet worden.
/// Dit is nog geen echte database-backup; het is een uitvoeringsplan.
/// </summary>
public sealed class SafetyOptions
{
    public bool PreviewRequired { get; set; } = true;
    public bool BackupRequired { get; set; } = true;
    public bool ApprovalRequired { get; set; } = true;
    public bool RollbackSupported { get; set; } = true;
    public string? ApprovedBy { get; set; }
}

/// <summary>
/// Een uitvoeringsrun.
/// </summary>
public sealed class NodRunContext
{
    public string RunId { get; }
    public DateTimeOffset CreatedAt { get; }
    public string? RuleSetName { get; }
    public SafetyOptions Safety { get; }

    // Zoek/commentaar: Constructor: maakt en initialiseert NodRunContext.
    public NodRunContext(string? ruleSetName, SafetyOptions? safety = null)
    {
        RunId = "NOD-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        CreatedAt = DateTimeOffset.UtcNow;
        RuleSetName = ruleSetName;
        Safety = safety ?? new SafetyOptions();
    }
}

/// <summary>
/// Resultaat van een preview-run.
/// </summary>
public sealed class NodPreviewReport
{
    public string RunId { get; set; } = "";
    public string? RuleSetName { get; set; }
    public string? TableName { get; set; }

    public int ScannedRows { get; set; }
    public int ChangedFields { get; set; }
    public int Warnings { get; set; }

    public List<string> Lines { get; } = new();

    // Zoek/commentaar: Methode ToString: centrale logica voor deze stap.
    public override string ToString()
    {
        var header = new[]
        {
            $"NOD Preview Report",
            $"Run ID: {RunId}",
            $"RuleSet: {RuleSetName}",
            $"Table: {TableName}",
            $"ScannedRows: {ScannedRows}",
            $"ChangedFields: {ChangedFields}",
            $"Warnings: {Warnings}",
            ""
        };

        return string.Join(Environment.NewLine, header.Concat(Lines));
    }
}

