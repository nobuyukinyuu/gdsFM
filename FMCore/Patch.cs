using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GdsFMJson;

public class Patch : Resource
{

    #if GODOT  //Needs default ctor
        Patch() {InitWaveformBank();}
    #endif

    //This ctor can be used to set the sample rate used in phase calculations by a patch. The algorithm validator should do it when making a patch.
    //TODO:  This ctor should also initialize the LFOs with the sample rate given, if LFOs are always allocated.....
    public Patch(double sample_rate) { Patch.sample_rate = sample_rate; 
                                      LFO.sample_rate = sample_rate; RbjFilter.sample_rate = sample_rate; InitWaveformBank();}


    // This value should typically be initialized to whatever the global sample rate is.
    public static double sample_rate = 44100.0;
    public double pitchMod = 1.0;  //Global pitch modifier.  Used by audio output to modify the pitch of samples requested.

    public const int VERSION = 1;  // Used to determine which version of the patch instrument this is, for forwards compatibility.
    public const string _iotype = "patch";
    public string name="";
    public string wiring="";  //Set to a valid WireUp() string.  Used to reconnect connections when copypasting or loading from external.
    Operator[] connections = new Operator[0];  // This is filled in by the algorithm validator and used when mixing.
    Dictionary<String, Operator> operators = new Dictionary<string, Operator>();  // A list of all valid operators created by the algorithm validator.


    public float gain = 1.0f;  //Straight multiplier to the end output.  Use db2linear conversion.
    public float Gain {get => GD.Linear2Db(gain); set => GD.Db2Linear(value);}  //Convenience func
    public double transpose = 1.0;  //Master tuning
    public double Transpose {get => Math.Log(transpose, 2) * 12; set => transpose = Math.Pow(2, value/12);}  //Convenience func

    public RbjFilter filter = new RbjFilter(sample_rate);
    public FormantFilter formantFilter = new FormantFilter(sample_rate);

    //List of LFOs available for operators to use.  3 initialized at first.  Operator will attempt to procure an LFO reference from list if its bank > -1.
    public List<LFO> LFOs = new List<LFO>( new LFO[] {new LFO(sample_rate), new LFO(sample_rate), new LFO(sample_rate)} );

#region Stereo Panning ======
    public const float C_PAN = 0.5f;  //Center panning value each side lerps to.
    public float _panL=1.0f, _panR=1.0f;  //Stereo panning multipliers

    float pan = 0.0f;
    public float Pan {get => pan*100; set => SetPanning(value / 100f);}  /// Gets or sets a value from -100 to 100.

 
    /// Takes a value from -1.0 to 1.0 and assigns _panL and _panR to respective values depending on center channel volume C_PAN.
    public void SetPanning(float val)
    {
        pan = val;
        var amt = Math.Abs(val);
        float l,r;

        if (val < 0) {  //Pan left channel
            l = amt;
            r = 1.0f-amt;

            _panL = GDSFmFuncs.lerp(C_PAN, 1.0f, l);
            _panR = GDSFmFuncs.lerp(0.0f, C_PAN, r);
            return;

        } else if (val > 0) {  //Pan right
            l = 1.0f-amt;
            r = amt;

            _panL = GDSFmFuncs.lerp(0.0f, C_PAN, l);
            _panR = GDSFmFuncs.lerp(C_PAN, 1.0f, r);
            return;    

        } else {  //Center channel.
            _panL = C_PAN;
            _panR = C_PAN;
            return;
        }
    }
#endregion


#region Pitch generator.  Rates must always be positive
    public double pal,pdl,psl,prl;  //Levels, from -100 to 100.
    public double par,pdr,prr;  //Rates, in samples.

    public double _padr;  //Precalculated attack and decay times combined.
    public double palMult=1, pdlMult=1, pslMult=1, prlMult=1;  //Multipliers to reference level

    public double Par { get => par / sample_rate * 1000.0; set {par = (value * sample_rate / 1000.0); _padr=par+pdr;} }
    public double Pdr { get => pdr / sample_rate * 1000.0; set {pdr = (value * sample_rate / 1000.0);  _padr=par+pdr;} }
    public double Prr { get => prr / sample_rate * 1000.0; set => prr = (value * sample_rate / 1000.0); }
 
    //Rates, in ms.
    public double Pal { get => pal; set => pal = _calcPitch(ref palMult, value); }
    public double Pdl { get => pdl; set => pdl = _calcPitch(ref pdlMult, value); }
    public double Psl { get => psl; set => psl = _calcPitch(ref pslMult, value); }
    public double Prl { get => prl; set => prl = _calcPitch(ref prlMult, value); }

    /// Used by pitch level properties to translate values into proper multipliers
    private double _calcPitch(ref double field, double value)
    {

        var amt = value / 100.0; 
        amt = Math.Pow(2, 2*amt) ;  //Magic scaling integer to multiplier

        field = amt;
        return value;
    }
    /// Recalculates the multipliers and _padr precalcs for the entire pitch gen. Useful for loads.
    private void RecalcPitchGen()
    {
        _padr = par + pdr;
        _calcPitch(ref palMult, pal);
        _calcPitch(ref pdlMult, pdl);
        _calcPitch(ref pslMult, psl);
        _calcPitch(ref prlMult, prl);
    }
#endregion

    //Algorithm presets
    readonly static int[][][] algorithms = new int[][][]{
        new int[][]{ new int[]{1,2}, new int[]{2,3}, new int[]{3,4}, new int[]{4,0} }, //Four serial connection
        new int[][]{ new int[]{1,3}, new int[]{2,3}, new int[]{3,4}, new int[]{4,0} }, //Three double mod serial connection
        new int[][]{ new int[]{1,4}, new int[]{2,3}, new int[]{3,4}, new int[]{4,0} }, //Double mod 1
        new int[][]{ new int[]{1,2}, new int[]{2,4}, new int[]{3,4}, new int[]{4,0} }, //Double mod 2
        new int[][]{ new int[]{1,2}, new int[]{2,0}, new int[]{3,4}, new int[]{4,0} }, //Two serial, two parallel
        new int[][]{ new int[]{1,2}, new int[]{1,3}, new int[]{1,4}, new int[]{2,0}, new int[]{3,0}, new int[]{4,0} }, //3x parallel, common modulator
        new int[][]{ new int[]{1,2}, new int[]{2,0}, new int[]{3,0}, new int[]{4,0} }, //Two serial, two sines
        new int[][]{ new int[]{1,0}, new int[]{2,0}, new int[]{3,0}, new int[]{4,0} }, //Four parallel sines
    };


#region Custom Waveform bank stuff
    public List<RTable<double>> customWaveforms = new List<RTable<double>>();
    public int WaveformBankSize {get => customWaveforms.Count;}
    public bool isValidWaveformBank (int idx){
        if (idx < 0 || idx>=customWaveforms.Count) return false;
        return true;
    }

    public void ResetWaveformBanks() {customWaveforms.Clear();  InitWaveformBank();}
    public void InitWaveformBank() {if (customWaveforms.Count == 0) AddWaveformBank();}
    public void AddWaveformBank() {customWaveforms.Add(RTable.FromPreset<Double>(RTable.Presets.FIFTY_PERCENT));}
    public Godot.Collections.Dictionary GetWaveformBank (int idx, bool returnDefault=false) 
    {
        if (!isValidWaveformBank(idx)) {
            if (returnDefault)  //Return a default table, probably for the operator's preview button.
            {return RTable.FromPreset<Double>(RTable.Presets.ZERO).ToDict();}
            return null;
        }
        return customWaveforms[idx].ToDict();
    }
    public void SetWaveformBank (int idx, Godot.Collections.Dictionary value) {
        if (!isValidWaveformBank(idx)) return; 
        customWaveforms[idx].SetValues(value);
        }

    public void SetWaveformBank (int idx, JSONObject input)
    {
        if (!isValidWaveformBank(idx)) return;
        customWaveforms[idx].SetValues(input);
    }

    public void RemoveWaveformBank(int idx=-1)
    {
        if (customWaveforms.Count <=1)  return;
        if (idx==-1) idx = customWaveforms.Count-1;

        var table = customWaveforms[idx];

        //Make sure no operators are using the table anymore.
        foreach (Operator op in operators.Values)
        {
            if (op.customWaveform == table) {
                op.customWaveform = null;
                op.waveformBank = -1;
            }
        }
        customWaveforms.RemoveAt(idx);  //This should hopefully dispose of the waveform now that references to it are gone.
    }

    public void SetWaveformValue(int bank, int index, double value)  //Sets a single value in the destination waveform for immediate feedback.
    {
        customWaveforms[bank].SetValue(index, value);
    }

    public void LoadCustomWaveform(int bank, string opname)  //Loads a waveform into the specified operator for use.
    {
        var op = GetOperator(opname);
        if (op==null){
            GD.Print("Patch.LoadCustomWaveform:  Can't find operator ", opname);
            return;
        }

        if (isValidWaveformBank(bank)){
            op.customWaveform = customWaveforms[bank];
            op.waveformBank = bank;
        } else {
            // GD.Print("Patch.LoadCustomWaveform:  Invalid bank ", bank);
            op.customWaveform = null;
            op.waveformBank = -1;
        }
    }

    /// Used by godot frontend to get a waveform bank's rTable values.
    public string CopyWaveformBank(int idx)
    {
        var output = customWaveforms[idx].ToString();
        OS.Clipboard = output;
        return output;
    }
    /// Used by godot frontend to set a waveform bank's rTable values.
    public int PasteWaveformBank(int idx, bool tableOnly)
    {
        return customWaveforms[idx].FromString(OS.Clipboard, tableOnly);
    }
#endregion


    ///  Calls WireUp using a 4OP preset.  Typically a value from 0-7.
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

    /// Wires up a patch using a valid dictionary of connections from the algorithm validator. Format: "Operator1Name=InputOp1,InputOp2; OperatorName2= [...]"
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

        wiring = input;
        return true;
    }

    ///  Adds a new operator to the specified collection.  Used when wiring up an algorithm from Godot.
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

    /// Used by Godot to fetch an envelope.
    public Envelope GetEG(string opName)
    {
        var op = GetOperator(opName);
        return op?.EG;
    }
    ///  Used by Godot to fetch an LFO from our banks.
    public LFO GetLFO(int bank)
    {
        if ( bank<0 || bank>=LFOs.Count) return null;
        return LFOs[bank];
    }


    ///  Total max release time of all operators in the patch, measured in samples.
    /// Used to determine the TTL of a note.  Note
    // public double GetReleaseTime(Operator[] connections, double lastReleaseTime=float.MaxValue) 
    public int GetReleaseTime(int note_number=0) 
    {
        const int ONE_MINUTE = 2880000; //1 minute at 48000hz
        //Get the Max release time of all parallel operators connected to the current patch output/operator.
        double parallel_time = 0;  
        for (int i=0; i < connections.Length; i++) //Operator op in connections)
        {
            var ksr = connections[i].EG.ksr[note_number];
            var rt = connections[i].EG._releaseTime * ksr;
            //Sometimes the release time can exceed the audible output of the note if sustain is shorter than infinite.  Choose the shorter of the times.
            rt = Math.Min(rt, connections[i].EG._ads * ksr);

            if (rt < ONE_MINUTE) parallel_time = Math.Max(parallel_time, rt);  //Now find the maximum of our ttl and the others.
        }

        if (parallel_time == 0) parallel_time = ONE_MINUTE;
        return (int) Math.Ceiling(parallel_time);
    }



    public void ResetConnections(){
        this.connections = null;
    }
    public void ResetOperator(string opname){
        //TODO:  Nothing here yet.  
    }
    public void ResetOperators()
    {
        //TODO:  Nothing here yet.  Would reset EG, curves, waveforms etc.
    }


    public void UpdateLFOs(int buffer_size=8192)
    {
        //TODO:  Make this a parallel process?
        foreach(LFO lfo in LFOs)
        {
            lfo.UpdateBuffer(Math.Max(1,buffer_size));
        }

    }

    /// Request multiple samples from this patch for a requested note.  Maybe better for parallelizing notes?
    public double[] request_samples(Note note, int nSamples=1, double pitchMod=1.0) //TODO: external timer funcref + execution frequency arguments here.
    {
        var output = new double[nSamples];
        for(int i=0; i < nSamples; i++)
        {
            for (int j=0; j < connections.Length; j++)
            {	
                Operator op = connections[j];
                output[i] += op.request_sample(note.phase[op.id], note, LFOs, i); 
            }

            // output[i] = filter.Filter((float) output[i]);  //Not threadsafe.  Don't do this

            // output[i] *= note.Velocity;   //TODO:  Apply master response curve here instead.  Most velocity should be controlled in EG.

            //Recalculate the pitch.
            note.hz = note.base_hz * note.PitchAtSamplePosition(this) * pitchMod * this.pitchMod * transpose;

        #if DEBUG
        if(Double.IsNaN(note.hz)) 
        {
            System.Diagnostics.Debugger.Break();  //Stop. NaN propagation. This should never trigger but if it does prepare for pain
            // throw new ArithmeticException("NaN encountered in calculated envelope output");
        }
        #endif


            //Iterate the sample timer.  The phase accumulators were already called from the Operators.
            note.Iterate(1);
        }

        return output;
    }

    /// Multiple note polyphony.  Old method for original single-thread fill_buffer
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
        // output /= 2;  //TODO:  Maybe don't do this?  Figure out what the velocity is like normally, maybe scale down a tiny bit instead.
        return output;
    }

    /// Requests a single sample frame from a note.
    public double mix(Note note){ 
        double avg = 0.0;  //Output average

        // foreach (Operator op in connections)
        // // Parallel.ForEach(connections, delegate(Operator op)
        // {	
        //     //Get running average of sample output of all operators connected to output.
        //     avg += op.request_sample(note.phase[op.id], note); 

        //     //Iterate the sample timer.
        //     // note.Iterate(op.id, 1, op.EG.totalMultiplier, sample_rate);
        //     // note.Accumulate(op.id,1, op.EG.totalMultiplier, op.EG.SampleRate);
        //     // note.Iterate(1);

        // } //);

        for (int j=0; j < connections.Length; j++)
        {	
            Operator op = connections[j];
            //FIXME:  lfoBufPos probably shouldn't be 0 maybe?  Try and remember what it does...
            avg += op.request_sample(note.phase[op.id], note, LFOs, 0); 
        }

        //Recalculate the pitch.
        note.hz = note.base_hz * note.PitchAtSamplePosition(this) * pitchMod * this.pitchMod * transpose;
        note.Iterate(1);
        return avg;
    }


    public void SetFilter(Godot.Collections.Array input, bool reset)
    {
        if (reset) filter.Reset();
        var t= (FilterType) Enum.ToObject(typeof(FilterType), input[0]);
        var freq = (float) input[1];
        var q = (float) input[2];

        // GD.Print("Filter set ", t);

        filter.calc_filter_coeffs( t, freq, q, 0, false);
    }

    public void SetFormant(Godot.Collections.Array input, float q, bool reset)
    {
        if (reset) formantFilter.Reset();

        formantFilter.peak0 = (float) input[0];
        formantFilter.peak1 = (float) input[1];
        formantFilter.q = q;


        formantFilter.Recalc();
    }


#region ====================== IO ======================
    public JSONObject JsonMetadata(PatchCopyFlags flags=PatchCopyFlags.ALL)
    {  
        //Begin constructing a new json object.
        JSONObject output = new JSONObject();

        //Add headers.
        if (flags.HasFlag(PatchCopyFlags.GENERAL)){
            output.AddPrim("name", name);
            output.AddPrim("gain", gain);
            output.AddPrim("transpose", transpose);
            output.AddPrim("pan", pan);  //Remember to recalculate this when reloading
        }

        //Get the operators.
        if (flags.HasFlag(PatchCopyFlags.OPS)){
            var ops = new JSONObject();
            //Describe the wiring.
            output.AddPrim("wiring", wiring);

            //Get the data for each operator.
            foreach (Operator op in operators.Values)
            {
                // var op = connections[i];
                var p = op.JsonMetadata(OPCopyFlags.ALL);
                ops.AddItem(op.name, p);
            }
            output.AddItem("operators", ops);
        }

        if (flags.HasFlag(PatchCopyFlags.PG))  //Pitch gen. When reloading don't forget to recalc the values not saved
        {
            output.AddPrim("pal", pal);
            output.AddPrim("pdl", pdl);
            output.AddPrim("psl", psl);
            output.AddPrim("prl", prl);

            //These values are calculated from the sample length back into the millisec length.  FIXME:  There may be drift over time..?
            output.AddPrim("par", Par);
            output.AddPrim("pdr", Pdr);
            output.AddPrim("prr", Prr);
        }

        if (flags.HasFlag(PatchCopyFlags.LFO))  //LFO banks.
        {

            var lfout = new JSONArray();
            for(int i=0; i < LFOs.Count; i++)
            {
                // lfout.AddItem("ksr", JSONData.ReadJSON(ksr.ToString()));
                lfout.AddItem(LFOs[i].JsonMetadata());
            } 
            output.AddItem("LFOs", lfout);
        }

        if (flags.HasFlag(PatchCopyFlags.WAVEFORMS)) //Waveform banks.
        {
            var p = new JSONArray();
            for(int i=0; i < customWaveforms.Count; i++)
            {
                // lfout.AddItem("ksr", JSONData.ReadJSON(ksr.ToString()));
                p.AddItem(customWaveforms[i].JsonMetadata());
            } 
            output.AddItem("customWaveforms", p);

        }


        return output;
    }

    /// Outputs a representation of this patch which can be used with clipboard and io operations.
    public override string ToString()
    {
        var output=JsonMetadata();
        output.AddPrim("_iotype", _iotype);
        return output.ToJSONString();
    }

    /// Combine with your io method of choice to save a file... Like ToString, but with version data.
    public string IOString()
    {
        var output=JsonMetadata();
        output.AddPrim("_iotype", _iotype);
        output.AddPrim("_version", VERSION);
        return output.ToJSONString();
    }


    /// Attempts to load a JSON string into the patch data.  Ignores version data;  useful for clipboard paste operations where data is assumed to be fresh.
    public int FromString(string input) { return FromString(input, false);}

    /// Attempts to load a JSON string into the patch data.
    public int FromString(string input, bool ignoreVersion)
    {
        var p = JSONData.ReadJSON(input);
        if (p is JSONDataError) return -1;  // JSON malformed.  Exit early.
        
        var j = (JSONObject) p;

        if (!ignoreVersion){
            var ver = j.GetItem("_version", -1);
            if (ver < VERSION) {GD.Print("Patch.FromString:  Unknown version number " , ver); return -2;}
            else if (ver > VERSION) GD.Print("Patch.FromString:  WARNING, newer patch version", ver ,"detected. Some settings may not load correctly.");
        }
        if (j.GetItem("_iotype", "") != _iotype) {
            GD.Print("Patch.FromString:  Incorrect iotype.  Expected 'patch', got '", j.GetItem("_iotype", ""), "'.");
            return -3;  //Incorrect iotype.  Exit early.
        }

        //If we got this far, the data is probably okay.  Let's try parsing it.......
        var idk = new List<string>();
        try
        {

            //Globals
            j.Assign("name", ref name);
            j.Assign("gain", ref gain);
            j.Assign("transpose", ref transpose);
            if (j.Assign("pan", ref pan))  SetPanning(pan);


            //Setup the waveform banks.
            if (j.HasItem("customWaveforms"))
            {
                var bankData = (JSONArray) j.GetItem("customWaveforms");
                customWaveforms.Clear();

                for(int i=0; i < bankData.Length; i++)
                {
                    AddWaveformBank();
                    SetWaveformBank(i, (JSONObject) bankData[i]);
                }

                //Make sure that if there's no custom waveform banks to re-initialize with a blank one.
                if (customWaveforms.Count == 0) AddWaveformBank();   
            }
           

            //Operators
            if (j.HasItem("wiring"))
            {
                WireUp(j.GetItem("wiring", ""));
                var ops = (JSONObject) j.GetItem("operators");

                //Get the data for each operator.
                foreach (Operator op in operators.Values)
                {
                  //TODO:  Look for each operator here and populate it.
                  if (!ops.HasItem(op.name))  continue;
                  var opData = (JSONObject) ops.GetItem(op.name);
                    op.FromString(opData.ToJSONString(), true);  // Serialized files don't have IO types for individual envelopes. We assume the data is valid.

                   //Operators can't grab data from a patch's databank by itself.  Feed it the bank that should be appropriate for it.
                   LoadCustomWaveform(op.waveformBank, op.name);

                }

            }

            //Populate the pitch generator.
            j.Assign("pal", ref pal);
            j.Assign("pdl", ref pdl);
            j.Assign("psl", ref psl);
            j.Assign("prl", ref prl);

            //We can't use Assign() to automatically recalculate rates because properties can't be passed as a ref param, so do it manually.
            Par = j.GetItem("par", 0);
            Pdr = j.GetItem("pdr", 0);
            Prr = j.GetItem("prr", 0);

            // j.Assign("par", ref Par);
            // j.Assign("pdr", ref Pdr);
            // j.Assign("prr", ref Prr);

            RecalcPitchGen();

            //Populate the LFOs.  
            var lfoData = j.HasItem("LFOs") ? (JSONArray) j.GetItem("LFOs") : null;
            if (lfoData != null)
            {
                var lfos = new List<LFO>();
                foreach (JSONDataItem dataItem in lfoData)
                {
                    var lfo = new LFO(sample_rate);
                    int err = lfo.ParseJson((JSONObject) dataItem);
                    if (err==0)  lfos.Add(lfo);
                }

                if (lfos.Count > 0)  {LFOs = lfos;}
                else { LFOs = new List<LFO>( new LFO[] {new LFO(sample_rate), new LFO(sample_rate), new LFO(sample_rate)}); }
            }

            // if (needsRecalc) recalc_adsr();
            // GD.Print( String.Format("Success: {0},  Failure: {1}, Unknown fields: {2}", okay, oops, idk.Contents()));



        } catch {
            return -1;  // JSON malformed or another error.
        }

        return 0;
    }

    /// Copies an instrument (this Patch) in an envelope format understood by BambooTracker.
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

    /// Pastes envelope data from BambooTracker in a format understood by gdsFM.
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

    /// Returns a string with some basic information about the patch.
    public string GetInfo()
    {
        var sw= new System.IO.StringWriter();

        sw.WriteLine("Current Patch:  " + name);

        // sw.WriteLine("Connections: " + connections.Length.ToString());

        for(int i=0; i<connections.Length;i++)
        {
            sw.WriteLine(connections[i].ConnectionInfo());
        }

        sw.WriteLine("Operators: " + operators.Count.ToString());
        return sw.ToString();
    }
#endregion  //IO




    public string GetMultipliers()  //Test for chainMul
    {
        var sb = new System.Text.StringBuilder("");
        foreach(Operator op in operators.Values)
        {
            sb.Append ("(");
            sb.Append (op.name);
            sb.Append(", ");
            sb.Append(op.LastChain);
            sb.Append(") ");
        }

        return sb.ToString();
    }

}
