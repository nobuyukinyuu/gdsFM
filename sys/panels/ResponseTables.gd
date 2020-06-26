extends Control

var curve_preview_btns = [$KSR, $KSL, $VR]

func _ready():
	pass

func load_settings(envelope):
	if !envelope:  return
	
	for o in curve_preview_btns:
		o.fetch_table(envelope, o.name.capitalize())  #TODO: use properties for this or something

	
