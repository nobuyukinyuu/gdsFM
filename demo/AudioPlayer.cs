// MIDI player demo AudioStreamPlayer

using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
// using System.Buffers;

using MidiSharp;
using MidiSharp.Events.Meta;
using MidiSharp.Events.Voice;
using MidiSharp.Events.Voice.Note;

namespace MidiDemo{

public class AudioPlayer : AudioStreamPlayer
{
public static float MixRate = 44100.0f;  //This is set to a global var sample_rate

//Each frame = 1000/MixRate ms. If default tickLen is 1ms, it would take < 44 frame resolution to be tick-accurate.
//If the tick length is lower due to faster tempo, lower this number to tickLen/frameLen increase accuracy.
public static int FramesPerEventCheck = 1; 
public int frameCounter;  //Keeps track of number of frames elapsed


public MidiSequence sequence;
AudioStreamGeneratorPlayback buf;  //Playback buffer
Vector2[] bufferdata = new Vector2[8192];

public Channel[] channels = new Channel[16];
public Patch[] patchBank = new Patch[127];

    Node global;

    public AudioPlayer() {       
        AudioStreamGenerator stream = (AudioStreamGenerator) this.Stream;
        MixRate = stream.MixRate;  Clock.sample_rate = MixRate;
        for (int i=0; i<channels.Length; i++){ channels[i]=new Channel(MixRate); }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

        buf = (AudioStreamGeneratorPlayback) GetStreamPlayback();

        //FIXME:  IMPLEMENT DRUMS
        channels[9].Mute = true;

        //Setup the patch bank.
        const string BANKPATH = "res://demo/bank0/";
        const string EXT = ".gfmp";
        for(int i=0; i<patchBank.Length; i++)
        {
            patchBank[i] = new Patch(MixRate);
            var inst = (GeneralMidiInstrument) i;
            var path= BANKPATH + inst.ToString() + EXT;
            var dir = new Godot.Directory();

            //Search for a bank to overload the initial one!!
            if (Godot.ResourceLoader.Exists(path) || dir.FileExists(path))
            {
                var f = new Godot.File();
                f.Open(path, File.ModeFlags.Read);
                patchBank[i].FromString(f.GetAsText());
                f.Close();
                GD.Print("Program ", i, " (", inst, ") loaded.");
            } else {
                //Init patch.
                patchBank[i].FromString(glue.INIT_PATCH, true);
            }
        }


        // //Set up the multithreading environment if we're to use multithreading
        // int workerthreads, iothreads;
        // System.Threading.ThreadPool.GetMinThreads(out workerthreads, out iothreads);
        // System.Threading.ThreadPool.SetMaxThreads(workerthreads*2, iothreads*2);

        // fill_buffer();   //prefill output buffer
        // Play();

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (!this.Playing)  return;
        fill_buffer();
        for (int i=0; i < channels.Length; i++)     channels[i].Update();  //Flag inactive notes and flush
    }


    /// Asyncronously flushes the preview channel and waits for an idle frame before continuing execution
    async public void ClearAllChannels()
    {
        for (int i=0; i < channels.Length; i++)  channels[i].FlushAll();
        await ToSignal(this.GetTree(), "idle_frame");
        // GD.Print("Flush OK.");
    }


    void fill_buffer()
    {
        int frames = buf.GetFramesAvailable();
        // var bufferdata = new List<Vector2[]>();  //frames from each channel
        var bufferdata = new Vector2[channels.Length][];  //frames from each channel
        var output = new Vector2[frames];       //final output
        object _lock = new object();


        //Get frame data for each channel.
        // for(int i=0; i<channels.Length; i++) {
        Parallel.For(0, channels.Length, delegate(int i) {
            lock(_lock)
            {
                // if (!channels[i].Empty)  //Fill the buffer for the channel.  If channel is 0, also process clock events.
                    bufferdata[i] = channels[i].request_samples(frames, ClockEvent, i) ;
            }
        } );

        //Assemble each set of frame data into the final output buffer.
        // for(int i=0; i<frames; i++) {
        Parallel.For(0, frames, delegate (int i) {
            // lock(_lock)
            // {
                for(int j=0; j < channels.Length; j++) //Cycle each channel into j.
                {
                    output[i].x += bufferdata[j][i].x * 0.25f; //quiet each channel by 1/16 (0.0625)
                    output[i].y += bufferdata[j][i].y * 0.25f;
                }
            // }
        } );

        buf.PushBuffer(output);
        // Clock.Iterate(frames, MixRate);
    }

    //This is used as a delegate that executes every sample frame.
    void ClockEvent(int channel)
    {
        if (frameCounter >= FramesPerEventCheck) 
        {
            frameCounter = 0;  //Reset the amount of frames needed to check for events again.
            var events = Clock.CheckForEvents( sequence );

            // Handle events here
            foreach (MidiSharp.Events.MidiEvent midiEvent in events)
            {
                //Determine event type and handle accordingly
                switch(midiEvent)
                {
                    //Meta events.  TODO
                    case TempoMetaMidiEvent ev:
                        Clock.SetTempo(ev.Value, sequence.TicksPerBeatOrFrame );
                        GD.Print("BPM: ", 60000000.0/( ev.Value ));
                        FramesPerEventCheck = (int)(Clock.tickFrameLen/4);
                        break;

                    //Voice channel events.  TODO
                    case ControllerVoiceMidiEvent ev:  //Continuous controller value.  Switch again.
                        switch((Controller) ev.Number)
                        {
                            case Controller.VolumeCoarse:
                                channels[ev.Channel].volume = (float)ev.Value / 127.0f;
                                break;
                            case Controller.PanPositionCoarse:
                                channels[ev.Channel].Pan = (float)ev.Value / 63.5f -1;
                                break;
                        }
                        break;
                    case ProgramChangeVoiceMidiEvent ev:
                        channels[ev.Channel].midi_program = ev.Number;
                        channels[ev.Channel].patch = patchBank[ev.Number];
                        break;

                    //Note events
                    case OnNoteVoiceMidiEvent ev:  //note: Can be more specific with "when ev" keyword
                        if (ev.Velocity == 0){ //Oh no!  This is an Off event in disguise!
                            channels[ev.Channel].TurnOffNote(ev.Note);
                            break; }

                        AddNote(ev.Channel, ev.Note, ev.Velocity);
                        break;
                    case OffNoteVoiceMidiEvent ev:  
                        channels[ev.Channel].TurnOffNote(ev.Note);
                        break;

                    default:
                        break;
                } //End switch
            } // End foreach
        }

        // Clock.Iterate(1,channel);
        //Don't iterate the frame counter unless we're sure all the channels have had a chance to process.
        if (channel==channels.Length-1) {
            Clock.Iterate();
            frameCounter ++;
        }
    }


    //TODO:  MOVE ME TO A DISCRETE MIXER OR SOMETHING?
    //      This function clobbers existing operator envelope settings!  Maybe we should have
    //      a version of Patch which will always contain valid Operators and only replace their
    //      settings if explicitly reset and not just re-validated with a new algorithm.
    public bool NewPatchFromString(String s, int ch=0)
    {
        double rate = (float) global.Get("sample_rate");

        channels[ch].patch = new Patch( rate );        
        return channels[ch].patch.WireUp(s);
    }
    public void NewPatch(int ch)
    {
        double rate = (float) global.Get("sample_rate");
        channels[ch].patch = new Patch( rate );        
    }
    public void NewPatch() {NewPatch(0);}

    // public bool UpdatePatchFromString(String s)
    // {
    //     double rate = (float) global.Get("sample_rate");

    //     if (this.patch == null) this.patch = new Patch(rate);
    //     return this.patch.WireUp(s);
    // }

    // /// Changes bypass value on an individual operator inside the current patch.
    // public Godot.Error Bypass(string opname, bool val)
    // {
    //         if (patch==null) return Godot.Error.DoesNotExist;
    //         Operator op = patch.GetOperator(opname);
    //         if (op == null) return Godot.Error.DoesNotExist;

    //         op.Bypass = val;
    //         return Godot.Error.Ok;
    // }


    /// Returns the number of notes that should be currently playing in our preview channel.
    public int Polyphony(int ch)
    {
        return channels[ch].Count;
    }





    ///////////////////////// MOVE THE STUFF BELOW TO CHANNEL OR SOMETHING.  INVESTIGATE SIGNALS


    /// Adds a note of the specific MIDI key to the preview notes.
    public Note AddNote(int channel, int note_number, int velocity)
    {
        if (channels[channel].patch != null)
        {
            foreach (LFO lfo in channels[channel].patch.LFOs)
            {
                lfo.Reset();
            }
        }

        Note note = new Note(note_number, velocity);
        note._channel = channels[channel];
        channels[channel].CheckPolyphony();  //NOTE: This can really slow down the process if attached to a debugger, so be careful
        channels[channel].Add(note);
        return note;    
    }

    /// Adds a note for a specific MIDI key, and attaches NoteOff signal from the specified handler. If we have no patch, note won't be added.
    public bool AddNote(int channel, int note_number, int velocity, Node handler)
    {
        if (channels[channel].patch==null) return false;
        var note = AddNote(channel, note_number, velocity);
        AttachNoteToSignal(channel, note, handler);
        return true;
    }


    //Signals for note off events are emitted by the MIDI event handler, which notes can use to start the release process without having to find them in the channel.
    //Might be possible to do this in the note, but TTL can't be determined without a Patch to calculate the release envelope.
    public void AttachNoteToSignal(int channel, Note note, Node signalSource)
    {

        //Commented out, for now.  Try using Channel.TurnOffNote instead.

        // signalSource.Connect("NoteOff", note as Note, "_on_ReleaseNote", 
        //                         new Godot.Collections.Array(){channels[channel].patch.GetReleaseTime(note)} );

    }


    /// Used by Godot frontend to change the preview pitch using a bend wheel.
    public void Pitch(int channel, double amt, float range)
    { 
        double rangemod = Math.Pow(2.0, (range/12) * amt);
        channels[channel].patch.pitchMod = rangemod;
    }


}
}