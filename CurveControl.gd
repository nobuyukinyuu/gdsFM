extends Tabs



func _ready():
	for o in [$A, $S, $D, $R]:
		o.get_node("Slider").connect("value_changed", self, "_on_slider_change", [o.name])
		o.get_node("Val").modulate.a = 0.3


func _on_slider_change(value, which):
	get_node(which + "/Val").text = str(value).pad_decimals(1)
	
	$Disp.get_node(which).material.set_shader_param("curve", value)
		
	#TODO:  Set currentEG envelope curve type
#	if currentEG:
#		currentEG.set(which, value)
	
#func redraw():
#	$Disp.get_node("which").material.set_shader_param("curve", value)
