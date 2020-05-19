extends MenuButton
enum {LINEAR, IN, OUT, IN_OUT, OUT_IN, TWELFTH_ROOT_OF_2}
enum {BOTH_SIDES, LEFT_ONLY, RIGHT_ONLY}

const CURVEMAP = [1, 2, 0.5, -2, -0.5]


func _ready():
	
	
	#We need to move the ease submenu to the invisible child popup of self.
	var p = get_popup()
	var submenu = $EaseSubmenu
	p.theme = submenu.theme

#	p.connect("index_pressed",self, "_on_Menu_index_pressed")
#	remove_child(submenu)
#	p.add_child(submenu)
	

	#FIXME:  THIS IS A HACKY WORKAROUND TO LACK OF PopupMenu.get_current_index()
	#		CONSIDER REPLACING IT IF IT IS AVAILABLE IN GODOT 3.2.2
	for i in 6:
		var q = $EaseSubmenu.duplicate()
		q.name = "Ease%s" % i
		p.add_child(q,true)
		q.owner = owner
		
		q.connect("index_pressed", self,"_on_EaseMenu_index_pressed", [i, i%2 !=0])


	$EaseSubmenu.queue_free()
	
	
	p.add_icon_item(preload("res://gfx/ui/icon_reset.svg"), "Default")
	p.add_separator("All Sides")
#	p.add_submenu_item("Left / Right ", "EaseSubmenu")
#	p.add_submenu_item("Right / Left ", "EaseSubmenu")
	p.add_submenu_item("Ascending (L / R)", "Ease0")
	p.add_submenu_item("Descending (R / L) ", "Ease1")
	p.add_separator("Left")
#	p.add_submenu_item("Left / Right ", "EaseSubmenu")
#	p.add_submenu_item("Right / Left ", "EaseSubmenu")
	p.add_submenu_item("LEFT: Ascending", "Ease2")
	p.add_submenu_item("LEFT: Descending ", "Ease3")
	p.add_separator("Right")
#	p.add_submenu_item("Left / Right ", "EaseSubmenu")
#	p.add_submenu_item("Right / Left ", "EaseSubmenu")
	p.add_submenu_item("RIGHT: Ascending", "Ease4")
	p.add_submenu_item("RIGHT: Descending ", "Ease5")

#	p.add_separator()
#	p.add_icon_item(preload("res://gfx/ui/icon_reset.svg"), "Default")

	p.rect_size.x += 32

#Activated when one of the ease menus is selected.
func _on_EaseMenu_index_pressed(curveType, parent_index, descending):
	var startpos = -1
	var endpos = -1
	
	#Determine the area we need to apply the curve preset.
	match int(parent_index/2):
		LEFT_ONLY:
			endpos = owner.get_node("Splits").split_index()
#			print ("Left only.  Startpos is ", startpos)
		RIGHT_ONLY:
			startpos = owner.get_node("Splits").split_index(true)
#			print ("Right only.  Startpos is ", startpos)

	#Map values to the number of notes in the MIDI spec, 128.
	if endpos == -1:  endpos = 31
	startpos += 1
	endpos += 1

	startpos *= 4  #0 becomes 0, 1 becomes 4, etc.
	endpos *= 4    #31 becomes 124, 32 becomes 128, etc.

#	#Make sure the value map always goes from lowest to highest.
#	if endpos < startpos:
#		var temp = startpos
#		startpos = endpos
#		endpos = temp

	#Now, apply the curve to the table.
	var tbl = owner.get_node("VU").tbl
	var size = float(endpos-startpos)  #Number of elements.
	match curveType:
		LINEAR:
			for i in range(startpos, endpos):
				var val = lerp(0, 100, i/(startpos+size))
				var pos = startpos+endpos-i-1 if descending else i
				tbl[pos] = val

		IN, OUT, IN_OUT, OUT_IN:
			for i in range(startpos, endpos):
				var val = ease(i/(startpos+size), CURVEMAP[curveType]) * 100
				var pos = startpos+endpos-i-1 if descending else i
				tbl[pos] = val

		TWELFTH_ROOT_OF_2:
			if descending:
				for i in range(startpos, endpos):
					var pos = range_lerp((i-startpos), 0, endpos-startpos, 0, 128) 
					tbl[i] = 1 / pow(2, (pos)/12.0) * 100
			else:  #Ascending
				for i in range(startpos, endpos):
					var j = range_lerp((i-startpos), 0, size, 0, 128)
#					print (j)
					tbl[i] = pow(2, (j-127)/12.0) * 100
				

	owner.get_node("VU").update()
	owner.updatePreviewTable()

	#  12th ROOT OF 2 CURVE is implemented as 1 / Pow(2, x/12) for descending,
	#  Pow(2, (x-127) / 12) for ascending
