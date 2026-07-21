extends Node2D

const BUTTON_COLOR := Color("ffffff")
const BUTTON_ALTERNATE_COLOR := Color("70d6ff")
const COLOR_RECT_COLOR := Color("ffffff")
const COLOR_RECT_ALTERNATE_COLOR := Color("ff70a6")

@onready var button: Button = $Button
@onready var color_rect: ColorRect = $ColorRect

var _button_uses_alternate_color := false
var _color_rect_uses_alternate_color := false


func _ready() -> void:
	PassthroughManager.Initialize(get_window(), null, 7, 1, true)

	PassthroughManager.RegisterControlClickArea(button)
	PassthroughManager.RegisterControlClickArea(color_rect)

	button.pressed.connect(_on_btn_clicked)
	color_rect.mouse_entered.connect(_on_col_rect_entered)
	color_rect.mouse_exited.connect(_on_col_rect_entered)

	# Control 的最终布局会在进入场景树后确定，因此延迟到下一帧执行命中测试。
	_verify_control_click_areas.call_deferred()


func _on_btn_clicked() -> void:
	_button_uses_alternate_color = not _button_uses_alternate_color
	button.self_modulate = (
		BUTTON_ALTERNATE_COLOR if _button_uses_alternate_color else BUTTON_COLOR
	)


func _on_col_rect_entered() -> void:
	_color_rect_uses_alternate_color = not _color_rect_uses_alternate_color
	color_rect.color = (
		COLOR_RECT_ALTERNATE_COLOR
		if _color_rect_uses_alternate_color
		else COLOR_RECT_COLOR
	)


func _verify_control_click_areas() -> void:
	_assert_control_center_is_clickable(button)
	_assert_control_center_is_clickable(color_rect)
	assert(
		not PassthroughManager.HasClickAreaAtPoint(Vector2.ZERO),
		"未注册 Control 的空白区域不应被识别为可点击区域。"
	)
	print("[ControlItem test] Control 点击区域注册及命中检测通过。")


func _assert_control_center_is_clickable(control: Control) -> void:
	var center := control.get_global_rect().get_center()
	assert(
		PassthroughManager.HasClickAreaAtPoint(center),
		"%s 的中心点 %s 应被识别为可点击区域。" % [control.name, center]
	)
