namespace Tiedragon.NodSystem.Core;

/// <summary>
/// Bewaart een volledige berekening.
/// 
/// Doel:
/// - vooruit rekenen
/// - tussenstappen onthouden
/// - direct terugrekenen via dezelfde stappen
/// 
/// Belangrijk:
/// Dit is GEEN algebra-oplosser.
/// Dit is een rekengeheugen.
/// Daardoor kan terugrekenen binnen dezelfde sessie veel veiliger.
/// </summary>
public sealed class CalculationTrace
{
    public decimal StartValue { get; }
    public decimal FinalValue { get; private set; }

    public List<CalculationTraceStep> Steps { get; } = new();

    // Zoek/commentaar: Constructor: maakt en initialiseert CalculationTrace.
    public CalculationTrace(decimal startValue)
    {
        StartValue = startValue;
        FinalValue = startValue;
    }

    // Zoek/commentaar: Voegt data of UI-regels toe voor AddStep.
    public void AddStep(CalculationTraceStep step)
    {
        Steps.Add(step);
        FinalValue = step.OutputValue;
    }
}

/// <summary>
/// Een stap in de trace.
/// </summary>
public sealed record CalculationTraceStep(
    string Expression,
    decimal InputValue,
    decimal OutputValue,
    string? AutoReverseExpression,
    IReadOnlyList<string>? ExplanationSteps = null
);

/// <summary>
/// Resultaat van een berekening inclusief trace.
/// </summary>
public sealed record NodTraceResult(
    string Text,
    decimal NumericValue,
    CalculationTrace Trace
);

