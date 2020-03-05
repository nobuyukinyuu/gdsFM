using Godot;
using System;
using System.Collections.Generic;

public class AudioOutput : AudioStreamPlayer
{
AudioStreamGeneratorPlayback buf;  //Playback buffer
int samples = 0;
//PoolVector2Array bufferdata = new PoolVector2Array();
// List<Vector2> bufferdata = new List<Vector2>();

Vector2[] bufferdata;

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

        fill_buffer();   //prefill buffer
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

    void fill_buffer(int frames = -1){
        if(buf == null)  return;
        int frames_to_fill = buf.GetFramesAvailable();
        if (frames >=0)  frames_to_fill = frames;

        bufferdata = new Vector2[frames_to_fill];

        for (int i=0; i<frames_to_fill; i++){
        // # The true phase is calculated by each oscillator's wave function.
		// # It's wrapped to a value between 0-1, but to account for detune,
		// # we don't wrap the phase here.
            float phase = (float) global.Call("get_secs") * BASE_TONE;

            GraphEdit Graph = GetNode("../GraphEdit") as GraphEdit;
            if ((bool) Graph.Get("connections_valid") == true){
                float s = (float) Graph.GetNode("Output").Call("mix", phase);
                bufferdata[i] = new Vector2(s,s);  //TODO:  Stereo mixing maybe
            }

            global.Set("samples", (int) global.Get("samples") + 1);
        }

        buf.PushBuffer(bufferdata);
    
    }

}
