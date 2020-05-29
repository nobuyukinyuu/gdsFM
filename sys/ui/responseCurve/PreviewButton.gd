tool
extends Button

export (bool) var use_log setget set_use_log

func set_use_log(val):
	use_log = val
	$P/Preview/LinLog.visible = val


func _ready():
	_on_ResponseButton_resized()

	$Popup/ResponseCurve/chkLinlog.connect("toggled", self, "_on_linlog_toggle")


func _on_ResponseButton_resized():
	$Lbl.text = text
	
	$P.rect_position.x = rect_size.x/2 - $P.rect_size.x/2
	$Lbl.rect_position.x = rect_size.x/2 - $Lbl.rect_size.x/2
	pass # Replace with function body.


func _on_ResponseButton_pressed():
	var pos = get_global_mouse_position()
#	pos.x -= $Popup/ResponseCurve.rect_size.x
	$Popup.popup(Rect2(pos, $Popup.rect_size))

func _on_linlog_toggle(val):
	set_use_log(val)
