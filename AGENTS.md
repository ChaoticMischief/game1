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

## Work tracking

Work is tracked in GitHub Issues (`gh issue list`, `gh issue view N`). Labels: `bug`, `feature`,
`tech-debt`, `art`, `design`, `audio`, `ci`, `upstream`, `documentation`. When you pick up an
issue, reference it in the branch name (`feat/42-double-jump`) and close it from the PR body
(`Fixes #42`). If you find a problem you're not fixing now, open an issue for it instead of
leaving a TODO.

Design documents are in `docs/design/`; architecture decisions in `docs/adr/` (see
`docs/README.md`). Update the relevant design doc in the same PR when behavior changes, and add
an ADR for significant technical decisions.

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

**Editor open → MCP for Unity** (`http://localhost:8080/mcp`; server `UnityMCP` in Claude Code's
machine-local config, added by `tools/bootstrap-macos.sh`, and `unityMCP` in `.cursor/mcp.json`). Use it to inspect and edit scenes, GameObjects,
components and assets, read the console, refresh/compile, enter play mode, run tests, take
screenshots and build. If its tools aren't available, the editor is closed or the server isn't
running (Unity: Window → MCP for Unity → Start Server).

**Rider's MCP server** (`rider`, user-level config; only while Rider is running) adds IDE
inspections, refactorings and project-wide search. It's optional: if it isn't available, carry
on without it. Never enable its "brave mode".

**Editor closed → `tools/unity/unity.sh`** (headless; refuses to run while the editor has the
project open):

| command | what it does |
|---|---|
| `tools/unity/unity.sh compile` | import + compile; prints `error CS…` lines and warnings in project code |
| `tools/unity/unity.sh test [editmode\|playmode\|all]` | run tests; prints counts and each failure |
| `tools/unity/unity.sh build <mac\|windows\|webgl> [--development]` | build enabled scenes to `Builds/` |
| `tools/unity/unity.sh sync-solution` | regenerate `game1.sln` / `*.csproj` |

Logs and NUnit XML results go to `Logs/agent/`. The live editor's log is
`~/Library/Logs/Unity/Editor.log`.

After changing C# code, verify it: compile (MCP refresh or `unity.sh compile`) and run the
tests. Don't report work as done while there are compile errors, new warnings, or failing tests.

## Engine choices

Stay on current Unity tech: the project will move to Unity 7 as soon as it's GA, so avoid
deprecated APIs (fix `CS0618` obsolete warnings rather than suppressing them). The full list of
what to use, and what to use when a feature first needs it (UI Toolkit, Cinemachine 3,
Addressables, Localization, `Awaitable`, …), is in `docs/adr/0001-engine-and-tooling.md`. Add a
package in the PR that first uses it, not ahead of time.

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

Code:

- `Assets/Scripts/Runtime/`: gameplay code, assembly `Game.Runtime`, namespace `Game` (use
  sub-namespaces per feature folder, e.g. `Game.Player` in `Assets/Scripts/Runtime/Player/`).
  Add a separate assembly only for a clear boundary (e.g. a reusable system), and give editor-only
  code its own `Editor` assembly.
- C# style is in `.editorconfig`: block-scoped namespaces (Unity 6 is C# 9), Unity naming
  (`m_PascalCase` private fields, `s_PascalCase` private static fields, PascalCase members,
  camelCase locals), `[SerializeField] private` fields rather than public fields.
- Analyzers: Microsoft.Unity.Analyzers (`Assets/Plugins/Analyzers/`) run on every compile, raised
  to warnings by `Assets/Default.ruleset`. Fix `UNT####` warnings; suppress one only with a
  comment explaining why.
- Keep MonoBehaviours thin; put logic in plain C# classes that EditMode tests can cover without
  a scene.

Project:

- `Assets/Scenes/Main.unity`: the main scene, first in Build Settings.
- `Assets/Editor/AgentTooling/`: `AgentCommands` (the `-executeMethod` entry points behind
  `unity.sh`) and the MCP for Unity defaults. Assembly `Game.EditorTools`.
- `Assets/Tests/EditMode/`, `Assets/Tests/PlayMode/`: NUnit tests (Unity Test Framework),
  assemblies `Game.Tests.EditMode` / `Game.Tests.PlayMode`, both referencing `Game.Runtime`.
- `docs/`: design docs and ADRs (above). `tests/hooks/`: Python tests for the agent hooks.
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
