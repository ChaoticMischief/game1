class_name GameLog
## The project's logging entry point: every line is tagged with a category ("Player", "Save", ...)
## so logs can be filtered. Godot's print/push_error go to the console and the log file; the
## sentry-godot SDK picks up push_error/push_warning and script errors on its own.


static func info(category: String, message: String) -> void:
	print("[%s] %s" % [category, message])


static func warning(category: String, message: String) -> void:
	push_warning("[%s] %s" % [category, message])


static func error(category: String, message: String) -> void:
	push_error("[%s] %s" % [category, message])
