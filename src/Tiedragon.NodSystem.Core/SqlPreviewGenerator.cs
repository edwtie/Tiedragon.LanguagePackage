using System.Text;

namespace Tiedragon.NodSystem.Core;

/// <summary>
/// Maakt SQL-preview voor NOD Data.
/// 
/// Belangrijk:
/// - Dit voert geen SQL uit.
/// - Dit genereert alleen leesbare SELECT/UPDATE-preview.
/// - Database dialect is bewust eenvoudig gehouden.
/// </summary>
public static class SqlPreviewGenerator
{
    // Zoek/commentaar: Genereert tekst of output voor GeneratePreviewSql.
    public static string GeneratePreviewSql(NodDocument doc)
    {
        if (doc.Data is null)
            throw new InvalidOperationException("No data block found.");

        var data = doc.Data;
        var table = RequireName(data.TableName, "table");

        var sb = new StringBuilder();
        sb.AppendLine("-- NOD SQL Preview");
        sb.AppendLine("-- This SQL is generated for review only.");
        sb.AppendLine("-- Do not run UPDATE before checking preview and backup.");
        sb.AppendLine();

        foreach (var field in data.Fields)
        {
            if (field.Lookup is not null)
            {
                AppendLookupPreview(sb, table, field);
                continue;
            }

            AppendFieldPreview(sb, table, field);
        }

        return sb.ToString();
    }

    // Zoek/commentaar: Genereert tekst of output voor GenerateUpdateSql.
    public static string GenerateUpdateSql(NodDocument doc)
    {
        if (doc.Data is null)
            throw new InvalidOperationException("No data block found.");

        var data = doc.Data;
        var table = RequireName(data.TableName, "table");

        var sb = new StringBuilder();
        sb.AppendLine("-- NOD SQL Update");
        sb.AppendLine("-- Run only after preview, backup and approval.");
        sb.AppendLine();

        foreach (var field in data.Fields)
        {
            if (field.Lookup is not null)
            {
                AppendLookupUpdate(sb, table, field);
                continue;
            }

            AppendFieldUpdate(sb, table, field);
        }

        return sb.ToString();
    }

    // Zoek/commentaar: Voegt een onderdeel toe aan bestaande output voor AppendFieldPreview.
    private static void AppendFieldPreview(StringBuilder sb, string table, DataFieldDefinition field)
    {
        var output = string.IsNullOrWhiteSpace(field.OutputField) ? field.FieldName : field.OutputField!;
        var expression = BuildSqlExpression(field, field.FieldName);

        sb.AppendLine($"-- Field preview: {field.FieldName} -> {output}");
        sb.AppendLine("SELECT");
        sb.AppendLine($"  {field.FieldName} AS old_value,");
        sb.AppendLine($"  {expression} AS new_value");
        sb.AppendLine($"FROM {table};");
        sb.AppendLine();
    }

    // Zoek/commentaar: Voegt een onderdeel toe aan bestaande output voor AppendFieldUpdate.
    private static void AppendFieldUpdate(StringBuilder sb, string table, DataFieldDefinition field)
    {
        var output = string.IsNullOrWhiteSpace(field.OutputField) ? field.FieldName : field.OutputField!;
        var expression = BuildSqlExpression(field, field.FieldName);

        sb.AppendLine($"-- Field update: {field.FieldName} -> {output}");
        sb.AppendLine($"UPDATE {table}");
        sb.AppendLine($"SET {output} = {expression};");
        sb.AppendLine();
    }

    // Zoek/commentaar: Voegt een onderdeel toe aan bestaande output voor AppendLookupPreview.
    private static void AppendLookupPreview(StringBuilder sb, string table, DataFieldDefinition field)
    {
        var lookup = field.Lookup!;
        var output = string.IsNullOrWhiteSpace(field.OutputField) ? field.FieldName : field.OutputField!;
        var left = lookup.LeftField ?? field.FieldName;
        var right = lookup.RightField ?? "id";

        sb.AppendLine($"-- Lookup/JOIN preview: {field.FieldName} -> {output}");
        sb.AppendLine("SELECT");
        sb.AppendLine($"  k.{field.FieldName} AS old_value,");
        sb.AppendLine($"  p.{right} AS matched_value");
        sb.AppendLine($"FROM {table} k");
        sb.AppendLine($"LEFT JOIN {lookup.LookupTable} p ON k.{left} = p.{right};");
        sb.AppendLine();
    }

    // Zoek/commentaar: Voegt een onderdeel toe aan bestaande output voor AppendLookupUpdate.
    private static void AppendLookupUpdate(StringBuilder sb, string table, DataFieldDefinition field)
    {
        var lookup = field.Lookup!;
        var output = string.IsNullOrWhiteSpace(field.OutputField) ? field.FieldName : field.OutputField!;
        var left = lookup.LeftField ?? field.FieldName;
        var right = lookup.RightField ?? "id";

        sb.AppendLine($"-- Lookup update skeleton: {field.FieldName} -> {output}");
        sb.AppendLine("-- Database-specific UPDATE JOIN syntax may differ.");
        sb.AppendLine($"-- UPDATE {table} SET {output} = <lookup new value> FROM {lookup.LookupTable} WHERE {table}.{left} = {lookup.LookupTable}.{right};");
        sb.AppendLine();
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildSqlExpression.
    private static string BuildSqlExpression(DataFieldDefinition field, string sourceColumn)
    {
        var expr = sourceColumn;

        // chg: prefix conversion via CASE WHEN
        if (field.ChangeRules.Count > 0)
        {
            var cases = new StringBuilder();
            cases.Append("CASE");

            foreach (var rule in field.ChangeRules)
            {
                var oldPrefix = EscapeSql(rule.OldPrefix);
                var newPrefix = EscapeSql(rule.NewPrefix);
                var start = rule.OldPrefix.Length + 1;

                cases.Append($" WHEN {sourceColumn} LIKE '{oldPrefix}%' THEN '{newPrefix}' || SUBSTR({sourceColumn}, {start})");
            }

            cases.Append($" ELSE {sourceColumn} END");
            expr = cases.ToString();
        }

        // trans: exact value mapping
        if (field.TranslateRules.Count > 0)
        {
            var cases = new StringBuilder();
            cases.Append("CASE");

            foreach (var rule in field.TranslateRules)
            {
                cases.Append($" WHEN {sourceColumn} = '{EscapeSql(rule.OldValue)}' THEN '{EscapeSql(rule.NewValue)}'");
            }

            cases.Append($" ELSE {sourceColumn} END");
            expr = cases.ToString();
        }

        // Alleen eenvoudige legacy math naar SQL.
        // Complexe expressions kunnen later via dialect-specifieke mapper.
        foreach (var step in field.LegacyMathSteps)
        {
            var number = step.Number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expr = $"({expr} {step.Operator} {number})";
        }

        foreach (var math in field.MathExpressions20)
        {
            expr = $"/* NOD expression needs mapper: {math} */ {expr}";
        }

        return expr;
    }

    // Zoek/commentaar: Controleert verplichte waarden voor RequireName.
    private static string RequireName(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Missing {name} name.");
        return value.Trim();
    }

    // Zoek/commentaar: Escapet tekst zodat output veilig blijft voor EscapeSql.
    private static string EscapeSql(string value) => value.Replace("'", "''");
}

