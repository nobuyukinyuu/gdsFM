using Godot;
using System;

// New Operator class for discrete core.

public class Operator
{
    public String name;
    public int id=0;  //Operator number, used to reference itself in ordered datasets specific to operators, like Note.feedback_history
    Operator[] connections = new Operator[0];
    public Operator[] Connections { get => connections; set => connections = value; }
    bool bypass = false;

    public bool Bypass { get => bypass; set => bypass = value; }

    //Envelope generator
    Envelope eg;// = new Envelope(name);
    

    public Envelope EG { get => eg; set => eg = value; }

    // double[] old_sample = {0f,0f};  //held values for feedback processing.  Move to EG?

    //Ctor to give me a name
    public Operator(string name)
    {
        this.name = name;
        //TODO: Proper ID check. Right now we just assume operators are named "OP1" etc based on OP number.
        this.id = Int32.Parse(name.Substring(2)) -1;  
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
        
        double output = 0.0;        
        double modulator = 0.0;

        double adsr = calc_eg(note);  //Get the adsr envelope from the EG to use later.


        if (Math.Abs(adsr) < 0.0005) 
            {

                return 0;
            }  //Exit early for quiet samples to save CPU.

        note.Accumulate(id,1, eg.totalMultiplier, eg.SampleRate);

        if (bypass && connections.Length ==0){
            return 0.0;
        } else if (connections.Length > 0)  //Not a terminus.  Probably a modulator.
        {	
            
            // First, mix the parallel modulators.
            for (var i=0; i < connections.Length; i++)
            {
                modulator += connections[i].request_sample(note.phase[id],note);
            }

            modulator /= connections.Length;  //mix down to 0.0-1.0f
            
            if (bypass)  return modulator;
            
            // Now modulate.
            //Modulate the phase according to the phase technique. In most FM engines this technique is always a sine wave.
            var phaseAmt = oscillators.wave(modulator, eg.fmTechnique, eg.techDuty);
            phase += phaseAmt;
            // note.phase[id] += phaseAmt/note.hz;
            // note.Accumulate(id, phaseAmt/eg.SampleRate);


            //Apply pitch modifiers.
            // phase *= eg.totalMultiplier;
            // if (eg.FixedFreq>0)  phase /= note.hz;
            

            //Get a waveform sample from the oscillator at the current phase after all phase-modifying processing has been done.
            output = oscillators.wave(phase*eg.totalMultiplier, eg.waveform | eg._use_duty, eg.duty, eg.reflect, 128-note.midi_note);  


            //Determine the EG position and attenuate.
            output *= adsr;
            output *= eg._totalLevel;  
            
            
        } else {  // Terminus.  No further modulation required. 

            //Apply pitch modifiers.
            // phase *= eg.totalMultiplier;
            // if (eg.FixedFreq>0)  phase /= note.hz;

            // if (eg.FixedFreq>0) //Fixed frequency
            // {
            //     phase *= eg.FixedFreq;
            //     phase /= note.hz;
            // } else {  //Ratio'd frequency
            //     phase *= eg._detune;
            //     phase *= eg._freqMult * eg._coarseDetune;
            // }

            // Process feedback
            if (eg.feedback > 0)
            {
                
                var average = (note.feedbackHistory[id][0] + note.feedbackHistory[id][1]) * 0.5;
                var scaled_fb = average / Math.Pow(2, 6.0f-eg.feedback);  //maybe use powfast if I can get it to support negative numbers
                note.feedbackHistory[id][1] = note.feedbackHistory[id][0]; 
                note.feedbackHistory[id][0] = oscillators.wave(phase + scaled_fb, eg.waveform | eg._use_duty, eg.duty, eg.reflect, 120-note.midi_note) * adsr;

                output = note.feedbackHistory[id][0];

            } else {
                output = oscillators.wave(phase, eg.waveform|eg._use_duty, eg.duty, eg.reflect, 128-note.midi_note) * adsr;  //Get a waveform from the oscillator.

            }

            output *= eg._totalLevel;  //Finally, Attenuate total level.  ADSR was applied to output earlier depending on FB.
        }

        //Iterate the sample timer and phase accumulator.
        // note.Accumulate(id,1, eg.totalMultiplier, eg.SampleRate);
        return output;        
    }

    // Calculate the position and value attenuation as determined by the envelope generator.
    double calc_eg(Note note)
    {
        //TODO:  Take a sample timer, NoteOff status, and NoteOff position from an external Note resource.
        return eg.VolumeAtSamplePosition(note);
    }

}
