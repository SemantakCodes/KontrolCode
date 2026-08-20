namespace KontrolCode.Core;

using System.Collections.Generic;
using KontrolCode.Core.Models;

public class Repository
{
    public string RepoPath { get; }
    public ObjectStore ObjectStore { get; }
    public Index Index { get; }
    public Config Config { get; }
    public RefStore RefStore { get; }

    private Repository(string repoPath)
    {
        RepoPath = repoPath;
        ObjectStore = new ObjectStore(repoPath);
        Index = Index.Load(repoPath);
        Config = Config.Load(repoPath);
        RefStore = new RefStore(repoPath);
    }

    public static Repository Create(string repoPath)
    {
        var kontrolcodeDir = Path.Combine(repoPath, ".kontrolcode");
        var objectsDir = Path.Combine(kontrolcodeDir, "objects");
        var refsDir = Path.Combine(kontrolcodeDir, "refs");
        var headsDir = Path.Combine(refsDir, "heads");
        var tagsDir = Path.Combine(refsDir, "tags");

        Directory.CreateDirectory(objectsDir);
        Directory.CreateDirectory(headsDir);
        Directory.CreateDirectory(tagsDir);

        var headPath = Path.Combine(kontrolcodeDir, "HEAD");
        File.WriteAllText(headPath, "ref: refs/heads/main");

        var config = new Config(repoPath);
        config.Save();

        var index = new Index(repoPath);
        index.Save();

        return new Repository(repoPath);
    }

    public static Repository Open(string repoPath)
    {
        var kontrolcodeDir = Path.Combine(repoPath, ".kontrolcode");
        if (!Directory.Exists(kontrolcodeDir))
        {
            throw new DirectoryNotFoundException($"Not a KontrolCode repository: {repoPath}");
        }
        return new Repository(repoPath);
    }

    public string HashObject(string filePath)
    {
        var blob = Blob.FromFile(filePath);
        return ObjectStore.Write(blob);
    }

    public void Add(string filePath)
    {
        var blobHash = HashObject(filePath);
        var relativePath = Path.GetRelativePath(RepoPath, filePath);
        Index.Add(relativePath, blobHash);
        Index.Save();
    }

    public string Commit(string message)
    {
        var entries = Index.GetAll();
        if (entries.Count == 0)
        {
            throw new InvalidOperationException("Nothing to commit, index is empty");
        }

        var tree = Tree.BuildFromIndex(entries);
        var treeHash = ObjectStore.Write(tree);

        var parentHash = RefStore.GetHeadCommitHash();
        var author = new Author(Config.UserName, Config.UserEmail, DateTimeOffset.UtcNow);
        var commit = new Commit(treeHash, parentHash, author, message);
        var commitHash = ObjectStore.Write(commit);

        var head = RefStore.Head;
        if (head.StartsWith("ref: "))
        {
            var branchName = head[5..].Replace("refs/heads/", "");
            RefStore.SetBranch(branchName, commitHash);
        }
        else
        {
            RefStore.SetHead(commitHash);
        }

        Index.Clear();
        Index.Save();

        return commitHash;
    }

    public IEnumerable<Commit> Log(string? startRef = null)
    {
        foreach (var (commit, _) in LogWithHash(startRef))
        {
            yield return commit;
        }
    }

    public IEnumerable<(Commit Commit, string Hash)> LogWithHash(string? startRef = null)
    {
        var currentHash = startRef ?? RefStore.GetHeadCommitHash();
        while (currentHash != null)
        {
            var commit = ObjectStore.Read(currentHash) as Commit;
            if (commit == null)
            {
                yield break;
            }
            yield return (commit, currentHash);
            currentHash = commit.ParentHash;
        }
    }
}
