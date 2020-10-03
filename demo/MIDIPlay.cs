using Godot;
using System;
using System.Collections.Generic;
using MidiSharp;


namespace MidiDemo{
    public class MIDIPlay : Control
    {
        public MidiSequence sequence;
        bool isPlaying;
        AudioStreamPlayer player;

    public override void _Ready()
        {
            player = GetNode<AudioStreamPlayer>("Output");
        }


    public void LoadMIDI(string path)
    {
        using (System.IO.Stream inputStream = System.IO.File.OpenRead(path))
        {
            sequence = MidiSequence.Open(inputStream);
        }

        var lbl = (Label) GetNode("SC/Label");
        lbl.Text = (string) sequence.ToString();

        //Set the MIDI sequence for the player
        var pl=(AudioPlayer)player;
        pl.sequence = sequence;

        //We need to determine the length of a tick for our clock. RN just find the first TempoMetaMidiEvent in track 0
        //And assume that's what the tempo will always be.  Value of this will be microseconds per quarter.
        foreach (MidiSharp.Events.MidiEvent ev in sequence.Tracks[0].Events)
        {
            if (ev is MidiSharp.Events.Meta.TempoMetaMidiEvent)
            {
                var data = (MidiSharp.Events.Meta.TempoMetaMidiEvent) ev;
                GD.Print("BPM: ", 60000000.0/( data.Value ));
                Clock.SetTempo(data.Value, sequence.TicksPerBeatOrFrame);
            }
        }
    }

    public override void _Process(float delta)
    {
    }    
    public override void _PhysicsProcess(float delta)
    {
        GetNode<Label>("Time").Text = Math.Floor(Clock.BeatsElapsed).ToString() + "\n" + Clock.SecondsElapsed.ToString();

        for (int i=0; i < 16; i++)
        {
            var pl = (AudioPlayer) player;
            // GetNode<Label>(String.Format("Preview/Roll{0}/Roll/Label", i)).Text = "[" + string.Join(", ", pl.channels[i].ActiveNotes()) + "]";
            GetNode<Label>(String.Format("Preview/Roll{0}/Roll/Label", i)).Text = "[" + string.Join(", ", pl.channels[i].Count) + "]";
            GetNode(String.Format("Preview/Roll{0}", i)).Set("program", pl.channels[i].midi_program);
            GetNode(String.Format("Preview/Roll{0}", i)).Set("active_keys", pl.channels[i].ActiveNotes());
        }
    }    

    /// Plays or pauses the midi
    public void PlayPause(bool playing)
    {
        if (playing)
        {
            player.Play();
        } else {
            player.Stop();
        }
    }

    public void Stop()
    {
        player.Stop();
        Clock.Reset();
    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }
    }
}

