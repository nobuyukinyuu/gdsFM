using Godot;
using System;

// New Operator class for discrete core.

public class Operator
{
    public String name;
    Operator[] connections = new Operator[0];
    public Operator[] Connections { get => connections; set => connections = value; }
    bool bypass = false;

    public bool Bypass { get => bypass; set => bypass = value; }

    Envelope eg = new Envelope();
    double[] old_sample = {0f,0f};  //held values for feedback processing.  Move to EG?

    //Ctor to give me a name
    public Operator(string name)
    {
        this.name = name;
    }

    //  //    Fills a buffer of samples.  Do this in the mixer or something.
    // public double[] FillBuffer(double[] phase)
    // {

    // }

    //Iterate over our connections, then mix and modulate them before returning the final modulated value.
    public double request_sample(double phase)
    {
        
        double output = 0.0f;
        
        // TODO:  Don't sin-modulate phase at first, add phases together for each
        //		  Parallel modulator, THEN call modulate on sample before passing back.
        
        double modulator = 0.0f;

        if (connections.Length > 0)
        {	
            
            // First, mix the parallel modulators.

            foreach (Operator o in connections){
                modulator += o.request_sample(phase);
            }

            modulator /= connections.Length;
            
            if (bypass)  return modulator;
            
            // Now modulate.
            phase += modulator;
            phase *= eg._detune;
            phase *= eg._freqMult;  
            phase = (oscillators.sint2(phase) + 1.0f) / 2.0f;
            
            output = oscillators.wave(phase, eg.waveform);  
            output *= eg._totalLevel;
            
            
        } else {  // Terminus.  No further modulation required.

            if (bypass)  return 0.0f;
            phase *= eg._detune;
            phase *= eg._freqMult;

            output = oscillators.wave(phase, eg.waveform); //* (1+ eg.dt/500.0) 
            
            // Process feedback
            if (eg.feedback > 0)
            {
                var average = (old_sample[0] + old_sample[1]) * 0.5;
                var scaled_fb = average / Math.Pow(2, 6.0f-eg.feedback);
                old_sample[1] = old_sample[0];
                old_sample[0] = oscillators.wave(phase + scaled_fb, eg.waveform);

                output = old_sample[0];
            }

            output *= eg._totalLevel;
        }
        return output;        
    }

}
