namespace KodeControl.Core.Models;

using System.Text;
using KodeControl.Utils;
using KodeControl.Core.Storage;

public abstract record GitObject
{
    public abstract string Type { get; }
    public abstract byte[] RawContent { get; }
    public virtual string Hash => HashHelper.ComputeSha1(RawContent);

    public abstract byte[] Serialize();

    protected static byte[] CreateRawContent(string type, byte[] content)
    {
        var header = Encoding.ASCII.GetBytes($"{type} {content.Length}\0");
        var result = new byte[header.Length + content.Length];
        header.CopyTo(result, 0);
        Array.Copy(content, 0, result, header.Length, content.Length);
        return result;
    }
}
