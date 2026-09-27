# game1

@AGENTS.md

## Claude Code specifics

- Your session id arrives in the SessionStart hook output. Use it for the `Session-Id` trailer.
- `.claude/hooks/` syncs this session's transcript and the project's auto-memory to the
  dev-history repo (location in `.claude/dev-repo-config.json`; never hardcode it). It also blocks
  reads of secret files. Don't try to work around the block.
- The `csharp-lsp` plugin (enabled in `.claude/settings.json`) gives you C# diagnostics,
  go-to-definition and find-references. It reads `game1.sln`; run
  `tools/unity/unity.sh sync-solution` if that file is missing or stale.
