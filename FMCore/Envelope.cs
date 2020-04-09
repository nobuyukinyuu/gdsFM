using Godot;
using System;

public class Envelope : Node 
{
    public Envelope()  //Default ctor.
    {
        recalc_adsr();
    }

    public Envelope(string name)
    {
        ownerName = name;
    }
    public string ownerName;  // Debug purposes

    //STANDARD FM EG STUFF
	double ar=31f;						//Attack
	double dr=0f;						//Decay
	double sr=0f;						//Sustain
	double rr = 15;                      //Release
	double sl=100.0;                      //Sustain level
    double tl = 50;                     //Total level  (attenuation)
    double ks=0;						//Key scale
	double mul=0.0f;					//Frequency multiplier  //double between 0.5 and 15
    double dt=0;                        //_detune


    //CUSTOM STUFF
	public Waveforms waveform = Waveforms.SINE;
	public double feedback = 0f;  //Operator feedback only used in carriers
	public double duty = 0.5f;  //Duty cycle is only used for pulse right now
    public bool reflect = false;  //Waveform reflection.  Inverts sine phase, makes sawtooth point to the right, etc.

    public Waveforms fmTechnique = Waveforms.SINE;  //FM Technique used to modulate phase when requesting a sample up the chain.
    public double techDuty = 0.5f;  //Duty cycle for the fmTechnique    
    public bool techReflect = false;  //Waveform reflection for the fmTechnique

    //ADSR easing curve values.
    public double ac = -2.0;  //Attack curve.  In-out.
    public double dc = 0.75;  //Decay curve.  50% Linear, 50% Ease-out.
    public double sc = 0.8;  //Sustain curve.  80% Linear, 20% Ease-out.
    public double rc = 0.5;  //Release curve.  Ease-out.



    //Property values used to translate "user-friendly" values to internal values.
    public double Ar { get => ar; set => _set_attack_time(value); }
    public double Dr { get => dr; set => _set_decay_time(value); }
    public double Sr { get => sr; set => _set_sustain_time(value); }
    public double Rr { get => rr;   set =>  _set_release_time(value); }
    public double Sl { get => sl; set  {sl = value;  _susLevel = value / 100.0;} }
    public double Tl { get => tl; set  {tl = value;  _totalLevel = value / 100.0f;} }
    public double Ks { get => ks; set => ks = value; }
    public double Mul { get => mul;  set => _set_frequency_multiplier(value); }
    public double Dt { get => dt; set => set_detune(value); }



    // True, internal values referenced by the operator to reduce re-calculating
    // these values when the above EG slider value changes.
    public double _attackTime;
    public double _decayTime;
    public double _susTime = float.MaxValue/5;
    public double _releaseTime = 0.0; //Calculated from rr when set
    public double _susLevel = 1.0;
    public double _totalLevel = 0.5;
	public double _freqMult = 0.5;
	public double _detune = 1;
	
    public double _egLength;  //total envelope length, in samples.  TODO:  Make ASDR true values int?
    double _ad;  //Attack time + decay time
    double _ads; //Attack, decay, and sustain time.
	double _adr; //Attack, decay, and release time.  Used to check whether the sample offset from release is beyond the need to calculate an easing curve.

    //ASDR Getter
    public double VolumeAtSamplePosition(int s, bool noteOff=false, int releaseSample=0)
    {

        double output=0;

        //Determine the envelope phase.
        if (s < _attackTime) { // Attack phase.
            //Interpolate between 0 and the total level.
            output= GDSFmFuncs.EaseFast(0.0, 1.0, s / _attackTime, ac);

        } else if ((s >= _attackTime) && (s < _ad) ) {  //Decay phase.
            //Interpolate between the total level and sustain level.
            output= GDSFmFuncs.EaseFast(1.0, _susLevel, (s-_attackTime) / _decayTime, dc);

        } else if ((s >= _ad) && !noteOff ) {  //Sustain phase.
            //Interpolate between sustain level and 0.
            output= GDSFmFuncs.EaseFast(_susLevel, 0, (s-_ad) / _susTime, sc);


        } else if (noteOff && (s-releaseSample > _adr)) {  //Release: Note off, but beyond the total time of the attack, decay, and release phases.
            return 0;

        // } else if ((noteOff && (s > _ad )) || s > _ads) {  //Release:  Note off, and not yet beyond the total time of the release phase.
        } else if (s > _ads) {  //Release:  Note off, and not yet beyond the total time of the release phase.

            //The initial release level is determined by the level at the last sustain phase.  Interpolate between this and 0.
            //If the note was released before _ad, the initial release level is equal to _susLevel.
            
            //Calculate sustain level at the the time of the noteOff event.
            double lastSL = GDSFmFuncs.EaseFast(_susLevel, 0, (releaseSample-_ad) / _susTime, sc);

            //Interpolate between last sustain level and 0.
            output= GDSFmFuncs.EaseFast(lastSL, 0, (s-releaseSample) / _releaseTime, rc);
        }

        if (noteOff)  //Apply release envelope.
        {
            output *= GDSFmFuncs.EaseFast(0.0, 1.0, s-releaseSample, rc);
        }
   
        return output;
    }

    //ASDR setters
    //Called when ADSR properties change
    void recalc_adsr()
    {
        _egLength = _attackTime + _decayTime + _susTime + _releaseTime;

        _ad  = _attackTime + _decayTime;
        _ads = _ad + _susTime;
        _adr = _ad + _releaseTime;

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
            _attackTime = get_curve_secs(BaseSecs.AR, val, 0.5) * SampleRate;
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
            _releaseTime = float.MaxValue/5.0;  //Stupid workaround to avoid Double.PositiveInfinity.  Who's gonna hold a note for 2.264668e+299 hours?
        } else {
            _releaseTime = get_curve_secs(BaseSecs.RR, val) * SampleRate;
        }
        recalc_adsr();
    }

	void _set_frequency_multiplier(double val){
		mul = val;
		
		if (val == 0)  val = 0.5f;
		_freqMult = val; //* hz
    }
	void set_detune(double val){
		dt = val;
		_detune = 1.0f + (val / 10000.0f);
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

//Determines the length of part of an ADSR curve for sample calculation.  
//Multiply the output of this method to the sample rate to get that number of samples.

//Consider baking a table of the appropriate length for each easing curve in the ADSR components at 1.0 TL.
//For RR, multiply the value output by this table by the level at sustain on KeyOff.  Process until hitting sustain.
//Precalculate absolute total length (in samples) of a blip. During KeyOff event, in sustain state whenever KeyOff is detected,
//Immediately move the play head to the total length minus RR length.
//http://forums.submarine.org.uk/phpBB/viewtopic.php?f=9&t=16
    double get_curve_secs(double base_secs, double val, double scaleFactor=1.0)
    {
        //estimated base rates for OPNA at value 1:
        //AR    30sec?   (32 values)
        //DR    120sec  (32 values)
        //SR    120sec  (32 values)
        //RR    54sec   (16 values)
        return base_secs / Math.Pow(2, (val-1) * scaleFactor);
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