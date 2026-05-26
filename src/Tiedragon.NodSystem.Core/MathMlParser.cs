using System.Xml;
using System.Xml.Linq;

namespace Tiedragon.NodSystem.Core;

// Copyright (c) Tiedragon. All rights reserved.
//
// Small MathML parser/validator for formula cards, solver reports and help
// documents. It intentionally validates MathML as strict XML and checks the
// expected <math> root, without trying to become a symbolic math engine.
public static class MathMlParser
{
    private static readonly XName MathRootName = XName.Get("math", "http://www.w3.org/1998/Math/MathML");
    private static readonly IReadOnlyDictionary<string, string> NamedMathEntities = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["&oint;"] = "&#x222E;",
        ["&micro;"] = "&#x00B5;",
        ["&sup2;"] = "&#x00B2;",
        ["&sup3;"] = "&#x00B3;",
    };

    public static MathMlDocument Parse(string mathMl)
    {
        if (!TryParse(mathMl, out var document, out var error))
            throw new FormatException(error);

        return document;
    }

    public static bool TryParse(string mathMl, out MathMlDocument document, out string error)
    {
        document = MathMlDocument.Empty;
        error = "";

        if (string.IsNullOrWhiteSpace(mathMl))
        {
            error = "MathML is empty.";
            return false;
        }

        try
        {
            var parsed = XDocument.Parse(
                NormalizeNamedEntities(mathMl),
                LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            if (parsed.Root is null)
            {
                error = "MathML has no root element.";
                return false;
            }

            if (!IsMathRoot(parsed.Root))
            {
                error = "MathML root must be <math>.";
                return false;
            }

            document = new MathMlDocument(parsed, parsed.Root.Name.LocalName, parsed.Root.Name.NamespaceName);
            return true;
        }
        catch (XmlException ex)
        {
            error = ex.LineNumber > 0
                ? $"MathML XML error at line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}"
                : "MathML XML error: " + ex.Message;
            return false;
        }
    }

    public static IReadOnlyList<MathMlFragment> FindFragments(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var fragments = new List<MathMlFragment>();
        var index = 0;
        while (index < html.Length)
        {
            var start = html.IndexOf("<math", index, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                break;

            var startTagEnd = html.IndexOf('>', start);
            if (startTagEnd < 0)
            {
                fragments.Add(new MathMlFragment(start, html[start..], IsComplete: false));
                break;
            }

            var end = html.IndexOf("</math>", startTagEnd + 1, StringComparison.OrdinalIgnoreCase);
            if (end < 0)
            {
                fragments.Add(new MathMlFragment(start, html[start..], IsComplete: false));
                break;
            }

            var endExclusive = end + "</math>".Length;
            fragments.Add(new MathMlFragment(start, html[start..endExclusive], IsComplete: true));
            index = endExclusive;
        }

        return fragments;
    }

    public static IReadOnlyList<string> ValidateFragments(string html)
    {
        var errors = new List<string>();
        foreach (var fragment in FindFragments(html))
        {
            if (!fragment.IsComplete)
            {
                errors.Add($"Incomplete MathML fragment at character {fragment.StartIndex}.");
                continue;
            }

            if (!TryParse(fragment.Text, out _, out var error))
                errors.Add($"MathML fragment at character {fragment.StartIndex}: {error}");
        }

        return errors;
    }

    private static bool IsMathRoot(XElement root)
    {
        return root.Name == MathRootName ||
            (root.Name.LocalName.Equals("math", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(root.Name.NamespaceName));
    }

    private static string NormalizeNamedEntities(string text)
    {
        foreach (var entity in NamedMathEntities)
            text = text.Replace(entity.Key, entity.Value, StringComparison.Ordinal);

        return text;
    }
}

public sealed record MathMlDocument(XDocument Xml, string RootName, string NamespaceName)
{
    public static MathMlDocument Empty { get; } = new(new XDocument(), "", "");
}

public sealed record MathMlFragment(int StartIndex, string Text, bool IsComplete);
