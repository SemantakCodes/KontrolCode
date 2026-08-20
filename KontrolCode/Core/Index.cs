namespace KodeControl.Core;

using System.Text.Json;
using KodeControl.Core.Models;

public class Index
{
    private readonly string _indexPath;
    private List<IndexEntry> _entries = [];

    public Index(string repoPath)
    {
        _indexPath = Path.Combine(repoPath, ".kodecontrol", "index");
    }

    public static Index Load(string repoPath)
    {
        var index = new Index(repoPath);
        if (File.Exists(index._indexPath))
        {
            var json = File.ReadAllText(index._indexPath);
            var entries = JsonSerializer.Deserialize<List<IndexEntry>>(json);
            index._entries = entries ?? [];
        }
        return index;
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_indexPath, json);
    }

    public void Add(string path, string blobHash, string mode = "100644")
    {
        var normalizedPath = NormalizePath(path);
        var existingIndex = _entries.FindIndex(e => e.Name == normalizedPath);
        var entry = new IndexEntry(mode, normalizedPath, blobHash);
        if (existingIndex >= 0)
        {
            _entries[existingIndex] = entry;
        }
        else
        {
            _entries.Add(entry);
        }
        _entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
    }

    public void Remove(string path)
    {
        var normalizedPath = NormalizePath(path);
        _entries.RemoveAll(e => e.Name == normalizedPath);
    }

    public void Clear() => _entries.Clear();

    public IReadOnlyList<IndexEntry> GetAll() => _entries.AsReadOnly();

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('.', '/');
    }
}
