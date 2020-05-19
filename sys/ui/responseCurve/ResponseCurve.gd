extends Panel

#Mapped value maximum for the element this control modifies.
export(int,1,255) var maxValue = 100  setget set_maxvalue


var dirty = false  #When altered this value changes to indicate data is ready to send.
signal value_changed  #emitted in $VU

func set_maxvalue(val):
	maxValue = val
	
	if is_inside_tree():  
		$VU.maxValue = val


func _ready():
	$VU.maxValue = maxValue

	yield(get_tree(),"idle_frame")
	yield(get_tree(),"idle_frame")
	pass_table(null, "")

#Gets the split value of a position 0-31
func get_split(value):
	var output:CheckButton =  $Splits.get_node(String(value))
	
	if output == null:
		print ("ResponseCurve:  Warning, split ", value, " doesn't exist")
		return false
		
	return output.pressed


func pass_table(to, property:String):
	var output:PoolRealArray = PoolRealArray($VU.tbl)
	
	for i in output.size():
		output[i] = lerp(0, maxValue, i/100.0)

	if to:
		to.set(property, output)
	else:
		print("ResponseCurve: Can't find the given object to set the '", property,"' field.")

#Updates the entire preview table at once.
func updatePreviewTable():
	emit_signal("value_changed", -1, $VU.tbl)
