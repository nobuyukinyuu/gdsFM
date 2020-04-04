#A lot of FM Instrument modular structure lives here, for now....
extends GraphEdit

#A temp list of connections used to validate an algorithm.
var test_connections = {}
var connections = {}  #Operator connections in an algorithm
var connections_valid = false

var dirty = false setget set_dirty  #Indicates if the node graph is out of date.
signal changed

func set_dirty(val):
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
#	prints("request from", from, "slot", from_slot, "to", to, ", slot", to_slot)

	if from != to:
		connect_node(from, from_slot, to, to_slot)
		set_dirty( true )
	else:
		prints("Warning:  Attempting to connect", from, "to itself")

	global.custom_algo = get_connection_list()

func _on_GraphEdit_disconnection_request(from, from_slot, to, to_slot):
	disconnect_node(from, from_slot, to, to_slot)
	set_dirty(true)

	global.custom_algo = get_connection_list()

func validate():
	
	var connection_list = get_connection_list()
	
	test_connections.clear()
	connections.clear()
	
	#Assemble a dictionary of everything each block's connected to.
	for o in connection_list:
		var key = o["to"]

		if !test_connections.has(key):
			test_connections[key] = {}
			
#		connections[key].append(o["from"])
		test_connections[key][ o["from"] ] = true

	print(test_connections, "\n", get_connection_list())

	#First, check if the speakers are connected to anything.
	if !test_connections.has("Output") or test_connections["Output"].empty():
		OS.alert("Output isn't connected to anything!")
		return
	
	#Now make sure there's no infinite loops in the operator connections.
	var is_invalid:bool = validate_loop("Output", {})
	if is_invalid:  
		connections_valid = false
		OS.alert("Algorithm validation failed.")
		return
	else:
		#Validation passed.  Assign connections.
		connections = test_connections
		connections_valid = true
		
#		#Clear all old connections.
#		for o in get_children():
#			if not o.is_in_group("operator"):  continue
#			o.connections.clear()

		print (connections)

#		for connection in connections:
#			get_node(connection).connections = connections[connection]
#
			
			
		#For the C# engine:  Tell the mixer to assign connections.
		#First, construct a string out of the connections because
		#the marshalling of nested dictionaries is face-melting
		var cs:String = ""
		for connection in connections:
			var ops:String = connection + "="
			for operator in connections[connection]:
				ops += operator + ","
			ops = ops.rstrip(",")
			cs += ops + ";"
		cs = cs.rstrip(";")

		print(cs)
		owner.get_node("Audio").UpdatePatchFromString(cs)
			

			
		
#		OS.alert("Validation OK.")
		set_dirty(false)
	
#Recursive function which validates an algorithm is free of infinite loops.

#TODO:  Doesn't work with common modulators!  Try to check only the input side
#		of the connections for loops?
func validate_loop(me, prior_connections, caller="?") -> bool:
	var out = false  #Return true if there's a loop detected.
	prior_connections[me] = true
	var next_connections:Dictionary

	if test_connections.has(me):
		next_connections = test_connections[me]
	else:
		next_connections = {}
	
	print(me, " next: ", next_connections, " prior: ", caller, ", ",
			 prior_connections)
	
	#Check all the next connections for a prior connection.
	for c in next_connections:
		if prior_connections.has(c) and c != "Output":
			OS.alert("Loop detected at %s to %s" % [me, c])
			return true
		else:  #So far so good.  Recurse.
			out = out or validate_loop(c, prior_connections.duplicate(), me)
	
	return out
