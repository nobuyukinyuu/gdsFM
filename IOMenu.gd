extends Panel

func _ready():
	$OpenDialog.current_dir = ProjectSettings.globalize_path("res://") + "patches/"
	print (OS.get_executable_path().get_base_dir())

func _on_OpenDialog_file_selected(path):
	if !global.currentPatch:  
		print("PatchEdit:  Can't load, patch doesn't exist")
		return
	
	var f = File.new()
	var ferr = f.open(path, File.READ)
	var err = global.currentPatch.FromString(f.get_as_text())
	f.close()

	if ferr != OK:
		print ("File error %s." % ferr)
	elif err != 0:  
		print ("Load attempt returned error code %s from patch." % err)
	else:
		#Update the UI to reflect the new patch settings.
		owner.get_node("GraphEdit").wire_up(global.currentPatch.wiring)
		
#		for o in $"../GraphEdit".get_children():
#			if o.is_in_group("operator"):
#				#Check if operator exists before trying to update the preview.
#				var eg = global.currentPatch.GetEG(o.name)
#				if !eg:  continue
#				owner.update_envelope_preview_all(o.get_node("EnvelopeDisplay"), eg)
		yield(get_tree(), "idle_frame")
		owner.update_ui()


func _on_SaveDialog_file_selected(path):
	if !global.currentPatch:  
		print("PatchEdit:  Can't save, patch doesn't exist")
		return
	
	var f = File.new()
	f.open(path, File.WRITE)
	f.store_string(global.currentPatch.IOString())
	f.close()
	


func _on_Dialog_about_to_show():
	$OpenDialog.invalidate()
	$SaveDialog.invalidate()
	pass # Replace with function body.


func _on_Open_pressed():
	$OpenDialog.popup_centered()


func _on_Save_pressed():
	$SaveDialog.popup_centered()


func _on_Copy_pressed():
	if !global.currentPatch:  return
	
	OS.clipboard = global.currentPatch.ToString()


func _on_Paste_pressed():
	owner.update_ui()
	pass # Replace with function body.
