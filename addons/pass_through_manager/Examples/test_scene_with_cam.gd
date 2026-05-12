extends Node2D

@export var cam: Camera2D = null

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	PassthroughManager.Initialize(get_window(), cam, 7, 1, true)
	pass # Replace with function body.

var start_screen_pos: Vector2
var start_cam_pos: Vector2
var is_dragging: bool = false
var is_dragging_dir: bool = false
var drag_start_world_pos: Vector2

func _input(event: InputEvent) -> void:
	_set_camera(event)
	pass

func _set_camera(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			cam.zoom *= 1.1
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			cam.zoom /= 1.1
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			if event.pressed:
				start_screen_pos = get_viewport().get_mouse_position()
				start_cam_pos = cam.global_position
				is_dragging = true
			else:
				is_dragging = false
	if event is InputEventMouseMotion and event.button_mask & MOUSE_BUTTON_MASK_RIGHT:
		if not is_dragging:
			return
		var current_screen_pos: Vector2 = get_viewport().get_mouse_position()
		var screen_delta: Vector2 = current_screen_pos - start_screen_pos
		cam.global_position = start_cam_pos - screen_delta / cam.zoom
