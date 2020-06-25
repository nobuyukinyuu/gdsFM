extends VBoxContainer

#var global.currentEG

func _ready():
	pass

func load_settings(envelope):
	global.currentEG = envelope
	$FB.value = envelope.feedback
	$DutyCycle.value = envelope.duty
	$chkReflect.pressed = envelope.reflect
	$chkUseDuty.pressed = envelope.UseDuty


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
