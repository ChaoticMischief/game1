extends GdUnitTestSuite
## Plays the version 1 bedroom through in the real scene with real physics, like Unity's
## BedroomPuzzleTests: the bear holds the plate, the rabbit hops over it, goes through the door and
## throws the lever, then the bear joins the rabbit at the exit.

const TIMEOUT_MS := 8000
const PX := 64.0

var _runner: GdUnitSceneRunner
var _room: RoomController
var _bear: Plushie
var _rabbit: Plushie
var _door: Door


func before_test() -> void:
	_runner = scene_runner("res://scenes/main.tscn")
	_room = _runner.find_child("Bedroom")
	_bear = _runner.find_child("Bear")
	_rabbit = _runner.find_child("Rabbit")
	_door = _runner.find_child("Door")
	await _runner.simulate_frames(5)


func test_r_and_b_select_the_rabbit_and_the_bear() -> void:
	var roster: PlushieRoster = _runner.find_child("PlushieRoster")
	assert_object(roster.active()).is_same(_bear)
	_runner.simulate_key_pressed(KEY_R)
	await _runner.await_input_processed()
	assert_object(roster.active()).is_same(_rabbit)
	assert_str(_runner.find_child("Status").text).starts_with("Currently selected plushie: Rabbit")
	_runner.simulate_key_pressed(KEY_B)
	await _runner.await_input_processed()
	assert_object(roster.active()).is_same(_bear)


func test_hint_button_reveals_tiers() -> void:
	var label: Label = _runner.find_child("HintText")
	assert_bool(label.visible).is_false()
	_runner.simulate_key_pressed(KEY_E)
	await _runner.await_input_processed()
	assert_bool(label.visible).is_true()
	assert_str(label.text).contains("Something heavy")


func test_both_plushies_can_escape_the_bedroom() -> void:
	var roster: PlushieRoster = _runner.find_child("PlushieRoster")
	roster.set_physics_process(false) # we drive the plushies ourselves
	roster.set_process_unhandled_input(false)

	# 1. The bear walks onto the plate and holds the door open.
	await _walk_to(_bear, -3.0)
	await _wait_until(func() -> bool: return _door.is_open, "door never opened with the bear on the plate")

	# 2. The rabbit hops over the bear and walks to just before the shelf.
	await _walk_to(_rabbit, -5.5)
	_rabbit.move_input = 1.0
	_rabbit.request_jump()
	await _wait_until(func() -> bool: return _rabbit.position.x > -1.0 * PX, "rabbit never got over the bear")
	await _walk_to(_rabbit, 3.0)

	# 3. The rabbit jumps onto the shelf and throws the lever.
	_rabbit.move_input = 1.0
	_rabbit.request_jump()
	await _wait_until(func() -> bool: return _rabbit.position.y < -2.0 * PX, "rabbit never reached the shelf")
	await _walk_to(_rabbit, 6.0)

	# 4. The bear steps off the plate: the lever keeps the door open, and the bear walks through.
	await _walk_to(_bear, 9.0)
	assert_bool(_door.is_open).override_failure_message("door closed after the bear left the plate").is_true()

	# 5. The rabbit joins the bear at the exit.
	await _walk_to(_rabbit, 9.0)
	await _wait_until(func() -> bool: return _room.is_complete, "room never completed")
	assert_bool(_runner.find_child("RoomComplete").visible).is_true()


func test_falling_out_of_the_world_resets_the_room() -> void:
	var start := _rabbit.position
	_rabbit.position = Vector2(0, 7.5 * PX)
	await _wait_until(func() -> bool: return _rabbit.position.distance_to(start) < 4.0, "room never reset")


func _walk_to(plushie: Plushie, x_units: float) -> void:
	var x := x_units * PX
	var deadline := Time.get_ticks_msec() + TIMEOUT_MS
	while absf(plushie.position.x - x) > 0.15 * PX:
		assert_int(Time.get_ticks_msec()).override_failure_message(
			"%s never reached x=%s (at %s)" % [plushie.display_name, x_units, plushie.position / PX]).is_less(deadline)
		if is_failure():
			return
		plushie.move_input = signf(x - plushie.position.x)
		await get_tree().physics_frame
	plushie.move_input = 0.0
	await _runner.simulate_frames(18)


func _wait_until(condition: Callable, failure: String) -> void:
	var deadline := Time.get_ticks_msec() + TIMEOUT_MS
	while not condition.call():
		assert_int(Time.get_ticks_msec()).override_failure_message(failure).is_less(deadline)
		if is_failure():
			return
		await get_tree().physics_frame
