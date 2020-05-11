#Delay value script
extends Label



func _ready():
	$Popup/Edit.connect("text_entered", self, "_on_Edit_text_entered")

func _gui_input(event):
	if event is InputEventMouseButton and event.pressed:
		$Popup/Edit.text = str($"../Slider".value)
		$Popup.popup()
		$Popup.rect_position = get_global_mouse_position()
		$Popup.rect_position.x -= $Popup.rect_size.x
		$Popup/Edit.select_all()


func _on_Edit_text_entered(new_text):
	$"../Slider".value = float(new_text)
#	owner._on_slider_change(int(new_text), $"..".name)
	$Popup.visible = false
	
