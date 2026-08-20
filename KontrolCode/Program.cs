using KontrolCode.Commands;

var commands = new List<ICommand>
{
    new InitCommand(),
    new HashObjectCommand(),
    new AddCommand(),
    new CommitCommand(),
    new LogCommand(),
    new BranchCommand(),
    new CheckoutCommand()
};

if (args.Length == 0)
{
    PrintUsage(commands);
    return 1;
}

var commandName = args[0];
var command = commands.FirstOrDefault(c => c.Name == commandName);

if (command == null)
{
    Console.Error.WriteLine($"Error: Unknown command '{commandName}'");
    Console.Error.WriteLine();
    PrintUsage(commands);
    return 1;
}

var commandArgs = args.Skip(1).ToArray();
return command.Execute(commandArgs);

static void PrintUsage(List<ICommand> commands)
    {
        Console.WriteLine("Usage: kontrolcode <command> [args...]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        foreach (var cmd in commands.OrderBy(c => c.Name))
        {
            Console.WriteLine($"  {cmd.Name,-12} {cmd.Description}");
        }
    }
