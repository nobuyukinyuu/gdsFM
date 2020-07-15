extends HBoxContainer

var operator

func _ready():
	pass
	

func load_settings():
	if !global.currentPatch or !global.currentEG:  return
	var id = "OP" + str(global.currentEG.opID+1)
	operator = global.currentPatch.GetOperator(id)

	$"../Banks".load_settings()

	$Slider.value = operator.ams * 100

func _on_Slider_value_changed(value):
	$Val.text = str(value)

	
	if operator:
		operator.ams = value / 100.0

