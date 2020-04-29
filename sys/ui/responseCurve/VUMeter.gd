extends TextureRect

var tbl = []
const full16 = preload("res://gfx/ui/vu/full16.png")
const indicator = preload("res://gfx/ui/vu/indicator16.png")

var elementWidth = 10

# Declare member variables here. Examples:
# var a = 2
# var b = "text"


# Called when the node enters the scene tree for the first time.
func _ready():
	elementWidth = texture.get_width()
	if elementWidth == null:  elementWidth = 10
	
	for i in 128:
		tbl.append(0)


func _gui_input(event):
	if event is InputEventMouseMotion and Input.is_mouse_button_pressed(BUTTON_LEFT):
		var arpos = clamp(int(lerp(0, 127, get_local_mouse_position().x / float(rect_size.x))) , 0, 127)
		var val = clamp(lerp(100,0, get_local_mouse_position().y / float(rect_size.y)) , 0, 100)
#		prints("arpos",arpos,"val",val)

		tbl[arpos] = val

		#Determine grouping.  Ideally we'd lerp the values between the one the user selected
		#and the values next to it on the VU meter display.  Rudimentary set is also fine..
		var numElements = int(rect_size.x / elementWidth)
		var groupWidth = numElements / 128.0  #Value used to stepify between 1/(arraySize) to 1/(VisualElements)
		var startPos = int(arpos * groupWidth) * (1/groupWidth)  #Stepified position.

#		prints("Elements:", numElements, "groupWidth:", groupWidth, "startPos:",startPos)
#
		for i in range(startPos, min(128, startPos+ (1/groupWidth))):
#			prints("Setting", i)
			tbl[i] = val

		update()

func _draw():
	
	for i in range(0, rect_size.x, elementWidth):
		var val =  tbl[ lerp(0, 128, i / float(rect_size.x)) ]
		var sz = Vector2(10, int(lerp(0, rect_size.y,val/200.0)) * 2 )
		var pos = Vector2(i, rect_size.y - sz.y)
		draw_texture_rect(full16, Rect2(pos, sz),true)
		draw_texture(indicator,pos)
#		if i == 310:  prints("drawrect", i, ":", pos, sz)

