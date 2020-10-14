extends Panel

func _ready():
	$OpenDialog.current_dir = ProjectSettings.globalize_path("res://") + "demo/bank0/"
	print (OS.get_executable_path().get_base_dir())

func _on_OpenDialog_file_selected(path):
	if !global.currentPatch:  
		print("PatchEdit:  Can't load, patch doesn't exist")
		return
	
	var f = File.new()
	var ferr = f.open(path, File.READ)
	
	$"../Audio".StopAll()  #Stop active notes before loading the string in to prevent null access.
	var err = global.currentPatch.FromString(f.get_as_text())
	f.close()

	if ferr != OK:
		print ("File error %s." % ferr)
	elif err != 0:  
		print ("Load attempt returned error code %s from patch." % err)
	else:
		#Update the UI to reflect the new patch settings.
		owner.get_node("GraphEdit").wire_up(global.currentPatch.wiring)
		

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
	if !global.currentPatch:  
		print("PatchEdit:  Can't paste, patch doesn't exist")
		return
	
	$"../Audio".StopAll()  #Stop active notes before loading the string in to prevent null access.
	var err = global.currentPatch.FromString(OS.clipboard, true)  #Second argument specifies to ignore IO version.

	if err != 0:  
		print ("Paste attempt returned error code %s from patch." % err)
	else:
		#Update the UI to reflect the new patch settings.
		owner.get_node("GraphEdit").wire_up(global.currentPatch.wiring)
		

		yield(get_tree(), "idle_frame")
		owner.update_ui()
