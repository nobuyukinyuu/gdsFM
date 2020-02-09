extends Control

var currentEG:global.Instr  #Current instrument associated with the controls.

func _ready():
	for o in $H.get_children():
		o.get_node("Slider").connect("value_changed", self, "_on_slider_change", [o.name])
		

func load_settings(instr:global.Instr):
	currentEG = instr
	
	for setting in $H.get_children():
		var s = setting.name.to_lower()
		get_node("H/%s/Slider" % setting.name).value = instr.get(s)


func _on_slider_change(value, which):
	$H.get_node(which + "/Val").text = str(value)
	
	if currentEG:
		currentEG.set(which.to_lower(), value)
	

#Gets a specified slider value.
func get_value(name):
	return get_node("H/%s/Slider" % name).value
