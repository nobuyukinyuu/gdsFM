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
var waveform = 0  #TODO:  Make enum.  Default 0 (Sine)
var duty = 0.5  #Duty cycle for pulses.  Might have other uses for config...

var feedback = 0  #TODO:  Figure out if this is OK to use on multiple operators


var connections = {}  #This is filled in by the algorithm validator.

var eg = global.Instr.new()   #Envelope generator

func _ready():
#	print(name, " inputs: ", get_connection_input_count(),\
#			 "	Outputs: ", get_connection_output_count())
	pass


func request_sample(phase, amp, is_carrier=false):
	var out = 0.0
	
	#TODO:  Don't sin-modulate phase at first, add phases together for each
	#		Parallel modulator, THEN call modulate on sample before passing back.
	
	if is_carrier:
		out = (phase * hz) * (1+ eg.dt/500.0) 

	for o in connections.keys():
		pass
