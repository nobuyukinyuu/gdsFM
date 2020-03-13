using Godot;
using System;
using System.Collections.Generic;
using System.Buffers;

public class AudioOutput : AudioStreamPlayer
{
AudioStreamGeneratorPlayback buf;  //Playback buffer
Vector2[] bufferdata = new Vector2[8192];
ArrayPool<Vector2> bufferpool = ArrayPool<Vector2>.Shared;

public Patch patch;  // FM Instrument Patch

Node global;

const float BASE_TONE = 440f;   //TODO:  Change this later when phase is calculated inside the operator based on elapsed samples

float hz = 44100.0f;  //This is set to a global var sample_rate

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        global = GetNode("/root/global");
        hz = (float) global.Get("sample_rate");

        AudioStreamGenerator stream = (AudioStreamGenerator) this.Stream;
        stream.MixRate = hz;
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

    public override void _PhysicsProcess(float delta)
    {
        if (bufferdata.Length < 2) return;
        
        Vector2 rect_size = (Vector2) (GetNode("../Panel").Get("rect_size"));
        Vector2[] pts = new Vector2[(int) Math.Min(rect_size.x, bufferdata.Length)];

        for(int i=0; i < pts.Length; i++)
        {
            float h = rect_size.y / 2.0f;
            pts[i] = new Vector2(i, h + bufferdata[i].y * h);
        }

            Line2D l = (Line2D) GetNode("../Panel/Line2D");
            l.Points = pts;
        
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
                var s = (float) patch.mix();
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

}
