extends VBoxContainer

var currentEG

func _ready():
	pass

func load_settings(envelope):
	currentEG = envelope
	$FB.value = envelope.feedback
	$DutyCycle.value = envelope.duty
	$chkReflect.pressed = envelope.reflect


# Feedback
func _on_FB_value_changed(value):
	$lblFeedback.text = "Feedback:  " + str(value)
	if currentEG:
		currentEG.feedback = value


func _on_DutyCycle_value_changed(value):
	$lblDuty.text = "Duty Cycle:  " + str(value)
	if currentEG:
		currentEG.duty = value


func _on_chkReflect_toggled(button_pressed):
	if currentEG:
		currentEG.reflect = button_pressed
