namespace Tiedragon.NodSystem.Core;

// Zoek/commentaar: Type-overzicht: class EquationDefinition bevat de hoofdlogica/data voor dit onderdeel.
public sealed class EquationDefinition
{
    public Dictionary<string, decimal> GivenValues { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string? EquationText { get; set; }
    public string? SolveVariable { get; set; }
    public List<ConstraintRule> Constraints { get; } = new();
}

// Zoek/commentaar: Type-overzicht: record ConstraintRule bevat de hoofdlogica/data voor dit onderdeel.
public sealed record ConstraintRule(string Variable, string Operator, decimal Value);
// Zoek/commentaar: Type-overzicht: record EquationResult bevat de hoofdlogica/data voor dit onderdeel.
public sealed record EquationResult(string Variable, decimal Value, string Explanation);

