extends Control

var buf:AudioStreamGeneratorPlayback  #Playback buffer
var samples = 0

var hz = 22050.0

var carrier_hz = 440.0
var modulator_hz = 220.0


func _ready():
	$Audio.stream.mix_rate = hz
	buf = $Audio.get_stream_playback()

	fill_buffer()  #Prefill buffer	
	$Audio.play()

func _process(delta):
	fill_buffer()
	
	
	
func fill_buffer(var frames=-1):
	if !buf:  return
	var frames_to_fill = buf.get_frames_available()
	if frames >=0:  frames_to_fill = frames
	var bufferdata = []

	while frames_to_fill > 0:
		samples +=1
		
#		var x = global.sint(2*PI * samples * (carrier_hz / hz))
		
		var carrier = (2*PI * samples * (carrier_hz / hz))
		var modulator = global.sint( 2*PI * samples * (modulator_hz / hz)) 
		

		var feedback = modulate(carrier , modulator_hz, $EGControl.get_value("MUL")) 
		for i in range($FB.value):
			feedback = modulate(feedback, carrier_hz)
		var x = global.sint(feedback) 
		
		bufferdata.append(Vector2.ONE * x)
		frames_to_fill -= 1

	buf.push_buffer(bufferdata)


#Carrier:  A waveform not yet sin-processed.
func modulate(carrier, modulator_hz, mul=1.0):
	var modulator = global.sint( 2*PI * samples * (modulator_hz / hz))
	return (carrier + mul*modulator)
	
	

func _on_FB_value_changed(value):
	$Label.text = "Feedback:  " + str(value)
