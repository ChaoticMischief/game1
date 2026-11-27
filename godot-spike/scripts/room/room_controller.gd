class_name RoomController
extends Node2D
## Owns a room's lifecycle. reset_room() is the single path that puts everything back to its start:
## falling out of the world uses it now, and the Horror-mode "caught" animation will end in it.
## Anything under this node in the "room_resettable" group gets reset_state() called.

signal completed

var is_complete := false


func complete() -> void:
	if is_complete:
		return
	is_complete = true
	completed.emit()


func reset_room() -> void:
	is_complete = false
	for node in get_tree().get_nodes_in_group(&"room_resettable"):
		if is_ancestor_of(node):
			node.reset_state()


func plushie_count() -> int:
	return find_children("*", "Plushie", true, false).size()
