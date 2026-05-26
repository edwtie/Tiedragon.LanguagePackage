/*
NOD SYSTEM -- ENGINE

Dit bestand voert het geparste NodDocument uit.

Taken:
- NOD 1.0 legacy math uitvoeren
- NOD 2.0 expressions uitvoeren
- chg uitvoeren
- trans uitvoeren
- reverse uitvoeren
- equation/data naar aparte engines doorsturen

Ontwerpregel:
NOD 1.0 reverse kan automatisch.
NOD 2.0 reverse moet expliciet via reverse-regel.
*/

using System.Globalization;

namespace Tiedragon.NodSystem.Core;

// Zoek/commentaar: Type-overzicht: class NodEngine bevat de hoofdlogica/data voor dit onderdeel.
public static class NodEngine
{
    // Zoek/commentaar: Voert een conversie uit voor ConvertForward.
    public static NodResult ConvertForward(NodDocument doc, string input)
    {
        if (doc.LegacyMathSteps.Count > 0)
        {
            var ans = NodParser.ParseFlexibleDecimal(input);
            foreach (var step in doc.LegacyMathSteps) ans = ApplyMathStep(ans, step);
            return new NodResult(Format(ans, doc.Format), ans);
        }

        if (doc.MathExpressions20.Count > 0)
        {
            var ans = NodParser.ParseFlexibleDecimal(input);
            foreach (var expression in doc.MathExpressions20)
                ans = NodExpressionEvaluator.Evaluate(expression, ans);
            return new NodResult(Format(ans, doc.Format), ans);
        }

        if (doc.CalculusSteps.Count > 0)
        {
            var ans = NodParser.ParseFlexibleDecimal(input);
            foreach (var step in doc.CalculusSteps)
                ans = CalculusEngine.Apply(step, ans);
            return new NodResult(Format(ans, doc.Format), ans);
        }

        if (doc.ChangeRules.Count > 0) return new NodResult(ApplyChange(input, doc.ChangeRules, false), null);
        if (doc.TranslateRules.Count > 0) return new NodResult(ApplyTranslate(input, doc.TranslateRules, false), null);

        return new NodResult(input, null);
    }

    // Zoek/commentaar: Voert een conversie uit voor ConvertReverse.
    public static NodResult ConvertReverse(NodDocument doc, string input)
    {
        if (doc.LegacyMathSteps.Count > 0)
        {
            var ans = NodParser.ParseFlexibleDecimal(input);
            foreach (var step in doc.LegacyMathSteps.AsEnumerable().Reverse().Select(s => s.Reverse()))
                ans = ApplyMathStep(ans, step);
            return new NodResult(Format(ans, doc.Format), ans);
        }

        if (doc.MathExpressions20.Count > 0)
        {
            var ans = NodParser.ParseFlexibleDecimal(input);

            if (!string.IsNullOrWhiteSpace(doc.ReverseExpression))
            {
                ans = NodExpressionEvaluator.Evaluate(doc.ReverseExpression, ans);
                return new NodResult(Format(ans, doc.Format), ans);
            }

            if (doc.MathExpressions20.Count == 1 &&
                NodReverse.TryCreateReverseExpression(doc.MathExpressions20[0], out var autoReverse))
            {
                ans = NodExpressionEvaluator.Evaluate(autoReverse, ans);
                return new NodResult(Format(ans, doc.Format), ans);
            }

            throw new InvalidOperationException(
                "NOD 2.0 expression needs explicit reverse expression, unless it is a simple reversible function."
            );
        }

        if (doc.ChangeRules.Count > 0) return new NodResult(ApplyChange(input, doc.ChangeRules, true), null);
        if (doc.TranslateRules.Count > 0) return new NodResult(ApplyTranslate(input, doc.TranslateRules, true), null);

        return new NodResult(input, null);
    }

    // Zoek/commentaar: Lost een vergelijking of berekening op voor SolveEquation.
    public static EquationResult SolveEquation(NodDocument doc)
    {
        if (doc.Equation is null) throw new InvalidOperationException("No equation block found.");
        return EquationEngine.Solve(doc.Equation);
    }

    // Zoek/commentaar: Methode TransformRow: centrale logica voor deze stap.
    public static IReadOnlyList<FieldTransformResult> TransformRow(NodDocument doc, IDictionary<string, string> row)
    {
        if (doc.Data is null) throw new InvalidOperationException("No data block found.");
        return DataTransformEngine.TransformRow(doc.Data, row);
    }

    // Zoek/commentaar: Methode DescribeDataPlan: centrale logica voor deze stap.
    public static string DescribeDataPlan(NodDocument doc)
    {
        if (doc.Data is null) return "No data plan found.";
        return DataPlanDescriber.Describe(doc.Data);
    }


    /// <summary>
    /// Voert math uit en bewaart alle tussenstappen in een CalculationTrace.
    ///
    /// Dit is vooral bedoeld voor Syscalculator 2.0:
    /// input1 -> input2 en daarna direct terug via trace.
    /// </summary>
    public static NodTraceResult ConvertForwardWithTrace(NodDocument doc, string input)
    {
        var ans = NodParser.ParseFlexibleDecimal(input);
        var trace = new CalculationTrace(ans);

        if (doc.LegacyMathSteps.Count > 0)
        {
            foreach (var step in doc.LegacyMathSteps)
            {
                var before = ans;
                ans = ApplyMathStep(ans, step);
                var reverseStep = step.Reverse();
                var reverseExpression = $"ans {reverseStep.Operator} {reverseStep.Number}";
                trace.AddStep(new CalculationTraceStep($"ans {step.Operator} {step.Number}", before, ans, reverseExpression));
            }

            return new NodTraceResult(Format(ans, doc.Format), ans, trace);
        }

        if (doc.MathExpressions20.Count > 0)
        {
            foreach (var expression in doc.MathExpressions20)
            {
                var before = ans;
                ans = NodExpressionEvaluator.Evaluate(expression, ans);

                NodReverse.TryCreateReverseExpression(expression, out var autoReverse);

                trace.AddStep(new CalculationTraceStep(
                    expression,
                    before,
                    ans,
                    string.IsNullOrWhiteSpace(autoReverse) ? null : autoReverse
                ));
            }

            return new NodTraceResult(Format(ans, doc.Format), ans, trace);
        }

        if (doc.CalculusSteps.Count > 0)
        {
            foreach (var step in doc.CalculusSteps)
            {
                var before = ans;
                ans = CalculusEngine.Apply(step, ans);

                // Calculus-stappen zijn in Alpha nog niet automatisch reversebaar.
                trace.AddStep(new CalculationTraceStep(
                    DescribeCalculusStep(step),
                    before,
                    ans,
                    null,
                    CalculusEngine.DescribeSteps(step, before, ans)
                ));
            }

            return new NodTraceResult(Format(ans, doc.Format), ans, trace);
        }

        throw new InvalidOperationException("Trace is only available for math-based NOD documents.");
    }

    /// <summary>
    /// Rekent terug via de bewaarde trace.
    ///
    /// Dit is geen algebra. De engine loopt gewoon de bewaarde stappen achteruit.
    /// Als een stap geen AutoReverseExpression heeft, stopt de engine met een foutmelding.
    /// </summary>
    public static NodResult ConvertReverseFromTrace(NodDocument doc, CalculationTrace trace, string? input = null)
    {
        var ans = string.IsNullOrWhiteSpace(input)
            ? trace.FinalValue
            : NodParser.ParseFlexibleDecimal(input);

        foreach (var step in trace.Steps.AsEnumerable().Reverse())
        {
            if (string.IsNullOrWhiteSpace(step.AutoReverseExpression))
                throw new InvalidOperationException(
                    $"No automatic reverse available for trace step: {step.Expression}. Add explicit reverse or split the formula into reversible steps."
                );

            ans = NodExpressionEvaluator.Evaluate(step.AutoReverseExpression, ans);
        }

        return new NodResult(Format(ans, doc.Format), ans);
    }


    // Zoek/commentaar: Past een regel, instelling of bewerking toe voor ApplyMathStep.
    internal static decimal ApplyMathStep(decimal ans, MathStep10 step) => step.Operator switch
    {
        '+' => ans + step.Number,
        '-' => ans - step.Number,
        '*' => ans * step.Number,
        '/' => ans / step.Number,
        _ => throw new InvalidOperationException($"Unknown operator: {step.Operator}")
    };

    private static string DescribeCalculusStep(CalculusStep step)
    {
        return step.Operation switch
        {
            CalculusOperation.Differentiate => $"diff {step.Expression}",
            CalculusOperation.Integrate => $"integral {Format(step.A ?? 0, null)},{Format(step.B ?? 0, null)} {step.Expression}",
            CalculusOperation.Limit => $"limit {Format(step.A ?? 0, null)} {step.Expression}",
            _ => step.Expression
        };
    }

    // Zoek/commentaar: Past een regel, instelling of bewerking toe voor ApplyChange.
    internal static string ApplyChange(string input, IReadOnlyList<ChangeRule> rules, bool reverse)
    {
        var patternResult = ApplyPatternChange(input, rules, reverse);
        if (patternResult is not null)
            return patternResult;

        ChangeRule? exactRule = null;
        var exactFrom = "";

        foreach (var rule in rules)
        {
            if (IsPatternChangeRule(reverse ? rule.NewPrefix : rule.OldPrefix))
                continue;

            var from = ChangePatternPrefix(reverse ? rule.NewPrefix : rule.OldPrefix);

            if (!input.StartsWith(from, StringComparison.OrdinalIgnoreCase))
                continue;

            if (from.Length < exactFrom.Length)
                continue;

            if (from.Length == exactFrom.Length && exactRule is not null)
            {
                exactRule = null;
                continue;
            }

            exactRule = rule;
            exactFrom = from;
        }

        if (exactRule is not null)
        {
            var to = ChangePatternPrefix(reverse ? exactRule.OldPrefix : exactRule.NewPrefix);
            return to + input[exactFrom.Length..];
        }

        var compactInput = CompactTranslateKey(input);
        ChangeRule? bestRule = null;
        var bestFrom = "";

        foreach (var rule in rules)
        {
            if (IsPatternChangeRule(reverse ? rule.NewPrefix : rule.OldPrefix))
                continue;

            var from = ChangePatternPrefix(reverse ? rule.NewPrefix : rule.OldPrefix);
            var compactFrom = CompactTranslateKey(from);

            if (compactFrom.Length == 0 || !compactInput.StartsWith(compactFrom, StringComparison.OrdinalIgnoreCase))
                continue;

            if (compactFrom.Length < bestFrom.Length)
                continue;

            if (compactFrom.Length == bestFrom.Length && bestRule is not null)
            {
                bestRule = null;
                continue;
            }

            bestRule = rule;
            bestFrom = compactFrom;
        }

        if (bestRule is not null)
        {
            var to = ChangePatternPrefix(reverse ? bestRule.OldPrefix : bestRule.NewPrefix);
            return to + compactInput[bestFrom.Length..];
        }

        return input;
    }

    // NOD 2.0 chg: patroonvervanging met x-capture. NOD 1.x chg blijft klassieke prefixvervanging.
    private static string? ApplyPatternChange(string input, IReadOnlyList<ChangeRule> rules, bool reverse)
    {
        string? bestOutput = null;
        var bestScore = -1;
        var ambiguous = false;

        foreach (var rule in rules)
        {
            var from = reverse ? rule.NewPrefix : rule.OldPrefix;
            var to = reverse ? rule.OldPrefix : rule.NewPrefix;

            if (!IsPatternChangeRule(from))
                continue;

            ValidateChangePattern(from, to);

            if (!TryApplyPatternChange(input, from, to, out var output))
                continue;

            var score = PatternSpecificityScore(from);
            if (score > bestScore)
            {
                bestOutput = output;
                bestScore = score;
                ambiguous = false;
            }
            else if (score == bestScore)
            {
                ambiguous = true;
            }
        }

        return ambiguous ? null : bestOutput;
    }

    private static bool IsPatternChangeRule(string value)
        => value.Contains('x', StringComparison.OrdinalIgnoreCase);

    private static void ValidateChangePattern(string fromPattern, string toPattern)
    {
        var leftCaptures = CountPatternCaptures(fromPattern);
        var rightCaptures = CountPatternCaptures(toPattern);

        if (leftCaptures != rightCaptures)
        {
            throw new InvalidOperationException(
                $"Invalid chg pattern: left pattern captures {leftCaptures} x-position(s), but right pattern uses {rightCaptures} x-position(s).");
        }
    }

    private static int CountPatternCaptures(string pattern)
        => pattern.Count(ch => ch is 'x' or 'X');

    private static int PatternSpecificityScore(string pattern)
    {
        var clean = CleanChangePattern(pattern);
        var fixedDigits = clean.Count(char.IsDigit);
        return fixedDigits * 1000 + clean.Length;
    }

    private static string CleanChangePattern(string pattern)
        => string.Concat(pattern.Where(ch => char.IsDigit(ch) || ch is 'x' or 'X'));

    private static string DigitsOnly(string value)
        => string.Concat(value.Where(char.IsDigit));

    private static bool TryApplyPatternChange(string input, string fromPattern, string toPattern, out string output)
    {
        output = input;

        var inputDigits = DigitsOnly(input);
        var cleanFromPattern = CleanChangePattern(fromPattern);

        if (inputDigits.Length != cleanFromPattern.Length)
            return false;

        var captures = new List<char>();
        for (var i = 0; i < cleanFromPattern.Length; i++)
        {
            var patternChar = cleanFromPattern[i];
            var inputDigit = inputDigits[i];

            if (patternChar is 'x' or 'X')
            {
                captures.Add(inputDigit);
                continue;
            }

            if (patternChar != inputDigit)
                return false;
        }

        var captureIndex = 0;
        var result = new List<char>();
        foreach (var ch in toPattern)
        {
            if (ch == ' ')
                continue;

            if (ch is 'x' or 'X')
            {
                result.Add(captures[captureIndex++]);
                continue;
            }

            if (char.IsDigit(ch) || ch == '-')
                result.Add(ch);
        }

        output = new string(result.ToArray());
        return true;
    }

    // NOD 1.x chg: klassieke prefixvervanging. Alleen de prefix vóór x telt hier.
    private static string ChangePatternPrefix(string value)
    {
        var index = value.IndexOf('x', StringComparison.OrdinalIgnoreCase);
        return index < 0 ? value : value[..index];
    }

    // Zoek/commentaar: Past een regel, instelling of bewerking toe voor ApplyTranslate.
    internal static string ApplyTranslate(string input, IReadOnlyList<TranslateRule> rules, bool reverse)
    {
        if (TryApplyTranslateDirection(input, rules, reverse, out var result))
            return result;

        return input;
    }

    // Zoek/commentaar: Probeert een trans-richting, eerst exact en daarna slim compact.
    private static bool TryApplyTranslateDirection(string input, IReadOnlyList<TranslateRule> rules, bool reverse, out string result)
    {
        foreach (var rule in rules)
        {
            var from = reverse ? rule.NewValue : rule.OldValue;
            var to = reverse ? rule.OldValue : rule.NewValue;

            if (input.Equals(from, StringComparison.OrdinalIgnoreCase))
            {
                result = to;
                return true;
            }
        }

        var compactInput = CompactTranslateKey(input);
        if (compactInput.Length == 0)
        {
            result = input;
            return false;
        }

        string? compactMatch = null;

        foreach (var rule in rules)
        {
            var from = reverse ? rule.NewValue : rule.OldValue;
            var to = reverse ? rule.OldValue : rule.NewValue;

            if (!compactInput.Equals(CompactTranslateKey(from), StringComparison.OrdinalIgnoreCase))
                continue;

            if (compactMatch is not null)
            {
                result = input;
                return false;
            }

            compactMatch = to;
        }

        if (compactMatch is not null)
        {
            result = compactMatch;
            return true;
        }

        result = input;
        return false;
    }

    // Zoek/commentaar: Maakt trans slimmer zonder exacte tekstregels te breken.
    private static string CompactTranslateKey(string text)
    {
        return string.Concat((text ?? "").Where(char.IsLetterOrDigit));
    }

    // Zoek/commentaar: Maakt tekst of waarden netjes leesbaar voor Format.
    internal static string Format(decimal value, string? format)
    {
        if (string.IsNullOrWhiteSpace(format)) return value.ToString(CultureInfo.InvariantCulture);
        if (format.Contains(".00") || format.Contains(",00")) return value.ToString("0.00", CultureInfo.InvariantCulture);
        if (format.Contains(".0") || format.Contains(",0")) return value.ToString("0.0", CultureInfo.InvariantCulture);
        return value.ToString(CultureInfo.InvariantCulture);
    }
}

