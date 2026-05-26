/*
NOD SYSTEM -- EQUATION ENGINE

Dit bestand is de eerste equation-laag voor NOD 2.0.

Ondersteund in deze prototypeversie:
- given m = 10
- given d = 2,5
- equation V = m / d
- solve V
- constraint V >= 0

Belangrijk:
Dit is nog geen volledige algebra-oplosser.
Deze versie lost alleen formules op waarbij de solve-variabele alleen links of rechts staat.
*/

namespace Tiedragon.NodSystem.Core;

// Zoek/commentaar: Type-overzicht: class EquationEngine bevat de hoofdlogica/data voor dit onderdeel.
public static class EquationEngine
{
    // Zoek/commentaar: Lost een vergelijking of berekening op voor Solve.
    public static EquationResult Solve(EquationDefinition equation)
    {
        if (string.IsNullOrWhiteSpace(equation.EquationText))
            throw new InvalidOperationException("Equation text is missing.");

        if (string.IsNullOrWhiteSpace(equation.SolveVariable))
            throw new InvalidOperationException("Solve variable is missing.");

        var parts = equation.EquationText.Split('=', 2);
        if (parts.Length != 2) throw new InvalidOperationException("Equation must contain '='.");

        var left = parts[0].Trim();
        var right = parts[1].Trim();
        var solve = equation.SolveVariable.Trim();

        // Prototype supports:
        // equation V = m / d
        // equation A = pi * r^2
        if (left.Equals(solve, StringComparison.OrdinalIgnoreCase))
        {
            var value = NodExpressionEvaluator.Evaluate(right, 0, equation.GivenValues);
            CheckConstraints(equation, solve, value);
            return new EquationResult(solve, value, $"{solve} = {right}");
        }

        if (right.Equals(solve, StringComparison.OrdinalIgnoreCase))
        {
            var value = NodExpressionEvaluator.Evaluate(left, 0, equation.GivenValues);
            CheckConstraints(equation, solve, value);
            return new EquationResult(solve, value, $"{solve} = {left}");
        }

        if (TrySolveNumerically(left, right, solve, equation.GivenValues, out var numericValue))
        {
            CheckConstraints(equation, solve, numericValue);
            return new EquationResult(solve, numericValue, $"{left} = {right}");
        }

        throw new NotSupportedException("This prototype solves equations when the solve variable is alone on one side, or when a numeric intersection can be found.");
    }

    private static bool TrySolveNumerically(
        string left,
        string right,
        string solve,
        IReadOnlyDictionary<string, decimal> givens,
        out decimal value)
    {
        value = 0;

        decimal F(decimal x)
        {
            var variables = new Dictionary<string, decimal>(givens, StringComparer.OrdinalIgnoreCase)
            {
                [solve] = x
            };

            return NodExpressionEvaluator.Evaluate(left, x, variables) -
                   NodExpressionEvaluator.Evaluate(right, x, variables);
        }

        if (!TryFindBracket(F, out var a, out var b))
            return false;

        var fa = F(a);
        if (Math.Abs(fa) < 0.0000001m)
        {
            value = a;
            return true;
        }

        for (var i = 0; i < 120; i++)
        {
            var mid = (a + b) / 2;
            var fm = F(mid);
            if (Math.Abs(fm) < 0.0000001m || Math.Abs(b - a) < 0.0000001m)
            {
                value = mid;
                return true;
            }

            if (Math.Sign(fa) == Math.Sign(fm))
            {
                a = mid;
                fa = fm;
            }
            else
            {
                b = mid;
            }
        }

        value = (a + b) / 2;
        return true;
    }

    private static bool TryFindBracket(Func<decimal, decimal> f, out decimal a, out decimal b)
    {
        a = 0;
        b = 0;

        decimal[] ranges = [10m, 100m, 1000m, 10000m];
        foreach (var range in ranges)
        {
            const int samples = 240;
            var step = (range * 2) / samples;
            decimal? previousX = null;
            decimal previousY = 0;

            for (var i = 0; i <= samples; i++)
            {
                var x = -range + step * i;
                decimal y;
                try
                {
                    y = f(x);
                }
                catch
                {
                    continue;
                }

                if (Math.Abs(y) < 0.0000001m)
                {
                    a = x;
                    b = x;
                    return true;
                }

                if (previousX is not null && Math.Sign(previousY) != Math.Sign(y))
                {
                    a = previousX.Value;
                    b = x;
                    return true;
                }

                previousX = x;
                previousY = y;
            }
        }

        return false;
    }

    // Zoek/commentaar: Controleert voorwaarden of grenzen voor CheckConstraints.
    private static void CheckConstraints(EquationDefinition equation, string variable, decimal value)
    {
        foreach (var c in equation.Constraints)
        {
            if (!c.Variable.Equals(variable, StringComparison.OrdinalIgnoreCase))
                continue;

            var ok = c.Operator switch
            {
                ">=" => value >= c.Value,
                "<=" => value <= c.Value,
                ">" => value > c.Value,
                "<" => value < c.Value,
                "=" => value == c.Value,
                _ => true
            };

            if (!ok)
                throw new InvalidOperationException($"Constraint failed: {c.Variable} {c.Operator} {c.Value}");
        }
    }
}

