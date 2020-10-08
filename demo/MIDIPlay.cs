using Godot;
using System;
using System.Collections.Generic;
using MidiSharp;


namespace MidiDemo{
    public class MIDIPlay : Control
    {
        public MidiSequence sequence;
        bool isPlaying;
        AudioPlayer player;

    public override void _Ready()
        {
            player = GetNode<AudioPlayer>("Output");
        }


    public void LoadMIDI(string path)
    {
        try
        {
            using (System.IO.Stream inputStream = System.IO.File.OpenRead(path))
            {
                sequence = MidiSequence.Open(inputStream);
            }

            var lbl = (Label) GetNode("SC/Label");
            lbl.Text = (string) sequence.ToString();

            Stop();  //Stop the player and clear the channels.

            //Set the MIDI sequence for the player.
            player.sequence = sequence;
            Clock.Reset(sequence.Tracks.Count);  //RESET THE CLOCK.  This properly sets the number of tracks for the clock to check.

        } catch (MidiParser.MidiParserException e) {
            GD.Print("WARNING:  Encountered a problem parsing MIDI.\n", e.ToString() );
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
            // GetNode<Label>(String.Format("Preview/Roll{0}/Roll/Label", i)).Text = "[" + string.Join(", ", pl.channels[i].ActiveNotes()) + "]";
            GetNode<Label>(String.Format("Preview/Roll{0}/Roll/Label", i)).Text = "[" + string.Join(", ", player.channels[i].Count) + "]";
            GetNode(String.Format("Preview/Roll{0}", i)).Set("program", player.channels[i].midi_program);
            GetNode(String.Format("Preview/Roll{0}", i)).Set("active_keys", player.channels[i].ActiveNotes());
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
        player.ClearAllChannels();
        GetNode<Button>("PlayPause").Pressed = false;
    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }
    }
}

