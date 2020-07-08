
#Control panel for Patch global settings
extends Control

var patch

func _ready():
	
	patch = owner.get_node("Audio").Patch
	
	for o in $V.get_children():
		if o.is_in_group("slider"):
			o.get_node("Slider").connect("value_changed", self, "_on_slider_change", [o.name])
	pass


func _on_Gain_value_changed(value):
	$V/Gain/Val.text = str(value) 
	$Label.text = str(db2linear(value))
	
	if owner.get_node("Audio").Patch:
		owner.get_node("Audio").Patch.gain = db2linear(value)



func _on_slider_change(value, which):
	$V.get_node(which + "/Val").text = str(value)
	
	if owner.get_node("Audio").Patch:
		owner.get_node("Audio").Patch.set(which, value)
		
