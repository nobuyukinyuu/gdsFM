extends Button
var step = 1.0  #Default step of the slider.  Automatically set on Ready
var slider:Slider
var popup:Popup

func _ready():
	connect("pressed", self, "_on_Button_pressed")

	yield(get_tree(), "idle_frame")
	slider = owner.get_node("H/Mul/Slider")
	
	step = slider.step
	popup = $"../.."
	
func _on_Button_pressed():
	#Ratio is in our text property.  Set mult based on ratio.
	
	var popup= $"../.."
	var val = float(text)


	#Re-enable step if multiple of original step, else disable step.
	slider.step = step if fmod(val, step) == 0 else 0.01

	slider.value = val
#	owner._on_slider_change(int(new_text), $"..".name)
	popup.visible = false

