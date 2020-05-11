tool
extends Control
class_name PatternNotePorta, "res://gfx/ui/godot_icons/old_bezier.svg"

#PatternNotePorta relies on all notes which need to be connected to it to be inside the Notes node.
#To connect these notes with portamento, some kind of control key modifier should be held while dragging.
#To disconnect this can also be used, but consider a context menu that allows a note to be taken out of
#The Notes node and "broken apart" such that a PatternNote returns back to the owner node.

export (bool) var reload = false setget reload

func reload(var val):
	reload_curves()


func _ready():
	reload_curves()


func ySort(a:Node, b:Node):
	return a.rect_position.y < b.rect_position.y

func reload_curves():
	#When the number of children in Notes changes, this is called to produce proper curve prototypes.
	#Each curve is placesd between the bounds of the appropriate nodes in the collection.
	#If a note in the collection isn't properly aligned, it should probably not be placed into the collection.
	
	#First, we have to sort notes by their Y order. Let's try a dumb hack
	var order = []
	for o in $Notes.get_children():  #Putting children in a dictionary will sort em by y-order
		order.append(o)
		
	order.sort_custom(self,"ySort")

	for i in order.size():
		$Notes.move_child(order.pop_back(),0)
	
	#Next, clear the Curves and generate a prototype for each.
	for o in $Curves.get_children():
		$Curves.remove_child(o)
		o.name = "FreedCurve"  #Prevents weird numbering when reallocating
		o.queue_free()
		
		
	for i in range($Notes.get_child_count()-1):
		var p = preload("res://sys/ui/PatternNotePortaCurve.tscn").instance()
		var a = $Notes.get_child(i)
		var b = $Notes.get_child(i+1)
		var recta = Rect2(a.rect_position,a.rect_size)
		var rectb = Rect2(b.rect_position,b.rect_size)
		
		if rectb.position.x < recta.position.x:  
			p.reverse = true
			var temp
			temp = recta.position.x
			recta.position.x = rectb.position.x
			rectb.position.x = temp
		
		
		p.rect_position = Vector2(recta.position.x, recta.end.y)
		p.rect_size = rectb.position - recta.end
		p.rect_size.x += recta.size.x + rectb.size.x
		
		
		$Curves.add_child(p)
		p.owner = self
		p.update()





