#nullable enable
using System.Text;

namespace Tiedragon.LanguagePackage;

public sealed class LanguagePackagePolicy
{
    private const string PolicyFileName = "language-package-policy.ini";
    private static Lazy<LanguagePackagePolicy> CurrentPolicy = new(Load);

    public static LanguagePackagePolicy Current => CurrentPolicy.Value;

    public static void ReloadForCurrentProcess()
    {
        CurrentPolicy = new Lazy<LanguagePackagePolicy>(Load);
    }

    public string SourcePath { get; private init; } = "";
    public string ManifestPath { get; private init; } = "manifest.json";
    public HashSet<string> AllowedExtensions { get; private init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp", ".css", ".gif", ".html", ".jpg", ".jpeg", ".js", ".json", ".lng", ".png", ".svg", ".webp",
    };

    public HashSet<string> BlockedExtensions { get; private init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bat", ".cmd", ".com", ".dll", ".exe", ".msi", ".ps1", ".scr", ".vbs",
    };

    public HashSet<string> ImageExtensions { get; private init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp", ".gif", ".jpg", ".jpeg", ".png", ".svg", ".webp",
    };

    public HashSet<string> TextExtensions { get; private init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ".css", ".html", ".js",
    };

    public HashSet<string> AllowedScripts { get; private init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "basis.js", "formula.js", "main-help.js", "nod.js", "nod-popup.js",
    };

    public HashSet<string> AllowedRootHelpFiles { get; private init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "basis.js",
        "document-body.html",
        "document-topic.html",
        "formula-card.html",
        "formula-film.css",
        "formula-film.html",
        "formula-film.js",
        "formula.js",
        "main-help.css",
        "main-help.js",
        "nod-popup.css",
        "nod-popup.js",
        "nod.js",
    };

    private List<PathRule> AllowedPrefixRules { get; init; } =
    [
        new("language/", PackagePathKind.Language),
        new("assets/", PackagePathKind.Image),
        new("manual/", PackagePathKind.Text),
        new("help/content/main/", PackagePathKind.Text),
        new("help/main/", PackagePathKind.Text),
        new("help/content/nod/", PackagePathKind.Text),
        new("help/nod/", PackagePathKind.Text),
        new("nod/", PackagePathKind.Text),
        new("help/content/legal/", PackagePathKind.Text),
        new("help/legal/", PackagePathKind.Text),
        new("help/content/tool-editor/", PackagePathKind.Text),
        new("help/tool-editor/", PackagePathKind.Text),
        new("tool-editor/", PackagePathKind.Text),
        new("help/content/HelpApi/", PackagePathKind.Text),
        new("source/templates/", PackagePathKind.Text),
        new("templates/", PackagePathKind.Text),
        new("formula/", PackagePathKind.Text),
    ];

    public bool IsManifestPath(string packagePath) =>
        Normalize(packagePath).Equals(ManifestPath, StringComparison.OrdinalIgnoreCase);

    public bool IsImagePath(string packagePath) =>
        ImageExtensions.Contains(Path.GetExtension(packagePath));

    public bool IsAllowedScriptFile(string fileName) =>
        AllowedScripts.Contains(fileName);

    public bool IsAllowedPackagePath(string packagePath)
    {
        var path = Normalize(packagePath);
        var extension = Path.GetExtension(path);
        if (path.Length == 0 || Path.IsPathRooted(path) || path.Split('/').Any(part => part == ".."))
            return false;
        if (BlockedExtensions.Contains(extension) || !AllowedExtensions.Contains(extension))
            return false;
        if (extension.Equals(".js", StringComparison.OrdinalIgnoreCase) &&
            !AllowedScripts.Contains(Path.GetFileName(path)))
        {
            return false;
        }

        if (IsManifestPath(path))
            return true;
        if (IsAllowedRootHelpPath(path))
            return true;

        foreach (var rule in AllowedPrefixRules)
        {
            if (!path.StartsWith(rule.Prefix, StringComparison.OrdinalIgnoreCase))
                continue;
            return rule.Kind switch
            {
                PackagePathKind.Image => ImageExtensions.Contains(extension),
                PackagePathKind.Language => extension.Equals(".lng", StringComparison.OrdinalIgnoreCase),
                PackagePathKind.Text => TextExtensions.Contains(extension),
                _ => false,
            };
        }

        return false;
    }

    public IReadOnlyList<string> DescribeAllowedPrefixRules()
    {
        return AllowedPrefixRules
            .Select(rule => rule.Prefix + ":" + rule.Kind.ToString().ToLowerInvariant())
            .ToArray();
    }

    private bool IsAllowedRootHelpPath(string packagePath)
    {
        if (!packagePath.StartsWith("help/", StringComparison.OrdinalIgnoreCase))
            return false;
        if (packagePath["help/".Length..].Contains('/'))
            return false;

        return AllowedRootHelpFiles.Contains(Path.GetFileName(packagePath));
    }

    private static LanguagePackagePolicy Load()
    {
        var defaults = new LanguagePackagePolicy();
        var path = FindPolicyFile();
        if (path is null)
            return defaults;

        var values = ReadIni(path);
        var package = values.TryGetValue("package", out var section)
            ? section
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var rules = ParseRules(Get(package, "allowedPrefixes"), defaults.AllowedPrefixRules);
        return new LanguagePackagePolicy
        {
            SourcePath = path,
            ManifestPath = Normalize(Get(package, "manifest", defaults.ManifestPath)),
            AllowedExtensions = ParseSet(Get(package, "allowedExtensions"), defaults.AllowedExtensions),
            BlockedExtensions = ParseSet(Get(package, "blockedExtensions"), defaults.BlockedExtensions),
            ImageExtensions = ParseSet(Get(package, "imageExtensions"), defaults.ImageExtensions),
            TextExtensions = ParseSet(Get(package, "textExtensions"), defaults.TextExtensions),
            AllowedScripts = ParseSet(Get(package, "allowedScripts"), defaults.AllowedScripts),
            AllowedRootHelpFiles = ParseSet(Get(package, "allowedRootHelpFiles"), defaults.AllowedRootHelpFiles),
            AllowedPrefixRules = rules,
        };
    }

    private static string? FindPolicyFile()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, PolicyFileName),
            Path.Combine(Environment.CurrentDirectory, PolicyFileName),
            Path.Combine(Environment.CurrentDirectory, "src", "Tiedragon.LanguagePackage", PolicyFileName),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static Dictionary<string, Dictionary<string, string>> ReadIni(string path)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var section = "";
        foreach (var rawLine in File.ReadAllLines(path, Encoding.UTF8))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';'))
                continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line[1..^1].Trim();
                if (!result.ContainsKey(section))
                    result[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            if (!result.ContainsKey(section))
                result[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            result[section][line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }

        return result;
    }

    private static string Get(Dictionary<string, string> values, string key, string fallback = "") =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;

    private static HashSet<string> ParseSet(string value, HashSet<string> fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new HashSet<string>(fallback, StringComparer.OrdinalIgnoreCase);

        return new HashSet<string>(
            value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            StringComparer.OrdinalIgnoreCase);
    }

    private static List<PathRule> ParseRules(string value, List<PathRule> fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [.. fallback];

        var rules = new List<PathRule>();
        foreach (var item in value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = item.LastIndexOf(':');
            if (separator <= 0)
                continue;
            var prefix = Normalize(item[..separator]);
            if (!prefix.EndsWith('/'))
                prefix += "/";
            if (!TryParseKind(item[(separator + 1)..], out var kind))
                continue;
            rules.Add(new PathRule(prefix, kind));
        }

        return rules.Count > 0 ? rules : [.. fallback];
    }

    private static bool TryParseKind(string value, out PackagePathKind kind)
    {
        var normalized = value.Trim();
        if (normalized.Equals("lng", StringComparison.OrdinalIgnoreCase))
        {
            kind = PackagePathKind.Language;
            return true;
        }

        return Enum.TryParse(normalized, ignoreCase: true, out kind);
    }

    private static string Normalize(string path) =>
        (path ?? "").Replace('\\', '/').TrimStart('/');

    private sealed record PathRule(string Prefix, PackagePathKind Kind);

    private enum PackagePathKind
    {
        Image,
        Language,
        Text,
    }
}
