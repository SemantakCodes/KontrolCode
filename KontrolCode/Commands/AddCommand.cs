namespace KodeControl.Commands;

using KodeControl.Core;

public class AddCommand : ICommand
{
    public string Name => "add";
    public string Description => "Add file contents to the index";

    public int Execute(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: mygit add <file>...");
            return 1;
        }

        try
        {
            var repo = Repository.Open(Directory.GetCurrentDirectory());

            foreach (var file in args)
            {
                if (!File.Exists(file))
                {
                    Console.Error.WriteLine($"Error: File not found: {file}");
                    return 1;
                }

                repo.Add(file);
                Console.WriteLine($"Added {file}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
