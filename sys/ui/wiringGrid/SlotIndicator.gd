tool
extends GridContainer
export (int, 2, 8) var total_ops = 4 setget set_ops

const sProto = preload("res://sys/ui/wiringGrid/SlotProto.tscn")
const spk = preload("res://gfx/ui/icon_speaker.svg")

var last_slot_focused=0 setget reset_focus

var _grid = []  #References to opNodes in a particular grid position


func reset_focus(val):
	prints("unfocusing", last_slot_focused, "and focusing", val)
	if last_slot_focused >=0:
		get_node(str(last_slot_focused)).unfocus()
		if val == last_slot_focused:  val = -1
	last_slot_focused = val

func set_ops(val):
	total_ops = val
	reinit_grid()


func reinit_grid():
	for o in get_children():
		o.queue_free()
	
	columns = total_ops
	
	yield (get_tree(), "idle_frame")
	yield (get_tree(), "idle_frame")

	#Make 2d array
	_grid.clear()
	for i in total_ops:
		var arr = []
		arr.resize(total_ops)
		_grid.append(arr)
	
	for i in range(total_ops*total_ops):
		var p = sProto.instance()
		p.name = str(i)
		add_child(p)
		p.owner = owner

	var start = total_ops*total_ops - total_ops
	for i in total_ops:
		var p = get_child(start+i)
		p.set_slot(i, 1)  #1=carrier
	
	update()

func _ready():
	reinit_grid()
	pass

func getSlot(x,y):
	return _grid[x][y]
func setSlot(x,y,val):
	_grid[x][y] = val
func slotIsEmpty(x,y):
	return _grid[x][y] == null

func _draw():
	#Draw the output diagram
	var tile_size = rect_size / total_ops
	var y = rect_size.y - tile_size.y/4

	for i in total_ops:
		var a = Vector2(i * tile_size.x + tile_size.x / 2, y)
		var b = Vector2(a.x, rect_size.y + 8)
		draw_line(a,b, ColorN("white"),1.0, true)

	y = rect_size.y + 8
	var half = tile_size.x / 2
	draw_line(Vector2(half, y), Vector2(rect_size.x, y), ColorN("white"), 1.0, true)
	draw_texture(spk,Vector2(rect_size.x, y) - Vector2(8,8))

