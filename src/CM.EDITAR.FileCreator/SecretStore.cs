using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using CM.EDITAR.Core;

namespace CM.EDITAR.FileCreator;

/// <summary>
/// Per-install secret used to authenticate FileCreator IPC. On Windows the secret is encrypted with
/// DPAPI (CurrentUser scope). On non-Windows hosts a chmod-600 file is used so the project still compiles.
/// </summary>
public sealed class SecretStore : ISecretStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SecretStore(string? path = null)
    {
        _path = path ?? Path.Combine(AppPaths.SecretsDir, "filecreator.secret");
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
    }

    public async Task<string> GetOrCreateTokenAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (File.Exists(_path))
            {
                var bytes = await File.ReadAllBytesAsync(_path, ct).ConfigureAwait(false);
                return DecodeToken(bytes);
            }
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            await File.WriteAllBytesAsync(_path, EncodeToken(token), ct).ConfigureAwait(false);
            return token;
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> ValidateAsync(string presented, CancellationToken ct = default)
    {
        var expected = await GetOrCreateTokenAsync(ct).ConfigureAwait(false);
        // Constant-time compare
        var a = Encoding.UTF8.GetBytes(expected);
        var b = Encoding.UTF8.GetBytes(presented ?? "");
        return CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static byte[] EncodeToken(string token)
    {
        var raw = Encoding.UTF8.GetBytes(token);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ProtectWithDpapi(raw);
        return raw;
    }

    private static string DecodeToken(byte[] stored)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Encoding.UTF8.GetString(UnprotectWithDpapi(stored));
        return Encoding.UTF8.GetString(stored);
    }

    [SupportedOSPlatform("windows")]
    private static byte[] ProtectWithDpapi(byte[] raw) =>
        ProtectedData.Protect(raw, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);

    [SupportedOSPlatform("windows")]
    private static byte[] UnprotectWithDpapi(byte[] enc) =>
        ProtectedData.Unprotect(enc, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
}
