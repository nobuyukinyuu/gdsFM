extends Label

var semitone_or_mult = 0  #Set to 1 for multiplier instead of semitone
var labels = ["Semitone:", "Multiplier:"]

func _gui_input(event):
	if event is InputEventMouseButton and event.pressed:
		$Popup.popup()
		$Popup.rect_position = get_global_mouse_position()
#		$Popup.rect_position.x -= $Popup.rect_size.x

		$Popup.rect_position -= Vector2(8, $Popup.rect_size.y -12)

		$Popup/Grid/Slider.grab_focus()

		

func _on_OK_pressed():
	var output
	if semitone_or_mult:
		output = $Popup/Grid/Slider.value
	else:
		output = pow(2, $Popup/Grid/Slider.value / 12.0) -1

	$"../Slider".value = output * 100

#	$Popup.hide()


func _on_Popup_popup_hide():
	$Popup/Grid/Slider.value = 0


func _on_Slider_value_changed(value):
	$Popup/Grid/lblValue.text = str(value)


func _on_Slider_gui_input(event):
	if event is InputEventKey and event.scancode == KEY_ENTER:
		_on_OK_pressed()


func _on_Label_gui_input(event):
	if event is InputEventMouseButton and event.pressed:
		semitone_or_mult = 1-semitone_or_mult
		$Popup/Grid/Label.text = labels[semitone_or_mult]
