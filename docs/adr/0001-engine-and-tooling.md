# 0001. Engine, packages and tooling baseline

- Status: Accepted
- Date: 2026-09-28

## Context

game1 is a long-lived Unity project developed largely by AI agents (Claude Code, Cursor) with a
human owner. The owner wants the newest technology throughout and plans to move to Unity 7 as
soon as it is generally available, so every choice should avoid deprecated or legacy paths that
would make that upgrade harder. The game's genre and features aren't decided yet.

## Decision

**Engine:** the latest Unity 6 release (currently 6000.6.3f1; the version lives only in
`ProjectSettings/ProjectVersion.txt`), upgraded to Unity 7 at GA.

**In use now:**

| Area | Choice | Instead of |
|---|---|---|
| Rendering | Universal Render Pipeline (URP) | Built-in pipeline |
| Input | Input System, project-wide actions | Input Manager (`UnityEngine.Input`) |
| Testing | Unity Test Framework (EditMode + PlayMode) | — |
| Static analysis | Microsoft.Unity.Analyzers, raised to warnings | — |
| Version control | Git + GitHub + LFS | Unity Version Control / Unity Cloud |
| CI | GitHub Actions + GameCI | Unity Build Automation |
| Agent access to the editor | MCP for Unity (CoplayDev) | Unity's subscription-only MCP |

**When a feature first needs it** (add the package in the PR that uses it, not before):

| Need | Use | Not |
|---|---|---|
| UI (menus, HUD) | UI Toolkit (UXML/USS) | uGUI (Canvas) |
| Cameras | Cinemachine 3.x (`Unity.Cinemachine`) | Cinemachine 2.x, hand-rolled camera rigs |
| Loading content at runtime | Addressables | `Resources/`, AssetBundles directly |
| Text localization | Localization package | Hard-coded strings |
| Async code | `Awaitable` / async-await | Coroutines for new async flows |
| Object lookup | `FindObjectsByType` / `FindAnyObjectByType` without sort mode, or explicit references | `FindObjectOfType`, sorted overloads |
| Physics queries | Non-allocating APIs | Allocating overloads in hot paths |

**Deferred:** save system (depends on the game), networking, platform services (Steam, etc.).

## Consequences

- Obsolete-API warnings (`CS0618`) are fixed, not suppressed; they flag work the Unity 7 upgrade
  would otherwise force later.
- New packages are added deliberately, with a PR that uses them, which keeps the dependency list
  meaningful and the Unity upgrade surface small.
- Unity 6 compiles C# 9, so C# 10+ features (file-scoped namespaces, etc.) wait for Unity 7.
- Anything in the "instead of" / "not" columns needs a new ADR to adopt.
