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

#Held values to allow keyboard tweaking.
var last_column = 0
var last_value = 0

enum {NO, PROCESS_LEFT, PROCESS_MID, PROCESS_RIGHT}  #For input events

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
	var process = NO
	if event is InputEventMouseMotion and Input.is_mouse_button_pressed(BUTTON_LEFT): process = PROCESS_LEFT
	if event is InputEventMouseButton: 
		if event.button_index == BUTTON_LEFT and event.pressed: 
			process = PROCESS_LEFT
		if event.button_index == BUTTON_RIGHT and !event.pressed:
			#Popup copipe menu
			process = PROCESS_RIGHT
			var pop:PopupMenu = owner.get_node("CPMenu")
			pop.popup(Rect2(get_global_mouse_position(), pop.rect_size))
	
	if process == PROCESS_LEFT:
		var xy = get_table_pos()
		var arpos = xy.x
		var val = xy.y

		var vol = maxValue * (val/100.0)
		
		#TODO:  Use this calculation when passing the table.....
		if linLog:  vol = (val/float(maxValue)) * (val/float(maxValue)) * maxValue  #Convert vol val to log
		
		
		#Generate a cursor to help user set proper map val
		set_cursor(String(vol))
		Input.set_custom_mouse_cursor(cursor_texture,0,Vector2(0,0)) #Temporarily blank to update
		Input.set_custom_mouse_cursor(custom_texture,0,Vector2(0,0))


		tbl[arpos] = val  #Array position set to value.

		#Determine grouping.  Ideally we'd lerp the values between the one the user selected
		#and the values next to it on the VU meter display.  Rudimentary set is also fine..
		var numElements = int(rect_size.x / elementWidth)
		var groupWidth = numElements / 128.0  #Value used to stepify between 1/(arraySize) to 1/(VisualElements)
		var startPos = int(arpos * groupWidth) * (1/groupWidth)  #Stepified position.
#		prints("Elements:", numElements, "groupWidth:", groupWidth, "startPos:",startPos)
#

		#Interpolation methods.
		if $"../Splits".get_node(String(int(startPos/4))).pressed:
			#Split mode value.  All values in this group are the same.
			for i in range(startPos, min(128, startPos+ (1/groupWidth))):
				print(i)
				tbl[i] = val
				owner.emit_signal("value_changed", i, val)
		else:
			#Interpolate values. startPos remains user-set. Lerp values to the next user point and previous one.
			#This should be ±4 from the startPos...
			var reach=4  #should be (1/groupWidth) to account for size differences;  fixme or test later
			var prevVal:float= tbl[max(0, startPos-reach)]
			var nextVal:float = tbl[min(123, startPos+reach)]  #Don't interpolate to a value the user can't set.

			#+1 to avoid overwriting the pre column's uservalue. end-exclusive loop avoids overwriting next one
			var first= max(0,startPos-reach+1)
			var last = min(128, startPos+reach)

			for i in range(first, startPos):
				var lerpVal = lerp(prevVal, val, (i-first)/float(startPos-first) )
#				print("lerp:", i, " to: ", lerpVal)
				tbl[i] = lerpVal
				owner.emit_signal("value_changed", i, lerpVal)
			for i in range(startPos, last):
				var lerpVal = lerp(val, nextVal, (i-startPos)/float(last-startPos) )
#				print ("lerpos: ", (i-startPos)/float(last-startPos))
#				print("lerp:", i, " to: ", lerpVal)
				tbl[i] = lerpVal
				owner.emit_signal("value_changed", i, lerpVal)
			tbl[startPos] = val  #Put the user value back in its canonical place.
			owner.emit_signal("value_changed", startPos, val)

		update()
	else:
		Input.set_custom_mouse_cursor(null)

#Returns a vector of the table position and value.
func get_table_pos() -> Vector2:
	var arpos = clamp(int(lerp(0, 127, get_local_mouse_position().x / float(rect_size.x))) , 0, 127)
	var val = clamp(lerp(100,0, get_local_mouse_position().y / float(rect_size.y)) , 0, 100)
	return Vector2(arpos, val)


func _draw():

	#Draw the bars.	
	for i in range(0, rect_size.x, elementWidth):
		var val =  tbl[ lerp(0, 128, i / float(rect_size.x)) ]
		var sz = Vector2(10, int(lerp(0, rect_size.y,val/200.0)) * 2 )
		var pos = Vector2(i, rect_size.y - sz.y)
		draw_texture_rect(full16, Rect2(pos, sz),true)
		if val > 0:  draw_texture(indicator,pos)
#		if i == 310:  prints("drawrect", i, ":", pos, sz)

	#Draw the minmax indicators for response floor and ceiling.
	if owner.get_node("sldMax").value < 100:
		var val = owner.get_node("sldMax").value
		var y = rect_size.y - (val/100.0 * rect_size.y)

		draw_rect(Rect2(0, 0, rect_size.x, y), ColorN('purple', 0.1))
		draw_line(Vector2(0,y), Vector2(rect_size.x, y), ColorN('purple', 0.5))
		
	if owner.get_node("sldMin").value > 0:
		var maxval = owner.get_node("sldMax").value / 100.0
		var minval = owner.get_node("sldMin").value / 100.0
		var y = rect_size.y - (minval*rect_size.y) * maxval
		draw_rect(Rect2(0, y, rect_size.x, rect_size.y-y), ColorN('green', 0.1))
		draw_line(Vector2(0,y), Vector2(rect_size.x, y), ColorN('green', 0.5))




#Sets the mouse cursor to something useful
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


func set_table(table):
	tbl = table
	update()


func _make_custom_tooltip(_for_text):
	var p = $ToolTipProto.duplicate()
#	p.text = for_text
	var pos = get_table_pos()
	var hint2 = ""
	
	if owner.note_scale and pos.x >=12:  #Display a helpful octave indicator on A and C notes.
		if int(pos.x) % 12 == 0:  hint2 = "n.a-%s\n" % (int(pos.x/12)-1)
		if int(pos.x) % 12 == 2:  hint2 = "n.c-%s\n" % (int(pos.x/12)-1)
	
	var yValue = tbl[int(pos.x)]
	
	if owner.float_scale and owner.rate_scale:
		yValue = 10000.0/ yValue if yValue>0 else 0
		
		yValue = str(int(yValue)) + "%" if yValue>0 else "0"
	elif owner.float_scale:
		yValue = str(yValue).pad_decimals(2) + "%"
	
	p.text = "%sx:%s\ny:%s" % [hint2, pos.x, yValue]
	return p
