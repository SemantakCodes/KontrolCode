namespace KontrolCode.Core;

using System.Text;
using KontrolCode.Core.Models;
using KontrolCode.Core.Storage;
using KontrolCode.Utils;

public class ObjectStore
{
    public string ObjectsDir { get; }

    public ObjectStore(string repoPath)
    {
        ObjectsDir = Path.Combine(repoPath, ".kontrolcode", "objects");
        Directory.CreateDirectory(ObjectsDir);
    }

    public string Write<T>(T obj) where T : GitObject
    {
        var (hash, _) = ObjectSerializer.WriteObject(ObjectsDir, obj.Type, obj.RawContent);
        return hash;
    }

    public GitObject Read(string hash)
    {
        if (!TryRead(hash, out var obj) || obj is null)
        {
            throw new FileNotFoundException($"Object not found: {hash}");
        }
        return obj;
    }

    public bool TryRead(string hash, out GitObject? obj)
    {
        obj = null;

        var dir = Path.Combine(ObjectsDir, hash[..2]);
        var filePath = Path.Combine(dir, hash[2..]);

        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            var compressed = File.ReadAllBytes(filePath);
            var decompressed = ObjectSerializer.Decompress(compressed);
            var (type, content) = ParseHeader(decompressed);

            obj = type switch
            {
                "blob" => new Blob(content),
                "tree" => ParseTree(content),
                "commit" => ParseCommit(content),
                _ => throw new InvalidDataException($"Unknown object type: {type}")
            };

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool Exists(string hash)
    {
        var dir = Path.Combine(ObjectsDir, hash[..2]);
        var filePath = Path.Combine(dir, hash[2..]);
        return File.Exists(filePath);
    }

    public string? FindByPrefix(string prefix)
    {
        if (prefix.Length < 2)
        {
            return null;
        }

        var dir = Path.Combine(ObjectsDir, prefix[..2]);
        if (!Directory.Exists(dir))
        {
            return null;
        }

        var restPrefix = prefix[2..];
        var files = Directory.GetFiles(dir, restPrefix + "*");
        if (files.Length == 1)
        {
            var fileName = Path.GetFileName(files[0]);
            return prefix[..2] + fileName;
        }
        else if (files.Length > 1)
        {
            throw new InvalidOperationException($"Ambiguous prefix: {prefix}");
        }
        return null;
    }

    private static (string Type, byte[] Content) ParseHeader(byte[] data)
    {
        var spaceIndex = Array.IndexOf(data, (byte)' ');
        if (spaceIndex < 0)
            throw new InvalidDataException("Invalid object header: missing space");

        var type = Encoding.ASCII.GetString(data, 0, spaceIndex);

        var nullIndex = Array.IndexOf(data, (byte)0, spaceIndex + 1);
        if (nullIndex < 0)
            throw new InvalidDataException("Invalid object header: missing null terminator");

        var lengthStr = Encoding.ASCII.GetString(data, spaceIndex + 1, nullIndex - spaceIndex - 1);
        if (!int.TryParse(lengthStr, out var length))
            throw new InvalidDataException("Invalid object header: invalid length");

        var content = new byte[length];
        Array.Copy(data, nullIndex + 1, content, 0, length);

        return (type, content);
    }

    private static Tree ParseTree(byte[] content)
    {
        var entries = new List<TreeEntry>();
        var offset = 0;

        while (offset < content.Length)
        {
            var spaceIndex = Array.IndexOf(content, (byte)' ', offset);
            if (spaceIndex < 0)
                throw new InvalidDataException("Invalid tree entry: missing space");

            var mode = Encoding.ASCII.GetString(content, offset, spaceIndex - offset);

            var nullIndex = Array.IndexOf(content, (byte)0, spaceIndex + 1);
            if (nullIndex < 0)
                throw new InvalidDataException("Invalid tree entry: missing null terminator");

            var name = Encoding.ASCII.GetString(content, spaceIndex + 1, nullIndex - spaceIndex - 1);

            if (nullIndex + 20 > content.Length)
                throw new InvalidDataException("Invalid tree entry: hash too short");

            var hashBytes = content.AsSpan(nullIndex + 1, 20);
            var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            entries.Add(new TreeEntry(mode, name, hash));
            offset = nullIndex + 21;
        }

        return new Tree(entries);
    }

    private static Commit ParseCommit(byte[] content)
    {
        var text = Encoding.ASCII.GetString(content);
        var lines = text.Split('\n');

        string treeHash = "";
        string? parentHash = null;
        Author? author = null;
        var messageLines = new List<string>();
        var inMessage = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.TrimEnd('\r');
            if (inMessage)
            {
                messageLines.Add(trimmedLine);
                continue;
            }

            if (string.IsNullOrEmpty(trimmedLine))
            {
                inMessage = true;
                continue;
            }

            if (trimmedLine.StartsWith("tree "))
            {
                treeHash = trimmedLine[5..];
            }
            else if (trimmedLine.StartsWith("parent "))
            {
                parentHash = trimmedLine[7..];
            }
            else if (trimmedLine.StartsWith("author "))
            {
                author = ParseAuthor(trimmedLine[7..]);
            }
        }

        var message = string.Join("\n", messageLines).TrimEnd();

        if (author is null)
            throw new InvalidDataException("Invalid commit: missing author");

        return new Commit(treeHash, parentHash, author, message);
    }

    private static Author ParseAuthor(string line)
    {
        var emailStart = line.IndexOf('<');
        var emailEnd = line.IndexOf('>');
        if (emailStart < 0 || emailEnd < 0)
            throw new InvalidDataException("Invalid author line: missing email");

        var name = line[..emailStart].Trim();
        var email = line[(emailStart + 1)..emailEnd];

        var timestampPart = line[(emailEnd + 1)..].Trim();
        var parts = timestampPart.Split(' ');
        if (parts.Length < 2)
            throw new InvalidDataException("Invalid author line: missing timestamp");

        var timestamp = long.Parse(parts[0]);
        // parts[1] is the timezone offset; not currently stored on Author.
        _ = parts[1];

        var when = DateTimeOffset.FromUnixTimeSeconds(timestamp);

        return new Author(name, email, when);
    }
}
