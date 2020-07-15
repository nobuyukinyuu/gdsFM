extends VBoxContainer

#var global.currentEG

func _ready():
	pass

func load_settings(envelope):
	global.currentEG = envelope
	$FB.value = envelope.feedback
	$DutyCycle.value = envelope.duty
	$H/chk/Reflect.pressed = envelope.reflect
	$H/chk/UseDuty.pressed = envelope.UseDuty

	$Grid/Waveform.select(envelope.waveform)
	$Grid/Waveform2.select(envelope.fmTechnique)



func _on_Waveform_item_selected(id, techWaveform:bool=false):
	if !global.currentEG:  return

	if !techWaveform:
		global.currentEG.waveform = id
	else:
		global.currentEG.fmTechnique = id



# Feedback
func _on_FB_value_changed(value):
	$lblFeedback.text = "Feedback:  " + str(value)
	if global.currentEG:
		global.currentEG.feedback = value


func _on_DutyCycle_value_changed(value):
	$lblDuty.text = "Duty Cycle:  " + str(value)
	if global.currentEG:
		global.currentEG.duty = value


func _on_chkReflect_toggled(button_pressed):
	if global.currentEG:
		global.currentEG.reflect = button_pressed


func _on_chkUseDuty_toggled(button_pressed):
	if global.currentEG:
		global.currentEG.UseDuty = button_pressed
