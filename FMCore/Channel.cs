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
    public Patch patch;  //TODO:  add property to handle program change?
    public int midi_program=0;  //Used for MIDI to identify an associated program with this channel.  Set manually.

    Dictionary<int, Note> lookupTbl = new Dictionary<int, Note>();  //Dictionary of active notes. Makes for faster lookup.

    public Channel(double sample_rate=44100) {
        patch = new Patch(sample_rate);
        patch.FromString(glue.INIT_PATCH, true);
    }

    //Stuff related to the channel's sample requesting functionality
    private bool mute;  public bool Mute { get => mute; set => mute = value; }
    public double volume = 1.0;  private float pan, _panL=0.5f, _panR=0.5f ;

    /// Takes a value from -1.0 to 1.0 and assigns _panL and _panR to respective values depending on center channel volume C_PAN.
    public float Pan
    {
        get => pan;
        set {
            float val = value;
            pan = val;
            var amt = Math.Abs(val);
            float l,r;

            if (val < 0) {  //Pan left channel
                l = amt;
                r = 1.0f-amt;

                _panL = GDSFmFuncs.lerp(Patch.C_PAN, 1.0f, l);
                _panR = GDSFmFuncs.lerp(0.0f, Patch.C_PAN, r);
                return;

            } else if (val > 0) {  //Pan right
                l = 1.0f-amt;
                r = amt;

                _panL = GDSFmFuncs.lerp(0.0f, Patch.C_PAN, l);
                _panR = GDSFmFuncs.lerp(Patch.C_PAN, 1.0f, r);
                return;    

            } else {  //Center channel.
                _panL = Patch.C_PAN;
                _panR = Patch.C_PAN;
                return;
            }
        } //End Set
    }

    #if DEBUG
        int maxPolyphony=16;
    #else
        int maxPolyphony=64;
    #endif

    /// Returns true if the Channel doesn't have any active notes currently.
    public bool Empty {get => this.Count == 0;}

    public Stack<Note> _flaggedForDeletion = new Stack<Note>();

    /// Adds a new note to the channel.
    public new void Add (Note item)
    {
        //NOTE:  When notes are no longer active, they still exist in the collection, but because lookupTbl 
        //      errors out on a key already existing, we may want to consider removing note elements from it
        //      immediately when a note is turned off. If multiple same-pitch notes are started and stopped
        //      before old ones are ready to die, the calls to TurnOffNote will fetch the last note added to the table.
        //      This could potentially leave notes hanging if two On events occur in a row.  
        //      The alternative is erroring out here during the lookupTbl add, having an inactive notes collection, or...

        base.Add(item);  //Call the superclass.
        // lookupTbl.Add(item.midi_note, item);  //Throws an error if a note is already active in the same pitch
        lookupTbl[item.midi_note] = item;   //Overwrites any note references active in the same pitch
    }

    /// Gets a note from the lookup table, or null.
    public Note GetNote (int value) {
        Note output;
        lookupTbl.TryGetValue(value, out output);
        return output;
    }


    /// Updates the state of the channel by flagging inactive notes and flushing them.
    public void Update()
    {
            FlagInactiveNotes();
            Flush();
    }

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
            lookupTbl.Remove(note.midi_note);

            note.Destroy();
            // note.QueueFree();
        }
    }

    /// Flushes all the notes in the channel immediately.  Good for panicing or when engine needs to clear references to stuff before they're accidentally accessed.
    public void FlushAll()
    {

        // TODO:  Find a threadsafe alternative to this which doesn't leverage Godot

        // // GD.Print("GDSFM Channel:  Flushing ", this.Count, " notes....");
        // for (int i=0; i < this.Count; i++)
        // {
        //     this[i].QueueFree();
        // }



        this.Clear();
        lookupTbl.Clear();
    }

    public Note FindActiveNote(int midi_note)
    {
        var n = midi_note;  //Import to local scope for the lambda below
        return Find((Note x) => (x.midi_note == n) && (x.pressed==true) && (x.releaseSample==0));
    }

    /// Deals with the channel exceeding its polyphony limit.
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



    /// Retrieves a buffer representing the active notes in this channel.
    public Vector2[] request_samples(int frames, Action<int> clockEvents, int clockChannel=0)
    {
        var bufferdata = new Vector2[frames];
        if (mute) return bufferdata;
        if (patch == null) return bufferdata;

        //Process the LFOs.
        patch.UpdateLFOs(frames);

        //Ask the Patch to process the notes.

        //TODO:  When changing this back to a parallelized routine with Patch.request_samples, get the clock events for the entire buffer
        //      as specific frames the CHANNEL volume and pan (and later, pitch adjust) should be.  Probably need to calculate it in advance.
        //      Instead of being a delegate, clock events could be a traditional func which provides a buffer of the exact state on each frame
        //      for each channel, called once per fill, and the channel's specific states sent to the individual channels for parallel processing.
        //      Perhaps an array of structs containing pan, volume, pitch and other cc data? Or would class instances be faster?
        //      If events aren't buffered all at once and precalculated, then processing of them can only occur BETWEEN buffers.  Not good...

        // object _lock = new object();

        for(int j=0; j < frames; j++ )
        {
            // lock(_lock) {  
                double output = 0;
                // Parallel.For(0, this.Count, delegate(int i)  //For each note in the channel....
                for(int i=0; i<this.Count;i++)  //For each note in the channel....
                {
                    // lock(_lock) { output += patch.mix(this[i]); }  
                     output += patch.mix(this[i]);   
                } //);  //End note loop

                bufferdata[j].x = (float) (output * patch._panL * patch.gain * volume * _panL);  
                bufferdata[j].y = (float) (output * patch._panR * patch.gain * volume * _panR);  
                clockEvents(clockChannel); //Process the clock events for the frame.  
                                          //This delegate should only iterate the clock on one channel, to sync other channels to the audio frame.
            // }
        } //); //End buffer loop



        return bufferdata;
    }

    /// Provides a list of active note numbers for display usage.
    public int[] ActiveNotes()
    {
        var output = new Stack<int>(this.Count);

        foreach(Note note in this)
        {
            if (note.pressed) output.Push(note.midi_note);
        }

        return output.ToArray();
    }

    /// Manually turns off a note on a given channel.  Release time is dependent on patch if no ttl_override is given.
    public bool TurnOffNote(int note_number, int ttl_override=-1)
    {
        // Note note = GetNote(note_number);
        Note note = FindActiveNote(note_number);
        if (note==null) return false;
        // if (note==null) throw new NullReferenceException("Note not found?");
 
        var ttl = (ttl_override>-1) ?  ttl_override : patch.GetReleaseTime(note_number);
        note._on_ReleaseNote(note_number, ttl);
        return true;
    }

}