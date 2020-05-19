extends TextureRect

var mask = ImageTexture.new()

func _ready():
	
	$"0".connect("toggled", self, "_on_Split_toggled", ["0"])
	for i in range(1,32):
		var p = $"0".duplicate()
		p.name = str(i)
		p.rect_position.x = i * 10
		
		p.connect("toggled", self, "_on_Split_toggled", [p.name])
		
		add_child(p)
		p.owner=owner
		p.rect_size = Vector2(10,40)
	
		var v = floor(i * 4)
		p.hint_tooltip = "> %s-%s | %s" % [v, v+3, v+4]
		if i == 31:  p.hint_tooltip = "> %s-%s | (No effect)" % [v, v+3]
	pass

	#Pass in a texture to the VU split indicator
	var img:Image = Image.new()
	img.create(32,1,false,Image.FORMAT_RGB8)
	mask.create_from_image(img,0)
	assert (mask!=null)
	assert (img!=null)
	
	owner.get_node("VU/Overlay").material.set_shader_param("mask", mask)

func _on_Split_toggled(pressed, whomst):
#	get_node(whomst)
	
	var column=int(whomst)
	var tex = mask

	
	if tex !=null:
		var img:Image = tex.get_data()
		img.lock()
		img.set_pixel(column,0, Color(255<<24) if pressed else Color(0) )
		img.unlock()
		mask.create_from_image(img,0)
	pass


#Find the first or last split enabled.
func split_index(last_instance=false):
	if !last_instance:
		#Go through entire split
		for i in get_child_count():
			var o = get_child(i)
			if o is CheckButton and o.pressed:  return i
		return -1
	else:  #Find the last instance of the split instead.
		for i in range(get_child_count()-1, -1, -1):
			var o = get_child(i)
			if o is CheckButton and o.pressed:  return i
		return -1
