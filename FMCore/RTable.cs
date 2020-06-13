using Godot;
using System;


//Response / Rate Table.  128 values in either 8-bit format (MIDI velocity, etc) or floating-point
public class RTable<T> : Resource
{
    public enum Presets {ZERO, FIFTY_PERCENT, ONE, IN, OUT, IN_OUT, TWELFTH_ROOT_OF_2}

    public T[] values = new T[128];
    public int floor, ceiling;
    public bool use_log = false;

    public RTable() {}

    //Indexer for the main array
    public T this[int i]
    {
        get{
            return values[i];
        }set{
            values[i] = value;
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
}

public static class RTable
{
    public enum Presets {ZERO, FIFTY_PERCENT, MAX, IN, OUT, IN_OUT, TWELFTH_ROOT_OF_2}

    public static RTable<U> FromPreset<U>(Presets preset) where U:struct
    {
        var output = new RTable<U>();
        switch (preset)
        {
            case Presets.MAX:
                if (typeof(U) == typeof(float) || typeof(U) == typeof(double) )
                {
                    for (int i=0; i<128; i++)   output[i] = (U) Convert.ChangeType(1, typeof(U));
                } else if (typeof(U).IsValueType==true) {  //Integer or Byte or Short
                    for (int i=0; i<128; i++)   output[i] = (U) Convert.ChangeType(127, typeof(U));
                }
                break;
            case Presets.FIFTY_PERCENT:
                for (int i=0; i<128; i++)   output[i] = (U) Convert.ChangeType(0.5, typeof(U));
                break;
        }
        return output;
    }
}

//Type-specific value assignment from an input string
//takes a string of characters in format:  "[0,1,2,.....127]"
public static class RTableExtensions
{
    public static void SetValues(this RTable<int> instance, string input)
    {
        var s = input.Substring(1,input.Length-2).Split(",",false);

        for(int i=0; i < instance.values.Length; i++)
        {
            instance.values[i] = Convert.ToByte(s[i]);
        }
    }
    public static void SetValues(this RTable<float> instance, string input)
    {
        var s = input.Substring(1,input.Length-2).SplitFloats(",",false);
        instance.values = s;
    }

}
