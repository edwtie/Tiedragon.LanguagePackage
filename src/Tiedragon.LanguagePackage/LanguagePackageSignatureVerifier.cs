#nullable enable

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Tiedragon.LanguagePackage;

public static class LanguagePackageSignatureVerifier
{
    private const string TrustedKeysFileName = "language-package-trusted-keys.json";
    private static readonly HashSet<string> KnownAlgorithms = new(StringComparer.OrdinalIgnoreCase)
    {
        "rsa-pss-sha256",
        "rsa-sha256",
    };

    public static void VerifyOrThrow(
        bool signed,
        string? signatureAlgorithm,
        string? signatureKeyId,
        string? signature,
        byte[] payload,
        string? payloadSha256,
        string softwareId,
        string packageType,
        string payloadFormat)
    {
        if (!signed)
            return;

        ValidateSignatureMetadata(signatureAlgorithm, signatureKeyId, signature);
        var normalizedAlgorithm = signatureAlgorithm!.Trim();
        var normalizedKeyId = signatureKeyId!.Trim();
        var normalizedSignature = signature!.Trim();

        var hash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(payloadSha256) &&
            !hash.Equals(payloadSha256.Trim().ToLowerInvariant(), StringComparison.Ordinal))
        {
            throw new InvalidDataException("Signed package payload hash does not match the header.");
        }

        var key = LoadTrustedKeys()
            .FirstOrDefault(key =>
                key.KeyId.Equals(normalizedKeyId, StringComparison.OrdinalIgnoreCase) &&
                key.Algorithm.Equals(normalizedAlgorithm, StringComparison.OrdinalIgnoreCase));
        if (key is null)
            throw new InvalidDataException("Signed language package key is not trusted.");
        if (!string.IsNullOrWhiteSpace(key.SoftwareId) &&
            !key.SoftwareId.Equals(softwareId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Signed language package key is not trusted for this software id.");
        }

        if (!string.IsNullOrWhiteSpace(key.PackageType) &&
            !key.PackageType.Equals(packageType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Signed language package key is not trusted for this package type.");
        }

        if (!string.IsNullOrWhiteSpace(key.PayloadFormat) &&
            !key.PayloadFormat.Equals(payloadFormat, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Signed language package key is not trusted for this payload format.");
        }

        if (!VerifySignature(key, normalizedAlgorithm, normalizedSignature, BuildSigningInput(
                softwareId,
                packageType,
                payloadFormat,
                hash,
                normalizedAlgorithm,
                normalizedKeyId)))
        {
            throw new InvalidDataException("Signed language package signature verification failed.");
        }
    }

    public static byte[] BuildSigningInput(
        string softwareId,
        string packageType,
        string payloadFormat,
        string payloadSha256,
        string signatureAlgorithm,
        string signatureKeyId)
    {
        var canonical =
            "SYSCALC-LNGPDK-SIGNATURE-V1\n" +
            "softwareId=" + Normalize(softwareId) + "\n" +
            "packageType=" + Normalize(packageType) + "\n" +
            "payloadFormat=" + Normalize(payloadFormat) + "\n" +
            "payloadSha256=" + Normalize(payloadSha256).ToLowerInvariant() + "\n" +
            "signatureAlgorithm=" + Normalize(signatureAlgorithm).ToLowerInvariant() + "\n" +
            "signatureKeyId=" + Normalize(signatureKeyId) + "\n";
        return Encoding.UTF8.GetBytes(canonical);
    }

    public static string GetTrustedPublicKeySha256(string? signatureKeyId, string? signatureAlgorithm)
    {
        if (string.IsNullOrWhiteSpace(signatureKeyId) || string.IsNullOrWhiteSpace(signatureAlgorithm))
            return "";

        var normalizedKeyId = signatureKeyId.Trim();
        var normalizedAlgorithm = signatureAlgorithm.Trim();
        var key = LoadTrustedKeys()
            .FirstOrDefault(key =>
                key.KeyId.Equals(normalizedKeyId, StringComparison.OrdinalIgnoreCase) &&
                key.Algorithm.Equals(normalizedAlgorithm, StringComparison.OrdinalIgnoreCase));
        if (key is null)
            return "";

        using var rsa = RSA.Create();
        rsa.ImportFromPem(key.PublicKeyPem);
        return Convert.ToHexString(SHA256.HashData(rsa.ExportSubjectPublicKeyInfo())).ToLowerInvariant();
    }

    private static bool VerifySignature(TrustedLanguagePackageKey key, string algorithm, string signature, byte[] signingInput)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(key.PublicKeyPem);
        var signatureBytes = Convert.FromBase64String(signature);
        var padding = algorithm.Equals("rsa-pss-sha256", StringComparison.OrdinalIgnoreCase)
            ? RSASignaturePadding.Pss
            : RSASignaturePadding.Pkcs1;
        return rsa.VerifyData(signingInput, signatureBytes, HashAlgorithmName.SHA256, padding);
    }

    private static void ValidateSignatureMetadata(string? algorithm, string? keyId, string? signature)
    {
        if (string.IsNullOrWhiteSpace(algorithm) || !KnownAlgorithms.Contains(algorithm.Trim()))
            throw new InvalidDataException("Signed language package uses an unsupported signature algorithm.");
        if (string.IsNullOrWhiteSpace(keyId) || !IsSafeKeyId(keyId))
            throw new InvalidDataException("Signed language package key id is invalid.");
        if (string.IsNullOrWhiteSpace(signature))
            throw new InvalidDataException("Signed language package signature is missing.");

        try
        {
            _ = Convert.FromBase64String(signature.Trim());
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException("Signed language package signature is not valid base64.", ex);
        }
    }

    private static IReadOnlyList<TrustedLanguagePackageKey> LoadTrustedKeys()
    {
        var paths = FindTrustedKeysFiles();
        if (paths.Count == 0)
            return [];

        var keys = new List<TrustedLanguagePackageKey>();
        foreach (var path in paths)
        {
            var store = JsonSerializer.Deserialize<TrustedLanguagePackageKeyStore>(
                File.ReadAllText(path, Encoding.UTF8),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (store?.Keys is { Count: > 0 })
                keys.AddRange(store.Keys);
        }

        return keys;
    }

    private static IReadOnlyList<string> FindTrustedKeysFiles()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, TrustedKeysFileName),
            Path.Combine(Environment.CurrentDirectory, TrustedKeysFileName),
            Path.Combine(Environment.CurrentDirectory, "src", "Tiedragon.LanguagePackage", TrustedKeysFileName),
        };

        return candidates
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string Normalize(string value) =>
        value.Trim().Replace("\r", "", StringComparison.Ordinal).Replace("\n", "", StringComparison.Ordinal);

    private static bool IsSafeKeyId(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length is > 0 and <= 96 &&
            trimmed.All(character =>
                char.IsLetterOrDigit(character) ||
                character is '.' or '-' or '_' or ':');
    }

    private sealed class TrustedLanguagePackageKeyStore
    {
        public List<TrustedLanguagePackageKey> Keys { get; set; } = [];
    }

    private sealed class TrustedLanguagePackageKey
    {
        public string KeyId { get; set; } = "";
        public string Algorithm { get; set; } = "";
        public string SoftwareId { get; set; } = "";
        public string PackageType { get; set; } = "";
        public string PayloadFormat { get; set; } = "";
        public string PublicKeyPem { get; set; } = "";
    }
}
