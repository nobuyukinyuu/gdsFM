extends Control

var currentLFO


func _ready():
	for bank in $Banks.get_children():
		bank.connect("pressed", self, "_on_Bank_change", [bank.name])
	
	for o in $V.get_children():
		if !o.is_in_group("slider"):  continue		
		o.get_node("Slider").connect("value_changed", self, "_on_slider_change", [o.name])


	#Load global patch LFO bank A
	yield(get_tree(),"idle_frame")
	yield(get_tree(),"idle_frame")
	if global.currentPatch:
		_on_Bank_change("0")



func _on_Bank_change(which):
	var output = global.currentPatch.GetLFO(int(which))
	if output:  
		currentLFO = output
		load_settings()
	else:
		print("Warning:  Can't find LFO ", which)



#Called on bank change
func load_settings():
	if !currentLFO:  
		print("Warning:  Attempting to load LFO settings from null")
		return
	
	$Waveform.select(currentLFO.waveform)
	$chkEnable.pressed = currentLFO.enabled
	$chkReflect.pressed = currentLFO.reflect_waveform
	$V/GridContainer/chkSync.pressed = currentLFO.keySync
	$V/GridContainer/chkSync2.pressed = currentLFO.oscSync
	
	for setting in $V.get_children():
		if !setting.is_in_group("slider"):  continue
		
		var s = setting.name
		get_node("V/%s/Slider" % setting.name).value = currentLFO.get(s)


func _on_slider_change(value, which):
	var decimal_places =   $V.get_node(which + "/Slider").step
	decimal_places = max(0, len(str(decimal_places)) - 2)

	$V.get_node(which + "/Val").text = str(value).pad_decimals(decimal_places)
	
	if currentLFO:
		currentLFO.set(which, value)





func _on_chkEnable_toggled(button_pressed):
	if currentLFO:  currentLFO.enabled = button_pressed

func _on_Waveform_item_selected(index):
	if currentLFO:  currentLFO.waveform = index


func _on_chkReflect_toggled(button_pressed):
	if currentLFO:  currentLFO.reflect_waveform = button_pressed


func _on_keySync_toggled(button_pressed):
	if currentLFO:  currentLFO.keySync = button_pressed
func _on_oscSync_toggled(button_pressed):
	if currentLFO:  currentLFO.oscSync = button_pressed
