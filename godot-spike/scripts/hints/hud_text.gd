class_name HudText
## The words shown on the HUD, kept out of the scene so tests can cover them.

const CONTROLS_SUMMARY := "A / D move   Space jump   R rabbit   B bear   E hint   ? all controls"
const HELP_TITLE := "Controls"
const HELP_BODY := """Keyboard
  A / D or the arrow keys: move
  Space: jump
  R: select the rabbit    B: select the bear
  1 / 2: previous / next plushie
  E: hint (or click the Man in the Moon)
  ? or F1: show / hide this help

Gamepad
  Left stick: move
  A (south button): jump
  D-pad left / right: switch plushie
  Y (north button): hint
  Back / View: show / hide this help

Tip: one plushie can hold a plate down while the other goes on."""


static func selected_line(plushie_name: String) -> String:
	return "Currently selected plushie: " + (plushie_name if not plushie_name.is_empty() else "none")
