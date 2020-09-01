#Envelope copy menu dialog
extends ConfirmationDialog

var current_tab = -1  #Set by the owner to automatically check a box.

func _ready():
	var lastChk = $V/CheckBox7
	
	#Setup some custom focus shit
	lastChk.focus_neighbour_bottom = get_ok().get_path()

	get_ok().focus_neighbour_top = lastChk.get_path()
	get_cancel().focus_neighbour_top = lastChk.get_path()
	
	get_ok().focus_previous = lastChk.get_path()
	get_cancel().focus_previous = lastChk.get_path()

	#Need to wait for this next part for global singletons to be ready.
	yield (get_tree(), "idle_frame")
	for i in $V.get_child_count():
		var o = $V.get_child(i)
		if o is CheckBox:  
			o.focus_next = get_ok().get_path()
			o.set_meta("flagval", pow(2, i))  #Sets the metadata value for the copy flags
	

func _on_CopyDialog_about_to_show():
	#Deselect previous selections
	for o in $V.get_children():
		if o is CheckBox:  o.pressed = false
	
	#Select the current tab.
	if current_tab > 0:
		var btn = $V.get_child(current_tab)
		btn.pressed = true
	
	owner.modulate.a = 0.6  #Dim the BG.

#	move_child($V, get_child_count())
	yield(get_tree(), "idle_frame")
	$V.get_child(0).grab_focus()

func _on_CopyDialog_popup_hide():
	current_tab = -1
	owner.modulate.a = 1.0



func _input(event):
	if event is InputEventKey and event.pressed:
		var active #Active control
		
		match event.scancode:
			KEY_ESCAPE:
				hide()
			
			KEY_1, KEY_KP_1:
				active = $V/CheckBox
			KEY_2, KEY_KP_2:
				active = $V/CheckBox2
			KEY_3, KEY_KP_3:
				active = $V/CheckBox3
			KEY_4, KEY_KP_4:
				active = $V/CheckBox4
			KEY_5, KEY_KP_5:
				active = $V/CheckBox5
			KEY_6, KEY_KP_6:
				active = $V/CheckBox6
			KEY_7, KEY_KP_7:
				active = $V/CheckBox7
			
		#Toggle the active button.
		if active:  active.pressed = !active.pressed


func _on_Dialog_confirmed():
	if !global.currentEG:  return

	#Accumulate the flag value.
	var flags = global.EGCopyFlags.NONE
	for i in $V.get_child_count():
		var o = $V.get_child(i)
		if o is CheckBox and o.pressed:  
			flags += o.get_meta("flagval")
	
	if flags != global.EGCopyFlags.NONE:  global.currentEG.ClipboardCopy(flags)
