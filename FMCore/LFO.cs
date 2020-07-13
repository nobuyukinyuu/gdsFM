using Godot;
using System;
using System.Collections.Generic;

public class LFO : Resource //: Node
{
    // This value should typically be initialized to whatever the global sample rate is.  Require it in the ctor
    public static double sample_rate = 44100.0;
    public bool enabled = false;  //Used by Patch to determine whether it should update the timer and recalculate the phase position from an oscillator.

    public int id=0;  //LFO bank number.  FIXME:  Needed?

    private double[] buffer;


    //User-specified values
    public Waveforms waveform = Waveforms.SINE;
    public bool reflect_waveform;

    private double depth = 1.0; //The maximum amount this LFO oscillates the frequency multiplier applied to the Operator output. Always positive.
    public double cycleTime = 1;  //Length, in seconds to reach one oscillation of the LFO waveform.
    public int delay = 0;  //Delay, in samples, before the oscillation kicks in.

    //Helper properties that allow specifying cycle time and delay in millisecs.
    public double CycleTime { get =>  cycleTime * 1000; set => cycleTime = (value/1000.0); }
    public double Delay { get =>  1000 * delay / sample_rate ; set =>  delay = (int)((value/1000.0)*sample_rate); }
    public double Depth { get => depth*100; set => depth = value/100.0; }


    //Running counters
    private int samples = 0;  //Samples elapsed.  Sample timer.  Can be reset by key sync.
    public double phase = 0.0;  //Phase accumulator for this LFO.
    public double currentOscillation = 1.0;  //This value is precalculated whenever the sample timer changes.  Reference it when applying totalMultiplier to save CPU.

    //Extra Options
    public bool keySync=true;  //Affects resets on the accumulator when a new key is pressed, otherwise the oscillator continues to cycle it from previous position.
    public bool legato = true;  //TODO:  If disabled, quantizes LFO values to nearest 12th root of 2

    //Constructors
    public LFO(){}
    public LFO(double sample_rate)
    {
        LFO.sample_rate = sample_rate;
    }



    public void UpdateBuffer(int length) {buffer = RequestBuffer(length);}
    public double[] RequestBuffer(int length)
    {
        var output = new double[length];

        bool delayed;  //Check if we need for delay to expire
        for (int i=0; i < length; i++)
        {
            if (!enabled) {output[i] = 1.0; continue;}
                else {output[i] = Calc(out delayed);}

            Iterate();
            if (!delayed) Accumulate();  //Don't update the phase accumulator until delay has passed.  This allows for oscillator sync.
        }

        return output;
    }

    public virtual void Reset()
    {
        samples=0;
        if (keySync)  phase = 0.0;
    }

    //Calculates a multiplier at the current LFO's sample position.  Used for Operators' pitch modulation sensitivity.
    public virtual double GetPitchMult(int noteSamples, double sensitivity=1.0)
    {
        if (samples < delay)
        {
            return 1.0;  //No adjustment to the LFO multiplier.  Wait until delay's passed.
        } else {
            //First, apply the sensitivity to the output before converting it to a pitch multiplier.
            //TODO:  Should depth magnitude also be applied here?  
            var output = depth * currentOscillation * sensitivity ;
            //Negative number output from the oscillator during precalc should be converted to its reciprocal to represent a lower change in pitch.
            if (output < 0)  {
                output =  1 / Math.Abs(1-output); 
            } else { output += 1; }
            return output;  
        }
    }

    //Calculates a multiplier at the specified buffer position.  Used for Operators' pitch modulation sensitivity.
    public virtual double GetPitchMult(int idx, int noteSamples, double sensitivity=1.0)
    {
        if (noteSamples < delay)  //Determine if the note is mature enough to have LFO applied.
        {
            return 1.0;  //No adjustment to the LFO multiplier.  Wait until delay's passed.
        } else {
            var osc = buffer[idx];
            var output = depth * osc * sensitivity ;

            //Negative number output from the oscillator during precalc should be converted to its reciprocal to represent a lower change in pitch.
            if (output < 0)  {
                output =  1 / Math.Abs(1-output); 
            } else { output += 1; }
            return output;  
        }
    }

    //Calculates a multiplier at the current LFO's sample position.  Used for Operators' total amplitude modulation sensitivity.
    public virtual double GetAmpMult(int noteSamples, double sensitivity=1.0)
    {
        if (noteSamples < delay)
        {
            return 1.0;  //No adjustment to the LFO multiplier.  Wait until delay's passed.
        } else {
            //The minimum level of amplitude is determined by the sensitivity.  This will be added to the output; the final value will be normalized.
            //The result is that a sensitivity of 0 always keeps output TL at max, and a full sensitivity of 1 will oscillate output from 0-1.            
            var output = (currentOscillation * sensitivity) + (1.0-sensitivity);

            //Output can range from -1 to 1 at this point, so scale it to the proper level for amplitude output.
            return (output+1) * 0.5;
        }
    }
    //Calculates a multiplier at the specified buffer position.  Used for Operators' total amplitude modulation sensitivity.
    public virtual double GetAmpMult(int idx, int noteSamples, double sensitivity=1.0)
    {
        if (noteSamples < delay)
        {
            return 1.0;  //No adjustment to the LFO multiplier.  Wait until delay's passed.
        } else {
            //The minimum level of amplitude is determined by the sensitivity.  This will be added to the output; the final value will be normalized.
            //The result is that a sensitivity of 0 always keeps output TL at max, and a full sensitivity of 1 will oscillate output from 0-1.            
            var output = (buffer[idx] * sensitivity) + (1.0-sensitivity);

            //Output can range from -1 to 1 at this point, so scale it to the proper level for amplitude output.
            return (output+1) * 0.5;
        }
    }


    //Gets the oscillator at current phase.
    public double Calc(out bool delayed)
    {
        if (samples < delay)
        {
            delayed = true;
            return 0.0;
        } else {
            delayed = false;
            var output = oscillators.wave(phase, waveform, reflect: reflect_waveform);
            return output;
        }
    }


    //Recalculates the current level of the LFO.  This should be called whenever sample timer updates.
    public void Recalc()
    {
        currentOscillation = Calc(out bool delayed);
    }

    //Is any multiplier != 1.0 necessary?  FIXME later
    public void Accumulate(int numsamples=1, double multiplier=1)
    {

        phase += numsamples / sample_rate / cycleTime ;
        // phase += numsamples / sample_rate * (cycleTime*multiplier);
    }

    //Called by the master clock, typically Patch when it wants to update its LFOs during a buffer fill.
    public void Iterate(int numsamples=1)
    {
        samples += numsamples;
    }

}

//DummyLFO class used to be below but was deleted because complexity of virtual methods might erase any advantage over 1 or 2 branch statements used to check LFO bank.