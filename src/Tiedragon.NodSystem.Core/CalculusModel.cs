namespace Tiedragon.NodSystem.Core;

/// <summary>
/// Numerieke calculus-stap.
/// 
/// Let op:
/// Dit is numerieke calculus, geen symbolische CAS.
/// </summary>
public sealed record CalculusStep(
    CalculusOperation Operation,
    string Expression,
    decimal? A = null,
    decimal? B = null
);

// Zoek/commentaar: Type-overzicht: enum CalculusOperation bevat de hoofdlogica/data voor dit onderdeel.
public enum CalculusOperation
{
    Differentiate,
    Integrate,
    Limit
}

