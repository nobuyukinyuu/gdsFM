using Godot;
using System;

public class Envelope : Resource 
{
	double ar=0f;						//Attack
	double dr=0f;						//Decay
	double sr=0f;						//Sustain
	double rr = 1;  //Release.  double between 0-1
	double sl=0;                        //Sustain level
    double tl = 50;             //Total level  (attenuation)
    double ks=0;						//Key scale
	double mul=0.0f;						//Frequency multiplier  //double between 0.5 and 15
    double dt=0;// setget set_detune		//_detune

	public double hz = 440.0f;  //Operator frequency.  Tuned to A-4 by default
	public double feedback = 0f;  //TODO:  Figure output if this is appropriate to use on multiple operators
	public double duty = 0.5f;  //Duty cycle is only used for pulse right now
	public Waveforms waveform = Waveforms.SINE;


    //Property values used to translate "user-friendly" values to internal values.
    public double Ar { get => ar; set => ar = value; }
    public double Dr { get => dr; set => dr = value; }
    public double Sr { get => sr; set => sr = value; }
    public double Rr { get => rr;   set =>  _set_release_time(value); }
    public double Sl { get => sl; set => sl = value; }
    public double Tl { get => tl; set  {tl = value;  _totalLevel = value / 100.0f;} }
    public double Ks { get => ks; set => ks = value; }
    public double Mul { get => mul;  set => _set_frequency_multiplier(value); }
    public double Dt { get => dt; set => set_detune(value); }



    // True, internal values referenced by the operator to reduce re-calculating
    // these values when the above EG slider value changes.
    public double _attackTime;
    public double _decayTime;
    public double _susTime;
    public double _releaseTime = 1.0f; //Calculated from rr when set
    public double _susLevel;
    public double _totalLevel = 0.5f;
	public double _freqMult = 0.5f;
	public double _detune = 1;
	
    public double _egLength;  //total envelope length, in samples
	
//	var hz = 440.0
	int samples = 0;  //Samples elapsed

    //ASDR setters
    //Called when ADSR properties change
    void recalc_adsr(bool reset_samples=false)
    {
        if (reset_samples)  samples = 0;
        _egLength = _attackTime + _decayTime + _susTime + _releaseTime;

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
            _attackTime = double.MaxValue/5.0;  //Stupid workaround to avoid Double.PositiveInfinity.  Who's gonna hold a note for 2.264668e+299 hours?
        } else {
            _attackTime = get_curve_secs(BaseSecs.AR, val) * SampleRate;
        }
        recalc_adsr();
    }

	void _set_release_time(double val){
        // rr = val;

		// if (val <= 0) {
		// 	_releaseTime = double.MaxValue;
        // }
		// else if (val >= 1.0){ _releaseTime = 0; }
		// else {	_releaseTime = -Math.Log(Rr) *2;  }  // an rr of 0.01 will produce a ~10s release time.
	
        rr = val;
        if (val <= 0) {
            _releaseTime = double.MaxValue/5.0;  //Stupid workaround to avoid Double.PositiveInfinity.  Who's gonna hold a note for 2.264668e+299 hours?
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
    double get_curve_secs(double base_secs, double val)
    {
        //estimated base rates for OPNA at value 1:
        //AR    30sec?   (32 values)
        //DR    120sec  (32 values)
        //SR    120sec  (32 values)
        //RR    54sec   (16 values)
        return base_secs / Math.Pow(2, val-1);
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