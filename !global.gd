extends Node

const MAX = 0x7FFFFFFFFFFFFFFF
const EPSILON = 0.00001

var sample_rate = 44100.0
var samples = 0  #Global oscillator timer.  Iterated every time an output sample is produced.

var currentEG  #typeof Envelope
var currentPatch  #typeof Patch

# IO flags to specify to gdsFM when copying part of an operator or the entire thing.
# This enum is duplicated in glue.cs as EGCopyFlags
enum EGCopyFlags{NONE=0, EG=1, TUNING=4, CURVE=2, WAVEFORM=8, RTABLES=16, LOWPASS=32, LFO=64, ALL=127}
enum OPCopyFlags{NONE=0, NAME=1,ID=2,EG=4,BYPASS=8, LFO=16, WAVEFORM_BANK=32, WAVEFORM=64, ALL=127, HEADERS=11}
enum PatchCopyFlags{NONE=0, GENERAL=1,  OPS=2, PG=4, LFO=8, WAVEFORMS=16, ALL=255}

# RBJ filter types
enum FilterType {NONE, LOWPASS, HIPASS, BANDPASS_CSG, BANDPASS_CZPG, NOTCH, ALLPASS, PEAKING, LOWSHELF, HISHELF}
const FilterNames = ["None", "Low Pass", "High Pass", "Bandpass (Skirt Gain)", "Bandpass (0dB Peak)", 
						"Notch", "All-pass", "Peaking" , "Low Shelf", "High Shelf"]

# 12-field array containing a LUT of semitone frequencies at all MIDI note numbers.
#Generated from center tuning (A-4) at 440hz.
# https://www.inspiredacoustics.com/en/MIDI_note_numbers_and_center_frequencies
var periods = []  #Size 128 +1
var semitone_ratio = []  #Size 12, used to get a multiplier within an octave
const NOTE_A4 = 69   # Nice.
const USE_DUTY = 0x100  #Use duty flag.  Pass with waveform to the oscillator.

# The 8 magical algorithms shared by all 4-op FM synthesizers.
const algorithms = [
	[ [1,2], [2,3], [3,4], [4,0] ], #Four serial connection
	[ [1,3], [2,3], [3,4], [4,0] ], #Three double mod serial connection
	[ [1,4], [2,3], [3,4], [4,0] ], #Double mod 1
	[ [1,2], [2,4], [3,4], [4,0] ], #Double mod 2
	[ [1,2], [2,0], [3,4], [4,0] ], #Two serial, two parallel
	[ [1,2], [1,3], [1,4], [2,0], [3,0], [4,0] ], #3x parallel, common modulator
	[ [1,2], [2,0], [3,0], [4,0] ], #Two serial, two sines
	[ [1,0], [2,0], [3,0], [4,0] ], #Four parallel sines
]

var custom_algo="" setget save_custom_algo

func save_custom_algo(connections):
	#Save the custom algorithm for later use in our standard algo format.
	assert (typeof(connections)==TYPE_ARRAY)
	var output = []
	
	for connection in connections:
		assert (typeof(connection)==TYPE_DICTIONARY)
		var pair = []
		for cName in [connection["from"], connection["to"]]:
			if cName.begins_with("OP"):  #Operator.
				pair.append(int( cName.right(2) ))
			elif cName.begins_with("Output"):
				pair.append(0)
		output.append(pair)
		
	custom_algo = output

func _ready():
	randomize()

	# Generate the period frequencies of every note based on center tuning (A-4) at 440hz
	# Calculated from the equal temperment note ratio (12th root of 2).
	periods.clear()
	periods.resize(129)  #Extra field accounts for G#9
	for i in periods.size():
		periods[i] = 440.0 * pow(2, (i-NOTE_A4) / 12.0 )
		
		
	semitone_ratio.clear()
	semitone_ratio.resize(13)
	for i in semitone_ratio.size():
		semitone_ratio[i] = pow(2, i/12)

#	print(periods)

	
#Sample Timer convenience functions.  Converts samples elapsed to readable time
func get_secs():
	return samples / sample_rate
	

func log2(n):
	return log(n) / log(2)

func arr_replace(arr:Array, a, b):
	var idx = arr.find(a)
	if idx >= 0:
		arr[idx] = b
		return true
	return false

class Instr:
	var rr = 1 setget set_release_time  #Float between 0-1
	var mul = 0 setget set_frequency_multiplier #Float between 0.5 and 15
	
	var ar=0						#Attack
	var dr=0						#Decay
	var sr=0						#Sustain
#	var rr=0						#Release
	var sl=0						#Sustain level
	var tl=50						#Total level  (attenuation)
	var ks=0						#Key scale
#	var mul=0						#Frequency multiplier
	var dt=0 setget set_detune		#Detune
#
#	var hz = 440.0  #Operator frequency.  Tuned to A-4 by default
#
	var feedback = 0  #TODO:  Figure out if this is appropriate to use on multiple operators
	var duty = 0.5  #Duty cycle is only used for pulse right now
	var waveform = 0


	# True, internal values referenced by the operator to reduce re-calculating
	# these values when the above EG slider value changes.
	var release_time = 1 #Calculated from rr when set
	var freq_mult = 0.5
	var detune = 1
	
	
#	var hz = 440.0
#	var samples = 0  #Samples elapsed

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

	func set_detune(val):
		dt = val
		detune = 1.0 + (val / 10000.0)

