class_name ExitZone
extends Area2D
## Completes the room once every plushie in it is inside this area.

@export var room: RoomController

var _inside := {}


func _ready() -> void:
	body_entered.connect(func(body: Node2D) -> void:
		if body is Plushie:
			_inside[body] = true
			if _inside.size() >= room.plushie_count():
				room.complete())
	body_exited.connect(func(body: Node2D) -> void: _inside.erase(body))
