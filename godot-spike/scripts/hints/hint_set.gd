class_name HintSet
extends Resource
## One puzzle's hint tiers, as a data file (rooms/*.tres) so they can be localized later.

## Array[String], not PackedStringArray: Godot 4.7.2 drops script-exported Packed*Array values
## when it converts resources to binary on export (see FINDINGS.md).
@export_multiline var tiers: Array[String] = []
