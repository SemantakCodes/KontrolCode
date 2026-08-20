namespace KontrolCode.Commands;

using KontrolCode.Core;

public class BranchCommand : ICommand
{
    public string Name => "branch";
    public string Description => "List, create, or delete branches";

    public int Execute(string[] args)
    {
        var delete = false;
        string? name = null;
        string? commit = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-d")
            {
                delete = true;
            }
            else if (!args[i].StartsWith("-"))
            {
                if (name == null)
                {
                    name = args[i];
                }
                else if (commit == null)
                {
                    commit = args[i];
                }
            }
        }

        try
        {
            var repo = Repository.Open(Directory.GetCurrentDirectory());

            if (delete)
            {
                if (string.IsNullOrEmpty(name))
                {
                    Console.Error.WriteLine("Error: Branch name required for deletion");
                    return 1;
                }
                return DeleteBranch(repo, name);
            }

            if (string.IsNullOrEmpty(name))
            {
                return ListBranches(repo);
            }

            return CreateBranch(repo, name, commit);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private int ListBranches(Repository repo)
    {
        var branches = repo.RefStore.GetAllBranches();
        var currentHead = repo.RefStore.Head;
        var currentBranch = "";

        if (currentHead.StartsWith("ref: refs/heads/"))
        {
            currentBranch = currentHead["ref: refs/heads/".Length..];
        }

        foreach (var (name, hash) in branches.OrderBy(b => b.Key))
        {
            var marker = name == currentBranch ? "* " : "  ";
            var shortHash = hash[..7];
            Console.WriteLine($"{marker}{name} {shortHash}");
        }

        return 0;
    }

    private int CreateBranch(Repository repo, string name, string? commit)
    {
        var targetHash = commit ?? repo.RefStore.GetHeadCommitHash();

        if (targetHash == null)
        {
            Console.Error.WriteLine("Error: No commit to base branch on");
            return 1;
        }

        if (repo.RefStore.GetBranch(name) != null)
        {
            Console.Error.WriteLine($"Error: Branch '{name}' already exists");
            return 1;
        }

        repo.RefStore.SetBranch(name, targetHash);
        return 0;
    }

    private int DeleteBranch(Repository repo, string name)
    {
        var branchPath = Path.Combine(repo.RepoPath, ".kontrolcode", "refs", "heads", name);

        if (!File.Exists(branchPath))
        {
            Console.Error.WriteLine($"Error: Branch '{name}' not found");
            return 1;
        }

        var currentHead = repo.RefStore.Head;
        if (currentHead == $"ref: refs/heads/{name}")
        {
            Console.Error.WriteLine($"Error: Cannot delete current branch '{name}'");
            return 1;
        }

        File.Delete(branchPath);
        Console.WriteLine($"Deleted branch {name}");
        return 0;
    }
}
