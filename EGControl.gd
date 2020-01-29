extends Control

func _ready():
	for o in $H.get_children():
		o.get_node("Slider").connect("value_changed", self, "_on_slider_change", [o.name])
		
		
		
func _on_slider_change(value, which):
	$H.get_node(which + "/Val").text = str(value)
	

#Gets a specified slider value.
func get_value(name):
	return get_node("H/%s/Slider" % name).value