using Godot;
using System;
using System.Collections.Generic;

public class Note : IComparable<Note>
{
    [Export]
    public float base_hz = 440.0f;  //Always fixed compared to the hz field;  used to return to normal after a pitch bend or lfo calc
	public float hz = 440.0f;  //Current frequency at any given time.
    
    public int samples = 0;  //Samples elapsed.  Sample timer. 
    public List<float> phase = new List<float>{0,0,0,0};  //Phase accumulator. This holds the sum of all previous Note.Iterate() periods. Allows smooth pitch changes.

    [Export]
    public int midi_velocity = 0; //0-127, probably
    public int midi_note; //Pitch of note.  0-127 for sure.

    public float Velocity  => midi_velocity/128.0f;  //Grab velocity as a float value 0-1.

    public bool[] delayed = {false,false,false,false};  //The state of the note's envelope phase.  Gets activated once delay is exceeded.
    public bool pressed = true;     //The current state of whether the note is on or off.
    public int releaseSample = 0;  //The sample at which noteOff was received.

    //Set by the mixer handling the channel. Mixer finds the patch being used for this Note, gets total release time, and patch determines when to release from channel.
    public float ttl = 79380000; //30 minutes by default

// Feedback relies on averaging the last 2 samples in an operator. To support polyphony, each note should store its own feedback history per-operator.
// When/if more/less operators are supported per patch, Note may need to examine patch on init to determine capacity. 
// If Patches are eventually linked to Notes, requesting samples can be moved here from AudioOutput and the Channel can request samples more directly.
    public List<float[]> feedbackHistory = new List<float[]>{new float[]{0.0f, 0.0f}, new float[]{0.0f, 0.0f},new float[]{0.0f, 0.0f},new float[]{0.0f, 0.0f}, };

//Similar to feedback, to determine running frequency for cutoff filters, a history must be kept per-operator.
    // public List<float[]> cutoffHistory = new List<float[]>{new float[]{0.0f, 0.0f}, new float[]{0.0f, 0.0f},new float[]{0.0f, 0.0f},new float[]{0.0f, 0.0f}, };

    //format:  in1, in2, out1, out2
    public List<float[]> filterHistory = new List<float[]>{new float[8], new float[8],new float[8],new float[8], };

//Similar to the above 2 values, this field smooths out the amplitude modulation when the note is attached to a patch with AMS enabled.
    public List<float> ampBuffer = new List<float>(new float[16]);


//The owner of the Note.  This could be a channel for arbitrary-n notes, or a channel which needs temporary polyphony.
    public Channel _channel; 

// 12-field array containing a LUT of semitone frequencies at all MIDI note numbers.
// Generated from center tuning (A-4) at 440hz.

// https://www.inspiredacoustics.com/en/MIDI_note_numbers_and_center_frequencies
static float[] periods = {} ;  //Size 128 +1
public const int NOTE_A4 = 69;   // Nice.


    //Constructors
    #if GODOT
        public Note() {}  //Default ctor Needed by Godot
    #endif    

    //Lookup frequency based on MIDI note number.
    public Note (int note_number, int velocity=0)
    {
        this.midi_velocity = velocity;
        this.midi_note = note_number;
        this.hz = lookup_frequency(note_number);
        this.base_hz = hz;
    }

    //Construct a note directly with the frequency specified.
    public Note(float hz, int velocity=0)
    {
        this.midi_note = -1;  //Custom sound location in channel
        this.hz = hz;
        this.base_hz = hz;
        this.midi_velocity = velocity;
    }

    public static void gen_period_table()
    {

        if (periods.Length == 0)
        {
            periods = new float[129]; // Extra field accounts for G#9

            for (int i=0; i < periods.Length; i++)
            {
                periods[i] = (float) (440.0 * Math.Pow(2.0, (i-NOTE_A4)/12.0));
            }
            // GD.Print("FMCore:  Note period table generated.");
            // foreach (double val in periods)  {GD.Print(val);}
        }    
    }

    public static float lookup_frequency(int note_number)
    {
        if (periods.Length == 0)
        {
            periods = new float[129]; // Extra field accounts for G#9
            gen_period_table();
        }
        return periods[note_number];
    }

    //Gets the phase increment based on the current pitch and sample rate.
    public float GetPhase(int idx, float sample_rate=44100.0f)
    {
        // return samples / sample_rate * hz;
        return this.phase[idx];
    }

    //Resets the phase index to 0.  Useful for syncing note phase or when delay is used.  
    public void ResetPhase(int idx)
    {
        phase[idx] = 0;
    }
    public void ResetPhase()
    {
        for(int i=0; i < phase.Count; i++){
        phase[i] = 0;
        }
    }


    /// Increments the phase accumulator, which controls the timbre of the note's overall sound.
    public void Accumulate(int idx, int numsamples, float multiplier, float sample_rate)
    {
        phase[idx] += numsamples / sample_rate * (hz*multiplier);
    }

    /// Returns what the phase would be if the accumulator was moved forward.
    public float PhaseIfAccumulated(int idx, int numsamples, float multiplier, float sample_rate)
    {
        return phase[idx] + (numsamples / sample_rate * (hz*multiplier));
    }

    //Iterates the sample timer.  
    public virtual void Iterate(int numsamples=1)
    {
        samples += numsamples;
    }


    //Determine pitch at our current sample position based on the pitch generator from the patch supplied.
    public float PitchAtSamplePosition(Patch p)
    {
        if (samples < p.par){ //Attack phase.
            return GDSFmFuncs.lerp(p.palMult, p.pdlMult,  samples / (float) p.par);

        // } else if (samples < p._padr) { //Decay phase.
        } else if ((samples >= p.par) && (samples < p._padr) ) { //Decay phase.
            return GDSFmFuncs.lerp(p.pdlMult, p.pslMult,   (samples - p.par) / p.pdr);

        } else if (!pressed && (samples-releaseSample) < p.prr){  //Release phase.
            return GDSFmFuncs.lerp(p.pslMult, p.prlMult,  (samples - releaseSample) /  p.prr);

        } else if (!pressed && (samples-releaseSample) >= p.prr){  //Post-release phase.
            return p.prlMult;

        } else {  //Sustain phase.
            return p.pslMult;
        }
    }


    //Convenience method for mixers, envelopes, whatever to check if this note is ready to die.
    public bool IsDestroyable()
    {
        return ((samples - releaseSample > ttl) && !pressed);
    }

    //Typically called by the mixer when a note runs past its release time.
    public void Destroy(){

        //Remove myself from the channel.
        if (_channel != null){
            _channel.Remove(this);
        }

        //TODO:  Find out if nulling one's self here would be threadsafe....
        _channel = null;

        // QueueFree();

    }

    public int CompareTo(Note other)
    {
        if (other == null) return 1;
        if (midi_note < other.midi_note) return -1;
        if (midi_note > other.midi_note) return 1;

        return 0;
    }


    //When this note receives a NoteOff event, it's sent release time from a Patch. MIDI preview handler can do this, or (later) patterns with patch data.
    public void _on_ReleaseNote(int pitch, float releaseTime)
    {
        if (midi_note == pitch && pressed)
        {
            releaseSample = samples;
            pressed = false;
            ttl = releaseTime;      
        }
    }

}

public struct PrecalculatedNoteReleaseData
{
    // public static Patch patch;  //Patch used to calculate the releaseTime
    public int offFrame;  //The frame in which the noteOff event is guaranteed to occur.  If <= 0, time is assumed to be infinite (manual NoteOff).
    public int releaseTime;  //The number of samples since the offFrame before we're considered nukeable.  
    public int killFrame;  //The frame when it's okay to kill off the note and remove it from a channel.
}


/// Represents a note with a precalculated time to live.
public class PrecalculatedNote : Note 
{
    public PrecalculatedNoteReleaseData predetermined;
    /// Creates a note with a predetermined length.
    public PrecalculatedNote (Patch patch, int framesBeforeNoteOff, int note_number, int velocity=0)
    {
        //Having a note with predetermined length is ideal for static playback, since the note will automatically trigger its off state,
        //Allowing parallel batching of notes, since a full buffer of frames can be requested without needing external processing of the note state.
        //The instance of nodes cannot destroy itself currently (it's not clear if this is safe) so manual destruction needs to occur after processing's finished.

        //NOTE:  Precalculating this information means if you change the patch this note references, it would need to be recalculated.
        predetermined.offFrame = framesBeforeNoteOff;
        predetermined.releaseTime = patch.GetReleaseTime(note_number);
        predetermined.killFrame = predetermined.releaseTime + predetermined.offFrame;  

        this.midi_velocity = velocity;
        this.midi_note = note_number;
        this.hz = lookup_frequency(note_number);
        this.base_hz = hz;

    }


    //Iterates the sample timer.  
    public override void Iterate(int numsamples=1)
    {
        samples += numsamples;

        //TODO:  Consider creating a subclass of PredeterminedLengthNote which overrides this method to check the stuff below, for faster operation.
        if (samples >= predetermined.offFrame)
        {
            _on_ReleaseNote(this.midi_note, predetermined.releaseTime);
        }
        
    }

}