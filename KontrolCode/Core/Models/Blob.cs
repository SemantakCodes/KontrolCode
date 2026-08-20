namespace KodeControl.Core.Models;

using KodeControl.Core.Storage;

public record Blob(byte[] Content) : GitObject
{
    public override string Type => "blob";

    public override byte[] RawContent => ObjectSerializer.SerializeBlob(Content);

    public override byte[] Serialize() => RawContent;

    public static Blob FromFile(string path)
    {
        var content = File.ReadAllBytes(path);
        return new Blob(content);
    }
}
