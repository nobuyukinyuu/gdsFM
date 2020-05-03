extends Panel

#Mapped value maximum for the element this control modifies.
export(int,1,255) var maxValue = 100  setget set_maxvalue

func set_maxvalue(val):
	maxValue = val
	$VU.maxValue = val


func _ready():
	pass


#Gets the split value of a position 0-31
func get_split(value):
	var output:CheckButton =  $Splits.get_node(String(value))
	
	if output == null:
		print ("ResponseCurve:  Warning, split ", value, " doesn't exist")
		return false
		
	return output.pressed
