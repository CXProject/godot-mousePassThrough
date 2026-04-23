extends Sprite2D

var is_dragging = false
var initial_position = Vector2.ZERO
var initial_mouse_position = Vector2.ZERO
@onready var collision_polygon_2d: CollisionPolygon2D = $Area2D/CollisionPolygon2D
var last_global_position = Vector2.ZERO
var last_global_scale = Vector2.ZERO
func _ready() -> void:
	PassthroughManager.RegisterCollisionPolygon2DClickArea(collision_polygon_2d);
	last_global_position = global_position
	last_global_scale = global_scale

	var left_click = InputEventMouseButton.new()
	left_click.button_index = MOUSE_BUTTON_LEFT
	if !InputMap.has_action("click"):
		InputMap.add_action("click")
	InputMap.action_add_event("click", left_click)

func _process(delta: float) -> void:
	if Input.is_action_just_pressed("click") and is_mouse_over():
		is_dragging = true
		initial_position = global_position
		initial_mouse_position = get_global_mouse_position()
	
	if is_dragging:
		if Input.is_action_just_released("click"):
			is_dragging = false
		
		global_position = get_global_mouse_position() - initial_mouse_position + initial_position

	# 判定形状是否变化
	if global_position != last_global_position or global_scale != last_global_scale:
		last_global_position = global_position
		last_global_scale = global_scale
		PassthroughManager.UpdateClickArea(collision_polygon_2d);

func is_mouse_over() -> bool:
	if get_global_mouse_position().x >= global_position.x - get_rect().size.x / 2 and get_global_mouse_position().x <= global_position.x + get_rect().size.x / 2:
		if get_global_mouse_position().y >= global_position.y - get_rect().size.y / 2 and get_global_mouse_position().y <= global_position.y + get_rect().size.y / 2:
			return true
	return false
