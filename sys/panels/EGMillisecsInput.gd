#Input popup for Millisecs and custom input for a value slider.
extends Label
export (bool) var positive_only = false #Makes control require positive, nonzero values
export (bool) var disable_step = false  #Forces off the default step of the control when custom input is entered

var step = 1.0  #Default step of the slider.  Automatically set on Ready

func _ready():
	$Popup/Edit.connect("text_entered", self, "_on_Edit_text_entered")

	if disable_step:
		step = $"../Slider".step

func _gui_input(event):
	if event is InputEventMouseButton and event.pressed:
		$Popup/Edit.text = str($"../Slider".value)
		$Popup.popup()
		$Popup.rect_position = get_global_mouse_position()
		$Popup.rect_position.x -= $Popup.rect_size.x
		$Popup/Edit.select_all()


func _on_Edit_text_entered(new_text):
	var val = float(new_text)
	if positive_only and val <= 0:
		return
	else:
		if disable_step:
			#Re-enable step if multiple of original step, else disable step.
			$"../Slider".step = step if fmod(val, step) == 0 else 0.01

		$"../Slider".value = val
	#	owner._on_slider_change(int(new_text), $"..".name)
		$Popup.visible = false
	
