tool
extends PanelContainer

enum opType {NONE, CARRIER, MODULATOR}
export (opType) var slot_type setget set_slot_type

var s_norm = preload("res://sys/ui/wiringGrid/slot.stylebox")
var s_hover = preload("res://sys/ui/wiringGrid/slot_hover.stylebox")
var s_hilight = preload("res://sys/ui/wiringGrid/slot_hilite.stylebox")

func set_slot_type(val):
	print("Setting Slot Type")
	slot_type = val
	match val:
		opType.NONE:
			self_modulate = Color(0.6,0.6,0.6,1)
		opType.CARRIER:
			self_modulate = Color(0,0.5,1,1)
		opType.MODULATOR:
			self_modulate = Color(1,0,0,1)

func _ready():
	pass

func reset():
	set_slot_type(opType.NONE)
	$Label.text = ""
	release_focus()

func _on_gui_input(event):
	if event is InputEventMouseButton:
#		grab_click_focus()
		if has_focus():  change_stylebox(s_hilight)
	pass # Replace with function body.



func change_stylebox(box):
	add_stylebox_override("panel", box)


func _on_mouse_entered():
	change_stylebox(s_hover)

func _on_mouse_exited():
	if has_focus():
		change_stylebox(s_hilight)
	else:
		change_stylebox(s_norm)
	


