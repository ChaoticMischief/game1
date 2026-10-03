# Licensing: code vs. game content

This repository contains two kinds of material under different terms.

## Code: MIT License (see `LICENSE`)

"Software" in `LICENSE` means the following, and nothing else:

- Source code and scripts: `*.cs`, `*.py`, `*.sh`, shader source (`*.shader`, `*.hlsl`,
  `*.cginc`, `*.compute`), assembly definitions (`*.asmdef`, `*.asmref`)
- Tooling and automation: everything under `tools/`, `.github/`, `.githooks/`, `.claude/`,
  `.cursor/`
- Project configuration: `Packages/`, `ProjectSettings/`, and repository config files
  (`.gitignore`, `.gitattributes`, `.editorconfig`, `.mcp.json`, `Assets/Default.ruleset`, and similar)
- Documentation: `README.md`, `AGENTS.md`, `CLAUDE.md`, `CONTRIBUTING.md`, `SECURITY.md`, these
  license files, and everything under `docs/` except `docs/design/`
- The `.meta` file of any file listed above

## Game content: all rights reserved

Copyright (c) 2026 ChaoticMischief. All rights reserved.

"Game content" means everything under `Assets/` that is not listed as Software above,
including scenes, prefabs, art, textures, sprites, models, animations, materials, audio,
music, fonts, UI layouts, levels, text, story and dialogue, together with their `.meta` files.
It also includes the game design documents under `docs/design/`, and the game's title, logos and
other branding, whether or not they appear in this repository.

No license is granted to game content. You may view it here as part of the public repository,
but you may not copy, modify, redistribute, or use it in other works or in builds of this game
you distribute, without written permission from ChaoticMischief.

Third-party packages referenced from `Packages/manifest.json` are not part of this repository
and remain under their own licenses. Third-party files committed here (for example the analyzer
in `Assets/Plugins/Analyzers/`) keep their own licenses, noted next to them.
