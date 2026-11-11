using System.Text;
using Api.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Api.Services;

public sealed record GeneratedLanApiKey(string Plaintext, string Salt, string Hash);

public interface ISecurityTokenService
{
    bool HasLanApiKey { get; }

    bool IsControlToken(string supplied);

    bool IsLanApiKey(string supplied);

    GeneratedLanApiKey GenerateLanApiKey();

    void SetLanKey(string salt, string hash);
}

internal sealed class SecurityTokenService : ISecurityTokenService
{
    private const int DerivedKeySize = 32;
    private const int Iterations = 150_000;
    private readonly byte[] _controlToken;
    private readonly object _gate = new();
    private byte[]? _lanSalt;
    private byte[]? _lanHash;

    public SecurityTokenService(IOptions<SecurityOptions> options)
    {
        _controlToken = Encoding.UTF8.GetBytes(options.Value.ControlToken);
        SetLanKey(options.Value.LanApiKeySalt, options.Value.LanApiKeyHash);
    }

    public bool HasLanApiKey
    {
        get
        {
            lock (_gate)
                return _lanSalt is not null && _lanHash is not null;
        }
    }

    public bool IsControlToken(string supplied) => FixedTimeEquals(_controlToken, Encoding.UTF8.GetBytes(supplied));

    public bool IsLanApiKey(string supplied)
    {
        byte[]? salt;
        byte[]? expected;
        lock (_gate)
        {
            salt = _lanSalt;
            expected = _lanHash;
        }

        if (salt is null || expected is null || string.IsNullOrWhiteSpace(supplied))
            return false;

        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(supplied, salt, Iterations, HashAlgorithmName.SHA256, DerivedKeySize);
        return FixedTimeEquals(expected, actual);
    }

    public GeneratedLanApiKey GenerateLanApiKey()
    {
        (string plaintext, string salt, string hash) = GenerateToken();
        return new GeneratedLanApiKey(plaintext, salt, hash);
    }

    public void SetLanKey(string salt, string hash)
    {
        lock (_gate)
        {
            _lanSalt = TryDecode(salt);
            _lanHash = TryDecode(hash);
        }
    }

    private static (string Plaintext, string Salt, string Hash) GenerateToken()
    {
        string plaintext = ToBase64Url(RandomNumberGenerator.GetBytes(32));
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(plaintext, salt, Iterations, HashAlgorithmName.SHA256, DerivedKeySize);
        return (plaintext, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    private static bool FixedTimeEquals(byte[] expected, byte[] actual) =>
        expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);

    private static byte[]? TryDecode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string ToBase64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
