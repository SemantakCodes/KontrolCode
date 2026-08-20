# KontrolCode

A Git-like version control system implemented in C# on .NET 8.

`KontrolCode` is a from-scratch implementation of a subset of Git: it stores content-addressed objects in a `.kontrolcode/` directory, tracks branches under `refs/heads`, persists an index, and exposes a small command-line interface that mirrors Git's most common commands.

---

## Features

- `init` — Initialize a new repository in the current directory
- `hash-object` — Compute the SHA-1 object ID of a file (optionally write it to the object store)
- `add` — Stage one or more files into the index
- `commit` — Record staged changes as a new commit
- `log` — Walk the commit history from `HEAD` (or any ref/commit)
- `branch` — List, create, or delete branches
- `checkout` — Switch `HEAD` to an existing branch or detach it at a commit

---

## Requirements

- .NET 8 SDK

## Building

From the `KontrolCode/` directory:

```bash
dotnet build
```

The compiled binary lands at `KontrolCode/bin/Debug/net8.0/KontrolCode` (or `KontrolCode.exe` on Windows).

## Running

From the `KontrolCode/` directory:

```bash
# Run via dotnet
dotnet run -- <command> [args]

# Or invoke the built executable directly
./bin/Debug/net8.0/KontrolCode <command> [args]
```

With no arguments, the binary prints a usage summary listing every registered command.

---

## Commands

| Command | Description |
| --- | --- |
| `init [path]` | Initialize a new repository at `path` (defaults to the current directory) |
| `hash-object [-w] <file>` | Print the SHA-1 object ID of `file`; with `-w`, also write the blob to the object store |
| `add <file>...` | Hash each file as a blob and stage it in the index |
| `commit -m <message>` | Create a tree from the index, write a commit object, and advance the current branch |
| `log [--all] [--oneline] [<ref>]` | Print commit history; `--all` walks every branch tip, `--oneline` prints one line per commit |
| `branch [-d] [<name>] [<commit>]` | With no args, list branches. With a name, create a branch at `commit` (or `HEAD`). With `-d`, delete a branch |
| `checkout <branch\|commit>` | Switch `HEAD` to a named branch, or detach it at a commit (full or unique-prefix hash) |

### Example workflow

```bash
# 1. Create a repo in the current directory
dotnet run -- init

# 2. Stage a file (reads bytes, writes a blob object, records it in the index)
echo "hello world" > hello.txt
dotnet run -- add hello.txt

# 3. Commit the index as a snapshot
dotnet run -- commit -m "Initial commit"

# 4. Inspect history
dotnet run -- log
dotnet run -- log --oneline

# 5. Branch and switch
dotnet run -- branch feature
dotnet run -- checkout feature
```

---

## Repository layout

When `init` runs, it creates `.kontrolcode/` inside the working directory:

```
.kontrolcode/
├── HEAD                  # Symbolic ref to the current branch ("ref: refs/heads/main")
├── config                # [user] name / email
├── index                 # JSON-serialized staging area
├── objects/              # Content-addressed object store
│   └── aa/               # 2-char hash prefix as a fan-out directory
│       └── <rest-of-hash> # zlib-compressed <type> <len>\0<content> blob
└── refs/
    ├── heads/<branch>    # Branch tip commit hash
    └── tags/<tag>        # Tag commit hash (set programmatically via RefStore)
```

### Object model

Objects use the standard Git header layout `<type> <byte-length>\0<content>` and are SHA-1 hashed. Compression is custom zlib (Deflate stream + Adler-32 checksum) rather than `System.IO.Compression.ZipArchive`, and the object is stored at `objects/<hash[0..2]>/<hash[2..]>` — the same fan-out scheme Git uses.

| Type | Model | Notes |
| --- | --- | --- |
| `blob` | `Blob(byte[] Content)` | Raw file contents |
| `tree` | `Tree(IReadOnlyList<TreeEntry>)` | Flat list of `(mode, name, hash)` entries (no subtrees yet) |
| `commit` | `Commit(TreeHash, ParentHash?, Author, Message)` | Single parent, one author/committer line |

The **index** is a `List<IndexEntry>` persisted as JSON to `.kontrolcode/index` — this is a simplification of Git's binary index format.

### Configuration

`.kontrolcode/config` holds user identity (`name`, `email`) used when authoring commits. The `Config` class loads on repo open and writes back on save; defaults are `KontrolCode User` / `user@kontrolcode.local`.

---

## Project structure

```
KontrolCode/
├── Program.cs                 # Entry point: command registration, arg dispatch, usage printer
├── KontrolCode.csproj         # net8.0 console project (Nullable enabled, ImplicitUsings enabled)
├── Commands/                  # ICommand implementations, one per CLI verb
│   ├── ICommand.cs
│   ├── InitCommand.cs
│   ├── HashObjectCommand.cs
│   ├── AddCommand.cs
│   ├── CommitCommand.cs
│   ├── LogCommand.cs
│   ├── BranchCommand.cs
│   └── CheckoutCommand.cs
├── Core/
│   ├── Repository.cs          # Top-level facade (Create/Open, HashObject, Add, Commit, Log, LogWithHash)
│   ├── Index.cs               # Staging area (load/save as JSON)
│   ├── Config.cs              # user.name / user.email
│   ├── ObjectStore.cs         # Read, Write, Exists, FindByPrefix, header + tree + commit parsers
│   ├── RefStore.cs            # HEAD, branches, tags, ref resolution
│   ├── Models/
│   │   ├── GitObject.cs       # abstract record — Type, RawContent, Hash (SHA-1), Serialize
│   │   ├── Blob.cs
│   │   ├── Tree.cs
│   │   ├── Commit.cs
│   │   ├── TreeEntry.cs
│   │   ├── IndexEntry.cs
│   │   └── Author.cs          # record (Name, Email, DateTimeOffset)
│   └── Storage/
│       └── ObjectSerializer.cs # SerializeBlob/Tree/Commit + Compress/Decompress (custom zlib) + WriteObject
└── Utils/
    └── HashHelper.cs          # SHA-1 helpers (byte[] and ReadOnlySpan<byte>)
```

---

## Implementation notes & caveats

- **Tree objects are flat.** `Tree.BuildFromIndex` produces a single-level tree from index entries; there is no recursive directory handling yet. Files with the same name at different paths are not supported.
- **Single-parent commits only.** `Commit` has a `ParentHash?` field and no merge support.
- **Index is JSON, not the binary Git index format.** Path normalization converts `\` to `/` and strips leading `./` so paths compare consistently.
- **`checkout` does not update the working tree.** It only rewrites `HEAD` (to either `ref: refs/heads/<branch>` or a raw detached hash). Files on disk are not modified to match the target commit.
- **`log --all`** dedupes commits seen across branch tips before printing.
- **Ambiguous short hashes** cause `ObjectStore.FindByPrefix` to throw — this is the same behavior as Git for `checkout <short-hash>`.
- **Tag refs** are written by `RefStore.CreateTag`, but no CLI command currently exposes tagging.
- The `TestRepo/` directory at the repository root is a scratch directory used during development; it is not part of the build.

---

## License

MIT
