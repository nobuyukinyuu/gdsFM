//Implementation of the Robert Bristow-Johnson filters
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

    public static double sample_rate=44100.0;
	public bool Enabled;

	public void Reset()
	{
		// reset filter coeffs
		b0a0=b1a0=b2a0=a1a0=a2a0=1.0f;
		
		// reset in/out history
		ou1=ou2=in1=in2=0.0f;	
	}

	public RbjFilter()  { Reset(); }

	public RbjFilter(double mixRate)
	{
		sample_rate = mixRate;
		Reset();
	}

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

	public void calc_filter_coeffs(FilterType type,double frequency,double q,double db_gain,bool q_is_bandwidth)
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


}