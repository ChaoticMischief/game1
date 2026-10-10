# Overview: Plushie Escape (working title)

Status: draft, agreed in discussion on 2026-10-10. Values marked *(default)* are proposals
nobody has confirmed yet.

## Pitch

Living plushies escape a house, one room at a time. The game is a 2D side-view dollhouse cutaway.
You switch between plushies with different abilities and combine them to solve each room's
puzzle. It starts in a young girl's bedroom (a girl somewhere between 6 and 11).

## Audience and platforms

- Free forever. Made as a learning project; Sarah is the art director (see
  `CONTENT-LICENSE.md`). All art that ships is human-made.
- Audience includes the furry community, which is a reason for the human-art-only rule.
- Platforms: Windows and macOS builds always; WebGL is a strong nice-to-have and stays in scope
  for now. If a requirement forces desktop only, that is acceptable.
- Single player. Local couch co-op is possible later; no online play.

## Modes

Both modes share the same rooms and can be switched at any time. A switch takes effect at the
next room restart or checkpoint.

| | Story | Horror |
|---|---|---|
| Feel | Cozy daydream, slumber-party warmth | Nightmare version of the same house |
| Threat | None, no time pressure | The house AI and its robots hunt you |
| Fail state | None | You get caught, a short animation plays, the room resets (FNAF style). No health bars. |
| The AI | Friendly guide, gives hints | Predator controlling cameras and the family's helper robots |

**Version 1 is Story mode only.** Horror is coming, so early code must not make it harder: keep
"what happens when the player is caught" behind one reset path, keep the world state resettable,
and keep lighting and props swappable per mode.

## The house AI (guide in Story mode)

One AI character with two faces. In Story mode it is the **Man in the Moon**, an old, bearded,
sleepy crescent profile. He appears as an icon in a screen corner; clicking or tapping it gives
**tiered hints** (a nudge first, then more specific, then the solution), like the hint button in
hidden-object games. The horror face and the name are undecided (see `backlog.md`).

## Setting and art direction

- The first room is the girl's bedroom. Sarah's direction: **super duper cozy, very warm and
  comfy, Beatrix Potter illustration feel.**
- Later rooms sit in the same childhood home, which also has near-future smart gadgets. In Story
  mode they all fit in and imply no threat.
- Backstory is not decided (options: a sick daughter, a Sid's-house-style setup, plushies locked
  in the attic by an evil stepmother). It is not needed for version 1.
- Version 1 uses **greybox shapes only**. Sarah's art replaces them later.

## Team

James (code), Sarah (art director), McCotter (QA, files bugs as GitHub issues).

## Design pillars

1. **Cozy first.** Story mode never feels stressful.
2. **Teamwork between plushies.** Every puzzle needs more than one plushie's ability.
3. **Small and shippable.** Small early versions beat scale.
4. **Horror-ready.** Nothing in Story mode should make Horror mode harder later.

## Version 1 target

See [`core-loop.md`](core-loop.md#version-1-done-when). In short: one bedroom room, two plushies,
one team puzzle, a hint icon, playable from a WebGL build.

## Not in version 1

Horror mode, hunter robots, the jump scare, couch co-op, skill trees, backstory, any final art
or audio.
