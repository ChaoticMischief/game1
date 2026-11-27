# Godot 4 spike: findings

Status: throwaway evaluation, 2026-11-26. **Don't merge.** The Unity project is untouched.

This spike ports the playable version 1 bedroom (`docs/design/core-loop.md`) to **Godot 4.7.2**
(latest stable) in GDScript, then measures it against the Unity build. GDScript, not C#, because
Godot 4's C# can't export to the web, and the game ships on the web.

## Summary

| | Unity 6 (current) | Godot 4.7.2 (spike) |
|---|---|---|
| Gameplay code (bedroom) | 1,054 lines / 826 code lines, 22 files | 515 lines / 323 code lines, 18 files |
| Scene authoring | 275-line C# builder generates a 2,756-line YAML scene | 374-line hand-written text scene + 81-line theme |
| Tests | 337 lines (EditMode + PlayMode) | 198 lines, 19 tests, about 9 s headless |
| Web download (gzip) | **17.6 MB** | **10.2 MB** (12.1 MB with Sentry) |
| Web load on localhost (to first frame) | about 0.6 s cold, 0.3 s warm, then the "Made with Unity" splash | about 0.55 s cold, 0.3 s warm |
| Runtime, bedroom in Chrome (60 Hz) | steady 60 fps; about 2 ms main-thread work per frame; dropped frames at 20x CPU slowdown | steady 60 fps; about 1 ms per frame; no dropped frames even at 20x |
| Memory after load (JS heap + wasm memory) | 27 MB + 144 MB = **171 MB** | 8.5 MB + 38 MB = **47 MB** |
| Web export, local (M-series Mac) | not measured (editor was closed; CI job below) | **1.2 to 1.4 s** |
| Web export on GitHub Actions | about 21 min (GameCI, needs a license) | **56 s for the whole job**, of which export 3 s |
| Engine download / license | Hub + editor (GBs), Unity account, license activation in CI | 170 MB app, no account, MIT |
| Crash reporting | sentry-unity | sentry-godot 2.5.1 supports web (verified that it boots; no events sent) |
| Agent tooling | MCP for Unity, `unity.sh`, C# LSP | Headless CLI for everything; community MCP servers only |
| Sarah's bone-animated characters | PSD Importer + 2D Animation (first-party) | **No first-party equivalent. This is the open risk.** |

**Verdict:** on code size, build speed, web size, runtime cost, memory, CI cost and agent
friendliness, Godot is as good or better. It fit the whole core loop in about a third of the
code, and it builds for the web in about a second. The one thing that could block a switch is Sarah's art pipeline: painted
characters animated with bones. Unity has a first-party path for that and Godot doesn't (see
"Art pipeline"). Before deciding, spend a day with Sarah rigging one plushie in Godot. If that
goes well, switching now is cheap: this port took one session.

## What was ported

All of the version 1 core loop: the bedroom greybox, bear and rabbit (mass, speed, jump height
match the Unity builder), R / B to select, 1 / 2 to cycle, the pressure plate, the door, the
latching lever on the shelf, the exit that needs both plushies, falling out of the world resets the
room (single `RoomController.reset_room()` path), the Man in the Moon hint button with three tiers
(E or click), the name labels over the plushies, the status bar, the controls help (? / F1), and the
build-version label. Gamepad bindings are defined but weren't tested on a pad.

Layout: `project.godot` (settings + input map), `scenes/main.tscn` (the room and HUD),
`scripts/{room,player,hints,diagnostics}/`, `rooms/bedroom_hints.tres` (hint text as data),
`ui/theme.tres`, `test/`, `tools/fetch-addons.sh` (pinned gdUnit4 and, optionally, sentry-godot;
they're gitignored rather than vendored).

Run it:

```sh
tools/fetch-addons.sh                                 # gdUnit4 v6.2.3 into addons/
godot --headless --path . --import
GODOT_BIN=$(which godot) addons/gdUnit4/runtest.sh --headless --ignoreHeadlessMode -a res://test
godot --headless --path . --export-release "Web" build/web/index.html
```

Not ported: Sentry wiring (feasibility only, below), the build-version stamping (`BuildVersion.cs`;
in Godot it's one `application/config/version` override before export), and desktop exports.

## Lines of code

Counted with blank lines and comment-only lines removed ("code lines"):

| Area | Unity C# | Godot GDScript | Why it shrank |
|---|---|---|---|
| Room (plate, door, lever, exit, reset, camera) | 315 | 195 | `CameraFit` is gone: `display/window/stretch` ("canvas_items" + "expand") fits the room to any window |
| Player (plushie, roster, selection, jump) | 235 | 153 | `CharacterBody2D.move_and_slide()` + `is_on_floor()` replace the overlap-box ground check |
| Hints + HUD | 329 | 107 | Layout and styling live in the scene and theme instead of code; name labels are children of the plushies, so no world-to-screen projection |
| Diagnostics | 175 | 60 | Sentry config and test trigger not ported (about 40 lines) |
| **Total runtime** | **1,054 (826 code)** | **515 (323 code)** | |
| Scene construction | 275 (builder) | 0 (the scene is text) | |
| Tests | 337 (290 code) | 198 (142 code) | |

Some of the gap is C#'s braces and `[SerializeField]` boilerplate, and some is that Godot's
idioms really are shorter (signals, `%UniqueName` lookups, `@export var room: RoomController`
node references written straight into the scene).

## Web build size and load time

Both measured from the same local server that gzips like a CDN, in the Claude desktop browser
pane (Chromium) on an Apple-silicon Mac. Unity: the current `dev` release
(`game1-WebGL-39f50f09.zip`). Godot: `export_presets.cfg` preset "Web" (single-threaded, so it
needs no cross-origin-isolation headers on itch.io).

| File | Unity (pre-gzipped by Unity) | Godot raw | Godot gzip |
|---|---|---|---|
| Engine (wasm) | 11.50 MB | 39.5 MB | 10.05 MB |
| Game data | 6.02 MB (`.data`) | 40 KB (`.pck`) | 20 KB |
| JS loader/framework | 0.11 MB | 0.28 MB | 0.07 MB |
| **Transferred** | **17.6 MB** | | **10.2 MB** |

- At 25 Mbit/s that's about 5.6 s of download for Unity against 3.3 s for Godot. On localhost
  both reach the first frame in about 0.3 to 0.6 s, so on real connections download size is what
  players feel.
- itch.io: serve Godot's `.wasm` compressed or it's 39.5 MB. Godot's export doesn't pre-compress
  the way Unity's does. Check that itch serves it gzipped, or gzip it in CI and set the header.
- Godot's engine wasm is generic. A custom export template with unused modules stripped (3D,
  physics 3D, etc.) usually roughly halves it. Not tried here, because it needs an Emscripten build
  of the engine in CI.
- The Unity build shows the "Made with Unity" splash after loading. Unity 6 Personal can turn it
  off, so that isn't a Godot advantage, just a setting to check.

## Runtime performance in the browser

Both web builds were run in Chrome 158 (headless, but drawing on the real GPU through ANGLE
Metal) on an Apple M4 Max at 60 Hz in a 1280x720 window, from the same local server. The Unity
build is the `dev` release `39f50f09`. Each configuration ran 3 times, and the tables show the
median. A run: load the page, wait 6 s (past Unity's splash), measure 8 s idle with the bear
selected, then 3 s holding D. Screenshots confirmed the bear walked to the door in both builds,
so the simulated input reached both engines. "Slowdown" is Chrome DevTools' CPU throttling, which
stands in for a modest machine: 4x is roughly a mid-range laptop, 10x to 20x a cheap Chromebook
or an old phone.

**Frame rate.** Both held a steady 60 fps (16.7 ms frames, p99 16.8 ms) idle and walking, at 1x,
4x and 10x. At **20x**, Unity dropped frames in all 3 runs. Frames over 20 ms while walking were
1, 4 and 70 of about 180, and the worst run averaged 28.6 ms (about 35 fps), with an idle hitch up
to 33 ms. Godot dropped none.

**Main-thread work per frame** (CDP `TaskDuration` ÷ frames, in thread time):

| Slowdown | Unity idle | Unity walking | Godot idle | Godot walking |
|---|---|---|---|---|
| 1x | 2.1 ms | 1.8 ms | 1.0 ms | 1.0 ms |
| 10x | 4.3 ms | 5.3 ms | 1.8 ms | 2.1 ms |
| 20x | 5.9 ms | 7.9 ms | 4.2 ms | 4.5 ms |

Compare the engines within a row, not one row with another. Chrome throttles by pausing the
thread, so thread time doesn't grow 20x, and the 4x row (omitted) came out lower than 1x for both
engines. Within each row, Godot does about half of Unity's work per frame. Both are far under the
16.7 ms budget for this greybox room. Real art, more rooms and Unity's 2D animation will change
this, so treat it as engine overhead, not a game budget.

**Memory** after load, after a forced garbage collection, identical across all runs:

| | JS heap | WebAssembly memory | Total |
|---|---|---|---|
| Unity | 27.1 MB | 144 MB | **171 MB** |
| Godot | 8.5 MB | 38 MB | **47 MB** |

"WebAssembly memory" is the size of the engine's linear memory, which is what the page actually
reserves. Unity's includes its configured initial heap, so some of it is headroom, but the browser
commits it either way. On low-memory phones and Chromebooks, 47 MB against 171 MB matters more
than the frame times.

**Load time under slowdown** (to the engine's "ready", excluding Unity's splash): about the same
for both, roughly 0.35 / 1.2 / 3.1 / 6.3 s for Unity and 0.4 / 1.3 / 3.3 / 6.9 s for Godot at
1x / 4x / 10x / 20x. That's wasm compilation and startup. Download time comes on top (see the
sizes above), and there Godot's smaller download wins on any real connection.

Not measured: GPU time (both draw a few dozen quads), Safari and Firefox, real phones, and
desktop builds.

Reproduce: `tools/web-serve.py <build dir> <port>` for each build (8401 Unity, 8402 Godot),
start Chrome with `--headless=new --remote-debugging-port=9334 --use-angle=metal
--enable-precise-memory-info`, then `python3 tools/web-perf.py 9334 3`.

## Headless export in GitHub Actions

`.github/workflows/godot-spike.yml` runs on this PR. It downloads the official Linux editor and
export templates from the godotengine/godot GitHub release (SHA-512 checked, cached), imports,
runs the gdUnit4 tests, and exports for the web. There's no license, no activation, no secrets and
no Docker image. Result on this PR ([run 45931107050](https://github.com/ChaoticMischief/game1/actions/runs/45931107050)),
uncached (first run):

| Step | Time |
|---|---|
| Download editor + templates (1.3 GB, SHA-512 checked) | 16 s (skipped when cached) |
| Import | 7 s |
| gdUnit4: 19 tests, all passing | 14 s |
| Web export | 3 s |
| **Whole job** | **56 s** |

The export it produced is the same size as the local one (39.5 MB wasm, 10.2 MB gzipped).

For comparison, the Unity CI's WebGL job takes about 21 minutes and is the critical path of a
16 to 21 minute pipeline, and it needs the Unity license secrets.

Windows and macOS exports would add a few seconds each (same binary, more templates). macOS
signing and notarization are the same work as in Unity.

## Sentry (sentry-godot)

Feasible on every platform the game ships on, including web, without committing a DSN:

- sentry-godot 2.5.1 (2026-11-12) lists Web as supported since 1.4.0. It's a GDExtension plus the
  Sentry JavaScript SDK on web. `tools/fetch-addons.sh --sentry` installs it. It's 127 MB
  unpacked, for every platform, so fetch it in CI rather than committing it.
- On web, GDExtensions need the "extensions support" export variant (dynamic-linking templates).
  The preset "Web (Sentry)" builds it: **12.1 MB** gzip (+1.9 MB) with the engine split into a
  1.5 MB main wasm and a 44 MB side module. It loaded and ran the bedroom in the browser.
- With no DSN it does nothing: "Automatic initialization is disabled because no DSN was
  provided". That's the same "forks don't report" behaviour as `SentryDsn.cs` today. CI would set
  `sentry/options/dsn` from the `SENTRY_DSN` secret just before export (e.g. an
  `override.cfg`), and the environment comes from `BuildInfo.sentry_environment()` (ported and
  tested).
- Things to know: it edits `project.godot` the first time it loads (turns on
  `debug/settings/gdscript/always_track_call_stacks`, adds a `[sentry]` section). On web it logs an
  error unless file logging is on or log attachments are off. GDScript errors arrive with stack
  traces, but native engine frames aren't symbolicated on web yet (sentry-godot #516). No events
  were sent, because there was no DSN.

## Working with agents

What went well:

- **Everything runs headless from one binary**: import, tests, export, and running a script
  against the game (`--headless -s probe.gd`). There's no "editor has the project open" lock like
  `unity.sh` has, no license and no Hub.
- **Text formats an agent can write.** The whole room and HUD were written as a `.tscn` by hand,
  from the Unity builder's numbers, and loaded first time. It's 374 readable lines, against
  Unity's 2,756-line YAML with fileIDs that in practice needs the editor or MCP to change.
- **Fast feedback**: import 1.4 s, the full test suite 9 s, the web export 1.3 s. The Unity
  equivalents are minutes.
- **gdUnit4** (v6.2.3): NUnit-like asserts, parameterized tests, a scene runner with simulated
  input and JUnit XML for CI. It prints "Headless mode is not supported" because input events
  aren't delivered headless, but with `--ignoreHeadlessMode` the R / B / E key tests passed here.
  If that stops working in CI, `xvfb-run` without `--headless` is the fallback. GUT 9.x is the
  other mature option.

What's weaker:

- **No official MCP server for the editor.** Community options: Coding-Solo/godot-mcp (Node,
  launches and runs projects), hi-godot/godot-ai (editor plugin, about 120 tools),
  beckett-godot-mcp (one GDScript file in the editor), Godot Editor MCP (Asset Library). None was
  tried here. They're young and of mixed quality, so pick one and pin it. Because the formats are
  text and the CLI is complete, an MCP is much less necessary than with Unity.
- **No headless language server.** Godot's GDScript LSP runs inside the editor (port 6005), so
  agents get no csharp-lsp-style diagnostics without an editor open. `gdtoolkit`
  (gdlint/gdformat) covers style. Type errors show up at import or test time, which is
  fast enough.
- **GDScript is less strict than C#.** Typed GDScript (used throughout) catches a lot, but there
  are no Roslyn analyzers. Models also sometimes write Godot 3 APIs. `AGENTS.md` would need a
  "Godot 4 only" note.
- `project.godot` belongs to the editor. A headless import rewrote it and dropped the comments
  written here. Keep explanations in docs, not in that file.

### An engine bug this spike hit

In Godot 4.7.2, **script-exported `PackedStringArray` / `Packed*Array` values are lost on export**
when text resources are converted to binary. The editor and tests saw the three hint tiers, and the
web build had none ("No hints here."). Minimal repro: a Resource script with
`@export var a: PackedStringArray`, a `.tres` that sets it, and `--export-pack`. Loading the pack
gives `a=[]`, while `Array[String]`, `Array` and `String` survive. Workaround in
`scripts/hints/hint_set.gd`: use `Array[String]`. It was caught only because the exported build
was played in a browser. A Godot move should include a smoke test that runs against the
exported pack, not only the editor project. Worth reporting upstream (not done from this spike).

## Art pipeline (Sarah)

Today's plan (`docs/design/backlog.md`): Sarah paints in **Procreate**, and characters are
**animated with bones in Unity**. In Unity that means the first-party PSD Importer (a layered PSB
becomes a rigged sprite) plus 2D Animation (bones, auto-generated geometry, auto weights, sprite
swap).

Godot:

- **Plain sprites, backgrounds and props: no problem.** PNG import, nearest/linear filtering,
  atlases, 9-slice, `Sprite2D`, `AnimatedSprite2D` for frame animation, `TileMapLayer`, lights
  and shaders in 2D. Replacing a greybox rect with a sprite is a one-node change.
- **Layered PSD import: no first-party importer.** Community plugins exist (a "PSD to Godot Scene
  Importer" on the Asset Library from 2026-09, which its author calls vibe coded; godot-limage;
  Proscenio by way of Blender). Procreate can also export each layer as PNG, which Godot imports
  natively, but someone then has to lay the parts out.
- **Bone animation: possible but more manual.** Godot has `Skeleton2D` / `Bone2D` /
  `Polygon2D` with weight painting, but you build each part's polygon and paint its weights by
  hand, with no automatic geometry or weights like Unity 2D Animation. Simple cutout animation
  (a hierarchy of `Sprite2D` parts keyed in an `AnimationPlayer`, no mesh deformation) is easy
  and may suit stiff plushies well. Spine has an official Godot runtime, but it's paid and its web
  support wasn't checked.

**This is the decision point.** Ask Sarah to take one plushie (layered Procreate export) through
Godot as either a cutout rig or a `Skeleton2D` rig. If she's happy with it, nothing else here
blocks a switch.

## Other notes for the decision

- Licensing: Godot is MIT, with no runtime fee, splash, account or seat. That removes the
  long-term licensing risk that started this spike.
- Version 1 "done when" (WebGL, finish the bedroom with both plushies, ask for a hint) holds in
  this port: the web build plays, and `test_both_plushies_can_escape_the_bedroom` runs the full
  solution on real physics.
- A real switch would also need: `AGENTS.md` / ADR 0001 rewritten, `unity.sh` and the Unity CI
  replaced by the much smaller workflow above, the itch.io push pointed at the Godot build, the
  dev-history hooks left alone (they're engine-agnostic), and desktop export presets.
- If the project stays on Unity, delete `godot-spike/` and `.github/workflows/godot-spike.yml`.
