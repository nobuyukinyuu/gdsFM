extends Control

onready var curve_preview_btns = [$KSR, $KSL, $VR]

func _ready():
	for o in $V.get_children():
		if o.is_in_group("slider"):
			o.get_node("Slider").connect("value_changed", self, "_on_slider_change", [o.name])



func load_settings(envelope):
	if !envelope:  return
	
	for o in curve_preview_btns:
		if o.dest_property == "":
			o.fetch_table(envelope, o.name.capitalize()) 
		else:
			o.fetch_table(envelope, o.dest_property)


	for setting in $V.get_children():
		var s = setting.name
		if setting.is_in_group("slider"):
			get_node("V/%s/Slider" % setting.name).value = envelope.get(s)
		elif setting.is_in_group("setting_group"):
			setting.load_settings()  #Applies to AMS settings, maybe others

	$V/chkEnableFilter.pressed = envelope.FilterEnabled

	
func _on_slider_change(value, which):
	$V.get_node(which + "/Val").text = str(value).pad_decimals(1)
	
	if global.currentEG:
		global.currentEG.set(which, value)


func _on_chkEnableFilter_toggled(button_pressed):
	if global.currentEG:
		global.currentEG.set("FilterEnabled", button_pressed)
