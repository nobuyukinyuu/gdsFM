extends GraphNode

#var ar=0	#Attack
#var dr=0	#Decay
#var sr=0	#Sustain
#var rr=0	#Release
#var sl=0	#Sustain level
#var tl=0	#Total level  (attenuation)
#var ks=0	#Key scale
#var mul=0	#Frequency multiplier
#var dt=0	#Detune

var hz = 440.0  #Operator frequency.  Tuned to A-4 by default

var feedback = 0  #TODO:  Figure out if this is OK to use on multiple operators

var connections = {}  #This is filled in by the algorithm validator.

var eg = global.Instr.new()   #Envelope generator


func _ready():
#	print(name, " inputs: ", get_connection_input_count(),\
#			 "	Outputs: ", get_connection_output_count())
	pass

#Sample returns a Vec2 of (phase, amplitude). Phase is used to calculate modulation.
func request_sample(phase:float, amp:float, is_carrier=false) -> Vector2:
	var out = Vector2.ZERO
	
	#TODO:  Don't sin-modulate phase at first, add phases together for each
	#		Parallel modulator, THEN call modulate on sample before passing back.
	
	var modulator = Vector2.ZERO
	if connections.size() > 0:  
		
		#First, mix the parallel modulators.
		for o in connections.keys():
			modulator += $"..".get_node(o).request_sample(phase, amp)
	
		modulator /= connections.size()
		
		if $Bypass.pressed:  return modulator
		
		#Now modulate.
		phase += modulator.y
		phase = (gen.sint2(phase) + 1) / 2.0
		
		out.x = hz
		out.y = gen.wave(phase, eg.waveform)  * (1+ eg.dt/500.0)
		
		out.y *= eg.tl / 100.0
		
	else:  #Terminus.  No further modulation required.
		out.x = hz
		if $Bypass.pressed:  
			out.y = 0
		else:
			out.y = gen.wave(phase, eg.waveform) * (1+ eg.dt/500.0) 
			out.y *= eg.tl / 100.0

	return out

