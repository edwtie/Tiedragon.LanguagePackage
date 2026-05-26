using System.Text.RegularExpressions;

namespace Tiedragon.NodSystem.Core;

/// <summary>
/// Normaliseert .nod-tekst voordat parser/editor/engine ermee werken.
/// 
/// Doel:
/// - oude line endings herstellen
/// - eenregelige geplakte NOD-tekst herstellen
/// - parser robuuster maken
/// 
/// Voorbeeld probleem:
/// Name Celsius naar Fahrenheitinput1 Celsiusinput2 Fahrenheitformat ##.00math ans * 1,8math ans + 32end
/// 
/// Wordt:
/// Name Celsius naar Fahrenheit
/// input1 Celsius
/// input2 Fahrenheit
/// format ##.00
/// math ans * 1,8
/// math ans + 32
/// end
/// </summary>
public static class NodTextNormalizer
{
    // Zoek/commentaar: Normaliseert invoertekst naar een vaste vorm voor Normalize.
    public static string Normalize(string text, bool repairConcatenated = true)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var normalized = text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        if (repairConcatenated && ShouldRepair(normalized))
            normalized = RepairConcatenatedNod(normalized);

        // Core gebruikt intern \n. UI mag daarna naar Environment.NewLine omzetten.
        normalized = normalized
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        return normalized.Trim('\uFEFF');
    }

    // Zoek/commentaar: Normaliseert invoertekst naar een vaste vorm voor NormalizeForEditor.
    public static string NormalizeForEditor(string text, bool repairConcatenated = true)
    {
        return Normalize(text, repairConcatenated)
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\n", Environment.NewLine);
    }

    // Zoek/commentaar: Methode ShouldRepair: centrale logica voor deze stap.
    private static bool ShouldRepair(string text)
    {
        var lines = text.Split('\n');

        // Als er al meerdere normale regels zijn, niet agressief repareren.
        if (lines.Length > 2)
            return false;

        var lower = text.ToLowerInvariant();

        var hits = 0;
        string[] keys =
        [
            "urln ",
            "input1 ", "input2 ", "inputr ",
            "result ", "resfou ",
            "symb1 ", "symb2 ", "symb3 ", "symb4 ",
            "format ", "tformat ",
            "math ", "chg ", "trans ",
            "reverse ", "mode ",
            "field ", "table ", "output ",
            "phoneformat ", "lookup ", "match ",
            "given ", "equation ", "solve ", "constraint ",
            "preview ", "backup ",
            "indoprint ", "indoend ",
            "end"
        ];

        foreach (var key in keys)
        {
            if (lower.Contains(key))
                hits++;
        }

        return hits >= 3;
    }

    // Zoek/commentaar: Herstelt of repareert invoertekst voor RepairConcatenatedNod.
    private static string RepairConcatenatedNod(string text)
    {
        var s = text.Trim();

        string[] commands =
        [
            "Name",
            "URLN",
            "input1", "input2", "inputr",
            "Result", "Resfou",
            "Symb1", "Symb2", "Symb3", "Symb4",
            "Symba1", "Symba2", "Symba3", "Symba4",
            "format", "tformat",
            "mode", "reverse",
            "math", "chg", "trans",
            "table", "field", "output", "phoneformat", "lookup", "match",
            "given", "equation", "solve", "constraint",
            "preview", "backup",
            "indoprint", "indoend",
            "end"
        ];

        foreach (var cmd in commands)
        {
            s = Regex.Replace(
                s,
                $@"(?<!^)(?<!\r)(?<!\n){Regex.Escape(cmd)}\b\s+",
                "\n" + cmd + " ",
                RegexOptions.IgnoreCase
            );
        }

        s = Regex.Replace(s, @"(?<!^)(?<!\r)(?<!\n)end\s*$", "\nend", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\bend\s*$", "end", RegexOptions.IgnoreCase);
        return s.TrimStart('\r', '\n');
    }
}

