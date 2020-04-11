using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public class Channel : List<Note>
{

    public Note FindActiveNote(int midi_note)
    {
        var n = midi_note;
        return Find((Note x) => (x.midi_note == n) && (x.pressed==true) && (x.releaseSample==0));
    }

    // private static bool isActiveNote(Note x)
    // {return (x.midi_note == midi_note) && (x.pressed) && (x.releaseSample==0);}

}

