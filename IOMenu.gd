extends Panel



func _on_OpenDialog_file_selected(path):
	
	#open file from filesystem, input string to patch paster
	pass # Replace with function body.


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
