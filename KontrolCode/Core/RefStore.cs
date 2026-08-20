namespace KontrolCode.Core;

public class RefStore
{
    private readonly string _repoPath;

    public string RepoPath => _repoPath;

    public string Head
    {
        get
        {
            var headPath = Path.Combine(_repoPath, ".kontrolcode", "HEAD");
            if (!File.Exists(headPath))
            {
                return "ref: refs/heads/main";
            }
            return File.ReadAllText(headPath).Trim();
        }
        private set
        {
            var headPath = Path.Combine(_repoPath, ".kontrolcode", "HEAD");
            File.WriteAllText(headPath, value);
        }
    }

    public RefStore(string repoPath)
    {
        _repoPath = repoPath;
        var refsDir = Path.Combine(_repoPath, ".kontrolcode", "refs");
        Directory.CreateDirectory(Path.Combine(refsDir, "heads"));
        Directory.CreateDirectory(Path.Combine(refsDir, "tags"));
    }

    public string? GetHeadCommitHash()
    {
        var head = Head;
        if (head.StartsWith("ref: "))
        {
            var refPath = head[5..];
            return GetRef(refPath);
        }
        return head;
    }

    public void SetHead(string refOrHash)
    {
        if (refOrHash.StartsWith("ref: ") || refOrHash.StartsWith("refs/"))
        {
            Head = refOrHash.StartsWith("ref: ") ? refOrHash : $"ref: {refOrHash}";
        }
        else
        {
            Head = refOrHash;
        }
    }

    public string? GetBranch(string name)
    {
        var branchPath = Path.Combine(_repoPath, ".kontrolcode", "refs", "heads", name);
        if (!File.Exists(branchPath))
        {
            return null;
        }
        return File.ReadAllText(branchPath).Trim();
    }

    public void SetBranch(string name, string hash)
    {
        var branchPath = Path.Combine(_repoPath, ".kontrolcode", "refs", "heads", name);
        File.WriteAllText(branchPath, hash);
    }

    public Dictionary<string, string> GetAllBranches()
    {
        var branches = new Dictionary<string, string>();
        var headsDir = Path.Combine(_repoPath, ".kontrolcode", "refs", "heads");
        if (!Directory.Exists(headsDir))
        {
            return branches;
        }
        foreach (var file in Directory.GetFiles(headsDir))
        {
            var name = Path.GetFileName(file);
            var hash = File.ReadAllText(file).Trim();
            branches[name] = hash;
        }
        return branches;
    }

    public void CreateTag(string name, string hash)
    {
        var tagPath = Path.Combine(_repoPath, ".kontrolcode", "refs", "tags", name);
        File.WriteAllText(tagPath, hash);
    }

    public string? GetTag(string name)
    {
        var tagPath = Path.Combine(_repoPath, ".kontrolcode", "refs", "tags", name);
        if (!File.Exists(tagPath))
        {
            return null;
        }
        return File.ReadAllText(tagPath).Trim();
    }

    private string? GetRef(string refPath)
    {
        var fullPath = Path.Combine(_repoPath, ".kontrolcode", refPath);
        if (!File.Exists(fullPath))
        {
            return null;
        }
        return File.ReadAllText(fullPath).Trim();
    }
}
