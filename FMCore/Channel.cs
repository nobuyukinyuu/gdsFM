using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public class Channel : List<Note>
{
    #if DEBUG
        int maxPolyphony=24;
    #else
        int maxPolyphony=72;
    #endif

    public Stack<Note> _flaggedForDeletion = new Stack<Note>();

    public void FlagForDeletion(Note note)
    {
        _flaggedForDeletion.Push(note);
    }

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

}

