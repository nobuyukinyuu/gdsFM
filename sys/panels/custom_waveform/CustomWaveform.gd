extends Control

const noteFont = preload("res://gfx/fonts/NoteFont.tres")
signal value_changed


func _ready():
	$H/Banks.get_popup().add_font_override("font", noteFont)
	$H/MenuButton.get_popup().theme = $CPMenu.theme
	$H/Banks.get_popup().theme = $CPMenu.theme

	var check = AtlasTexture.new()
	var uncheck = AtlasTexture.new()
	
	check.atlas = preload("res://gfx/ui/radio_check.png")
	uncheck.atlas = preload("res://gfx/ui/radio_check.png")
	uncheck.region = Rect2(0,0,16,16)
	check.region = Rect2(16,0,16,16)
	
	$H/Banks.get_popup().add_icon_override("radio_unchecked", uncheck)
	$H/Banks.get_popup().add_icon_override("radio_checked", check)

	connect("value_changed", self, "_on_value_changed")

	var pop = $H/MenuButton.get_popup()
	pop.connect("index_pressed", self, "_on_menu_item_selected")

func fetch_table(index=0):
	if !global.currentPatch:  return
	var input:Dictionary = global.currentPatch.GetWaveformBank(index,false) 
		
	if input:
		$VU.set_table(input.get("values"))
		return input
		
	else:
		print("Waveform: Can't find Patch's custom wavetable bank at %s." % index)

func _on_value_changed(idx, val):
	if !global.currentPatch:  return
	var bank = $H/Banks.selected
	global.currentPatch.SetWaveformValue(bank, idx, val)


func _on_Banks_item_selected(index):
	fetch_table(index)

func _on_menu_item_selected(index):  #Called when needing to add or remove banks
	match index:
		0: #Add
			add_bank()
			var sz = $H/Banks.get_item_count()
			$H/Banks.add_item(str(sz), sz)
		2: #Remove
			var sz = $H/Banks.get_item_count()
			if sz <= 1:  return
			var oldbank = $H/Banks.selected
			remove_bank(oldbank)
			$H/Banks.remove_item(oldbank)
			$H/Banks.select(min($H/Banks.get_item_count()-1, oldbank))
			
			#Rename old banks.
			for i in range($H/Banks.selected, $H/Banks.get_item_count()):
				$H/Banks.set_item_text(i, str(i))
			
			#Load new bank here
			fetch_table($H/Banks.selected)

func add_bank():
	global.currentPatch.AddWaveformBank()

func remove_bank(index):
	global.currentPatch.RemoveWaveformBank(index)



func _on_CPMenu_index_pressed(index):
	if index == 0:  #Copy
		pass
	elif index == 1:  #Paste
		pass 
