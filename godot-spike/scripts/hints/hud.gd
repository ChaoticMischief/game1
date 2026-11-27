extends CanvasLayer
## The HUD: the Man in the Moon hint button and hint text, the "room complete" message, a bottom
## status bar naming the selected plushie, and the controls panel (? or F1). Plushie name labels
## live on the plushies themselves (world space), so nothing here projects positions.

@export var room: RoomController
@export var roster: PlushieRoster
## Hint tiers for this room: data, not code, so they can be localized later.
@export var hints: HintSet

var _sequence: HintSequence
@onready var _hint_label: Label = %HintText
@onready var _complete_label: Label = %RoomComplete
@onready var _status: Label = %Status
@onready var _help: Control = %Help


func _ready() -> void:
	_sequence = HintSequence.new(PackedStringArray(hints.tiers) if hints else PackedStringArray())
	%HintButton.pressed.connect(show_next_hint)
	%HelpTitle.text = HudText.HELP_TITLE
	%HelpBody.text = HudText.HELP_BODY
	room.completed.connect(_complete_label.show)
	roster.active_changed.connect(_on_active_changed)


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed(&"hint"):
		show_next_hint()
	elif event.is_action_pressed(&"help"):
		_help.visible = not _help.visible


func _process(_delta: float) -> void:
	if not room.is_complete:
		_complete_label.hide()


func show_next_hint() -> void:
	_hint_label.text = "No hints here." if _sequence.tiers.is_empty() else _sequence.next()
	_hint_label.show()


func _on_active_changed(active: Plushie) -> void:
	_status.text = HudText.selected_line(active.display_name if active else "") \
			+ "     |     " + HudText.CONTROLS_SUMMARY
