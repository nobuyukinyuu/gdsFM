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
	


	
# Feedback
func _on_FB_value_changed(value):
	$Label.text = "Feedback:  " + str(value)
	if $TC/EGControl.currentEG:
		$TC/EGControl.currentEG.feedback = value


func _on_btnValidate_pressed():
	$TC/EGControl.disable(true)
	$GraphEdit.validate()
	pass # Replace with function body.


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
