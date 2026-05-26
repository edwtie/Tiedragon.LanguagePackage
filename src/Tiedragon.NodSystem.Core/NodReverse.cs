using System.Text.RegularExpressions;

namespace Tiedragon.NodSystem.Core;

/// <summary>
/// Maakt automatische reverse-expressies voor eenvoudige omkeerbare NOD 2.0-stappen.
/// 
/// Deze class is belangrijk voor Calculation Trace:
/// vooruit:  math-stap uitvoeren
/// terug:    bijbehorende inverse stap uitvoeren
///
/// Voorbeelden:
/// sin(ans)       -> asin(ans)
/// sind(ans)      -> asind(ans)
/// tan(ans)       -> atan(ans)
/// tand(ans)      -> atand(ans)
/// ln(ans)        -> exp(ans)
/// exp(ans)       -> ln(ans)
/// log(ans,2)     -> pow(2,ans)
/// log(ans,10)    -> pow(10,ans)
/// sqrt(ans)      -> ans^2
/// rad(ans)       -> deg(ans)
/// deg(ans)       -> rad(ans)
/// ans * e^2      -> ans / e^2
/// ans + 10       -> ans - 10
///
/// Niet alles is veilig:
/// - cos heeft meerdere mogelijke hoeken.
/// - sin heeft ook meerdere oplossingen, maar asin is bruikbaar als hoofdwaarde.
/// - ans^2 blijft dubbelzinnig zonder trace/context.
/// </summary>
public static class NodReverse
{
    private static readonly Dictionary<string, string> InverseFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sin"] = "asin",
        ["asin"] = "sin",

        ["cos"] = "acos",
        ["acos"] = "cos",

        ["tan"] = "atan",
        ["atan"] = "tan",

        ["sind"] = "asind",
        ["asind"] = "sind",

        ["cosd"] = "acosd",
        ["acosd"] = "cosd",

        ["tand"] = "atand",
        ["atand"] = "tand",

        ["rad"] = "deg",
        ["deg"] = "rad",

        ["ln"] = "exp",
        ["exp"] = "ln",

        ["sqrt"] = "pow2"
    };

    // Zoek/commentaar: Probeert deze actie uit te voeren en geeft succes/mislukking terug voor TryCreateReverseExpression.
    public static bool TryCreateReverseExpression(string expression, out string reverseExpression)
    {
        expression = expression.Trim();

        if (TryReverseLogWithBase(expression, out reverseExpression))
            return true;

        if (TryReverseSimpleFunction(expression, out reverseExpression))
            return true;

        if (TryReverseAnsOperatorValue(expression, out reverseExpression))
            return true;

        reverseExpression = "";
        return false;
    }

    // Zoek/commentaar: Probeert deze actie uit te voeren en geeft succes/mislukking terug voor TryReverseLogWithBase.
    private static bool TryReverseLogWithBase(string expression, out string reverseExpression)
    {
        // log(ans,2) -> pow(2,ans)
        // log(ans,e) -> pow(e,ans)
        // log(ans,10) -> pow(10,ans)
        var match = Regex.Match(
            expression,
            @"^\s*log\s*\(\s*ans\s*,\s*(.+?)\s*\)\s*$",
            RegexOptions.IgnoreCase
        );

        if (!match.Success)
        {
            reverseExpression = "";
            return false;
        }

        var baseExpression = match.Groups[1].Value.Trim();
        reverseExpression = $"pow({baseExpression},ans)";
        return true;
    }

    // Zoek/commentaar: Probeert deze actie uit te voeren en geeft succes/mislukking terug voor TryReverseSimpleFunction.
    private static bool TryReverseSimpleFunction(string expression, out string reverseExpression)
    {
        // func(ans)
        var match = Regex.Match(
            expression,
            @"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*\(\s*ans\s*\)\s*$",
            RegexOptions.IgnoreCase
        );

        if (!match.Success)
        {
            reverseExpression = "";
            return false;
        }

        var functionName = match.Groups[1].Value;

        if (!InverseFunctions.TryGetValue(functionName, out var inverse))
        {
            reverseExpression = "";
            return false;
        }

        reverseExpression = inverse.Equals("pow2", StringComparison.OrdinalIgnoreCase)
            ? "ans^2"
            : $"{inverse}(ans)";
        return true;
    }

    // Zoek/commentaar: Probeert deze actie uit te voeren en geeft succes/mislukking terug voor TryReverseAnsOperatorValue.
    private static bool TryReverseAnsOperatorValue(string expression, out string reverseExpression)
    {
        // ans * e^2
        // ans / e^2
        // ans + 32
        // ans - 32
        var match = Regex.Match(
            expression,
            @"^\s*ans\s*([+\-*/])\s*(.+?)\s*$",
            RegexOptions.IgnoreCase
        );

        if (!match.Success)
        {
            reverseExpression = "";
            return false;
        }

        var op = match.Groups[1].Value;
        var value = match.Groups[2].Value.Trim();

        var inverseOp = op switch
        {
            "+" => "-",
            "-" => "+",
            "*" => "/",
            "/" => "*",
            _ => ""
        };

        if (inverseOp.Length == 0)
        {
            reverseExpression = "";
            return false;
        }

        reverseExpression = $"ans {inverseOp} {value}";
        return true;
    }
}

