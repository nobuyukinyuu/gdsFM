tool
extends Control

var tree = {"1": "2,4"}
var font = preload("res://gfx/fonts/NoteFont.tres")

var opsPerLevel = [[],[],[],[]]  #n-elements long where n is the total number of operators
var op:opNode = opNode.new()

func _ready():
#	var d = opNode.new()
#	d.id = 5
#
#	var a = opNode.new()
#	a.id = 1
#
#	var b = opNode.new()
#	b.id = 2
#
#	var c = opNode.new()
#	c.id = 3
#	b.connections = [c]
#	a.connections = [c]
#
#	op.connections = [a,b,d]
	pass
	
func _draw():
	draw_op(op.id, Vector2.ZERO,0, op.connections, Vector2(-32,-32))
	resetLevels()

func draw_op(id, pos:Vector2, level=0, connections=[], offset=Vector2.ZERO):
	#Consider opsPerLevel to contain arrays of the ops that wer used on each level,
	#Then we can check where to point to them based on position, otherwise we add a free slot
	
	var pos2=pos+offset
	
	draw_rect(Rect2(pos2+Vector2.ONE, Vector2(32,32)), ColorN("black"), false)
	draw_rect(Rect2(pos2, Vector2(32,32)), ColorN("black", 0.5), true)
	draw_rect(Rect2(pos2, Vector2(32,32)), ColorN("white"), false)
	
	draw_string(font, pos2 + Vector2(13,13), str(id+1), ColorN("black"))
	draw_string(font, pos2 + Vector2(12,12), str(id+1), ColorN("white"))
	
	for connection in connections:
		var slot_pos:Vector2 = get_slot_pos(connection.id, level) + offset #+ Vector2(pos.x,0)
		var a = pos2 + Vector2(16 - 8*sign(pos2.x-slot_pos.x), 4)
		var b = slot_pos + Vector2(16 + 8*sign(pos2.x-slot_pos.x), 28)

		draw_op(connection.id, slot_pos-offset, level+1, connection.connections, offset)
		draw_arrow(a,b)

func get_slot_pos(id, level):
	var output
	var idx = opsPerLevel[level].find(id)
	if idx >=0:
		output = Vector2((idx) * 40, (level+1) * -40)
	else:
		output = Vector2(opsPerLevel[level].size() * 40, (level+1) * -40)
		opsPerLevel[level].append(id)
	return output

func resetLevels():
	opsPerLevel = [[],[],[],[]]

func draw_arrow(a, b, color=Color(1,1,1,1), width=1.0):
	var arrow_spread= PI/6
	var arrow_length = 4
	var pts:PoolVector2Array
	pts.resize(3)
	pts[1] = a

	var angle = atan2(a.y-b.y, a.x-b.x) + PI
	
	pts[0] = Vector2(a.x + arrow_length*cos(angle+arrow_spread), a.y + arrow_length*sin(angle+arrow_spread))
	pts[2] = Vector2(a.x + arrow_length*cos(angle-arrow_spread), a.y + arrow_length*sin(angle-arrow_spread))


	draw_line(a,b,color,width, true)
	draw_line(a,pts[0],color,width, true)
	draw_line(a,pts[2],color,width, true)

class opNode:
	var id = 0  #Must be nonzero
	var connections = []
