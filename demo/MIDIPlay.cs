using Godot;
using System;
using System.Collections.Generic;
using MidiSharp;


namespace MidiDemo{
    public class MIDIPlay : Control
    {
        MidiSequence sequence;
        bool isPlaying;

    public override void _Ready()
        {

        }


        public void LoadMIDI(string path)
        {
            using (System.IO.Stream inputStream = System.IO.File.OpenRead(path))
            {
                sequence = MidiSequence.Open(inputStream);
            }

            var lbl = (Label) GetNode("SC/Label");
            lbl.Text = (string) sequence.ToString();

            //We need to determine the length of a tick for our clock. RN just find the first TempoMetaMidiEvent in track 0
            //And assume that's what the tempo will always be.  Value of this will be microseconds per quarter.
            foreach (MidiSharp.Events.MidiEvent ev in sequence.Tracks[0].Events)
            {
                if (ev is MidiSharp.Events.Meta.TempoMetaMidiEvent)
                {
                    GD.Print("BPM: ", 60000000.0/((MidiSharp.Events.Meta.TempoMetaMidiEvent)ev).Value );
                }
            }

        }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }
    }
}

