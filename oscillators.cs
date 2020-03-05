using Godot;
using System;

public class oscillators : Node
{
	Random random = new Random ();
	const float TAU = Mathf.Tau;

	float[] sintable;
	enum Waveforms {SINE, SAW, TRI, PULSE, ABSINE, WHITE, PINK, BROWN};
	
	PinkNumber pinkr = new PinkNumber() ; 
	float lastr = 0.0f;

	readonly static float[] TRITABLE = {0.0f, 0.2f, 0.4f, 0.6f, 0.8f, 1.0f, 0.8f, 0.6f, 0.4f, 0.2f, 0.0f, -0.2f, -0.4f,
							-0.6f, -0.8f, -1.0f, -0.8f, -0.6f, -0.4f, -0.2f};


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		gen_sin_table(2048);
	}


	public void gen_sin_table(int res) {
		sintable = new float[res];
		for(int i=0; i < res; i++){
			sintable[i] = Mathf.Sin(TAU * i / (float)(res));
		}
	}
// Grab a sine value from the lookup table
	float sint(float n){
		int sz = sintable.Length;
		int idx = (int) Mathf.Round(n/TAU * (float)sz);
		idx = idx % sz;

		return sintable[idx];
	}

// Grab a sine from the lookup table, from 0-1 instead of 0-TAU.
	float sint2(float n){
		int sz = sintable.Length;
		int idx = (int) Mathf.Round(n*sz);
		idx = idx % sz;

		return sintable[idx];
	}


	float wave(float n, Waveforms waveform = Waveforms.SINE, float duty = 0.5f){
		n %= 1.0f;

		switch(waveform){
			case Waveforms.PULSE:
				if (n >= duty) return 1f;
				return -1f;

			case Waveforms.SINE:
				return sint2(n);

			case Waveforms.ABSINE:
				return Mathf.Abs(sint2(n));

			case Waveforms.TRI:
				return TRITABLE[(int)n*20];

			case Waveforms.SAW:
				return 1.0f - (n * 2.0f);

			case Waveforms.PINK:
				return pinkr.GetNextValue();

			case Waveforms.BROWN:
				lastr += (float)random.NextDouble() * 0.2f - 0.1f;
				lastr *= 0.99f;
				return lastr;

			case Waveforms.WHITE:
				return (float)random.NextDouble() * 2.0f - 1.0f;

			default:
				return 0f;
		}

	}


//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }



}


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
  public int GetNextValue()
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
	  return sum;
	}
};
