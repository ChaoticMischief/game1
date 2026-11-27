extends Label
## Shows the build version in the bottom-left corner so every screenshot and bug report
## identifies the exact build (see BuildInfo.should_show_overlay for when).


func _ready() -> void:
	var version: String = ProjectSettings.get_setting("application/config/version", "")
	var is_editor := OS.has_feature("editor")
	visible = BuildInfo.should_show_overlay(version, OS.is_debug_build(), is_editor)
	text = BuildInfo.display_text(version, is_editor)
