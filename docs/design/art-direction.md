# Art direction: Plushie Escape (working title)

Status: draft. Sarah is the art director; she owns every decision in this file. Values marked
*(default)* are proposals nobody has confirmed. Sections marked **TODO (Sarah)** are hers to fill
in; they are deliberately left open rather than guessed. The technical side (sizes, formats,
folders) is in [`art-spec.md`](art-spec.md). The game itself is described in
[`overview.md`](overview.md).

## Rules that don't change

- **Human-made art only.** Everything that ships is drawn, painted or modelled by a person. No
  AI-generated art in the game, not even as a placeholder: use greybox shapes or rough sketches.
  The audience includes the furry community, which is strongly against AI art.
- **Nothing generated goes in this repository or in any build**, and no AI output is traced or
  painted over.
- **Free forever.** The game is a learning project and always free; art is still all rights
  reserved (see [`CONTENT-LICENSE.md`](../../CONTENT-LICENSE.md)).
- **Credit and reference.** Reference images are for your own board, not for the repo. If you
  use another artist's work as direct inspiration, say who in the PR.

## The feel

Sarah's direction (from [`overview.md`](overview.md)): **super duper cozy, very warm and comfy,
Beatrix Potter illustration feel.** Think soft watercolour and ink, gentle muted colours, small
domestic details, animals that look lived-in and loved. The world is a 2D side-view dollhouse
cutaway: we see into a room as if the front wall were removed.

Story mode is a cozy daydream, a slumber party. Horror mode is the nightmare version of the same
house. Same rooms, different light and props (see [Two modes](#two-modes)).

## The cast

| Character | Notes | Status |
|---|---|---|
| Bear plushie | First playable plushie | TODO (Sarah): design sketch |
| Rabbit plushie | First playable plushie | TODO (Sarah): design sketch |
| Man in the Moon | The house AI's friendly face: an old, bearded, sleepy crescent profile. Appears as a small icon in a screen corner; tapping it gives hints. | Concept sketches exist (Sarah); TODO: final design |
| The girl | Owner of the bedroom, aged somewhere between 6 and 11. May appear only as a presence (shadow, photo) in version 1. | TODO (Sarah): decide how visible she is |
| Horror face and robots | The AI's predator face and the family's helper robots. Not in version 1. | Parked; see `backlog.md` |

Plushies are *plushies*: stitched seams, soft shapes, button or bead eyes, visible fabric. They
move like toys that have come alive, not like cartoon animals. *(default; Sarah to confirm.)*

## Rooms

- Version 1 is one room: the girl's bedroom. Later rooms are in the same childhood home, which
  also has near-future smart gadgets (screens, speakers, robot vacuum). In Story mode these look
  friendly and fit in; they must not look threatening.
- Each room is built from separate layers (back wall, furniture, foreground) so lighting and
  props can change per mode and so things can be added without repainting the room.

## Two modes

Art has to be swappable between Story and Horror without redrawing the room. Plan for it now:

- **Lighting:** the room's light is a separate layer or lighting setup, not painted into the
  background. Story: warm lamp and daylight. Horror: cold, dim, long shadows. *(default)*
- **Props:** props that differ per mode (a nightlight vs. a flickering screen, a teddy-bear
  poster vs. a missing poster) are separate sprites on their own layers.
- **Palette shift:** the Story palette and the Horror palette are two variations of one set of
  colours, so the house is clearly the same house.
- Version 1 is **Story only**; draw nothing for Horror yet, but keep the layers separable.

## Style guide

All of these are for Sarah to decide. Until she does, do not invent them.

- **TODO (Sarah): palette.** Main colours for Story mode, accent colours, and how Horror shifts
  them. Suggested format: a swatch image plus hex codes in this file.
- **TODO (Sarah): line.** Outlines or no outlines? Line weight at the reference size (see
  [`art-spec.md`](art-spec.md#sizes)), line colour (black, or a darker shade of the fill?).
- **TODO (Sarah): shape language.** Soft and round for plushies and the cosy home; what do
  the smart gadgets look like next to them?
- **TODO (Sarah): texture and rendering.** Flat colour, watercolour texture, fabric texture?
  How much texture survives at game size?
- **TODO (Sarah): references.** A short list of illustrators, books and games with a sentence
  each on what to take from them. Reference images stay on your own board (for example PureRef),
  not in the repo.
- **TODO (Sarah): animation feel.** Bouncy, floppy, stiff-toy? Which actions need art in
  version 1 (idle, walk, jump, pick up)?
- **TODO (Sarah): UI look.** HUD, the hint icon, help panel, fonts (free-to-redistribute fonts
  only).

## Greybox replacement

Version 1 is built from **greybox** shapes (plain coloured shapes, `Assets/Art/Greybox/`), so
gameplay can be tested before any art exists. Replacing them with real art goes like this:

1. Pick one object (say the bear) and agree its size and role with James: how big in the room
   (see [`art-spec.md`](art-spec.md#sizes)), what it does, which animations it needs.
2. Sarah sketches it, then paints it following the [art spec](art-spec.md).
3. The art is added to the project (see the PR checklist in the spec); the greybox object is
   swapped for the real sprite. The object's size and collider stay as they were, so puzzles
   don't break. If the art needs a different shape, say so in the PR and James adjusts.
4. The greybox shape stays available until the real art is merged.

Order of replacement *(default)*: the bear and rabbit, then the bedroom background and
furniture, then the Man in the Moon icon, then UI.

## Contributing art

Art contributors need to agree that ChaoticMischief may use their art in the game without
restriction; see [`CONTRIBUTING.md`](../../CONTRIBUTING.md#licensing-of-contributions). Setting up
a Windows machine for this is one script (see the README).

## Open questions

- Is the title "Plushie Escape" staying? The logo and title lettering wait on that.
- How much of the girl do we see?
- TODO (Sarah): anything else the style needs before art production starts.
