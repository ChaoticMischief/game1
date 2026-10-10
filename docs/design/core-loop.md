# Core loop

Status: draft. Values marked *(default)* are proposals nobody has confirmed yet.

## Minute to minute

1. Look at the room (side-on cutaway) and spot the exit and what blocks it.
2. Move the current plushie, using its ability, to reach or change something.
3. Switch to the other plushie to do the part the first one can't.
4. Combine them: a plushie left holding something keeps holding it while you control the other.
5. Reach the exit together. The room is solved.
6. Stuck? Click the Man in the Moon icon for a hint, and again for a stronger one.

## Plushies (version 1)

| Plushie | Role | Abilities *(default)* |
|---|---|---|
| Big strong **bear** | Strength | Pushes and carries heavy objects; its weight holds down pressure plates; walks, jumps low |
| Smaller **rabbit** | Agility | Jumps high; fits through small gaps; light, so it can't press plates or move heavy things |

Rules:
- One plushie is active at a time. Switching is one input (key, button, or tap on the plushie).
- An inactive plushie stays where it is and keeps whatever it is holding or standing on.
- **Who controls a plushie is separate from the plushie itself** (a "controller" or "player slot"
  owns the active plushie), so couch co-op can be added later without a rewrite.
- No health, no timer, no failure in Story mode.

## Version 1 room: the bedroom

One room, one team puzzle, greybox only. It is built by `Game > Build Bedroom Greybox`
(`Assets/Editor/Greybox/BedroomGreyboxBuilder.cs`), and `BedroomPuzzleTests` plays it through.

1. The bear stands on the pressure plate. It is heavy enough (mass 5 vs. 3 needed) and the door
   opens. It must stay there: the door shuts if the plate is released.
2. The rabbit starts behind the bear, hops over it, and goes through the open door.
3. Beyond the door a lever sits on a shelf at 1.6 units. Only the rabbit's jump (2.2) reaches it;
   the bear's (1.0) cannot, but the bear can walk underneath. The lever latches the door open.
4. The bear steps off the plate and follows through the door.
5. Both plushies in the green exit zone completes the room.

Falling out of the world resets the room. Tuning lives in the builder: bear mass 5, speed 4, jump
1.0; rabbit mass 1, speed 5, jump 2.2; plate needs mass 3; lever needs 0.5.

## Hints

- The Man in the Moon is an icon in a screen corner (placeholder shape for now).
- Click, tap, or press a button to open a hint. Each further press gives the next tier.
- Tiers per puzzle: 1) a nudge about where to look, 2) which plushie does what, 3) the full
  solution. Hint text is data, not code, so it can be localized later.
- Hints never block play and cost nothing.

## Version 1: done when

A player can load the WebGL build, finish the bedroom by using both plushies, and ask for a hint.

Supporting requirements:
- Runs from the main scene with no setup.
- Controllable with keyboard and gamepad (Input System actions); touch is optional.
- Also builds for Windows and macOS.
- A "room reset" path exists and is the single place a future "caught" animation will call.

## Horror readiness (checklist for every feature)

- Is world state resettable to the room's start or last checkpoint?
- Is there a single "player was caught" path to hook into later?
- Can lighting, props and the AI's face be swapped per mode?
- Are mode-specific rules behind one mode setting rather than scattered checks?
