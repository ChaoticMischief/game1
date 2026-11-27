class_name PlushieSelection
extends RefCounted
## Which plushie is active. Wraps around when cycling; knows nothing about scenes or input.

var count: int
var index := 0


func _init(p_count: int) -> void:
	count = maxi(p_count, 0)


## Activates the next plushie, wrapping to the first. Returns true if the active one changed.
func next() -> bool:
	return select(0 if count == 0 else (index + 1) % count)


## Activates the previous plushie, wrapping to the last. Returns true if the active one changed.
func previous() -> bool:
	return select(0 if count == 0 else (index + count - 1) % count)


## Activates plushie `i` if it exists. Returns true if the active one changed.
func select(i: int) -> bool:
	if i < 0 or i >= count or i == index:
		return false
	index = i
	return true


func reset() -> void:
	index = 0
