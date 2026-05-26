namespace Tiedragon.NodSystem.Core;

/// <summary>
/// Numerieke calculus-engine.
/// 
/// diff     = centrale differentie
/// integral = Simpson-regel
/// limit    = links/rechts-benadering
/// </summary>
public static class CalculusEngine
{
    private const decimal DerivativeH = 0.00001m;
    private const int DefaultIntegralIntervals = 1000;

    // Zoek/commentaar: Methode Differentiate: centrale logica voor deze stap.
    public static decimal Differentiate(string expression, decimal x)
    {
        var left = NodExpressionEvaluator.Evaluate(expression, x - DerivativeH);
        var right = NodExpressionEvaluator.Evaluate(expression, x + DerivativeH);

        return (right - left) / (2 * DerivativeH);
    }

    // Zoek/commentaar: Methode Integrate: centrale logica voor deze stap.
    public static decimal Integrate(string expression, decimal a, decimal b, int intervals = 1000)
    {
        if (intervals < 2)
            intervals = 2;

        if (intervals % 2 == 1)
            intervals++;

        var h = (b - a) / intervals;
        var sum = NodExpressionEvaluator.Evaluate(expression, a) +
                  NodExpressionEvaluator.Evaluate(expression, b);

        for (var i = 1; i < intervals; i++)
        {
            var x = a + h * i;
            var fx = NodExpressionEvaluator.Evaluate(expression, x);
            sum += (i % 2 == 0 ? 2 : 4) * fx;
        }

        return sum * h / 3;
    }

    // Zoek/commentaar: Methode Limit: centrale logica voor deze stap.
    public static decimal Limit(string expression, decimal point)
    {
        decimal[] hs = [0.001m, 0.0001m, 0.00001m];

        decimal last = 0;
        var hasValue = false;

        foreach (var h in hs)
        {
            var left = NodExpressionEvaluator.Evaluate(expression, point - h);
            var right = NodExpressionEvaluator.Evaluate(expression, point + h);

            last = (left + right) / 2;
            hasValue = true;
        }

        if (!hasValue)
            throw new InvalidOperationException("Could not approximate limit.");

        return last;
    }

    // Zoek/commentaar: Past een regel, instelling of bewerking toe voor Apply.
    public static decimal Apply(CalculusStep step, decimal ans)
    {
        return step.Operation switch
        {
            CalculusOperation.Differentiate => Differentiate(step.Expression, ans),

            CalculusOperation.Integrate => Integrate(
                step.Expression,
                step.A ?? throw new InvalidOperationException("Integral start is missing."),
                step.B ?? throw new InvalidOperationException("Integral end is missing.")
            ),

            CalculusOperation.Limit => Limit(
                step.Expression,
                step.A ?? ans
            ),

            _ => throw new InvalidOperationException("Unknown calculus operation.")
        };
    }

    // Zoek/commentaar: Maakt educatieve tussenstappen voor animatie/uitleg in de UI.
    public static IReadOnlyList<string> DescribeSteps(CalculusStep step, decimal ans, decimal output)
    {
        return step.Operation switch
        {
            CalculusOperation.Differentiate => DescribeDifferentiate(step.Expression, ans, output),
            CalculusOperation.Integrate => DescribeIntegrate(
                step.Expression,
                step.A ?? throw new InvalidOperationException("Integral start is missing."),
                step.B ?? throw new InvalidOperationException("Integral end is missing."),
                output),
            CalculusOperation.Limit => DescribeLimit(step.Expression, step.A ?? ans, output),
            _ => Array.Empty<string>()
        };
    }

    private static IReadOnlyList<string> DescribeDifferentiate(string expression, decimal x, decimal output)
    {
        var leftX = x - DerivativeH;
        var rightX = x + DerivativeH;
        return
        [
            $"Neem x = {Format(x)}.",
            $"Gebruik centrale differentie met h = {Format(DerivativeH)}.",
            $"Bereken f(x - h) en f(x + h): {expression} bij {Format(leftX)} en {Format(rightX)}.",
            $"Helling: (f(x + h) - f(x - h)) / (2h) ≈ {Format(output)}."
        ];
    }

    private static IReadOnlyList<string> DescribeIntegrate(string expression, decimal a, decimal b, decimal output)
    {
        return
        [
            $"Neem het gebied van {Format(a)} tot {Format(b)}.",
            $"Verdeel het interval in {DefaultIntegralIntervals} stukjes.",
            $"Bereken {expression} op de meetpunten.",
            $"Gebruik de Simpson-regel met 1-4-2-4-...-1 gewichten.",
            $"Oppervlakte ≈ {Format(output)}."
        ];
    }

    private static IReadOnlyList<string> DescribeLimit(string expression, decimal point, decimal output)
    {
        return
        [
            $"Neem x dicht bij {Format(point)} van links en rechts.",
            $"Bereken {expression} met steeds kleinere afstand tot {Format(point)}.",
            $"Vergelijk links en rechts en neem het midden.",
            $"Limiet ≈ {Format(output)}."
        ];
    }

    private static string Format(decimal value)
        => value.ToString("0.############################", System.Globalization.CultureInfo.InvariantCulture);
}

