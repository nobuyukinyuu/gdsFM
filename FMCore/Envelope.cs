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
	
	
//	var hz = 440.0
	int samples = 0;  //Samples elapsed

	void _set_release_time(double val){
        // double output;
        rr = val;

		if (val <= 0) {
			_releaseTime = double.MaxValue;
        }
		else if (val >= 1.0){ _releaseTime = 0; }
		else {	_releaseTime = -Math.Log(Rr) *2;  }  // an rr of 0.01 will produce a ~10s release time.
	
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
    

}