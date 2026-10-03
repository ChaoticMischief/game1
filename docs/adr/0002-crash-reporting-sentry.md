# 0002. Crash and error reporting with Sentry

- Status: Accepted
- Date: 2026-10-03

## Context

Players' builds need to report errors and crashes back to us, tagged with the exact build
(`Application.version`). Unity's own crash reporting is part of Unity Cloud, which the project
doesn't use (see 0001). The repo is public, so nothing that lets a fork's or a stranger's build
report into our project should be committed.

## Decision

- **Sentry's Unity SDK** (`io.sentry.unity`, the `getsentry/unity` git package, pinned to a
  release tag). Its log handler turns `Debug.LogError`/`LogException` (and so `GameLog.Error` /
  `GameLog.Exception`) into events and earlier log lines into breadcrumbs; native crashes are
  captured on macOS and Windows.
- **Options** live in `Assets/Resources/Sentry/SentryOptions.asset` (edit via Tools > Sentry),
  with `Game.Diagnostics.SentryConfiguration` setting the environment in code: `editor`,
  `development`, `local` (dirty build), `prerelease` (0.x) or `production`. The release is the
  SDK default, `game1@<Application.version>`.
- **The DSN is never committed.** `Game.EditorTools.SentryDsn` writes it into the options just
  before a build and clears it afterwards, taking it from `-sentryDsn <dsn>` (CI, from the
  `SENTRY_DSN` repository secret), the `SENTRY_DSN` environment variable, or the gitignored
  `UserSettings/SentryDsn.txt` (local builds). An EditMode test fails if the committed asset
  contains one. Without a DSN the SDK stays off: in the editor, in forks and in local builds
  that don't set one.
- **Off in the editor** (`CaptureInEditor` false), so Play mode doesn't report.
- **Exceptions to 0001**, for Sentry's sake only: the SDK requires uGUI (`com.unity.ugui`, for
  its optional user-feedback form) and loads its options from `Resources/`. Our own UI still uses
  UI Toolkit, and our own content still doesn't go in `Resources/`.

## Consequences

- Test that reporting works with any player: run it with `-sentry-test` and it logs one error
  and one exception naming its version.
- Debug-symbol upload is off until a Sentry auth token is set up (tracked in an issue). Until
  then, native crash stack traces are unsymbolicated, and IL2CPP line numbers (which need the
  upload) are off, so WebGL C# stack traces have no line numbers. Mono builds (macOS, Windows)
  are unaffected.
- Sentry's free plan has an event quota and a single user; revisit if either becomes limiting.
