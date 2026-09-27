# game1: instructions for AI agents

Unity 6 (version in `ProjectSettings/ProjectVersion.txt`) + C# game. These instructions apply to
every agent (Claude Code, Cursor, and any added later). Agent-specific extras live in
`CLAUDE.md` / `.cursor/`.

## Commits (required)

End every commit with a `Session-Id: <your session/conversation id>` trailer. Your session
start hook tells you the id:

```sh
git commit -m "Short summary" --trailer "Session-Id: <id>"
```

`.githooks/post-commit` uses it to file the commit under your session in the dev-history repo.
Without it the commit is recorded as a human commit. If the hook prints a `PermissionError`
(your shell is sandboxed and can't write outside the project), ignore it and don't re-run the
hook: your session's sync records the commit.

## Branches and pull requests (required)

`main` is protected: no direct pushes. For every change:

1. `git switch -c <type>/<short-name>` (e.g. `feat/player-movement`, `fix/jump-height`,
   `chore/ci-cache`) from an up-to-date `main`.
2. Commit with the `Session-Id` trailer, push the branch, and open a PR with
   `gh pr create --fill` (or a title and body that explain the change).
3. CI (`.github/workflows/ci.yml`) must pass: hook tests, workflow lint, secret scan, Unity
   tests, and builds for macOS, Windows and WebGL. Check with `gh pr checks --watch`. Fix
   failures on the branch rather than working around them.
4. Merge with a merge commit (`gh pr merge --merge`), and only when the user asks you to.
   Squash and rebase merges are disabled because they'd give your commits new SHAs on `main`
   that the dev-history repo doesn't know about.

Every merge to `main` publishes the three builds as the rolling `dev` prerelease on GitHub.

The repo is public. Never commit anything that shouldn't be public: private notes belong in
the dev-history repo, and paid or third-party assets need a license that allows public
redistribution. Game content is all rights reserved (see `CONTENT-LICENSE.md`).

## Working with Unity

Two ways to drive Unity. Pick by whether the editor has the project open.

**Editor open → MCP for Unity** (server `unityMCP`, `http://localhost:8080/mcp`, configured in
`.mcp.json` and `.cursor/mcp.json`). Use it to inspect and edit scenes, GameObjects,
components and assets, read the console, refresh/compile, enter play mode, run tests, take
screenshots and build. If its tools aren't available, the editor is closed or the server isn't
running (Unity: Window → MCP for Unity → Start Server).

**Editor closed → `tools/unity/unity.sh`** (headless; refuses to run while the editor has the
project open):

| command | what it does |
|---|---|
| `tools/unity/unity.sh compile` | import + compile; prints `error CS…` lines |
| `tools/unity/unity.sh test [editmode\|playmode\|all]` | run tests; prints counts and each failure |
| `tools/unity/unity.sh build <mac\|windows\|webgl> [--development]` | build enabled scenes to `Builds/` |
| `tools/unity/unity.sh sync-solution` | regenerate `game1.sln` / `*.csproj` |

Logs and NUnit XML results go to `Logs/agent/`. The live editor's log is
`~/Library/Logs/Unity/Editor.log`.

After changing C# code, verify it: compile (MCP refresh or `unity.sh compile`) and run the
tests. Don't report work as done while there are compile errors or failing tests.

## Engine choices

Stay on current Unity tech: the project will move to Unity 7 as soon as it's GA, so avoid
deprecated APIs (fix `CS0618` obsolete warnings rather than suppressing them).

- **Rendering: URP.** Pipeline asset `Assets/Settings/URP.asset` (renderer `URP_Renderer.asset`)
  is the project default; quality levels inherit it. Use URP/Shader Graph shaders, never Built-in
  ones.
- **Input: Input System only** (the legacy Input Manager is disabled). Use the project-wide
  actions in `Assets/Settings/InputSystem_Actions.inputactions` (`InputSystem.actions`), never
  `UnityEngine.Input`.

## Versioning

Player Settings > Version holds the base `major.minor.patch` (pre-release `0.x`; `1.0.0` is the
first GA). Every build is stamped `major.minor.patch.yyyy.MM.dd.hash8`, e.g.
`1.0.0.2026.09.27.54d78a87`, from the commit's UTC date and hash, with `-dirty` added for
uncommitted changes. See `Assets/Editor/AgentTooling/BuildVersion.cs`. At runtime it's
`Application.version`. Bump the base version only when asked.

## Project layout and conventions

- `Assets/Scenes/Main.unity`: the main scene, first in Build Settings.
- `Assets/Editor/AgentTooling/`: `AgentCommands` (the `-executeMethod` entry points behind
  `unity.sh`) and the MCP for Unity defaults. Assembly `Game.EditorTools`.
- `Assets/Tests/EditMode/`, `Assets/Tests/PlayMode/`: NUnit tests (Unity Test Framework),
  assemblies `Game.Tests.EditMode` / `Game.Tests.PlayMode`. New gameplay code should live in
  its own assembly definition so tests can reference it.
- Every asset has a `.meta` file with a GUID. Create, move and rename assets through Unity (MCP
  tools, or move the `.meta` along with the file) and commit `.meta` files with their assets.
  Never hand-edit GUIDs.
- Never edit or commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/`, or the generated
  `.sln` / `.csproj` files (all gitignored).
- Scenes, prefabs and other assets are serialized as text (Force Text). Prefer changing them
  through Unity rather than editing YAML by hand.
- Binary assets (images, audio, models, fonts) are stored in Git LFS via `.gitattributes`.
- Version control is Git/GitHub only. Don't add Unity Version Control
  (`com.unity.collab-proxy`) or link a Unity Cloud project.
- Package changes go in `Packages/manifest.json`. Pin git packages to a tag.

## Off limits

Don't read secret-bearing files: `.env*`, `*.pem`, `*.key`, `secrets/`, `credentials/`. Hooks
block this for Claude Code and Cursor; ask the user for any value you need.

The dev-history repo (location in `.claude/dev-repo-config.json`) is written only by hooks.
Don't edit it by hand.
