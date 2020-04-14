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

    //Envelope generator
    Envelope eg;// = new Envelope(name);
    

    public Envelope EG { get => eg; set => eg = value; }

    double[] old_sample = {0f,0f};  //held values for feedback processing.  Move to EG?

    //Ctor to give me a name
    public Operator(string name)
    {
        this.name = name;
        eg = new Envelope(name);
    }

    //  //    Fills a buffer of samples.  Do this in the mixer or something.
    // public double[] FillBuffer(double[] phase)
    // {

    // }

    //Iterate over our connections, then mix and modulate them before returning the final modulated value.
    //TODO:  Replace samplePos with proper argument for a Note class which can include NoteOff state, sample position at release time, etc.
    public double request_sample(double phase, Note note)
    {
        
        double output = 0.0f;
        
        // TODO:  Don't sin-modulate phase at first, add phases together for each
        //		  Parallel modulator, THEN call modulate on sample before passing back.
        
        double modulator = 0.0f;

        if (connections.Length > 0)  // We're a Modulator.
        {	
            
            // First, mix the parallel modulators.

            foreach (Operator o in connections){
                modulator += o.request_sample(phase, note);
            }

            modulator /= connections.Length;  //mix down to 0.0-1.0f
            
            if (bypass)  return modulator;
            
            // Now modulate.
            phase += modulator;
            phase *= eg._detune;
            phase *= eg._freqMult;  
            // phase = (oscillators.sint2(phase) + 1.0f) / 2.0f;  //TODO:  Seperate field for modulation waveform.  Cool new sounds!
            phase = (oscillators.wave(phase, eg.fmTechnique, eg.duty) + 1.0f) / 2.0f;  
            
            output = oscillators.wave(phase, eg.waveform, eg.duty, eg.reflect);  //Get a waveform from the oscillator.


            //Determine the EG position and attenuate.
            output *= calc_eg(note);


            output *= eg._totalLevel;  //Finally, Attenuate total level.
            
            
        } else {  // Terminus.  No further modulation required.  This is a carrier.

            if (bypass)  return 0.0;
            phase *= eg._detune;
            phase *= eg._freqMult;


            //Determine the EG position and attenuate.
            double asdr = calc_eg(note);

            // Process feedback
            // TODO:  Oxe feedback is pre-envelope, just like this code.  DX is supposedly post-envelope. Check and determine if FB is more useful there.
            if (eg.feedback > 0)
            {
                
                var average = (old_sample[0] + old_sample[1]) * 0.5;
                var scaled_fb = average / Math.Pow(2, 6.0f-eg.feedback);  //maybe use powfast if I can get it to support negative numbers
                old_sample[1] = old_sample[0];
                old_sample[0] = oscillators.wave(phase + scaled_fb, eg.waveform, eg.duty, eg.reflect) * asdr;

                output = old_sample[0];
            
            } else {
                output = oscillators.wave(phase, eg.waveform, eg.duty, eg.reflect) * asdr;  //Get a waveform from the oscillator.

            }




            output *= eg._totalLevel;  //Finally, Attenuate total level.
        }
        return output;        
    }

    // Calculate the position and value attenuation as determined by the envelope generator.
    double calc_eg(Note note)
    {
        //TODO:  Take a sample timer, NoteOff status, and NoteOff position from an external Note resource.
        return eg.VolumeAtSamplePosition(note);
    }

}
