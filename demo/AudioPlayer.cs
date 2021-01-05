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
    public Patch[] patchBank = new Patch[127];   //MIDI Programs
    public MidiEventParser parser = new MidiEventParser();

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
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _PhysicsProcess(float delta)
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
            for(int i=0; i<channels.Length; i++) {
            // Parallel.For(0, channels.Length, delegate(int i) {
            //     lock(_lock)
                {
                    // if (!channels[i].Empty)  //Fill the buffer for the channel.  If channel is 0, also process clock events.
                        bufferdata[i] = channels[i].request_samples(frames, ClockEvent, i) ;
                }
            } //);

            //Assemble each set of frame data into the final output buffer.
            // for(int i=0; i<frames; i++) {
            Parallel.For(0, frames, delegate (int i) {
                // lock(_lock)
                // {
                    for(int j=0; j < channels.Length; j++) //Cycle each channel into j.
                    {
                        output[i].x += bufferdata[j][i].x * 0.3f; //quiet each channel by 1/16 (0.0625)
                        output[i].y += bufferdata[j][i].y * 0.3f;
                    }
                // }
            } );

            buf.PushBuffer(output);
            // Clock.Iterate(frames, MixRate);
        }

        //This is used as a delegate that executes every sample frame.
        void ClockEvent(int channel)
        {
            //FIXME:  restore the original early exit when not on a tick.

            if (channel==channels.Length-1) 
            {        
            // if (frameCounter >= FramesPerEventCheck) 
            // {
            //     frameCounter = 0;  //Reset the amount of frames needed to check for events again.
                var events = Clock.CheckForEvents( sequence ) ?? System.Linq.Enumerable.Empty<MidiSharp.Events.MidiEvent>();

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

                            //FIXME:  When changing the buffer fill algorithm to a parallized one requesting sample batches, this won't work anymore
                            //        Because Channel will be requesting an entire buffer from Patch at once and clock events won't be able to execute in the middle.
                            //        Precalculate for each channel the frame that notes should be activated, as well as all other audio state changes......
                            //        This probably includes CCs......
                            AddNote(ev.Channel, ev.Note, ev.Velocity);
                            break;
                        case OffNoteVoiceMidiEvent ev:  
                            channels[ev.Channel].TurnOffNote(ev.Note);
                            break;

                        default:
                            break;
                    } //End switch
                } // End foreach
            } //End frame counter event check

            //Don't iterate the frame counter unless we're sure all the channels have had a chance to process.
            if (channel==channels.Length-1) {
                Clock.Iterate();
                frameCounter ++;
            }
        }




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

        /// Used by Godot frontend to change the preview pitch using a bend wheel.
        public void Pitch(int channel, double amt, float range)
        { 
            double rangemod = Math.Pow(2.0, (range/12) * amt);
            channels[channel].patch.pitchMod = rangemod;
        }
    }


}