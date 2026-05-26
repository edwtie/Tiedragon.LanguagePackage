namespace Tiedragon.NodSystem.Core;

public sealed record SolverStepReport(
    string Title,
    string ResultText,
    IReadOnlyList<string> Steps,
    IReadOnlyList<string> StepMathMl,
    FormulaRuleCard? RuleCard,
    decimal? NumericValue,
    EquationGraphInfo? EquationGraph = null,
    CalculusGraphInfo? CalculusGraph = null,
    int? GraphRevealStepIndex = null
);

public sealed record FormulaRuleCard(
    string Title,
    string Subtitle,
    string FormulaMathMl,
    string ExampleText
);

public sealed record EquationGraphInfo(
    string SolveVariable,
    string LeftExpression,
    string RightExpression,
    IReadOnlyDictionary<string, decimal> GivenValues,
    decimal Solution
);

public sealed record CalculusGraphInfo(
    CalculusOperation Operation,
    string Expression,
    decimal InputValue,
    decimal OutputValue,
    decimal? A,
    decimal? B
);

public static class SolverStepBuilder
{
    public static SolverStepReport Build(NodDocument doc, string input)
    {
        if (doc.Equation is not null && !string.IsNullOrWhiteSpace(doc.Equation.EquationText))
            return BuildEquationReport(doc.Equation);

        if (doc.CalculusSteps.Count > 0)
            return BuildCalculusReport(doc, input);

        throw new InvalidOperationException("Solver steps are available for equation solve, solve diff and solve integral.");
    }

    private static SolverStepReport BuildEquationReport(EquationDefinition equation)
    {
        var result = EquationEngine.Solve(equation);
        var steps = new List<string>
        {
            $"Vergelijking: {equation.EquationText}",
            $"Te zoeken: {equation.SolveVariable}"
        };

        if (equation.GivenValues.Count > 0)
            steps.Add("Bekende waarden: " + string.Join(", ", equation.GivenValues.Select(pair => $"{pair.Key} = {Format(pair.Value)}")));

        var parts = equation.EquationText!.Split('=', 2);
        var left = parts[0].Trim();
        var right = parts[1].Trim();
        var solve = equation.SolveVariable!.Trim();

        if (left.Equals(solve, StringComparison.OrdinalIgnoreCase))
        {
            steps.Add($"{solve} staat al alleen links.");
            steps.Add($"Bereken de rechterkant: {right}.");
        }
        else if (right.Equals(solve, StringComparison.OrdinalIgnoreCase))
        {
            steps.Add($"{solve} staat al alleen rechts.");
            steps.Add($"Bereken de linkerkant: {left}.");
        }
        else
        {
            steps.Add("Bekijk de linkerkant en rechterkant als twee grafieken.");
            steps.Add($"Zoek het snijpunt waar {left} gelijk is aan {right}.");
        }

        foreach (var constraint in equation.Constraints.Where(c => c.Variable.Equals(solve, StringComparison.OrdinalIgnoreCase)))
            steps.Add($"Controleer voorwaarde: {constraint.Variable} {constraint.Operator} {Format(constraint.Value)}.");

        steps.Add($"Antwoord: {result.Variable} = {Format(result.Value)}.");
        var stepMath = steps.Select(BuildTextMathMl).ToArray();

        return new SolverStepReport(
            "Vergelijking oplossen",
            $"{result.Variable} = {Format(result.Value)}",
            steps,
            stepMath,
            BuildEquationRuleCard(left, right, solve),
            result.Value,
            new EquationGraphInfo(solve, left, right, new Dictionary<string, decimal>(equation.GivenValues, StringComparer.OrdinalIgnoreCase), result.Value));
    }

    private static SolverStepReport BuildCalculusReport(NodDocument doc, string input)
    {
        var trace = NodEngine.ConvertForwardWithTrace(doc, input);
        if (doc.CalculusSteps.Count == 1 &&
            TryBuildPowerDerivativeLesson(doc.CalculusSteps[0], trace, out var lessonReport))
        {
            return lessonReport;
        }

        var steps = new List<string>();
        var stepMath = new List<string>();
        if (doc.CalculusSteps.Count == 1)
        {
            var step = doc.CalculusSteps[0];
            steps.Add(BuildCalculusOpeningText(step));
            stepMath.Add(BuildCalculusOpeningMath(step));
        }

        steps.Add($"Startwaarde: x = {Format(trace.Trace.StartValue)}.");
        stepMath.Add(BuildAssignmentMathMl("x", Format(trace.Trace.StartValue)));

        foreach (var traceStep in trace.Trace.Steps)
        {
            steps.Add($"Bewerking: {traceStep.Expression}");
            stepMath.Add(BuildOperationMathMl(traceStep.Expression));
            if (traceStep.ExplanationSteps is not null)
            {
                steps.AddRange(traceStep.ExplanationSteps);
                stepMath.AddRange(BuildCalculusVisualSteps(traceStep.Expression, traceStep.ExplanationSteps));
            }
            steps.Add($"Uitkomst van deze stap: {Format(traceStep.OutputValue)}.");
            stepMath.Add(BuildAssignmentMathMl("uitkomst", Format(traceStep.OutputValue)));
        }

        steps.Add($"Antwoord: {trace.Text}.");
        stepMath.Add(BuildAssignmentMathMl("antwoord", trace.Text));

        return new SolverStepReport(
            "Calculus stap-voor-stap",
            trace.Text,
            steps,
            stepMath,
            BuildCalculusRuleCard(doc.CalculusSteps[0]),
            trace.NumericValue,
            CalculusGraph: doc.CalculusSteps.Count == 1
                ? new CalculusGraphInfo(
                    doc.CalculusSteps[0].Operation,
                    doc.CalculusSteps[0].Expression,
                    trace.Trace.StartValue,
                    trace.NumericValue,
                    doc.CalculusSteps[0].A,
                    doc.CalculusSteps[0].B)
                : null);
    }

    private static bool TryBuildPowerDerivativeLesson(CalculusStep step, NodTraceResult trace, out SolverStepReport report)
    {
        report = null!;
        if (!TryReadPowerDerivative(step.Expression, out var variable, out var exponent))
            return false;

        var newExponent = exponent - 1;
        var simplified = SimplifyPowerDerivative(variable, exponent);
        var hasStationaryPointAtZero = exponent > 1;
        var stationaryX = "0";
        var lessonResult = hasStationaryPointAtZero
            ? $"f'(x) = {simplified}; {variable} = {stationaryX}"
            : $"f'(x) = {simplified}";

        var steps = new List<string>
        {
            $"Dit is de functie: f(x) = {variable}^{exponent}.",
            "We gaan differentieren: f(x) wordt f'(x).",
            $"De macht {exponent} schuift naar links en wordt de coefficient.",
            $"De nieuwe macht wordt {exponent} - 1 = {newExponent}.",
            newExponent == 1
                ? $"Omdat {variable}^1 gewoon {variable} is, wordt de afgeleide {simplified}."
                : $"Vereenvoudig de afgeleide tot {simplified}.",
            $"Antwoord: f'(x) = {simplified}."
        };

        var stepMath = new List<string>
        {
            BuildFunctionPowerMathMl(variable, exponent),
            BuildDerivativeNotationAnimationSvg(variable, exponent),
            BuildPowerRuleAnimationSvg(variable, exponent),
            BuildExponentReductionAnimationSvg(variable, exponent, newExponent),
            newExponent == 1
                ? BuildRemoveOneExponentAnimationSvg(variable, exponent, simplified)
                : BuildPowerDerivativeSimplifyAnimationSvg(variable, exponent, newExponent, simplified),
            BuildDerivativeResultAnimationSvg(simplified)
        };

        int? graphRevealStepIndex = null;
        if (hasStationaryPointAtZero)
        {
            steps.Add($"Zoek waar de helling nul is: {simplified} = 0.");
            stepMath.Add(BuildSetDerivativeZeroAnimationSvg(simplified));

            if (newExponent == 1)
            {
                steps.Add($"Los op: {variable} = 0 / {exponent} = {stationaryX}.");
                stepMath.Add(BuildSolveZeroDerivativeAnimationSvg(variable, exponent, stationaryX));
            }
            else
            {
                steps.Add($"De coefficient {exponent} mag weg: {variable}^{newExponent} = 0.");
                stepMath.Add(BuildDivideCoefficientPowerAnimationSvg(variable, exponent, newExponent));
                steps.Add($"Alleen {variable} = 0 maakt {variable}^{newExponent} gelijk aan nul.");
                stepMath.Add(BuildRootZeroPowerAnimationSvg(variable, newExponent, stationaryX));
            }

            steps.Add("Teken nu de grafiek en markeer het punt waar de helling nul is.");
            stepMath.Add(BuildGraphReadyAnimationSvg(variable, stationaryX));
            graphRevealStepIndex = steps.Count - 1;
        }

        report = new SolverStepReport(
            "Calculus stap-voor-stap",
            lessonResult,
            steps,
            stepMath,
            BuildCalculusRuleCard(step),
            0,
            CalculusGraph: new CalculusGraphInfo(
                step.Operation,
                step.Expression,
                0,
                0,
                step.A,
                step.B),
            GraphRevealStepIndex: graphRevealStepIndex);
        return true;
    }

    private static string BuildCalculusOpeningText(CalculusStep step)
    {
        return step.Operation switch
        {
            CalculusOperation.Differentiate => $"We starten met de functie: {ToStudentExpression(step.Expression)}.",
            CalculusOperation.Integrate => $"We starten met de functie die we gaan primitiveren: {ToStudentExpression(step.Expression)}.",
            CalculusOperation.Limit => $"We starten met de functie voor de limiet: {ToStudentExpression(step.Expression)}.",
            _ => $"We starten met: {ToStudentExpression(step.Expression)}."
        };
    }

    private static string BuildCalculusOpeningMath(CalculusStep step)
    {
        if (TryReadPowerDerivative(step.Expression, out var variable, out var exponent))
        {
            return $$"""<math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mi>f</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>=</mo><msup><mi>{{variable}}</mi><mn>{{exponent}}</mn></msup></mrow></math>""";
        }

        var expression = ToStudentExpression(step.Expression);
        return $$"""<math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mi>f</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>=</mo><mtext>{{System.Net.WebUtility.HtmlEncode(expression)}}</mtext></mrow></math>""";
    }

    private static string ToStudentExpression(string expression)
    {
        var text = expression.Trim();
        var space = text.IndexOf(' ');
        if (space >= 0)
            text = text[(space + 1)..].Trim();

        return System.Text.RegularExpressions.Regex.Replace(text, @"\bans\b", "x", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static FormulaRuleCard BuildEquationRuleCard(string left, string right, string solve)
    {
        return new FormulaRuleCard(
            "Regelkaart: vergelijking oplossen",
            "Maak links en rechts gelijk; het antwoord ligt waar de twee kanten elkaar raken.",
            $$"""<math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mtext>{{System.Net.WebUtility.HtmlEncode(left)}}</mtext><mo>=</mo><mtext>{{System.Net.WebUtility.HtmlEncode(right)}}</mtext><mo>&#x21D2;</mo><mtext>snijpunt</mtext></mrow></math>""",
            "Voorbeeld: bij y = 2x en y = 20 zoek je waar de lijnen kruisen.");
    }

    private static FormulaRuleCard BuildCalculusRuleCard(CalculusStep step)
    {
        return step.Operation switch
        {
            CalculusOperation.Differentiate => BuildDerivativeRuleCard(step.Expression),
            CalculusOperation.Integrate => new FormulaRuleCard(
                "Regelkaart: primitiveren",
                "Integreren rekent terug van afgeleide naar functie; bij onbepaald primitiveren komt er + C bij.",
                """<math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><msup><mi>x</mi><mi>n</mi></msup><mo>&#x2192;</mo><mfrac><msup><mi>x</mi><mrow><mi>n</mi><mo>+</mo><mn>1</mn></mrow></msup><mrow><mi>n</mi><mo>+</mo><mn>1</mn></mrow></mfrac><mo>+</mo><mi>C</mi></mrow></math>""",
                "Voorbeeld: 2x wordt x^2 + C, want de afgeleide van x^2 is 2x."),
            CalculusOperation.Limit => new FormulaRuleCard(
                "Regelkaart: limiet",
                "Kijk naar welke waarde de functie nadert als x dichter bij het gekozen getal komt.",
                """<math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><munder><mo>lim</mo><mrow><mi>x</mi><mo>&#x2192;</mo><mi>a</mi></mrow></munder><mi>f</mi><mo>(</mo><mi>x</mi><mo>)</mo></mrow></math>""",
                "Voorbeeld: vul steeds waarden vlak bij a in en kijk waar de uitkomst naartoe gaat."),
            _ => new FormulaRuleCard(
                "Regelkaart",
                "Deze solver laat de gekozen wiskundestap als losse stappen zien.",
                BuildTextMathMl(step.Expression),
                "Volg de stappen van boven naar beneden.")
        };
    }

    private static FormulaRuleCard BuildDerivativeRuleCard(string expression)
    {
        if (TryReadPowerDerivative(expression, out var variable, out var exponent))
        {
            return new FormulaRuleCard(
                "Regelkaart: machtsregel",
                "De macht schuift naar voren; daarna wordt de macht 1 kleiner.",
                """<math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mfrac><mi>d</mi><mrow><mi>d</mi><mi>x</mi></mrow></mfrac><msup><mi>x</mi><mi>n</mi></msup><mo>=</mo><mi>n</mi><msup><mi>x</mi><mrow><mi>n</mi><mo>-</mo><mn>1</mn></mrow></msup></mrow></math>""",
                $"Voorbeeld: {variable}^{exponent} wordt {exponent}{variable}^({exponent}-1).");
        }

        return new FormulaRuleCard(
            "Regelkaart: differentieren",
            "Differentieren geeft de helling of veranderingssnelheid van een functie.",
            """<math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mfrac><mi>d</mi><mrow><mi>d</mi><mi>x</mi></mrow></mfrac><mi>f</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>=</mo><mi>f</mi><mo>'</mo><mo>(</mo><mi>x</mi><mo>)</mo></mrow></math>""",
            "Voorbeeld: de afgeleide van x^2 is 2x.");
    }

    private static string Format(decimal value)
    {
        const decimal snapTolerance = 0.000000001m;
        var whole = Math.Round(value, 0);
        if (Math.Abs(value - whole) < snapTolerance)
            value = whole;
        else
            value = Math.Round(value, 8);

        return value.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static IEnumerable<string> BuildCalculusVisualSteps(string expression, IReadOnlyList<string> explanationSteps)
    {
        if (TryBuildPowerDerivativeMath(expression, out var powerSteps))
            return powerSteps;

        return explanationSteps.Select(BuildTextMathMl);
    }

    private static bool TryBuildPowerDerivativeMath(string expression, out IReadOnlyList<string> steps)
    {
        steps = Array.Empty<string>();
        var trimmed = expression.Trim();
        const string prefix = "diff ";
        if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!TryReadPowerDerivative(trimmed, out var variable, out var exponent))
            return false;

        var newExponent = exponent - 1;
        var simplified = SimplifyPowerDerivative(variable, exponent);

        steps =
        [
            $$"""<math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mfrac><mi>d</mi><mrow><mi>d</mi><mi>x</mi></mrow></mfrac><msup><mi>{{variable}}</mi><mn class="hot">{{exponent}}</mn></msup></mrow></math>""",
            BuildPowerRuleAnimationSvg(variable, exponent),
            $$"""<math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mn>{{exponent}}</mn><mo>&#x22C5;</mo><msup><mi>{{variable}}</mi><mn class="hot">{{newExponent}}</mn></msup><mo>=</mo><mtext>{{System.Net.WebUtility.HtmlEncode(simplified)}}</mtext></mrow></math>"""
        ];
        return true;
    }

    private static string SimplifyPowerDerivative(string variable, int exponent)
    {
        if (exponent == 0)
            return "0";

        var newExponent = exponent - 1;
        return newExponent switch
        {
            0 => exponent.ToString(System.Globalization.CultureInfo.InvariantCulture),
            1 => $"{exponent}{variable}",
            _ => $"{exponent}{variable}^{newExponent}"
        };
    }

    private static string BuildPowerDerivativeSimplifyAnimationSvg(string variable, int exponent, int newExponent, string simplified)
    {
        var safeVariable = System.Net.WebUtility.HtmlEncode(variable);
        var safeExponent = System.Net.WebUtility.HtmlEncode(exponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var safeNewExponent = System.Net.WebUtility.HtmlEncode(newExponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var safeSimplified = System.Net.WebUtility.HtmlEncode(simplified);
        return $$"""
        <div class="power-simplify-animation" aria-label="Machtsregel vereenvoudigen">
          <svg viewBox="0 0 520 70" role="img">
            <text class="label" x="18" y="44">f</text>
            <text class="settled-prime" x="30" y="36">'</text>
            <text class="label" x="44" y="44">(</text>
            <text class="label function-arg-x" x="56" y="44">x</text>
            <text class="label" x="68" y="44">)</text>
            <text class="label" x="82" y="44">=</text>
            <text class="formula-base expanded-part" x="112" y="44">{{safeExponent}}</text>
            <text class="dot expanded-part" x="136" y="44">&#x22C5;</text>
            <text class="formula-base expanded-part" x="156" y="44">{{safeVariable}}</text>
            <text class="formula-exp expanded-part" x="182" y="23">{{safeNewExponent}}</text>
            <text class="formula-base final-result compact-result" x="112" y="44">{{safeSimplified}}</text>
            <text class="caption simplify-caption" x="224" y="42">puntje weg, macht blijft meedoen</text>
          </svg>
        </div>
        """;
    }

    private static string BuildFunctionPowerMathMl(string variable, int exponent)
    {
        var safeVariable = System.Net.WebUtility.HtmlEncode(variable);
        var safeExponent = System.Net.WebUtility.HtmlEncode(exponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return $$"""
        <div class="function-line-animation" aria-label="Functie start">
          <svg viewBox="0 0 620 70" role="img">
            <text class="label" x="18" y="44">f</text>
            <text class="label" x="38" y="44">(</text>
            <text class="label function-arg-x" x="50" y="44">x</text>
            <text class="label" x="62" y="44">)</text>
            <text class="label" x="82" y="44">=</text>
            <text class="formula-base" x="112" y="44">{{safeVariable}}</text>
            <text class="formula-exp" x="138" y="23">{{safeExponent}}</text>
            <text class="caption" x="184" y="42">dit is de functie</text>
          </svg>
        </div>
        """;
    }

    private static string BuildPowerRuleFormulaMathMl()
    {
        return """<math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mfrac><mi>d</mi><mrow><mi>d</mi><mi>x</mi></mrow></mfrac><msup><mi>x</mi><mi>n</mi></msup><mo>=</mo><mi>n</mi><msup><mi>x</mi><mrow><mi>n</mi><mo>-</mo><mn>1</mn></mrow></msup></mrow></math>""";
    }

    private static string BuildDerivativeNotationAnimationSvg(string variable, int exponent)
    {
        var safeVariable = System.Net.WebUtility.HtmlEncode(variable);
        var safeExponent = System.Net.WebUtility.HtmlEncode(exponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return $$"""
        <div class="derivative-notation-animation" aria-label="Afgeleide notatie animatie">
          <svg viewBox="0 0 620 70" role="img">
            <text class="label" x="18" y="44">f</text>
            <text class="prime-mark" x="30" y="36">'</text>
            <text class="label x-arg-left" x="38" y="44">(
              <animate attributeName="x" from="38" to="44" dur=".65s" begin=".65s" fill="freeze"></animate>
            </text>
            <text class="label function-arg-x x-arg-letter" x="50" y="44">x
              <animate attributeName="x" from="50" to="56" dur=".65s" begin=".65s" fill="freeze"></animate>
            </text>
            <text class="label x-arg-right" x="62" y="44">)
              <animate attributeName="x" from="62" to="68" dur=".65s" begin=".65s" fill="freeze"></animate>
            </text>
            <text class="label" x="82" y="44">=</text>
            <text class="formula-base" x="112" y="44">{{safeVariable}}</text>
            <text class="formula-exp" x="138" y="23">{{safeExponent}}</text>
            <text class="caption derivative-caption-from" x="184" y="42">dit is de functie</text>
            <text class="caption derivative-caption-to" x="184" y="42">we zoeken de afgeleide</text>
          </svg>
        </div>
        """;
    }

    private static string BuildExponentReductionMathMl(string variable, int exponent, int newExponent)
    {
        return $$"""<math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><msup><mi>{{System.Net.WebUtility.HtmlEncode(variable)}}</mi><mrow><mn>{{exponent}}</mn><mo>-</mo><mn>1</mn></mrow></msup><mo>=</mo><msup><mi>{{System.Net.WebUtility.HtmlEncode(variable)}}</mi><mn class="hot">{{newExponent}}</mn></msup></mrow></math>""";
    }

    private static string BuildExponentReductionAnimationSvg(string variable, int exponent, int newExponent)
    {
        var safeVariable = System.Net.WebUtility.HtmlEncode(variable);
        var safeExponent = System.Net.WebUtility.HtmlEncode(exponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var safeNewExponent = System.Net.WebUtility.HtmlEncode(newExponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return $$"""
        <div class="exponent-reduction-animation" aria-label="Exponent verminderen animatie">
          <svg viewBox="0 0 420 70" role="img">
            <text class="label" x="18" y="44">f</text>
            <text class="settled-prime" x="30" y="36">'</text>
            <text class="label" x="44" y="44">(</text>
            <text class="label function-arg-x" x="56" y="44">x</text>
            <text class="label" x="68" y="44">)</text>
            <text class="label" x="82" y="44">=</text>
            <text class="formula-base coefficient-settle" x="112" y="44">{{safeExponent}}</text>
            <text class="dot" x="136" y="44">&#x22C5;</text>
            <text class="formula-base" x="156" y="44">{{safeVariable}}</text>
            <text class="formula-exp exponent-calc" x="182" y="23">{{safeExponent}} - 1</text>
            <text class="formula-exp exponent-result" x="182" y="23">{{safeNewExponent}}</text>
            <text class="caption" x="224" y="42">{{safeExponent}} - 1 wordt {{safeNewExponent}}</text>
          </svg>
        </div>
        """;
    }

    private static string BuildDerivativeResultMathMl(string simplified)
    {
        return $$"""<math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mi>f</mi><mo>'</mo><mo>(</mo><mi>x</mi><mo>)</mo><mo>=</mo><mtext>{{System.Net.WebUtility.HtmlEncode(simplified)}}</mtext></mrow></math>""";
    }

    private static string BuildDerivativeResultAnimationSvg(string simplified)
    {
        var safeSimplified = System.Net.WebUtility.HtmlEncode(simplified);
        return $$"""
        <div class="derivative-result-animation" aria-label="Afgeleide antwoord">
          <svg viewBox="0 0 620 70" role="img">
            <text class="label" x="18" y="44">f</text>
            <text class="settled-prime" x="30" y="36">'</text>
            <text class="label" x="44" y="44">(</text>
            <text class="label function-arg-x" x="56" y="44">x</text>
            <text class="label" x="68" y="44">)</text>
            <text class="label" x="82" y="44">=</text>
            <text class="formula-base answer-result" x="112" y="44">{{safeSimplified}}</text>
            <text class="caption" x="184" y="42">antwoord van de afgeleide</text>
          </svg>
        </div>
        """;
    }

    private static string BuildSetDerivativeZeroAnimationSvg(string simplified)
    {
        var safeSimplified = System.Net.WebUtility.HtmlEncode(simplified);
        return $$"""
        <div class="zero-slope-animation" aria-label="Helling nul animatie">
          <svg viewBox="0 0 520 84" role="img">
            <text class="label zero-left-label" x="18" y="44">f</text>
            <text class="settled-prime zero-left-label" x="30" y="36">'</text>
            <text class="label zero-left-label" x="44" y="44">(</text>
            <text class="label function-arg-x zero-left-label" x="56" y="44">x</text>
            <text class="label zero-left-label" x="68" y="44">)</text>
            <text class="label zero-left-label" x="82" y="44">=</text>
            <text class="formula-base moving-slope" x="112" y="44">{{safeSimplified}}
              <animate attributeName="x" from="112" to="224" dur="1.4s" begin=".35s" fill="freeze"></animate>
            </text>
            <text class="zero-number derivative-zero" x="112" y="44">0</text>
            <text class="zero-arrow transition-arrow" x="180" y="44">&#x21D2;</text>
            <text class="label zero-equation" x="278" y="44">=</text>
            <text class="zero-number" x="306" y="44">0</text>
            <text class="caption zero-rule-hint" x="336" y="42">f'(x)=0</text>
          </svg>
        </div>
        """;
    }

    private static string BuildSolveZeroDerivativeAnimationSvg(string variable, int exponent, string stationaryX)
    {
        var safeVariable = System.Net.WebUtility.HtmlEncode(variable);
        var safeExponent = System.Net.WebUtility.HtmlEncode(exponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var safeStationaryX = System.Net.WebUtility.HtmlEncode(stationaryX);
        return $$"""
        <div class="solve-zero-animation" aria-label="Nulpunt afgeleide oplossen">
          <svg viewBox="0 0 560 84" role="img">
            <text class="moving-divisor" x="224" y="44">{{safeExponent}}
              <animate attributeName="x" from="224" to="238" dur="1.65s" begin=".35s" fill="freeze"></animate>
              <animate attributeName="y" from="44" to="60" dur="1.65s" begin=".35s" fill="freeze"></animate>
              <animate attributeName="font-size" from="28" to="18" dur="1.65s" begin=".35s" fill="freeze"></animate>
            </text>
            <text class="formula-base solve-left-part" x="252" y="44">{{safeVariable}}
              <animate attributeName="x" from="252" to="178" dur="1.65s" begin=".35s" fill="freeze"></animate>
            </text>
            <text class="label solve-left-part" x="278" y="44">=
              <animate attributeName="x" from="278" to="208" dur="1.65s" begin=".35s" fill="freeze"></animate>
            </text>
            <text class="formula-base moving-zero numerator-part" x="306" y="44">0
              <animate attributeName="x" from="306" to="238" dur="1.65s" begin=".35s" fill="freeze"></animate>
              <animate attributeName="y" from="44" to="35" dur="1.65s" begin=".35s" fill="freeze"></animate>
              <animate attributeName="font-size" from="28" to="18" dur="1.65s" begin=".35s" fill="freeze"></animate>
            </text>
            <line class="fraction-line fraction-part" x1="234" y1="41" x2="270" y2="41"></line>
            <text class="fraction ghost-divisor fraction-part" x="238" y="60">{{safeExponent}}</text>
            <text class="zero-arrow" x="292" y="44">&#x21D2;</text>
            <text class="formula-base final-equation" x="336" y="44">{{safeVariable}}</text>
            <text class="label final-equation" x="366" y="44">=</text>
            <text class="zero-number final-equation" x="394" y="44">{{safeStationaryX}}</text>
            <text class="caption solve-caption" x="430" y="44">0 / {{safeExponent}} blijft 0</text>
          </svg>
        </div>
        """;
    }

    private static string BuildGraphReadyMathMl(string variable, string stationaryX)
    {
        return $$"""<math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mtext>grafiek:</mtext><mspace width="0.5em"/><mi>{{System.Net.WebUtility.HtmlEncode(variable)}}</mi><mo>=</mo><mn>{{System.Net.WebUtility.HtmlEncode(stationaryX)}}</mn><mtext> is het helling-nul punt</mtext></mrow></math>""";
    }

    private static string BuildDivideCoefficientPowerAnimationSvg(string variable, int exponent, int newExponent)
    {
        var safeVariable = System.Net.WebUtility.HtmlEncode(variable);
        var safeExponent = System.Net.WebUtility.HtmlEncode(exponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var safeNewExponent = System.Net.WebUtility.HtmlEncode(newExponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return $$"""
        <div class="solve-zero-animation" aria-label="Coefficient wegdelen animatie">
          <svg viewBox="0 0 620 84" role="img">
            <text class="moving-divisor" x="112" y="44">{{safeExponent}}
              <animate attributeName="x" from="112" to="238" dur="1.7s" begin=".35s" fill="freeze"></animate>
              <animate attributeName="y" from="44" to="60" dur="1.7s" begin=".35s" fill="freeze"></animate>
              <animate attributeName="font-size" from="28" to="18" dur="1.7s" begin=".35s" fill="freeze"></animate>
            </text>
            <text class="dot solve-left-part" x="136" y="44">&#x22C5;</text>
            <text class="formula-base solve-left-part" x="156" y="44">{{safeVariable}}
              <animate attributeName="x" from="156" to="178" dur="1.7s" begin=".35s" fill="freeze"></animate>
            </text>
            <text class="formula-exp solve-left-part" x="182" y="23">{{safeNewExponent}}
              <animate attributeName="x" from="182" to="204" dur="1.7s" begin=".35s" fill="freeze"></animate>
            </text>
            <text class="label solve-left-part" x="220" y="44">=
              <animate attributeName="x" from="220" to="238" dur="1.7s" begin=".35s" fill="freeze"></animate>
            </text>
            <text class="formula-base moving-zero numerator-part" x="248" y="44">0
              <animate attributeName="x" from="248" to="238" dur="1.7s" begin=".35s" fill="freeze"></animate>
              <animate attributeName="y" from="44" to="35" dur="1.7s" begin=".35s" fill="freeze"></animate>
              <animate attributeName="font-size" from="28" to="18" dur="1.7s" begin=".35s" fill="freeze"></animate>
            </text>
            <line class="fraction-line fraction-part" x1="234" y1="41" x2="270" y2="41"></line>
            <text class="fraction ghost-divisor fraction-part" x="238" y="60">{{safeExponent}}</text>
            <text class="zero-arrow" x="292" y="44">&#x21D2;</text>
            <text class="formula-base final-equation" x="336" y="44">{{safeVariable}}</text>
            <text class="formula-exp final-equation" x="362" y="23">{{safeNewExponent}}</text>
            <text class="label final-equation" x="390" y="44">=</text>
            <text class="zero-number final-equation" x="418" y="44">0</text>
            <text class="caption solve-caption" x="452" y="44">0 / {{safeExponent}} blijft 0</text>
          </svg>
        </div>
        """;
    }

    private static string BuildRootZeroPowerAnimationSvg(string variable, int newExponent, string stationaryX)
    {
        var safeVariable = System.Net.WebUtility.HtmlEncode(variable);
        var safeNewExponent = System.Net.WebUtility.HtmlEncode(newExponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var safeStationaryX = System.Net.WebUtility.HtmlEncode(stationaryX);
        return $$"""
        <div class="root-zero-animation" aria-label="Macht nul oplossen">
          <svg viewBox="0 0 620 84" role="img">
            <text class="formula-base root-variable" x="112" y="44">{{safeVariable}}</text>
            <text class="formula-exp root-exp" x="138" y="23">{{safeNewExponent}}</text>
            <text class="label root-equals" x="176" y="44">=</text>
            <text class="zero-number root-zero" x="204" y="44">0</text>
            <text class="zero-arrow root-arrow" x="262" y="44">&#x21D2;</text>
            <text class="formula-base final-root" x="326" y="44">{{safeVariable}}</text>
            <text class="label final-root" x="356" y="44">=</text>
            <text class="zero-number final-root" x="384" y="44">{{safeStationaryX}}</text>
            <text class="caption root-caption" x="424" y="42">{{safeVariable}}^{{safeNewExponent}} is nul alleen bij {{safeVariable}} = 0</text>
          </svg>
        </div>
        """;
    }

    private static string BuildGraphReadyAnimationSvg(string variable, string stationaryX)
    {
        var safeVariable = System.Net.WebUtility.HtmlEncode(variable);
        var safeStationaryX = System.Net.WebUtility.HtmlEncode(stationaryX);
        return $$"""
        <div class="graph-ready-animation" aria-label="Grafiek klaarzetten">
          <svg viewBox="0 0 620 70" role="img">
            <text class="label graph-label" x="18" y="44">grafiek:</text>
            <text class="formula-base graph-equation-part" x="336" y="44">{{safeVariable}}
              <animate attributeName="x" from="336" to="112" dur="1.15s" begin=".25s" fill="freeze"></animate>
            </text>
            <text class="label graph-equation-part" x="366" y="44">=
              <animate attributeName="x" from="366" to="142" dur="1.15s" begin=".25s" fill="freeze"></animate>
            </text>
            <text class="zero-number graph-equation-part" x="394" y="44">{{safeStationaryX}}
              <animate attributeName="x" from="394" to="170" dur="1.15s" begin=".25s" fill="freeze"></animate>
            </text>
            <text class="caption graph-caption" x="214" y="42">eerst punt, daarna lijn rustig tekenen</text>
          </svg>
        </div>
        """;
    }

    private static string BuildRemoveOneExponentAnimationSvg(string variable, int exponent, string simplified)
    {
        var safeVariable = System.Net.WebUtility.HtmlEncode(variable);
        var safeExponent = System.Net.WebUtility.HtmlEncode(exponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var safeSimplified = System.Net.WebUtility.HtmlEncode(simplified);
        return $$"""
        <div class="remove-one-animation" aria-label="Macht een verdwijnt animatie">
          <svg viewBox="0 0 430 70" role="img">
            <text class="label" x="18" y="44">f</text>
            <text class="settled-prime" x="30" y="36">'</text>
            <text class="label" x="44" y="44">(</text>
            <text class="label function-arg-x" x="56" y="44">x</text>
            <text class="label" x="68" y="44">)</text>
            <text class="label" x="82" y="44">=</text>
            <text class="formula-base expanded-part" x="112" y="44">{{safeExponent}}</text>
            <text class="dot expanded-part" x="136" y="44">&#x22C5;</text>
            <text class="formula-base expanded-part" x="156" y="44">{{safeVariable}}</text>
            <text class="formula-exp one-exp" x="182" y="23">1</text>
            <text class="formula-base final-result compact-result" x="112" y="44">{{safeSimplified}}</text>
            <text class="formula-base simplify-hint-base" x="184" y="44">{{safeVariable}}</text>
            <text class="formula-exp simplify-hint-exp" x="208" y="23">1</text>
            <text class="dot simplify-hint-arrow" x="232" y="44">&#x2192;</text>
            <text class="formula-base simplify-hint-result" x="264" y="44">{{safeVariable}}</text>
          </svg>
        </div>
        """;
    }

    private static string BuildDerivativeValueMathMl(string x, string output)
    {
        return $$"""<math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mi>f</mi><mo>'</mo><mo>(</mo><mn>{{System.Net.WebUtility.HtmlEncode(x)}}</mn><mo>)</mo><mo>=</mo><mn>{{System.Net.WebUtility.HtmlEncode(output)}}</mn></mrow></math>""";
    }

    private static string BuildPowerRuleAnimationSvg(string variable, int exponent)
    {
        var safeVariable = System.Net.WebUtility.HtmlEncode(variable);
        var safeExponent = System.Net.WebUtility.HtmlEncode(exponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var safeNewExponent = System.Net.WebUtility.HtmlEncode((exponent - 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        return $$"""
        <div class="power-rule-animation" aria-label="Machtsregel animatie">
          <svg viewBox="0 0 620 70" role="img">
            <text class="label" x="18" y="44">f</text>
            <text class="settled-prime" x="30" y="36">'</text>
            <text class="label" x="44" y="44">(</text>
            <text class="label function-arg-x" x="56" y="44">x</text>
            <text class="label" x="68" y="44">)</text>
            <text class="label" x="82" y="44">=</text>
            <text class="formula-base" x="112" y="44">{{safeVariable}}
              <animate attributeName="x" from="112" to="156" dur="1.9s" begin="0.35s" fill="freeze"></animate>
            </text>
            <text class="formula-exp ghost-exp" x="138" y="23">{{safeExponent}}</text>
            <text class="moving-exp" x="138" y="23">{{safeExponent}}
              <animate attributeName="x" from="138" to="112" dur="1.9s" begin="0.35s" fill="freeze"></animate>
              <animate attributeName="y" from="23" to="44" dur="1.9s" begin="0.35s" fill="freeze"></animate>
              <animate attributeName="font-size" from="18" to="28" dur="1.9s" begin="0.35s" fill="freeze"></animate>
            </text>
            <g class="target-formula">
              <text class="dot" x="136" y="44">&#x22C5;</text>
              <text class="formula-exp exponent-calc" x="182" y="23">{{safeExponent}} - 1</text>
            </g>
            <text class="caption" x="224" y="42">macht wordt coefficient</text>
          </svg>
        </div>
        """;
    }

    private static bool TryReadPowerDerivative(string expression, out string variable, out int exponent)
    {
        variable = "";
        exponent = 0;
        var trimmed = expression.Trim();
        const string prefix = "diff ";
        var formula = trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? trimmed[prefix.Length..].Trim()
            : trimmed;
        var match = System.Text.RegularExpressions.Regex.Match(formula, @"^(ans|x)\s*\^\s*(-?\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success)
            return false;

        variable = match.Groups[1].Value.Equals("ans", StringComparison.OrdinalIgnoreCase) ? "x" : match.Groups[1].Value;
        exponent = int.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
        return true;
    }

    private static string BuildOperationMathMl(string expression)
    {
        return $$"""<math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mtext>{{System.Net.WebUtility.HtmlEncode(expression)}}</mtext></mrow></math>""";
    }

    private static string BuildAssignmentMathMl(string left, string right)
    {
        return $$"""<math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mtext>{{System.Net.WebUtility.HtmlEncode(left)}}</mtext><mo>=</mo><mtext>{{System.Net.WebUtility.HtmlEncode(right)}}</mtext></mrow></math>""";
    }

    private static string BuildTextMathMl(string text)
    {
        return $$"""<math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mtext>{{System.Net.WebUtility.HtmlEncode(text)}}</mtext></mrow></math>""";
    }
}

