extends FileDialog


# Declare member variables here. Examples:
# var a = 2
# var b = "text"


# Called when the node enters the scene tree for the first time.
func _ready():
	yield (get_tree(), "idle_frame")
	popup_centered()
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
#func _process(delta):
#	pass


func _on_FileDialog_file_selected(path):
	owner.LoadMIDI(path)
	pass # Replace with function body.


func _input(event):
	if Input.is_action_just_pressed("Output"):
		popup_centered()
