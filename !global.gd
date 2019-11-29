extends Node

var sintable = []

func _ready():
	gen_sin_table(1024)


func gen_sin_table(res):
	sintable.clear()
	for i in res:
		sintable.append(  sin(2*PI * i/float(res))  )
		

#Grab a sine value from the lookup table.
func sint(n):
	var sz = sintable.size()
	var idx = round( n / (2*PI) * float(sz))
	idx = wrapi(idx, 0, sz)

	
	
	return sintable[idx]