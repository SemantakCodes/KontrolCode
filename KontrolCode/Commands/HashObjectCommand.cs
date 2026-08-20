namespace KontrolCode.Commands;

using KontrolCode.Core;
using KontrolCode.Core.Models;

public class HashObjectCommand : ICommand
{
    public string Name => "hash-object";
    public string Description => "Compute object ID and optionally create a blob from a file";

    public int Execute(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: kontrolcode hash-object <file> [-w]");
            return 1;
        }

        var writeToStore = false;
        var filePath = "";

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-w")
            {
                writeToStore = true;
            }
            else if (!args[i].StartsWith("-"))
            {
                filePath = args[i];
            }
        }

        if (string.IsNullOrEmpty(filePath))
        {
            Console.Error.WriteLine("Error: No file specified");
            return 1;
        }

        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"Error: File not found: {filePath}");
            return 1;
        }

        try
        {
            var blob = Blob.FromFile(filePath);
            var hash = blob.Hash;

            if (writeToStore)
            {
                var repo = Repository.Open(Directory.GetCurrentDirectory());
                repo.HashObject(filePath);
            }

            Console.WriteLine(hash);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
