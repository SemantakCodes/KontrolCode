namespace KontrolCode.Utils;

using System.Security.Cryptography;
using System.Text;

public static class HashHelper
{
    public static string ComputeSha1(byte[] data)
    {
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeSha1(ReadOnlySpan<byte> data)
    {
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(data.ToArray());
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeHash(string type, byte[] content)
    {
        var header = Encoding.ASCII.GetBytes($"{type} {content.Length}\0");
        var data = new byte[header.Length + content.Length];
        header.CopyTo(data, 0);
        content.CopyTo(data.AsSpan(header.Length));
        return ComputeSha1(data);
    }
}
