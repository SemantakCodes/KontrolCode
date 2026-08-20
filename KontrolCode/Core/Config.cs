namespace KontrolCode.Core;

public class Config
{
    private readonly string _configPath;
    private string _userName = "KontrolCode User";
    private string _userEmail = "user@kontrolcode.local";

    public string UserName
    {
        get => _userName;
        set => _userName = value;
    }

    public string UserEmail
    {
        get => _userEmail;
        set => _userEmail = value;
    }

    public Config(string repoPath)
    {
        _configPath = Path.Combine(repoPath, ".kontrolcode", "config");
    }

    public static Config Load(string repoPath)
    {
        var config = new Config(repoPath);
        if (File.Exists(config._configPath))
        {
            config.Parse(File.ReadAllText(config._configPath));
        }
        return config;
    }

    public void Save()
    {
        var lines = new List<string>
        {
            "[user]",
            $"    name = {_userName}",
            $"    email = {_userEmail}",
            ""
        };
        File.WriteAllLines(_configPath, lines);
    }

    private void Parse(string content)
    {
        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("name ="))
            {
                _userName = trimmed[6..].Trim();
            }
            else if (trimmed.StartsWith("email ="))
            {
                _userEmail = trimmed[7..].Trim();
            }
        }
    }
}
