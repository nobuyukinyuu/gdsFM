using Godot;
using System;

using System.Runtime.CompilerServices;

public enum Waveforms {SINE, SAW, TRI, PULSE, ABSINE, WHITE, PINK, BROWN};

public class oscillators : Node
{
	static Random random = new Random ();
	const double TAU = Mathf.Tau;

	static double[] sintable;

	
	static PinkNumber pinkr = new PinkNumber() ; 
	static double lastr = 0.0f;

	readonly static double[] TRITABLE = {0.0f, 0.2f, 0.4f, 0.6f, 0.8f, 1.0f, 0.8f, 0.6f, 0.4f, 0.2f, 0.0f, -0.2f, -0.4f,
							-0.6f, -0.8f, -1.0f, -0.8f, -0.6f, -0.4f, -0.2f};


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		gen_sin_table(2048);
	}


	public static void gen_sin_table(int res) {
		sintable = new double[res];
		for(int i=0; i < res; i++){
			sintable[i] = Math.Sin(TAU * i / (double)(res));
		}
	}
// Grab a sine value from the lookup table
	public static double sint(double n){
		int sz = sintable.Length;
		int idx = (int) Math.Round(n/TAU * (double)sz);
		idx = OscMath.Wrap(idx,0, sz);

		return sintable[idx];
	}

// Grab a sine from the lookup table, from 0-1 instead of 0-TAU.
	public static double sint2(double n){
		int sz = sintable.Length;
		int idx = (int) Math.Round(n*sz);
		idx = OscMath.Wrap(idx,0, sz);

		return sintable[idx];
	}


	public static double wave(double n, Waveforms waveform = Waveforms.SINE, double duty = 0.5f, bool reflect=false){
		n %= 1.0f;

		double sReflect = reflect? -1 : 1;

		switch(waveform){
			case Waveforms.PULSE:
				if ((n >= duty) ^ reflect) return 1f;
				return -1f;

			case Waveforms.SINE:
				return sint2(n)  * sReflect;

			case Waveforms.ABSINE:
				return Math.Abs(sint2(n)) * sReflect;

			case Waveforms.TRI:
				return TRITABLE[(int)(Math.Abs(n)*20)] * sReflect;

			case Waveforms.SAW:
				// return (1.0f - (n * 2.0f)) * sReflect; 
				// return (1.0 - (Math.Min((n/duty), 1.0) * 2.0 * duty)) * sReflect; 
				duty += Single.Epsilon;
				duty *= 2.0;
				return 1.0 - Math.Max(n + duty - 1.0, 0.0) * (1.0/duty) * 2 * sReflect;

			case Waveforms.PINK:
				return pinkr.GetNextValue();

			case Waveforms.BROWN:
				lastr += (double)random.NextDouble() * 0.2f - 0.1f;
				lastr *= 0.99f;
				return lastr;

			case Waveforms.WHITE:
				return (double)random.NextDouble() * 2.0f - 1.0f;

			default:
				return 0f;
		}

	}

}

// Pink noise generator
class PinkNumber
{
private
  int max_key;
  Random rand= new Random();
  int key;
  int[] white_values = new int[5];
  int range;
public
  PinkNumber(int range = 128)
	{
	  max_key = 0x1f; // Five bits set
	  this.range = range;
	  key = 0;
	  for (int i = 0; i < 5; i++)
 white_values[i] = rand.Next() % (range/5);
	}
  public double GetNextValue()
	{
	  int last_key = key;
	  int sum;

	  key++;
	  if (key > max_key)
 key = 0;
	  // Exclusive-Or previous value with current value. This gives
	  // a list of bits that have changed.
	  int diff = last_key ^ key;
	  sum = 0;
	  for (int i = 0; i < 5; i++)
 {
   // If bit changed get new random number for corresponding
   // white_value
   if ((diff & (1 << i)) > 0 )
	 white_values[i] = rand.Next() % (range/5);
   sum += white_values[i];
 }
	  return sum / 64.0f-1.0f;
	}
};


//Auxillary functions intended to make stuff chooch faster
static class OscMath
{

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Wrap(int value, int min, int max) {
		int range = max - min;
		return range == 0 ? min : min + ((((value - min) % range) + range) % range);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int64 Wrap64(Int64 value, Int64 min, Int64 max) {
		Int64 range = max - min;
		return range == 0 ? min : min + ((((value - min) % range) + range) % range);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double WrapF(double value, double min, double max) {
		double range = max - min;
		return is_zero_approx(range) ? min : value - (range * Math.Floor((value - min) / range));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool is_zero_approx(Double s) {
		return Math.Abs(s) < Double.Epsilon;
	}

}