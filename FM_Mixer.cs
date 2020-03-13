using Godot;
using System;

// Speakers output.  This terminus requests the first operator with an empty list.
// equesting empty from an OP produces a straight waveform output without any FM.

public class FM_Mixer : Node
{
    // Godot.Collections.Dictionary connections = new Godot.Collections.Dictionary();  // This is filled in by the algorithm validator.
    Operator[] connections;  // This is filled in by the algorithm validator.

    public double mix(float phase){ 
        double avg = 0.0f;
        
        foreach (Operator op in connections)
        {	
            // var op = (GraphNodeOperator) GetNode("../" + o);
            avg += (float) op.request_sample(phase);  
        }

        //If assertion failed, we'd get a divide by zero here.
        System.Diagnostics.Debug.Assert(connections.Length > 0, "No connections to speaker. This shouldn't happen");

        avg /= connections.Length;  //Shitty average-based mixing.
        
        return avg;
    }
}
