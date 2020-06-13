extends Control
signal envelope_changed


func _ready():
	for o in $V/H.get_children():
		if o.is_in_group("slider"):
			o.get_node("Slider").connect("value_changed", self, "_on_slider_change", [o.name])
			o.get_node("Slider").connect("gui_input", self, "_on_slider_input", [o])


func _on_slider_change(value, which):
	$V/H.get_node(which + "/Val").text = str(value).pad_decimals(1)
	
	_on_envelope_changed()
	
#	if currentEG:
#		currentEG.set(which, value)

func _on_slider_input(ev, which):
	var value = which.get_node("Slider").value
	var press = false
	if ev is InputEventMouseMotion or ev is InputEventMouseButton:
		if Input.is_mouse_button_pressed(BUTTON_LEFT):  press = true
	if press:
#			_on_envelope_changed()
			emit_signal("envelope_changed", value, which.name)
	


func _on_envelope_changed():
	for o in $V/H.get_children():
		$V/PitchDisplay.set(o.name.to_lower(), o.get_node("Slider").value)
	pass
