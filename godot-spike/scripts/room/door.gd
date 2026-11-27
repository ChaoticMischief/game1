class_name Door
extends StaticBody2D
## A door that is open while any of its plates is active (held down, or latched like a lever).

@export var plates: Array[PressurePlate] = []
@export var open_height := 256.0
@export var speed := 384.0

var is_open := false
var _closed_position: Vector2
@onready var _shape: CollisionShape2D = $CollisionShape2D


func _ready() -> void:
	_closed_position = position
	add_to_group(&"room_resettable")


func _physics_process(delta: float) -> void:
	is_open = plates.any(func(plate: PressurePlate) -> bool: return plate.is_active())
	var target := _closed_position + (Vector2.UP * open_height if is_open else Vector2.ZERO)
	position = position.move_toward(target, speed * delta)
	_shape.disabled = is_open


func reset_state() -> void:
	position = _closed_position
	is_open = false
	_shape.set_deferred(&"disabled", false)
