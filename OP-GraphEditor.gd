extends GraphEdit

#A temp list of connections used to validate an algorithm.
var test_connections = {}
var connections = {}  #Operator connections in an algorithm

var dirty = false setget set_changed  #Indicates if the node graph is out of date.
signal changed

func set_changed(val):
	dirty = val
	emit_signal("changed", dirty)


func _ready():
	
	add_valid_connection_type(0,1)
	add_valid_connection_type(1,0)
	
	add_valid_left_disconnect_type(0)
	add_valid_left_disconnect_type(1)
	
	add_valid_right_disconnect_type(0)
	add_valid_right_disconnect_type(1)
	
	for o in get_children():
		if o is GraphNode:
			o.title = o.name


func _on_GraphEdit_connection_request(from, from_slot, to, to_slot):
	prints("request from", from, "slot", from_slot, "to", to, ", slot", to_slot)

	if from != to:
		connect_node(from, from_slot, to, to_slot)
		set_changed( true )
	else:
		prints("Warning:  Attempting to connect", from, "to itself")


	#For processing output, we want to request the sample packet from the input.
	#The request function should be given a list of processed nodes as an argument.
	#The operator requesting a sample should add itself to the list it received.
	#Then, each node recursively checks its own inputs, and requests a waveform
	#from the input node if that node isn't already in the list.
	#
	# Once receiving the waveform, it performs frequency oscillation and passes
	# the output to the function caller.

func _on_GraphEdit_disconnection_request(from, from_slot, to, to_slot):
		disconnect_node(from, from_slot, to, to_slot)
		set_changed(true)


func validate():
	var connection_list = get_connection_list()
	
	test_connections.clear()
	
	#Assemble a dictionary of everything each block's connected to.
	for o in connection_list:
		var key = o["to"]

		if !test_connections.has(key):
			test_connections[key] = {}
			
#		connections[key].append(o["from"])
		test_connections[key][ o["from"] ] = true

	print(test_connections)

	#First, check if the speakers are connected to anything.
	if !test_connections.has("Output") or test_connections["Output"].empty():
		OS.alert("Output isn't connected to anything!")
		return
	
	#Now make sure there's no infinite loops in the operator connections.
	var is_invalid:bool = validate_loop("Output", {})
	if is_invalid:  
		OS.alert("Algorithm validation failed.")
		return
	else:
		#Validation passed.  Assign connections.
		connections = test_connections
#		OS.alert("Validation OK.")
		set_changed(false)
	
#Recursive function which validates an algorithm is free of infinite loops.
func validate_loop(me, prior_connections) -> bool:
	var out = false  #Return true if there's a loop detected.
	prior_connections[me] = true
	var next_connections:Dictionary

	if test_connections.has(me):
		next_connections = test_connections[me]
	else:
		next_connections = {}
	
	#Check all the next connections for a prior connection.
	for c in next_connections:
		if prior_connections.has(c):
			OS.alert("Loop detected at %s to %s" % [me, c])
			return true
		else:  #So far so good.  Recurse.
			out = out or validate_loop(c, prior_connections)
	
	return out
