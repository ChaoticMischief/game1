class_name Plushie
extends CharacterBody2D
## One plushie's body: moves and jumps from the input it is handed. It never reads input itself;
## PlushieRoster (the "player slot") owns who controls it, so couch co-op can be added later.
## Abilities differ by tuning: the bear is heavy (presses plates) and jumps low, the rabbit is
## light and jumps high.

@export var display_name := "Plushie"
@export var mass := 1.0
@export var move_speed := 256.0
@export var acceleration := 1920.0
@export var jump_height := 64.0
## Input action that makes this the active plushie, e.g. &"select_bear"; empty for none.
@export var select_action: StringName

## Horizontal input in [-1, 1], set by whoever controls this plushie.
var move_input := 0.0
var is_active := false
var _jump_queued := false
var _start_position: Vector2
@onready var _gravity: float = ProjectSettings.get_setting("physics/2d/default_gravity")
@onready var _marker: CanvasItem = $ActiveMarker
@onready var _name_label: Label = $NameLabel


func _ready() -> void:
	_start_position = position
	add_to_group(&"room_resettable")
	_name_label.text = display_name


func request_jump() -> void:
	_jump_queued = true


func set_active(active: bool) -> void:
	is_active = active
	_marker.visible = active
	_name_label.theme_type_variation = &"NameLabelSelected" if active else &"NameLabel"


func reset_state() -> void:
	position = _start_position
	velocity = Vector2.ZERO
	move_input = 0.0
	_jump_queued = false


func _physics_process(delta: float) -> void:
	velocity.x = move_toward(velocity.x, move_input * move_speed, acceleration * delta)
	if is_on_floor():
		if _jump_queued:
			velocity.y = -JumpMath.velocity_for_height(jump_height, _gravity)
	else:
		velocity.y += _gravity * delta
	_jump_queued = false
	move_and_slide()
