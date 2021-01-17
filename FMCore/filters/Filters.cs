// C# Implementation of the Robert Bristow-Johnson filters
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Godot;

// Bandpass CSG = "Bandpass constant skirt gain, peak gain = Q"
// Bandpass CZPG= "Bandpass constant 0 dB peak gain"
// Allpass: all frequencies are passed through unattenuated, but the phase of the signal changes around the target frequency. What a phaser does.
public enum FilterType {NONE, LOWPASS, HIPASS, BANDPASS_CSG, BANDPASS_CZPG, NOTCH, ALLPASS, PEAKING, LOWSHELF, HISHELF}

public class RbjFilter
{

	// filter coeffs
	float b0a0=1,b1a0=1,b2a0=1,a1a0=1,a2a0=1;
	
	// in/out history
	float ou1,ou2,in1,in2;

    public static float sample_rate=44100.0f;

	public void Reset()
	{
		// reset filter coeffs
		b0a0=b1a0=b2a0=a1a0=a2a0=1.0f;
		
		// reset in/out history
		ou1=ou2=in1=in2=0.0f;	
	}

	public RbjFilter()  { Reset(); }

	public RbjFilter(float mixRate)
	{
		sample_rate = mixRate;
		Reset();
	}

	/// Uses built-in history buffers to this filter.  Can only support one stream per RbjFilter.
	public float Filter(float in0)
	{
		// filter
		float yn = b0a0*in0 + b1a0*in1 + b2a0*in2 - a1a0*ou1 - a2a0*ou2;

		// push in/out buffers
		in2=in1;
		in1=in0;
		ou2=ou1;
		ou1=yn;

		// return output
		return yn;
	}

	/// Filters a sample based on the specified filter i/o history using our precalculated coefficients.<!---->
	public float Filter(float in0, ref float in1, ref float in2, ref float ou1, ref float ou2)
	{
	    //io format:  in1, in2, out1, out2

		// filter
		float yn = b0a0*in0 + b1a0*in1 + b2a0*in2 - a1a0*ou1 - a2a0*ou2;


		// push in/out buffers
		in2=in1;
		in1=in0;
		ou2=ou1;
		ou1=yn;

		// return output
		return yn;
	}

	public void Recalc(FilterType type,double frequency,double q,double db_gain,bool q_is_bandwidth=false)
	{
		// temp pi
		const double temp_pi=3.1415926535897932384626433832795;
		// temp coef vars
		double alpha=0,a0=0,a1=0,a2=0,b0=0,b1=0,b2=0;

		// peaking, lowshelf and hishelf
		if((int)type>6)
		{
			double A	=	Math.Pow(10.0,(db_gain/40.0));
			double omega=	2.0*temp_pi*frequency/sample_rate;
			double tsin	=	Math.Sin(omega);
			double tcos	=	Math.Cos(omega);
			
			if(q_is_bandwidth)
			alpha=tsin * Math.Sinh(Math.Log(2.0)/2.0*q*omega/tsin);
			else
			alpha=tsin/(2.0*q);

			double beta	=	Math.Sqrt(A)/q;
			
			// peaking
			if(type==FilterType.PEAKING)
			{
				b0 = (float) (1.0+alpha*A);
				b1 = (float) (-2.0*tcos);
				b2 = (float) (1.0-alpha*A);
				a0 = (float) (1.0+alpha/A);
				a1 = (float) (-2.0*tcos);
				a2 = (float) (1.0-alpha/A);
			}
			
			// lowshelf
			if(type==FilterType.LOWSHELF)
			{
				b0 = (float) (A*((A+1.0)-(A-1.0)*tcos+beta*tsin));
				b1 = (float) (2.0*A*((A-1.0)-(A+1.0)*tcos));
				b2 = (float) (A*((A+1.0)-(A-1.0)*tcos-beta*tsin));
				a0 = (float) ((A+1.0)+(A-1.0)*tcos+beta*tsin);
				a1 = (float) (-2.0*((A-1.0)+(A+1.0)*tcos));
				a2 = (float) ((A+1.0)+(A-1.0)*tcos-beta*tsin);
			}

			// hishelf
			if(type==FilterType.HISHELF)
			{
				b0 = (float) (A*((A+1.0)+(A-1.0)*tcos+beta*tsin));
				b1 = (float) (-2.0*A*((A-1.0)+(A+1.0)*tcos));
				b2 = (float) (A*((A+1.0)+(A-1.0)*tcos-beta*tsin));
				a0 = (float) ((A+1.0)-(A-1.0)*tcos+beta*tsin);
				a1 = (float) (2.0*((A-1.0)-(A+1.0)*tcos));
				a2 = (float) ((A+1.0)-(A-1.0)*tcos-beta*tsin);
			}

		} else {

			// other filters
			double omega	=	2.0*temp_pi*frequency/sample_rate;
			double tsin	=	Math.Sin(omega);
			double tcos	=	Math.Cos(omega);

			if(q_is_bandwidth)
			alpha=tsin*Math.Sinh(Math.Log(2.0)/2.0*q*omega/tsin);
			else
			alpha=tsin/(2.0*q);

			
			// lowpass
			if(type==FilterType.LOWPASS)
			{
				b0=(1.0-tcos)/2.0;
				b1=1.0-tcos;
				b2=(1.0-tcos)/2.0;
				a0=1.0+alpha;
				a1=-2.0*tcos;
				a2=1.0-alpha;
			}

			// hipass
			if(type==FilterType.HIPASS)
			{
				b0=(1.0+tcos)/2.0;
				b1=-(1.0+tcos);
				b2=(1.0+tcos)/2.0;
				a0=1.0+ alpha;
				a1=-2.0*tcos;
				a2=1.0-alpha;
			}

			// bandpass csg
			if(type==FilterType.BANDPASS_CSG)
			{
				b0=tsin/2.0;
				b1=0.0;
			    b2=-tsin/2;
				a0=1.0+alpha;
				a1=-2.0*tcos;
				a2=1.0-alpha;
			}

			// bandpass czpg
			if(type==FilterType.BANDPASS_CZPG)
			{
				b0=alpha;
				b1=0.0;
				b2=-alpha;
				a0=1.0+alpha;
				a1=-2.0*tcos;
				a2=1.0-alpha;
			}

			// notch
			if(type==FilterType.NOTCH)
			{
				b0=1.0;
				b1=-2.0*tcos;
				b2=1.0;
				a0=1.0+alpha;
				a1=-2.0*tcos;
				a2=1.0-alpha;
			}

			// allpass
			if(type==FilterType.ALLPASS)
			{
				b0=1.0-alpha;
				b1=-2.0*tcos;
				b2=1.0+alpha;
				a0=1.0+alpha;
				a1=-2.0*tcos;
				a2=1.0-alpha;
			}

			if(type==FilterType.NONE)
			{
				b0=1;
				b1=1;
				b2=1;
				a0=1;
				a1=1;
				a2=1;
			}
		}

		// set filter coeffs
		b0a0 = (float) (b0/a0);
		b1a0 = (float) (b1/a0);
		b2a0 = (float) (b2/a0);
		a1a0 = (float) (a1/a0);
		a2a0 = (float) (a2/a0);
	}



    public class FilterData
    {
		RbjFilter rbj;// = new RbjFilter();
        public FilterType filterType = FilterType.NONE;  //Backwards compatibility with gfmp version 1

        private bool enabled = true;  //Used by Envelope to determine whether to pass the sample to the cutoff filter.

		//Backwards compatibility with V1.  Will return TRUE if a filterType is selected.
        public bool Enabled { get => filterType != (FilterType.NONE);  set {enabled = value;  /* if (!value) filterType=FilterType.NONE; */ } }

        public float cutoff=22050;  //Field is named such for backwards compatibility.  Frequency value.
        public float resonanceAmp=1.0f;  //Resonance amplitude.  Field is named such for backwards compatibility.
		public float gain;

		public FilterData() {rbj=new RbjFilter();}
		public FilterData(FilterType f) {filterType = f; rbj=new RbjFilter();}
		public FilterData(FilterType f, float mixRate) {filterType = f; rbj=new RbjFilter(mixRate);}

		public void Reset() {rbj.Reset();}

		public void Recalc()
		{
			rbj.Recalc(filterType, cutoff, resonanceAmp, gain);
		}

		public float Filter(float sample)
		{
			return rbj.Filter(sample);
		}

		public float Filter(float sample, ref float in1, ref float in2, ref float ou1, ref float ou2)
		{
			return rbj.Filter(sample, ref in1, ref in2, ref ou1, ref ou2);
		}

		//Overridable call for compatibility with FormantFilterData
		public virtual float Filter(float sample,  ref float[] filterHistory)
		{ return Filter(sample, ref filterHistory[0], ref filterHistory[1], 
                               	ref filterHistory[2], ref filterHistory[3]);
		}

        public override string ToString()
        {
            var output=new GdsFMJson.JSONObject();

			output.AddPrim("type", filterType.ToString());
            output.AddPrim("frequency", cutoff);
            output.AddPrim("q", resonanceAmp);
            output.AddPrim("gain", gain);

            return output.ToJSONString();
        }
    }

	public class FormantFilterData : FilterData
	{
		public FormantFilter rbj; //= new FormantFilter();
		public float freq2;  //Peak frequency 2 for formant filter

		FormantFilterData() {rbj=new FormantFilter();}
		public FormantFilterData(FilterType f) {filterType = f; rbj=new FormantFilter();}
		public FormantFilterData(FilterType f, float mixRate) {filterType = f; rbj=new FormantFilter(mixRate);}


		public override float Filter(float sample,  ref float[] filterHistory)
		{ return Filter(sample, ref filterHistory[0], ref filterHistory[1], 
                               	ref filterHistory[2], ref filterHistory[3]);
		}
	}

}