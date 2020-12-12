tool
extends PanelContainer

enum opType {NONE, CARRIER, MODULATOR}
export (opType) var slot_type setget set_slot_type

var s_norm = preload("res://sys/ui/wiringGrid/slot.stylebox")
var s_hover = preload("res://sys/ui/wiringGrid/slot_hover.stylebox")
var s_hilight = preload("res://sys/ui/wiringGrid/slot_hilite.stylebox")

var id = 0 
var connections = []
var level = 0  #height in modulation priority

var dragTree = preload("res://sys/ui/wiringGrid/DragTree.tscn")


func set_slot_type(val):
#	print("Setting Slot Type")
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

func set_slot(_id, slotType):
	id = _id
	set_slot_type(slotType)
	$Label.text = str(id+1)

func reset():
	set_slot_type(opType.NONE)
	$Label.text = ""
	unfocus()
	
func unfocus():
	release_focus()
	change_stylebox(s_norm)


func _on_gui_input(event):
	if event is InputEventMouseButton and event.pressed:
		if has_focus():  
			change_stylebox(s_hilight)
			$"..".last_slot_focused = int(name)

func change_stylebox(box):
	add_stylebox_override("panel", box)

func _on_mouse_entered():
	change_stylebox(s_hover)

func _on_mouse_exited():
	if has_focus():
		change_stylebox(s_hilight)
	else:
		change_stylebox(s_norm)



func get_drag_data(position):
	#Todo:  Generate a drag tree based on our operator's connections and stuf
#	set_drag_preview(owner.get_node("DragTree").duplicate())
	var p = dragTree.instance()
	p.op.id = id
	p.op.connections = connections
	set_drag_preview(p)
	
	
#	mouse_default_cursor_shape = CURSOR_DRAG
	return [id]
	pass
	
func can_drop_data(position, data):
	if data is Array and data.size() > 0:
		return true
	else:  
		return false

func drop_data(position, data):
	print ("Dropped from ", data)
