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

func _input(event):
	#DEBUG:  reset EG
	if event.is_action_pressed("play"):
		global.samples = 0
		$Audio.Reset()
	

	if Input.is_action_just_pressed("play"):
		OS.clipboard = var2str($GraphEdit.get_connection_list())
		print ("Copied.  " + OS.clipboard)
	
# Feedback
func _on_FB_value_changed(value):
	$Label.text = "Feedback:  " + str(value)
	if $TC/EGControl.currentEG:
		$TC/EGControl.currentEG.feedback = value


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
		$Waveform.select(envelope.waveform)
		$FB.value = envelope.feedback
	
		$TC/EGControl.disable(false)


func _on_Waveform_item_selected(id):
	if !$TC/EGControl.currentEG:  return

	$TC/EGControl.currentEG.waveform = id


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
