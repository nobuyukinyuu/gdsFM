extends TextureRect
export(Color) var fillColor = Color("003e6b")
export(Color) var lineColor = Color("0072c4")

var tbl = []
var elementWidth = 2

const overlay = preload("res://gfx/ui/vu/dotmatrix_overlay.png")

func _ready():
	owner.get_node("Popup/ResponseCurve").connect("value_changed", self, "_on_table_changed")
	
	
	for i in 128:
		tbl.append(0)


func _draw():
	var tbl2 = []

	for i in range(0, rect_size.x):
		var val = tbl[ lerp(0, 128, i / float(rect_size.x)) ]
		var pos = lerp(rect_size.y, 0, val/100.0)
		draw_line( Vector2(i+1,pos), Vector2(i+1,rect_size.y), fillColor )
		tbl2.append(pos)

	for i in range(0, tbl2.size()-1):
		if tbl2[i] >= rect_size.y:  continue
		var col = lineColor
		if tbl2[i] >= rect_size.y-1:  col.a *=0.5


		#Range_lerp for cases when the generated table doesn't match the draw rect width....
#		var pos = Vector2(range_lerp(i, 0,tbl2.size(), 0, rect_size.x), tbl2[i])
#		var pos2 = Vector2(range_lerp(i+1, 0,tbl2.size(), 0, rect_size.x), tbl2[i+1])

		var pos = Vector2(i+1, tbl2[i])
		var pos2 = Vector2(i+2, tbl2[i+1])
		draw_line(pos, pos2, col, 2, false)

	#Draw Overlay.
	for i in 3:
		draw_texture(overlay, Vector2.ZERO)


func _on_table_changed(idx, val):
	tbl[idx] = val
	update()
