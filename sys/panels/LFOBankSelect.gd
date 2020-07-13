extends HBoxContainer

enum Type {OTHER, AMPLITUDE, PITCH}
export (Type) var lfo_type
var bankTypeNames = ["", "lfoBankAmp", "lfoBankPitch"]

var operator

var lastButtonPressed = -1

func _ready():
	for o in get_children():
		if o is Button:
			o.connect("pressed", self, "_on_bank_change", [int(o.name)])



func load_settings():
	if !global.currentPatch or !global.currentEG:  return
	var id = "OP" + str(global.currentEG.opID+1)
	operator = global.currentPatch.GetOperator(id)

	if !operator:
		print ("Can't get operator for LFO bank? ", id)
		return

	print("Setting lfo operator,", operator)

	set_no_bank()
	var currentBank = operator.get(bankTypeNames[lfo_type])
	if currentBank >= 0:
		get_node(str(currentBank)).pressed = true
		lastButtonPressed = currentBank

func _on_bank_change(which):
	print ("bank change: ", which, ".  Last: ", lastButtonPressed)
	if which == lastButtonPressed:
		set_no_bank()
		which = -1

	if operator:
		operator.set(bankTypeNames[lfo_type], which)
		lastButtonPressed = which
	else:
		set_no_bank()

#Clear the active bank select button
func set_no_bank():
	for o in get_children():
		if o is Button:
			o.pressed = false
	lastButtonPressed = -1
