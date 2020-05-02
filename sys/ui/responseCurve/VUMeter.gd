extends TextureRect

var tbl = []
const full16 = preload("res://gfx/ui/vu/full16.png")
const indicator = preload("res://gfx/ui/vu/indicator16.png")

var elementWidth = 10  #How wide is a column?

export(int,1,255) var maxValue = 100
export(bool) var linLog = false

var cursor = preload("res://gfx/ui/vu/cursor.png").get_data()
var numbers = preload("res://gfx/fonts/small_numerics_thin.png").get_data()
var cursor_img = Image.new()
var cursor_texture=ImageTexture.new()
var custom_texture=ImageTexture.new()

# Called when the node enters the scene tree for the first time.
func _ready():
	elementWidth = texture.get_width()
	if elementWidth == null:  elementWidth = 10
	
	for i in 128:
		tbl.append(0)

	cursor_img.create(24,16,false,Image.FORMAT_RGBA8)
	var tex = ImageTexture.new()

	cursor_img.lock()
	cursor_img.blit_rect(cursor,Rect2(Vector2.ZERO, cursor.get_size()),Vector2.ZERO)
	cursor_img.unlock()
#	set_cursor("01.9")
	cursor_texture.create_from_image(cursor_img,0)
	custom_texture.create_from_image(cursor_img,0)
#	cursor_texture = tex



func _gui_input(event):
	if event is InputEventMouseMotion and Input.is_mouse_button_pressed(BUTTON_LEFT):
		var arpos = clamp(int(lerp(0, 127, get_local_mouse_position().x / float(rect_size.x))) , 0, 127)
		var val = clamp(lerp(100,0, get_local_mouse_position().y / float(rect_size.y)) , 0, 100)
#		prints("arpos",arpos,"val",val)

		var vol = maxValue * (val/100.0)
		if linLog:  vol = (val/float(maxValue)) * (val/float(maxValue)) * maxValue
		
		set_cursor(String(vol))
		Input.set_custom_mouse_cursor(cursor_texture,0,Vector2(0,0)) #Temporarily blank to update
		Input.set_custom_mouse_cursor(custom_texture,0,Vector2(0,0))


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
	else:
		Input.set_custom_mouse_cursor(null)

func _draw():
	
	for i in range(0, rect_size.x, elementWidth):
		var val =  tbl[ lerp(0, 128, i / float(rect_size.x)) ]
		var sz = Vector2(10, int(lerp(0, rect_size.y,val/200.0)) * 2 )
		var pos = Vector2(i, rect_size.y - sz.y)
		draw_texture_rect(full16, Rect2(pos, sz),true)
		draw_texture(indicator,pos)
#		if i == 310:  prints("drawrect", i, ":", pos, sz)



func set_cursor(volume:String):
	cursor_img.lock()

	for i in range(1,4):
		cursor_img.blit_rect(numbers,Rect2(4,0,4,8), Vector2(i*4 +8,8))
	
	for i in min(4, String(int(volume)).length()):
		var n = int(volume.ord_at(i))
		
		var pos = Vector2(4,0)
		if n == 46:
			pos = Vector2.ZERO
		elif (n>=48 and n<58):
			pos = Vector2((n-48)*4 + 8, 0)
		cursor_img.blit_rect(numbers,Rect2(pos, Vector2(4,8)),Vector2(i*4 +8,8))
	
#	if volume.begins_with("100"):  cursor_img.blit_rect(numbers,Rect2(4,0,4,8), Vector2(12,8))
	
	cursor_img.unlock()
	custom_texture.create_from_image(cursor_img,0)
