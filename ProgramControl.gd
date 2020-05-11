extends Control

var patch

func _ready():
	pass


func _on_Gain_value_changed(value):
	$V/Gain/Val.text = str(value) 
	pass # Replace with function body.


func _on_slider_change(value, which):
	$V.get_node(which + "/Val").text = str(value)
	
	if patch:
		patch.set(which, value)
		
