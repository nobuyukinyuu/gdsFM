using Godot;
using System;
using System.Collections.Generic;
using GdsFMJson;

// New Operator class for discrete core.

public class Operator : Resource
{
    public const string _iotype = "operator";
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
            sb.Append(" < (");
            while (amt > 1)
            {    
                var op=connections[amt-1];
            
                // if (once) {sb.Append(" <= ("); once=false;}
                sb.Append(op.ConnectionInfo()).Append(", ");
                amt -= 1;
            } 
            sb.Append(connections[0].ConnectionInfo());
            sb.Append(")");

        } else {
            foreach (Operator op in connections)
            {
                if (once) {sb.Append(" } "); once=false;}
                sb.Append(op.ConnectionInfo());
            }
        }
        return sb.ToString();
    }


    public double LastChain;   //////////////////DEBUG, REMOVE ME
    /// Iterate over our connections, then mix and modulate them before returning the final modulated value.
    public double request_sample(Note note, List<LFO> LFOs = null, int lfoBufPos = 0)
    {
        
        double output = 0.0;        
        double modulator = 0.0;

        double adsr = calc_eg(note);  //Get the adsr envelope from the EG to use later.
        double phase = note.phase[id];

        if (Math.Abs(adsr) < 0.0005) 
            {
                goto Finalize;
            }  //Exit early for quiet samples to save CPU.


        if (bypass && connections.Length ==0){
            goto Finalize;  //No output.
        } else if (connections.Length > 0)  //Not a terminus.  Probably a modulator.
        {	

            // First, mix the parallel modulators.
            for (var i=0; i < connections.Length; i++)
            {
                modulator += connections[i].request_sample(note, LFOs, lfoBufPos);
            }

            modulator /= connections.Length;  //mix down to 0.0-1.0f.   Is this correct?
            
            if (bypass)  { return modulator; }


            // Now modulate.
            //Modulate the phase according to the phase technique. In most FM engines this technique is always a sine wave.
            var phaseAmt = oscillators.wave(modulator, eg.fmTechnique, eg.techDuty);
            phase += phaseAmt; 
            

            //Get a waveform sample from the oscillator at the current phase after all phase-modifying processing has been done.
            if (eg.waveform== Waveforms.CUSTOM){
                output = oscillators.wave(phase, customWaveform);  
            } else {
                output = oscillators.wave(phase, eg.waveform | eg._use_duty, eg.duty, eg.reflect, note.midi_note);  
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
                    wave = oscillators.wave(phase + scaled_fb, customWaveform) * adsr;  
                } else {
                    wave = oscillators.wave(phase + scaled_fb, eg.waveform | eg._use_duty, eg.duty, eg.reflect, note.midi_note) * adsr;
                }

                note.feedbackHistory[id][1] = note.feedbackHistory[id][0]; 
                note.feedbackHistory[id][0] = wave;

                output = note.feedbackHistory[id][0];

            } else {  //No feedback
                if (eg.waveform== Waveforms.CUSTOM){
                    output = oscillators.wave(phase, customWaveform) * adsr;  
                } else {
                    //Get a waveform from the oscillator.
                    output = oscillators.wave(phase, eg.waveform|eg._use_duty, eg.duty, eg.reflect, note.midi_note) * adsr;  
                }

            }

            //Apply Amplitude LFO here.  Grab the calculation from the bank associated with this Operator and multiply it with eg._totalLevel.
            output *= (lfoBankAmp>=0 && LFOs != null) ? LFOs[lfoBankAmp].GetAmpMult(lfoBufPos, note.ampBuffer, ams) : 1.0;

            output *= eg._totalLevel;  //Finally, Attenuate total level.  ADSR was applied to output earlier depending on FB.
        }

        Finalize: 
          //Apply the filter.
          if (eg.filter.Enabled) output = GDSFmLowPass.Filter(output, eg.filter, ref note.cutoffHistory[id][0], ref note.cutoffHistory[id][1]);
          return output;        
    }



    /// Gets the total EG + LFO multiplier for the operator at the given position in the audio buffer.  Used to accumulate phase for an Operator.
    public double GetMult(Note note, List<LFO> LFOs = null, int lfoBufPos = 0)
    {
        var lfoMult = (lfoBankPitch>=0 && LFOs != null) ? LFOs[lfoBankPitch].GetPitchMult(lfoBufPos, note.samples, pms) : 1.0;
        var mult = eg.FixedFreq>0? eg.totalMultiplier * lfoMult / note.hz: eg.totalMultiplier * lfoMult;
        return mult;
    }

    /// Calculate the position and value attenuation as determined by the envelope generator.
    double calc_eg(Note note)
    {
        //TODO:  Take a sample timer, NoteOff status, and NoteOff position from an external Note resource.
        return eg.VolumeAtSamplePosition(note);
    }

#region IO
    public JSONObject JsonMetadata(OPCopyFlags flags= OPCopyFlags.ALL, EGCopyFlags egCopyFlags=EGCopyFlags.ALL)
    {
        //Begin constructing a new json object.
        JSONObject p = new JSONObject();

        //Stuff that you probably wouldn't want to copy over from one operator to another
        //IE., Stuff you only want if you want to save to disk.  (OPCopyFlags.HEADERS)
        if (flags.HasFlag(OPCopyFlags.NAME)){   p.AddPrim("name", name);     }
        if (flags.HasFlag(OPCopyFlags.ID)){     p.AddPrim("id", id);         }
        if (flags.HasFlag(OPCopyFlags.BYPASS)){ p.AddPrim("bypass", bypass); }

        //Everything else
        if (flags.HasFlag(OPCopyFlags.EG))      p.AddItem ("eg", eg.JsonMetadata(egCopyFlags));

        if (flags.HasFlag(OPCopyFlags.LFO))
        {
            p.AddPrim("lfoBankAmp", lfoBankAmp);
            p.AddPrim("lfoBankPitch", lfoBankPitch);
            p.AddPrim("ams", ams);
            p.AddPrim("pms", pms);
        }
        
        //TODO:  waveform bank stuf
        //      Do we need to copy the actual waveform in the bank?  Might not be needed for copy, but then again pasting operators 
        //      between patches like this might wanna preserve it....
        if (flags.HasFlag(OPCopyFlags.WAVEFORM_BANK))
        {
            p.AddPrim("waveformBank", waveformBank);
        }

        return p;
    }

    //Probably being used for clipboard reasons.  Omit header and inject iotype.
    public override string ToString()
    {
        var output = JsonMetadata(OPCopyFlags.ALL & ~OPCopyFlags.HEADERS);  //Get meta but without headers.
        output.AddPrim("_iotype", _iotype);
        return output.ToJSONString(); 
    }

    /// Attempts to load a JSON string into this operator.!--
    public IOError FromString(string input, bool ignoreIOtype)
    {
        var p = JSONData.ReadJSON(input);
        if (p is JSONDataError) return IOError.JSON_MALFORMED;  // JSON malformed.  Exit early.
        
        var j = (JSONObject) p;
        // var ver = j.GetItem("_version", -1);
        if (!ignoreIOtype && (j.GetItem("_iotype", "") != _iotype)) return IOError.INCORRECT_IOTYPE;  //Incorrect iotype.  Exit early.

        //If we got this far, the data is probably okay.  Let's try parsing it.......
        // var idk = new List<string>();
        try
        {
            //Globals
            j.Assign("name", ref name);
            j.Assign("id", ref id);
            j.Assign("bypass", ref bypass);

            j.Assign("lfoBankAmp", ref lfoBankAmp);
            j.Assign("lfoBankPitch", ref lfoBankPitch);
            j.Assign("ams", ref ams);
            j.Assign("pms", ref pms);

            j.Assign("waveformBank", ref waveformBank);

            //Envelopes
            var egData = j.HasItem("eg")? j.GetItem("eg") : null;
            if (egData != null)
            {
                //Tell us how many has parsed
                var err = eg.FromString(egData.ToJSONString(), ignoreIOtype);
                // GD.Print ( err );
            }
            

        } catch {
            return IOError.JSON_MALFORMED;
        }

        return IOError.OK;
    }
    public IOError FromString(string input) { return FromString(input, false); }

#endregion

}
