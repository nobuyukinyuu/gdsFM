extends MenuButton

func _ready():
	
	
	var p = get_popup()
	var submenu = $EaseSubmenu
	
#	p.connect("index_pressed",self, "_on_Menu_index_pressed")
	p.connect("id_focused" ,self, "_on_Menu_index_pressed")
	remove_child(submenu)
	p.add_child(submenu)
	
	p.theme = submenu.theme
	
	p.add_separator("All Sides")
#	p.add_submenu_item("Left / Right ", "EaseSubmenu")
#	p.add_submenu_item("Right / Left ", "EaseSubmenu")
	p.add_submenu_item("Left / Right ", "EaseSubmenu")
	p.add_submenu_item("Right / Left ", "EaseSubmenu")
	p.add_separator("Left")
#	p.add_submenu_item("Left / Right ", "EaseSubmenu")
#	p.add_submenu_item("Right / Left ", "EaseSubmenu")
	p.add_submenu_item("L / R ", "EaseSubmenu")
	p.add_submenu_item("R / L ", "EaseSubmenu")
	p.add_separator("Right")
#	p.add_submenu_item("Left / Right ", "EaseSubmenu")
#	p.add_submenu_item("Right / Left ", "EaseSubmenu")
	p.add_submenu_item("L / R ", "EaseSubmenu")
	p.add_submenu_item("R / L ", "EaseSubmenu")
	
	
	
	print(p.is_item_disabled(1))
	
	pass

func _on_Menu_index_pressed(idx):
	print(idx)


	#TODO:  IMPLEMENT 12th ROOT OF 2 CURVE AS 1 / Pow(2, x/12) for descending,
	#		Pow(2, (x-127) / 12) for ascending
