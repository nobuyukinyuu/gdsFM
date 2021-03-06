extends Tabs

#var global.currentEG

signal envelope_changed

func _ready():
	for o in [$A, $S, $D, $R]:
		o.get_node("Slider").connect("value_changed", self, "_on_slider_change", [o.name])
		o.get_node("Slider").connect("gui_input", self, "_on_slider_input", [o])

		o.get_node("Val").modulate.a = 0.5


func load_settings(envelope):
	global.currentEG = envelope
	
	for o in ["A", "D", "S", "R"]:
		var node = get_node(o + "/Slider")
		node.value = envelope.get(o + "c")



func _on_slider_change(value, which):
	get_node(which + "/Val").text = str(value).pad_decimals(2)
	
	$Disp.get_node(which).material.set_shader_param("curve", value)
		
	if global.currentEG:
#		global.currentEG.set(which.to_lower() + "c", value)
		global.currentEG.set(which + "c", value)
		emit_signal("envelope_changed", value, which + "c")

func _on_slider_input(ev, which):
	var value = which.get_node("Slider").value
	if ev is InputEventMouseMotion and Input.is_mouse_button_pressed(BUTTON_LEFT):
		emit_signal("envelope_changed", value, which.name + "c")


#func redraw():
#	$Disp.get_node("which").material.set_shader_param("curve", value)
