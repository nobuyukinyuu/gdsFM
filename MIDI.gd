extends Node
onready var note = owner.get_node("Audio/PreviewNote")

func _ready():
	OS.open_midi_inputs()
	
	yield (get_tree(), "idle_frame")
	yield (get_tree(), "idle_frame")
	

	owner.get_node("Audio").previewNote = note
	pass # Replace with function body.


func _input(event):
		
	if event is InputEventMIDI:
		match event.message:
			MIDI_MESSAGE_NOTE_ON:
				print (note.Velocity)
				note.pressed = true
				note.samples = 0
				note.hz = note.lookup_frequency(int(event.pitch))
				note.midi_velocity = event.velocity
				
				print("Pitch: %s\nVelocity: %s\nPressure: %s\n" % [event.pitch, event.velocity, event.pressure])

			MIDI_MESSAGE_NOTE_OFF:
				note.pressed = false
