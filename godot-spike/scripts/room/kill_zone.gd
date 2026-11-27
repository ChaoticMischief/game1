class_name KillZone
extends Area2D
## Resets the room when a plushie leaves the playable area (falls out of the world).

@export var room: RoomController


func _ready() -> void:
	body_entered.connect(func(body: Node2D) -> void:
		if body is Plushie:
			room.reset_room.call_deferred())
