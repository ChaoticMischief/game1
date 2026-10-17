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

## Setting up a machine (Windows, for artists)

This sets up a Windows 10/11 PC for making art, from a blank machine: Git (with Git LFS and Git
Credential Manager), GitHub Desktop, GitHub CLI, Unity Hub and the matching Unity editor with
WebGL support, Krita, and PureRef when winget has it. It also clones the game and sets this
clone's git config. It does not install code tools (Rider, .NET SDK, gitleaks, Claude Code) and
does not touch the private dev-history repo. `winget` (App Installer) is built into current
Windows 10/11; if the script says it is missing, update "App Installer" in the Microsoft Store.

Open **PowerShell** (Start menu > PowerShell) and download the script to a temporary folder:

```powershell
$dir = Join-Path $env:TEMP 'game1-setup'
New-Item -ItemType Directory -Force $dir | Out-Null
Invoke-WebRequest https://raw.githubusercontent.com/ChaoticMischief/game1/main/tools/bootstrap-windows.ps1 -OutFile "$dir\bootstrap-windows.ps1"
notepad "$dir\bootstrap-windows.ps1"   # read it before you run it
```

Then run it with your GitHub username (add `-DryRun` first to see what it would do):

```powershell
powershell -ExecutionPolicy Bypass -File "$dir\bootstrap-windows.ps1" -GitHubUser <your-github-name>
```

Options: `-RepoUrl`, `-TargetDir` (default `%USERPROFILE%\dev\game1`), `-GitName`, `-GitEmail`,
`-SkipUnity`, `-SkipKrita`, `-SkipPureRef`, `-SkipGitHubCli`. It is safe to re-run: anything
already installed is left alone, and an existing clone is only fast-forwarded. It never asks for
a password or token; git opens a browser window to sign in to GitHub the first time it needs to.
At the end it lists what is left for a person: the Unity sign-in and license, the GitHub
sign-in, and adding the project to Unity Hub. Artists then read
[`docs/design/art-direction.md`](docs/design/art-direction.md) and
[`docs/design/art-spec.md`](docs/design/art-spec.md). The script has been checked for syntax, but
not run on a real Windows machine yet; report problems as issues.

`.githooks/` holds the Git LFS hooks (because `core.hooksPath` bypasses the ones
`git lfs install` would create) plus `post-commit`, which records each commit in
the private dev-history repo whose location (and clone URL) is set in
`.claude/dev-repo-config.json`. If that repo isn't checked out, the dev-history part
does nothing.

## Agent tooling

`AGENTS.md` is the shared instruction file for every agent (`CLAUDE.md` imports it). It covers:

- **MCP for Unity** (`com.coplaydev.unity-mcp`, pinned in `Packages/manifest.json`): lets agents
  drive the open editor over `http://localhost:8080/mcp`. The server starts with the editor.
  Cursor's entry is committed in `.cursor/mcp.json`; Claude Code's (`UnityMCP`) is machine-local,
  added by the bootstrap script, because the package rewrites Claude Code's config itself.
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

## CI and builds

Every pull request and push to `main` runs `.github/workflows/ci.yml` on GitHub-hosted runners
with [GameCI](https://game.ci): hook tests, workflow lint, a gitleaks secret scan, Unity
EditMode/PlayMode tests, and macOS/Windows/WebGL builds. `main` only accepts pull requests
that pass all of them. Each merge to `main` publishes the builds as the rolling
[`dev` prerelease](../../releases/tag/dev).

- **Code coverage:** the Unity tests job records coverage of `Game.*` assemblies (EditMode and
  PlayMode combined), shows a summary on the run's page, and uploads the HTML report as the
  `code-coverage` artifact.
- **Faster PR builds:** pull requests build WebGL with `-fastBuild` (IL2CPP "Faster (smaller)
  builds", Debug C++ configuration); the published `main` builds stay fully optimized.
- **LFS cache:** jobs pull Git LFS files through the Actions cache
  (`.github/actions/lfs-pull-cached`), so they don't use the LFS bandwidth quota every run.
- **Upstream updates:** `.github/workflows/upstream.yml` runs weekly (or on demand from the
  Actions tab) and keeps one `upstream` issue listing newer Unity editor, package and analyzer
  versions. Dependabot covers GitHub Actions.

- **itch.io:** each merge to `main` also pushes the WebGL build to itch.io (channel `html5`) with
  [butler](https://itch.io/docs/butler/), once you set the `ITCH_TARGET` repository variable
  (`<itch user>/<game slug>`) and the `BUTLER_API_KEY` secret (itch.io > Settings > API keys).
  Without them the job does nothing. Variable and secret changes only apply to runs that start
  afterwards. To re-push the current `dev` WebGL build without rebuilding, run the
  "Push dev WebGL build to itch.io" workflow (`gh workflow run itch.yml`). In the itch.io page's settings, mark the upload as "played
  in the browser". The WebGL build is gzip-compressed, so the host must send
  `Content-Encoding: gzip` for the `.gz` files; if itch.io doesn't, enable Unity's decompression
  fallback (Player Settings > Publishing Settings).

Unity is activated with a Personal license from the `UNITY_EMAIL` / `UNITY_PASSWORD` repository
secrets. Pull requests from forks can't use secrets, so their Unity jobs are skipped.

Builds report errors and crashes to Sentry when they're given a DSN: CI passes the `SENTRY_DSN`
repository secret, and local builds read `SENTRY_DSN` or `UserSettings/SentryDsn.txt`. The DSN
is never committed. CI builds also upload debug symbols, using the `SENTRY_AUTH_TOKEN` secret.
Run any build with `-sentry-test` to send a test error. See
[ADR 0002](docs/adr/0002-crash-reporting-sentry.md).

## Version control: Git/GitHub only

Source control is Git + GitHub (with LFS), not Unity Cloud. Don't install Unity
Version Control (`com.unity.collab-proxy`), and don't link the project to a Unity
Cloud project. If Unity Hub or the editor offers either, decline.
`ProjectSettings/VersionControlSettings.asset` stays on `Visible Meta Files`, and
asset serialization stays on `Force Text`.

## License

Code (C#, scripts, tooling, configuration) is under the [MIT License](LICENSE). Game content
(art, audio, scenes, levels, story, the game's name and branding) is all rights reserved. See
[CONTENT-LICENSE.md](CONTENT-LICENSE.md) for exactly which files are which.
