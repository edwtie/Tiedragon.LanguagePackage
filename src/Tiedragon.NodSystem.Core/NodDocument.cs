/*
NOD SYSTEM -- MODELCLASSES

Dit bestand bevat de datastructuren van NOD System 1.0/2.0.

Belangrijk:
- Deze classes voeren zelf niets uit.
- De parser vult deze classes.
- De engine gebruikt deze classes om regels uit te voeren.

Historische scheiding:
- LegacyMathSteps = oude NOD 1.0 math zoals: math ans * 1,8
- MathExpressions20 = NOD 2.0 expressies zoals: math ans * e^2
- ChangeRules = chg-regels, vooral telefoonnummer-/prefixomzetting
- TranslateRules = trans-regels, vaste waarde naar vaste waarde
- Equation = mode equation met given/equation/solve/constraint
- Data = mode data met table/field/output/phoneformat
*/

namespace Tiedragon.NodSystem.Core;

// Zoek/commentaar: Type-overzicht: class NodDocument bevat de hoofdlogica/data voor dit onderdeel.
public sealed class NodDocument
{
    public string? Name { get; set; }
    public string? Format { get; set; }
    public string? Mode { get; set; }
    public string? ReverseExpression { get; set; }
    public string? LegacyInput1Label { get; set; }
    public string? LegacyInput2Label { get; set; }
    public List<NodValueDefinition> Inputs { get; } = new();
    public List<NodValueDefinition> Outputs { get; } = new();
    public List<NodDeprecation> Deprecations { get; } = new();

    public List<MathStep10> LegacyMathSteps { get; } = new();
    public List<string> MathExpressions20 { get; } = new();

    // NOD 2.0 Calculus:
    // diff ans^2
    // integral 0,1 ans^2
    // limit 0 sin(ans)/ans
    public List<CalculusStep> CalculusSteps { get; } = new();
    public List<ChangeRule> ChangeRules { get; } = new();
    public List<TranslateRule> TranslateRules { get; } = new();

    public EquationDefinition? Equation { get; set; }
    public DataDefinition? Data { get; set; }
}

// Zoek/commentaar: Type-overzicht: record MathStep10 bevat de hoofdlogica/data voor dit onderdeel.
public sealed record MathStep10(char Operator, decimal Number)
{
    // Zoek/commentaar: Methode Reverse: centrale logica voor deze stap.
    public MathStep10 Reverse() => Operator switch
    {
        '+' => new MathStep10('-', Number),
        '-' => new MathStep10('+', Number),
        '*' => new MathStep10('/', Number),
        '/' => new MathStep10('*', Number),
        _ => throw new InvalidOperationException($"Unknown operator: {Operator}")
    };
}

// Zoek/commentaar: Type-overzicht: record ChangeRule bevat de hoofdlogica/data voor dit onderdeel.
public sealed record ChangeRule(string OldPrefix, string NewPrefix);
// Zoek/commentaar: Type-overzicht: record TranslateRule bevat de hoofdlogica/data voor dit onderdeel.
public sealed record TranslateRule(string OldValue, string NewValue);
// Zoek/commentaar: Type-overzicht: record NodResult bevat de hoofdlogica/data voor dit onderdeel.
public sealed record NodResult(string Text, decimal? NumericValue);
public sealed record NodValueDefinition(string Name, string? Label, string? Kind = null);
public sealed record NodDeprecation(string Command, string Message);

