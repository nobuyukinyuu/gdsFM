using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MidiSharp;


namespace MidiDemo{
    public class MIDIPlay : Control
    {
        public MidiSequence sequence;
        bool isPlaying;
        AudioPlayer player;

        Queue<int[]>[] noteBuffer = new Queue<int[]>[16];  //Active note buffer.

    public override void _Ready()
        {
            player = GetNode<AudioPlayer>("Output");


            //Determine how many frames of delay the audio buffer is in order to make active keys display at the correct time.
            var buflen_t = (AudioStreamGenerator) player.Stream;
            var buf_size = Godot.Engine.IterationsPerSecond * buflen_t.BufferLength;

            //Initialize the queues.
            for(int i=0; i < noteBuffer.Length; i++)
            {
                noteBuffer[i] = new Queue<int[]>((int)buf_size);  //Create queue of buffer_length.
                
                //Fill the buffer with an empty array.
                for (int j=0; j < buf_size; j++)
                {
                    noteBuffer[i].Enqueue( new int[0] );
                }
            }

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

            AudioStreamGenerator streamGenerator = (AudioStreamGenerator) player.Stream;
            player.parser.ParseSequence(sequence, streamGenerator.MixRate);

        } catch (MidiParser.MidiParserException e) {
            GD.Print("WARNING:  Encountered a problem parsing MIDI.\n", e.ToString() );
        }

    }

    // public override void _Process(float delta)   {    }    
    public override void _PhysicsProcess(float delta)
    {
        GetNode<Label>("Time").Text = Math.Floor(Clock.BeatsElapsed).ToString() + "\n" + Clock.SecondsElapsed.ToString();

        if (!player.Playing) return;
        // for (int i=0; i < 16; i++)
        Parallel.For (0, 16, delegate(int i)
        {
            //Enqueue the active notes just processed to the buffer to retrieve later.
            noteBuffer[i].Enqueue(player.channels[i].ActiveNotes());

            GetNode<Label>(String.Format("Preview/Roll{0}/Roll/Label", i)).Text = "[" + string.Join(", ", player.channels[i].Count) + "]";
            GetNode(String.Format("Preview/Roll{0}", i)).Set("program", player.channels[i].midi_program);

            // int[] keys;
            // var ok = noteBuffer[i].TryDequeue(out keys);  //Will set ok true if dequeue success, otherwise keys will be null
            // GetNode(String.Format("Preview/Roll{0}", i)).Set("active_keys",  ok? keys: new int[0] );
            GetNode(String.Format("Preview/Roll{0}", i)).Set("active_keys",  noteBuffer[i].Dequeue() ?? new int[0] );
        } );
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

    }  //End Class
} //End Namespace

//  public class FixedQueue<T> : System.Collections.Concurrent.ConcurrentQueue<T>
// {
//     private readonly object syncObject = new object();

//     public int Size { get; private set; }

//     public FixedQueue(int size)
//     {
//         Size = size;
//     }

//     public new void Enqueue(T obj)
//     {
//         base.Enqueue(obj);
//         lock (syncObject)
//         {
//             while (base.Count > Size)
//             {
//                 T outObj;
//                 base.TryDequeue(out outObj);
//             }
//         }
//     }
// }