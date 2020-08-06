extends Panel

#Mapped value maximum for the element this control modifies.
export(int,1,255) var maxValue = 100  setget set_maxvalue
export(String) var title setget set_title

var note_scale = true  #Set to false if the x-axis isn't mapping note numbers.
var float_scale = true  #Set to false if the y-axis isn't mapping percentage but direct remapped values.
var rate_scale = false  #Set to true if the y-axis maps a rate (ie:  percentage of original time)

#if true: When marshalling the table to c#, zero values will become epsilon.
#this is to prevent rate curve calculations or others that don't play well with zeroes producing NaNs.
#export(bool) var allow_zero

var dirty = false  #When altered this value changes to indicate data is ready to send.
signal value_changed  #emitted in $VU
signal minmax_changed  

func set_title(val):
	title = val
	$lblTitle.text = val

func set_maxvalue(val):
	maxValue = val
	
	if is_inside_tree():  
		$VU.maxValue = val


func _ready():
	$VU.maxValue = maxValue

	yield(get_tree(),"idle_frame")
	yield(get_tree(),"idle_frame")

#Gets the split value of a position 0-31
func get_split(value):
	var output:CheckButton =  $Splits.get_node(String(value))
	
	if output == null:
		print ("ResponseCurve:  Warning, split ", value, " doesn't exist")
		return false
		
	return output.pressed


#Passes the table to the specified field.  Only works if the property specified is a godot array, probably...
#func pass_table(to, property:String):
#	var output:PoolRealArray = PoolRealArray($VU.tbl)
#	var minimum = global.EPSILON if dont_pass_0 else 0
#
#	for i in output.size():
#		output[i] = range_lerp( clamp(output[i], minimum, maxValue), 0, 100, 0, maxValue)
#
#	if to:
#		to.set(property, output)
#	else:
#		print("ResponseCurve: Can't find the given object to set the '", property,"' field.")
##		print(to_json(output))

#Like pass_table(), but relies on RTable<T>.SetValues()
func set_rtable(envelope, target:String):
	var output = {}
	var values:PoolRealArray = PoolRealArray($VU.tbl)

	#Set values
#	var minimum = global.EPSILON if dont_pass_0 else 0
	var minimum = 0
	
	for i in values.size():
#		values[i] = range_lerp( clamp(values[i], minimum, maxValue), 0, maxValue, 0, 100)
		values[i] = clamp(values[i], minimum, 100)

	#Set options
	output["values"] = values
	output["use_log"] = $chkLinlog.pressed
	output["floor"] = $sldMin.value
	output["ceiling"] = $sldMax.value
#	output["allow_zero"] = allow_zero

	if envelope:
#		var rtable = envelope.get(target)  #Destination RTable<T> output's being sent to.

#		if rtable:
		if property_exists(target, envelope.get_property_list()):
#			rtable.call("SetValues", output )
			envelope.set(target, output )
		else:
			print("ResponseCurve: Can't find the target property '", target,"' to send the table to.")
	else:
		print("ResponseCurve: Can't find the given envelope to set the '", target,"' field.")

#Fetches a response curve from the RTable<T> specified with target inside the given envelope.
func fetch_table(envelope, target:String):
	if envelope:
		var input:Dictionary = envelope.get(target) #Should trigger property's "ToDict" func.
		
		if input:
			$VU.set_table(input.get("values"))
			$sldMax.value = input.get("ceiling", 100)
			$sldMin.value = input.get("floor", 0)
			$chkLinlog.pressed = input.get("use_log", false)
#			if target == "Ksr":  print(input)
			return input
			
		else:
			print("ResponseCurve: Can't find RTable '", target,"' to fetch the table from.")
	else:
		print("ResponseCurve: Can't find the given envelope to fetch the table of '", envelope,"' from.")

#Updates the entire preview table at once.
func updatePreviewTable():
	emit_signal("value_changed", -1, $VU.tbl)


func _on_MinMax_value_changed(_value):
	#Brackets represent a condensed ligature for "100" in numerics_5x8 font
	var _min:String = String($sldMin.value).pad_zeros(2) if $sldMin.value < 100 else "[]"
	var _max:String = String($sldMax.value).pad_zeros(2) if $sldMax.value < 100 else "[]"
	$lblMinMax.text = "%s/%s" % [_min, _max]
	
	$VU.update()
	emit_signal("minmax_changed", $sldMin.value, $sldMax.value)


func property_exists(name:String, input:Array) -> bool:
	#Currently, a bug seems to exist where fetching property list from C# objects aren't properly marshalled
	#in all circumstances for an export template build.  This doesn't seem to be the case for editor/debug builds
	#So one workaround may be to ignore the check altogether and simply crash if the property doesn't exist
	#if we detect we're running a standalone build.  See link for more details:
	#https://github.com/godotengine/godot/issues/40108   (FIXME)
	
	if !OS.has_feature("standalone"):
		for item in input:
			if item["name"] == name:  return true
			
		return false
	else:
		return true  #Skip check
		
		


# Copy paste
func _on_CPMenu_index_pressed(index):
	match index:
		0:  #Copy
#			OS.clipboard = 
			pass
		1:  #Paste
			pass
	pass # Replace with function body.
