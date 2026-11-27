class_name PlushieRoster
extends Node
## The player slot: reads the input actions and hands them to the active plushie. Inactive
## plushies get no input, so they keep standing on whatever they were on.

## Emitted whenever the active plushie changes (including on start and room reset).
signal active_changed(active: Plushie)

@export var plushies: Array[Plushie] = []

var selection: PlushieSelection


func _ready() -> void:
	selection = PlushieSelection.new(plushies.size())
	add_to_group(&"room_resettable")
	_apply_active.call_deferred()


func active() -> Plushie:
	return null if plushies.is_empty() else plushies[selection.index]


func _unhandled_input(event: InputEvent) -> void:
	var changed := false
	if event.is_action_pressed(&"next_plushie"):
		changed = selection.next()
	elif event.is_action_pressed(&"previous_plushie"):
		changed = selection.previous()
	for i in plushies.size():
		var action := plushies[i].select_action
		if not action.is_empty() and event.is_action_pressed(action):
			changed = selection.select(i) or changed
	if changed:
		_apply_active()
	if event.is_action_pressed(&"jump") and active():
		active().request_jump()


func _physics_process(_delta: float) -> void:
	if active():
		active().move_input = Input.get_axis(&"move_left", &"move_right")


func reset_state() -> void:
	selection.reset()
	_apply_active()


func _apply_active() -> void:
	for i in plushies.size():
		plushies[i].set_active(i == selection.index)
		if i != selection.index:
			plushies[i].move_input = 0.0
	active_changed.emit(active())
