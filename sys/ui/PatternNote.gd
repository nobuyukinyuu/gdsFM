tool
extends Control
class_name PatternNote, "res://gfx/ui/icon_note.svg"
export (Texture) var texture = preload("res://gfx/ui/pill.png")
export (Vector2) var noteOffset = Vector2.ZERO
const offsets=[-2,-2,-1,-1,0,0,-1,-2,-2,-3,-3,-4]  #12-TET grid offsets

var midtex = AtlasTexture.new()

# Called when the node enters the scene tree for the first time.
func _ready():
	rect_min_size.x = texture.get_size().x

	var sz = texture.get_size()
	var mid = Vector2(sz.x, min(sz.y/3, rect_size.y/2))
	midtex.region = Rect2(Vector2(0,mid.y), mid)
	midtex.atlas = texture

	update()

func _notification(what):
	if what == NOTIFICATION_RESIZED:
		update()

func _draw():
	#Get src texture rects
	var sz = texture.get_size()
	var top = Rect2(0,0, sz.x, min(sz.y/3, rect_size.y/2) )
	var btm = Rect2(Vector2(0, sz.y - top.size.y), top.size )


	var offset = Vector2(rect_size.x/2 - sz.x/2, 0) + noteOffset

	#Blit dest texture rects
	draw_texture_rect(midtex, Rect2(Vector2(0,top.size.y)+offset, 
			Vector2(sz.x, rect_size.y-top.size.y*2)), true)
	
	draw_texture_rect_region(texture, Rect2(offset, top.size), top)
	draw_texture_rect_region(texture, Rect2(Vector2(0,rect_size.y-btm.size.y)+offset,btm.size), btm)

	
