# Art spec

Technical requirements for art that goes into the game. For *what* the art should look like, see
[`art-direction.md`](art-direction.md). Values marked *(default)* are proposals nobody has
confirmed; change them here (and tell James) when you learn better. Everything here assumes the
art is human-made (see the art direction's rules).

## Sizes

The game is 2D side view. The reference screen is **1920 x 1080** *(default)*; the camera fits the
room to any window, so art is drawn larger than it's shown and scaled down.

| Setting | Value |
|---|---|
| Pixels per unit (PPU) | **100** *(default)*: 100 px of sprite = 1 world unit. Set on import. |
| Colour space | **sRGB**, 8 bits per channel. Don't use Display P3 or CMYK. |
| Resolution rule | Draw at **2x the on-screen size** *(default)* so art stays sharp on high-resolution screens; the importer scales it down. |

Canvas sizes by asset type *(default; all multiples of 4 so textures compress cleanly)*:

| Asset | Canvas | Format |
|---|---|---|
| Room background (one layer) | 3840 x 2160 px, or less if the layer doesn't fill the room | PNG, or layered PSD for source |
| Character (bear, rabbit) | about 512 x 512 px canvas for a body about 400 px tall | **Layered PSD** (one layer per body part) |
| Prop / furniture | as small as fits, up to 1024 px on the long side | PNG with transparency |
| UI icon (hint icon, buttons) | 256 x 256 px | PNG with transparency |
| UI panel | up to 1024 px | PNG with transparency |

How big things are in the room is agreed per object with James (see greybox replacement in the
art direction). Keep the same ratio as the greybox shape it replaces.

## File formats

- **Game-ready (what the game reads):** `.png`, transparent background, straight (not
  premultiplied) alpha, sRGB, no embedded colour profile other than sRGB. Don't leave a white or
  coloured fringe around soft edges.
- **Characters to be animated:** a **layered `.psd`**, one layer per part that moves
  (head, ears, each arm, each leg, body, tail, eyes, mouth), no flattened copy, no layer
  effects or blend modes other than Normal, layers named as in [Naming](#naming). Unity's PSD
  importer turns the layers into sprites and a bone-rig skeleton. See
  [Layered PSDs to rigged characters](#layered-psds-to-rigged-characters).
- **Anything else:** PNG. Don't commit JPG for anything with transparency.
- Don't commit `.procreate` files (see [Source files and Git LFS](#source-files-and-git-lfs)).

### Exporting from Procreate

1. **Characters:** Actions (spanner) > Share > **PSD**. Procreate keeps layers, names and
   opacity. Before exporting: name every layer, hide guide and sketch layers or delete them,
   merge layers that don't move independently, set blend modes to Normal, and make sure the
   canvas is **RGB (sRGB)**. (Canvas > Canvas information > Color profile.)
2. **Props, backgrounds, icons:** hide the paper-colour layer (or Background colour off), then
   Actions > Share > **PNG** (keeps transparency). Check the corners are transparent.
3. Create the canvas at its final size from the table above (Canvas > Canvas information shows
   the size and the maximum layer count). The big room backgrounds may limit how many layers an
   iPad can hold; draw them in horizontal bands or smaller layers if Procreate runs out.
4. Send the file to the Windows machine (AirDrop to iCloud Drive, or Files > share), then add
   it to the project as described in [PR checklist](#pull-request-checklist-for-art).

Krita (installed by the Windows setup script) can open and tidy PSDs, trim canvases and export
PNGs if Procreate isn't enough.

## Naming

Lowercase, words joined with `_`, no spaces or accents, no version numbers in the file name
(Git is the history). Pattern *(default)*:

```text
<category>_<name>[_<variant>].<ext>
```

- Characters: `char_bear.psd`, `char_rabbit.psd`. Layers inside: `head`, `ear_l`, `ear_r`,
  `arm_l`, `arm_r`, `leg_l`, `leg_r`, `body`, `tail`, `eyes`, `mouth`, ... (`_l` / `_r` is the
  character's own left and right).
- Props: `prop_lamp.png`, `prop_bed.png`, `prop_nightlight_story.png`, `prop_nightlight_horror.png`
- Room layers: `room_bedroom_back.png`, `room_bedroom_furniture.png`, `room_bedroom_front.png`,
  `room_bedroom_light_story.png`, `room_bedroom_light_horror.png`
- UI: `ui_hint_icon.png`, `ui_button_help.png`

Variants for the two modes use the `_story` / `_horror` suffix. Mode-specific lighting and props
must be separate files, never painted into the shared layers.

## Folder layout

Under `Assets/Art/` *(default)*. Keep **source** (layered, high-resolution) separate from
**game-ready** (what Unity should use directly):

```text
Assets/Art/
  Greybox/                 placeholder shapes (exists already)
  Characters/
    Bear/                  char_bear.psd (layered; this is also the game file)
    Rabbit/
    MoonMan/
  Props/                   one PNG per prop, subfolders by room if it grows
  Rooms/
    Bedroom/               room_bedroom_*.png (back, furniture, front, light_story, light_horror)
  UI/                      ui_*.png
  Source/                  layered working files too big or messy for the game (PSD, KRA).
                           Not read by the game; excluded from builds because nothing references it.
```

Unity creates a `.meta` file next to every asset; **commit it together with the asset** and never
rename or move assets outside Unity (or without their `.meta`), or the references to them break.

## Layered PSDs to rigged characters

Characters are animated with bones, not frame by frame: a layered PSD is imported with Unity's
**2D PSD Importer**, which makes one sprite per layer, and the **2D Animation** package adds a
skeleton, weights the sprites to bones and animates them. That keeps the art small and lets one
set of drawings do every animation.

For this to work: one layer per moving part, parts drawn *whole* (draw the full arm even where the
body covers it) so nothing shows a gap when it rotates, a neutral pose (arms slightly out, so the
parts don't overlap too much), and no merged layers. James and Sarah set up the first character
together.

> **Follow-up, not in this PR:** the packages (`com.unity.2d.psdimporter`, `com.unity.2d.animation`)
> aren't in `Packages/manifest.json` yet. By the project rule they are added in the PR that first
> uses them, with a test plushie. Tracked in a GitHub issue.

## WebGL limits

WebGL is a nice-to-have target but is kept in scope, so budget for it *(default)*:

- Textures: **at most 2048 x 2048** for characters, props and UI; room backgrounds up to
  **4096 x 4096** per layer, and only a few of them. Powers of two aren't required, multiples of
  4 are.
- Memory: an uncompressed 4096 x 4096 RGBA texture is 64 MB in memory. The build compresses
  textures, but the browser tab still has a limited memory budget (aim for the whole game under
  **about 500 MB**). Prefer more small layers over fewer huge ones, and trim transparent borders.
- Don't ship giant flat areas: a plain-colour 4096 px layer should be a small tiled or stretched
  image instead.
- Many tiny props should be packed into a sprite atlas (James sets this up).
- Fewer layers on screen helps speed; ask before adding full-screen layers with transparency.

## Source files and Git LFS

`.gitattributes` stores `.png`, `.jpg`, `.psd`, `.psb`, `.tga`, `.tif`, `.exr`, `.aseprite` and other
binary types in **Git LFS**, so large images don't bloat the repository. `.procreate`, `.kra`, `.ai`
and `.clip` aren't listed. **Don't commit `.procreate` masters**: they're large (hundreds of MB),
iPad-only and can't be merged. Keep them in iCloud Drive or another backup you control, and
commit the exported PSD or PNG. If the team wants the masters in the repository after all,
the fix is adding `*.procreate filter=lfs diff=lfs merge=lfs -text` (and `*.kra`) to
`.gitattributes` in a separate PR; mind the free LFS storage and bandwidth quota on GitHub.

## Pull request checklist for art

GitHub Desktop is enough: create a branch (Branch > New branch, `art/<what>`), copy the files into
the project folder, open the project in Unity once so it creates the `.meta` files, review the
changes, commit and push, then open a pull request.

- [ ] Human-made; no AI-generated or AI-assisted images, and no traced or copied work.
- [ ] Right folder and file name from this spec; `.meta` files are included.
- [ ] sRGB, correct size, PPU 100 (or the importer settings noted in the PR).
- [ ] Transparent where it should be: no white fringe, corners transparent.
- [ ] Characters: layered PSD, layers named, one per moving part, neutral pose.
- [ ] Textures within the [WebGL limits](#webgl-limits).
- [ ] Mode-specific art (lighting, props) is on separate files.
- [ ] Looks right in the Unity Scene view (and swapped for its greybox if it replaces one).
- [ ] Nothing else is in the PR: no `.procreate` files, no `Library/`, no personal files.
- [ ] CI passes.

## Contributor art agreement

Game art is all rights reserved by ChaoticMischief (see
[`CONTENT-LICENSE.md`](../../CONTENT-LICENSE.md)). By opening a pull request that adds art you
confirm that you made it, and that ChaoticMischief may use it in the game without restriction, as
[`CONTRIBUTING.md`](../../CONTRIBUTING.md#licensing-of-contributions) says. The project owner
handles any separate written agreement with regular art contributors.
