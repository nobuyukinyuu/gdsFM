tool
class_name ResponseCurvePreviewButton
extends Button


export (bool) var use_log setget set_use_log

func set_use_log(val):
	use_log = val
	$P/Preview/LinLog.visible = val


func _ready():
	_on_ResponseButton_resized()

	$Popup/ResponseCurve/chkLinlog.connect("toggled", self, "_on_linlog_toggle")
	
	$Popup/ResponseCurve.title = text


func _on_ResponseButton_resized():
	$Lbl.text = text
	
	$P.rect_position.x = rect_size.x/2 - $P.rect_size.x/2
	$Lbl.rect_position.x = rect_size.x/2 - $Lbl.rect_size.x/2
	pass # Replace with function body.


func _on_ResponseButton_pressed():
	var pos = get_global_mouse_position() - Vector2($Popup.rect_size.x * 0.8, 0)
#	pos.x -= $Popup/ResponseCurve.rect_size.x
	$Popup.popup(Rect2(pos, $Popup.rect_size))

func _on_linlog_toggle(val):
	set_use_log(val)

func fetch_table(envelope, target:String):
	var new_table:Dictionary = $Popup/ResponseCurve.fetch_table(envelope, target)
	if !new_table:  print("PreviewButton.gd:  Something went wrong trying to fetch the rTable.....")
	$P/Preview.refresh_all(new_table.get("values"))
	set_use_log(new_table.get("use_log", false))

	return new_table

