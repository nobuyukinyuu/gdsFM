using Godot;
using System;

using System.Runtime.CompilerServices;

[Flags]
public enum Waveforms {SINE, SAW, TRI, PULSE, ABSINE, WHITE, PINK, BROWN, CUSTOM, USE_DUTY=0x100};

public class oscillators : Node
{
	const double TAU = Mathf.Tau;

	static double[] sintable;


	//Used for periodic noise
	static byte[] noise_counter = new byte[129];   //Extra field is to accomodate 
	static double[] lastNoiseValue = new double[129];

	static PinkNumber pinkr = new PinkNumber() ; 
	static double lastr = 0.0f;

	readonly static double[] TRITABLE = {0.0f, 0.2f, 0.4f, 0.6f, 0.8f, 1.0f, 0.8f, 0.6f, 0.4f, 0.2f, 0.0f, -0.2f, -0.4f,
							-0.6f, -0.8f, -1.0f, -0.8f, -0.6f, -0.4f, -0.2f};


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// gen_sin_table(2048);
		gen_sin_table(9600);  //Sounds better at high frequencies....
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


	public static double wave(double n, Waveforms waveform = Waveforms.SINE, double duty = 0.5, bool reflect=false, int auxData=1){
		n %= 1.0;

		double sReflect = reflect? -1 : 1;

		switch(waveform){
			case Waveforms.PULSE:
			case Waveforms.PULSE | Waveforms.USE_DUTY:
				// if (duty%1.0 == 0) duty = 0.5;  //Prevents silence from full duty wave cycles and allows sane duty defaults for other waveforms
				if ((n >= duty) ^ reflect) return 1f;
				return -1f;

			case Waveforms.SINE:
				return sint2(n)  * sReflect;

			case Waveforms.SINE|Waveforms.USE_DUTY:
				return n>=duty ? 0.0 : sint2(n*1.0/(duty+Double.Epsilon)) * sReflect;

			case Waveforms.ABSINE:
				return Math.Abs(sint2(n)) * sReflect;
			case Waveforms.ABSINE|Waveforms.USE_DUTY:
				return n >= duty? 0.0:  Math.Abs(sint2(n*1.0/(duty+Double.Epsilon))) * sReflect;

			case Waveforms.TRI:
				return TRITABLE[(int)(Math.Abs(n)*20)] * sReflect;
			case Waveforms.TRI|Waveforms.USE_DUTY:
				return n >= duty? 0.0:  TRITABLE[(int)(Math.Abs(n)*20)] * sReflect;

			case Waveforms.SAW:
				return (1.0 - (n * 2.0)) * sReflect; 

			case Waveforms.SAW|Waveforms.USE_DUTY:
				// return (1.0f - (n * 2.0f)) * sReflect; 
				// return (1.0 - (Math.Min((n/duty), 1.0) * 2.0 * duty)) * sReflect; 
				duty += Single.Epsilon;
				// duty *= 2.0;   //Used when duty needs to be in the 0-0.5 range due to a 0.5 default
				return 1.0 - Math.Max(n + duty - 1.0, 0.0) * (1.0/duty) * 2 * sReflect;

			case Waveforms.PINK:
				return pinkr.GetNextValue();
			case Waveforms.PINK|Waveforms.USE_DUTY:
				return n<duty?  pinkr.GetNextValue() : 0.0;

			case Waveforms.BROWN:
				lastr += ThreadSafeRandom.NextDouble() * 0.2 - 0.1;
				lastr *= 0.99f;
				return lastr;
			case Waveforms.BROWN|Waveforms.USE_DUTY:
				if (n < duty) {
					lastr += ThreadSafeRandom.NextDouble() * 0.2 - 0.1;
					lastr *= 0.99f;
					return lastr;					
				} else {
					return 0.0;
				}

			case Waveforms.WHITE:
				// return (double)random.NextDouble() * 2.0 - 1.0;
				noise_counter[auxData] += 0b1;
				noise_counter[auxData] %= Convert.ToByte(auxData);

				if (noise_counter[auxData]==0) lastNoiseValue[auxData]=ThreadSafeRandom.NextDouble() * 2.0 - 1.0;
				return lastNoiseValue[auxData];

			case Waveforms.WHITE|Waveforms.USE_DUTY:
				noise_counter[auxData] += 0b1;
				noise_counter[auxData] %= Convert.ToByte(auxData);

				if (noise_counter[auxData]==0) lastNoiseValue[auxData]=ThreadSafeRandom.NextDouble() * 2.0 - 1.0;
				return n<duty? lastNoiseValue[auxData] : 0.0;

			default:
				return 0;  //FIXME:  REROUTE CASES FOR FUNCTIONS NOT SUPPORTING DUTY CYCLE OR ELSE THIS WILL OCCUR
		}

	}

	//Custom waveform function
	public static double wave(double n, RTable<double> auxData=null){
		if (auxData==null) return 0.0;
		n %= 1.0;
		return auxData[(int)(Math.Abs(n)*auxData.values.Length -double.Epsilon)];
	}
}



// Pink noise generator
class PinkNumber
{
private
  int max_key;
//   private static Random rand= new Random();
  int key;
  int[] white_values = new int[5];
  int range;
public
  PinkNumber(int range = 128)
	{
	  max_key = 0x1f; // Five bits set
	  this.range = range;
	  key = 0;
	  for (int i=0; i<5; i++)	white_values[i] = ThreadSafeRandom.Next() % (range/5);
	}

  public double GetNextValue()
	{
	  int last_key = key;
	  int sum;

	  key++;
	  if (key > max_key)    key = 0;
	  // Exclusive-Or previous value with current value. This gives
	  // a list of bits that have changed.
	  int diff = last_key ^ key;
	  sum = 0;
	  for (int i = 0; i < 5; i++)
 {
   // If bit changed get new random number for corresponding
   // white_value
   if ((diff & (1 << i)) > 0 )
	 white_values[i] = ThreadSafeRandom.Next() % (range/5);
   sum += white_values[i];
 }
	  return sum / 64.0-1.0;
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

public static class ThreadSafeRandom
{
    private static Random _global = new Random();
    [ThreadStatic]
    private static Random _local;

    public static int Next()
    {
        Random inst = _local;
        if (inst == null)
        {
            int seed;
            lock (_global) seed = _global.Next();
            _local = inst = new Random(seed);
        }
        return inst.Next();
    }

    public static double NextDouble()
    {
        Random inst = _local;
        if (inst == null)
        {
            int seed;
            lock (_global) seed = _global.Next();
            _local = inst = new Random(seed);
        }
        return inst.NextDouble();
    }
}
