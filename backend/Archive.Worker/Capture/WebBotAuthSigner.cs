using System.Security.Cryptography;
using System.Text;
using NSec.Cryptography;

namespace Archive.Worker.Capture;

public sealed class WebBotAuthSigner : IDisposable
{
    private const string Label = "sig1";
    private static readonly SignatureAlgorithm Algorithm = SignatureAlgorithm.Ed25519;
    private readonly WebBotAuthOptions _options;
    private readonly Key? _key;

    public WebBotAuthSigner(WebBotAuthOptions options)
    {
        _options = options;
        if (!options.Enabled)
            return;

        byte[]? keyBytes = null;
        try
        {
            keyBytes = File.ReadAllBytes(options.PrivateKeyPath!);
            _key = Key.Import(Algorithm, keyBytes, KeyBlobFormat.PkixPrivateKeyText);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException)
        {
            throw new InvalidOperationException(
                "Der Ed25519-Schlüssel für Web Bot Auth konnte nicht geladen werden.", exception);
        }
        finally
        {
            if (keyBytes is not null)
                CryptographicOperations.ZeroMemory(keyBytes);
        }

        var publicKey = _key.PublicKey.Export(KeyBlobFormat.RawPublicKey);
        PublicKeyX = Base64Url(publicKey);
        var canonicalJwk = Encoding.ASCII.GetBytes(
            $"{{\"crv\":\"Ed25519\",\"kty\":\"OKP\",\"x\":\"{PublicKeyX}\"}}");
        KeyId = Base64Url(SHA256.HashData(canonicalJwk));
    }

    public bool Enabled => _options.Enabled;
    public string? PublicKeyX { get; }
    public string? KeyId { get; }

    public IReadOnlyDictionary<string, string> CreateHeaders(Uri target)
    {
        if (!_options.Enabled)
            return new Dictionary<string, string>();
        if (target.Scheme != Uri.UriSchemeHttp && target.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Web Bot Auth kann nur HTTP(S)-Anfragen signieren.");

        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expires = created + (long)_options.SignatureLifetime.TotalSeconds;
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var signatureAgent = $"\"{_options.SignatureAgent!.GetLeftPart(UriPartial.Authority)}\"";
        var signatureParams = $"(\"@authority\" \"signature-agent\");created={created};" +
            $"keyid=\"{KeyId}\";alg=\"ed25519\";expires={expires};nonce=\"{nonce}\";tag=\"web-bot-auth\"";
        var authority = target.GetComponents(UriComponents.HostAndPort, UriFormat.UriEscaped);
        var signatureBase = $"\"@authority\": {authority}\n" +
            $"\"signature-agent\": {signatureAgent}\n" +
            $"\"@signature-params\": {signatureParams}";
        var signature = Algorithm.Sign(_key!, Encoding.ASCII.GetBytes(signatureBase));

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Signature-Agent"] = signatureAgent,
            ["Signature-Input"] = $"{Label}={signatureParams}",
            ["Signature"] = $"{Label}=:{Convert.ToBase64String(signature)}:"
        };
    }

    public void Dispose() => _key?.Dispose();

    private static string Base64Url(ReadOnlySpan<byte> value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
