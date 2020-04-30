extends GraphNode

func _ready():
	$Bypass.connect("pressed", self, "_on_Bypass_Pressed")
	$EnvelopeDisplay.Attack=31
	$EnvelopeDisplay.Release=31
	
func _on_Bypass_Pressed():
	var err = owner.get_node("Audio").Bypass(name, $Bypass.pressed)
	if err != OK:
		$Bypass.pressed = false
	
