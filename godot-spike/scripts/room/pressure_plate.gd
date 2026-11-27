class_name PressurePlate
extends Area2D
## Counts the mass of bodies standing in it. A heavy plushie (the bear) holds a plate down; a light
## one (the rabbit) does not. With `latches` it is a lever: the first press sticks until the room
## resets.

@export var required_mass := 3.0
@export var latches := false
@export var idle_color := Color(0.55, 0.35, 0.35)
@export var active_color := Color(0.9, 0.75, 0.2)

var state: PlateState


func _ready() -> void:
	state = PlateState.new(required_mass, latches)
	add_to_group(&"room_resettable")
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)
	_refresh()


func is_active() -> bool:
	return state.is_active()


func reset_state() -> void:
	state.reset()
	_refresh()


func _on_body_entered(body: Node2D) -> void:
	var mass = body.get(&"mass")
	if mass != null:
		state.add(body.get_instance_id(), mass)
		_refresh()


func _on_body_exited(body: Node2D) -> void:
	state.remove(body.get_instance_id())
	_refresh()


func _refresh() -> void:
	modulate = active_color if state.is_active() else idle_color
