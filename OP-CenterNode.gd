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

var feedback = 0  #TODO:  Figure out if this is appropriate to use on multiple operators

var eg = global.Instr.new()   #Envelope generator

func _ready():
#	print(name, " inputs: ", get_connection_input_count(),\
#			 "	Outputs: ", get_connection_output_count())
	pass


func request_sample():
	pass
