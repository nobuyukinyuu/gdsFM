#Waveform generators / oscillators live here.
extends Node

var sintable = []  #LUT for a full sine. TODO:  use symmetry to only need 1/4 sine
enum waveforms {SINE, SAW, TRI, PULSE, ABSINE, WHITE, PINK, BROWN}

var pinkr = PinkNoise.PinkNumber.new()  #Pink noise generator
var lastr = 0 #Brown noise last random number to walk by

const TRI_TABLE = [0, 0.2, 0.4, 0.6, 0.8, 1, 0.8, 0.6, 0.4, 0.2, 0, -0.2, -0.4,
					-0.6, -0.8, -1, -0.8, -0.6, -0.4, -0.2]


func _ready():
	gen_sin_table(4096)

#Generate the sine lookup table at the specified resolution.
func gen_sin_table(res):
	sintable.clear()
	sintable.resize(res)
	for i in res:
		sintable[i] = (  sin(TAU * i/float(res))  )
		

#Grab a sine value from the lookup table.
func sint(n):
	var sz = sintable.size()
	var idx = round( n / (TAU) * float(sz))
	idx = wrapi(idx, 0, sz)

	return sintable[idx]

#Grab a sine from the lookup table, from 0-1 instead of 0-TAU.
func sint2(n):
	var sz:int = sintable.size()
	var idx = round(n * sz)
	idx = wrapi(idx, 0, sz)
	
	return sintable[idx]


#Return a sample from the specified phase for the specified waveform.
func wave(n, waveform = waveforms.SINE, duty = 0.5):
	n = fmod(n, 1.0)
	match waveform:
		waveforms.PULSE:
			if n>= duty:  return 1
			return -1
		waveforms.SINE:
			return sint2(n)
		waveforms.ABSINE:
			return abs(sint2(n))
		waveforms.TRI:
			return TRI_TABLE[int(n*20)]
		waveforms.SAW:
			return 1.0 - (n * 2)
		waveforms.PINK:
			return pinkr.next()
		waveforms.BROWN:
			lastr += randf() * 0.2 - 0.1
			lastr *= 0.99
			return lastr
		waveforms.WHITE, _:
			return randf() * 2 - 1.0  #White noise
