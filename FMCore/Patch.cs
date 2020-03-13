using Godot;
using System;
using System.Collections.Generic;

//Todo:   PatchNote:Patch extension??  Might reduce confusion overall?
public class Patch : Resource
{
    protected int samples = 0;  //Sample timer 
    double hz = 440.0f;   //Sample pitch

    // This value should typically be initialized to whatever the global sample rate is.
    public static double sample_rate = 44100.0f;

    Operator[] connections = new Operator[0];  // This is filled in by the algorithm validator and used when mixing.
    Dictionary<String, Operator> operators = new Dictionary<string, Operator>();  // A list of all valid operators created by the algorithm validator.

    // Wires up a patch using a valid dictionary of connections from the algorithm validator. Format: {PatchName("Output"):{InputOpNames}, Operator1Name:{InputOps}, ...}
    //
    // TODO:This function clobbers existing operator envelope settings!  Maybe we should have
    //      a version of Patch which will always contain valid Operators and only replace their
    //      settings if explicitly reset and not just re-validated with a new algorithm.

    public bool WireUp(String input)
    {
        //Construct a dictionary from the input string.
        var dict = new Dictionary<String, String[]>();
        foreach(String opConnections in input.Split(";",false))
        {
            var kv = opConnections.Split("=", false);  //Key-Value pair (array of String[2])
            String[] values = kv[1].Split(",", false);  //List of connections to the operator named in kv[0]
            
            GD.Print("Adding " + kv[0].Trim() + " to dict");
            dict.Add( kv[0].Trim(), values);            
        }

        // Construct the operators for this patch.  Some operators don't have forward connections, so we must check all values in addition to keys.
        foreach (String k in dict.Keys)
        {
            foreach (String[] v in dict.Values) 
            {
                foreach(String s in v)  AddOp(s.Trim());
            } 
            AddOp(k);
        }

        // Now, wire them up to each other.
        foreach(string opString in dict.Keys)
        {
            var items= new List<Operator>();

            try
            {
                foreach(String s in dict[opString])   {
                    items.Add(operators[s]);
                                GD.Print("Adding " + s + " to " + opString + "'s connections");
                }
            }
            catch(Exception e){
                GD.PrintErr(e.ToString());
            }


            if (opString=="Output") 
            {
                connections = items.ToArray();
            } else {
                operators[opString].Connections = items.ToArray();
            }

        }

        return true;
    }

    private void AddOp(string name)
    {
        if (name=="Output") return;
        if (!operators.ContainsKey(name))  operators.Add(name, new Operator(name));
    }
    public Operator GetOperator(string name)
    {
        if (operators.ContainsKey(name))  return operators[name];
        return null;
    }

    public Envelope GetEG(string opName)
    {
        var op = GetOperator(opName);
        return op?.EG;
    }

    public void Reset(bool timer=true, bool connections=false){
        if (timer) this.samples = 0;
        if (connections) this.connections = null;
    }

    //This ctor can be used to set the sample rate used in phase calculations by a patch. The algorithm validator should do it when making a patch.
    public Patch(double sample_rate) => Patch.sample_rate = sample_rate;


    // For speaker output.  This terminus requests samples from the first set of operators.
    // This class also contains the envelope timer for its connections.
    public double mix(){
        return mix(samples / sample_rate * hz);
    }
    public double mix(double phase){ 
        double avg = 0.0f;
        
        foreach (Operator op in connections)
        {	
            avg += (double) op.request_sample(phase);  
        }

        //If assertion failed, we'd get a divide by zero here.
        if (connections.Length <= 0){
            var msg = "Patch.mix: No operator connections to patch. This shouldn't happen";
            GD.PrintErr(msg);
            throw new System.Exception(msg);
        }

        avg /= connections.Length;  //Shitty average-based mixing.        
        samples += 1;
        return avg;
    }
}
