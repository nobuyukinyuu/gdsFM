#tool
extends GridContainer
export (int, 2, 8) var total_ops:int = 4 setget set_ops

const sProto = preload("res://sys/ui/wiringGrid/SlotProto.tscn")
const spk = preload("res://gfx/ui/icon_speaker.svg")

var last_slot_focused=0 setget reset_focus

var _grid = []  #References to opNodes in a particular grid position
var ops = []  #array of each operator being used and its connections
var isReady=false


func _ready():
#	reinit_grid(total_ops)
	isReady = true
	set_ops(total_ops)
	yield (get_tree(), "idle_frame")
	yield (get_tree(), "idle_frame")
	reset_default_op_positions()
	
	update()

func reset_focus(val):
	prints("unfocusing", last_slot_focused, "and focusing", val)
	if last_slot_focused >=0:
		get_node(str(last_slot_focused)).unfocus()
#		if val == last_slot_focused:  val = -1
	last_slot_focused = val

func set_ops(val):  #Set the number of operators in the grid.  Property setter.
#	print ("SetOps: ", val, "total_ops: ", total_ops)
	var oldsz = total_ops
	total_ops = val
	if not isReady:  return
	resize_op_array(val)
	reinit_grid(val)
	
	if val < oldsz:  
		reinit_connections()  #Grid's smaller.  Can't guarantee connection tree is valid.
		reset_default_op_positions()  #Gotta move the remaining ops left back to carrier positions.


func resize_op_array(newsz):  #Deals with re-initializing new opNodes in a larger array.
	var oldsz = ops.size()	
	ops.resize(newsz)
	
	if newsz > oldsz:
		#Fill with new opNodes.
		for i in range(oldsz, newsz):
			ops[i] = opNode.new()
			ops[i].id = i

func reinit_connections():  #Clears all opNode connections by making new opNodes for all ops.
	ops.clear()
	for i in total_ops:
		var p = opNode.new()
		p.id = i
		ops.append(p)

func reinit_grid(gridSize):  #Completely nuke the controls and rebuild the slot indicator grid.
	for o in get_children():
		if o.is_connected("dropped", self, "request_move"):
			o.disconnect("dropped", self, "request_move")
		if o.is_connected("right_clicked", self, "_onSlotRightClicked"):
			o.disconnect("right_clicked", self, "_onSlotRightClicked")
		o.queue_free()
	
	columns = gridSize
	
	yield (get_tree(), "idle_frame")
	yield (get_tree(), "idle_frame")

	resize_grid(gridSize)
	
	for i in range(gridSize*gridSize):  #Repopulate controls.
		var p = sProto.instance()
		p.name = str(i)
		p.editor_description = "Slot %s" % i
		p.gridPos = Vector2( i % gridSize, i/gridSize )
		p.connect("dropped", self, "request_move")
		p.connect("right_clicked", self, "_onSlotRightClicked")
		p.set_slot(-1, 0)  #0=None
		add_child(p)
		p.owner = owner

	update()

func reset_default_op_positions():  #Flips the opNodes positions to default and changes slot indicators to match.
	var start = total_ops*total_ops - total_ops
	for i in total_ops:
		var p = get_child(start+i)
		p.set_slot(i, 1)  #1=carrier

		setGridID(p.gridPos, i)

		#Move the op nodes to the new positions.
		ops[i].gridPos = p.gridPos

	redraw_grid()

func clearGridIDs():  #Fills grid with nulls.
	resize_grid(total_ops)
func resize_grid(newsz):
	_grid.clear()
	for i in newsz:
		var arr = []
		arr.resize(newsz)
		_grid.append(arr)

#Grid operations
func getGridID(pos):		return _grid[pos.x][pos.y]
func setGridID(pos,val):	_grid[pos.x][pos.y] = val
func resetGridID(pos):  	_grid[pos.x][pos.y] = null
func gridPosIsEmpty(pos):	return _grid[pos.x][pos.y] == null
func slotNodeAt(pos):
	return get_node(str(pos.y * total_ops + pos.x))


func redraw_grid():
	for slot in get_children():
		slot.reset()
	
	for op in ops:
		if op.pos_valid():  #Get the slot and set it
			var slot = slotNodeAt(op.gridPos)
			var opType = 1 if op.gridPos.y == total_ops-1 else 2
			slot.set_slot(op.id, opType)
		
	update()

#Tree logic:  When requesting connection from one op to another, make sure the destination operator
#		 isn't in the list of the source operator's connections.  If it is, swap the connections
#		between the 2 operators.  Consider updating the drag tooltip to state a swap? 
#		Any operators that weren't part of the swap should update references to the old op with the new one.
# 		If a swap isn't the intended operation, Remove the entire
#		reference tree starting at any operators that connect to the source op.  Reconnect to dest op.


enum targets {nothing, output, operator, swap}
func check_target_type(source, dest):
	if getGridID(source) == getGridID(dest):  return [targets.nothing, 0]
	var source_op = ops[getGridID(source)]
	var dest_op = getGridID(dest)
	if dest_op != null:  dest_op = ops[dest_op]  #Only fetch the opNode if it's actually there.
												#If not, we'll find a proper spot to connect to later.

	if dest_op != null:  #Determine whether the destination is in the connections of the source op.
		if source_op.has_connection_to(dest_op):
			return [targets.swap, dest_op.id]
		else:  #Bingo, modulator
			return [targets.operator, dest_op.id]
	else:
		if dest.y+1 == total_ops:  return [targets.output, 0]
		#TODO:  Find free slot drop-down here and indicate the target in the diagram.
		return [targets.nothing, 0]

func request_move(source, dest):
	if getGridID(source) == getGridID(dest):  return
	
	#If anything goes wrong with the operation, restore the grid to the original state.
	var backup_grid = _grid.duplicate(true)
	var backup_ops = ops.duplicate(true)
	
	print ("Dropped from ", source, " into ", dest)
	#Get source operator.
	var source_op = ops[getGridID(source)]
	var dest_op = getGridID(dest)
	if dest_op != null:  dest_op = ops[dest_op]  #Only fetch the opNode if it's actually there.
												#If not, we'll find a proper spot to connect to later.

	if dest_op != null:  #Determine whether the destination is in the connections of the source op.
		#If so, swap their connections and grid positions and call it a day.
		if source_op.has_connection_to(dest_op):
			source_op.swap_connections_with(dest_op)  

			for o in ops:  #The connections were swapped, but not the references inside them.  Do it now.
				global.arr_replace(o.connections, source_op, 0xFFFF)
				global.arr_replace(o.connections, dest_op, source_op)
				global.arr_replace(o.connections, 0xFFFF, dest_op)
			
			#Now swap their grid positions.
			setGridID(dest, source_op.id)
			setGridID(source, dest_op.id)
			source_op.gridPos = dest
			dest_op.gridPos = source
			redraw_grid()
			return
			
		else:
			#We bingo'd a destination that's free to connect to, so add the source op to the dest op connections.
			#We also need to update the grid to reflect the new stack, and all ops in the tree need free slots 
			#found on the levels we put them on.
			
			
			#Remove the tree from the grid and break source ops' connections to it.
			remove_tree_at(source)
			break_all_connections_to(source_op)
			
			#Now connect the source to the destination and find slots for the tree.
			dest_op.connections.append(source_op)

			#The ops for the tree have invalid grid positions, so we need to find free positions for all of them.
			#Using the source_op's Y position, we find an open space in the level it inhabits on the op stack.
			#The first call to recursive func should be level of dest-1. This calls func for all connections
			#until each one is found a home.
			find_free_slots(source_op, dest.y-1, dest.x)
			
			#eg:  find_free_slots(source_op, destPos) calls func(each_connection_op, source_op.gridPos)
			#This is different from finding a connection point, which we do later if we didn't bingo.
			#TODO ======================================
			
			#Next:  redraw grid and exit out
			redraw_grid()
			return
	

func find_free_slots(op, level:int, start_from=0):
	print("Looking for free slot for OP%s on level %s starting at %s..." % [op.id, level, start_from])
	#Finds free slots for all items on a tree.
	op.gridPos = free_slot(level, start_from)
	setGridID(op.gridPos, op.id)

	for connection in op.connections:
		find_free_slots(connection, level-1, start_from)

func free_slot(level, start_from=0):  #returns the position of the first free slot on the level (ypos) specified.
	for x in range(start_from, total_ops):
		var pos = Vector2(x, level)
		if getGridID(pos) !=null:  continue
		return pos
			
	#Uh-oh.  Couldn't find a spot to the right of the operator.  Let's look to the left.
	var rev_range = range(0, start_from)
	rev_range.invert()
	for x in rev_range:
		var pos = Vector2(x, level)
		if getGridID(pos) !=null:  continue
		return pos

	print("free_slot():  No free slot found at level %s!!" %level)
	assert(false)

func break_all_connections_to(source_op):
	#Remove all references to the source operator in other operators' connections.
	for op in ops:
		var idx = op.connections.find(source_op)
		if idx >= 0: op.connections.remove(idx)
	
	
#Removes an entire operator tree off the grid.
func remove_tree_at(gridPos):
	if gridPosIsEmpty(gridPos):
		print ("remove_tree_at(%s): Nothing here..." % gridPos)
		return

	var sourceID = getGridID(gridPos)

	#Assemble a list of IDs to remove from grid.
	var ids = {sourceID: sourceID}
	ops[sourceID].get_tree_ids(ids)
	
	print ("Removing tree for OPs ", ids)
	
	for id in ids.keys():
		resetGridID(ops[id].gridPos)



func find_connection_point(dest):
	#Find a free connection point vertically on this stack. It's the first operator below drop dest.
	#If there's no operators found, then the operator we're dragging becomes a new carrier.
	#Function that calls this will be called separately

	#Get the first free slot below where the user dropped his tree.
	var free_x = total_ops
	for i in range(dest.y, total_ops):
		
		pass   #TODO================================
	



#======================= DRAW ROUTINES ================================
func _draw():
	#Draw the output diagram
	var tile_size = rect_size / total_ops
	var y = rect_size.y - tile_size.y/4

	for i in total_ops:
		var a = Vector2(i * tile_size.x + tile_size.x / 2, y)
		var b = Vector2(a.x, rect_size.y + 8)
		draw_line(a,b, ColorN("white"),1.0, true)

	y = rect_size.y + 8
	var half = tile_size.x / 2
	draw_line(Vector2(half, y), Vector2(rect_size.x, y), ColorN("white"), 1.0, true)
	draw_texture(spk,Vector2(rect_size.x, y) - Vector2(8,8))
	
	#Draw connections.
	for op in ops:
		for connection in op.connections:
			draw_connection(op.gridPos, connection.gridPos)

func draw_connection(source, dest):
	#Translate the connection point to the control's location on the grid.
	var tile_size = rect_size / total_ops
	source *= tile_size
	dest *= tile_size

	source += tile_size / 2
	dest += tile_size / 2

	var nudge = tile_size / 4
	var x_bias = sign(dest.x-source.x) * nudge.x
	source.x += x_bias
	dest.x -= x_bias
	source.y -= nudge.y
	dest.y += nudge.y
	
	draw_arrow(source, dest) 
	
func draw_arrow(a, b, color=Color(1,1,1,1), width=1.0):
	var arrow_spread= PI/6
	var arrow_length = 4
	var pts:PoolVector2Array
	pts.resize(3)
	pts[1] = a

	var angle = atan2(a.y-b.y, a.x-b.x) + PI
	
	pts[0] = Vector2(a.x + arrow_length*cos(angle+arrow_spread), a.y + arrow_length*sin(angle+arrow_spread))
	pts[2] = Vector2(a.x + arrow_length*cos(angle-arrow_spread), a.y + arrow_length*sin(angle-arrow_spread))

	draw_line(a,b,color,width, true)
	draw_line(a,pts[0],color,width, true)
	draw_line(a,pts[2],color,width, true)


func _onSlotRightClicked(id):
	var slot = get_node(str(id))
	if slot.id < 0:
		slot.hint_tooltip = "Slot %s: (Nothing here)" % id
	else:
		slot.hint_tooltip = "Slot %s. %s" % [id, ops[slot.id].get_connection_string()]

#===================== CLASS =============================
class opNode:
	var id = 0  
	var connections = []
	var gridPos = Vector2.ONE * -1

	func pos_valid():  return gridPos != Vector2(-1,-1)

	func reset_pos():
		gridPos = Vector2.ONE * -1

	func has_connection_to(op):  #Recursively checks if another operator is in the tree.
		for o in connections:
			if o == op:  return true
			if o.has_connection_to(op):  return true
		return false
		
	func swap_connections_with(op):  #Note:  You need to also update the grid slots to reflect this
		prints("Swapping connections between", id+1, "and", op.id+1)
		var c = connections
		global.arr_replace(c, id, op.id)
		global.arr_replace(op.connections, op.id, id)
		connections = op.connections
		op.connections = c

	func get_tree_ids(output_dict={}):  #Returns the IDs of every operator connected to this tree.
		for o in connections:
			output_dict[o.id] = o.id
			o.get_tree_ids(output_dict)

	func get_connection_string():
		var output = "OP" + str(id+1) + ": ["
		for op in connections:
			output += str(op.id+1) + ", "
		return output + "]"
