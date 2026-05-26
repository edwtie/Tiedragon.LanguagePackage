namespace Tiedragon.NodSystem.Core;

/// <summary>
/// Eenvoudige document/text batch engine.
/// 
/// Deze versie werkt op in-memory strings.
/// Later kan dit worden gekoppeld aan echte .txt/.docx/.odt-bestanden.
/// </summary>
public sealed record DocumentTextRule(string OldText, string NewText);

// Zoek/commentaar: Type-overzicht: record DocumentBatchResult bevat de hoofdlogica/data voor dit onderdeel.
public sealed record DocumentBatchResult(
    int DocumentsScanned,
    int DocumentsChanged,
    int Replacements,
    IReadOnlyList<string> Lines
);

// Zoek/commentaar: Type-overzicht: class DocumentBatchEngine bevat de hoofdlogica/data voor dit onderdeel.
public static class DocumentBatchEngine
{
    // Zoek/commentaar: Methode ReplaceAll: centrale logica voor deze stap.
    public static DocumentBatchResult ReplaceAll(
        IReadOnlyDictionary<string, string> documents,
        IReadOnlyList<DocumentTextRule> rules)
    {
        var lines = new List<string>();
        var changedDocs = 0;
        var replacements = 0;

        foreach (var doc in documents)
        {
            var text = doc.Value;
            var original = text;
            var docReplacements = 0;

            foreach (var rule in rules)
            {
                var count = CountOccurrences(text, rule.OldText);
                if (count == 0) continue;

                text = text.Replace(rule.OldText, rule.NewText, StringComparison.Ordinal);
                docReplacements += count;
                replacements += count;
            }

            if (!string.Equals(original, text, StringComparison.Ordinal))
            {
                changedDocs++;
                lines.Add($"{doc.Key}: {docReplacements} replacements");
            }
        }

        return new DocumentBatchResult(documents.Count, changedDocs, replacements, lines);
    }

    // Zoek/commentaar: Telt voorkomens of onderdelen voor CountOccurrences.
    private static int CountOccurrences(string text, string search)
    {
        if (string.IsNullOrEmpty(search)) return 0;

        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += search.Length;
        }

        return count;
    }
}

