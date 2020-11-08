
#Control panel for Patch global settings
extends Control

#var patch

func _ready():	
#	patch = owner.get_node("Audio").Patch
	
	for o in $V.get_children():
		if o.is_in_group("slider"):
			o.get_node("Slider").connect("value_changed", self, "_on_slider_change", [o.name])
	pass

	for o in $Filter.get_children():
		if o.is_in_group("slider"):
			o.get_node("Slider").connect("value_changed", self, "_on_filter_change", [o.name])

func _on_Gain_value_changed(value):
	$V/Gain/Val.text = str(value) 
#	$Label.text = str(db2linear(value))
	
	if global.currentPatch:
		global.currentPatch.gain = db2linear(value)



func _on_slider_change(value, which):
	$V.get_node(which + "/Val").text = str(value)

	if global.currentPatch:
		global.currentPatch.set(which, value)

#Filters
func _on_filter_change(value, which):
	$Filter.get_node(which + "/Val").text = str(value)
	update_filter()
	
func _on_FilterType_item_selected(index):
	update_filter(true)

func update_filter(reset=false):
	if !global.currentPatch:  return

	var output = []
	output.append($Filter/Type/Drop.selected)
	output.append($Filter/Hz/Slider.value)
	output.append($Filter/Q/Slider.value)

	global.currentPatch.SetFilter(output, reset)


#Patch info
func _on_Timer_timeout():
	if !global.currentPatch:  return
	$Label.text = global.currentPatch.GetInfo()

#Patch name
func _on_LineEdit_text_changed(new_text):
	if !global.currentPatch:  return
	global.currentPatch.name = new_text

#Done when pasting or opening new file
func reload():
	if !global.currentPatch:  return
	var patch = global.currentPatch

	$V/Gain/Slider.value = patch.Gain
	$V/Transpose/Slider.value = patch.Transpose
	$V/Pan/Slider.value = patch.Pan

	$Name/LineEdit.text = patch.name




func _on_Q_value_changed(value):
	owner.get_node("FormantGrid").q = value
	owner.get_node("FormantGrid").recalc()
	pass # Replace with function body.
