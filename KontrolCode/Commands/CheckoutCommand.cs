namespace KodeControl.Commands;

using KodeControl.Core;

public class CheckoutCommand : ICommand
{
    public string Name => "checkout";
    public string Description => "Switch branches or restore working tree files";

    public int Execute(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: mygit checkout <branch|commit>");
            return 1;
        }

        var target = args[0];

        try
        {
            var repo = Repository.Open(Directory.GetCurrentDirectory());

            var branchHash = repo.RefStore.GetBranch(target);
            if (branchHash != null)
            {
                repo.RefStore.SetHead($"ref: refs/heads/{target}");
                Console.WriteLine($"Switched to branch '{target}'");
                return 0;
            }

            string? commitHash = target;
            if (target.Length < 40)
            {
                commitHash = repo.ObjectStore.FindByPrefix(target);
            }

            if (commitHash != null && repo.ObjectStore.Exists(commitHash))
            {
                repo.RefStore.SetHead(commitHash);
                var shortHash = commitHash[..7];
                Console.WriteLine($"HEAD detached at {shortHash}");
                return 0;
            }

            Console.Error.WriteLine($"Error: Unknown branch or commit: {target}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
