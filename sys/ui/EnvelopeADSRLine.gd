tool
extends TextureRect

const PRECISION=16
export (bool) var reverse
export (Color) var color = ColorN("white")
export (float, 0.5, 5, 0.5) var thickness = 1
export (float, -2, 5, 0.1) var curve=1 setget update_curve

export(float,0,1) var tl=1  #Total level
export(float,0,1) var sl=0  #sustain floor

func update_curve(val):
	curve = val
	update()

func _ready():
	update()
	
#func _physics_process(_delta):
#	update()						#DON'T DO THIS


func _draw():

	var h = rect_size.y/(PRECISION*2)
	for i in range(0, PRECISION*2, 2):
		
		draw_line(Vector2(rect_size.x, h*i), Vector2(rect_size.x, h*(i+1)), ColorN("white", 0.1),0.5,false)

	var x = rect_size.x / PRECISION
	if reverse:
		var w = rect_size.x
		for i in PRECISION:
			var pos1= Vector2(w-x*i, (1.0-ease(i/float(PRECISION), curve)) * rect_size.y)
			var pos2= Vector2(w-x*(i+1), (1.0-ease((i+1)/float(PRECISION), curve)) * rect_size.y)
			
			pos1.y *= sl
			pos2.y *= sl
			
			pos1.y *=tl  
			pos1.y += (1.0 - tl) * rect_size.y
			pos2.y *=tl  
			pos2.y += (1.0 - tl) * rect_size.y
			
			draw_line(pos1, pos2, color, thickness,true)
	else:
		for i in PRECISION:
			var pos1= Vector2(x*i, (1.0-ease(i/float(PRECISION), curve)) * rect_size.y)
			var pos2= Vector2(x*(i+1), (1.0-ease((i+1)/float(PRECISION), curve)) * rect_size.y)

			pos1.y *= sl
			pos2.y *= sl
			
			pos1.y *=tl
			pos1.y += (1.0 - tl) * rect_size.y
			pos2.y *=tl
			pos2.y += (1.0 - tl) * rect_size.y

			draw_line(pos1,pos2,color,thickness,true)




