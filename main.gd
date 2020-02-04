extends Control

var buf:AudioStreamGeneratorPlayback  #Playback buffer
var samples = 0
var bufferdata = PoolVector2Array([])

var hz = 44100.0  #This is set to a global var sample_rate

var carrier_hz = 440.0
var old_op1_sample = [0,0]
var modulator_hz = 440.0

var inst = global.Instr.new()

func _ready():
	hz = global.sample_rate
	
	$Audio.stream.mix_rate = hz
	buf = $Audio.get_stream_playback()

	fill_buffer()  #Prefill buffer
	$Audio.play()
	
#	$Panel/Line2D.position = $Panel.rect_position
	var newpts = []
	newpts.resize($Panel.rect_size.x)
	$Panel/Line2D.points = newpts


func _input(event):
	#DEBUG:  reset EG
	if event.is_action_pressed("ui_accept"):
		global.samples = 0

func _process(delta):
	fill_buffer()

func _physics_process(delta):
	if bufferdata.size() < 2:  return
	for i in min($Panel.rect_size.x, bufferdata.size()):
		var h = $Panel.rect_size.y/2
		$Panel/Line2D.points[i] = Vector2(i, h + bufferdata[i].y * h) 
	
#	$Panel/Line2D.points = pts
	
func fill_buffer(var frames=-1):
	if !buf:  return
	var frames_to_fill = buf.get_frames_available()
	if frames >=0:  frames_to_fill = frames

	bufferdata.resize(frames_to_fill)

	for i in frames_to_fill:
		
		#Calculate the phase position to reduce calculations needed by each operator
		var phase = (global.samples / hz * TAU) 
		
		
		var mul = $EGControl.get_value("MUL") +1
		var tl = (100 - $EGControl.get_value("TL")) / 100.0

		var carrier = (phase * carrier_hz) * (1+ $EGControl.get_value("DT")/500.0) 
		var modulator = (phase * modulator_hz )

		var release_env = (1.0-min(1.0, global.get_secs()*0.7))  #DEBUG, TEMPORARY

		#Process feedback
		if $FB.value > 0:
			var average = (old_op1_sample[0] + old_op1_sample[1]) / 2.0
			var scaled_fb = average * $FB.value #/ pow(2, $FB.value)
			old_op1_sample[1] = old_op1_sample[0]
			old_op1_sample[0] = sin(scaled_fb + carrier) * tl


			carrier = old_op1_sample[0]
		else:
			carrier = sin(carrier) * tl * release_env
			pass


		modulator = global.sint(sin(modulator*mul) + (carrier) * TAU*4) * release_env		
		var x = clamp(modulator, -1.5, 1.5)


		bufferdata[i] = (Vector2.ONE * x)

		global.samples +=1

	buf.push_buffer(bufferdata)

#Carrier:  A waveform not yet sin-processed.
func modulate(carrier, modulator, tl, mul=1.0):
#	var modulator = global.sint(TAU * global.samples * (mod_hz / hz))
	return global.sint(sin(modulator*mul) * tl + (carrier) * TAU*4)
	
	
func _on_FB_value_changed(value):
	$Label.text = "Feedback:  " + str(value)



func _on_btnValidate_pressed():
	$GraphEdit.validate()
	pass # Replace with function body.


func _on_GraphEdit_changed(dirty=false):
#	print("boop, it's ", dirty)
	if dirty:
		$btnValidate.disabled = false
	else:
		$btnValidate.disabled = true
