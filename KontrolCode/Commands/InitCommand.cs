namespace KodeControl.Commands;

using KodeControl.Core;

public class InitCommand : ICommand
{
    public string Name => "init";
    public string Description => "Create an empty KodeControl repository";

    public int Execute(string[] args)
    {
        var path = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        var fullPath = Path.GetFullPath(path);

        try
        {
            Repository.Create(fullPath);
            Console.WriteLine($"Initialized empty KodeControl repository in {fullPath}/.kodecontrol/");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
