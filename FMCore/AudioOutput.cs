using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
// using System.Buffers;

public class AudioOutput : AudioStreamPlayer
{
public static float MixRate = 44100.0f;  //This is set to a global var sample_rate
AudioStreamGeneratorPlayback buf;  //Playback buffer
Vector2[] bufferdata = new Vector2[8192];

    private Patch patch;  // FM Instrument Patch
    public Patch Patch { get => patch; set => patch = value; }

    // public Note previewNote;  //Monophonic note used to preview the patch.
    // public Note PreviewNote () {return previewNote;}

public Channel PreviewNotes = new Channel();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        AudioStreamGenerator stream = (AudioStreamGenerator) this.Stream;
        MixRate = stream.MixRate;
        buf = (AudioStreamGeneratorPlayback) GetStreamPlayback();

        // // Prepare the audio buffer's pool of Vector2s.
        // //Set the buffer data to the length of the actual buffer.
        // bufferpool = new Vector2[(int) ( (float)hz * stream.BufferLength )];

        // //prefill buffer pool
        // for(int i=0; i<bufferpool.Length; i++){ bufferpool[i] = new Vector2(0.0f,0.0f); }


        //Set up the multithreading environment if we're to use multithreading
        int workerthreads, iothreads;
        System.Threading.ThreadPool.GetMinThreads(out workerthreads, out iothreads);
        System.Threading.ThreadPool.SetMaxThreads(workerthreads*2, iothreads*2);

        // fill_buffer3();   //prefill output buffer
        Play();

        // System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        // long frequency = System.Diagnostics.Stopwatch.Frequency;
        // var avg=0.0;
        // for(int r=0; r<10;r++){
        //     watch.Start();
        //     for (int i=0; i<30000000; i++)
        //     {
        //         oscillators.sint2(i/20.0);
        //     }
        //     watch.Stop();
        //     var time = watch.ElapsedTicks / (double)frequency * 1000;
        //     avg+= time;
        //     GD.Print("Elapsed: ", time, "ms");

        //     watch.Reset();
        // }  GD.Print("Average:  ", avg/10.0, "ms");
  
  
        // avg=0;
        // for(int r=0; r<10;r++){
        //     watch.Start();
        //     for (int i=0; i<30000000; i++)
        //     {
        //         oscillators.sint3(i/20.0);
        //     }
        //     watch.Stop();
        //     var time = watch.ElapsedTicks / (double)frequency * 1000;
        //     avg+= time;
        //     GD.Print("Elapsed: ", time, "ms");

        //     watch.Reset();
        // }  GD.Print("Average:  ", avg/10.0, "ms");
  

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(float delta)
    {
        fill_buffer2();
        PreviewNotes.Update();
        // PreviewNotes.FlagInactiveNotes();
        // PreviewNotes.Flush();
    }


    /// Asyncronously flushes the preview channel and waits for an idle frame before continuing execution
    async public void StopAll()
    {
        PreviewNotes.FlushAll();
        await ToSignal(this.GetTree(), "idle_frame");
        // GD.Print("Flush OK.");
    }

    /// Old single-threaded buffer fill.  Fills the buffer using Patch.cs one frame at a time.
    void fill_buffer()
    {
        if(buf == null)  return;
        int frames = buf.GetFramesAvailable();
        bufferdata = new Vector2[frames];

        for (int i=0; i< frames; i++)
        {
            if (patch != null)
            {
                var s = patch.mix(PreviewNotes);

                 bufferdata[i].x = s;  //TODO:  Stereo mixing maybe
                 bufferdata[i].y = s;  //TODO:  Stereo mixing maybe   
            }
        }

        buf.PushBuffer(bufferdata); 

        // #if DEBUG
        // if (buf.GetSkips()>0) GD.Print("Skips: ", buf.GetSkips());
        // #endif
    }

    /// Parallelized buffer fill.  Seems less processor efficient in debug mode.....
    void fill_buffer2()
    {
        int frames = buf.GetFramesAvailable();
        bufferdata = new Vector2[frames];

        //Process the LFOs.
        patch.UpdateLFOs(frames);

        //Ask the Patch to process the notes.
        object _lock = new object();
        Parallel.For(0, PreviewNotes.Count, delegate(int i)
        {
            if (patch != null)
            {
                
                var output = patch.request_samples(PreviewNotes[i], frames);

                //Assemble the output samples into the buffer.
                Parallel.For(0, frames, delegate(int j) 
                {
                    lock(_lock) {  //Using a normal lock because the spinlocks below aren't guaranteed to be atomic and make fuzzy artifacts... maybe fix one day..

                        //TODO:  Applies patch mixing globally. If multiple programs are being mixed this needs to change so mixing is applied per-patch.
                        //      Global mixing is only used here for a slight speed boost.
                        bufferdata[j].x += output[j] * 0.5f * patch._panL * patch.gain;  
                        bufferdata[j].y += output[j] * 0.5f * patch._panR * patch.gain;  
                    }

                    // System.Threading.Interlocked.Exchange(ref bufferdata[j].x, GDSFmFuncs.InterlockedAdd(ref bufferdata[j].x, (float) output[j]) );
                    // System.Threading.Interlocked.Exchange(ref bufferdata[j].y, GDSFmFuncs.InterlockedAdd(ref bufferdata[j].y, (float) output[j]) );

                } );
            }
        } );

        //TODO:  Attenuate the samples here with another parallel For loop on the final buffer.  Automatic gain control maybe applied here?

        //FIXME:  This breaks stereo panning
        // for(int i=0; i<bufferdata.Length; i++)
        // {
        //     // bufferdata[i].x = patch.filter.Filter(bufferdata[i].x);
        //     bufferdata[i].x = patch.formantFilter.Filter(bufferdata[i].x);
        //     bufferdata[i].y = bufferdata[i].x;
        // }

        // if (patch!=null)  LowPass(patch.CutOff,patch.Resonance);
        buf.PushBuffer(bufferdata); 
    }

    /// This may be a serial process, but for some reason is faster than fill_buffer().  Maybe because there's less function calls?
    void fill_buffer3()
    {
        int frames = buf.GetFramesAvailable();
        bufferdata = new Vector2[frames];

        //Process the LFOs.
        patch.UpdateLFOs(frames);

        for(int i=0; i < PreviewNotes.Count; i++)
        {
            if (patch != null)
            {
                var output = patch.request_samples(PreviewNotes[i], frames);

                //Assemble the output samples into the buffer.
                for(int j=0; j < frames; j++)
                {
                    //TODO:  Applies patch mixing globally. If multiple programs are being mixed this needs to change so mixing is applied per-patch.
                    //      Global mixing is only used here for a slight speed boost.
                    bufferdata[j].x += output[j] * 0.5f * patch._panL * patch.gain;  
                    bufferdata[j].y += output[j] * 0.5f * patch._panR * patch.gain;  
                } 
            }
        }

        //TODO:  Attenuate the samples here with another parallel For loop on the final buffer.  Automatic gain control maybe applied here?

        buf.PushBuffer(bufferdata); 
    }


    //TODO:  MOVE ME TO A DISCRETE MIXER OR SOMETHING?
    //      This function clobbers existing operator envelope settings!  Maybe we should have
    //      a version of Patch which will always contain valid Operators and only replace their
    //      settings if explicitly reset and not just re-validated with a new algorithm.
    public bool NewPatchFromString(String s)
    {
        this.patch = new Patch( MixRate );        
        return this.patch.WireUp(s);
    }
    public void NewPatch()
    {
        this.patch = new Patch( MixRate );
    }

    public bool UpdatePatchFromString(String s)
    {
        if (this.patch == null) this.patch = new Patch(MixRate);
        return this.patch.WireUp(s);
    }

    /// Changes bypass value on an individual operator inside the current patch.
    public Godot.Error Bypass(string opname, bool val)
    {
            if (patch==null) return Godot.Error.DoesNotExist;
            Operator op = patch.GetOperator(opname);
            if (op == null) return Godot.Error.DoesNotExist;

            op.Bypass = val;
            return Godot.Error.Ok;
    }


    /// Returns the number of notes that should be currently playing in our preview channel.
    public int Polyphony()
    {
        return PreviewNotes.Count;
    }

    /// Adds a note of the specific MIDI key to the preview notes.
    public Note AddNote(int note_number, int velocity)
    {
        if (patch != null)
        {
            foreach (LFO lfo in patch.LFOs)
            {
                lfo.Reset();
            }
        }

        Note note = new Note(note_number, velocity);
        note._channel = PreviewNotes;
        PreviewNotes.CheckPolyphony();  //NOTE: This can really slow down the process if attached to a debugger, so be careful
        PreviewNotes.Add(note);
        return note;    
    }

    /// Adds a note for a specific MIDI key, and attaches NoteOff signal from the specified handler. If we have no patch, note won't be added.
    public bool AddNote(int note_number, int velocity, Node handler)
    {
        if (patch==null) return false;
        var note = AddNote(note_number, velocity);
        AttachNoteToSignal(note, handler);
        return true;
    }

    public void TurnOffNote(int note_number)
    {
        Note note = PreviewNotes.FindActiveNote(note_number);        

        #if DEBUG
        if (note==null) throw new NullReferenceException("Note not found?");
        #endif

        // note.releaseSample = note.samples;
        // note.pressed = false;

        if (patch==null) // Uh oh, no patch right now.  Probably should just kill the note.
        {
            note.Destroy();
        } else {  // Patch is okay.  Set the TTL to prepare the note to be killed off by the Patch when sent the Channel contents.
            var ttl = patch.GetReleaseTime(note_number);
            note._on_ReleaseNote(note_number, ttl);
        }
    }

    //Signals for note off events are emitted by the MIDI event handler, which notes can use to start the release process without having to find them in the channel.
    //Might be possible to do this in the note, but TTL can't be determined without a Patch to calculate the release envelope.
    public void AttachNoteToSignal(Note note, Node signalSource)
    {

        //Commented out, for now.  Try using Channel.TurnOffNote instead.

        // signalSource.Connect("NoteOff", note as Note, "_on_ReleaseNote", 
        //                         new Godot.Collections.Array(){channels[channel].patch.GetReleaseTime(note)} );

    }


    /// Used by Godot frontend to change the preview pitch using a bend wheel.
    public void Pitch(float amt, float range)
    { 
        float rangemod = (float) Math.Pow(2.0, (range/12) * amt);
        patch.pitchMod = rangemod;
    }


    public string GetPhases()
    {
        var sb = new System.Text.StringBuilder("");
        if (!PreviewNotes.Empty)
        {
            for(int i=0; i<4; i++)
            {
                sb.Append ((int) PreviewNotes[0].GetPhase(i));
                sb.Append(", ");
            }
        }

        return sb.ToString();
    }

}
