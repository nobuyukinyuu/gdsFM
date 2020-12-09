tool
extends Control

var tree = {"1": "2,4"}
var font = preload("res://gfx/fonts/NoteFont.tres")

var opsPerLevel = [0,0,0,0]

func _ready():
	pass

func _draw():
	var a = opNode.new()
	a.id = 2
	
	var b = opNode.new()
	b.id = 3
	
	var c = opNode.new()
	c.id = 4
	b.connections = [c]
	a.connections = [c]
	
	draw_op(1, Vector2.ZERO,0,[a,b])
	resetLevels()

func draw_op(id, pos:Vector2, level=0, connections=[], offset=Vector2.ZERO):
	#Consider opsPerLevel to contain arrays of the ops that wer used on each level,
	#Then we can check where to point to them based on position, otherwise we add a free slot
	
	
	draw_rect(Rect2(pos+Vector2.ONE, Vector2(32,32)), ColorN("black"), false)
	draw_rect(Rect2(pos, Vector2(32,32)), ColorN("black", 0.5), true)
	draw_rect(Rect2(pos, Vector2(32,32)), ColorN("white"), false)
	
	draw_string(font, pos + Vector2(13,13), str(id), ColorN("black"))
	draw_string(font, pos + Vector2(12,12), str(id), ColorN("white"))
	
	for connection in connections:
		var slot_pos:Vector2 = get_slot_pos(level+1) #+ Vector2(pos.x,0)
		var a = pos + Vector2(16 - 8*sign(pos.x-slot_pos.x), 0)
		var b = slot_pos + Vector2(16 + 8*sign(pos.x-slot_pos.x), 32)

		draw_op(connection.id, slot_pos, level+1, connection.connections)
		draw_arrow(a,b)

func get_slot_pos(level):
	var output = Vector2(opsPerLevel[level] * 40, level * -40)
	opsPerLevel[level] += 1
	return output

func resetLevels():
	opsPerLevel = [0,0,0,0]

func draw_arrow(a, b, color=Color(1,1,1,1), width=1.0):
	draw_line(a,b,color,width, true)

class opNode:
	var id = 0  #Must be nonzero
	var connections = []
