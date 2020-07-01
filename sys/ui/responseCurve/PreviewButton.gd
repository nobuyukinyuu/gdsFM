tool
class_name ResponseCurvePreviewButton
extends Button

export (bool) var use_log setget set_use_log

enum rtable_dests {ENVELOPE, PATCH}

export(rtable_dests) var target = rtable_dests.ENVELOPE  #Where will the table be passed to?
export(String) var dest_property  #The envelope/patch property the table from this goes to.

#if true: When marshalling the table to c#, zero values will become epsilon.
#this is to prevent rate curve calculations or others that don't play well with zeroes producing NaNs.
#export(bool) var allow_zero = true

export(bool) var note_scale = true  #Set to false if the x-axis isn't mapping note numbers.
export(bool) var float_scale = true  #Set to false if the y-axis isn't mapping percentage.
export(bool) var rate_scale = false  #Set to true if the y-axis maps a rate (ie:  percentage of original time)


func set_use_log(val):
	use_log = val
	$P/Preview/LinLog.visible = val


func _ready():
	_on_ResponseButton_resized()

	$Popup/ResponseCurve/chkLinlog.connect("toggled", self, "_on_linlog_toggle")
	$Popup/ResponseCurve.title = text
	$Popup.connect("popup_hide", self, "send_table")
	
	if dest_property == "":  dest_property = name.capitalize()

	$Popup/ResponseCurve.note_scale = note_scale
	$Popup/ResponseCurve.float_scale = float_scale
	$Popup/ResponseCurve.rate_scale = rate_scale

#	$Popup/ResponseCurve.allow_zero = allow_zero

func _on_ResponseButton_resized():
	$Lbl.text = text
	
	$P.rect_position.x = rect_size.x/2 - $P.rect_size.x/2
	$Lbl.rect_position.x = rect_size.x/2 - $Lbl.rect_size.x/2
	pass # Replace with function body.


func _gui_input(event):
	if event is InputEventMouseButton and event.is_pressed() and event.button_mask==BUTTON_RIGHT:
		var output = $Popup/ResponseCurve.fetch_table(global.currentEG, dest_property)["precalc_values"]
		
#		for i in 128:
#			global.currentEG.get(name.to_lower()).precalc_values
		
		print(name,": ", output)

func _on_ResponseButton_pressed():
	var pos = get_global_mouse_position() - Vector2($Popup.rect_size.x * 0.8, 0)
#	pos.x -= $Popup/ResponseCurve.rect_size.x
	$Popup.popup(Rect2(pos, $Popup.rect_size))

func _on_linlog_toggle(val):
	set_use_log(val)

func fetch_table(dest, property:String):
	var new_table:Dictionary = $Popup/ResponseCurve.fetch_table(dest, property)
	if !new_table:  print("PreviewButton.gd:  Something went wrong trying to fetch the rTable.....")
	$P/Preview.refresh_all(new_table.get("values"))
	set_use_log(new_table.get("use_log", false))

	return new_table

func send_table():
	match target:
		rtable_dests.ENVELOPE:
			if !global.currentEG:  return
			$Popup/ResponseCurve.set_rtable(global.currentEG, dest_property)
		rtable_dests.PATCH:
			#TOOD:  either assign global.patch or grab from #Audio.patch...
			pass



