namespace KodeControl.Core.Models;

using KodeControl.Core.Storage;

public record Commit(string TreeHash, string? ParentHash, Author Author, string Message) : GitObject
{
    public override string Type => "commit";

    public override byte[] RawContent => ObjectSerializer.SerializeCommit(this);

    public override byte[] Serialize() => RawContent;
}
