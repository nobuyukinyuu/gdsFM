extends Node
#onready var note = owner.get_node("Audio/PreviewNote")

signal NoteOff

func _ready():
	OS.open_midi_inputs()
	
	yield (get_tree(), "idle_frame")
	yield (get_tree(), "idle_frame")
	


func _input(event):
		
	if event is InputEventMIDI:
		match event.message:
			MIDI_MESSAGE_NOTE_ON:

				$"../Audio".AddNote(event.pitch, event.velocity, self)
				print("Pitch: %s\nVelocity: %s\nPressure: %s\n" % [event.pitch, event.velocity, event.pressure])

			MIDI_MESSAGE_NOTE_OFF:
#				$"../Audio".TurnOffNote(event.pitch)
				emit_signal("NoteOff", event.pitch)
