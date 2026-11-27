class_name PlateState
extends RefCounted
## Pressure plate logic, keyed by whatever identifies a body (its instance id): bodies standing on
## it add their mass, and it counts as pressed once the total reaches required_mass. A latching
## plate (a lever) stays latched after its first press until reset.

var required_mass: float
var latches: bool
var total_mass := 0.0
var latched := false
var _masses := {}


func _init(p_required_mass: float, p_latches: bool) -> void:
	required_mass = p_required_mass
	latches = p_latches


func is_pressed() -> bool:
	return not _masses.is_empty() and total_mass >= required_mass


## True while the plate is holding whatever it controls open.
func is_active() -> bool:
	return is_pressed() or latched


func add(id: int, mass: float) -> void:
	_masses[id] = mass
	_recalculate()


func remove(id: int) -> void:
	if _masses.erase(id):
		_recalculate()


func reset() -> void:
	_masses.clear()
	total_mass = 0.0
	latched = false


func _recalculate() -> void:
	total_mass = 0.0
	for mass in _masses.values():
		total_mass += mass
	if latches and is_pressed():
		latched = true
