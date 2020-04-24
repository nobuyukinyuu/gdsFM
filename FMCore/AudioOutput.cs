using Godot;
using System;
using System.Collections.Generic;
using System.Buffers;

public class AudioOutput : AudioStreamPlayer
{
public static float MixRate = 44100.0f;  //This is set to a global var sample_rate

AudioStreamGeneratorPlayback buf;  //Playback buffer
Vector2[] bufferdata = new Vector2[8192];

public Patch patch;  // FM Instrument Patch

public Note previewNote;  //Monophonic note used to preview the patch.
    // public Note PreviewNote () {return previewNote;}

public Channel PreviewNotes = new Channel();


    Node global;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        global = GetNode("/root/global");
        MixRate = (float) global.Get("sample_rate");

        AudioStreamGenerator stream = (AudioStreamGenerator) this.Stream;
        MixRate = stream.MixRate;
        buf = (AudioStreamGeneratorPlayback) GetStreamPlayback();

        // // Prepare the audio buffer's pool of Vector2s.
        // //Set the buffer data to the length of the actual buffer.
        // bufferpool = new Vector2[(int) ( (float)hz * stream.BufferLength )];

        // //prefill buffer pool
        // for(int i=0; i<bufferpool.Length; i++){ bufferpool[i] = new Vector2(0.0f,0.0f); }

        fill_buffer();   //prefill output buffer
        Play();


    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        fill_buffer();
    }


    //Fills the buffer using Patch.cs
    void fill_buffer()
    {
        if(buf == null)  return;
        int frames = buf.GetFramesAvailable();
        bufferdata = new Vector2[frames];

        for (int i=0; i< frames; i++)
        {
            if (patch != null)
            {
                var s = (float) patch.mix(PreviewNotes);

                 bufferdata[i].x = s;  //TODO:  Stereo mixing maybe
                 bufferdata[i].y = s;  //TODO:  Stereo mixing maybe   
            }
        }

        buf.PushBuffer(bufferdata); 
    }




    //TODO:  MOVE ME TO A DISCRETE MIXER OR SOMETHING?
    //      This function clobbers existing operator envelope settings!  Maybe we should have
    //      a version of Patch which will always contain valid Operators and only replace their
    //      settings if explicitly reset and not just re-validated with a new algorithm.
    public bool NewPatchFromString(String s)
    {
        double rate = (float) global.Get("sample_rate");

        this.patch = new Patch( rate );        
        return this.patch.WireUp(s);
    }
    public bool UpdatePatchFromString(String s)
    {
        double rate = (float) global.Get("sample_rate");

        if (this.patch == null) this.patch = new Patch(rate);
        return this.patch.WireUp(s);
    }

    //Changes bypass value on an individual operator inside the current patch.
    public int Bypass(string opname, bool val)
    {

            Operator op = patch.GetOperator(opname);
            if (op!=null)  op.Bypass = val;
            if (op!=null) return 0; else return -1;

    }


    //DEBUG.  This would be in a Note class instead once that exists.  Resets sample timer.
    public void Reset()
    {
        if (previewNote !=null)  previewNote.Reset();
    }

    //Returns the number of notes that should be currently playing in our preview channel.
    public int Polyphony()
    {
        return PreviewNotes.Count;
    }

    //Adds a note of the specific MIDI key to the preview notes.
    public Note AddNote(int note_number, int velocity)
    {
        Note note = new Note(note_number, velocity);
        note._channel = PreviewNotes;
        PreviewNotes.Add(note);
        return note;
    
    }

    //Adds a note for a specific MIDI key, and attaches NoteOff signal from the specified handler. If we have no patch, note won't be added.
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
        if (note==null) throw new NullReferenceException("Note not found?");

        note.releaseSample = note.samples;
        note.pressed = false;

        if (patch==null) // Uh oh, no patch right now.  Probably should just kill the note.
        {
            note.Destroy();
        } else {  // Patch is okay.  Set the TTL to prepare the note to be killed off by the Patch when sent the Channel contents.
            note.ttl = patch.GetReleaseTime();
        }
    }

    //Signals for note off events are emitted by the MIDI event handler, which notes can use to start the release process without having to find them in the channel.
    //Might be possible to do this in the note, but TTL can't be determined without a Patch to calculate the release envelope.
    public void AttachNoteToSignal(Note note, Node signalSource)
    {
        //TODO:  Test when the release time calculation is bound to the Note.  Probably on NoteOff....
        signalSource.Connect("NoteOff", note as Note, "_on_ReleaseNote", new Godot.Collections.Array(){patch.GetReleaseTime()} );
        //Other event signals here, as necessary.
    }


    public void Pitch(double amt)
    { //DEBUG PURPOSES ONLY
        if (PreviewNotes.Count > 0)
        {
            switch (Math.Sign(amt))
            {
                case 1:
                    PreviewNotes[0].hz = 440.0 + 440 * Math.Abs(amt);
                    return;
                case -1:
                    PreviewNotes[0].hz = 220 + 220.0 * Math.Abs(1+amt);
                    return;

            }
        }
    }
}
