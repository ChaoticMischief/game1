extends GdUnitTestSuite
## Unit tests for the scene-free logic, ported from Unity's GameplayLogicTests / BuildInfoTests.


func test_velocity_reaches_requested_height() -> void:
	var v := JumpMath.velocity_for_height(2.0, 19.62)
	assert_float(v * v / (2.0 * 19.62)).is_equal_approx(2.0, 0.0001)


@warning_ignore("unused_parameter")
func test_invalid_input_gives_no_jump(height: float, gravity: float, test_parameters := [[0.0, 9.8], [-1.0, 9.8], [1.0, 0.0]]) -> void:
	assert_float(JumpMath.velocity_for_height(height, gravity)).is_equal(0.0)


func test_selected_line_names_the_plushie() -> void:
	assert_str(HudText.selected_line("Bear")).is_equal("Currently selected plushie: Bear")
	assert_str(HudText.selected_line("")).ends_with("none")


func test_help_mentions_every_control() -> void:
	for word in ["Space", "R:", "B:", "E:", "F1", "Left stick"]:
		assert_str(HudText.HELP_BODY).contains(word)


func test_selection_wraps_both_ways() -> void:
	var s := PlushieSelection.new(3)
	assert_bool(s.next()).is_true()
	s.next()
	s.next()
	assert_int(s.index).is_equal(0)
	assert_bool(s.previous()).is_true()
	assert_int(s.index).is_equal(2)


func test_single_or_empty_roster_never_changes() -> void:
	for count in [0, 1]:
		var s := PlushieSelection.new(count)
		assert_bool(s.next() or s.previous()).is_false()


func test_select_ignores_out_of_range_and_reset_returns_to_first() -> void:
	var s := PlushieSelection.new(2)
	assert_bool(s.select(5)).is_false()
	assert_bool(s.select(1)).is_true()
	s.reset()
	assert_int(s.index).is_equal(0)


func test_heavy_body_presses_light_body_does_not() -> void:
	var plate := PlateState.new(3.0, false)
	plate.add(1, 1.0)
	assert_bool(plate.is_pressed()).is_false()
	plate.add(2, 5.0)
	assert_bool(plate.is_pressed()).is_true()


func test_masses_add_up_and_release_deactivates() -> void:
	var plate := PlateState.new(3.0, false)
	plate.add(1, 2.0)
	plate.add(2, 2.0)
	assert_bool(plate.is_active()).is_true()
	plate.remove(2)
	assert_bool(plate.is_active()).is_false()


func test_latching_plate_stays_active_until_reset() -> void:
	var lever := PlateState.new(0.5, true)
	lever.add(1, 1.0)
	lever.remove(1)
	assert_bool(lever.is_active()).is_true()
	lever.reset()
	assert_bool(lever.is_active()).is_false()


func test_empty_plate_with_zero_requirement_is_not_pressed() -> void:
	assert_bool(PlateState.new(0.0, false).is_pressed()).is_false()


func test_hints_reveal_one_tier_per_request_then_repeat_the_last() -> void:
	var hints := HintSequence.new(PackedStringArray(["a", "b"]))
	assert_str(hints.next()).is_equal("a")
	assert_str(hints.next()).is_equal("b")
	assert_str(hints.next()).is_equal("b")
	hints.reset()
	assert_str(hints.next()).is_equal("a")
	assert_str(HintSequence.new(PackedStringArray()).next()).is_empty()


func test_build_info_overlay_and_sentry_environment() -> void:
	assert_bool(BuildInfo.should_show_overlay("1.0.0.2026.09.28.1a2b3c4d", false, false)).is_false()
	assert_bool(BuildInfo.should_show_overlay("0.1.0.2026.09.28.1a2b3c4d", false, false)).is_true()
	assert_str(BuildInfo.display_text("0.1.0", true)).is_equal("0.1.0 (editor)")
	assert_str(BuildInfo.sentry_environment("1.0.0-dirty", false, false)).is_equal("local")
	assert_str(BuildInfo.sentry_environment("1.0.0", false, false)).is_equal("production")
