extends VBoxContainer


const note_names = ["A-", "A#", "B-", "C-", "C#", "D-", "D#", "E-", "F-", "F#", "G-", "G#"]
const noteColors = preload("res://gfx/noteColors/5th.png")
const noteFont = preload("res://gfx/fonts/NoteFont.tres")

const ratios = [ #0.71,
	0.78, 0.87, 1, 1.41, 1.57, 1.73, 2, 2.82, 3, 
	3.14, 3.46, 4, 4.24, 4.71, 5, 5.19, 5.65,
	6, 6.28, 6.92, 7, 7.07, 7.85, 8, 8.48, 
	8.65, 9, 9.42, 9.89, 10, 10.38, 10.99, 11,
	11.3, 12, 12.11, 12.56, 12.72, 13, 13.84, 14,
	14.1, 14.13, 15, 15.55, 15.57, 15.7, 16.96, 17.27,
	17.3, 18.37, 18.84, 19.03, 19.78, 20.41, 20.76, 
	21.2, 21.98, 22.49, 23.55, 24.22, 25.95, 34, 
	]

# Called when the node enters the scene tree for the first time.
func _ready():
	
	#Populate the preset dropdown with note names
	
	for i in range(128):
		var octave = "?" if i < 12 else floor((i-12)/12)
		var note = note_names[(i+3) % 12]
		
		var tex = AtlasTexture.new()
		tex.atlas = noteColors
		tex.region = Rect2(0,(i+3)%12*16,16,16)
		
		$Presets.add_item(note + str(octave), i)
		$Presets.set_item_icon(i, tex)
	
	$Presets.selected = global.NOTE_A4
	$Presets.get_popup().add_font_override("font", noteFont)

	var check = AtlasTexture.new()
	var uncheck = AtlasTexture.new()
	
	check.atlas = preload("res://gfx/ui/radio_check.png")
	uncheck.atlas = preload("res://gfx/ui/radio_check.png")
	uncheck.region = Rect2(0,0,16,16)
	check.region = Rect2(16,0,16,16)
	
	$Presets.get_popup().add_icon_override("radio_unchecked", uncheck)
	$Presets.get_popup().add_icon_override("radio_checked", check)
	
	
	#Populate the ratio presets.
	for i in ratios.size():
		var g = $Ratios/Pop/Grid
		var p = $Ratios/Pop/Grid/"0".duplicate()
		p.name = str(i+1)
		p.text = str(ratios[i])
		
		if int(ratios[i]) == float(ratios[i]):  #Integer
			p.icon = preload("res://gfx/ui/icon_integer_zahlen.svg")
			p.modulate = Color('#bbeeff')
		g.add_child(p)
		p.owner = owner
	
	
	#Really hacky way to get our spinbox to auto-select all text when clicking.
	$txtHz.get_child(0).connect("gui_input", self, "_on_txtHz_gui_input")
	

#Called by the owner to load in our settings manually.
func load_settings(eg):
	$txtHz.value = eg.FixedFreq
	$chkFixed.pressed = true if eg.FixedFreq>0 else false

	if eg.FixedFreq >=0:
		var C0 = global.periods[12]
		var idx = int(clamp( round(12*global.log2(eg.FixedFreq/C0)) + 12, 0, 127))
		$Presets.selected = idx



func _on_chkFixed_toggled(button_pressed):
	if owner.currentEG:
		owner.currentEG.FixedFreq = $txtHz.value if button_pressed else 0

func update_freq():
	if owner.currentEG:
		owner.currentEG.FixedFreq = $txtHz.value

func _on_txtHz_value_changed(value):
	if $chkFixed.pressed:  update_freq()

	var C0 = global.periods[12]
	var idx = int(clamp( round(12*global.log2(value/C0)) + 12, 0, 127))
	$Presets.selected = idx

func _on_Presets_item_selected(id):
	$txtHz.value = (global.periods[id])
	if $chkFixed.pressed:  update_freq()



func _on_Presets_pressed():
	#Scroll the popup to the active item.  Why is this not built into OptionButton?
	var popup = $Presets.get_popup()
	var amt = max($Presets.selected * 16, -popup.rect_size.y + owner.rect_size.y)
	popup.rect_position.y -= amt
	

func _on_txtHz_gui_input(event):
	if event is InputEventMouseButton and event.pressed and event.button_index == BUTTON_LEFT:
		$txtHz.get_child(0).text = ""  #Hacky lmao
	elif event is InputEventKey and event.scancode == KEY_ESCAPE:
		$txtHz.get_child(0).text = "%s %s" % [$txtHz.value, $txtHz.suffix]



func _on_Ratios_pressed():
	$Ratios/Pop.rect_position = get_global_mouse_position()
#	$Ratios/Pop.rect_position.x -= $Ratios/Pop.rect_size.x

	$Ratios/Pop.popup()
	pass # Replace with function body.
