namespace KontrolCode.Core.Models;

using System.Collections.Generic;
using KontrolCode.Core.Storage;

public record Tree(IReadOnlyList<TreeEntry> Entries) : GitObject
{
    public override string Type => "tree";

    public override byte[] RawContent => ObjectSerializer.SerializeTree(Entries);

    public override byte[] Serialize() => RawContent;

    public static Tree BuildFromIndex(IEnumerable<IndexEntry> entries)
    {
        var treeEntries = new List<TreeEntry>();
        foreach (var entry in entries)
        {
            treeEntries.Add(new TreeEntry(entry.Mode, entry.Name, entry.Hash));
        }
        return new Tree(treeEntries);
    }
}
