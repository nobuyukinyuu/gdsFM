extends Control

var curve_preview_btns = [$KSR, $KSL, $VR]

func _ready():
	pass

func load_settings(envelope):
	if !envelope:  return
	
	for o in curve_preview_btns:
		if o.dest_property == "":
			o.fetch_table(envelope, o.name.capitalize()) 
		else:
			o.fetch_table(envelope, o.dest_property)

	
