extends TextureRect

func _ready():
	
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

func _on_Split_toggled(pressed, whomst):
#	get_node(whomst)
	pass
