using Godot;
using System;

// using TinyJson;
using Newtonsoft.Json;

//Envelope Generator.  Includes pitch, curve, ADSR, feedback, waveform, etc.
public class Envelope : Node 
{

//This measurement was done against DX7 detune at A-4, where every 22 cycles the tone would change (-detune) samples at a recording rate of 44100hz.
//See const definitions in Note.cs for more information about the extra-fine detune increment.
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

    [System.Runtime.Serialization.IgnoreDataMember]
    public string ownerName;  // Debug purposes
    [System.Runtime.Serialization.IgnoreDataMember]
    public int opID;  // Operator index typically associated with this envelope. Used to reset delay phases.

    //STANDARD FM EG STUFF
	double ar=31;						//Attack
	double dr=31;						//Decay
	double sr=6;            			//Sustain
	double rr = 15;                      //Release
	double sl=100.0;                      //Sustain level
    double tl = 100;                     //Total level  (attenuation)
    double ks=0;						//Key scale
	double mul=1.0;					//Frequency multiplier  //double between 0.5 and 15
    double dt=0;                        //_detune
    double dt2=0;                        // Coarse detune (semitone, -12 to 12)

    double delay=0;                     //Envelope delay
    double fixed_frequency=0;           //Use fixed frequency if >0


    //CUSTOM STUFF
	public Waveforms waveform = Waveforms.SINE;
	public double feedback = 0;  //Operator feedback only used in op chain termini.
    public bool reflect = false;  //Waveform reflection.  Inverts sine phase, makes sawtooth point to the right, etc.

	public double duty = 0.5;  //Duty cycle is only used for pulse right now
    public Waveforms _use_duty = 0;  //Set to Waveforms.USE_DUTY (0x100) to use duty cycle variants of functions
    //Property accessor for GDScript that flips the internal duty flag to the proper native bitmask.
    public bool UseDuty { get => _use_duty==Waveforms.USE_DUTY; set => _use_duty=value? Waveforms.USE_DUTY : Waveforms.SINE;}


    public Waveforms fmTechnique = Waveforms.SINE;  //FM Technique used to modulate phase when requesting a sample up the chain.
    public double techDuty = 0.5;  //Duty cycle for the fmTechnique.  Currently UNUSED (no use_tech_duty bit set) to save CPU. 
    public bool techReflect = false;  //Waveform reflection for the fmTechnique


    //ADSR easing curve values.
    public double ac = -2.0;  //Attack curve.  In-out.
    public double dc = 0.75;  //Decay curve.  75% Linear, 25% Ease-out.
    public double sc = 0.5;  //Sustain curve.  50% Linear, 50% Ease-out.
    public double rc = 0.25;  //Release curve.  75% Ease-out.


    //Response curves.
    internal RTable<Double> ksr = RTable.FromPreset<Double>(RTable.Presets.TWELFTH_ROOT_OF_2 | RTable.Presets.DESCENDING, 
                                                           floor:100, allow_zero:false );      //KeyScale rate. Lower values shrink envelope timings.
    private RTable<Double> ksl = RTable.FromPreset<Double>(RTable.Presets.TWELFTH_ROOT_OF_2 | RTable.Presets.DESCENDING,
                                                            floor:100);  //KeyScale level. Multiplies from 0-100% against TL of this envelope.
    private RTable<Double> vr = RTable.FromPreset<Double>(RTable.Presets.IN_OUT 
                                                         |0, floor:100);  //Velocity response. Sensitivity goes from 0% to 100% (0-1).  Default 0


    public Godot.Collections.Dictionary Ksr { get => ksr.ToDict(); set => ksr.SetValues(value); }
    public Godot.Collections.Dictionary Ksl { get => ksl.ToDict(); set => ksl.SetValues(value); }
    public Godot.Collections.Dictionary Vr { get => vr.ToDict(); set => vr.SetValues(value); }



    //Property values used to translate "user-friendly" values to internal values, which are different than standard FM values.
    public double Ar { get => ar; set => _set_attack_time(value); }
    public double Dr { get => dr; set => _set_decay_time(value); }
    public double Sr { get => sr; set => _set_sustain_time(value); }
    public double Rr { get => rr;   set =>  _set_release_time(value); }
    public double Sl { get => sl; set  {sl = value;  _susLevel = value / 100.0;} }
    public double Tl { get => tl; set  {tl = value;  _totalLevel = value / 100.0;} }
    public double Ks { get => ks; set => ks = value; }
    public double Mul { get => mul;  set => _set_octave_multiplier(value); }
    public double Dt { get => dt; set => set_detune(value); }
    public double Dt2 { get => dt2; set => set_detune2(value); }
    public double FixedFreq { get => fixed_frequency; set {fixed_frequency = value;  update_multiplier();} }

    public double Delay { get => delay; set {_delay = value*SampleRate / 1000; delay=value;} }



    // True, internal values referenced by the operator to reduce re-calculating
    // these values when the above EG slider value changes.
    public double _attackTime = 50;//double.Epsilon; //Needs to be large enough to avoid a clicking pop under certain circumstances when the phase isn't 0
    public double _decayTime = 50;//double.Epsilon;
    public double _susTime = 2880000; //float.MaxValue/5;  //about a minute, depending on sample rate
    public double _releaseTime = 100; //Calculated from rr when set
    public double _susLevel = 1.0;
    public double _totalLevel = 1.0;
	public double _freqMult = 1.0;  //Octave multiplier (MUL)
	public double _coarseDetune = 1;
	public double _detune = 1;
    public double _delay;
	
    public double totalMultiplier = 1;  //Precalculated value of mul+dt+dt2 or the fixed frequency. See update_multiplier().

    //Internal ADSR timer calculating values.
    public double _egLength;  //total envelope length, in samples.  TODO:  Make ASDR true values int?
    double _ad;  //Attack time + decay time
    public double _ads; //Attack, decay, and sustain time.
	double _adr; //Attack, decay, and release time.  Used to check whether the sample offset from release is beyond the need to calculate an easing curve.

    //ASDR Getter
    public double VolumeAtSamplePosition(Note note)
    {

        int s = note.samples;
        bool noteOff=!note.pressed;
        int releaseSample=note.releaseSample;
        double output=0;

        //Determine key scaling rate for this note.
        double _ksr= ksr[note.midi_note];

        //Generate keyScaled envelope times
        var atkTime = _attackTime * ksr[note.midi_note];
        var decTime = _decayTime * ksr[note.midi_note];
        var susTime = _susTime * ksr[note.midi_note];
        var rlsTime = _releaseTime * ksr[note.midi_note];
        var ad = atkTime + decTime + _delay;

        //Determine the envelope phase.
        if (s < _delay)  {return 0;} //Delay phase.
        else if (s >= _delay && note.delayed[opID]==false) {note.ResetPhase(opID);  note.delayed[opID]=true;}


        if (s-_delay < atkTime) { // Attack phase.
            //Interpolate between 0 and the total level.
            output= GDSFmFuncs.EaseFast(0.0, 1.0, (s-_delay) / atkTime, ac);
            // if(Double.IsNaN(output)) {System.Diagnostics.Debugger.Break();}

        } else if ((s-_delay >= atkTime) && (s-_delay < ad) ) {  //Decay phase.
            //Interpolate between the total level and sustain level.
            output= GDSFmFuncs.EaseFast(1.0, _susLevel, (s-(atkTime+_delay)) / decTime, dc);
            // if(Double.IsNaN(output)) {System.Diagnostics.Debugger.Break();}

        } else if ((s-delay >= ad) ) {  //Sustain phase.
            //Interpolate between sustain level and 0.
            output= GDSFmFuncs.EaseFast(_susLevel, 0, (s-_delay-ad) / susTime, sc);
            // if(Double.IsNaN(output)) {System.Diagnostics.Debugger.Break();}
        }

        // #if DEBUG
        // if(Double.IsNaN(output)) System.Diagnostics.Debugger.Break();
        // #endif

        if (noteOff && (s-releaseSample) < rlsTime)  //Apply release envelope.
        {
            output *= GDSFmFuncs.EaseFast(1.0, 0.0, (s-releaseSample) / rlsTime, rc);
            // if(Double.IsNaN(output)) {System.Diagnostics.Debugger.Break();}
        } else if (noteOff && (s-releaseSample) >= rlsTime) {
            return 0;
        }

        #if DEBUG
        if(Double.IsNaN(output)) 
        {
            System.Diagnostics.Debugger.Break();  //Stop. NaN propagation. This should never trigger but if it does prepare for pain
            return 0;
            // throw new ArithmeticException("NaN encountered in calculated envelope output");
        }
        #endif

        return output * ksl[note.midi_note] * vr[note.midi_velocity];
    }

    //ASDR setters
    //Called when ADSR properties change
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

    void _set_attack_time(double val) {
        ar = val;
        if (val <=0) {
            _attackTime = float.MaxValue/5.0;  //Stupid workaround to avoid Double.PositiveInfinity.  Who's gonna hold a note for 2.264668e+299 hours?
        } else {
            _attackTime = get_curve_secs(BaseSecs.AR, val, 0.5, 0) * SampleRate;
        }
        recalc_adsr();
    }

    void _set_decay_time(double val) {
        dr = val;
        if (val <=0) {
            _decayTime = 0;
        } else {
            _decayTime = get_curve_secs(BaseSecs.DR, val, 0.5) * SampleRate;
        }
        recalc_adsr();
    }

    void _set_sustain_time(double val) {
        sr = val;
        if (val <=0) {
            _susTime = float.MaxValue/5.0;  //Stupid workaround to avoid Double.PositiveInfinity.  Who's gonna hold a note for 2.264668e+299 hours?
        } else {
            _susTime = get_curve_secs(BaseSecs.SR, val, 0.5) * SampleRate;
        }
        recalc_adsr();
    }

	void _set_release_time(double val){
        // rr = val;

		// if (val <= 0) {
		// 	_releaseTime = float.MaxValue;
        // }
		// else if (val >= 1.0){ _releaseTime = 0; }
		// else {	_releaseTime = -Math.Log(Rr) *2;  }  // an rr of 0.01 will produce a ~10s release time.
	
        rr = val;
        if (val <= 0) {
            _releaseTime = SampleRate * 1800;  //Workaround to avoid Double.PositiveInfinity and stuck notes.  30 minutes release
        } else {
            _releaseTime = get_curve_secs(BaseSecs.RR, val) * SampleRate;
        }
        recalc_adsr();
    }


    //Determines the length of part of an ADSR curve for sample calculation.  
    //Multiply the output of this method to the sample rate to get that number of samples.

    //Consider baking a table of the appropriate length for each easing curve in the ADSR components at 1.0 TL.
    //For RR, multiply the value output by this table by the level at sustain on KeyOff.  Process until hitting sustain.
    //Precalculate absolute total length (in samples) of a blip. During KeyOff event, in sustain state whenever KeyOff is detected,
    //Immediately move the play head to the total length minus RR length.
    //http://forums.submarine.org.uk/phpBB/viewtopic.php?f=9&t=16
    double get_curve_secs(double base_secs, double val, double scaleFactor=1.0, double minlength=0.005)
    {
        //estimated base rates for OPNA at value 1:
        //AR    30sec?   (32 values)
        //DR    120sec  (32 values)
        //SR    120sec  (32 values)
        //RR    54sec   (16 values)
        return base_secs / Math.Pow(2, (val-1) * scaleFactor);
    }




// =========  PITCH AND TUNING RELATED SETTERS  ===============
 
    //Whenever a tuning field changes, this should be called to update the precalculated total multiplier amount. 
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

 
    //Octave multiplier
	void _set_octave_multiplier(double val){
		mul = val;
		
		if (val == 0)  val = 0.5f;
		_freqMult = val; 

        update_multiplier();
    }

    //Extra fine detune
	void set_detune(double val){  
		dt = val;
		// _detune = 1.0f + (val / 10000.0f);  //approx max multiplier is 1.0 ± 0.01
        switch (Math.Sign(val))
        {
            case 0:
                _detune = 1.0;
                break;

            case 1:
        		_detune = (double) GDSFmFuncs.lerp(1.0M, DETUNE_MAX, (Decimal) (val / 100.0)) ;
                break;

            case -1:
        		_detune = (double) GDSFmFuncs.lerp(1.0M, DETUNE_MIN, (Decimal) (-val / 100.0)) ;
                break;
        }

        update_multiplier();
    }

    //Coarse detune (Semitone)
    void set_detune2(double val)
    {
        dt2 = val;
        switch (Math.Sign(val))
        {
            case 0:
                _coarseDetune = 1.0;
                break;
            
            case 1:
                _coarseDetune = 1 * Math.Pow(2, Math.Abs(val) / 12.0);
                break;
            
            case -1:
                _coarseDetune = 1 / Math.Pow(2, Math.Abs(val) / 12.0);
                break;
        }

        update_multiplier();
    }



    // ================ IO ==================

    public String PrintJson()
    {
        var settings=new JsonSerializerSettings();
        settings.ContractResolver = new IgnoreParentPropertiesResolver(true);
        // settings.Formatting = Formatting.Indented;

        var output = JsonConvert.SerializeObject(this, settings);
        GD.Print(output);
        return output;
    }

    public void CopyEnvelope()
    {
        //Copy all properties.
        // var output = TinyJson.JSONWriter.ToJson(this);
        var output = PrintJson();
        if (output != null) OS.Clipboard = output;

    }

    public int ParseEnvelope(string json)
    {
        //TODO:  Don't do properties, recalculate only if necessary or explicitly specified
        

        return 0;
    }

    //Gets the sample rate from the global singleton
    public double SampleRate {
        get 
        {
            //// NOPE NOPE NOPE, NEVERMIND.  This clobbers any semblance of portability.  Use AudioOutput and work on abstracting it from the autoloads.
            // return (double) AutoLoadHelper<Double>.Get("global", "sample_rate");
            return AudioOutput.MixRate;
        }
    }

}

//Struct for typical length of one part of an Envelope at value 1 on OPNA
struct BaseSecs
{
    public const double AR = 30;
    public const double DR = 120;
    public const double SR = 120;
    public const double RR = 54;
}