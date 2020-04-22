extends Control

#var buf:AudioStreamGeneratorPlayback  #Playback buffer
#var samples = 0
#var bufferdata = PoolVector2Array([])

var hz = 44100.0  #This is set to a global var sample_rate


func _ready():
#	hz = global.sample_rate
#
#	$Audio.stream.mix_rate = hz
#	buf = $Audio.get_stream_playback()
#
#	fill_buffer()  #Prefill buffer
#	$Audio.play()
	
	var newpts = []
	newpts.resize($Panel.rect_size.x)
	$Panel/Line2D.points = newpts

	$TC/EGControl.disable(true)
	$TC.set_tab_title(0, "EG")
	$TC.set_tab_icon(0, preload("res://gfx/ui/icon_adsr.svg"))
	$TC.set_tab_icon(1, preload("res://gfx/ui/icon_tuning.svg"))
	$TC.set_tab_icon(2, preload("res://gfx/ui/icon_curve.svg"))
	$TC.set_tab_icon(3, preload("res://gfx/ui/icon_waveform.svg"))

func _input(event):
	#DEBUG:  reset EG
	if event.is_action_pressed("play"):
		global.samples = 0
		$Audio.Reset()
	

	if Input.is_action_just_pressed("play"):
		OS.clipboard = var2str($GraphEdit.get_connection_list())
		print ("Copied.  " + OS.clipboard)
	

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
		var envelope = $Audio.patch.GetEG(node.name)
		if !envelope:  return
		$TC/EGControl.load_settings(envelope)
		$TC/Tuning.load_settings(envelope)
		$TC/Curve.load_settings(envelope)
		$TC/Waveform/Options.load_settings(envelope)

		$TC/Waveform/Grid/Waveform.select(envelope.waveform)
		$TC/Waveform/Grid/Waveform2.select(envelope.fmTechnique)

		$TC/EGControl.disable(false)
#		$TC.enable


func _on_Waveform_item_selected(id, techWaveform:bool=false):
	if !$TC/EGControl.currentEG:  return

	if !techWaveform:
		$TC/EGControl.currentEG.waveform = id
	else:
		$TC/EGControl.currentEG.fmTechnique = id


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
