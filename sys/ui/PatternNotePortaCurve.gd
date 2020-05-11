tool
extends Control
export (Texture) var texture = preload("res://gfx/ui/sequencer/pill.png")
export (Vector2) var noteOffset = Vector2.ZERO
export (Vector2) var note2Offset = Vector2.ZERO
const offsets=[-2,-2,-1,-1,0,0,-1,-2,-2,-3,-3,-4]  #12-TET grid offsets

export (bool) var reverse

var textureFlip:Texture

var curve = Curve2D.new()

# Called when the node enters the scene tree for the first time.
func _ready():
	rect_min_size.x = texture.get_size().x * 2

	update_texture()
	
	resize_curve()
	update()

func _notification(what):
	if what == NOTIFICATION_RESIZED or what == NOTIFICATION_VISIBILITY_CHANGED:
		if what == NOTIFICATION_VISIBILITY_CHANGED: update_texture()  #Editor disposes of texture if invisible?
		resize_curve()
		update()


func update_texture():
	var img = Image.new()
	img.copy_from(texture.get_data())
	img.flip_y()
	textureFlip = ImageTexture.new()
	textureFlip.create_from_image(img, texture.flags)
	
	

func resize_curve():
	curve.clear_points()
	var sz = texture.get_size()
	var j = Vector2.ZERO
	var h = Vector2(-sz.x/2.0, sz.y / 2.0)

	if !reverse:
		curve.add_point(noteOffset-h, Vector2.ZERO,Vector2(0,1)*rect_size +h)
		curve.add_point(Vector2.ONE*rect_size + note2Offset+h, 
						Vector2(0,-1)*rect_size -h,Vector2.ZERO)
	else:
		h.x *= -1
		curve.add_point(Vector2(rect_size.x, 0) + noteOffset-h, Vector2.ZERO,Vector2(0,1)*rect_size +h)
		curve.add_point(Vector2(0, rect_size.y) + note2Offset+h, 
						Vector2(0,-1)*rect_size -h,Vector2.ZERO)


func _draw():
	#Get src texture rects
	var sz = texture.get_size()
#	var top = Rect2(0,0, sz.x, min(sz.y/3, rect_size.y/2) )
#	var btm = Rect2(Vector2(0, sz.y - top.size.y), top.size )


	var offset =  noteOffset #- Vector2(top.size.x/2, top.size.y)



	$Line2D.texture = textureFlip if rect_size.y < 5 else texture
	$Line2D.texture = texture
	$Line2D.points = curve.get_baked_points()
	

