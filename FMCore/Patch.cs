using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

//Todo:   PatchNote:Patch extension??  Might reduce confusion overall?
public class Patch : Resource
{

    #if GODOT  //Needs default ctor
        Patch() {}
    #endif

    // This value should typically be initialized to whatever the global sample rate is.
    public static double sample_rate = 44100.0;

    Operator[] connections = new Operator[0];  // This is filled in by the algorithm validator and used when mixing.
    Dictionary<String, Operator> operators = new Dictionary<string, Operator>();  // A list of all valid operators created by the algorithm validator.

    // Wires up a patch using a valid dictionary of connections from the algorithm validator. Format: {PatchName("Output"):{InputOpNames}, Operator1Name:{InputOps}, ...}
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

        // Construct the a list of operators for this patch.  Some operators don't have forward connections, so we must check all values in addition to keys.
        var validOps = new List<String>();
        foreach (String k in dict.Keys)
        {
            foreach (String[] v in dict.Values) 
            {
                foreach(String s in v)  validOps.Add(s.Trim());
            } 
            validOps.Add(k);
        }

        //Remove any unused operators by replacing the collection with a new one.
        //Sweep the old collection checking for operators to reuse.  Add any missing ones to the new one.
        var newOperators = new Dictionary<String, Operator>();
        foreach(String item in validOps)
        {
            if (item=="Output")  continue;
            if (operators.ContainsKey(item))  //The operator exists in the previous collection.
            {
                if (newOperators.ContainsKey(item)) continue;  //We've already added this operator.
                newOperators.Add(item, operators[item]);
                operators[item].Connections = new Operator[0];

            } else {  //No operator existed in the previous configuration.  Add it.
                AddOp(item, newOperators);
            }
        }
        operators = newOperators;   //Replace the old op collection.  Old collection gets GC'd.


        // Now, wire the Operators up to each other.
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

    private void AddOp(string name, Dictionary<String, Operator> collection)
    {
        if (name=="Output") return;
        if (!collection.ContainsKey(name))  collection.Add(name, new Operator(name));
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

    //Total max release time of all operators in the patch, measured in samples.
    // public double GetReleaseTime(Operator[] connections, double lastReleaseTime=float.MaxValue) 
    public double GetReleaseTime() 
    {
        //Get the Max release time of all parallel operators connected to the current patch output/operator.
        double parallel_time = 0;  
        for (int i=0; i < connections.Length; i++) //Operator op in connections)
        {
            parallel_time = Math.Max(parallel_time, connections[i].EG._releaseTime);            
        }

        return parallel_time;
    }

    public void ResetConnections(){
        this.connections = null;
    }
    public void ResetOperator(string opname){
        //Nothing here yet.  
    }
    public void ResetOperators()
    {
        //Nothing here yet.  Would reset EG, curves, waveforms etc.
    }

    //This ctor can be used to set the sample rate used in phase calculations by a patch. The algorithm validator should do it when making a patch.
    public Patch(double sample_rate) => Patch.sample_rate = sample_rate;
    // public Patch() {}  //Default constructor.


    //Request multiple samples from this patch for a requested note.  Maybe better for parallelizing notes?
    public double[] request_samples(Note note, int nSamples=1)
    {
        var output = new double[nSamples];
        for(int i=0; i < nSamples; i++)
        {
            for (int j=0; j < connections.Length; j++)
            {	
                Operator op = connections[j];
                output[i] = op.request_sample(note.phase[op.id], note); 
            }
            note.Iterate(1);
        }
        return output;
    }

    //Multiple note polyphony
    public double mix(List<Note> notes)
    {
        double output = 0.0;
        
        // TODO!!!!!!!! FIX CONCURRENT LIST MODIFICATION IN SEPARATE THREADS.  FIXME FIXME FIXME
        // NOTES COME IN AS AN EVENT ON A SEPARATE THREAD, SO MODIFICATION (NOTE DELETION) MUST BE QUEUED TO THE NEXT SYNCED PROCESS THREAD
        for (int i = 0; i < notes.Count; i++) //Note note in notes)
        // Parallel.For (0, notes.Count, delegate(int i) 
        {
            if (i >= notes.Count || notes[i] == null) continue;
            // if (notes[i] ==null) return;
            output += mix(notes[i]);
    
        //    //Check if the note needs to die.
        //     if (notes[i].IsDestroyable())  notes[i].Destroy();

        } 


        // Shitty average based mixing of total notes being played again.  Count is an O(1) operation. 
        output /= 2;  //TODO:  Maybe don't do this?  Figure out what the velocity is like normally, maybe scale down a tiny bit instead.
        return output;
    }

    //TODO:  Figure out why this is slow and if it is actually processing the tasks in parallel
    //      Consider trying to spawn a bunch of tasks in a for loop and somehow await the results of all before continuing
    public double mixAsync(List<Note> notes)
    {
        var task = Task<Double>.Run(  () => 
            {
                double output=0;  //Why do I have to declare the initial value in a value type? Is it because this is a lambda?  Why?
                for (int i = 0; i < notes.Count; i++)
                {
                    if (notes[i] ==null) continue;
                    // if (notes[i] ==null) return;
                    output += mix(notes[i]);
                } 
                return output;
            } );

        return task.Result / 2;
    }

    public double mix(Note note){ 
        double avg = 0.0;  //Output average

        foreach (Operator op in connections)
        // Parallel.ForEach(connections, delegate(Operator op)
        {	
            //Get running average of sample output of all operators connected to output.
            avg += op.request_sample(note.phase[op.id], note); 

            //Iterate the sample timer.
            // note.Iterate(op.id, 1, op.EG.totalMultiplier, sample_rate);
            // note.Accumulate(op.id,1, op.EG.totalMultiplier, op.EG.SampleRate);
            // note.Iterate(1);

        } //);
        note.Iterate(1);

        #if DEBUG  //We probably don't need this in release mode
            //If assertion failed, we'd get a divide by zero here.
            if (connections.Length <= 0){
                var msg = "Patch.mix: No operator connections to patch. This shouldn't happen";
                GD.PrintErr(msg);
                throw new System.Exception(msg);
            }
        #endif

        avg /= connections.Length;  //Shitty average-based mixing.        

        avg *=  note.Velocity;  // TODO:  REMOVE ME WHEN PER-OPERATOR VELOCITY SENSITIVITY IS IMPLEMENTED?

        // note.Iterate(1, pitch_avg, sample_rate);

        

        return avg;
    }
}
