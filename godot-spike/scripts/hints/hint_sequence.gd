class_name HintSequence
extends RefCounted
## Tiered hints for one puzzle, shown one tier per request: a nudge, then who-does-what, then the
## solution. Asking again after the last tier repeats it. Hint text is data supplied by the caller.

var tiers: PackedStringArray
## How many tiers have been revealed so far.
var shown := 0


func _init(p_tiers: PackedStringArray) -> void:
	tiers = p_tiers


func has_more() -> bool:
	return shown < tiers.size()


## Reveals and returns the next tier, or the last one again once all are shown. Empty if none.
func next() -> String:
	if tiers.is_empty():
		return ""
	if has_more():
		shown += 1
	return tiers[shown - 1]


func reset() -> void:
	shown = 0
