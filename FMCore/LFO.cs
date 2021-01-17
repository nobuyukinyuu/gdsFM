using Godot;
using System;
using System.Collections.Generic;
using GdsFMJson;

public class LFO : Resource //: Node
{
    public const string _iotype = "lfo";
    // This value should typically be initialized to whatever the global sample rate is.  Require it in the ctor
    public static float sample_rate = 44100.0f;
    public bool enabled = false;  //Used by Patch to determine whether it should update the timer and recalculate the phase position from an oscillator.

    public int id=0;  //LFO bank number.  FIXME:  Needed?

    private float[] buffer=new float[1];


    //User-specified values
    public Waveforms waveform = Waveforms.SINE;
    public bool reflect_waveform;

    private float depth = 1.0f; //The maximum amount this LFO oscillates the frequency multiplier applied to the Operator output. Always positive.
    public float cycleTime = 1;  //Length, in seconds to reach one oscillation of the LFO waveform.
    public int delay = 0;  //Delay, in samples, before the oscillation kicks in.

    public float bias = 0;  //DC offset bias

    //Helper properties that allow specifying cycle time and delay in millisecs.
    public float CycleTime { get =>  cycleTime * 1000; set => cycleTime = (value/1000.0f); }
    public float Delay { get =>  1000 * delay / sample_rate ; set =>  delay = (int)((value/1000.0f)*sample_rate); }
    public float Depth { get => depth*100; set => depth = value/100.0f; }
    public float Bias { get => bias*100; set => bias = value/100.0f; }


    //Running counters
    private int samples = 0;  //Samples elapsed.  Sample timer.  Can be reset by key sync.
    public float phase = 0.0f;  //Phase accumulator for this LFO.
    public float currentOscillation = 1.0f;  //This value is precalculated whenever the sample timer changes.  Reference it when applying totalMultiplier to save CPU.

    //Extra Options
    public bool keySync=true;  //Affects resets on the sample timer when a new key is pressed; if disabled, delay is only used to affect an LFO's phase offset.
    public bool oscSync=true;  //Affects resets on the accumulator when a new key is pressed, otherwise the oscillator continues to cycle it from previous position.
    public bool legato = true;  //If disabled, quantizes LFO values to nearest 12th root of 2

    // public static readonly float[] NEAREST_12 = new float[] //Nearest power of 12, used for quantizing LFO when discrete note (no legato) mode is enabled
    //         {0, 0.059463, 0.122462, 0.189207, 0.259921, 0.33484, 0.414214, 0.498307, 0.587401, 0.681793, 0.781797, 0.887749, 1.0};

    #region IO
        /// Typically used to get output for clipboard data.
        public override string ToString()
        {
            var output = JsonMetadata();
            output.AddPrim ("_iotype", _iotype);
            return output.ToJSONString();
        }

        /// Typically used to fetch an object for IO.  For clipboard type output, use ToString().
        public JSONObject JsonMetadata()
        {
            var output = new JSONObject();
            output.AddPrim("enabled", enabled);
            output.AddPrim("waveform", (int) waveform);
            output.AddPrim("reflect_waveform", reflect_waveform);

            output.AddPrim("depth", depth);
            output.AddPrim("cycleTime", cycleTime);
            output.AddPrim("delay", delay);
            output.AddPrim("bias", bias);

            output.AddPrim("keySync", keySync);
            output.AddPrim("oscSync", oscSync);
            output.AddPrim("legato", legato);

            return output;
        }

        public int FromString(string input, bool ignoreIOtype=false)
        {
            var p = JSONData.ReadJSON(input);
            if (p is JSONDataError) return -1;  // JSON malformed.  Exit early.
            
            var j = (JSONObject) p;
            // var ver = j.GetItem("_version", -1);
            if (!ignoreIOtype && (j.GetItem("_iotype", "") != _iotype)) return -3;  //Incorrect iotype.  Exit early.

            //If we got this far, the data is probably okay.  Let's try parsing it.......
            return ParseJson(j);
        }

        public int ParseJson(JSONObject input)
        {
            try{
                input.Assign("enabled", ref enabled);
                if (input.HasItem("waveform")) 
                    waveform = (Waveforms) input.GetItem("waveform", (int) waveform);
                input.Assign("reflect_waveform", ref reflect_waveform);

                input.Assign("depth", ref depth);
                input.Assign("cycleTime", ref cycleTime);
                input.Assign("delay", ref delay);
                input.Assign("bias", ref bias);

                input.Assign("keySync", ref keySync);
                input.Assign("oscSync", ref oscSync);
                input.Assign("legato", ref legato);
            } catch {
                return -1;
            }

            return 0;
        }
    #endregion

    //Constructors
    public LFO(){}
    public LFO(float sample_rate)
    {
        LFO.sample_rate = sample_rate;
    }


    /// Sets this LFO's buffer to the next chunk of frames needed by the audio buffer filling algorithm. Each frame is used as a reference by operators.
    public void UpdateBuffer(int length) {buffer = RequestBuffer(length);}
    public float[] RequestBuffer(int length)
    {
        var output = new float[length];

        bool delayed;  //Check if we need for delay to expire
        for (int i=0; i < length; i++)
        {
            if (!enabled) {output[i] = 1.0f; continue;}
                else {output[i] = Calc(out delayed);}

            Iterate();
            if (!delayed) Accumulate();  //Don't update the phase accumulator until delay has passed.  This allows for oscillator sync.
        }

        return output;
    }

    public virtual void Reset(bool force = false)
    {
        if (keySync || force)  samples=0;
        if (oscSync || force){ phase = 0.0f;  
            if (!keySync)  Accumulate(delay); } //When there's oscillator sync but no key sync, presume the delay specifies a phase offset.
        
    }

    /// Calculates a multiplier at the current LFO's sample position.  Used for Operators' pitch modulation sensitivity.
    public virtual float GetPitchMult(int noteSamples, float sensitivity=1.0f)
    {
        if (samples < delay)
        {
            return 1.0f;  //No adjustment to the LFO multiplier.  Wait until delay's passed.
        } else {
            //First, apply the sensitivity to the output before converting it to a pitch multiplier.
            //TODO:  Should depth magnitude also be applied here?  
            var output = depth * currentOscillation * sensitivity ;
            if (!legato) output = NearestSemitone(output);

            //Negative number output from the oscillator during precalc should be converted to its reciprocal to represent a lower change in pitch.
            // if (output < 0)  {
            //     output =  1 / Math.Abs(1-output); 
            // } else { output += 1; }

            output = (float) Math.Pow(2, output);

            return output+bias;  
        }
    }

    /// Calculates a multiplier at the specified buffer position.  Used for Operators' pitch modulation sensitivity.
    public virtual float GetPitchMult(int idx, int noteSamples, float sensitivity=1.0f)
    {
        if (samples < delay)  //Determine if the note is mature enough to have LFO applied.
        {
            return 1.0f;  //No adjustment to the LFO multiplier.  Wait until delay's passed.
        } else {
            var osc = buffer[idx];
            var output = depth * osc * sensitivity ;
            if (!legato) output = NearestSemitone(output);

            //Negative number output from the oscillator during precalc should be converted to its reciprocal to represent a lower change in pitch.
            // if (output < 0)  {
            //     output =  1 / Math.Abs(1-output); 
            // } else { output += 1; }

            output = (float) Math.Pow(2, output);

            return output+bias;  
        }
    }


    /// Calculates a multiplier at the current LFO's sample position.  Used for Operators' total amplitude modulation sensitivity.
    public virtual float GetAmpMult(int noteSamples, float sensitivity=1.0f)
    {
        if (samples < delay)
        {
            return 1.0f;  //No adjustment to the LFO multiplier.  Wait until delay's passed.
        } else {
            //The minimum level of amplitude is determined by the sensitivity.  This will be added to the output; the final value will be normalized.
            //The result is that a sensitivity of 0 always keeps output TL at max, and a full sensitivity of 1 will oscillate output from 0-1.            
            var output = (currentOscillation * sensitivity) + (1.0f-sensitivity);

            //Output can range from -1 to 1 at this point, so scale it to the proper level for amplitude output.
            return (output+1) * 0.5f;
        }
    }
    /// Calculates a multiplier at the specified buffer position.  Used for Operators' total amplitude modulation sensitivity.
    public virtual float GetAmpMult(int idx, List<float> ampBuffer, float sensitivity=1.0f)
    {
        float output;
        if (samples < delay)
        {
            output = 1.0f;  //No adjustment to the LFO multiplier.  Wait until delay's passed.
        } else {
            //The minimum level of amplitude is determined by the sensitivity.  This will be added to the output; the final value will be normalized.
            //The result is that a sensitivity of 0 always keeps output TL at max, and a full sensitivity of 1 will oscillate output from 0-1.            
            output = (buffer[idx] * sensitivity) + (1.0f-sensitivity);
            output = (output +1) * 0.5f;
        }
            var average = 0.0f;
            for (int i=0; i < ampBuffer.Count; i++){
                average += ampBuffer[i];
            }   average /= ampBuffer.Count;

            ampBuffer.Add(output);
            ampBuffer.RemoveAt(0);

            //Output can range from -1 to 1 at this point, so scale it to the proper level for amplitude output.
            return average;
    }
    // double[] ampBuffer = new double[3];


    /// Gets the oscillator at current phase.
    public float Calc(out bool delayed)
    {
        if (samples < delay)
        {
            delayed = true;
            return 0.0f;
        } else {
            delayed = false;
            var output = oscillators.wave(phase, waveform, reflect: reflect_waveform);
            return output;
        }
    }


    /// Recalculates the current level of the LFO.  This should be called whenever sample timer updates.
    public void Recalc()
    {
        currentOscillation = Calc(out bool delayed);
    }

    //Is any multiplier != 1.0 necessary?  FIXME later
    public void Accumulate(int numsamples=1, float multiplier=1)
    {

        phase += numsamples / sample_rate / cycleTime ;
        // phase += numsamples / sample_rate * (cycleTime*multiplier);
    }

    /// Called by the master clock, typically Patch when it wants to update its LFOs during a buffer fill.
    public void Iterate(int numsamples=1)
    {
        samples += numsamples;
    }

    /// Retreives the nearest semitone to a given input position (-1.0 to 1.0)
    static float NearestSemitone(double input)
    {
        // var sign = Math.Sign(input);
        // var whole = Math.Truncate(input);
        // var frac = Math.Abs(input - whole);

        // var idx = (int) (frac * (NEAREST_12.Length-double.Epsilon));
        // return (NEAREST_12[idx] + (whole>1? whole:0)) * sign ;

        return (float) Math.Round(input*12)/12;

    }
}

//DummyLFO class used to be below but was deleted because complexity of virtual methods might erase any advantage over 1 or 2 branch statements used to check LFO bank.