# game1

Unity + C# game.

## Toolchain

- Unity (version in `ProjectSettings/ProjectVersion.txt`, via Unity Hub), with Windows (Mono)
  and WebGL build support
- JetBrains Rider (primary C# IDE) and/or Cursor
- .NET SDK, Git LFS, gitleaks (redacts secrets from the dev history), gh

## Setting up a machine (macOS)

```sh
git clone https://github.com/ChaoticMischief/game1.git ~/dev/game1
~/dev/game1/tools/bootstrap-macos.sh
```

Run it in a normal terminal (the .NET SDK installer asks for sudo). It's safe to re-run. It
installs the tools, sets this clone's git config (tracked hooks and the UnityYAMLMerge
driver), clones the dev-history repo next to this one, restores Claude Code's memory for
this project from the dev repo's mirror, and installs the matching Unity editor. At the
end it lists the steps that need a person: license, sign-ins, and choosing Rider as
Unity's script editor.

`.githooks/` holds the Git LFS hooks (because `core.hooksPath` bypasses the ones
`git lfs install` would create) plus `post-commit`, which records each commit in
the private dev-history repo whose location (and clone URL) is set in
`.claude/dev-repo-config.json`. If that repo isn't checked out, the dev-history part
does nothing.

## Agent tooling

`AGENTS.md` is the shared instruction file for every agent (`CLAUDE.md` imports it). It covers:

- **MCP for Unity** (`com.coplaydev.unity-mcp`, pinned in `Packages/manifest.json`): lets agents
  drive the open editor over `http://localhost:8080/mcp`. The server starts with the editor, and
  `.mcp.json` (Claude Code) and `.cursor/mcp.json` (Cursor) point at it.
- **`tools/unity/unity.sh`**: headless compile, tests, builds and solution sync while the editor is
  closed.
- **C# code intelligence**: the `csharp-lsp` plugin for Claude Code (csharp-ls), and optionally
  Rider's built-in MCP server.

## AI session capture

Every AI session in this repo is recorded in the dev-history repo:

- **Claude Code**: `.claude/settings.json` hooks → `.claude/hooks/`
- **Cursor**: `.cursor/hooks.json` → `.cursor/hooks/cursor_hook.py`. Cursor also loads
  Claude Code hooks by default; those detect Cursor's payload and stand down, so each
  session is captured once, under its real agent.

Both apply the same secrets-read policy (`.env*`, `*.pem`, `*.key`, `secrets/`,
`credentials/`), and both tell the agent to end commits with a `Session-Id:` trailer,
which links each commit to its session.

## Version control: Git/GitHub only

Source control is Git + GitHub (with LFS), not Unity Cloud. Don't install Unity
Version Control (`com.unity.collab-proxy`), and don't link the project to a Unity
Cloud project. If Unity Hub or the editor offers either, decline.
`ProjectSettings/VersionControlSettings.asset` stays on `Visible Meta Files`, and
asset serialization stays on `Force Text`.

CI will be added later.
