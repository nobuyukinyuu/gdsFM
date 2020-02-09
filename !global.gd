extends Node

var sample_rate = 48000.0
var samples = 0  #Global oscillator timer.  Iterated every time an output sample is produced.

# 12-field array containing a LUT of semitone frequencies at all MIDI note numbers.
#Generated from center tuning (A-4) at 440hz.

# https://www.inspiredacoustics.com/en/MIDI_note_numbers_and_center_frequencies
var periods = []  #Size 128 +1
const NOTE_A4 = 69   # Nice.


func _ready():
	randomize()

	# Generate the period frequencies of every note based on center tuning (A-4) at 440hz
	# Calculated from the equal temperment note ratio (12th root of 2).
	periods.clear()
	periods.resize(129)  #Extra field accounts for G#9
	for i in periods.size():
		periods[i] = 440.0 * pow(2, (i-NOTE_A4) / 12.0 )

#	print(periods)

	
#Sample Timer convenience functions.  Converts samples elapsed to readable time
func get_secs():
	return samples / sample_rate
	


class Instr:
	var rr = 1 setget set_release_time  #Float between 0-1
	var mul = 0 setget set_frequency_multiplier #Float between 0.5 and 15
	
	var ar=0	#Attack
	var dr=0	#Decay
	var sr=0	#Sustain
#	var rr=0	#Release
	var sl=0	#Sustain level
	var tl=50	#Total level  (attenuation)
	var ks=0	#Key scale
#	var mul=0	#Frequency multiplier
	var dt=0	#Detune
#
#	var hz = 440.0  #Operator frequency.  Tuned to A-4 by default
#
	var feedback = 0  #TODO:  Figure out if this is appropriate to use on multiple operators
	var duty = 0.5  #Duty cycle is only used for pulse right now
	var waveform = 0


	var release_time = 1 #Calculated from rr when set
	var freq_mult = 0.5
	var hz = 440.0

	var samples = 0  #Samples elapsed

	func set_release_time(val):
		rr = val 
		if val <= 0:
			release_time = 0x7FFFFFFFFFFFFFFF
		elif val >= 1.0:
			release_time = 0
		else:
			release_time = -log(rr) *2  # an rr of 0.01 will produce a ~10s release time.
	
	func set_frequency_multiplier(val):
		mul = val
		
		if val == 0:  val = 0.5
		freq_mult = val #* hz


class Patch:
	var opChain  #Entire list of GraphNode connections
#	var 
	
	
