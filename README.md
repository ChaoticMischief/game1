# game1

Unity + C# game.

## Toolchain

- Unity 6000.6.3f1 (via Unity Hub), with Windows (Mono) and WebGL build support
- JetBrains Rider (primary C# IDE) and/or Cursor
- .NET SDK, Git LFS

On macOS: `brew install --cask unity-hub rider cursor dotnet-sdk && brew install git-lfs`

## First-time setup after cloning

Enable the tracked git hooks (run once per clone):

```sh
git config core.hooksPath .githooks
```

`.githooks/` holds the Git LFS hooks (because `core.hooksPath` bypasses the ones
`git lfs install` would create) plus `post-commit`, which records each commit in
the private dev-history repo whose location is set in `.claude/dev-repo-config.json`.
If that repo isn't checked out next to this one, the dev-history part does nothing.

Register Unity's smart merge tool for scenes/prefabs (path is per-machine, macOS shown):

```sh
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver \
  "'/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/Helpers/UnityYAMLMerge' merge -h -p --force %O %B %A %A"
git config merge.unityyamlmerge.recursive binary
```

CI will be added later.
