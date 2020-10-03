using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Channel : List<Note>
{
    // TODO:  If using the channel to request samples from the current patch, handle notes on during a program change.
    //          Ideas:  A "sink" channel or a patch override in Note.  Maybe handle its own AudioServer bus at runtime?

    /// Patch associated with this channel.
    public Patch patch=new Patch(44100);  //TODO:  add property to handle program change?

    private bool mute;  public bool Mute { get => mute; set => mute = value; }


#if DEBUG
    int maxPolyphony=24;
    #else
        int maxPolyphony=72;
    #endif

    /// Returns true if the Channel doesn't have any active notes currently.
    public bool Empty {get => this.Count == 0;}

    public Stack<Note> _flaggedForDeletion = new Stack<Note>();
    public void FlagForDeletion(Note note){    _flaggedForDeletion.Push(note);    }
    public void FlagInactiveNotes()
    {
        for(int i=0; i<this.Count; i++) 
        {
            Note note = this[i];
            if(note != null && note.IsDestroyable()) FlagForDeletion(note);
        }
    }

    /// Flushes the inactive notes flagged for deletion in this channel.
    public void Flush()
    {
        while (_flaggedForDeletion.Count>1)
        {
            Note note = _flaggedForDeletion.Pop();
            this.Remove(note);
            note.QueueFree();
        }
    }

    /// Flushes all the notes in the channel immediately.  Good for panicing or when engine needs to clear references to stuff before they're accidentally accessed.
    public void FlushAll()
    {
        GD.Print("GDSFM Channel:  Flushing ", this.Count, " notes....");
        for (int i=0; i < this.Count; i++)
        {
            this[i].QueueFree();
        }

        this.Clear();
    }

    public Note FindActiveNote(int midi_note)
    {
        var n = midi_note;  //Import to local scope for the lambda below
        return Find((Note x) => (x.midi_note == n) && (x.pressed==true) && (x.releaseSample==0));
    }

    //Deals with the channel exceeding its polyphony limit.
    public void CheckPolyphony()
    {
        while (this.Count > maxPolyphony)
        {
            this.RemoveAt(0);   //Lazy improper method.  Should probably check another, presorted list for the reference to remove.  FIXME
            //Ideally:  We contain a list of "overflow candidates" which is re-sorted on new note insertion.  Maybe override base.Add?
            //The sort operation checks in order of priority:  NoteOffs come first, within them sorted by higher ttl. The rest are by insertion order.
            //When max polyphony is exceeded, we pop off the front
        }
    }

    // private static bool isActiveNote(Note x)
    // {return (x.midi_note == midi_note) && (x.pressed) && (x.releaseSample==0);}

    /// Updates the state of the channel by flagging inactive notes and flushing them.
    public void Update()
    {
            FlagInactiveNotes();
            Flush();
    }

    /// Retrieves a buffer representing the active notes in this channel.
    public Vector2[] request_samples(int frames, Action<int> clockEvents, int clockChannel=0)
    {
        var bufferdata = new Vector2[frames];
        if (mute) return bufferdata;
        if (patch == null) return bufferdata;

        //Process the LFOs.
        patch.UpdateLFOs(frames);

        //Ask the Patch to process the notes.
        object _lock = new object();
        double output = 0;

        for(int j=0; j < frames; j++ )
        {
            // lock(_lock) {  
                Parallel.For(0, this.Count, delegate(int i)  //For each note in the channel....
                {
                    lock(_lock) { output += patch.mix(this[i]); }  
                } );  //End note loop

                bufferdata[j].x = (float) output * 0.5f * patch._panL * patch.gain;  
                bufferdata[j].y = (float) output * 0.5f * patch._panR * patch.gain;  
                clockEvents(clockChannel); //Process the clock events for the frame.  Will only happen if channel is 0.
            // }
        } //); //End buffer loop



        return bufferdata;
    }

    public int[] ActiveNotes()
    {
        var output = new int[Count];

        for(int i=0; i < Count; i++)
        {
            output[i] = this[i].midi_note;
        }

        return output;
    }

}