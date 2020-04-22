extends Control

var currentEG  #:global.Instr  #Current instrument associated with the controls.



func _ready():
	
	
	for o in $H.get_children():
		if o.is_in_group("slider"):
			o.get_node("Slider").connect("value_changed", self, "_on_slider_change", [o.name])
		

func load_settings(instr):
	if !instr:  
		print("Warning:  Attempting to load EG settings from null")
		return
	
	currentEG = instr
	
	for setting in $H.get_children():
		var s = setting.name
		if setting.is_in_group("slider"):
			get_node("H/%s/Slider" % setting.name).value = instr.get(s)



func _on_slider_change(value, which):
	$H.get_node(which + "/Val").text = str(value).pad_decimals(1)
	
	if currentEG:
		currentEG.set(which, value)
	

#Gets a specified slider value.
func get_value(name):
	return get_node("H/%s/Slider" % name).value


func disable(yes:bool):
	for setting in $H.get_children():
		if setting.is_in_group("slider"):
			get_node("H/%s/Slider" % setting.name).editable = !yes




