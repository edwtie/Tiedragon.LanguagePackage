/*
NOD SYSTEM -- PARSER

Dit bestand leest .nod-tekst en bouwt een NodDocument op.

Belangrijk ontwerp:
1. De parser voert geen conversie uit.
2. De parser bepaalt alleen welke regel bij welke laag hoort.
3. Oude NOD 1.0 math en nieuwe NOD 2.0 math worden hier gescheiden.

De belangrijkste bugfix:
- math ans * 1,8   => LegacyMathSteps / MathStep10
- math ans * e^2   => MathExpressions20 / expression parser

Waarom:
MathStep10 accepteert alleen echte decimal-getallen.
e^2 is geen decimal-getal, maar een expressie.
*/

using System.Globalization;
using System.Text.RegularExpressions;

namespace Tiedragon.NodSystem.Core;

// Zoek/commentaar: Type-overzicht: class NodParser bevat de hoofdlogica/data voor dit onderdeel.
public static class NodParser
{
    // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor Parse.
    public static NodDocument Parse(string text)
    {
        text = NodTextNormalizer.Normalize(text);

        var doc = new NodDocument();
        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        DataFieldDefinition? currentField = null;

        for (var i = 0; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var line = lines[i].Trim();

            if (line.Length == 0 || line.StartsWith("'"))
                continue;

            var (keyword, value) = SplitKeyword(line);
            var key = keyword.ToLowerInvariant();

            switch (key)
            {
                case "name":
                    doc.Name = value;
                    break;

                case "format":
                    doc.Format = value;
                    break;

                case "mode":
                    ValidateMode(value, lineNumber);
                    doc.Mode = value;
                    if (value.Equals("data", StringComparison.OrdinalIgnoreCase) ||
                        value.Equals("sql-transform", StringComparison.OrdinalIgnoreCase))
                        doc.Data ??= new DataDefinition();
                    else if (value.Equals("equation", StringComparison.OrdinalIgnoreCase))
                        doc.Equation ??= new EquationDefinition();
                    break;

                case "reverse":
                    doc.ReverseExpression = value;
                    break;

                case "table":
                    doc.Data ??= new DataDefinition();
                    doc.Data.TableName = value;
                    break;

                case "field":
                    doc.Data ??= new DataDefinition();
                    currentField = new DataFieldDefinition(value);
                    doc.Data.Fields.Add(currentField);
                    break;

                case "input":
                    doc.Inputs.Add(ParseValueDefinition(value, fallbackName: $"input{doc.Inputs.Count + 1}"));
                    break;

                case "inputr":
                    doc.Outputs.Add(ParseReverseValueDefinition(value));
                    break;

                case "output":
                    if (currentField is not null)
                        currentField.OutputField = value.Trim();
                    else
                        doc.Outputs.Add(ParseValueDefinition(value, fallbackName: $"output{doc.Outputs.Count + 1}"));
                    break;

                case "preview":
                    doc.Data ??= new DataDefinition();
                    doc.Data.Preview = ParseBool(value);
                    break;

                case "backup":
                    doc.Data ??= new DataDefinition();
                    doc.Data.Backup = ParseBool(value);
                    break;

                case "phoneformat":
                    if (currentField is null)
                        throw new FormatException($"Line {lineNumber}: phoneformat must be inside a field block.");
                    ParsePhoneFormat(value, currentField);
                    break;

                case "lookup":
                    if (currentField is null)
                        throw new FormatException($"Line {lineNumber}: lookup must be inside a field block.");
                    currentField.Lookup = new LookupDefinition(value.Trim());
                    break;

                case "match":
                    if (currentField?.Lookup is null)
                        throw new FormatException($"Line {lineNumber}: match must be after lookup.");
                    ParseLookupMatch(value, currentField.Lookup, lineNumber);
                    break;

                case "given":
                    doc.Equation ??= new EquationDefinition();
                    ParseGiven(value, doc.Equation, lineNumber);
                    break;

                case "equation":
                    doc.Equation ??= new EquationDefinition();
                    doc.Equation.EquationText = value;
                    break;

                case "solve":
                    if (TryParseCalculusMath(value, lineNumber, out var solveCalculusStep))
                    {
                        doc.CalculusSteps.Add(solveCalculusStep);
                        break;
                    }

                    doc.Equation ??= new EquationDefinition();
                    doc.Equation.SolveVariable = value.Trim();
                    break;

                case "constraint":
                    doc.Equation ??= new EquationDefinition();
                    doc.Equation.Constraints.Add(ParseConstraint(value, lineNumber));
                    break;

                case "chg":
                    var chg = ParsePair(value, (a, b) => new ChangeRule(Unquote(a), Unquote(b)), lineNumber);
                    if (currentField is not null) currentField.ChangeRules.Add(chg);
                    else doc.ChangeRules.Add(chg);
                    break;

                case "trans":
                    var trans = ParsePair(value, (a, b) => new TranslateRule(Unquote(a), Unquote(b)), lineNumber);
                    if (currentField is not null) currentField.TranslateRules.Add(trans);
                    else doc.TranslateRules.Add(trans);
                    break;

                case "math":
                    if (currentField is not null) ParseMath(value, currentField, lineNumber);
                    else ParseMath(value, doc, lineNumber);
                    break;


                case "diff":
                case "derivative":
                    doc.CalculusSteps.Add(new CalculusStep(CalculusOperation.Differentiate, value.Trim()));
                    break;

                case "integral":
                    doc.CalculusSteps.Add(ParseIntegral(value, lineNumber));
                    break;

                case "limit":
                    doc.CalculusSteps.Add(ParseLimit(value, lineNumber));
                    break;

                case "end":
                    return FinishDocument(doc);

                case "input1":
                    doc.LegacyInput1Label = value.Trim();
                    doc.Inputs.Add(CreateLegacyInputDefinition(doc, "input1", doc.LegacyInput1Label));
                    doc.Deprecations.Add(new NodDeprecation(
                        "input1",
                        IsTextRuleDocument(doc)
                            ? "input1 is legacy-compatible in Syscalculator 2.0 beta and remains supported through 2.x; migrate to 'input texta ...' before 3.0."
                            : "input1 is legacy-compatible in Syscalculator 2.0 beta and remains supported through 2.x; migrate to 'input x ...' before 3.0."));
                    break;

                case "input2":
                    doc.LegacyInput2Label = value.Trim();
                    doc.Outputs.Add(CreateLegacyOutputDefinition(doc.LegacyInput2Label));
                    doc.Deprecations.Add(new NodDeprecation(
                        "input2",
                        IsTextRuleDocument(doc)
                            ? "input2 is legacy-compatible in Syscalculator 2.0 beta and remains supported through 2.x; migrate to 'input textb ...' before 3.0."
                            : "input2 is legacy-compatible in Syscalculator 2.0 beta and remains supported through 2.x; migrate to 'output y ...' before 3.0."));
                    break;

                // Accepted but ignored by this prototype.
                case "urln":
                case "symb1":
                case "symb2":
                case "symb3":
                case "symb4":
                case "symba1":
                case "symba2":
                case "symba3":
                case "symba4":
                case "tformat":
                case "result":
                case "resfou":
                case "errres":
                case "indoprint":
                case "indoend":
                case "ldnr":
                case "date":
                case "phcode":
                    break;

                default:
                    throw new FormatException($"Line {lineNumber}: unknown NOD command '{keyword}'.");
            }
        }

        return FinishDocument(doc);
    }

    private static NodDocument FinishDocument(NodDocument doc)
    {
        FinalizeLegacyInputDefinitions(doc);
        ValidateLegacyInput2Usage(doc);
        return doc;
    }

    private static void ValidateMode(string value, int lineNumber)
    {
        _ = value;
        _ = lineNumber;
    }

    private static void FinalizeLegacyInputDefinitions(NodDocument doc)
    {
        if (!IsTextRuleDocument(doc))
        {
            EnsureLegacyOutputDefinition(doc);
            return;
        }

        for (var i = 0; i < doc.Inputs.Count; i++)
        {
            var input = doc.Inputs[i];
            if (input.Name.Equals("x", StringComparison.OrdinalIgnoreCase) && doc.LegacyInput1Label is not null)
                doc.Inputs[i] = new NodValueDefinition("texta", input.Label, "text");
        }

        if (doc.LegacyInput2Label is not null && !doc.Inputs.Any(input => input.Name.Equals("textb", StringComparison.OrdinalIgnoreCase)))
            doc.Inputs.Add(new NodValueDefinition("textb", doc.LegacyInput2Label, "text"));

        for (var i = 0; i < doc.Deprecations.Count; i++)
        {
            var dep = doc.Deprecations[i];
            if (dep.Command.Equals("input1", StringComparison.OrdinalIgnoreCase))
                doc.Deprecations[i] = dep with
                {
                    Message = "input1 is legacy-compatible in Syscalculator 2.0 beta and remains supported through 2.x; migrate to 'input texta ...' before 3.0."
                };
            else if (dep.Command.Equals("input2", StringComparison.OrdinalIgnoreCase))
                doc.Deprecations[i] = dep with
                {
                    Message = "input2 is legacy-compatible in Syscalculator 2.0 beta and remains supported through 2.x; migrate to 'input textb ...' before 3.0."
                };
        }
    }

    private static void EnsureLegacyOutputDefinition(NodDocument doc)
    {
        if (doc.LegacyInput2Label is null)
            return;

        if (doc.Outputs.Count == 0)
            doc.Outputs.Add(CreateLegacyOutputDefinition(doc.LegacyInput2Label));
    }

    private static NodValueDefinition CreateLegacyInputDefinition(NodDocument doc, string command, string? label)
    {
        if (IsTextRuleDocument(doc))
            return new NodValueDefinition("texta", label, "text");

        return new NodValueDefinition("x", label);
    }

    private static NodValueDefinition CreateLegacyOutputDefinition(string? label)
    {
        return new NodValueDefinition("y", label);
    }

    private static void ValidateLegacyInput2Usage(NodDocument doc)
    {
        if (doc.LegacyInput2Label is null || IsTextRuleDocument(doc))
            return;

        var hasModernMultiInput = doc.Inputs.Any(input =>
            !input.Name.Equals("x", StringComparison.OrdinalIgnoreCase) &&
            !input.Name.Equals("texta", StringComparison.OrdinalIgnoreCase));

        if (!hasModernMultiInput && !LooksLikeLimitedVectorOrMatrix(doc.Name))
            return;

        throw new FormatException("input2 is not supported for 2D vector or 2x2 matrix NOD. Use explicit input x/input y for values and inputr y/output y for the reverse or result side.");
    }

    private static bool LooksLikeLimitedVectorOrMatrix(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var normalized = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();
        return normalized.Contains("2d vector", StringComparison.Ordinal) ||
               normalized.Contains("vector 2d", StringComparison.Ordinal) ||
               normalized.Contains("2x2 matrix", StringComparison.Ordinal) ||
               normalized.Contains("matrix 2x2", StringComparison.Ordinal);
    }

    private static NodValueDefinition ParseReverseValueDefinition(string value)
    {
        value = value.Trim();
        if (value.Length == 0)
            return new NodValueDefinition("y", null);

        var parts = value.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
            return new NodValueDefinition("y", Unquote(parts[0]));

        var first = parts[0];
        var rest = parts[1];
        if (first.Equals("y", StringComparison.OrdinalIgnoreCase) ||
            first.Equals("output", StringComparison.OrdinalIgnoreCase) ||
            first.Equals("result", StringComparison.OrdinalIgnoreCase))
            return new NodValueDefinition("y", Unquote(rest));

        return new NodValueDefinition("y", Unquote(value));
    }

    private static bool IsTextRuleDocument(NodDocument doc)
    {
        return doc.TranslateRules.Count > 0 || doc.ChangeRules.Count > 0;
    }

    private static NodValueDefinition ParseValueDefinition(string value, string fallbackName)
    {
        value = value.Trim();
        if (value.Length == 0)
            return new NodValueDefinition(fallbackName, null);

        var parts = value.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var first = parts[0];
        var rest = parts.Length > 1 ? parts[1] : "";

        if (first.Equals("text", StringComparison.OrdinalIgnoreCase) ||
            first.Equals("texta", StringComparison.OrdinalIgnoreCase) ||
            first.Equals("textb", StringComparison.OrdinalIgnoreCase) ||
            first.Equals("telefoon", StringComparison.OrdinalIgnoreCase) ||
            first.Equals("phone", StringComparison.OrdinalIgnoreCase) ||
            first.Equals("telephone", StringComparison.OrdinalIgnoreCase))
        {
            var kind = IsPhoneInputKind(first) ? "phone" : "text";
            if (string.IsNullOrWhiteSpace(rest))
                return new NodValueDefinition(NormalizeInputKindName(first), null, kind);

            var label = LooksLikeIdentifier(rest) && first.Equals("text", StringComparison.OrdinalIgnoreCase)
                ? null
                : Unquote(rest);
            var name = LooksLikeIdentifier(rest) && IsGenericTypedInput(first) && !first.Equals("telefoon", StringComparison.OrdinalIgnoreCase)
                ? rest
                : NormalizeInputKindName(first);
            return new NodValueDefinition(name, label, kind);
        }

        return new NodValueDefinition(first, string.IsNullOrWhiteSpace(rest) ? null : Unquote(rest));
    }

    private static bool IsGenericTypedInput(string value)
    {
        return value.Equals("text", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("telefoon", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("phone", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("telephone", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPhoneInputKind(string value)
    {
        return value.Equals("telefoon", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("phone", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("telephone", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeInputKindName(string value)
    {
        return IsPhoneInputKind(value) ? "phone" : value.ToLowerInvariant();
    }

    private static bool LooksLikeIdentifier(string value)
    {
        return Regex.IsMatch(value.Trim(), @"^[A-Za-z_][A-Za-z0-9_]*$");
    }

    // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseMath.
    private static void ParseMath(string value, NodDocument doc, int lineNumber)
    {

        if (TryParseCalculusMath(value, lineNumber, out var calculusStep))
        {
            doc.CalculusSteps.Add(calculusStep);
            return;
        }

        if (TryRewriteVectorMath(value, out var vectorExpression))
            AddExpressionMath(doc, vectorExpression);
        else if (TryRewriteLegacyModMath(value, out var modExpression))
            AddExpressionMath(doc, modExpression);
        else if (TryParseLegacyMath(value, out var legacyStep))
            AddLegacyMath(doc, legacyStep);
        else
        {
            ValidateExpression(value, lineNumber);
            AddExpressionMath(doc, value.Trim());
        }
    }

    // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseMath.
    private static void ParseMath(string value, DataFieldDefinition field, int lineNumber)
    {
        if (TryRewriteVectorMath(value, out var vectorExpression))
            AddExpressionMath(field, vectorExpression);
        else if (TryRewriteLegacyModMath(value, out var modExpression))
            AddExpressionMath(field, modExpression);
        else if (TryParseLegacyMath(value, out var legacyStep))
            AddLegacyMath(field, legacyStep);
        else
        {
            ValidateExpression(value, lineNumber);
            AddExpressionMath(field, value.Trim());
        }
    }

    private static void AddLegacyMath(NodDocument doc, MathStep10 step)
    {
        if (doc.MathExpressions20.Count > 0)
            doc.MathExpressions20.Add(ToExpression(step));
        else
            doc.LegacyMathSteps.Add(step);
    }

    private static void AddExpressionMath(NodDocument doc, string expression)
    {
        PromoteLegacyMath(doc.LegacyMathSteps, doc.MathExpressions20);
        doc.MathExpressions20.Add(expression);
    }

    private static void AddLegacyMath(DataFieldDefinition field, MathStep10 step)
    {
        if (field.MathExpressions20.Count > 0)
            field.MathExpressions20.Add(ToExpression(step));
        else
            field.LegacyMathSteps.Add(step);
    }

    private static void AddExpressionMath(DataFieldDefinition field, string expression)
    {
        PromoteLegacyMath(field.LegacyMathSteps, field.MathExpressions20);
        field.MathExpressions20.Add(expression);
    }

    private static void PromoteLegacyMath(List<MathStep10> legacySteps, List<string> expressions)
    {
        foreach (var step in legacySteps)
            expressions.Add(ToExpression(step));

        legacySteps.Clear();
    }

    private static string ToExpression(MathStep10 step)
        => $"ans {step.Operator} {step.Number.ToString(CultureInfo.InvariantCulture)}";

    // Zoek/commentaar: Probeert tekst veilig te parsen voor TryParseLegacyMath.
    private static bool TryParseLegacyMath(string value, out MathStep10 step)
    {
        step = default!;
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 3) return false;
        if (!parts[0].Equals("ans", StringComparison.OrdinalIgnoreCase)) return false;
        if (parts[1].Length != 1) return false;

        var op = parts[1][0];
        if (op is not ('+' or '-' or '*' or '/')) return false;

        if (!TryParseFlexibleDecimal(parts[2], out var number)) return false;

        step = new MathStep10(op, number);
        return true;
    }


    /// <summary>
    /// Ondersteunt korte NOD-vectornotatie:
    /// math vec 3 4
    /// math vec (3,4,12)
    /// wordt intern:
    /// length(vec(3,4))
    /// length(vec(3,4,12))
    /// De uitkomst is de lengte van de vectorpijl vanaf de oorsprong.
    /// </summary>
    private static bool TryRewriteVectorMath(string value, out string expression)
    {
        expression = "";

        var trimmed = value.Trim();
        var firstSpace = trimmed.IndexOfAny([' ', '\t']);
        if (firstSpace < 0)
            return false;

        var op = trimmed[..firstSpace].Trim();
        if (!op.Equals("vec", StringComparison.OrdinalIgnoreCase) &&
            !op.Equals("vector", StringComparison.OrdinalIgnoreCase))
            return false;

        var rest = trimmed[(firstSpace + 1)..].Trim();
        if (rest.Length == 0)
            throw new FormatException("math vec expects 2 or 3 components.");

        var components = SplitVectorComponents(rest);
        if (components.Count is not (2 or 3))
            throw new FormatException("math vec expects 2 or 3 components.");

        expression = $"length(vec({string.Join(",", components)}))";
        return true;
    }

    private static IReadOnlyList<string> SplitVectorComponents(string value)
    {
        value = value.Trim();
        if (value.StartsWith('(') && value.EndsWith(')'))
            value = value[1..^1].Trim();

        var separator = value.Contains(',') ? ',' : ' ';
        return value
            .Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => part.Length > 0)
            .ToArray();
    }


    /// <summary>
    /// Ondersteunt NOD 2.0 modulo in een oude leesbare stijl:
    /// math ans mod 2
    /// wordt intern:
    /// mod(ans,2)
    ///
    /// Dit gaat niet naar MathStep10, omdat MathStep10 alleen + - * / ondersteunt.
    /// </summary>
    private static bool TryRewriteLegacyModMath(string value, out string expression)
    {
        expression = "";

        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 3)
            return false;

        if (!parts[0].Equals("ans", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!parts[1].Equals("mod", StringComparison.OrdinalIgnoreCase) &&
            !parts[1].Equals("%", StringComparison.OrdinalIgnoreCase))
            return false;

        expression = $"mod(ans,{parts[2]})";
        return true;
    }

    // Zoek/commentaar: Controleert of invoer geldig is voor ValidateExpression.
    private static void ValidateExpression(string expression, int lineNumber)
    {
        if (Regex.IsMatch(expression, @"\banse\s*\^", RegexOptions.IgnoreCase))
            throw new FormatException($"Line {lineNumber}: use 'ans * e^2', not 'anse^2'.");
    }


    /// <summary>
    /// Parse calculus binnen het math-commando.
    ///
    /// Ondersteunde syntax:
    /// math diff ans^2
    /// math derivative ans^2
    /// math integral 0,1 ans^2
    /// math limit 0 sin(ans)/ans
    ///
    /// Oude losse syntax diff/integral/limit kan eventueel ook blijven,
    /// maar de voorkeursvorm is: math ...
    /// </summary>
    private static bool TryParseCalculusMath(string value, int lineNumber, out CalculusStep step)
    {
        step = default!;

        var trimmed = value.Trim();
        var firstSpace = trimmed.IndexOfAny([' ', '\t']);

        if (firstSpace < 0)
            return false;

        var op = trimmed[..firstSpace].Trim().ToLowerInvariant();
        var rest = trimmed[(firstSpace + 1)..].Trim();

        switch (op)
        {
            case "diff":
            case "derivative":
                step = new CalculusStep(CalculusOperation.Differentiate, rest);
                return true;

            case "integral":
                step = ParseIntegral(rest, lineNumber);
                return true;

            case "limit":
                step = ParseLimit(rest, lineNumber);
                return true;

            default:
                return false;
        }
    }

    // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseIntegral.
    private static CalculusStep ParseIntegral(string value, int lineNumber)
    {
        // Syntax:
        // 0,1 ans^2
        //
        // Probleem: decimale komma bestaat ook.
        // Daarom wordt hier "0,1" geinterpreteerd als range als beide kanten getal zijn.
        var space = value.IndexOfAny([' ', '\t']);
        if (space < 0)
            throw new FormatException(LinePrefix(lineNumber) + "expected integral range and expression, e.g. 'integral 0,1 ans^2'.");

        var range = value[..space].Trim();
        var expression = value[(space + 1)..].Trim();

        var comma = range.IndexOf(',');
        if (comma < 0)
            throw new FormatException(LinePrefix(lineNumber) + "expected integral range a,b.");

        var a = ParseFlexibleDecimal(range[..comma]);
        var b = ParseFlexibleDecimal(range[(comma + 1)..]);

        return new CalculusStep(CalculusOperation.Integrate, expression, a, b);
    }

    // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseLimit.
    private static CalculusStep ParseLimit(string value, int lineNumber)
    {
        // Syntax:
        // 0 sin(ans)/ans
        var space = value.IndexOfAny([' ', '\t']);
        if (space < 0)
            throw new FormatException(LinePrefix(lineNumber) + "expected limit point and expression, e.g. 'limit 0 sin(ans)/ans'.");

        var point = ParseFlexibleDecimal(value[..space]);
        var expression = value[(space + 1)..].Trim();

        return new CalculusStep(CalculusOperation.Limit, expression, point);
    }

    // Zoek/commentaar: Methode LinePrefix: centrale logica voor deze stap.
    private static string LinePrefix(int lineNumber)
    {
        return lineNumber > 0 ? $"Line {lineNumber}: " : "";
    }

    // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseGiven.
    private static void ParseGiven(string value, EquationDefinition equation, int lineNumber)
    {
        var eq = value.IndexOf('=');
        if (eq < 0) throw new FormatException($"Line {lineNumber}: expected given name = value.");

        var name = value[..eq].Trim();
        var numberText = value[(eq + 1)..].Trim();
        equation.GivenValues[name] = ParseFlexibleDecimal(numberText);
    }

    // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseConstraint.
    private static ConstraintRule ParseConstraint(string value, int lineNumber)
    {
        var match = Regex.Match(value, @"^\s*([A-Za-z_\u03C0][A-Za-z0-9_\u03C0]*)\s*(>=|<=|=|>|<)\s*(.+?)\s*$");
        if (!match.Success)
            throw new FormatException($"Line {lineNumber}: invalid constraint.");

        return new ConstraintRule(
            match.Groups[1].Value,
            match.Groups[2].Value,
            ParseFlexibleDecimal(match.Groups[3].Value)
        );
    }

    // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParsePhoneFormat.
    private static void ParsePhoneFormat(string value, DataFieldDefinition field)
    {
        field.PhoneFormat ??= new PhoneFormatOptions();

        var parts = value.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var key = parts[0].ToLowerInvariant();
        var val = parts.Length > 1 ? Unquote(parts[1].Trim()) : "true";

        switch (key)
        {
            case "country": field.PhoneFormat.Country = val; break;
            case "keep_separator": field.PhoneFormat.KeepSeparator = val; break;
            case "remove_spaces": field.PhoneFormat.RemoveSpaces = ParseBool(val); break;
            case "remove_dots": field.PhoneFormat.RemoveDots = ParseBool(val); break;
            case "remove_slashes": field.PhoneFormat.RemoveSlashes = ParseBool(val); break;
            case "remove_parentheses": field.PhoneFormat.RemoveParentheses = ParseBool(val); break;
            case "remove_text_prefix": field.PhoneFormat.RemoveTextPrefix = ParseBool(val); break;
            case "normalize_international": field.PhoneFormat.NormalizeInternational = ParseBool(val); break;
        }
    }

    // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseLookupMatch.
    private static void ParseLookupMatch(string value, LookupDefinition lookup, int lineNumber)
    {
        var eq = value.IndexOf('=');
        if (eq < 0) throw new FormatException($"Line {lineNumber}: expected match left = right.");

        lookup.LeftField = value[..eq].Trim();
        lookup.RightField = value[(eq + 1)..].Trim();
    }

    // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseBool.
    private static bool ParseBool(string value)
    {
        return value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Trim().Equals("1", StringComparison.OrdinalIgnoreCase);
    }

    // Zoek/commentaar: Probeert tekst veilig te parsen voor TryParseFlexibleDecimal.
    private static bool TryParseFlexibleDecimal(string value, out decimal parsed)
    {
        value = value.Trim().Replace(',', '.');
        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
    }

    // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseFlexibleDecimal.
    public static decimal ParseFlexibleDecimal(string value)
    {
        if (TryParseFlexibleDecimal(value, out var parsed)) return parsed;
        throw new FormatException($"Invalid decimal number: '{value}'");
    }

    // Zoek/commentaar: Methode SplitKeyword: centrale logica voor deze stap.
    private static (string Keyword, string Value) SplitKeyword(string line)
    {
        var index = line.IndexOfAny([' ', '\t']);
        return index < 0 ? (line, "") : (line[..index].Trim(), line[(index + 1)..].Trim());
    }

    private static T ParsePair<T>(string value, Func<string, string, T> create, int lineNumber)
    {
        var comma = value.IndexOf(',');
        if (comma < 0) throw new FormatException($"Line {lineNumber}: expected 'old,new'.");
        return create(value[..comma].Trim(), value[(comma + 1)..].Trim());
    }

    // Zoek/commentaar: Methode Unquote: centrale logica voor deze stap.
    private static string Unquote(string value)
    {
        value = value.Trim();
        return value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"')
            ? value[1..^1]
            : value;
    }
}

