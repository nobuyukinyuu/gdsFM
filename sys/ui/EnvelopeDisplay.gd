class_name EnvelopeDisplay
extends Panel

export (float, 0.5, 5, 0.1) var thickness = 1 setget update_thickness
export (float, -2, 5, 0.1) var ac=-2 setget update_ac
export (float, -2, 5, 0.1) var dc=-2 setget update_dc 
export (float, -2, 5, 0.1) var sc= 1 setget update_sc
export (float, -2, 5, 0.1) var rc= 2 setget update_rc

export(float,0,1) var tl=1.0  setget update_tl #Total level
export(float,0,1) var sl=1.0  setget update_sl #sustain level

export(int, 0, 31) var Attack=31 setget update_ar
export(int, 0, 31) var Decay=31 setget update_dr
export(int, 0, 31) var Sustain=31 setget update_sr
export(int, 0, 31) var Release=15 setget update_rr

var ar = 1.0  #Between epsilon and 1. lerp between 31-0
var dr = 1.0  #Ratio of DR to SR should add up to 2.  Map the range on env update.
var sr = 1.0
var rr = 1.0  #Between epsilon and 1.  Lerp between 15-0

func update_tl(value):
	if value >= 1:  
		value = log(value)/log(10) / 2
		tl = value
	else:
		tl = value / 100.0
		
	update_vol()
func update_sl(value):
	if value >= 1:  
		var lin = log(value)/log(10) / 2
		value = lerp(lin, value / 100.0, 0.5)
		sl = value
	else:
		sl = value / 100.0
	update_vol()

func update_ac(val):
	var n = $ADSR/"0"
	n.curve = val
func update_dc(val):
	if val!=0:  val = 1/val
	var n = $ADSR/"1"
	n.curve = val
func update_sc(val):
	if val!=0:  val = 1/val
	var n = $ADSR/"2"
	n.curve = val
func update_rc(val):
	if val!=0:  val = 1/val
	var n = $ADSR/"3"
	n.curve = val
	

func update_ar(val):
	Attack = val
	ar = lerp(1, 0, val/32.0)
	update_env()
func update_dr(val):
	Decay = val
#	update_dsr()

	dr = lerp(2, 0, val/32.0) 
	update_env()

func update_sr(val):
	Sustain = val
	sr = lerp(1, 0, val/32.0) 
	update_env()
func update_rr(val):
	Release = val
	rr = lerp(1, 0, val/15) 
	update_env()

#Calculate ratio between decay and sustain
func update_dsr():
	var d = lerp(1, 0, Decay/32.0)  +0.1
	var s = lerp(1, 0, Sustain/32.0) +1.0
	
	dr = range_lerp(d, 0, d+s, 0, 1)
	sr = range_lerp(s, 0, d+s, 0, 1)
	update_env()
	

func update_env():
	$ADSR/"0".tl = tl		  #Attack level end
	$ADSR/"1".tl = tl		  #Decay level start

	var sl2 = sl * tl
	$ADSR/"1".sl = 1.0-sl	  #Decay Level end
	$ADSR/"2".tl = sl2	 #Sustain level start
	
	var rl= sl
	
	$ADSR/"2".sl = 1.0-sr   #Sustain level final
	$ADSR/"3".tl = sr*sl*tl   #Release level start

	if Release < 1:
		$ADSR/"3".sl = 0.5-(rr/2.0)  #Release level end  (Lerp between 0 and rlev at rrate)
	else:
		$ADSR/"3".sl = 1  #Release level end  (Lerp between 0 and rlev at rrate)		
	
	$ADSR/"0".size_flags_stretch_ratio = ar
	$ADSR/"1".size_flags_stretch_ratio = dr if Decay>0 else 0
	$ADSR/"2".size_flags_stretch_ratio = sr
	$ADSR/"3".size_flags_stretch_ratio = rr
	
	update()

func update_vol():
	for o in $ADSR.get_children():
		o.update()

func update_thickness(val):
	thickness = val
	
	for o in $ADSR.get_children():
		o.thickness = thickness
		o.update()


func set_all(a,d,s,r, svol,vol, acurve,dcurve,scurve,rcurve):
	update_ar(a)
	update_dr(d)
	update_sr(s)
	update_rr(r)
	
	sl = svol / 100.0
	tl = vol / 100.0
	
	ac = acurve
	dc = dcurve
	sc = scurve
	rc = rcurve
	
	
	$ADSR/"0".curve = acurve
	$ADSR/"1".curve = dcurve
	$ADSR/"2".curve = scurve
	$ADSR/"3".curve = rcurve
		
	update_env()
	update_vol()

func _ready():
	update_vol()
