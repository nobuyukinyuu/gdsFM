tool
extends Panel
var hChunk:float = rect_size.x / 3
var vChunk:float = rect_size.y / 4
const PRECISION = 16

export (float, -100, 100,1) var pal setget set_pal
export (float, 0, 10000,1) var par = 1000 setget set_par 

export (float, -100, 100,1) var pdl setget set_pdl
export (float, 0, 10000,1) var pdr = 50 setget set_pdr

export (float, -100, 100,1) var psl setget set_psl

export (float, 0, 10000,1) var prr = 5000 setget set_prr
export (float, -100, 100,1) var prl setget set_prl

const rates = ["par", "par", "pdr", "prr"]
const levels = ["pal", "pdl", "psl", "prl"]

const font = preload("res://gfx/fonts/numerics_5x8.tres")

func set_pal(val):
	pal = val
	update()
func set_pdl(val):
	pdl = val
	update()
func set_psl(val):
	psl = val
	update()
func set_prl(val):
	prl = val
	update()

func set_par(val):
	par = val
	update()
func set_pdr(val):
	pdr = val
	update()
func set_prr(val):
	prr = val
	update()


func _ready():
	update()
	pass

func _on_PitchDisplay_resized():
	update()



func _draw():
	hChunk = rect_size.x / 3
	vChunk = rect_size.y / 4
	
	var pts = []

	draw_line(Vector2(0,rect_size.y/2), Vector2(rect_size.x, rect_size.y/2), ColorN('white',0.3))

	for i in 4:
		#Draw octave indicators
		draw_line(Vector2(0,vChunk * i), Vector2(rect_size.x,vChunk * i), ColorN('white',0.2))

		#Determine points.
		var startPt_x = 0 
		
		var startPt_y = get(levels[i]) / 100.0 * vChunk*2 + rect_size.y / 2
		if i>0:
			var sclX = (get(rates[i]) / 10000.0)
			if sclX > 0:  sclX =pow(sclX, 0.25)  #Exponential scaling of width
			startPt_x = max(sclX * hChunk, 2)
			pts.append(Vector2(pts[pts.size()-1].x + startPt_x, rect_size.y - startPt_y))
		else:
			pts.append(Vector2(0, rect_size.y - startPt_y))
	
		#Draw dots on pts
		draw_circle(pts[i], 3, ColorN('white', 0.5))
	
	#Draw dotted line
	var h = rect_size.y/(PRECISION*2)
	for i in range(0, PRECISION*2, 2):
		draw_line(Vector2(pts[2].x, h*i), Vector2(pts[2].x, h*(i+1)), ColorN("white", 0.2),0.5,false)

	#Set lines	
	$Line2D.points = pts


	#Draw fonts
#	draw_string(font, Vector2(pts[2].x, 0), "Note Off")
	draw_texture(preload("res://gfx/ui/panels/note_off.png"), Vector2(pts[2].x+2, 3),ColorN('white', 0.5))
	
	var cumulative_time = 0
	var last_vpos = -999 #rect_size.y / 2
	for i in range(1, 4):
		cumulative_time += get(rates[i])

		var secs:String
		if cumulative_time >= 1000:
			secs = String( cumulative_time / 1000.0 ) 
			secs = secs.pad_decimals(2) + "s"
		else:
			secs = String(int(cumulative_time)) + "ms"
		
		
		var updown = sign( (get(levels[min(3, i+1)]) + get(levels[min(3, i-1)])) / 2 
							- get(levels[i])) #* -1
		if updown == -1:  updown = -2  #Double up dist
		if updown == 0:  updown = 1  #Prevent "on the line" positions

		var vpos = pts[i].y + 6*updown
		
		#Check if the last label was too close. If x distance is < 25px, y 8, add offset
		var offset = 0
		if (pts[i].x - pts[i-1].x < 25) and abs(vpos - last_vpos) < 8:
			var diff = vpos - last_vpos
			offset = sign(diff + 0.000001) * 10 #- diff
		
		if i == 2 and vpos < 12:  vpos = 12  #Accounts for NoteOff text
		
		vpos = clamp(vpos, 0, rect_size.y - 8 - offset)
		last_vpos = vpos + offset
		draw_string(font, Vector2(pts[i].x, vpos + offset).floor(), secs )




