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

public List<Note> PreviewNotes = new List<Note>();


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
                var s = (float) patch.mix(previewNote);
                s *= (float) previewNote.Velocity;  //DEBUG:  REMOVE ME WHEN PER-OPERATOR VELOCITY SENSITIVITY IS IMPLEMENTED?

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
            op.Bypass = val;
            if (op!=null) return 0; else return -1;

    }


    //DEBUG.  This would be in a Note class instead once that exists.  Resets sample timer.
    public void Reset()
    {
        if (previewNote !=null)  previewNote.Reset();
    }

}
