using Godot;
using System;

//Response / Rate Table.  128 values in either 8-bit format (MIDI velocity, etc) or floating-point
public class RTable<T> : Resource
{
    public enum Presets {ZERO, FIFTY_PERCENT, ONE, IN, OUT, IN_OUT, TWELFTH_ROOT_OF_2}

    public T[] precalc_values = new T[128];  //Calculated from values
    public T[] values = new T[128];
    public double floor, ceiling=100;
    public bool use_log = false;

    public RTable() {}

    //Indexer for the main array
    // public T this[int i]
    // {
    //     get{
    //         return precalc_values[i];
    //     }set{
    //         precalc_values[i] = value;
    //     }
    // }

    //Outputs a JSON-compatible string for marshalling to/from gdscript
    public override string ToString()
    {
        var output = new System.Text.StringBuilder("{'values'=[", 1024);

        for(int i=0; i < values.Length; i++)
        {
            output.Append(values[i]).Append(",");
        }

        output.Append(", 'floor'=").Append(floor*100.0);
        output.Append(", 'ceiling'=").Append(ceiling*100.0);
        output.Append(", 'use_log'=").Append(use_log);
        output.Append("]}");

        return output.ToString();
    }

    public void Reverse()
    {
        Array.Reverse(values);
    }

    public void RecalcValues() 
    {
        for (int i=0; i < values.Length;  i++)
        {
            //Range-remap the values from 0-1 to fit within the floor and ceiling. This reduces recalcs when fetching
            //TODO:  Remap linLog rescaling!!!!

            //Presume values is float or double instead of T. Precalc values will have to be T and upper limits defined based on type.
            // if (typeof(T) == typeof(float) || typeof(T) == typeof(double) )
                var val = Convert.ToDouble( values[i] );
                val = val * (ceiling/100.0);  //Apply ceiling.
                val = (floor/100.0) +  val * (1.0-(floor/100.0));  //Apply floor.

                // System.Diagnostics.Debug.Assert( 
                //             GDSFmFuncs.TryChangeType<Double, T>(val, out precalc_values[i] ) 
                //         );

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(byte) )
                {
                    precalc_values[i] = (T) Convert.ChangeType(val, typeof(T));
                } else {
                    throw new ArrayTypeMismatchException("RTable can't convert to this instance type (" + typeof(T).ToString() + ")");
                }

        }
    }
}

public static class RTable
{
    public enum Presets {ZERO, FIFTY_PERCENT, MAX, LINEAR, IN, OUT, IN_OUT, TWELFTH_ROOT_OF_2, DESCENDING=0x100, FLOOR100=0x1000}
    static readonly System.Collections.Generic.Dictionary<Presets, Double> curveMap = new System.Collections.Generic.Dictionary<Presets, double>
    {
        [Presets.ZERO] = 0,
        [Presets.LINEAR] = 1,
        [Presets.IN] = 2.0,
        [Presets.OUT] = 0.5,
        [Presets.IN_OUT] = -2.0,
    };

    public static RTable<U> FromPreset<U>(Presets preset) where U:struct
    {
        var output = new RTable<U>();
        switch (preset)
        {
            case Presets.MAX:
            case Presets.MAX|Presets.DESCENDING:
                if (typeof(U) == typeof(float) || typeof(U) == typeof(double) )
                {
                    for (int i=0; i<128; i++)   output.values[i] = (U) Convert.ChangeType(1, typeof(U));
                } else if (typeof(U).IsValueType==true) {  //Integer or Byte or Short
                    for (int i=0; i<128; i++)   output.values[i] = (U) Convert.ChangeType(127, typeof(U));
                }
                break;
            case Presets.FIFTY_PERCENT:
            case Presets.FIFTY_PERCENT|Presets.DESCENDING:
                if (typeof(U) == typeof(float) || typeof(U) == typeof(double) )
                {
                    for (int i=0; i<128; i++)   output.values[i] = (U) Convert.ChangeType(0.5, typeof(U));
                } else if (typeof(U).IsValueType==true) {  //Integer or Byte or Short
                    for (int i=0; i<128; i++)   output.values[i] = (U) Convert.ChangeType(63, typeof(U));
                }
                break;
            case Presets.LINEAR:
            case Presets.LINEAR|Presets.DESCENDING:
                if (typeof(U) == typeof(float) || typeof(U) == typeof(double) )
                {
                    for (int i=0; i<128; i++)   output.values[i] = (U) Convert.ChangeType(i/127.0, typeof(U));
                } else if (typeof(U).IsValueType==true) {  //Integer or Byte or Short
                    for (int i=0; i<128; i++)   output.values[i] = (U) Convert.ChangeType(i, typeof(U));
                }
                break;
            case Presets.IN: case Presets.OUT: case Presets.IN_OUT:
            case Presets.IN|Presets.DESCENDING: case Presets.OUT|Presets.DESCENDING: case Presets.IN_OUT|Presets.DESCENDING:
                if (typeof(U) == typeof(float) || typeof(U) == typeof(double) )
                {
                    for (int i=0; i<128; i++)   output.values[i] = (U) Convert.ChangeType(GDSFmFuncs.Ease(0,1, i/127.0 , curveMap[preset]) , typeof(U));
                } else if (typeof(U).IsValueType==true) {  //Integer or Byte or Short
                    for (int i=0; i<128; i++)   output.values[i] = (U) Convert.ChangeType(GDSFmFuncs.Ease(0,127, i/127.0 , curveMap[preset]) , typeof(U));
                }
                break;
            case Presets.TWELFTH_ROOT_OF_2:
            case Presets.TWELFTH_ROOT_OF_2|Presets.DESCENDING:
            case Presets.TWELFTH_ROOT_OF_2|Presets.DESCENDING|Presets.FLOOR100:
                if (typeof(U) == typeof(float) || typeof(U) == typeof(double) )
                {
                    for (int i=0; i<128; i++)   output.values[i] = (U) Convert.ChangeType(Pow12th(i), typeof(U));
                } else if (typeof(U).IsValueType==true) {  //Integer or Byte or Short
                    for (int i=0; i<128; i++)   output.values[i] = (U) Convert.ChangeType(Pow12th(i) * 127, typeof(U));
                }
                break;

        }
        if ((preset & Presets.DESCENDING) == Presets.DESCENDING)  output.Reverse();
        if ((preset & Presets.FLOOR100) == Presets.FLOOR100)  output.floor=100;  //Set the value floor to 100%. Makes curve always return max.

        output.RecalcValues();
        return output;
    }

    //Returns a value from 0-1 with input x in the range 0-127 following 12th root of 2 exponential curvature.  Useful for octave keyscaled curves.
    static double Pow12th(double x)
    {
        return Math.Pow(2, (x-127)/12.0);
    }
}

public static class RTableExtensions
{

    //Type-specific value assignment from an input string
    //takes a string of characters in format:  "[0,1,2,.....127]"
    public static void SetValues(this RTable<int> instance, string input)
    {
        var s = input.Substring(1,input.Length-2).Split(",",false);

        for(int i=0; i < instance.values.Length; i++)
        {
            instance.values[i] = Convert.ToByte(s[i]);
        }
    }
    public static void SetValues(this RTable<double> instance, string input)
    {
        var s = input.Substring(1,input.Length-2).SplitFloats(",",false);

        for (int i=0; i < s.Length; i++)
        {
           instance.values[i] = s[i];
        }
    }

    //Input from a float source, value's coming in from 0-100 and should be tamped down to 0-1.
    public static void SetValues(this RTable<double> instance, Godot.Collections.Dictionary input)
    {
        GD.Print("attempting to assign RTable.......");
        GD.Print(input["values"].GetType());
        //Structure of dict:   {values=[PoolRealArray tbl], floor=0, ceiling=100, use_log=false...}
        instance.floor = Convert.ToDouble( input["floor"] ) ;
        instance.ceiling = Convert.ToDouble( input["ceiling"] ) ;
        instance.use_log = Convert.ToBoolean( input["use_log"] );

        //Convert values to our maximum.
        var vals= (System.Single[]) input["values"];

        for (int i=0; i < vals.Length; i++)
        {
            instance.values[i] = (double) vals[i] / 100.0;
        }
    }

    // //Input from a float source, value's coming in from 0-100 and should be tamped down to 0-127.
    // public static void SetValues(this RTable<Byte> instance, Godot.Collections.Dictionary input)
    // {
    //     //Structure of dict:   {values=[PoolRealArray tbl], minFloor=0.0, maxCeil=1.0, uselog=false...}
    //     instance.floor = (double) input["floor"] / 100.0;
    //     instance.ceiling = (double) input["ceiling"] / 100.0;
    //     instance.use_log = (bool) input["use_log"];

    //     //Convert values to our maximum.
    //     var vals= (Godot.Collections.Array) input["values"];

    //     for (int i=0; i<vals.Count; i++)
    //     {
    //         //The epsilon here makes sure the byte value is never 128 (invalid in 7-bit midi velocity or note scale)
    //         instance.values[i] = (Byte) ( ((double) vals[i]/100.0) * (128-Single.Epsilon) );
    //     }
    // }

    // public static Godot.Collections.Array ToGodotArray(this RTable<Byte> instance)
    // {
    //     // var output = new float[128];
    //     var output = new Godot.Collections.Array(new float[128]);

    //     for(int i=0; i<128; i++)
    //     {
    //         output[i] =  Convert.ToInt32(instance.values[i]);  //TODO:  RANGE LERP THIS FROM 0-100 OR ELSE SHIT WILL BREAK LATER.  FIXME!!!
    //     }

    //     return output;
    // }

    // public static Godot.Collections.Array ToGodotArray(this RTable<Double> instance)
    // {
    //     // var output = new float[128];
    //     var output = new Godot.Collections.Array(new float[128]);

    //     for(int i=0; i<128; i++)
    //     {
    //         output[i] = (float) (instance.values[i] * 100.0);
    //     }

    //     return output;
    // }

    //TODO:  EQUIVALENT FOR RTable<Byte>
    public static Godot.Collections.Dictionary ToDict(this RTable<Double> instance)
    {
        // var output = new float[128];
        var out_tbl = new Godot.Collections.Array(new float[128]);
        var output = new Godot.Collections.Dictionary();

        for(int i=0; i<128; i++)
        {
            out_tbl[i] = (float) (instance.values[i] * 100.0);
        }

        output["values"] = out_tbl;
        output["floor"] = instance.floor * 100.0;
        output["ceiling"] = instance.ceiling * 100.0;
        output["use_log"] = instance.use_log;


        return output;
    }


}
