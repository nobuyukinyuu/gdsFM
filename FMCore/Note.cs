using Godot;
using System;
using System.Collections.Generic;

public class Note : Node
{
    [Export]
	public double hz = 440.0;
    [Export]
    public int samples = 0;  //Samples elapsed.  Sample timer.  TODO:  Separate phase timer from envelope timer???? (Currently synced)
    [Export]
    public int midi_velocity = 0; //0-127, probably

    public double Velocity  => midi_velocity/128.0;

    public bool pressed = true;
    public int releaseSample = 0;  //The sample at which noteOff was received.


//The owner of the Note.  This could be a channel for arbitrary-n notes, or a channel which needs temporary polyphony.
    public List<Note> _channel; 

// 12-field array containing a LUT of semitone frequencies at all MIDI note numbers.
// Generated from center tuning (A-4) at 440hz.

// https://www.inspiredacoustics.com/en/MIDI_note_numbers_and_center_frequencies
static double[] periods = {} ;  //Size 128 +1
const int NOTE_A4 = 69;   // Nice.


    //Constructors
    #if GODOT
        public Note() {}  //Default ctor Needed by Godot
    #endif    

    //Lookup frequency based on MIDI note number.
    public Note (int note_number, int velocity=0)
    {
        this.hz = lookup_frequency(note_number);
        this.midi_velocity = velocity;
    }

    //Construct a note directly with the frequency specified.
    public Note(double hz, int velocity=0)
    {
        this.hz = hz;
        this.midi_velocity = velocity;
    }

    public override void _Ready()
    {
        if (periods.Length == 0)
        {
            periods = new double[129]; // Extra field accounts for G#9
            gen_period_table();
        }
    }

    public static void gen_period_table()
    {
        for (int i=0; i < periods.Length; i++)
        {
            periods[i] = 440.0 * Math.Pow(2.0, (i-NOTE_A4)/12.0);
        }
        // GD.Print("FMCore:  Note period table generated.");
        // foreach (double val in periods)  {GD.Print(val);}
    
    }

    public static double lookup_frequency(int note_number)
    {
        if (periods.Length == 0)
        {
            periods = new double[129]; // Extra field accounts for G#9
            gen_period_table();
        }
        return periods[note_number];
    }

    //Gets the phase increment based on the current pitch and sample rate.
    public double GetPhase(double sample_rate=44100.0)
    {
        return samples / sample_rate * hz;
    }


    public void Reset(bool hz=false, bool samples=true, bool velocity = false, bool pressed=true)
    {
        if (hz) this.hz = 440.0;
        if (samples) this.samples = 0;
        if (pressed) this.pressed = true;
    }

    //Typically called by an Envelope when a note runs past its release time.
    public void Destroy(){

        //Remove myself from the channel.
        if (_channel != null){
            _channel.Remove(this);
        }

        QueueFree();

    }
}
