namespace KontrolCode.Commands;

using KontrolCode.Core;

public class CommitCommand : ICommand
{
    public string Name => "commit";
    public string Description => "Record changes to the repository";

    public int Execute(string[] args)
    {
        string? message = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-m" && i + 1 < args.Length)
            {
                message = args[i + 1];
                i++;
            }
        }

        if (string.IsNullOrEmpty(message))
        {
            Console.Error.WriteLine("Usage: kontrolcode commit -m <message>");
            Console.Error.WriteLine("Error: Commit message required (-m)");
            return 1;
        }

        try
        {
            var repo = Repository.Open(Directory.GetCurrentDirectory());
            var commitHash = repo.Commit(message);

            var shortHash = commitHash[..7];
            var firstLine = message.Split('\n')[0];
            Console.WriteLine($"[{shortHash}] {firstLine}");
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
