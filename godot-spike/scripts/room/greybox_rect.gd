@tool
class_name GreyboxRect
extends Node2D
## Placeholder art: a tinted rectangle centred on the node, drawn in the editor too. Sarah's
## sprites replace these (swap the node for a Sprite2D).

@export var size := Vector2(64, 64):
	set(value):
		size = value
		queue_redraw()
@export var color := Color.WHITE:
	set(value):
		color = value
		queue_redraw()


func _draw() -> void:
	draw_rect(Rect2(-size / 2, size), color)
