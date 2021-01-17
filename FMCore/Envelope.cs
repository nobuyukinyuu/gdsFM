using Godot;
using System;

// using Newtonsoft.Json;
using GdsFMJson;

/// Envelope Generator.  Includes pitch, curve, ADSR, feedback, waveform, etc.
public class Envelope : Node 
{
    public const string _iotype = "envelope";
    public const int VERSION = 2;  // Used to determine which io version of the envelope this is, for forwards compatibility.

//This measurement was done against DX7 detune at A-4, where every 22 cycles the tone would change (-detune) samples at a recording rate of 44100hz.
//See const definitions in glue.cs for more information about the extra-fine detune increment.
    const Decimal DETUNE_440 = 2205M / 22M;
    const Decimal DETUNE_MIN = (2198M / 22M) / DETUNE_440 ;  //Smallest detune multiplier, a fraction of 1.0
    const Decimal DETUNE_MAX = (2212M / 22M) / DETUNE_440;   //Largest detune multiplier, a multiple of 1.0

    void init()
    {
        // ksr.floor = 100.0;  //Make sure the default KS curve isn't applied to the envelope until the user overrides it. Rate will always be 100% of original.
        ksr.RecalcValues();
        recalc_adsr();
    }

    public Envelope()  //Default ctor.
    {
        init();
    }
    public override void _Ready()
    {
        init();
    }

    public Envelope(string name){ ownerName = name; init(); }
    public Envelope(int id){ this.opID=id; init();}
    ~Envelope() { QueueFree(); }


    [System.Runtime.Serialization.IgnoreDataMember]
    public string ownerName;  // Debug purposes
    [System.Runtime.Serialization.IgnoreDataMember]
    public int opID;  // Operator index typically associated with this envelope. Used to reset delay phases.

    //STANDARD FM EG STUFF
    float ar=31;						//Attack
    float dr=31;						//Decay
    float sr=6;            			//Sustain
    float rr = 15;                      //Release
    float sl=100.0f;                      //Sustain level
    float tl = 100;                     //Total level  (attenuation)
    float ks=0;						//Key scale
    float mul=1.0f;					//Frequency multiplier  //float between 0.5 and 15
    float dt=0;                        //_detune
    float dt2=0;                        // Coarse detune (semitone, -12 to 12)

    float delay=0;                     //Envelope delay
    float fixed_frequency=0;           //Use fixed frequency if >0


    //CUSTOM STUFF
	public Waveforms waveform = Waveforms.SINE;
	public float feedback = 0;  //Operator feedback only used in op chain termini.
    public RbjFilter.FilterData filter = new RbjFilter.FilterData(FilterType.NONE);  //Lowpass cutoff + resonance filter.
    public bool reflect = false;  //Waveform reflection.  Inverts sine phase, makes sawtooth point to the right, etc.

	public float duty = 0.5f;  //Duty cycle is only used for pulse right now
    public Waveforms _use_duty = 0;  //Set to Waveforms.USE_DUTY (0x100) to use duty cycle variants of functions
    //Property accessor for GDScript that flips the internal duty flag to the proper native bitmask.
    public bool UseDuty { get => _use_duty==Waveforms.USE_DUTY; set => _use_duty=value? Waveforms.USE_DUTY : Waveforms.SINE;}


    public Waveforms fmTechnique = Waveforms.SINE;  //FM Technique used to modulate phase when requesting a sample up the chain.
    public float techDuty = 0.5f;  //Duty cycle for the fmTechnique.  Currently UNUSED (no use_tech_duty bit set) to save CPU. 
    public bool techReflect = false;  //Waveform reflection for the fmTechnique


    //ADSR easing curve values.
    public float ac = DefaultCurves.A;  //Attack curve.  In-out.
    public float dc = DefaultCurves.D;  //Decay curve.  75% Linear, 25% Ease-out.
    public float sc = DefaultCurves.S;  //Sustain curve.  50% Linear, 50% Ease-out.
    public float rc = DefaultCurves.R;  //Release curve.  75% Ease-out.

    public float Ac {get => ac; set=> RecacheCurve(ref AttackCurve, ref ac, value);}
    public float Dc {get => dc; set=> RecacheCurve(ref DecayCurve, ref dc, value);}
    public float Sc {get => sc; set=> RecacheCurve(ref SustainCurve, ref sc, value);}
    public float Rc {get => rc; set=> RecacheCurve(ref ReleaseCurve, ref rc, value);}

    //Cached curves.  256kb each
    public CurveCache AttackCurve = DefaultCurves.Attack;
    public CurveCache DecayCurve = DefaultCurves.Decay;
    public CurveCache SustainCurve = DefaultCurves.Sustain;
    public CurveCache ReleaseCurve = DefaultCurves.Release;


    #region Response Tables.
    internal RTable<float> ksr = RTable.FromPreset<float>(RTable.Presets.TWELFTH_ROOT_OF_2 | RTable.Presets.DESCENDING, 
                                                           floor:100, allow_zero:false );      //KeyScale rate. Lower values shrink envelope timings.
    private RTable<float> ksl = RTable.FromPreset<float>(RTable.Presets.TWELFTH_ROOT_OF_2 | RTable.Presets.DESCENDING,
                                                            floor:100);  //KeyScale level. Multiplies from 0-100% against TL of this envelope.
    private RTable<float> vr = RTable.FromPreset<float>(RTable.Presets.IN_OUT 
                                                         |0, floor:100);  //Velocity response. Sensitivity goes from 0% to 100% (0-1).  Default 0


    public Godot.Collections.Dictionary Ksr { get => ksr.ToDict(); set => ksr.SetValues(value); }
    public Godot.Collections.Dictionary Ksl { get => ksl.ToDict(); set => ksl.SetValues(value); }
    public Godot.Collections.Dictionary Vr { get => vr.ToDict(); set => vr.SetValues(value); }

    /// Used by the Godot frontend to copy a response table to the clipboard.
    public void CopyTable (string which)
    {
        which = which.ToLower();
        switch(which)
        {
            case "ksr":
                OS.Clipboard = ksr.ToString();
                break;
            case "ksl":
                OS.Clipboard = ksl.ToString();
                break;
            case "vr":
                OS.Clipboard = vr.ToString();
                break;
            default:
                GD.Print("Envelope.cs:  Unknown response table '", which, "'...");
                break;
        }    
    }
    /// Used by the Godot frontend to paste into a response table from the clipboard.
    public void PasteTable(string which)
    {
        which = which.ToLower();
        switch(which)
        {
            case "ksr":
                ksr.FromString(OS.Clipboard, false);
                break;
            case "ksl":
                ksl.FromString(OS.Clipboard, false);
                break;
            case "vr":
                vr.FromString(OS.Clipboard, false);
                break;
            default:
                GD.Print("Envelope.cs:  Unknown response table '", which, "'...");
                break;
        }    
    }
    #endregion


    //Property values used to translate "user-friendly" values to internal values, which are different than standard FM values.
    public float Ar { get => ar; set => _set_attack_time(value); }
    public float Dr { get => dr; set => _set_decay_time(value); }
    public float Sr { get => sr; set => _set_sustain_time(value); }
    public float Rr { get => rr;   set =>  _set_release_time(value); }
    public float Sl { get => sl; set  {sl = value;  _susLevel = value / 100.0f;} }
    public float Tl { get => tl; set  {tl = value;  _totalLevel = value / 100.0f;} }
    public float Ks { get => ks; set => ks = value; }
    public float Mul { get => mul;  set => _set_octave_multiplier(value); }
    public float Dt { get => dt; set => set_detune(value); }
    public float Dt2 { get => dt2; set => set_detune2(value); }
    public float FixedFreq { get => fixed_frequency; set {fixed_frequency = value;  update_multiplier();} }

    public float Delay { get => delay; set {_delay = value*SampleRate / 1000; delay=value;} }

    //Filter related properties.  Some of these are for backwards-compatibility only.
    public bool FilterEnabled { get => filter.Enabled; set => filter.Enabled = value; }  //Backwards compatibility
    public float CutOff { get => filter.cutoff; set {filter.cutoff = value; filter.Recalc();} } //Backwards compatibility
    public float Resonance { get => filter.resonanceAmp; set {filter.resonanceAmp = value; filter.Recalc();} } //Backwards compatibility
    public float Hz { get => filter.cutoff; set {filter.cutoff = value; filter.Recalc();} }  //Filter effective frequency
    public float Q { get => filter.resonanceAmp; set {filter.resonanceAmp = value; filter.Recalc();} }
    public float Gain { get => filter.gain; set {filter.gain = value; filter.Recalc();} }
    public FilterType FilterType {get=> filter.filterType; set {filter.Reset(); filter.filterType=value;  filter.Recalc();} }  


    // True, internal values referenced by the operator to reduce re-calculating
    // these values when the above EG slider value changes.
    public float _attackTime = 50;//float.Epsilon; //Needs to be large enough to avoid a clicking pop under certain circumstances when the phase isn't 0
    public float _decayTime = 50;//float.Epsilon;
    public float _susTime = 2880000; //float.MaxValue/5;  //about a minute, depending on sample rate
    public float _releaseTime = 100; //Calculated from rr when set
    public float _susLevel = 1.0f;
    public float _totalLevel = 1.0f;
	public float _freqMult = 1.0f;  //Octave multiplier (MUL)
	public float _coarseDetune = 1;
	public float _detune = 1;
    public float _delay;
	
    public float totalMultiplier = 1;  //Precalculated value of mul+dt+dt2 or the fixed frequency. See update_multiplier().

    //Internal ADSR timer calculating values.
    public float _egLength;  //total envelope length, in samples.  TODO:  Make ASDR true values int?
    float _ad;  //Attack time + decay time
    public float _ads; //Attack, decay, and sustain time.
	float _adr; //Attack, decay, and release time.  Used to check whether the sample offset from release is beyond the need to calculate an easing curve.


    /// Performs a full recalculation of all preset values.  Useful for loading/pasting from external sources.
    public void RecalcAll()
    {
        Ar = ar;
        Dr = dr;
        Sr = sr;
        Rr = rr;
        Sl = sl;
        Tl = tl;
        Ks = ks;
        Mul= mul;
        Dt = dt;
        Dt2= dt2;
        FixedFreq = fixed_frequency;
        Delay = delay;

        Ac = ac;
        Dc = dc;
        Sc = sc;
        Rc = rc;

        ksr.RecalcValues();
        ksl.RecalcValues();
        vr.RecalcValues();

        filter.Recalc();
        update_multiplier();
        recalc_adsr();
    }

    //ASDR Getter
    public float VolumeAtSamplePosition(Note note)
    {

        int s = note.samples;
        bool noteOff=!note.pressed;
        int releaseSample=note.releaseSample;
        float output=0;

        //Determine key scaling rate for this note.
        float _ksr= ksr[note.midi_note];

        //Generate keyScaled envelope times
        var atkTime = _attackTime * ksr[note.midi_note];
        var decTime = _decayTime * ksr[note.midi_note];
        var susTime = _susTime * ksr[note.midi_note];
        var ad = atkTime + decTime + _delay;

        if (s>= ad+susTime) return 0;  //Gone past the end of the envelope.

        //Determine the envelope phase.
        if (s < _delay)  {return 0;} //Delay phase.
        else if (s >= _delay && note.delayed[opID]==false) {note.ResetPhase(opID);  note.delayed[opID]=true;}  //Delay has elapsed.


        if (s-_delay < atkTime) { // Attack phase.
            // //Interpolate between 0 and the total level.
            // output= GDSFmFuncs.EaseFast(0.0, 1.0, (s-_delay) / atkTime, ac);

            output = AttackCurve[ (s-_delay) / atkTime ];

            // if(float.IsNaN(output)) {System.Diagnostics.Debugger.Break();}
        } else if ((s-_delay >= atkTime) && (s < ad) ) {  //Decay phase.
            // //Interpolate between the total level and sustain level.
            // output= GDSFmFuncs.EaseFast(1.0, _susLevel, (s-(atkTime+_delay)) / decTime, dc);

            // GD.Print("s=", s, "  delay=", _delay, "  atkTime=", atkTime);
            output = DecayCurve.MappedTo(1.0f, _susLevel, (s-(atkTime+_delay)) / decTime);

            // if(float.IsNaN(output)) {System.Diagnostics.Debugger.Break();}
        // } else if ((s-delay >= ad) ) {  //Sustain phase.
        } else {  //Sustain phase.
            // //Interpolate between sustain level and 0.
            // output= GDSFmFuncs.EaseFast(_susLevel, 0, (s-_delay-ad) / susTime, sc);

            output = SustainCurve.MappedTo(_susLevel, 0, (s-ad) / susTime);

            // if(float.IsNaN(output)) {System.Diagnostics.Debugger.Break();}
        }

        // #if DEBUG
        // if(float.IsNaN(output)) System.Diagnostics.Debugger.Break();
        // #endif

        var rlsTime = _releaseTime * ksr[note.midi_note];
        if (noteOff && (s-releaseSample) < rlsTime)  //Apply release envelope.
        {
            // output *= GDSFmFuncs.EaseFast(1.0, 0.0, (s-releaseSample) / rlsTime, rc);

            output *= 1.0f - ReleaseCurve[ (s-releaseSample) / rlsTime ];  //TODO:  Consider pre-caching a modified level

            // if(float.IsNaN(output)) {System.Diagnostics.Debugger.Break();}
        } else if (noteOff && (s-releaseSample) >= rlsTime) {
            return 0;
        }

        // #if DEBUG
        // if(float.IsNaN(output)) 
        // {
        //     System.Diagnostics.Debugger.Break();  //Stop. NaN propagation. This should never trigger but if it does prepare for pain
        //     return 0;
        //     // throw new ArithmeticException("NaN encountered in calculated envelope output");
        // }
        // #endif

        return output * ksl[note.midi_note] * vr[note.midi_velocity];
    }

    #region ASDR setters
    /// Called when ADSR properties change
    void recalc_adsr()
    {
        _egLength = _attackTime + _decayTime + _susTime + _releaseTime + _delay;

        _ad  = _attackTime + _decayTime + _delay;
        _ads = _ad + _susTime + _delay;
        _adr = _ad + _releaseTime + _delay;

        //TODO:  Recalculate lookup tables at the specified curve exponential.
        //      Probably should be done using properties for each curve value of the ADSR component.
        //      If it's "exactly" linear or quadratic we can probably just lerp...
        //      https://www.measurethat.net/Benchmarks/Show/1914/6/mathpow2n-vs-table-lookup
        //      Table lookup appears to be ~1.4x faster
        // http://www.hxa7241.org/articles/content/fast-pow-adjustable_hxa7241_2007.html

    }

    /// Creates a new curve cache for the specified curva value.
    void RecacheCurve(ref CurveCache which, ref float easingField, float val)
    {
        //Check if the curve we want to recache actually exists in the default curve caches.  This could potentially save a lot of memory.
        foreach(CurveCache curve in DefaultCurves.ADSR)
        {
            if (val==curve.EaseValue) //Curve matches one of the default cached curves.  Use a reference to this instead.
            {
                which = curve;
                easingField = curve.EaseValue;
                return;
            }
        }

        //No matching cache found.  Create a new one.
        var cache = new CurveCache(val);
        which = cache;
        easingField = val;
        // GD.Print("Recaching curve to ", val);  //remove me
    }

    void _set_attack_time(float val) {
        ar = val;
        if (val <=0) {
            _attackTime = float.MaxValue/5.0f;  //Stupid workaround to avoid float.PositiveInfinity.  Who's gonna hold a note for 2.264668e+299 hours?
        } else {
            _attackTime = get_curve_secs(BaseSecs.AR, val, 0.5f, 0) * SampleRate;
        }
        recalc_adsr();
    }

    void _set_decay_time(float val) {
        dr = val;
        if (val <=0) {
            _decayTime = 0;
        } else {
            _decayTime = get_curve_secs(BaseSecs.DR, val, 0.5f) * SampleRate;
        }
        recalc_adsr();
    }

    void _set_sustain_time(float val) {
        sr = val;
        if (val <=0) {
            _susTime = float.MaxValue/5.0f;  //Stupid workaround to avoid float.PositiveInfinity.  Who's gonna hold a note for 2.264668e+299 hours?
        } else {
            _susTime = get_curve_secs(BaseSecs.SR, val, 0.5f) * SampleRate;
        }
        recalc_adsr();
    }

	void _set_release_time(float val){
        // rr = val;

		// if (val <= 0) {
		// 	_releaseTime = float.MaxValue;
        // }
		// else if (val >= 1.0){ _releaseTime = 0; }
		// else {	_releaseTime = -Math.Log(Rr) *2;  }  // an rr of 0.01 will produce a ~10s release time.
	
        rr = val;
        if (val <= 0) {
            _releaseTime = SampleRate * 1800;  //Workaround to avoid float.PositiveInfinity and stuck notes.  30 minutes release
        } else {
            _releaseTime = get_curve_secs(BaseSecs.RR, val) * SampleRate;
        }
        recalc_adsr();
    }
#endregion


    /// Determines the length of part of an ADSR curve for sample calculation.  
    /// Multiply the output of this method to the sample rate to get that number of samples.

    // Consider baking a table of the appropriate length for each easing curve in the ADSR components at 1.0 TL.
    // For RR, multiply the value output by this table by the level at sustain on KeyOff.  Process until hitting sustain.
    // Precalculate absolute total length (in samples) of a blip. During KeyOff event, in sustain state whenever KeyOff is detected,
    // Immediately move the play head to the total length minus RR length.
    // http://forums.submarine.org.uk/phpBB/viewtopic.php?f=9&t=16
    float get_curve_secs(float base_secs, float val, float scaleFactor=1.0f, float minlength=0.005f)
    {
        //estimated base rates for OPNA at value 1:
        //AR    30sec?   (32 values)
        //DR    120sec  (32 values)
        //SR    120sec  (32 values)
        //RR    54sec   (16 values)
        return base_secs / (float) Math.Pow(2, (val-1) * scaleFactor);
    }


#region =========  PITCH AND TUNING RELATED SETTERS  ===============
 
    /// Whenever a tuning field changes, this should be called to update the precalculated total multiplier amount. 
    void update_multiplier()
    {
        if (fixed_frequency > 0)
        {
            //Note:  This multiplier needs to be divided down by the Note hz rate if Note freq does not equal 1. See Operator.cs
            totalMultiplier = fixed_frequency;  
            
        } else {
            totalMultiplier = _freqMult * _coarseDetune * _detune;
        }
    }

 
    /// Octave multiplier
	void _set_octave_multiplier(float val){
		mul = val;
		
		if (val == 0)  val = 0.5f;
		_freqMult = val; 

        update_multiplier();
    }

    /// Extra fine detune
	void set_detune(float val){  
		dt = val;
		// _detune = 1.0f + (val / 10000.0f);  //approx max multiplier is 1.0 ± 0.01
        switch (Math.Sign(val))
        {
            case 0:
                _detune = 1.0f;
                break;

            case 1:
        		_detune = (float) GDSFmFuncs.lerp(1.0M, DETUNE_MAX, (Decimal) (val / 100.0)) ;
                break;

            case -1:
        		_detune = (float) GDSFmFuncs.lerp(1.0M, DETUNE_MIN, (Decimal) (-val / 100.0)) ;
                break;
        }

        update_multiplier();
    }

    /// Coarse detune (Semitone)
    void set_detune2(float val)
    {
        dt2 = val;
        switch (Math.Sign(val))
        {
            case 0:
                _coarseDetune = 1.0f;
                break;
            
            case 1:
                _coarseDetune = 1 * (float) Math.Pow(2, Math.Abs(val) / 12.0);
                break;
            
            case -1:
                _coarseDetune = 1 / (float) Math.Pow(2, Math.Abs(val) / 12.0);
                break;
        }

        update_multiplier();
    }
#endregion //Pitch and tuning


#region ================ IO ==================
    /// Produces a GdsFMIO.JSONObject which can be used for serialization.
    public JSONObject JsonMetadata(EGCopyFlags flags, bool add_iotype=false)
    {
        //NOTE:  LFO sensitivity is not added to the json object here.  That's done in Operator with the other Operator-level meta.

        //Begin constructing a new json object.
        JSONObject p = new JSONObject();

        if (add_iotype)  p.AddPrim("_iotype", _iotype);  //IOType is needed for clipboard safety.
        else p.AddPrim("_version", VERSION);  //Version number is probably only useful for long-term IO and not clipboard operations.

        if (flags.HasFlag(EGCopyFlags.EG)){
             p.AddPrim("ar", ar);
             p.AddPrim("dr", dr);
             p.AddPrim("sr", sr);
             p.AddPrim("rr", rr);
             p.AddPrim("sl", sl);
             p.AddPrim("tl", tl);
             p.AddPrim("ks", ks);
             p.AddPrim("delay", delay);

             //TODO:  True meta values
        }
        if (flags.HasFlag(EGCopyFlags.CURVE)){
            p.AddPrim("ac", ac);
            p.AddPrim("dc", dc);
            p.AddPrim("sc", sc);
            p.AddPrim("rc", rc);
        }
        if (flags.HasFlag(EGCopyFlags.WAVEFORM)){
            p.AddPrim("waveform", waveform.ToString());
            p.AddPrim("feedback", feedback);
            // p.AddPrim("filter", filter);
            p.AddPrim("reflect", reflect);

            if ( add_iotype || (_use_duty == Waveforms.USE_DUTY || waveform == Waveforms.PULSE) )
            {  //Save a little disk space by not adding these tags if they're unused, but always copy them if it's a clipboard operation.
                p.AddPrim("_use_duty", (int) _use_duty);
                p.AddPrim("duty", duty);
            }

            p.AddPrim("fmTechnique", fmTechnique.ToString());
            p.AddPrim("techDuty", techDuty);
            p.AddPrim("techReflect", techReflect);

            //TODO:  ADD THE RELEVANT CUSTOM WAVEFORM BANK
        }
        if (flags.HasFlag(EGCopyFlags.TUNING)){
             p.AddPrim("mul", mul);
             p.AddPrim("dt", dt);
             p.AddPrim("dt2", dt2);
             p.AddPrim("delay", delay);
             p.AddPrim("fixed_frequency", fixed_frequency);            
        }
        if (flags.HasFlag(EGCopyFlags.LOWPASS)){
            p.AddItem("filter", JSONData.ReadJSON(filter.ToString()) );
        }

        if (flags.HasFlag(EGCopyFlags.RTABLES)){
            p.AddItem("ksr", ksr.JsonMetadata());
            p.AddItem("ksl", ksl.JsonMetadata());
            p.AddItem("vr", vr.JsonMetadata());
        }

        return p;
    }

    ///  Used by godot frontend for a direct clipboard copy.
    public void ClipboardCopy(EGCopyFlags flags)
    {
        GD.Print("EGCopyFlags: ", flags);
        OS.Clipboard = JsonMetadata(flags, true).ToJSONString();
    }

    //Omit ID header and inject iotype.
    public override string ToString()
    {
        //NOTE: DOES NOT CONTAIN OPERATOR SPECIFIC DATA SUCH AS LFO AND WAVEFORM BANK. GET THIS FROM OP OR PATCH INSTEAD.
        var output = JsonMetadata(EGCopyFlags.ALL, true);
        return output.ToJSONString(); 
    }

    public IOError FromString(string input, bool ignoreIOtype)
    {
        var p = JSONData.ReadJSON(input);
        if (p is JSONDataError) return IOError.JSON_MALFORMED;  // JSON malformed.  Exit early.        
        var j = (JSONObject) p;

        if (!ignoreIOtype && (j.GetItem("_iotype", "") != _iotype)) return IOError.INCORRECT_IOTYPE;  //Incorrect iotype.  Exit early.

        //// Uncomment the below line once Version 1 files are no longer supported.  This version didn't have an EG version tag.
        // if (j.GetItem("_version", -1) == -1) return IOError.UNSUPPORTED_VERSION;  //Probably a very early version file.

        var version = j.GetItem("_version", 1);  //Backwards compatibility with version 1, which had no tag.  FIXME:  Remove support eventually.

        //If we got this far, the data is probably okay.  Let's try parsing it.......
        // var idk = new List<string>();
        try
        {

            //Version 2 files use readable enums in the file format.  We need to convert these from integers to the proper enum value if parsing v1 files.
            if (version==1)  
            {
                //Convert enum values to readable ones.
                if (j.HasItem("waveform")) waveform = (Waveforms) j.GetItem("waveform", (int) waveform);
                if (j.HasItem("fmTechnique")) fmTechnique = (Waveforms) j.GetItem("fmTechnique", (int) fmTechnique);

            if (j.HasItem("filter"))
            {
                filter.Enabled = true;
                var filt = (JSONObject) j.GetItem("filter");
                var enabled = filt.GetItem("enabled", false);
                filter.filterType = enabled? FilterType.LOWPASS : FilterType.NONE;  //Lowpass was the only type of filter supported by v1.
                filt.Assign("cutoff", ref filter.cutoff);  filter.cutoff = Math.Min(filter.cutoff, 22050);
                filt.Assign("resonanceAmp", ref filter.resonanceAmp);
                filter.gain = 0;
            }

            } else {  //Assume the version is current?
                //Version 2 parse.  Will not process if the tag is of an unknown type (such as future versions) or doesn't exist.
 
                Waveforms wf,tech;
                if (Enum.TryParse(j.GetItem("waveform", ""), true, out wf)) waveform = wf;
                if (Enum.TryParse(j.GetItem("fmTechnique", ""), true, out tech))  fmTechnique = tech;

                if (j.HasItem("filter"))
                {
                    //Check to see if a valid type was specified.  If not, set filterType to NONE.
                    var filt = (JSONObject) j.GetItem("filter");
                    FilterType ft = FilterType.NONE;

                    //Backwards compatibility with "Enabled" field.  Always set Enabled to TRUE for v2.  FilterType determines filtering.
                    filter.Enabled = true;
                    if (Enum.TryParse(filt.GetItem("type", ""), true, out ft))  filter.filterType = ft;
                    
                    filt.Assign("frequency", ref filter.cutoff);  filter.cutoff = Math.Min(filter.cutoff, 22050);
                    filt.Assign("q", ref filter.resonanceAmp);
                    filt.Assign("gain", ref filter.gain);
                }
            }

             j.Assign("ar", ref ar);
             j.Assign("dr", ref dr);
             j.Assign("sr", ref sr);
             j.Assign("rr", ref rr);
             j.Assign("sl", ref sl);
             j.Assign("tl", ref tl);
             j.Assign("ks", ref ks);
             j.Assign("delay", ref delay);

            j.Assign("ac", ref ac);
            j.Assign("dc", ref dc);
            j.Assign("sc", ref sc);
            j.Assign("rc", ref rc);

            j.Assign("feedback", ref feedback);
            j.Assign("reflect", ref reflect);

            j.Assign("duty", ref duty);
            if (j.HasItem("_use_duty")) _use_duty = (Waveforms) j.GetItem("_use_duty", (int) _use_duty);

            j.Assign("techDuty", ref techDuty);
            j.Assign("techReflect", ref techReflect);

            j.Assign("mul", ref mul);
            j.Assign("dt", ref dt);
            j.Assign("dt2", ref dt2);
            j.Assign("delay", ref delay);
            j.Assign("fixed_frequency", ref fixed_frequency);            


            //Set the tables
            if (j.HasItem("ksr"))  ksr.SetValues((JSONObject) j.GetItem("ksr"));
            if (j.HasItem("ksl"))  ksl.SetValues((JSONObject) j.GetItem("ksl"));
            if (j.HasItem("vr"))  vr.SetValues((JSONObject) j.GetItem("vr"));

        } catch {
            return IOError.JSON_MALFORMED;
        }

        RecalcAll();
        return IOError.OK;
    }

#endregion //IO



    /// Gets the sample rate from the global singleton
    public float SampleRate {
        get 
        {
            //// NOPE NOPE NOPE, NEVERMIND.  This clobbers any semblance of portability.  Use AudioOutput and work on abstracting it from the autoloads.
            // return (float) AutoLoadHelper<float>.Get("global", "sample_rate");
            // return AudioOutput.MixRate;
            return Patch.sample_rate;
        }
    }

}

/// Struct for typical length of one part of an Envelope at value 1 on OPNA
struct BaseSecs
{
    public const float AR = 30;
    public const float DR = 120;
    public const float SR = 120;
    public const float RR = 54;
}