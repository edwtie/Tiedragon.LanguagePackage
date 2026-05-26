namespace Tiedragon.NodSystem.Core;

// Zoek/commentaar: Type-overzicht: class DataDefinition bevat de hoofdlogica/data voor dit onderdeel.
public sealed class DataDefinition
{
    public string? TableName { get; set; }
    public bool Preview { get; set; }
    public bool Backup { get; set; }
    public List<DataFieldDefinition> Fields { get; } = new();
}

// Zoek/commentaar: Type-overzicht: class DataFieldDefinition bevat de hoofdlogica/data voor dit onderdeel.
public sealed class DataFieldDefinition
{
    public string FieldName { get; }

    public List<ChangeRule> ChangeRules { get; } = new();
    public List<TranslateRule> TranslateRules { get; } = new();
    public List<MathStep10> LegacyMathSteps { get; } = new();
    public List<string> MathExpressions20 { get; } = new();

    public string? OutputField { get; set; }
    public PhoneFormatOptions? PhoneFormat { get; set; }
    public LookupDefinition? Lookup { get; set; }

    // Zoek/commentaar: Constructor: maakt en initialiseert DataFieldDefinition.
    public DataFieldDefinition(string fieldName)
    {
        FieldName = fieldName;
    }
}

// Zoek/commentaar: Type-overzicht: class PhoneFormatOptions bevat de hoofdlogica/data voor dit onderdeel.
public sealed class PhoneFormatOptions
{
    public string? Country { get; set; }
    public bool RemoveSpaces { get; set; }
    public bool RemoveDots { get; set; }
    public bool RemoveSlashes { get; set; }
    public bool RemoveParentheses { get; set; }
    public bool RemoveTextPrefix { get; set; }
    public bool NormalizeInternational { get; set; }
    public string? KeepSeparator { get; set; }
}

// Zoek/commentaar: Type-overzicht: class LookupDefinition bevat de hoofdlogica/data voor dit onderdeel.
public sealed class LookupDefinition
{
    public string LookupTable { get; }
    public string? LeftField { get; set; }
    public string? RightField { get; set; }
    public string? OutputField { get; set; }
    public string? OutputSourceField { get; set; }

    // Zoek/commentaar: Constructor: maakt en initialiseert LookupDefinition.
    public LookupDefinition(string lookupTable)
    {
        LookupTable = lookupTable;
    }
}

// Zoek/commentaar: Type-overzicht: record FieldTransformResult bevat de hoofdlogica/data voor dit onderdeel.
public sealed record FieldTransformResult(
    string FieldName,
    string OutputField,
    string Original,
    string Normalized,
    string Result
);

