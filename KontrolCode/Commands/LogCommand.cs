namespace KodeControl.Commands;

using KodeControl.Core;
using KodeControl.Core.Models;

public class LogCommand : ICommand
{
    public string Name => "log";
    public string Description => "Show commit logs";

    public int Execute(string[] args)
    {
        var showAll = false;
        var oneline = false;
        string? startRef = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--all":
                    showAll = true;
                    break;
                case "--oneline":
                    oneline = true;
                    break;
                default:
                    if (!args[i].StartsWith("-"))
                    {
                        startRef = args[i];
                    }
                    break;
            }
        }

        try
        {
            var repo = Repository.Open(Directory.GetCurrentDirectory());

            if (showAll)
            {
                var branches = repo.RefStore.GetAllBranches();
                var seen = new HashSet<string>();

                foreach (var (branch, hash) in branches)
                {
                    if (seen.Contains(hash))
                        continue;

                    foreach (var (commit, commitHash) in repo.LogWithHash(hash))
                    {
                        if (seen.Add(commitHash))
                        {
                            PrintCommit(commit, commitHash, oneline);
                        }
                    }
                }

                return 0;
            }

            foreach (var (commit, commitHash) in repo.LogWithHash(startRef))
            {
                PrintCommit(commit, commitHash, oneline);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static void PrintCommit(Commit commit, string hash, bool oneline)
    {
        if (oneline)
        {
            var shortHash = hash[..7];
            var firstLine = commit.Message.Split('\n')[0];
            Console.WriteLine($"{shortHash} {firstLine}");
        }
        else
        {
            Console.WriteLine($"commit {hash}");
            Console.WriteLine($"Author: {commit.Author.Name} <{commit.Author.Email}>");
            Console.WriteLine($"Date:   {commit.Author.When:ddd MMM dd HH:mm:ss yyyy zzz}");
            Console.WriteLine();
            Console.WriteLine($"    {commit.Message.Replace("\n", "\n    ")}");
            Console.WriteLine();
        }
    }
}
