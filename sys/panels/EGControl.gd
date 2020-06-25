extends Control

#var global.currentEG  #:global.Instr  #Current instrument associated with the controls.

signal envelope_changed


func _ready():
	
	
	for o in $H.get_children():
		if o.is_in_group("slider"):
			o.get_node("Slider").connect("value_changed", self, "_on_slider_change", [o.name])
			o.get_node("Slider").connect("gui_input", self, "_on_slider_input", [o])



func load_settings(instr):
	if !instr:  
		print("Warning:  Attempting to load EG settings from null")
		return
	
	global.currentEG = instr
	
	for setting in $H.get_children():
		var s = setting.name
		if setting.is_in_group("slider"):
			get_node("H/%s/Slider" % setting.name).value = instr.get(s)
		elif setting.is_in_group("setting_group"):
			setting.load_settings(instr)  #Applies to FixedFreq settings, maybe others


func _on_slider_change(value, which):
	$H.get_node(which + "/Val").text = str(value).pad_decimals(1)
	
	if global.currentEG:
		global.currentEG.set(which, value)
		
#		if ["Ar", "Dr", "Sr", "Rr", "Sl", "Tl"].has(which):
#			emit_signal("envelope_changed", value, which)
	
func _on_slider_input(ev, which):
	var value = which.get_node("Slider").value
	if ev is InputEventMouseMotion and Input.is_mouse_button_pressed(BUTTON_LEFT):
		if ["Ar", "Dr", "Sr", "Rr", "Sl", "Tl"].has(which.name):  
			#Tuning and EG share a script. Don't update on tuning.
			emit_signal("envelope_changed", value, which.name)
			
#		else:   #DEBUG, check to update mult display
#			if global.currentEG: 
#				var totalMult = 0.0
#				totalMult += global.currentEG._freqMult * global.currentEG._coarseDetune * global.currentEG._detune
#				$TODO.text = str(totalMult)


#Gets a specified slider value.
func get_value(name):
	return get_node("H/%s/Slider" % name).value


func disable(yes:bool):
	for setting in $H.get_children():
		if setting.is_in_group("slider"):
			get_node("H/%s/Slider" % setting.name).editable = !yes




