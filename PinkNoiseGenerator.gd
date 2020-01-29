extends Node

#Stolen from bfxr who stole it from http://www.firstpr.com.au/dsp/pink-noise/#Filtering
class PinkNumber:
	var max_key = 0x1f
	var key = 0
	var white_values = [0,0,0,0,0]
	var _range = 128
	
	func _init():
		for i in range(5):
			white_values[i] =  randf() * (_range/5)

	func next():
		var last_key = key
		var sum
		
		key +=1
		if key > max_key:
			key = 0
		# XOR previous value with current value.
		# This gives a list of bits that have changed.
		var diff = last_key ^ key
		sum = 0
		
		for i in range(5):
			#If bit changed get new random number for corresponding white_value
			if diff & (1 << i):
				white_values[i] = randf() * (_range/5)
			sum += white_values[i]
			
		return sum/64.0-1.0