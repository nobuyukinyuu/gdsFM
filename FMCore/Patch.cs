using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Patch : Resource
{

    #if GODOT  //Needs default ctor
        Patch() {}
    #endif

    // This value should typically be initialized to whatever the global sample rate is.
    public static double sample_rate = 44100.0;


    Operator[] connections = new Operator[0];  // This is filled in by the algorithm validator and used when mixing.
    Dictionary<String, Operator> operators = new Dictionary<string, Operator>();  // A list of all valid operators created by the algorithm validator.


    public float gain = 1.0f;  //Straight multiplier to the end output.  Use db2linear conversion.


    readonly int[][][] algorithms = new int[][][]{
        new int[][]{ new int[]{1,2}, new int[]{2,3}, new int[]{3,4}, new int[]{4,0} }, //Four serial connection
        new int[][]{ new int[]{1,3}, new int[]{2,3}, new int[]{3,4}, new int[]{4,0} }, //Three double mod serial connection
        new int[][]{ new int[]{1,4}, new int[]{2,3}, new int[]{3,4}, new int[]{4,0} }, //Double mod 1
        new int[][]{ new int[]{1,2}, new int[]{2,4}, new int[]{3,4}, new int[]{4,0} }, //Double mod 2
        new int[][]{ new int[]{1,2}, new int[]{2,0}, new int[]{3,4}, new int[]{4,0} }, //Two serial, two parallel
        new int[][]{ new int[]{1,2}, new int[]{1,3}, new int[]{1,4}, new int[]{2,0}, new int[]{3,0}, new int[]{4,0} }, //3x parallel, common modulator
        new int[][]{ new int[]{1,2}, new int[]{2,0}, new int[]{3,0}, new int[]{4,0} }, //Two serial, two sines
        new int[][]{ new int[]{1,0}, new int[]{2,0}, new int[]{3,0}, new int[]{4,0} }, //Four parallel sines
    };


    //Calls WireUp using a 4OP preset
    public bool WireUp(int algorithm)
    {
        var cs = algorithms[algorithm];  //Order 2 array of int

        var ops=new List<List<int>>();
        for(int i=0; i < 5; i++)    ops.Add(new List<int>());
        foreach (int[] pair in cs)  ops[pair[1]].Add(pair[0]);

        var output = "";
        for(int i=0; i < ops.Count; i++)
        {
            if (ops[i].Count == 0) continue;
            var inputs = "";
            foreach(int input in ops[i])
            {
                inputs +=  (input==0? "Output" : "OP"+input.ToString() ) + ",";
            }
            inputs = inputs.Remove(inputs.Length-1);
            output += String.Format("{0}={1};", i==0? "Output" : "OP"+i.ToString(), inputs);
        }
        output = output.Remove(output.Length-1);

        // System.Diagnostics.Debug.Print(output);
        return WireUp(output);
    }

    // Wires up a patch using a valid dictionary of connections from the algorithm validator. Format: "Operator1Name=InputOp1,InputOp2; OperatorName2= [...]"
    public bool WireUp(String input)
    {

        //Construct a dictionary from the input string.
        var dict = new Dictionary<String, String[]>();
        foreach(String opConnections in input.Split(";",false))
        {
            var kv = opConnections.Split("=", false);  //Key-Value pair (array of String[2])
            String[] values = kv[1].Split(",", false);  //List of connections to the operator named in kv[0]
            
            // GD.Print("Adding " + kv[0].Trim() + " to dict");
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
                                // GD.Print("Adding " + s + " to " + opString + "'s connections");
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
    //Used to determine the TTL of a note.  
    // public double GetReleaseTime(Operator[] connections, double lastReleaseTime=float.MaxValue) 
    public double GetReleaseTime(Note note=null) 
    {
        const int ONE_MINUTE = 2880000; //1 minute at 48000hz
        //Get the Max release time of all parallel operators connected to the current patch output/operator.
        double parallel_time = 0;  
        for (int i=0; i < connections.Length; i++) //Operator op in connections)
        {
            var ksr = (note!=null? connections[i].EG.ksr[note.midi_note] : 1.0);
            var rt = connections[i].EG._releaseTime * ksr;
            //Sometimes the release time can exceed the audible output of the note if sustain is shorter than infinite.  Choose the shorter of the times.
            rt = Math.Min(rt, connections[i].EG._ads * ksr);

            if (rt < ONE_MINUTE) parallel_time = Math.Max(parallel_time, rt);  //Now find the maximum of our ttl and the others.
        }

        if (parallel_time == 0) parallel_time = ONE_MINUTE;
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
                output[i] += op.request_sample(note.phase[op.id], note); 
            }

            // output[i] *= note.Velocity;   //TODO:  Apply master response curve instead.  Most velocity should be controlled in EG.
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


    //Copies an instrument (this Patch) in an envelope format understood by BambooTracker.
    public void BambooCopy(int algorithm = 0)
    {
        var output = "FM_ENVELOPE:";

        if (operators.ContainsKey("OP1"))  output += Math.Floor(Math.Min(7, operators["OP1"].EG.feedback)).ToString() + ",";
        output += algorithm.ToString() + ",\n";

        for(int i=0; i < Math.Min(4, operators.Count); i++)
        {
            output += operators["OP" + i.ToString()].EG.Ar.ToString();
            output += ",";
            output += operators["OP" + i.ToString()].EG.Dr.ToString();
            output += ",";
            output += operators["OP" + i.ToString()].EG.Sr.ToString();
            output += ",";
            output += operators["OP" + i.ToString()].EG.Rr.ToString();
            output += ",";
            output += operators["OP" + i.ToString()].EG.Sl.ToString();
            output += ",";
            output += operators["OP" + i.ToString()].EG.Tl.ToString();
            output += ",";
            output += operators["OP" + i.ToString()].EG.Ks.ToString();
            output += ",";
            output += Math.Floor(operators["OP" + i.ToString()].EG.Mul).ToString();
            output += ",";
            output += operators["OP" + i.ToString()].EG.Dt.ToString();  //TODO:  Map to 0-7
            output += ",-1,\n";

            OS.Clipboard = output;
        }
    }

    public int BambooPaste()
    {
        if (!OS.Clipboard.StartsWith("FM_ENVELOPE:")) return -1;

        var lines = OS.Clipboard.Substring(12).Split("\n", false);
        if (lines.Length < 5) return -1;

        int feedback = Int32.Parse(lines[0][0].ToString());
        int algorithm = Int32.Parse(lines[0][2].ToString());

        if (algorithm >= 0 && algorithm < 8)  WireUp(algorithm);

        operators["OP1"].EG.feedback = feedback / 2.0;  //FIXME:  Key scaling probably is necessary to stop extreme feedback. Deliberately dialed down as a temp workaround.

        //Go and try to populate the envelope generators.
        foreach(Operator op in operators.Values)
        {
            string[] val = lines[op.id + 1].Split(",", false);
            // AR,DR,SR,RR,SL,TL,KS,MUL,DT

            op.EG.waveform = Waveforms.SINE;
            op.EG.fmTechnique = Waveforms.SINE;
            op.EG.duty = 0.5;
            op.EG.UseDuty = false;

            op.EG.Ar = Int32.Parse(val[0]);
            op.EG.Dr = Int32.Parse(val[1]);
            op.EG.Sr = Int32.Parse(val[2]);
            op.EG.Rr = Int32.Parse(val[3]);

            //These mappings might not be perfect.  SL attenuation goes from 0-15 to -93dB, TL goes from 0-127 to -96dB.
            op.EG.Sl = GD.Db2Linear(-6.2f * (Int32.Parse(val[4]))) * 100 ;
            op.EG.Tl = GD.Db2Linear(-0.75f * Int32.Parse(val[5])) * 100 ;

            var remap= new int[] {100, 50, 25, 0};

            // op.EG.Ks = Int32.Parse(val[6]);  //TODO:  Map key scaling rates to 12th root of 2 defaults and scale down...
            op.EG.ksr = RTable.FromPreset<Double>(RTable.Presets.DESCENDING | RTable.Presets.TWELFTH_ROOT_OF_2, floor: remap[Int32.Parse(val[6])] );

            op.EG.Mul = Int32.Parse(val[7]);

            op.EG.Dt = (Int32.Parse(val[8]) - 3.5) * (100/3.5);  //This may not be correct either....
        }

        return algorithm;
    }

}
