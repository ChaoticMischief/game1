# Contributing

game1 is developed in the open, mostly by AI agents working with the maintainer. Contributions
are welcome, within the limits below.

## Licensing of contributions

- **Code** you contribute (C#, scripts, tooling, configuration) is licensed under the
  [MIT License](LICENSE), like the rest of the code.
- **Game content** (art, audio, levels, story, design documents and other material listed in
  [CONTENT-LICENSE.md](CONTENT-LICENSE.md)) is all rights reserved. Please don't open PRs that add
  game content unless the maintainer has asked for it and you agree that ChaoticMischief may use it
  in the game without restriction.
- Don't add third-party assets or code unless their license allows public redistribution and use
  in a commercial game. Paid Asset Store content can't be committed here.

## How to contribute

1. For anything beyond a small fix, open an issue first to discuss it.
2. Branch from `main`, make the change, and add or update tests.
3. Open a pull request. CI must pass: hook tests, workflow lint, secret scan, Unity tests, and
   macOS/Windows/WebGL builds. Pull requests from forks can't run the Unity jobs (no access to
   the license secrets); the maintainer will run them.
4. `main` only accepts merge commits from pull requests.

[`AGENTS.md`](AGENTS.md) describes the project's conventions in detail (it's written for AI
agents but applies to everyone): engine choices, code layout, testing, and the tools in `tools/`.
Setting up a Mac is one script: `tools/bootstrap-macos.sh`.

## Reporting problems

Use the issue templates for bugs and feature requests. For security problems, see
[SECURITY.md](SECURITY.md) instead of opening a public issue.
