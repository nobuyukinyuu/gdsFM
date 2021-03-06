extends Control

var lastOperatorEnvelope  #Preview from GraphNode of last operator selected.



func _ready():
	
	#Setup tabs
	$TC/EGControl.disable(true)
	$TC.set_tab_title(0, "EG")
	$TC.set_tab_title(2, "Tune")
	$TC.set_tab_title(3, "Wave")
	$TC.set_tab_icon(0, preload("res://gfx/ui/icon_adsr.svg"))
	$TC.set_tab_icon(1, preload("res://gfx/ui/icon_curve.svg"))
	$TC.set_tab_icon(2, preload("res://gfx/ui/icon_tuning.svg"))
	$TC.set_tab_icon(3, preload("res://gfx/ui/icon_waveform.svg"))
	$TC.set_tab_icon(4, preload("res://gfx/ui/icon_response.svg"))
	
	$TC.set_popup($CPMenu)	
	
	$TC2.set_tab_icon(0, preload("res://gfx/ui/icon_note2.svg"))
	$TC2.set_tab_icon(1, preload("res://gfx/ui/icon_lfo.svg"))
	$TC2.set_tab_icon(2, preload("res://gfx/ui/icon_pitch.svg"))
	$TC2.set_tab_icon(3, preload("res://gfx/ui/icon_response.svg"))
	
	#Setup envelope connections
	$TC/EGControl.connect("envelope_changed", self, "_on_Env_update")
	$TC/Curve.connect("envelope_changed", self, "_on_Env_update")

	if !$Audio.patch:
		$Audio.NewPatch()
		yield(get_tree(),"idle_frame")
		global.currentPatch = $Audio.patch
		
	$TC2/Wave.fetch_table()

func _input(event):

#	if Input.is_key_pressed(KEY_F12):
#		print (global.currentEG._susTime)

	if Input.is_action_just_pressed("Output"):
#		print(global.currentEG.ToString())
#		OS.clipboard = (global.currentEG.ToString())
		OS.clipboard = (global.currentPatch.ToString())
		pass
	
	if event.is_action("BambooCopy"):
		pass
	if event.is_action("BambooPaste") and event.pressed:
		print ("Attempting paste...")
		if !$Audio.patch:
			$Audio.NewPatch()
			yield(get_tree(),"idle_frame")

		if $Audio.patch:
			var algorithm = $Audio.patch.BambooPaste()
			if algorithm < 0 or algorithm > 7:
				print ("BambooPaste failed.")
				return
			print ("Algorithm ", algorithm)
			
			$Algorithm.select(algorithm)
			_on_Algorithm_item_selected(algorithm)
			
			update_smol_envelope_previews()

func _on_btnValidate_pressed():
	$TC/EGControl.disable(true)
	$GraphEdit.validate()

#	global.custom_algo = $GraphEdit.get_connection_list()


func _on_GraphEdit_changed(dirty=false):
#	print("boop, it's ", dirty)
	if dirty:
		$btnValidate.disabled = false
	else:
		$btnValidate.disabled = true
		

#Load the envelope generator for the node.
func _on_GraphEdit_node_selected(node):
	if !$Audio.patch:  return
	if node.is_in_group("operator"):
		select_EG(node.name)

		yield(get_tree(),"idle_frame")
		lastOperatorEnvelope = node
		
func _on_Algorithm_item_selected(id):
	var algo = []
	if id == 8:
		algo = global.custom_algo
	else:
		algo = global.algorithms[id]

	if !typeof(algo)==TYPE_ARRAY or algo.empty():  return
	$GraphEdit.clear_connections()
	
	#Reconnect
	for connection in algo:
		#Each pair is a 2 field array indicating the connection.
		var pair = ["", ""]
		for i in 2:
			if connection[i] == 0:
				pair[i] = "Output"
			else:
				pair[i] = "OP%s" % connection[i]
			
		$GraphEdit.connect_node(pair[0],0, pair[1],0)

	$GraphEdit.dirty = true

func _on_Env_update(value, which):
	if !lastOperatorEnvelope:  return
	
	var env:EnvelopeDisplay = lastOperatorEnvelope.get_node("EnvelopeDisplay")
#	prints ("Setting", which,"to", value, "in", env)

	update_envelope_preview(env,which,value)
	update_envelope_preview($EnvelopeDisplay,which,value)


func update_envelope_preview(env:EnvelopeDisplay, propertyName:String, value):
#	print("Update ", propertyName, " of ", env.get_path())
	match propertyName:
		"Ar":
			env.Attack = value
		"Dr":
			env.Decay = value
		"Sr":
			env.Sustain = value
		"Rr":
			env.Release = value
		"Sl":
			env.sl = value 
			env.update_env()
			env.update_vol()
		"Tl":
			env.tl = value 
			env.update_env()
			env.update_vol()
		"Ac":
			env.ac = value
		"Dc":
			env.dc = value
		"Sc":
			env.sc = value
		"Rc":
			env.rc = value

func update_envelope_preview_all(env:EnvelopeDisplay, eg):
	if eg == null:
		print ("update_envelope_preview:  envelope is null")
		return
	env.Attack = eg.ar
	env.Decay = eg.dr
	env.Sustain = eg.sr
	env.Release = eg.rr
	env.sl = eg.sl
	env.tl = eg.tl 
	env.ac = eg.ac
	env.dc = eg.dc
	env.sc = eg.sc
	env.rc = eg.rc
	env.update_env()
	env.update_vol()

func update_smol_envelope_previews():
	for o in $GraphEdit.get_children():
		if o.is_in_group("operator"):
			update_envelope_preview_all(o.get_node("EnvelopeDisplay"),$Audio.patch.GetEG(o.name))
	

#Called when opening a file or pasting a patch.
func update_ui():
	for o in $GraphEdit.get_children():
		if o.is_in_group("operator"):
#			update_envelope_preview_all(o.get_node("EnvelopeDisplay"), $Audio.patch.GetEG(o.name))
			var eg = global.currentPatch.GetEG(o.name)
			update_envelope_preview_all(o.get_node("EnvelopeDisplay"), eg)
			
	
	$TC/EGControl.disable(true)

	$TC2/Patch.reload()

	yield(get_tree(), "idle_frame")
	$TC2/LFO.reload()
	$TC2/Pitch.load_settings()
	$TC2/Wave.reload()

#Used when loading a new EG to current.
func select_EG(which):
	if !$Audio.patch:  return

	var envelope = $Audio.patch.GetEG(which)
	if !envelope:  return
	global.currentEG = envelope
	$TC/EGControl.load_settings(envelope)
	$TC/Tuning.load_settings(envelope)
	$TC/Curve.load_settings(envelope)
	$TC/Waveform/Options.load_settings(envelope)
	$TC/Response.load_settings(envelope)

	$TC/EGControl.disable(false)
#		$TC.enable
	
	print("Selected: ", which)
	update_envelope_preview_all($EnvelopeDisplay, envelope)  #Update big preview



#================ Copy paste menu stuff ==========================
func _on_CPMenu_index_pressed(index):
	match index:
		0:  #Copy
			$CPMenu/Dialog.current_tab = $TC.current_tab
			$CPMenu/Dialog.popup(Rect2(get_global_mouse_position() - Vector2(64, 16), Vector2.ONE))

		2:  #Paste
			if global.currentEG:
				var err = global.currentEG.FromString(OS.clipboard, false)
				if err != 0:  
					print ("Paste returned error ", err, ".")
				else:
					select_EG("OP" + str(global.currentEG.opID+1))
					update_smol_envelope_previews()



			
func _on_TC_gui_input(event):
	if event is InputEventMouseButton and event.pressed and event.button_index == BUTTON_RIGHT:
		if Rect2(Vector2.ZERO, Vector2($TC.rect_size.x, 32)).has_point($TC.get_local_mouse_position()):
			$CPMenu.popup(Rect2(get_global_mouse_position(), Vector2.ONE))




