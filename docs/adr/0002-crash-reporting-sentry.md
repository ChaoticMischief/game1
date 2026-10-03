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
- **Debug symbols are uploaded by CI builds** (added for #3), so native crashes symbolicate
  and C# frames from release builds get file and line numbers.
  `Game.EditorTools.SentryCliConfiguration` gives Sentry's build step the auth token from
  `-sentryAuthToken` (the `SENTRY_AUTH_TOKEN` repository secret) or the environment, and takes
  the organization from the org token; `SentryCliOptions.asset` holds only the project name.
  Local builds have no token and skip the upload. A failed upload fails the CI build.
- **Exceptions to 0001**, for Sentry's sake only: the SDK requires uGUI (`com.unity.ugui`, for
  its optional user-feedback form) and loads its options from `Resources/`. Our own UI still uses
  UI Toolkit, and our own content still doesn't go in `Resources/`.

## Consequences

- Test that reporting works with any player: run it with `-sentry-test` and it logs one error
  and one exception naming its version.
- Sentry's Unity SDK only uploads symbols for standalone (macOS, Windows, Linux) and console
  builds, not WebGL. IL2CPP line numbers are therefore off: WebGL is our only IL2CPP build, and
  its C# stack traces have no line numbers. Native and C# symbols for macOS and Windows are
  uploaded.
- While sentry-cli runs, Sentry writes the auth token to `sentry.properties` in the build
  folder and deletes it afterwards. The builds are published, so CI fails a build whose output
  contains that file or the token, and an EditMode test fails if the committed
  `SentryCliOptions.asset` contains a token.
- Sentry's free plan has an event quota and a single user; revisit if either becomes limiting.
