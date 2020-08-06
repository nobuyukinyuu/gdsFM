using Godot;
using System;
using System.Collections.Generic;

// New Operator class for discrete core.

public class Operator : Resource
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


    //LFO stuff.
    //Banks are used to determine which bank this operator should access for its PMD and AMD.  If -1, the operator will not apply LFO.
    public int lfoBankAmp = -1;
    public int lfoBankPitch = -1;
    //Sensitivity variables for amplitude and pitch.  These are normalized from 0-1.
    public double pms, ams;

    public RTable<double> customWaveform;  //Waveform oscillator used when eg.Waveform == Waveforms.CUSTOM
    public int waveformBank = -1; //Used to assist in finding where the customWaveform lives inside the associated Patch.

    /// Ctor to give me a name
    public Operator(string name)
    {
        this.name = name;
        //TODO: Proper ID check. Right now we just assume operators are named "OP1" etc based on OP number.
        this.id = Int32.Parse(name.Substring(2)) -1;  
        eg = new Envelope(this.id);
    }

    /// Default ctor.  Don't use this.  Only for godot editor
    public Operator(){}


    /// Iterate over our connections and return info about them.
    public string ConnectionInfo()
    {
        var sb = new System.Text.StringBuilder(name);
        var once=true;

        var amt = connections.Length;

        if (amt > 1)
        {
            sb.Append(" <= (");
            while (amt > 1)
            {    
                var op=connections[amt-1];
            
                // if (once) {sb.Append(" <= ("); once=false;}
                sb.Append(op.ConnectionInfo()).Append(" + ");
                amt -= 1;
            } 
            sb.Append(connections[0].ConnectionInfo());
            sb.Append(")");

        } else {
            foreach (Operator op in connections)
            {
                if (once) {sb.Append("<="); once=false;}
                sb.Append(op.ConnectionInfo());
            }
        }
        return sb.ToString();
    }

    /// Iterate over our connections, then mix and modulate them before returning the final modulated value.
    public double request_sample(double phase, Note note, List<LFO> LFOs = null, int lfoBufPos = 0)
    {
        
        double output = 0.0;        
        double modulator = 0.0;

        double adsr = calc_eg(note);  //Get the adsr envelope from the EG to use later.


        if (Math.Abs(adsr) < 0.0005) 
            {
                goto Finalize;
            }  //Exit early for quiet samples to save CPU.


        if (bypass && connections.Length ==0){
            goto Finalize;  //No output
        } else if (connections.Length > 0)  //Not a terminus.  Probably a modulator.
        {	
            
            // First, mix the parallel modulators.
            for (var i=0; i < connections.Length; i++)
            {
                modulator += connections[i].request_sample(phase,note, LFOs, lfoBufPos);
            }

            modulator /= connections.Length;  //mix down to 0.0-1.0f.   Is this correct?
            
            if (bypass)  return modulator;
            
            // Now modulate.
            //Modulate the phase according to the phase technique. In most FM engines this technique is always a sine wave.
            var phaseAmt = oscillators.wave(modulator, eg.fmTechnique, eg.techDuty);
            phase += phaseAmt;
            

            //Get a waveform sample from the oscillator at the current phase after all phase-modifying processing has been done.
            if (eg.waveform== Waveforms.CUSTOM){
                output = oscillators.wave(phase, customWaveform);  
            } else {
                output = oscillators.wave(phase, eg.waveform | eg._use_duty, eg.duty, eg.reflect, 128-note.midi_note);  
            }


            //Apply Amplitude LFO here.  Grab the calculation from the bank associated with this Operator and multiply it with eg._totalLevel.
            output *= (lfoBankAmp>=0 && LFOs != null) ? LFOs[lfoBankAmp].GetAmpMult(lfoBufPos, note.ampBuffer, ams) : 1.0;

            //Determine the EG position and attenuate.
            output *= adsr;
            output *= eg._totalLevel;  

            
        } else {  // Terminus.  No further modulation required. 

            // Process feedback
            if (eg.feedback > 0)
            {
                
                var average = (note.feedbackHistory[id][0] + note.feedbackHistory[id][1]) * 0.5;
                var scaled_fb = average / Math.Pow(2, 6.0f-eg.feedback);  //maybe use powfast if I can get it to support negative numbers

                double wave;
                if (eg.waveform== Waveforms.CUSTOM){
                    wave = oscillators.wave(note.phase[id] + scaled_fb, customWaveform) * adsr;  
                } else {
                    wave = oscillators.wave(note.phase[id] + scaled_fb, eg.waveform | eg._use_duty, eg.duty, eg.reflect, 128-note.midi_note) * adsr;
                }

                note.feedbackHistory[id][1] = note.feedbackHistory[id][0]; 
                // note.feedbackHistory[id][0] = oscillators.wave(note.phase[id] + scaled_fb, eg.waveform | eg._use_duty, eg.duty, eg.reflect, 128-note.midi_note) * adsr;
                note.feedbackHistory[id][0] = wave;

                output = note.feedbackHistory[id][0];

            } else {
                if (eg.waveform== Waveforms.CUSTOM){
                    output = oscillators.wave(note.phase[id], customWaveform) * adsr;  
                } else {
                    //Get a waveform from the oscillator.
                    output = oscillators.wave(note.phase[id], eg.waveform|eg._use_duty, eg.duty, eg.reflect, 128-note.midi_note) * adsr;  
                }

            }

            //Apply Amplitude LFO here.  Grab the calculation from the bank associated with this Operator and multiply it with eg._totalLevel.
            output *= (lfoBankAmp>=0 && LFOs != null) ? LFOs[lfoBankAmp].GetAmpMult(lfoBufPos, note.ampBuffer, ams) : 1.0;

            output *= eg._totalLevel;  //Finally, Attenuate total level.  ADSR was applied to output earlier depending on FB.
        }

        //Iterate the phase accumulator.
        Finalize: 
          //Apply Pitch LFO here.  Grab the calculation from the bank associated with this Operator and multiply it with eg.totalMultiplier.
          var lfoMult = (lfoBankPitch>=0 && LFOs != null) ? LFOs[lfoBankPitch].GetPitchMult(lfoBufPos, note.samples, pms) : 1.0;
          note.Accumulate(id,1, eg.FixedFreq>0? eg.totalMultiplier/note.hz * lfoMult: eg.totalMultiplier * lfoMult, eg.SampleRate);

          //Apply the filter.
          if (eg.filter.enabled) output = GDSFmLowPass.Filter(output, eg.filter, ref note.cutoffHistory[id][0], ref note.cutoffHistory[id][1]);
          return output;        
    }

    /// Calculate the position and value attenuation as determined by the envelope generator.
    double calc_eg(Note note)
    {
        //TODO:  Take a sample timer, NoteOff status, and NoteOff position from an external Note resource.
        return eg.VolumeAtSamplePosition(note);
    }

}
