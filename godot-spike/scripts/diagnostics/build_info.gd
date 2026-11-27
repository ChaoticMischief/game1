class_name BuildInfo
## Decisions about showing the build version and which Sentry environment a build reports to,
## kept free of scene state so unit tests cover them.


## Show the version overlay in the editor, debug builds, pre-1.0 versions and builds of
## uncommitted code; hide it in release builds from 1.0 on.
static func should_show_overlay(version: String, is_debug_build: bool, is_editor: bool) -> bool:
	return is_editor or is_debug_build or is_pre_release(version) or is_dirty(version)


static func display_text(version: String, is_editor: bool) -> String:
	return version + " (editor)" if is_editor else version


static func is_pre_release(version: String) -> bool:
	return version.begins_with("0.")


static func is_dirty(version: String) -> bool:
	return version.ends_with("-dirty")


## Sentry environment: "editor", "development", "local", "prerelease" or "production".
static func sentry_environment(version: String, is_debug_build: bool, is_editor: bool) -> String:
	if is_editor:
		return "editor"
	if is_debug_build:
		return "development"
	if is_dirty(version):
		return "local"
	if is_pre_release(version):
		return "prerelease"
	return "production"
