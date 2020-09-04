extends ColorRect

#var pts:Array = [Vector2.ZERO, Vector2.ONE]

var startpos = 0
var h =  rect_size.y / 2.0

func _ready():
#	pts.resize(rect_size.x)
	Engine.physics_jitter_fix = 0
	pass


func _process(_delta):
	if $"../Audio".bufferdata.size() > 0 and visible:
		update()

func _draw():
	var pts = PoolVector2Array($"../Audio".bufferdata)
	if pts.empty():  return
	var step = pts.size() / float(rect_size.x)
#	var window:int = min(min(rect_size.x, pts.size()), 440)


#	for i in range(pts.size()):
#	for i in range(window):
#		var v = Vector2(i * (rect_size.x/window), -pts[(i + startpos) % window].y * h + h)
#		var v2 = Vector2((i+1)* (rect_size.x/window), -pts[(i+1 + startpos) % window].y * h + h)
#		self.draw_line(v, v2, Color("#6680ff"),1.0, true)

#	var lastY = h
	for i in range(rect_size.x):
		var d = -pts[(i*step)].x
		var d2 = -pts[min(pts.size()-1, i*step+step)].x

		draw_line(Vector2(i,h), Vector2(i,d*h+h), Color(0.4, 0.5, 1.0, 0.10))
		draw_line(Vector2(i,d*h+h), Vector2(i+0.5,d2*h+h), Color(0.4, 0.5, 1.0, 0.9), 1.0, true)
#		draw_line(Vector2(i-1,lastY), Vector2(i,d*h+h), Color(0.4, 0.5, 1.0, 0.1), 0.5, true)
		
#		lastY = d*h+h

