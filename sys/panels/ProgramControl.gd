extends Control

var patch

func _ready():
	
	patch = owner.get_node("Audio").Patch
	pass


func _on_Gain_value_changed(value):
	$V/Gain/Val.text = str(value) 
	$Label.text = str(db2linear(value))
	
	if owner.get_node("Audio").Patch:
		owner.get_node("Audio").Patch.gain = db2linear(value)



func _on_slider_change(value, which):
	$V.get_node(which + "/Val").text = str(value)
	
	if patch:
		patch.set(which, value)
		
