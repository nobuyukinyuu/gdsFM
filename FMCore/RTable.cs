using Godot;
using System;

//Response / Rate Table.  128 values in either 8-bit format (MIDI velocity, etc) or floating-point
public class RTable<T> : Resource
{
    const string iotype="RTable";
    readonly string subtype = typeof(T).ToString();

    public enum Presets {ZERO, FIFTY_PERCENT, ONE, IN, OUT, IN_OUT, TWELFTH_ROOT_OF_2}

    public T[] precalc_values = new T[128];  //Calculated from values with the floor, ceiling, and optional epsilon applied (if zeroes are prohibited)
    public T[] values = new T[128];
    public double floor=0, ceiling=100;
    public bool use_log = false;

//Allows zeros to return from the precalculated table.  This is very bad for rates or anything that divides by this number.
    public bool allow_zero;

    public RTable() {}

//Indexer for the main array
    public T this[int i]
    {
        get{
            return precalc_values[i];
        }set{
            precalc_values[i] = value;
        }
    }

//Outputs a JSON-compatible string for marshalling to/from gdscript
    public override string ToString()
    {
        var output = new System.Text.StringBuilder("{'values'=[", 1024);

        for(int i=0; i < values.Length; i++)
        {
            output.Append(values[i]).Append(",");
        }

        output.Append(", 'floor'=").Append(floor);
        output.Append(", 'ceiling'=").Append(ceiling);
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

            //Presume values is float or double instead of T. Precalc values will have to be T and upper limits defined based on type.
            // if (typeof(T) == typeof(float) || typeof(T) == typeof(double) )
                var val = Convert.ToDouble( values[i] );
                val = val * (ceiling/100.0);  //Apply ceiling.
                val = (floor/100.0) +  val * (1.0-(floor/100.0));  //Apply floor.

                if (val ==0 && !allow_zero)  val += float.Epsilon;  //Apply epsilon.
                if (use_log)  val = Math.Pow(val, RTable.LOGFACTOR);  //Apply LinLog conversion (0.5 = 0.1, etc).

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(byte) )
                {
                    precalc_values[i] = (T) Convert.ChangeType(val, typeof(T));
                } else {
                    throw new ArrayTypeMismatchException("RTable can't convert to this instance type (" + typeof(T).ToString() + ")");
                }

        }
    }
    public void RecalcValue(int i) 
    {
        //Range-remap the values from 0-1 to fit within the floor and ceiling. This reduces recalcs when fetching

        //Presume values is float or double instead of T. Precalc values will have to be T and upper limits defined based on type.
        // if (typeof(T) == typeof(float) || typeof(T) == typeof(double) )
        var val = Convert.ToDouble( values[i] );
        val = val * (ceiling/100.0);  //Apply ceiling.
        val = (floor/100.0) +  val * (1.0-(floor/100.0));  //Apply floor.

        if (val ==0 && !allow_zero)  val += float.Epsilon;  //Apply epsilon.
        if (use_log)  val = Math.Pow(val, RTable.LOGFACTOR);  //Apply LinLog conversion (0.5 = 0.1, etc).

        if (typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(byte) )
        {
            precalc_values[i] = (T) Convert.ChangeType(val, typeof(T));
        } else {
            throw new ArrayTypeMismatchException("RTable can't convert to this instance type (" + typeof(T).ToString() + ")");
        }
        
    }

}

public static class RTable
{
    public const double LOGFACTOR = 3.32192809488736; // Math.Log(0.1) / Math.Log(0.5);  Raising an input value 0.5^LOGFACTOR produces 0.1
    public enum Presets {ZERO, FIFTY_PERCENT, MAX, LINEAR, IN, OUT, IN_OUT, TWELFTH_ROOT_OF_2, DESCENDING=0x100}
    static readonly System.Collections.Generic.Dictionary<Presets, Double> curveMap = new System.Collections.Generic.Dictionary<Presets, double>
    {
        [Presets.ZERO] = 0,
        [Presets.LINEAR] = 1,
        [Presets.IN] = 2.0,
        [Presets.OUT] = 0.5,
        [Presets.IN_OUT] = -2.0,
    };

    public static RTable<U> FromPreset<U>(Presets preset, double floor=0, double ceiling=100, bool allow_zero=true) where U:struct
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
                if (typeof(U) == typeof(float) || typeof(U) == typeof(double) )
                {
                    for (int i=0; i<128; i++)   output.values[i] = (U) Convert.ChangeType(Pow12th(i), typeof(U));
                } else if (typeof(U).IsValueType==true) {  //Integer or Byte or Short
                    for (int i=0; i<128; i++)   output.values[i] = (U) Convert.ChangeType(Pow12th(i) * 127, typeof(U));
                }
                break;

        }
        if ((preset & Presets.DESCENDING) == Presets.DESCENDING)  output.Reverse();
        
        output.floor = floor;
        output.ceiling = ceiling;
        output.allow_zero = allow_zero;

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
        // GD.Print("attempting to assign RTable.......");
        // GD.Print(input["values"].GetType());

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

        instance.RecalcValues();
    }

    public static void SetValue(this RTable<double> instance, int index, double input)
    {
        //Convert values to our maximum.
        instance.values[index] = (double) input / 100.0;

        instance.RecalcValue(index);
    }

    //TODO:  EQUIVALENT FOR RTable<Byte>
    public static Godot.Collections.Dictionary ToDict(this RTable<Double> instance)
    {
        // var output = new float[128];
        var out_tbl = new Godot.Collections.Array(new float[128]);
        var output = new Godot.Collections.Dictionary();

        var out_tbl_precalc = new Godot.Collections.Array(new float[128]);

        for(int i=0; i<128; i++)
        {
            out_tbl[i] = (float) (instance.values[i] * 100.0);
            out_tbl_precalc[i] = (float) instance.precalc_values[i];
        }

        output["values"] = out_tbl;
        output["precalc_values"] = out_tbl_precalc;
        output["floor"] = instance.floor;
        output["ceiling"] = instance.ceiling;
        output["use_log"] = instance.use_log;


        return output;
    }


}
