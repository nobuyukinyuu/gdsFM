extends VBoxContainer

#var global.currentEG
var operator

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

	var id = "OP" + str(global.currentEG.opID+1)
	operator = global.currentPatch.GetOperator(id)

	var bank:int = operator.waveformBank
	var waveform:Dictionary = global.currentPatch.GetWaveformBank(bank,true) 
	$H/CustomWave/Preview.shallow_update_preview(waveform)

	$OscBank/SpinBox.max_value = global.currentPatch.WaveformBankSize-1
	$OscBank/SpinBox.value = bank


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

#Custom waveform.
func _on_OscBank_SpinBox_value_changed(value):
	if !global.currentPatch:  return
	var spin:SpinBox = $OscBank/SpinBox
	var bankSize = global.currentPatch.WaveformBankSize
	
	spin.value = min(value, bankSize-1)
#	if bankSize == 1:  return
	
	spin.max_value = bankSize-1
#	print ("SpinValue: ", value)

	var input:Dictionary = global.currentPatch.GetWaveformBank(spin.value,true) 
		
	if input:
		$H/CustomWave/Preview.shallow_update_preview(input)
		
		if operator:
			global.currentPatch.LoadCustomWaveform(spin.value, operator.name)
		else:
			print("WaveformSpinbox: Can't find operator associated with." % global.currentEG.opID)
		
	else:
		print("WaveformSpinbox: Can't find Patch's custom wavetable bank at %s." % spin.value)

	
