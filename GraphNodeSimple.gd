extends GraphNode

func _ready():
	$Bypass.connect("pressed", self, "_on_Bypass_Pressed")
	
func _on_Bypass_Pressed():
	var err = owner.get_node("Audio").Bypass(name, $Bypass.pressed)
	assert(err==OK)
	
