/*
NOD SYSTEM -- DATA TRANSFORM ENGINE

Dit bestand voert NOD Data-regels uit op een in-memory rij.

Doel:
- databaseveldachtige transformaties kunnen testen zonder echte database
- phoneformat normalisatie testen
- chg/trans/math op velden toepassen

Voor enterprise/SQL is dit later uit te breiden naar:
- SQL preview
- UPDATE generatie
- JOIN preview
- backup/rollback/auditlog
*/

using System.Text.RegularExpressions;

namespace Tiedragon.NodSystem.Core;

// Zoek/commentaar: Type-overzicht: class DataTransformEngine bevat de hoofdlogica/data voor dit onderdeel.
public static class DataTransformEngine
{
    // Zoek/commentaar: Methode TransformRow: centrale logica voor deze stap.
    public static IReadOnlyList<FieldTransformResult> TransformRow(DataDefinition data, IDictionary<string, string> row)
    {
        var results = new List<FieldTransformResult>();

        foreach (var field in data.Fields)
        {
            row.TryGetValue(field.FieldName, out var original);
            original ??= "";

            var normalized = ApplyPhoneFormat(original, field.PhoneFormat);
            var result = normalized;

            if (field.ChangeRules.Count > 0)
                result = NodEngine.ApplyChange(result, field.ChangeRules, reverse: false);

            if (field.TranslateRules.Count > 0)
                result = NodEngine.ApplyTranslate(result, field.TranslateRules, reverse: false);

            if (field.LegacyMathSteps.Count > 0 || field.MathExpressions20.Count > 0)
            {
                var ans = NodParser.ParseFlexibleDecimal(result);

                foreach (var step in field.LegacyMathSteps)
                    ans = NodEngine.ApplyMathStep(ans, step);

                foreach (var expr in field.MathExpressions20)
                    ans = NodExpressionEvaluator.Evaluate(expr, ans);

                result = NodEngine.Format(ans, null);
            }

            var output = string.IsNullOrWhiteSpace(field.OutputField)
                ? field.FieldName
                : field.OutputField!;

            results.Add(new FieldTransformResult(field.FieldName, output, original, normalized, result));
        }

        return results;
    }

    // Zoek/commentaar: Past een regel, instelling of bewerking toe voor ApplyPhoneFormat.
    public static string ApplyPhoneFormat(string input, PhoneFormatOptions? options)
    {
        if (options is null) return input;

        var value = input.Trim();

        if (options.RemoveTextPrefix)
        {
            value = Regex.Replace(value, @"^\s*(telefoon:?|tel\.?)\s*", "", RegexOptions.IgnoreCase);
        }

        if (options.RemoveSpaces) value = value.Replace(" ", "");
        if (options.RemoveDots) value = value.Replace(".", "");
        if (options.RemoveSlashes) value = value.Replace("/", "");
        if (options.RemoveParentheses)
        {
            value = value.Replace("(", "");
            value = value.Replace(")", "");
        }

        // Always remove hyphen before prefix conversion, because phone chg normally expects pure prefix.
        value = value.Replace("-", "");

        if (options.NormalizeInternational &&
            string.Equals(options.Country, "NL", StringComparison.OrdinalIgnoreCase))
        {
            if (value.StartsWith("+31", StringComparison.OrdinalIgnoreCase))
                value = "0" + value[3..];

            if (value.StartsWith("0031", StringComparison.OrdinalIgnoreCase))
                value = "0" + value[4..];
        }

        return value;
    }
}

// Zoek/commentaar: Type-overzicht: class DataPlanDescriber bevat de hoofdlogica/data voor dit onderdeel.
public static class DataPlanDescriber
{
    // Zoek/commentaar: Methode Describe: centrale logica voor deze stap.
    public static string Describe(DataDefinition data)
    {
        var lines = new List<string>
        {
            $"Table: {data.TableName ?? "(not set)"}",
            $"Preview: {data.Preview}",
            $"Backup: {data.Backup}",
            $"Fields: {data.Fields.Count}"
        };

        foreach (var field in data.Fields)
        {
            lines.Add($"- Field: {field.FieldName}");
            if (!string.IsNullOrWhiteSpace(field.OutputField))
                lines.Add($"  Output: {field.OutputField}");
            if (field.PhoneFormat is not null)
                lines.Add($"  PhoneFormat: country={field.PhoneFormat.Country}, normalizeInternational={field.PhoneFormat.NormalizeInternational}");
            foreach (var chg in field.ChangeRules)
                lines.Add($"  chg {chg.OldPrefix},{chg.NewPrefix}");
            foreach (var trans in field.TranslateRules)
                lines.Add($"  trans {trans.OldValue},{trans.NewValue}");
            foreach (var math in field.LegacyMathSteps)
                lines.Add($"  math ans {math.Operator} {math.Number}");
            foreach (var expr in field.MathExpressions20)
                lines.Add($"  math {expr}");
            if (field.Lookup is not null)
                lines.Add($"  lookup {field.Lookup.LookupTable}, match {field.Lookup.LeftField} = {field.Lookup.RightField}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}

