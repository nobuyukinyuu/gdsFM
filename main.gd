extends Control

var buf:AudioStreamGeneratorPlayback  #Playback buffer
var samples = 0
var bufferdata = PoolVector2Array([])

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


func _input(event):
	#DEBUG:  reset EG
	if event.is_action_pressed("play"):
		global.samples = 0

func _process(delta):
#	fill_buffer()
	pass

func _physics_process(delta):
#	if bufferdata.size() < 2:  return
#	for i in min($Panel.rect_size.x, bufferdata.size()):
#		var h = $Panel.rect_size.y/2
#		$Panel/Line2D.points[i] = Vector2(i, h + bufferdata[i].y * h) 
	
	
	
#	$Panel/Line2D.points = pts
#	$Label2.text = str($GraphEdit/OP4.eg.freq_mult)
	pass
	
func fill_buffer(var frames=-1):
	if !buf:  return
	var frames_to_fill = buf.get_frames_available()
	if frames >=0:  frames_to_fill = frames

	bufferdata.resize(frames_to_fill)

	for i in frames_to_fill:
		

		# The true phase is calculated by each oscillator's wave function.
		# It's wrapped to a value between 0-1, but to account for detune,
		# we don't wrap the phase here.
		var phase = global.get_secs() * 440
		

		if $GraphEdit.connections_valid:
			var s = $GraphEdit/Output.mix(phase)
			bufferdata[i] = s * Vector2.ONE

		global.samples +=1

	buf.push_buffer(bufferdata)


	
func _on_FB_value_changed(value):
	$Label.text = "Feedback:  " + str(value)
	if $EGControl.currentEG:
		$EGControl.currentEG.feedback = value


func _on_btnValidate_pressed():
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
	if node.is_in_group("operator"):
		$EGControl.load_settings(node.eg)
		$Waveform.select(node.eg.waveform)
		$FB.value = node.eg.feedback
	


func _on_Waveform_item_selected(id):
	if !$EGControl.currentEG:  return

	$EGControl.currentEG.waveform = id
