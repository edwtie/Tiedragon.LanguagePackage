#nullable enable
using SharpCompress.Archives;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tiedragon.NodSystem.Core;

namespace Tiedragon.LanguagePackage;

internal static class Program
{
    private const string ToolName = "Language Package";
    private const string ToolVersion = "1.0";
    private const string MagicText = "SYSCALC-LNGPDK";
    private const string ObjectPackageExtension = ".objpdk";
    private const string CompiledPackageExtension = ".lngpdk";
    private const string CompilerRecipeFileName = "compiler-recipe.json";
    private const int ContainerFormat = 1;
    private const string ProducerName = "Tiedragon";
    private const string PackageType = "language";
    private const int MaxHeaderBytes = 64 * 1024;
    private const int MaxEntryCount = 2048;
    private const long MaxEntryBytes = 16L * 1024 * 1024;
    private const long MaxTotalEntryBytes = 128L * 1024 * 1024;
    private const long MaxPayloadBytes = 192L * 1024 * 1024;
    private const string FormulaIndexPackageTemplate = """
<h1>{{title}}</h1>
<p>{{intro}}</p>
<table>
  <thead><tr><th>{{column_formula}}</th><th>{{column_category}}</th><th>{{column_tags}}</th><th>{{column_description}}</th></tr></thead>
  <tbody>
{{rows}}  </tbody>
</table>
""";

    private const string FormulaCardPackageTemplate = """
<h1>{{title}}</h1>
<div class="notice">{{tags}}</div>
<p>{{description}}</p>

<h2>{{section_formula}}</h2>
<p><code>{{formula_text}}</code></p>
<div class="formula">{{mathml_card}}</div>

<h2>{{section_text}}</h2>
<pre>{{plain_text}}</pre>

<h2>{{section_latex}}</h2>
<pre>{{latex}}</pre>

<h2>{{section_mathml}}</h2>
<pre>{{mathml_pre}}</pre>

<h2>{{section_example_nod}}</h2>
<pre>{{example_nod}}</pre>
""";

    private const string NodFullPagePackageTemplate = """
<h1>{{title}}</h1>
<p>{{intro}}</p>

<h2>{{section_title}}</h2>
<p>{{section_body}}</p>
""";

    private const string NodCommandPagePackageTemplate = """
<h1>{{command}}</h1>
<p>{{summary}}</p>

<div class="notice"><b>{{syntax_label}}</b> <code>{{syntax}}</code></div>

<h2>{{example_title}}</h2>
<pre>{{example_nod}}</pre>
""";

    private const string NodPopupPackageTemplate = """
<div class="title">{{command}}</div>
<div class="syntax">{{syntax}}</div>
<div><b>{{meaning_label}}</b> {{meaning}}</div>
<div><b>{{example_label}}</b> <code>{{example}}</code></div>
""";

    private const string NodSnippetPackageTemplate = """
<div class="topic-grid">
  {{items}}
</div>
""";

    private static readonly byte[] Magic = Encoding.ASCII.GetBytes(MagicText);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private static readonly JsonSerializerOptions AgentJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private static readonly Regex HtmlLinkRegex = new(
        "(?<attr>href|src)\\s*=\\s*(?<quote>[\"'])(?<path>[^\"']+)\\k<quote>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ConceptHelpMarkupRegex = new(
        "<div\\s+class\\s*=\\s*[\"'][^\"']*(concept-banner|help-warning)[^\"']*[\"'][\\s\\S]*?\\bConcept\\b[\\s\\S]*?</div>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] RequiredSignedLanguageKeys =
    [
        "help.signed_package_verified",
        "help.signed_package_tip",
        "dialog.language.info.title",
        "dialog.language.info.name",
        "dialog.language.info.code",
        "dialog.language.info.author",
        "dialog.language.info.product",
        "dialog.language.info.package",
        "dialog.language.info.file",
        "dialog.language.info.version",
        "dialog.language.info.signed",
        "dialog.language.info.algorithm",
        "dialog.language.info.key",
        "dialog.language.info.local_author",
        "dialog.language.info.unsigned",
        "common.yes",
        "common.no",
    ];

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
                return Usage();

            return args[0].ToLowerInvariant() switch
            {
                "compile" => PackLanguage(args),
                "pack-language" => PackLanguage(args),
                "compile-signed" => PackSignedLanguage(args),
                "agent-compile" => AgentCompileLanguage(args),
                "agent-compile-signed" => AgentCompileSignedLanguage(args),
                "agent-compile-with-base" => AgentCompileLanguageWithBase(args),
                "agent-compile-with-base-signed" => AgentCompileSignedLanguageWithBase(args),
                "create-signing-key" => CreateSigningKey(args),
                "export-formula-cards" => ExportFormulaCards(args),
                "export-formula-template" => ExportFormulaTemplate(args),
                "export-html-templates" => ExportHtmlTemplates(args),
                "validate" => ValidatePackage(args),
                "inspect" => InspectPackage(args),
                _ => Usage(),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("error: " + ex.Message);
            return 1;
        }
    }

    private static int Usage()
    {
        Console.WriteLine(ToolName + " " + ToolVersion);
        Console.WriteLine("Tiedragon.LanguagePackage");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  compile <input-folder-or-objpdk> <output.lngpdk>");
        Console.WriteLine("  pack-language <input-folder-or-objpdk> <output.lngpdk>");
        Console.WriteLine("  compile-signed <input-folder-or-objpdk> <output.lngpdk> <private-key.pem> <key-id>");
        Console.WriteLine("  agent-compile <input-folder-or-objpdk> <output.lngpdk>");
        Console.WriteLine("  agent-compile-signed <input-folder-or-objpdk> <output.lngpdk> <private-key.pem> <key-id>");
        Console.WriteLine("  agent-compile-with-base <base-folder-or-package> <input-folder-or-objpdk> <output.lngpdk>");
        Console.WriteLine("  agent-compile-with-base-signed <base-folder-or-package> <input-folder-or-objpdk> <output.lngpdk> <private-key.pem> <key-id>");
        Console.WriteLine("  create-signing-key <private-key.pem> <public-key.lngpdk.pubkey.json> <key-id>");
        Console.WriteLine("  export-formula-cards <output-folder> [language-file.lng]");
        Console.WriteLine("  export-formula-template <output.lng> [language-file.lng]");
        Console.WriteLine("  export-html-templates <output-folder>");
        Console.WriteLine("  validate <package.lngpdk>");
        Console.WriteLine("  inspect <package.lngpdk>");
        return 2;
    }

    private static int PackLanguage(string[] args)
    {
        if (args.Length != 3)
            return Usage();

        var result = BuildLanguagePackage(args[1], args[2]);
        WriteHumanBuildResult(result);
        return 0;
    }

    private static int PackSignedLanguage(string[] args)
    {
        if (args.Length != 5)
            return Usage();

        var result = BuildLanguagePackage(args[1], args[2], ReadSigningOptions(args[3], args[4]));
        WriteHumanBuildResult(result);
        return 0;
    }

    private static void WriteHumanBuildResult(LanguagePackageBuildResult result)
    {
        Console.WriteLine("created: " + result.OutputPath);
        Console.WriteLine("key: " + result.PackageKey);
        Console.WriteLine("language: " + result.LanguageCode);
        Console.WriteLine("entries: " + result.EntryCount);
        Console.WriteLine("packageSha256: " + result.PackageSha256);
        Console.WriteLine("payloadSha256: " + result.PayloadSha256);
        Console.WriteLine("signed: " + result.Signed);
        if (!string.IsNullOrWhiteSpace(result.SignatureAlgorithm))
            Console.WriteLine("signatureAlgorithm: " + result.SignatureAlgorithm);
        if (!string.IsNullOrWhiteSpace(result.SignatureKeyId))
            Console.WriteLine("signatureKeyId: " + result.SignatureKeyId);
    }

    private static int AgentCompileLanguage(string[] args)
    {
        if (args.Length != 3)
        {
            WriteAgentError("E_ARGS", "agent-compile requires <input-folder-or-objpdk> <output.lngpdk>.");
            return 2;
        }

        try
        {
            var result = BuildLanguagePackage(args[1], args[2]);
            Console.WriteLine(JsonSerializer.Serialize(result, AgentJsonOptions));
            return 0;
        }
        catch (Exception ex)
        {
            WriteAgentError(GetErrorCode(ex), ex.Message);
            return 1;
        }
    }

    private static int AgentCompileSignedLanguage(string[] args)
    {
        if (args.Length != 5)
        {
            WriteAgentError("E_ARGS", "agent-compile-signed requires <input-folder-or-objpdk> <output.lngpdk> <private-key.pem> <key-id>.");
            return 2;
        }

        try
        {
            var result = BuildLanguagePackage(args[1], args[2], ReadSigningOptions(args[3], args[4]));
            Console.WriteLine(JsonSerializer.Serialize(result, AgentJsonOptions));
            return 0;
        }
        catch (Exception ex)
        {
            WriteAgentError(GetErrorCode(ex), ex.Message);
            return 1;
        }
    }

    private static int AgentCompileLanguageWithBase(string[] args)
    {
        if (args.Length != 4)
        {
            WriteAgentError("E_ARGS", "agent-compile-with-base requires <base-folder-or-package> <input-folder-or-objpdk> <output.lngpdk>.");
            return 2;
        }

        try
        {
            var result = BuildLanguagePackageWithBase(args[1], args[2], args[3]);
            Console.WriteLine(JsonSerializer.Serialize(result, AgentJsonOptions));
            return 0;
        }
        catch (Exception ex)
        {
            WriteAgentError(GetErrorCode(ex), ex.Message);
            return 1;
        }
    }

    private static int AgentCompileSignedLanguageWithBase(string[] args)
    {
        if (args.Length != 6)
        {
            WriteAgentError("E_ARGS", "agent-compile-with-base-signed requires <base-folder-or-package> <input-folder-or-objpdk> <output.lngpdk> <private-key.pem> <key-id>.");
            return 2;
        }

        try
        {
            var result = BuildLanguagePackageWithBase(args[1], args[2], args[3], ReadSigningOptions(args[4], args[5]));
            Console.WriteLine(JsonSerializer.Serialize(result, AgentJsonOptions));
            return 0;
        }
        catch (Exception ex)
        {
            WriteAgentError(GetErrorCode(ex), ex.Message);
            return 1;
        }
    }

    private static int CreateSigningKey(string[] args)
    {
        if (args.Length != 4)
            return Usage();

        var privateKeyPath = Path.GetFullPath(args[1]);
        var trustedKeyPath = Path.GetFullPath(args[2]);
        var keyId = args[3].Trim();
        if (!IsSafeKey(keyId))
            throw new InvalidOperationException("Signing key id is invalid.");

        Directory.CreateDirectory(Path.GetDirectoryName(privateKeyPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(trustedKeyPath)!);

        using var rsa = RSA.Create(3072);
        File.WriteAllText(privateKeyPath, rsa.ExportPkcs8PrivateKeyPem(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        var trusted = new TrustedLanguagePackageKey(
            keyId,
            "rsa-pss-sha256",
            "tiedragon.syscalculator",
            PackageType,
            "zip",
            rsa.ExportSubjectPublicKeyInfoPem());
        File.WriteAllText(trustedKeyPath, JsonSerializer.Serialize(trusted, JsonOptions), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        Console.WriteLine("privateKey: " + privateKeyPath);
        Console.WriteLine("publicKey: " + trustedKeyPath);
        Console.WriteLine("keyId: " + keyId);
        Console.WriteLine("algorithm: rsa-pss-sha256");
        return 0;
    }

    private static void WriteAgentError(string code, string message)
    {
        var error = new LanguagePackageAgentError(false, code, message);
        Console.WriteLine(JsonSerializer.Serialize(error, AgentJsonOptions));
    }

    private static int ExportFormulaCards(string[] args)
    {
        if (args.Length is not 2 and not 3)
            return Usage();

        var outputFolder = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(outputFolder);
        var languageMap = args.Length == 3
            ? ReadLanguageMap(Path.GetFullPath(args[2]))
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var cards = FormulaCardCatalog.GetDefaultCards();
        var localizedCards = cards
            .Select(card => LocalizeFormulaCard(card, languageMap))
            .ToArray();
        File.WriteAllText(
            Path.Combine(outputFolder, "index.html"),
            BuildFormulaIndexHtml(localizedCards, languageMap, FormulaIndexPackageTemplate),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        foreach (var card in localizedCards.OrderBy(card => card.Title, StringComparer.CurrentCultureIgnoreCase))
        {
            MathMlParser.Parse(card.Source.MathMl);
            var relativePath = BuildFormulaCardPackagePath(card.Source);
            var outputPath = Path.Combine(outputFolder, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, BuildFormulaCardHtml(card, languageMap, FormulaCardPackageTemplate), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        Console.WriteLine("exported formula cards: " + cards.Count);
        Console.WriteLine("output: " + outputFolder);
        return 0;
    }

    private static int ExportFormulaTemplate(string[] args)
    {
        if (args.Length is not 2 and not 3)
            return Usage();

        var outputPath = Path.GetFullPath(args[1]);
        var languageMap = args.Length == 3
            ? ReadLanguageMap(Path.GetFullPath(args[2]))
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var cards = FormulaCardCatalog.GetDefaultCards();
        var template = BuildFormulaTemplate(cards, languageMap);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, template, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        Console.WriteLine("exported formula template: " + cards.Count);
        Console.WriteLine("output: " + outputPath);
        return 0;
    }

    private static int ExportHtmlTemplates(string[] args)
    {
        if (args.Length != 2)
            return Usage();

        var outputFolder = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(outputFolder);
        var formulaFolder = Path.Combine(outputFolder, "source", "templates", "formula");
        Directory.CreateDirectory(formulaFolder);
        File.WriteAllText(Path.Combine(formulaFolder, "index.html"), FormulaIndexPackageTemplate, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.WriteAllText(Path.Combine(formulaFolder, "card.html"), FormulaCardPackageTemplate, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        var nodFolder = Path.Combine(outputFolder, "source", "templates", "nod");
        Directory.CreateDirectory(nodFolder);
        File.WriteAllText(Path.Combine(nodFolder, "full-page.html"), NodFullPagePackageTemplate, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.WriteAllText(Path.Combine(nodFolder, "command-page.html"), NodCommandPagePackageTemplate, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.WriteAllText(Path.Combine(nodFolder, "popup.html"), NodPopupPackageTemplate, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.WriteAllText(Path.Combine(nodFolder, "snippet.html"), NodSnippetPackageTemplate, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        Console.WriteLine("exported html templates: 6");
        Console.WriteLine("output: " + outputFolder);
        return 0;
    }

    private static string GetErrorCode(Exception ex)
    {
        if (ex is DirectoryNotFoundException)
            return "E_INPUT_NOT_FOUND";
        if (ex is FileNotFoundException fileNotFound)
            return fileNotFound.FileName?.EndsWith("manifest.json", StringComparison.OrdinalIgnoreCase) == true
                ? "E_MANIFEST_MISSING"
                : "E_FILE_NOT_FOUND";
        if (ex is UnauthorizedAccessException or IOException)
            return "E_IO";
        if (ex is JsonException)
            return "E_MANIFEST_JSON";
        if (ex is InvalidOperationException && ex.Message.Contains(".lngpdk", StringComparison.OrdinalIgnoreCase))
            return "E_OUTPUT_EXTENSION";
        if (ex is not InvalidDataException and not InvalidOperationException)
            return "E_UNKNOWN";

        var message = ex.Message;
        if (message.Contains("base package", StringComparison.OrdinalIgnoreCase))
            return "E_BASE_PACKAGE";
        if (message.Contains("manifest", StringComparison.OrdinalIgnoreCase))
            return "E_MANIFEST_INVALID";
        if (message.Contains("language/", StringComparison.OrdinalIgnoreCase) && message.Contains("required", StringComparison.OrdinalIgnoreCase))
            return "E_REQUIRED_FILE";
        if (message.Contains("absolute", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("unsafe path", StringComparison.OrdinalIgnoreCase))
        {
            return "E_PATH_UNSAFE";
        }
        if (message.Contains("blocked file type", StringComparison.OrdinalIgnoreCase))
            return "E_FILE_BLOCKED";
        if (message.Contains("unsupported file type", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("unsupported package path", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("unsupported script file", StringComparison.OrdinalIgnoreCase))
        {
            return "E_FILE_UNSUPPORTED";
        }
        if (message.Contains("mojibake", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("broken internal link", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("missing image", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("concept warning markup", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("JavaScript links", StringComparison.OrdinalIgnoreCase))
        {
            return "E_QUALITY_GATE";
        }
        if (message.Contains("too many files", StringComparison.OrdinalIgnoreCase))
            return "E_LIMIT_FILE_COUNT";
        if (message.Contains("too large", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("payload size", StringComparison.OrdinalIgnoreCase))
        {
            return "E_LIMIT_SIZE";
        }
        if (message.Contains("SHA-256", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("checksum", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("hash", StringComparison.OrdinalIgnoreCase))
        {
            return "E_CHECKSUM";
        }
        if (message.Contains("encrypted", StringComparison.OrdinalIgnoreCase))
            return "E_ENCRYPTED";
        if (message.Contains("signature", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("signed", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("signing", StringComparison.OrdinalIgnoreCase))
        {
            return "E_SIGNATURE";
        }
        if (message.Contains("container format", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("header format", StringComparison.OrdinalIgnoreCase))
        {
            return "E_UNSUPPORTED_FORMAT";
        }

        return "E_PACKAGE_INVALID";
    }

    internal static LanguagePackageBuildResult BuildLanguagePackage(
        string inputFolderValue,
        string outputPathValue,
        LanguagePackageSigningOptions? signing = null)
    {
        using var preparedInput = PrepareSourceFolder(inputFolderValue);
        var inputFolder = preparedInput.Folder;
        var outputPath = Path.GetFullPath(outputPathValue);
        if (!Path.GetExtension(outputPath).Equals(CompiledPackageExtension, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Output file must use .lngpdk extension.");

        var manifestPath = Path.Combine(inputFolder, "manifest.json");
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("manifest.json is required.", manifestPath);

        var manifest = JsonSerializer.Deserialize<LanguagePackageManifest>(
            File.ReadAllText(manifestPath),
            JsonOptions) ?? throw new InvalidDataException("manifest.json is invalid.");
        ValidateManifest(manifest);
        ValidateSourceFolder(inputFolder, manifest);
        if (signing is not null)
            ValidateSignedPackageLanguageKeys(inputFolder, manifest);

        var payload = BuildZipPayload(inputFolder, manifest);
        var payloadHash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        var header = new LanguagePackageContainerHeader
        {
            Format = ContainerFormat,
            SoftwareId = manifest.SoftwareId,
            PackageType = PackageType,
            PayloadFormat = "zip",
            PayloadSha256 = payloadHash,
            Encrypted = false,
            Signed = signing is not null,
        };
        if (signing is not null)
        {
            header.SignatureAlgorithm = signing.Algorithm;
            header.SignatureKeyId = signing.KeyId;
            header.Signature = SignPayloadHeader(header, payloadHash, signing);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        WriteWrappedPackage(outputPath, header, payload);

        return new LanguagePackageBuildResult(
            Success: true,
            OutputPath: outputPath,
            PackageKey: manifest.PackageKey,
            Product: manifest.Product,
            SoftwareId: manifest.SoftwareId,
            LanguageCode: manifest.LanguageCode,
            DisplayName: manifest.DisplayName,
            EntryCount: Directory.GetFiles(inputFolder, "*", SearchOption.AllDirectories).Length,
            PackageSha256: ComputeSha256(File.ReadAllBytes(outputPath)),
            PayloadSha256: payloadHash,
            Encrypted: false,
            Signed: signing is not null,
            SignatureAlgorithm: signing?.Algorithm ?? "",
            SignatureKeyId: signing?.KeyId ?? "",
            BasePackage: "",
            AddedEntries: [],
            AddedLanguageKeys: []);
    }

    internal static LanguagePackageBuildResult BuildLanguagePackageWithBase(
        string basePackageValue,
        string inputFolderValue,
        string outputPathValue,
        LanguagePackageSigningOptions? signing = null)
    {
        var basePackage = Path.GetFullPath(basePackageValue);
        using var preparedInput = PrepareSourceFolder(inputFolderValue);
        var inputFolder = preparedInput.Folder;

        var baseEntries = ReadPackageEntryBytes(basePackage);
        if (!baseEntries.TryGetValue("manifest.json", out var baseManifestBytes))
            throw new InvalidDataException("Base package manifest.json is missing.");

        var baseManifest = JsonSerializer.Deserialize<LanguagePackageManifest>(
            DecodeUtf8Text(baseManifestBytes),
            JsonOptions) ?? throw new InvalidDataException("Base package manifest.json is invalid.");
        ValidateManifest(baseManifest);

        var targetManifestPath = Path.Combine(inputFolder, "manifest.json");
        if (!File.Exists(targetManifestPath))
            throw new FileNotFoundException("manifest.json is required.", targetManifestPath);

        var targetManifest = JsonSerializer.Deserialize<LanguagePackageManifest>(
            File.ReadAllText(targetManifestPath),
            JsonOptions) ?? throw new InvalidDataException("manifest.json is invalid.");
        ValidateManifest(targetManifest);
        ValidateSameApp(baseManifest, targetManifest);

        var tempFolder = Path.Combine(Path.GetTempPath(), "Tiedragon.LanguagePackage", "base-merge", Guid.NewGuid().ToString("N"));
        try
        {
            CopySourceFolder(inputFolder, tempFolder);
            var addedEntries = AddMissingBaseEntries(baseEntries, baseManifest, tempFolder);
            var addedLanguageKeys = AddMissingLanguageKeys(baseEntries, baseManifest, targetManifest, tempFolder);
            var result = BuildLanguagePackage(tempFolder, outputPathValue, signing);
            return result with
            {
                BasePackage = basePackage,
                AddedEntries = addedEntries,
                AddedLanguageKeys = addedLanguageKeys,
            };
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static PreparedSourceFolder PrepareSourceFolder(string sourceValue)
    {
        var sourcePath = Path.GetFullPath(sourceValue);
        if (Directory.Exists(sourcePath))
            return new PreparedSourceFolder(sourcePath, null);

        if (File.Exists(sourcePath) && Path.GetExtension(sourcePath).Equals(ObjectPackageExtension, StringComparison.OrdinalIgnoreCase))
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "Tiedragon.LanguagePackage", "objpdk", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempFolder);
            ExtractObjectPackage(sourcePath, tempFolder);
            return new PreparedSourceFolder(tempFolder, tempFolder);
        }

        throw new DirectoryNotFoundException("Input source must be a folder or .objpdk package: " + sourceValue);
    }

    private static void ExtractObjectPackage(string sourcePackage, string targetFolder)
    {
        var root = Path.GetFullPath(targetFolder);
        using var file = File.OpenRead(sourcePackage);
        using var archive = new ZipArchive(file, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
                continue;

            var relative = NormalizeEntryName(entry.FullName);
            ValidateEntryName(relative);
            var targetPath = GetSafeEntryPath(root, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            using var input = entry.Open();
            using var output = File.Create(targetPath);
            input.CopyTo(output);
        }
    }

    private static LanguagePackageSigningOptions ReadSigningOptions(string privateKeyPathValue, string keyIdValue)
    {
        var privateKeyPath = Path.GetFullPath(privateKeyPathValue);
        if (!File.Exists(privateKeyPath))
            throw new FileNotFoundException("Private signing key was not found.", privateKeyPath);

        var keyId = keyIdValue.Trim();
        if (!IsSafeKey(keyId))
            throw new InvalidOperationException("Signing key id is invalid.");

        return new LanguagePackageSigningOptions(
            "rsa-pss-sha256",
            keyId,
            File.ReadAllText(privateKeyPath, Encoding.UTF8));
    }

    private static string SignPayloadHeader(
        LanguagePackageContainerHeader header,
        string payloadSha256,
        LanguagePackageSigningOptions signing)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(signing.PrivateKeyPem);
        var signingInput = LanguagePackageSignatureVerifier.BuildSigningInput(
            header.SoftwareId,
            header.PackageType,
            header.PayloadFormat,
            payloadSha256,
            signing.Algorithm,
            signing.KeyId);
        var signature = rsa.SignData(signingInput, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        return Convert.ToBase64String(signature);
    }

    private static int ValidatePackage(string[] args)
    {
        if (args.Length != 2)
            return Usage();

        var packagePath = Path.GetFullPath(args[1]);
        var inspection = Inspect(packagePath);
        ValidateArchiveEntries(inspection.Payload);
        var manifestText = ReadArchiveEntry(inspection.Payload, "manifest.json") ??
            throw new InvalidDataException("manifest.json is missing.");
        var manifest = JsonSerializer.Deserialize<LanguagePackageManifest>(manifestText, JsonOptions) ??
            throw new InvalidDataException("manifest.json is invalid.");
        ValidateManifest(manifest);

        if (ReadArchiveEntry(inspection.Payload, "language/" + manifest.LanguageCode + ".lng") is null)
            throw new InvalidDataException($"language/{manifest.LanguageCode}.lng is missing.");
        if (inspection.Header is not null &&
            !inspection.Header.SoftwareId.Equals(manifest.SoftwareId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Header softwareId does not match manifest softwareId.");
        }

        Console.WriteLine("valid: " + packagePath);
        Console.WriteLine("key: " + manifest.PackageKey);
        Console.WriteLine("product: " + manifest.Product);
        Console.WriteLine("softwareId: " + manifest.SoftwareId);
        Console.WriteLine("language: " + manifest.LanguageCode);
        Console.WriteLine("wrapped: " + inspection.IsWrapped);
        Console.WriteLine("packageSha256: " + inspection.PackageSha256);
        Console.WriteLine("payloadSha256: " + ComputeSha256(inspection.Payload));
        return 0;
    }

    private static int InspectPackage(string[] args)
    {
        if (args.Length != 2)
            return Usage();

        var packagePath = Path.GetFullPath(args[1]);
        var inspection = Inspect(packagePath);
        var manifestText = ReadArchiveEntry(inspection.Payload, "manifest.json");
        var manifest = manifestText is null
            ? null
            : JsonSerializer.Deserialize<LanguagePackageManifest>(manifestText, JsonOptions);

        Console.WriteLine("file: " + packagePath);
        Console.WriteLine("wrapped: " + inspection.IsWrapped);
        Console.WriteLine("packageSha256: " + inspection.PackageSha256);
        Console.WriteLine("payloadBytes: " + inspection.Payload.Length);
        Console.WriteLine("payloadSha256: " + ComputeSha256(inspection.Payload));
        if (inspection.Header is not null)
        {
            Console.WriteLine("softwareId: " + inspection.Header.SoftwareId);
            Console.WriteLine("packageType: " + inspection.Header.PackageType);
            Console.WriteLine("payloadFormat: " + inspection.Header.PayloadFormat);
            Console.WriteLine("headerSha256: " + inspection.Header.PayloadSha256);
            Console.WriteLine("encrypted: " + inspection.Header.Encrypted);
            Console.WriteLine("signed: " + inspection.Header.Signed);
        }

        if (manifest is not null)
        {
            Console.WriteLine("manifestKey: " + manifest.PackageKey);
            Console.WriteLine("displayName: " + manifest.DisplayName);
            Console.WriteLine("languageCode: " + manifest.LanguageCode);
        }

        Console.WriteLine("entries:");
        foreach (var entry in ListEntries(inspection.Payload).OrderBy(entry => entry, StringComparer.OrdinalIgnoreCase))
            Console.WriteLine("  " + entry);

        return 0;
    }

    private static Dictionary<string, string> ReadLanguageMap(string languageFile)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(languageFile))
            return map;

        foreach (var rawLine in File.ReadAllLines(languageFile, Encoding.UTF8))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';'))
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (key.Length == 0)
                continue;

            map[key] = DecodeLanguageValue(value);
        }

        return map;
    }

    private static string DecodeLanguageValue(string value)
    {
        return value
            .Replace("\\r\\n", "\n", StringComparison.Ordinal)
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("\\t", "\t", StringComparison.Ordinal);
    }

    private static string BuildFormulaTemplate(IReadOnlyList<FormulaCard> cards, IReadOnlyDictionary<string, string> languageMap)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Syscalculator formula card language template");
        builder.AppendLine("# Format: key=value");
        builder.AppendLine("# Copy missing keys into a language/*.lng file and translate the values.");
        builder.AppendLine();

        AppendLanguageTemplateValue(builder, languageMap, "formula_card.index.title", "Formulekaart");
        AppendLanguageTemplateValue(builder, languageMap, "formula_card.index.intro", "De formulekaart bundelt wiskundige basisregels, MathML, LaTeX en voorbeeld-NOD voor gebruik in Syscalculator.");
        AppendLanguageTemplateValue(builder, languageMap, "formula_card.index.column.formula", "Formule");
        AppendLanguageTemplateValue(builder, languageMap, "formula_card.index.column.category", "Categorie");
        AppendLanguageTemplateValue(builder, languageMap, "formula_card.index.column.tags", "Tags");
        AppendLanguageTemplateValue(builder, languageMap, "formula_card.index.column.description", "Uitleg");
        AppendLanguageTemplateValue(builder, languageMap, "formula_card.section.formula", "Formule");
        AppendLanguageTemplateValue(builder, languageMap, "formula_card.section.text", "Tekst");
        AppendLanguageTemplateValue(builder, languageMap, "formula_card.section.latex", "LaTeX");
        AppendLanguageTemplateValue(builder, languageMap, "formula_card.section.mathml", "MathML");
        AppendLanguageTemplateValue(builder, languageMap, "formula_card.section.example_nod", "Voorbeeld-NOD");

        foreach (var card in cards.OrderBy(card => card.Id, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine();
            builder.AppendLine("# " + card.Id);
            var key = "formula_card.card." + card.Id + ".";
            AppendLanguageTemplateValue(builder, languageMap, key + "title", card.Title);
            AppendLanguageTemplateValue(builder, languageMap, key + "formula_text", card.Formula);
            AppendLanguageTemplateValue(builder, languageMap, key + "plain_text", card.PlainText);
            AppendLanguageTemplateValue(builder, languageMap, key + "tags", string.Join("|", card.LevelTags));
            AppendLanguageTemplateValue(builder, languageMap, key + "description", card.Description);
            AppendLanguageTemplateValue(builder, languageMap, key + "example_nod", card.ExampleNod);
        }

        return builder.ToString();
    }

    private static void AppendLanguageTemplateValue(StringBuilder builder, IReadOnlyDictionary<string, string> languageMap, string key, string fallback)
    {
        builder
            .Append(key)
            .Append('=')
            .AppendLine(EncodeLanguageValue(T(languageMap, key, fallback)));
    }

    private static string EncodeLanguageValue(string value)
    {
        return value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    private static LocalizedFormulaCard LocalizeFormulaCard(FormulaCard card, IReadOnlyDictionary<string, string> languageMap)
    {
        var key = "formula_card.card." + card.Id + ".";
        return new LocalizedFormulaCard(
            card,
            T(languageMap, key + "title", card.Title),
            T(languageMap, key + "formula_text", card.Formula),
            T(languageMap, key + "plain_text", card.PlainText),
            SplitFormulaTags(T(languageMap, key + "tags", string.Join("|", card.LevelTags))),
            T(languageMap, key + "description", card.Description),
            T(languageMap, key + "example_nod", card.ExampleNod));
    }

    private static IReadOnlyList<string> SplitFormulaTags(string value)
    {
        return value
            .Split(['|', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToArray();
    }

    private static string T(IReadOnlyDictionary<string, string> languageMap, string key, string fallback)
    {
        return languageMap.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    private static string BuildFormulaIndexHtml(IReadOnlyList<LocalizedFormulaCard> cards, IReadOnlyDictionary<string, string> languageMap, string template)
    {
        var rows = new StringBuilder();
        foreach (var card in cards.OrderBy(card => card.Title, StringComparer.CurrentCultureIgnoreCase))
        {
            rows.Append("    <tr><td><a href=\"")
                .Append(Html(BuildFormulaCardPackagePath(card.Source)))
                .Append("\">")
                .Append(Html(card.Title))
                .Append("</a></td><td>")
                .Append(Html(FriendlyFormulaCategory(BuildFormulaCardCategory(card.Source))))
                .Append("</td><td>")
                .Append(Html(string.Join(", ", card.Tags)))
                .Append("</td><td>")
                .Append(Html(card.Description))
                .Append("</td></tr>")
                .AppendLine();
        }

        return ApplyFormulaTemplate(template, new Dictionary<string, string?>
        {
            ["title"] = Html(T(languageMap, "formula_card.index.title", "Formulekaart")),
            ["intro"] = Html(T(languageMap, "formula_card.index.intro", "De formulekaart bundelt wiskundige basisregels, MathML, LaTeX en voorbeeld-NOD voor gebruik in Syscalculator.")),
            ["column_formula"] = Html(T(languageMap, "formula_card.index.column.formula", "Formule")),
            ["column_category"] = Html(T(languageMap, "formula_card.index.column.category", "Categorie")),
            ["column_tags"] = Html(T(languageMap, "formula_card.index.column.tags", "Tags")),
            ["column_description"] = Html(T(languageMap, "formula_card.index.column.description", "Uitleg")),
            ["rows"] = rows.ToString()
        });
    }

    private static string BuildFormulaCardHtml(LocalizedFormulaCard card, IReadOnlyDictionary<string, string> languageMap, string template)
    {
        return ApplyFormulaTemplate(template, new Dictionary<string, string?>
        {
            ["title"] = Html(card.Title),
            ["tags"] = Html(string.Join(", ", card.Tags)),
            ["description"] = RenderFormulaCardDescription(card.Description),
            ["section_formula"] = Html(T(languageMap, "formula_card.section.formula", "Formule")),
            ["formula_text"] = Html(card.FormulaText),
            ["mathml_card"] = card.Source.MathMl,
            ["section_text"] = Html(T(languageMap, "formula_card.section.text", "Tekst")),
            ["plain_text"] = Html(card.PlainText),
            ["section_latex"] = Html(T(languageMap, "formula_card.section.latex", "LaTeX")),
            ["latex"] = Html(card.Source.Latex),
            ["section_mathml"] = Html(T(languageMap, "formula_card.section.mathml", "MathML")),
            ["mathml_pre"] = Html(card.Source.MathMl),
            ["section_example_nod"] = Html(T(languageMap, "formula_card.section.example_nod", "Voorbeeld-NOD")),
            ["example_nod"] = Html(card.ExampleNod)
        });
    }

    private static string ApplyFormulaTemplate(string template, IReadOnlyDictionary<string, string?> values)
    {
        var result = template;
        foreach (var (key, value) in values)
            result = result.Replace("{{" + key + "}}", value ?? "", StringComparison.Ordinal);

        return result;
    }

    private static string BuildFormulaCardPackagePath(FormulaCard card)
    {
        return BuildFormulaCardCategory(card) + "/" + card.Id + ".html";
    }

    private static string BuildFormulaCardCategory(FormulaCard card)
    {
        var tags = card.LevelTags;
        if (tags.Any(tag => tag.Equals("Statistiek", StringComparison.OrdinalIgnoreCase)))
            return "statistiek";
        if (tags.Any(tag => tag.Equals("Kansrekening", StringComparison.OrdinalIgnoreCase)))
            return "kansrekening";
        if (tags.Any(tag => tag.Equals("Goniometrie", StringComparison.OrdinalIgnoreCase)))
            return "goniometrie";
        if (tags.Any(tag => tag.Contains("Meetkunde", StringComparison.OrdinalIgnoreCase) ||
                            tag.Equals("2D", StringComparison.OrdinalIgnoreCase) ||
                            tag.Equals("3D", StringComparison.OrdinalIgnoreCase)))
            return "meetkunde";
        if (tags.Any(tag => tag.Contains("Analyse", StringComparison.OrdinalIgnoreCase) ||
                            tag.Contains("Different", StringComparison.OrdinalIgnoreCase) ||
                            tag.Contains("Integra", StringComparison.OrdinalIgnoreCase)))
            return "analyse";
        if (tags.Any(tag => tag.Equals("Algebra", StringComparison.OrdinalIgnoreCase)))
            return "algebra";

        return "basis";
    }

    private static string FriendlyFormulaCategory(string category)
    {
        return category switch
        {
            "analyse" => "Analyse",
            "algebra" => "Algebra",
            "basis" => "Basis",
            "goniometrie" => "Goniometrie",
            "kansrekening" => "Kansrekening",
            "meetkunde" => "Meetkunde",
            "statistiek" => "Statistiek",
            _ => category
        };
    }

    private static string RenderFormulaCardDescription(string description)
    {
        var html = Html(description);
        foreach (var command in new[] { "length", "distance", "dot", "angle", "angled", "cross", "det", "trace", "mget", "vec" })
        {
            html = Regex.Replace(
                html,
                @"\b" + Regex.Escape(command) + @"\b",
                "<a class=\"cmd-link\" href=\"nodpage:cmd:" + command + "\"><code>" + command + "</code></a>",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return html;
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private static byte[] BuildZipPayload(string inputFolder, LanguagePackageManifest manifest)
    {
        var recipe = LoadCompilerRecipe(inputFolder);
        var explicitEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in Directory.GetFiles(inputFolder, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var relative = Path.GetRelativePath(inputFolder, file).Replace('\\', '/');
                if (IsCompileSourceOnlyEntry(relative))
                    continue;

                ValidateEntryName(relative);

                var info = new FileInfo(file);
                if (info.Length > MaxEntryBytes)
                    throw new InvalidDataException("File is too large: " + relative);

                WriteArchiveEntry(archive, relative, File.ReadAllBytes(file));
                explicitEntries.Add(NormalizeEntryName(relative));
            }

            foreach (var entry in BuildGeneratedEntries(inputFolder, manifest, recipe))
            {
                if (explicitEntries.Contains(entry.Key))
                    continue;

                WriteArchiveEntry(archive, entry.Key, Encoding.UTF8.GetBytes(entry.Value));
            }
        }

        return memory.ToArray();
    }

    private static IReadOnlyDictionary<string, string> BuildGeneratedEntries(
        string inputFolder,
        LanguagePackageManifest manifest,
        LanguagePackageCompilerRecipe recipe)
    {
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (recipe.GenerateFormulaCards)
        {
            foreach (var entry in BuildGeneratedFormulaEntries(inputFolder, manifest))
                entries[entry.Key] = entry.Value;
        }

        return entries;
    }

    private static LanguagePackageCompilerRecipe LoadCompilerRecipe(string inputFolder)
    {
        var path = Path.Combine(inputFolder, CompilerRecipeFileName);
        if (!File.Exists(path))
            return LanguagePackageCompilerRecipe.Default;

        var recipe = JsonSerializer.Deserialize<LanguagePackageCompilerRecipe>(
            File.ReadAllText(path, Encoding.UTF8),
            JsonOptions) ?? throw new InvalidDataException(CompilerRecipeFileName + " is invalid.");
        recipe.Validate();
        return recipe;
    }

    private static IReadOnlyDictionary<string, string> BuildGeneratedFormulaEntries(string inputFolder, LanguagePackageManifest manifest)
    {
        var languagePath = Path.Combine(inputFolder, "language", manifest.LanguageCode + ".lng");
        var languageMap = File.Exists(languagePath)
            ? ReadLanguageMap(languagePath)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var indexTemplate = ReadOptionalTemplate(inputFolder, "formula/index.html", "formula-index.html", FormulaIndexPackageTemplate);
        var cardTemplate = ReadOptionalTemplate(inputFolder, "formula/card.html", "formula-card.html", FormulaCardPackageTemplate);
        var cards = FormulaCardCatalog.GetDefaultCards()
            .Select(card => LocalizeFormulaCard(card, languageMap))
            .ToArray();
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["formula/index.html"] = BuildFormulaIndexHtml(cards, languageMap, indexTemplate)
        };

        foreach (var card in cards.OrderBy(card => card.Title, StringComparer.CurrentCultureIgnoreCase))
            entries["formula/" + BuildFormulaCardPackagePath(card.Source)] = BuildFormulaCardHtml(card, languageMap, cardTemplate);

        return entries;
    }

    private static string ReadOptionalTemplate(string inputFolder, string templatePath, string legacyFileName, string fallback)
    {
        var path = Path.Combine(inputFolder, "source", "templates", templatePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path))
            return File.ReadAllText(path, Encoding.UTF8);

        var legacyPath = Path.Combine(inputFolder, "templates", legacyFileName);
        return File.Exists(legacyPath) ? File.ReadAllText(legacyPath, Encoding.UTF8) : fallback;
    }

    private static void WriteArchiveEntry(ZipArchive archive, string relative, byte[] bytes)
    {
        ValidateEntryName(relative);
        if (bytes.Length > MaxEntryBytes)
            throw new InvalidDataException("File is too large: " + relative);

        var entry = archive.CreateEntry(relative, CompressionLevel.Optimal);
        using var entryStream = entry.Open();
        entryStream.Write(bytes);
    }

    private static bool IsCompileSourceOnlyEntry(string entryName)
    {
        var normalized = NormalizeEntryName(entryName);
        return normalized.Equals(CompilerRecipeFileName, StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("source/templates/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("templates/", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, byte[]> ReadPackageEntryBytes(string sourcePath)
    {
        if (Directory.Exists(sourcePath))
            return ReadDirectoryEntryBytes(sourcePath);
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Base package was not found.", sourcePath);

        var payload = Path.GetExtension(sourcePath).Equals(ObjectPackageExtension, StringComparison.OrdinalIgnoreCase)
            ? File.ReadAllBytes(sourcePath)
            : Inspect(sourcePath).Payload;
        ValidateArchiveEntries(payload);

        var entries = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        using var archive = ArchiveFactory.OpenArchive(new MemoryStream(payload, writable: false));
        foreach (var entry in archive.Entries)
        {
            if (entry.IsDirectory)
                continue;

            var name = NormalizeEntryName(entry.Key);
            ValidateEntryName(name);
            using var stream = entry.OpenEntryStream();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            entries[name] = memory.ToArray();
        }

        return entries;
    }

    private static IReadOnlyDictionary<string, byte[]> ReadDirectoryEntryBytes(string sourceFolder)
    {
        var root = Path.GetFullPath(sourceFolder);
        var entries = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            ValidateEntryName(relative);
            entries[relative] = File.ReadAllBytes(file);
        }

        return entries;
    }

    private static void CopySourceFolder(string sourceFolder, string targetFolder)
    {
        var root = Path.GetFullPath(sourceFolder);
        foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            ValidateEntryName(relative);
            WriteEntryBytes(targetFolder, relative, File.ReadAllBytes(file));
        }
    }

    private static IReadOnlyList<string> AddMissingBaseEntries(
        IReadOnlyDictionary<string, byte[]> baseEntries,
        LanguagePackageManifest baseManifest,
        string targetFolder)
    {
        var added = new List<string>();
        foreach (var entry in baseEntries.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (entry.Key.Equals("manifest.json", StringComparison.OrdinalIgnoreCase) ||
                entry.Key.Equals("language/" + baseManifest.LanguageCode + ".lng", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var targetPath = GetSafeEntryPath(targetFolder, entry.Key);
            if (File.Exists(targetPath))
                continue;

            WriteEntryBytes(targetFolder, entry.Key, entry.Value);
            added.Add(entry.Key);
        }

        return added;
    }

    private static IReadOnlyList<string> AddMissingLanguageKeys(
        IReadOnlyDictionary<string, byte[]> baseEntries,
        LanguagePackageManifest baseManifest,
        LanguagePackageManifest targetManifest,
        string targetFolder)
    {
        var baseLanguagePath = "language/" + baseManifest.LanguageCode + ".lng";
        if (!baseEntries.TryGetValue(baseLanguagePath, out var baseLanguageBytes))
            throw new InvalidDataException(baseLanguagePath + " is missing in base package.");

        var targetLanguagePath = "language/" + targetManifest.LanguageCode + ".lng";
        var targetPath = GetSafeEntryPath(targetFolder, targetLanguagePath);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        if (!File.Exists(targetPath))
            File.WriteAllText(targetPath, "# Added from base package " + baseManifest.PackageKey + Environment.NewLine, Encoding.UTF8);

        var targetText = File.ReadAllText(targetPath, Encoding.UTF8);
        var knownKeys = ReadLanguageKeys(targetText);
        var added = new List<string>();
        var additions = new StringBuilder();
        foreach (var line in DecodeUtf8Text(baseLanguageBytes).Replace("\r\n", "\n").Split('\n'))
        {
            if (!TryReadLanguageKey(line, out var key) || knownKeys.Contains(key))
                continue;

            additions.AppendLine(line.TrimEnd('\r'));
            knownKeys.Add(key);
            added.Add(key);
        }

        if (additions.Length == 0)
            return added;

        var separator = targetText.EndsWith('\n') ? "" : Environment.NewLine;
        File.AppendAllText(
            targetPath,
            separator + "# Added from base package " + baseManifest.PackageKey + Environment.NewLine + additions,
            Encoding.UTF8);
        return added;
    }

    private static HashSet<string> ReadLanguageKeys(string text)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (TryReadLanguageKey(line, out var key))
                keys.Add(key);
        }

        return keys;
    }

    private static bool TryReadLanguageKey(string line, out string key)
    {
        key = "";
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith(';'))
            return false;

        var separator = trimmed.IndexOf('=');
        if (separator <= 0)
            return false;

        key = trimmed[..separator].Trim();
        return key.Length > 0;
    }

    private static string DecodeUtf8Text(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
    }

    private static void ValidateSameApp(LanguagePackageManifest baseManifest, LanguagePackageManifest targetManifest)
    {
        if (!baseManifest.Producer.Equals(targetManifest.Producer, StringComparison.OrdinalIgnoreCase) ||
            !baseManifest.Product.Equals(targetManifest.Product, StringComparison.OrdinalIgnoreCase) ||
            !baseManifest.SoftwareId.Equals(targetManifest.SoftwareId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Base package app identity does not match target manifest.");
        }
    }

    private static void WriteEntryBytes(string rootFolder, string entryName, byte[] bytes)
    {
        var path = GetSafeEntryPath(rootFolder, entryName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
    }

    private static string GetSafeEntryPath(string rootFolder, string entryName)
    {
        var root = Path.GetFullPath(rootFolder);
        var fullPath = Path.GetFullPath(Path.Combine(root, NormalizeEntryName(entryName).Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Unsafe path: " + entryName);

        return fullPath;
    }

    private static void WriteWrappedPackage(string outputPath, LanguagePackageContainerHeader header, byte[] payload)
    {
        var headerBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header, JsonOptions));
        if (headerBytes.Length > MaxHeaderBytes)
            throw new InvalidDataException("Header is too large.");
        if (payload.Length > MaxPayloadBytes)
            throw new InvalidDataException("Payload is too large.");

        using var output = File.Create(outputPath);
        output.Write(Magic);
        using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);
        writer.Write(ContainerFormat);
        writer.Write(headerBytes.Length);
        writer.Write(headerBytes);
        writer.Write(payload);
    }

    internal static PackageInspection Inspect(string packagePath)
    {
        if (!File.Exists(packagePath))
            throw new FileNotFoundException(packagePath);

        var bytes = File.ReadAllBytes(packagePath);
        var packageSha256 = ComputeSha256(bytes);
        if (!TryUnwrap(bytes, out var header, out var payload))
            payload = bytes;

        return new PackageInspection(header is not null, header, packageSha256, payload);
    }

    private static string ComputeSha256(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static bool TryUnwrap(byte[] packageBytes, out LanguagePackageContainerHeader? header, out byte[] payload)
    {
        header = null;
        payload = packageBytes;
        if (packageBytes.Length < Magic.Length + sizeof(int) + sizeof(int) ||
            !packageBytes.AsSpan(0, Magic.Length).SequenceEqual(Magic))
        {
            return false;
        }

        using var memory = new MemoryStream(packageBytes);
        memory.Position = Magic.Length;
        using var reader = new BinaryReader(memory, Encoding.UTF8, leaveOpen: true);
        var version = reader.ReadInt32();
        if (version != ContainerFormat)
            throw new InvalidDataException($"Unsupported container format: {version}.");

        var headerLength = reader.ReadInt32();
        if (headerLength <= 0 || headerLength > MaxHeaderBytes)
            throw new InvalidDataException("Invalid header length.");

        var headerBytes = reader.ReadBytes(headerLength);
        header = JsonSerializer.Deserialize<LanguagePackageContainerHeader>(
            Encoding.UTF8.GetString(headerBytes),
            JsonOptions) ?? throw new InvalidDataException("Invalid header JSON.");
        ValidateContainerHeader(header);

        payload = reader.ReadBytes((int)(memory.Length - memory.Position));
        if (payload.Length <= 0 || payload.Length > MaxPayloadBytes)
            throw new InvalidDataException("Invalid payload size.");

        if (!string.IsNullOrWhiteSpace(header.PayloadSha256))
        {
            var hash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
            if (!hash.Equals(header.PayloadSha256.Trim().ToLowerInvariant(), StringComparison.Ordinal))
                throw new InvalidDataException("Payload SHA-256 does not match header.");
        }

        LanguagePackageSignatureVerifier.VerifyOrThrow(
            header.Signed,
            header.SignatureAlgorithm,
            header.SignatureKeyId,
            header.Signature,
            payload,
            header.PayloadSha256,
            header.SoftwareId,
            header.PackageType,
            header.PayloadFormat);

        return true;
    }

    internal static string? ReadArchiveEntry(byte[] payload, string entryName)
    {
        using var archive = ArchiveFactory.OpenArchive(new MemoryStream(payload, writable: false));
        var entry = archive.Entries.FirstOrDefault(entry =>
            !entry.IsDirectory &&
            NormalizeEntryName(entry.Key).Equals(entryName, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            return null;
        if (entry.Size > MaxEntryBytes)
            throw new InvalidDataException("Archive entry is too large: " + entryName);

        using var stream = entry.OpenEntryStream();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    internal static IReadOnlyList<string> ListEntries(byte[] payload)
    {
        using var archive = ArchiveFactory.OpenArchive(new MemoryStream(payload, writable: false));
        return archive.Entries
            .Where(entry => !entry.IsDirectory)
            .Select(entry => NormalizeEntryName(entry.Key))
            .ToList();
    }

    private static void ValidateSourceFolder(string inputFolder, LanguagePackageManifest manifest)
    {
        var files = Directory.GetFiles(inputFolder, "*", SearchOption.AllDirectories);
        if (files.Length > MaxEntryCount)
            throw new InvalidDataException("Too many files.");

        long totalBytes = 0;
        var entries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(inputFolder, file).Replace('\\', '/');
            if (IsCompileSourceOnlyEntry(relative))
                continue;

            ValidateEntryName(relative);
            entries.Add(NormalizeEntryName(relative));
            var length = new FileInfo(file).Length;
            if (length > MaxEntryBytes)
                throw new InvalidDataException("File is too large: " + relative);
            totalBytes += length;
        }

        if (totalBytes > MaxTotalEntryBytes)
            throw new InvalidDataException("Input folder is too large.");
        if (!File.Exists(Path.Combine(inputFolder, "language", manifest.LanguageCode + ".lng")))
            throw new InvalidDataException($"language/{manifest.LanguageCode}.lng is required.");

        ValidateSourceQuality(inputFolder, entries);
    }

    private static void ValidateSignedPackageLanguageKeys(string inputFolder, LanguagePackageManifest manifest)
    {
        var languagePath = Path.Combine(inputFolder, "language", manifest.LanguageCode + ".lng");
        var languageMap = ReadLanguageMap(languagePath);
        var missingKeys = RequiredSignedLanguageKeys
            .Where(key => !languageMap.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            .ToArray();

        if (missingKeys.Length == 0)
            return;

        throw new InvalidDataException(
            "Signed language package is missing required language keys: " +
            string.Join(", ", missingKeys) +
            ". Add these keys before compiling/signing.");
    }

    internal static void ValidateArchiveEntries(byte[] payload)
    {
        using var archive = ArchiveFactory.OpenArchive(new MemoryStream(payload, writable: false));
        var count = 0;
        long totalBytes = 0;
        foreach (var entry in archive.Entries)
        {
            if (entry.IsDirectory)
                continue;

            count++;
            if (count > MaxEntryCount)
                throw new InvalidDataException("Package contains too many files.");
            if (entry.Size > MaxEntryBytes)
                throw new InvalidDataException("Package entry is too large: " + entry.Key);
            totalBytes += Math.Max(0L, entry.Size);
            if (totalBytes > MaxTotalEntryBytes)
                throw new InvalidDataException("Package is too large after decompression.");
            ValidateEntryName(NormalizeEntryName(entry.Key));
        }
    }

    private static void ValidateManifest(LanguagePackageManifest manifest)
    {
        if (manifest.Format != 1)
            throw new InvalidDataException("Unsupported manifest format.");
        if (!ProducerName.Equals(manifest.Producer, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Manifest producer must be Tiedragon.");
        if (!IsSafeProductName(manifest.Product))
            throw new InvalidDataException("Manifest product is invalid.");
        if (!IsSafeKey(manifest.SoftwareId))
            throw new InvalidDataException("Manifest softwareId is invalid.");
        if (!IsSafeKey(manifest.PackageKey))
            throw new InvalidDataException("Manifest key is invalid.");
        if (!IsSafeLanguageCode(manifest.LanguageCode))
            throw new InvalidDataException("Language code is invalid.");
        if (string.IsNullOrWhiteSpace(manifest.DisplayName))
            throw new InvalidDataException("displayName is required.");
    }

    private static void ValidateContainerHeader(LanguagePackageContainerHeader header)
    {
        if (header.Format != ContainerFormat)
            throw new InvalidDataException("Unsupported header format.");
        if (!IsSafeKey(header.SoftwareId))
            throw new InvalidDataException("Unsupported softwareId.");
        if (!PackageType.Equals(header.PackageType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Unsupported packageType.");
        if (header.Encrypted)
            throw new InvalidDataException("Encrypted packages are not supported yet.");
        if (!string.IsNullOrWhiteSpace(header.PayloadSha256) && !IsSha256Hex(header.PayloadSha256))
            throw new InvalidDataException("Invalid payloadSha256.");
        if (!header.Signed &&
            (!string.IsNullOrWhiteSpace(header.SignatureAlgorithm) ||
             !string.IsNullOrWhiteSpace(header.SignatureKeyId) ||
             !string.IsNullOrWhiteSpace(header.Signature)))
        {
            throw new InvalidDataException("Signature fields are present but signed is false.");
        }
    }

    private static void ValidateEntryName(string entryName)
    {
        if (Path.IsPathRooted(entryName))
            throw new InvalidDataException("Absolute paths are not allowed: " + entryName);

        var normalized = NormalizeEntryName(entryName);
        if (normalized.Length == 0 || normalized.Split('/').Any(part => part == ".."))
            throw new InvalidDataException("Unsafe path: " + entryName);

        if (!LanguagePackagePolicy.Current.IsAllowedPackagePath(normalized))
            throw new InvalidDataException("Unsupported package path: " + normalized);
    }

    private static void ValidateSourceQuality(string inputFolder, HashSet<string> entries)
    {
        foreach (var entry in entries.Order(StringComparer.OrdinalIgnoreCase))
        {
            if (!IsTextEntry(entry))
                continue;

            var path = Path.Combine(inputFolder, entry.Replace('/', Path.DirectorySeparatorChar));
            var text = File.ReadAllText(path, Encoding.UTF8);
            ValidateTextQuality(entry, text);
            if (Path.GetExtension(entry).Equals(".html", StringComparison.OrdinalIgnoreCase))
                ValidateHtmlReferences(entry, text, entries);
        }
    }

    private static void ValidateTextQuality(string entryName, string text)
    {
        if (ContainsMojibakeMarker(text))
            throw new InvalidDataException("Encoding/mojibake detected: " + entryName);
        if (Path.GetExtension(entryName).Equals(".json", StringComparison.OrdinalIgnoreCase))
            _ = JsonDocument.Parse(text);
        if (ConceptHelpMarkupRegex.IsMatch(text))
            throw new InvalidDataException("Concept warning markup must not be stored in help content: " + entryName);
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidDataException("Empty text file: " + entryName);
    }

    private static void ValidateHtmlReferences(string entryName, string text, HashSet<string> entries)
    {
        foreach (Match match in HtmlLinkRegex.Matches(text))
        {
            var attribute = match.Groups["attr"].Value;
            var value = match.Groups["path"].Value.Trim();
            if (value.Length == 0 || IsIgnoredLink(value))
                continue;
            if (value.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("JavaScript links are not allowed: " + entryName);

            var target = ResolvePackageReference(entryName, value);
            if (target.Length == 0)
                continue;

            if (!entries.Contains(target))
            {
                var kind = attribute.Equals("src", StringComparison.OrdinalIgnoreCase) || IsImagePath(target)
                    ? "Missing image/media reference"
                    : "Broken internal link";
                throw new InvalidDataException(kind + ": " + entryName + " -> " + value);
            }

            if ((attribute.Equals("src", StringComparison.OrdinalIgnoreCase) || IsImagePath(target)) && !IsImagePath(target))
                throw new InvalidDataException("HTML media reference is not an allowed image: " + entryName + " -> " + value);
        }
    }

    private static string ResolvePackageReference(string sourceEntry, string reference)
    {
        var cleaned = reference.Split(['?', '#'], 2)[0].Replace('\\', '/').Trim();
        if (cleaned.Length == 0)
            return "";
        while (cleaned.StartsWith("./", StringComparison.Ordinal))
            cleaned = cleaned[2..];
        if (cleaned.StartsWith("/", StringComparison.Ordinal))
            return NormalizeEntryName(cleaned);

        var sourceDirectory = Path.GetDirectoryName(sourceEntry)?.Replace('\\', '/') ?? "";
        var parts = (sourceDirectory.Length == 0 ? cleaned : sourceDirectory + "/" + cleaned)
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        var stack = new List<string>();
        foreach (var part in parts)
        {
            if (part == ".")
                continue;
            if (part == "..")
            {
                if (stack.Count == 0)
                    throw new InvalidDataException("Unsafe path: " + reference);
                stack.RemoveAt(stack.Count - 1);
                continue;
            }

            stack.Add(part);
        }

        return string.Join('/', stack);
    }

    private static bool IsIgnoredLink(string value)
    {
        return value.StartsWith("#", StringComparison.Ordinal) ||
            value.StartsWith("http:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("https:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("nodpage:", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTextEntry(string entryName)
    {
        var extension = Path.GetExtension(entryName);
        return extension.Equals(".css", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".js", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".json", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".lng", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImagePath(string entryName)
    {
        return LanguagePackagePolicy.Current.IsImagePath(entryName);
    }

    private static bool ContainsMojibakeMarker(string text)
    {
        return text.Contains('\u00c3') ||
            text.Contains('\u00c2') ||
            text.Contains('\u00e2') ||
            text.Contains("\u00e4\u00b8", StringComparison.Ordinal) ||
            text.Contains("\u00e6\u2013", StringComparison.Ordinal);
    }

    private static string NormalizeEntryName(string? entryName)
    {
        return (entryName ?? "").Replace('\\', '/').TrimStart('/');
    }

    private static bool IsSafeKey(string value)
    {
        return value.Length > 0 &&
            value.Length <= 96 &&
            value.All(character => char.IsLetterOrDigit(character) || character is '.' or '-' or '_');
    }

    private static bool IsSafeProductName(string value)
    {
        return value.Length > 0 &&
            value.Length <= 96 &&
            value.All(character => char.IsLetterOrDigit(character) ||
                char.IsWhiteSpace(character) ||
                character is '.' or '-' or '_');
    }

    private static bool IsSafeLanguageCode(string value)
    {
        return value.Length is >= 2 and <= 12 &&
            value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_');
    }

    private static bool IsSha256Hex(string value)
    {
        var text = value.Trim();
        return text.Length == 64 &&
            text.All(character => char.IsDigit(character) ||
                character is >= 'a' and <= 'f' ||
                character is >= 'A' and <= 'F');
    }
}

public sealed record PackageInspection(
    bool IsWrapped,
    LanguagePackageContainerHeader? Header,
    string PackageSha256,
    byte[] Payload);

internal sealed record LocalizedFormulaCard(
    FormulaCard Source,
    string Title,
    string FormulaText,
    string PlainText,
    IReadOnlyList<string> Tags,
    string Description,
    string ExampleNod);

public sealed record LanguagePackageBuildResult(
    bool Success,
    string OutputPath,
    string PackageKey,
    string Product,
    string SoftwareId,
    string LanguageCode,
    string DisplayName,
    int EntryCount,
    string PackageSha256,
    string PayloadSha256,
    bool Encrypted,
    bool Signed,
    string SignatureAlgorithm,
    string SignatureKeyId,
    string BasePackage,
    IReadOnlyList<string> AddedEntries,
    IReadOnlyList<string> AddedLanguageKeys);

public sealed class LanguagePackageCompilerRecipe
{
    public static LanguagePackageCompilerRecipe Default { get; } = new();

    public int Format { get; set; } = 1;
    public bool GenerateFormulaCards { get; set; }

    public void Validate()
    {
        if (Format != 1)
            throw new InvalidDataException("Unsupported compiler recipe format.");
    }
}

public sealed record LanguagePackageSigningOptions(
    string Algorithm,
    string KeyId,
    string PrivateKeyPem);

public static class LanguagePackageCompiler
{
    public static LanguagePackageBuildResult Compile(string inputFolderOrObjectPackage, string outputPackagePath)
    {
        return Program.BuildLanguagePackage(inputFolderOrObjectPackage, outputPackagePath);
    }

    public static LanguagePackageBuildResult CompileSigned(
        string inputFolderOrObjectPackage,
        string outputPackagePath,
        LanguagePackageSigningOptions signingOptions)
    {
        return Program.BuildLanguagePackage(inputFolderOrObjectPackage, outputPackagePath, signingOptions);
    }

    public static LanguagePackageBuildResult CompileWithBase(
        string baseFolderOrPackage,
        string inputFolderOrObjectPackage,
        string outputPackagePath)
    {
        return Program.BuildLanguagePackageWithBase(baseFolderOrPackage, inputFolderOrObjectPackage, outputPackagePath);
    }

    public static LanguagePackageBuildResult CompileSignedWithBase(
        string baseFolderOrPackage,
        string inputFolderOrObjectPackage,
        string outputPackagePath,
        LanguagePackageSigningOptions signingOptions)
    {
        return Program.BuildLanguagePackageWithBase(
            baseFolderOrPackage,
            inputFolderOrObjectPackage,
            outputPackagePath,
            signingOptions);
    }
}

public static class LanguagePackageReader
{
    public static PackageInspection Inspect(string packagePath)
    {
        return Program.Inspect(packagePath);
    }

    public static IReadOnlyList<string> ListEntries(string packagePath)
    {
        return Program.ListEntries(Program.Inspect(packagePath).Payload);
    }

    public static void ValidateEntries(string packagePath)
    {
        Program.ValidateArchiveEntries(Program.Inspect(packagePath).Payload);
    }

    public static bool ContainsEntry(string packagePath, string entryName)
    {
        return Program.ReadArchiveEntry(Program.Inspect(packagePath).Payload, entryName) is not null;
    }

    public static bool TryReadEntry(string packagePath, string entryName, out string content)
    {
        content = "";
        try
        {
            var value = Program.ReadArchiveEntry(Program.Inspect(packagePath).Payload, entryName);
            if (value is null)
                return false;

            content = value;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryReadManifest(string packagePath, out LanguagePackageManifest manifest)
    {
        manifest = null!;
        if (!TryReadEntry(packagePath, "manifest.json", out var manifestText))
            return false;

        try
        {
            manifest = JsonSerializer.Deserialize<LanguagePackageManifest>(
                    manifestText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ??
                throw new InvalidDataException("manifest.json is invalid.");
            return true;
        }
        catch
        {
            manifest = null!;
            return false;
        }
    }
}

internal sealed class PreparedSourceFolder(string folder, string? temporaryFolder) : IDisposable
{
    public string Folder { get; } = folder;

    public void Dispose()
    {
        if (string.IsNullOrWhiteSpace(temporaryFolder))
            return;

        try
        {
            if (Directory.Exists(temporaryFolder))
                Directory.Delete(temporaryFolder, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

internal sealed record TrustedLanguagePackageKeyStore
{
    public IReadOnlyList<TrustedLanguagePackageKey> Keys { get; init; } = [];
}

internal sealed record TrustedLanguagePackageKey(
    string KeyId,
    string Algorithm,
    string SoftwareId,
    string PackageType,
    string PayloadFormat,
    string PublicKeyPem);

internal sealed record LanguagePackageAgentError(
    bool Success,
    string Code,
    string Error);

public sealed class LanguagePackageManifest
{
    public int Format { get; set; }
    public string Key { get; set; } = "";
    public string Id { get; set; } = "";
    public string Producer { get; set; } = "";
    public string Product { get; set; } = "";
    public string SoftwareId { get; set; } = "";
    public string LanguageCode { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string NativeName { get; set; } = "";
    public string AppMinVersion { get; set; } = "";
    public string PackageVersion { get; set; } = "";
    public string FallbackLanguage { get; set; } = "eng";

    public string PackageKey => string.IsNullOrWhiteSpace(Key) ? Id : Key;
}

public sealed class LanguagePackageContainerHeader
{
    public int Format { get; set; }
    public string SoftwareId { get; set; } = "";
    public string PackageType { get; set; } = "";
    public string PayloadFormat { get; set; } = "";
    public string PayloadSha256 { get; set; } = "";
    public bool Encrypted { get; set; }
    public bool Signed { get; set; }
    public string SignatureAlgorithm { get; set; } = "";
    public string SignatureKeyId { get; set; } = "";
    public string Signature { get; set; } = "";
}
