// Helper classes and other C# to GDScript glue go here
using Godot;
using System;
using System.Runtime.CompilerServices;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Linq;
using System.Threading;

//"Glue" class used for interacting with GDScript code only
public class glue : Node
{

    public static double PowFast(double a, double b) {
        return GDSFmFuncs.PowFast(a,b);
    }

    public static double EaseFast(double percent, double curve){
        return GDSFmFuncs.EaseFast(percent, curve);
    }
}

// C# Implementations of math funcs, probably faster than delegating from the GD namespace?
public static class GDSFmFuncs
{
    //Godot easing func.
    public static double Ease(double p_x, double p_c) {
        if (p_x < 0.0)
            p_x = 0.0f;
        else if (p_x > 1.0)
            p_x = 1.0f;
        if (p_c > 0.0) {
            if (p_c < 1.0) {
                return 1.0 - Math.Pow(1.0 - p_x, 1.0 / p_c);
            } else {
                return Math.Pow(p_x, p_c);
            }
        } else if (p_c < 0.0) {
            //inout ease

            if (p_x < 0.5) {
                return Math.Pow(p_x * 2.0, -p_c) * 0.5;
            } else {
                return (1.0 - Math.Pow(1.0 - (p_x - 0.5) * 2.0, -p_c)) * 0.5 + 0.5;
            }
        } else
            return 0.0; // no ease (raw)
    }

    //Ease, but produces values between the range specified.
    public static double Ease(double first, double last, double percent, double curve=1.0)
    {
        double amt = last-first;
        return Ease(percent, curve) * amt + first;
    }

//Ease, but with approximate values for exponent using PowFast().
    public static double EaseFast(double p_x, double p_c) {
        if (p_x < 0.0)
            p_x = 0.0f;
        else if (p_x > 1.0)
            p_x = 1.0f;
        if (p_c > 0.0) {
            if (p_c < 1.0) {
                return 1.0 - PowFast(1.0 - p_x, 1.0 / p_c);
            } else {
                return PowFast(p_x, p_c);
            }
        } else if (p_c < 0.0) {
            //inout ease

            if (p_x < 0.5) {
                return PowFast(p_x * 2.0, -p_c) * 0.5;
            } else {
                return (1.0 - PowFast(1.0 - (p_x - 0.5) * 2.0, -p_c)) * 0.5 + 0.5;
            }
        } else
            return 0.0; // no ease (raw)
    }

    //EaseFast, but produces values between the range specified.
    public static double EaseFast(double first, double last, double percent, double curve=1.0f)
    {
        double amt = last-first;
        return EaseFast(percent, curve) * amt + first;
    }


// Approximation of Math.Pow which SHOULD be within 1.7% margin of error.  Used to determine attenuation curves for ADSR envelopes.
// https://www.reddit.com/r/gamedev/comments/n7na0/fast_approximation_to_mathpow/
// ONLY WORKS FOR POSITIVE NUMBERS
    public static double PowFast(double a, double b) {
            // System.Diagnostics.Debug.Assert(Math.Sign(b) >= 0);

            // exponentiation by squaring
            double r = 1.0;
            int exp = (int) b;
            double _base = a;
            
            while (exp != 0) 
            {
                if ((exp & 1) != 0) {
                    r *= _base;
                }
                _base *= _base;
                exp >>= 1;
            }
        
            // use the IEEE 754 trick for the fraction of the exponent
            double b_faction = b - (int)b;
            long tmp = BitConverter.DoubleToInt64Bits(a);
            long tmp2 = (long) (b_faction * (tmp - 4606921280493453312L)) + 4606921280493453312L;

            return r * BitConverter.Int64BitsToDouble(tmp2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double lerp(double value1, double value2, double amount)
    {
        return value1 + (value2 - value1) * amount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Decimal lerp(Decimal value1, Decimal value2, Decimal amount)
    {
        return value1 + (value2 - value1) * amount;
    }


    //Provides an atomic add operation during parallel processing.  "Optimistically concurrent" func; loops until thread-safe
    public static double InterlockedAdd (ref double location1, double value)
    {
        double newCurrentValue = location1; // Non-volatile read, so may be stale
        while (true)
        {
            double currentValue = newCurrentValue;
            double newValue = currentValue + value;
            newCurrentValue = Interlocked.CompareExchange(ref location1, newValue, currentValue);

            if (newCurrentValue == currentValue)    return newValue;
        }
    }
    public static float InterlockedAdd (ref float location1, float value)
    {
        float newCurrentValue = location1; // Non-volatile read, so may be stale
        while (true)
        {
            float currentValue = newCurrentValue;
            float newValue = currentValue + value;
            newCurrentValue = Interlocked.CompareExchange(ref location1, newValue, currentValue);

            if (newCurrentValue == currentValue)    return newValue;
        }
    }
}



// Helps C# code find an autoload or property of an autoload
public class AutoLoadHelper<T> : Node
{
    public static T Get(string autoload, string propertyName)  //Find property of an autoload
    {
        SceneTree tree = (SceneTree) Engine.GetMainLoop();
        Node root = tree.EditedSceneRoot;
        return (T) root.GetNode(autoload).Get(propertyName);
    }
}

public class AutoLoadHelper : Node 
{
    public static Node GetNode(string autoload)
    {
        SceneTree tree = (SceneTree) Engine.GetMainLoop();
        Node root = tree.EditedSceneRoot;
        return root.GetNode(autoload);
    }

}




//ContractResolver which is used to specify only the specific subclass properties should be added when serializing
public class IgnoreParentPropertiesResolver : DefaultContractResolver
{
    bool IgnoreBase = false;
    public IgnoreParentPropertiesResolver(bool ignoreBase)
    {
        IgnoreBase = ignoreBase;
    }
    protected override System.Collections.Generic.IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var allProps = base.CreateProperties(type, memberSerialization);
        if (!IgnoreBase) return allProps;

        //Choose the properties you want to serialize/deserialize
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly;
        // var props = type.GetProperties(~System.Reflection.BindingFlags.FlattenHierarchy); 
        var props = type.GetFields(flags); 

        return allProps.Where(p => props.Any(a => a.Name == p.PropertyName)).ToList();
    }
}