namespace KontrolCode.Core.Storage;

using System.IO.Compression;
using System.Text;
using KontrolCode.Utils;
using KontrolCode.Core.Models;

public static class ObjectSerializer
{
    public static byte[] SerializeBlob(ReadOnlySpan<byte> content)
    {
        var header = Encoding.ASCII.GetBytes($"blob {content.Length}\0");
        var result = new byte[header.Length + content.Length];
        header.CopyTo(result, 0);
        content.CopyTo(result.AsSpan(header.Length));
        return result;
    }

    public static byte[] SerializeTree(IReadOnlyList<TreeEntry> entries)
    {
        using var ms = new MemoryStream();
        foreach (var entry in entries)
        {
            var modeBytes = Encoding.ASCII.GetBytes(entry.Mode);
            var nameBytes = Encoding.ASCII.GetBytes(entry.Name);
            var hashBytes = Convert.FromHexString(entry.Hash);

            ms.Write(modeBytes);
            ms.WriteByte((byte)' ');
            ms.Write(nameBytes);
            ms.WriteByte(0);
            ms.Write(hashBytes);
        }

        var content = ms.ToArray();
        var header = Encoding.ASCII.GetBytes($"tree {content.Length}\0");
        var result = new byte[header.Length + content.Length];
        header.CopyTo(result, 0);
        Array.Copy(content, 0, result, header.Length, content.Length);
        return result;
    }

    public static byte[] SerializeCommit(Commit commit)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"tree {commit.TreeHash}");
        if (!string.IsNullOrEmpty(commit.ParentHash))
        {
            sb.AppendLine($"parent {commit.ParentHash}");
        }
        var author = commit.Author;
        var when = author.When.ToUnixTimeSeconds();
        var timezone = author.When.ToString("zzz").Replace(":", "");
        sb.AppendLine($"author {author.Name} <{author.Email}> {when} {timezone}");
        sb.AppendLine($"committer {author.Name} <{author.Email}> {when} {timezone}");
        sb.AppendLine();
        sb.Append(commit.Message);

        var content = Encoding.ASCII.GetBytes(sb.ToString());
        var header = Encoding.ASCII.GetBytes($"commit {content.Length}\0");
        var result = new byte[header.Length + content.Length];
        header.CopyTo(result, 0);
        Array.Copy(content, 0, result, header.Length, content.Length);
        return result;
    }

    public static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        output.WriteByte(0x78);
        output.WriteByte(0x9C);

        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(data);
        }

        var adler = ComputeAdler32(data);
        output.WriteByte((byte)(adler >> 24));
        output.WriteByte((byte)(adler >> 16));
        output.WriteByte((byte)(adler >> 8));
        output.WriteByte((byte)adler);

        return output.ToArray();
    }

    public static byte[] Decompress(byte[] data)
    {
        if (data.Length < 6 || data[0] != 0x78 || data[1] != 0x9C)
            throw new InvalidDataException("Invalid zlib header");

        using var input = new MemoryStream(data, 2, data.Length - 6);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }

    private static uint ComputeAdler32(byte[] data)
    {
        const uint MOD = 65521;
        uint a = 1, b = 0;
        foreach (var byteVal in data)
        {
            a = (a + byteVal) % MOD;
            b = (b + a) % MOD;
        }
        return (b << 16) | a;
    }

    public static (string Hash, byte[] Compressed) WriteObject(string objectsDir, string type, byte[] content)
    {
        var hash = HashHelper.ComputeSha1(content);
        var compressed = Compress(content);

        var dir = Path.Combine(objectsDir, hash[..2]);
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, hash[2..]);

        File.WriteAllBytes(filePath, compressed);

        return (hash, compressed);
    }
}
