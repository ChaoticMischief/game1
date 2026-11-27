class_name JumpMath
## Jump physics helpers, kept free of scene state so unit tests cover them.


## Upward launch speed that reaches `height` under `gravity` (both positive).
static func velocity_for_height(height: float, gravity: float) -> float:
	if height <= 0.0 or gravity <= 0.0:
		return 0.0
	return sqrt(2.0 * gravity * height)
