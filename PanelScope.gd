extends ColorRect

#var pts:Array = [Vector2.ZERO, Vector2.ONE]

var startpos = 0

func _ready():
#	pts.resize(rect_size.x)
	pass


func _process(delta):
	if $"../Audio".bufferdata.size() > 0 and visible:
		update()

func _draw():
	var pts = PoolVector2Array($"../Audio".bufferdata)
	var window:int = min(min(rect_size.x, pts.size()), 440)
	pts.resize( window )
	
	
	var h =  rect_size.y / 2.0
	
	for i in range(pts.size()):
		var v = Vector2(i * (rect_size.x/window), pts[(i + startpos) % window].y * h + h)
		var v2 = Vector2((i+1)* (rect_size.x/window), pts[(i+1 + startpos) % window].y * h + h)
		self.draw_line(v, v2, Color("#6680ff"),1.0, true)

#	startpos += window
