extends VBoxContainer


const note_names = ["A-", "A#", "B-", "C-", "C#", "D-", "D#", "E-", "F-", "F#", "G-", "G#"]
const noteColors = preload("res://gfx/noteColors/5th.png")
const noteFont = preload("res://gfx/ui/NoteFont.tres")

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
	
	

	


func _on_chkFixed_toggled(button_pressed):
	if owner.currentEG:
		owner.currentEG.FixedFreq = abs(int($txtHz.text)) if button_pressed else 0

func update_freq():
	if owner.currentEG:
		owner.currentEG.FixedFreq = abs(int($txtHz.text)) 

func _on_txtHz_text_entered(_new_text):
	if $chkFixed.pressed:  update_freq()

func _on_Presets_item_selected(id):
	$txtHz.text = str(global.periods[id])
	if $chkFixed.pressed:  update_freq()



func _on_Presets_pressed():
	#Scroll the popup to the active item.  Why is this not built into OptionButton?
	var popup = $Presets.get_popup()
	var amt = max($Presets.selected * 16, -popup.rect_size.y + owner.rect_size.y)
	popup.rect_position.y -= amt
	
