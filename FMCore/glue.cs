// Helper classes and other C# to GDScript glue go here
using Godot;
using System;
using System.Runtime.CompilerServices;

// using Newtonsoft.Json;
// using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Linq;
using System.Threading;

/// Flags for copying and exporting bits of an envelope when serializing.
[Flags]  public enum EGCopyFlags{NONE=0, EG=1, TUNING=4, CURVE=2, WAVEFORM=8, RTABLES=16, LOWPASS=32, LFO=64, ALL=127};
[Flags]  public enum OPCopyFlags{NONE=0, NAME=1, ID=2, EG=4, BYPASS=8, LFO=16, WAVEFORM_BANK=32, WAVEFORM=64, ALL=127,   HEADERS=11};
[Flags]  public enum PatchCopyFlags{NONE=0, GENERAL=1,  OPS=2, PG=4, LFO=8, WAVEFORMS=16, ALL=255};
// [Flags]  public enum LFOCopyFlags{NONE=0, NAME=1, TRANSPOSE=2, GAIN=4, PAN=8, OPS=16, PG=32, LFO=64, WAVEFORMS=128, ALL=255};

namespace GdsFMJson
{
    public enum IOError{OK=0, JSON_MALFORMED=-1, UNKNOWN_VERSION=-2, UNSUPPORTED_VERSION=-20, INCORRECT_IOTYPE=-3, }
}


//"Glue" class used for interacting with GDScript code only
public class glue : Node
{
    //Simple 1op sine wave used to re-initialize a patch.
    public const string INIT_PATCH = "{\"name\":\"init\",\"gain\":1,\"transpose\":2,\"pan\":0,\"wiring\":\"Output=OP4\",\"operators\":{\"OP4\":{\"name\":\"OP4\",\"id\":3,\"bypass\":false,\"eg\":{\"ar\":28,\"dr\":31,\"sr\":6,\"rr\":14,\"sl\":100,\"tl\":100,\"ks\":0,\"delay\":0,\"ac\":-2,\"dc\":0.75,\"sc\":0.5,\"rc\":0.25,\"waveform\":0,\"feedback\":0,\"reflect\":false,\"duty\":0.5,\"_use_duty\":0,\"fmTechnique\":0,\"techDuty\":0.5,\"techReflect\":false,\"mul\":1,\"dt\":0,\"dt2\":0,\"fixed_frequency\":0,\"filter\":{\"enabled\":false,\"cutoff\":44100,\"resonanceAmp\":1},\"ksr\":{\"values\":[1,0.9438743,0.8908987,0.8408964,0.7937005,0.7491536,0.7071068,0.6674199,0.6299605,0.5946035,0.561231,0.5297316,0.5,0.4719371,0.4454494,0.4204482,0.3968503,0.3745768,0.3535534,0.33371,0.3149803,0.2973018,0.2806155,0.2648658,0.25,0.2359686,0.2227247,0.2102241,0.1984251,0.1872884,0.1767767,0.166855,0.1574901,0.1486509,0.1403078,0.1324329,0.125,0.1179843,0.1113623,0.1051121,0.09921256,0.09364419,0.08838835,0.08342749,0.07874507,0.07432544,0.07015388,0.06621645,0.0625,0.05899214,0.05568117,0.05255603,0.04960628,0.0468221,0.04419417,0.04171374,0.03937253,0.03716272,0.03507694,0.03310822,0.03125,0.02949607,0.02784058,0.02627801,0.02480314,0.02341105,0.02209709,0.02085687,0.01968627,0.01858136,0.01753847,0.01655411,0.015625,0.01474804,0.01392029,0.01313901,0.01240157,0.01170552,0.01104854,0.01042844,0.009843133,0.00929068,0.008769235,0.008277056,0.0078125,0.007374018,0.006960146,0.006569503,0.006200785,0.005852762,0.005524272,0.005214218,0.004921567,0.00464534,0.004384617,0.004138528,0.00390625,0.003687009,0.003480073,0.003284752,0.003100393,0.002926381,0.002762136,0.002607109,0.002460783,0.00232267,0.002192309,0.002069264,0.001953125,0.001843504,0.001740037,0.001642376,0.001550196,0.001463191,0.001381068,0.001303555,0.001230392,0.001161335,0.001096154,0.001034632,0.0009765625,0.0009217522,0.0008700183,0.0008211879,0.0007750982,0.0007315953,0.000690534,0.0006517773],\"floor\":100,\"ceiling\":100,\"use_log\":false},\"ksl\":{\"values\":[1,0.9438743,0.8908987,0.8408964,0.7937005,0.7491536,0.7071068,0.6674199,0.6299605,0.5946035,0.561231,0.5297316,0.5,0.4719371,0.4454494,0.4204482,0.3968503,0.3745768,0.3535534,0.33371,0.3149803,0.2973018,0.2806155,0.2648658,0.25,0.2359686,0.2227247,0.2102241,0.1984251,0.1872884,0.1767767,0.166855,0.1574901,0.1486509,0.1403078,0.1324329,0.125,0.1179843,0.1113623,0.1051121,0.09921256,0.09364419,0.08838835,0.08342749,0.07874507,0.07432544,0.07015388,0.06621645,0.0625,0.05899214,0.05568117,0.05255603,0.04960628,0.0468221,0.04419417,0.04171374,0.03937253,0.03716272,0.03507694,0.03310822,0.03125,0.02949607,0.02784058,0.02627801,0.02480314,0.02341105,0.02209709,0.02085687,0.01968627,0.01858136,0.01753847,0.01655411,0.015625,0.01474804,0.01392029,0.01313901,0.01240157,0.01170552,0.01104854,0.01042844,0.009843133,0.00929068,0.008769235,0.008277056,0.0078125,0.007374018,0.006960146,0.006569503,0.006200785,0.005852762,0.005524272,0.005214218,0.004921567,0.00464534,0.004384617,0.004138528,0.00390625,0.003687009,0.003480073,0.003284752,0.003100393,0.002926381,0.002762136,0.002607109,0.002460783,0.00232267,0.002192309,0.002069264,0.001953125,0.001843504,0.001740037,0.001642376,0.001550196,0.001463191,0.001381068,0.001303555,0.001230392,0.001161335,0.001096154,0.001034632,0.0009765625,0.0009217522,0.0008700183,0.0008211879,0.0007750982,0.0007315953,0.000690534,0.0006517773],\"floor\":100,\"ceiling\":100,\"use_log\":false},\"vr\":{\"values\":[0,0.0001240002,0.000496001,0.001116002,0.001984004,0.003100006,0.004464009,0.006076012,0.007936016,0.01004402,0.01240002,0.01500403,0.01785604,0.02095604,0.02430405,0.02790006,0.03174406,0.03583607,0.04017608,0.04476409,0.0496001,0.05468411,0.06001612,0.06559613,0.07142414,0.07750016,0.08382418,0.09039619,0.0972162,0.1042842,0.1116002,0.1191642,0.1269763,0.1350363,0.1433443,0.1519003,0.1607043,0.1697563,0.1790564,0.1886044,0.1984004,0.2084444,0.2187364,0.2292765,0.2400645,0.2511005,0.2623845,0.2739165,0.2856966,0.2977246,0.3100006,0.3225246,0.3352967,0.3483167,0.3615847,0.3751008,0.3888648,0.4028768,0.4171368,0.4316449,0.4464009,0.4614049,0.4766569,0.492157,0.507843,0.523343,0.5385951,0.5535991,0.5683551,0.5828632,0.5971232,0.6111352,0.6248993,0.6384153,0.6516833,0.6647033,0.6774753,0.6899994,0.7022754,0.7143034,0.7260835,0.7376155,0.7488995,0.7599356,0.7707235,0.7812636,0.7915556,0.8015997,0.8113956,0.8209437,0.8302436,0.8392956,0.8480996,0.8566557,0.8649637,0.8730237,0.8808358,0.8883998,0.8957158,0.9027838,0.9096038,0.9161758,0.9224998,0.9285759,0.9344039,0.9399839,0.945316,0.9503999,0.9552359,0.9598239,0.9641639,0.9682558,0.9721,0.975696,0.979044,0.9821439,0.984996,0.9876,0.9899561,0.992064,0.993924,0.995536,0.9969,0.998016,0.998884,0.999504,0.999876,1],\"floor\":0,\"ceiling\":100,\"use_log\":false}},\"lfoBankAmp\":-1,\"lfoBankPitch\":-1,\"ams\":0,\"pms\":0,\"waveformBank\":-1}},\"pal\":0,\"pdl\":0,\"psl\":0,\"prl\":0,\"par\":0,\"pdr\":0,\"prr\":0,\"LFOs\":[{\"enabled\":false,\"waveform\":0,\"reflect_waveform\":false,\"depth\":1,\"cycleTime\":1,\"delay\":0,\"bias\":0,\"keySync\":true,\"oscSync\":true,\"legato\":true},{\"enabled\":false,\"waveform\":0,\"reflect_waveform\":false,\"depth\":1,\"cycleTime\":1,\"delay\":0,\"bias\":0,\"keySync\":true,\"oscSync\":true,\"legato\":true},{\"enabled\":false,\"waveform\":0,\"reflect_waveform\":false,\"depth\":1,\"cycleTime\":1,\"delay\":0,\"bias\":0,\"keySync\":true,\"oscSync\":true,\"legato\":true}],\"customWaveforms\":[{\"values\":[0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5],\"floor\":0,\"ceiling\":100,\"use_log\":false}],\"_iotype\":\"patch\"}";


    //Each increment of detune is 1/8 the ratio between a period of 2205/22 at 44100hz sample rate (440hz tone) and 2197/22 (about 441.6hz).
    //This measurement was done against DX7 detune at A-4, where every 22 cycles the tone would change (-detune) samples.
    const Decimal DETUNE_HZ_INCREMENT = 1.60218479745106964041875284479M / 8M;  
    const Decimal DETUNE_HZ_DENCREMENT = 1M / 1.60218479745106964041875284479M / 8M; //Each decrement of detune is 1/8 the ratio between periods of 2205/22 and 2213/22.


    //Initialize several components of the engine
    public override void _Ready()
    {
        // GDSFmIO.init();
        Note.gen_period_table();
		oscillators.gen_sin_table(9600);  //Sounds better at high frequencies....
        
    }

}



// C# Implementations of math funcs, probably faster than delegating from the GD namespace?
public static class GDSFmFuncs
{
    static readonly Action DoNothing = () => {};  //Empty lambda for stuffing arrays full of actions or otherwise "pass" on an action set

    /// Godot easing func.
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

    /// Ease, but produces values between the range specified.
    public static double Ease(double first, double last, double percent, double curve=1.0)
    {
        double amt = last-first;
        return Ease(percent, curve) * amt + first;
    }

    /// Ease, but with approximate values for exponent using PowFast().
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
    public static float lerp(float value1, float value2, float amount)
    {
        return value1 + (value2 - value1) * amount;
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

        //Type converter for generic class
        public static bool TryChangeType<T, TR>(T input, out TR output) where T : IConvertible
    {
        bool result = false;
        try
        {
            Type type = Nullable.GetUnderlyingType(typeof(TR));
            output = (TR)Convert.ChangeType(input, type);
            result = true;
        }
        catch(Exception)
        {
            output = default(TR);
        }
        return result;
    }

    /// Lists the contents of a List<String>.
    public static string Contents (this System.Collections.Generic.List<String> instance)
    {
        var output = new System.Text.StringBuilder();

        output.Append("{");
        foreach (string s in instance)
        {
            output.Append(s).Append(",");
        }
        output.Append("}");

        return output.ToString();
    }

    /// Extension method which resizes a List<T>.
    public static void Resize<T>(this System.Collections.Generic.List<T> list, int sz, T c)
    {
        int cur = list.Count;
        if(sz < cur)
            list.RemoveRange(sz, cur - sz);
        else if(sz > cur)
        {
            if(sz > list.Capacity)//this bit is purely an optimisation, to avoid multiple automatic capacity changes.
              list.Capacity = sz;
            list.AddRange(Enumerable.Repeat(c, sz - cur));
        }
    }
    /// Creates a List<T> with <c>sz</c> newly initialized elements.
    public static void Resize<T>(this System.Collections.Generic.List<T> list, int sz) where T : new()
    {
        Resize(list, sz, new T());
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


// public static class GDSFmIO
// {
//     public static JsonSerializerSettings serializerSettings = new JsonSerializerSettings();
//     public static IgnoreParentPropertiesResolver resolver = new IgnoreParentPropertiesResolver(true);

//     public static void init()
//     {
//         serializerSettings.ContractResolver = resolver;
//         serializerSettings.Formatting = Formatting.Indented;
//         serializerSettings.NullValueHandling = NullValueHandling.Ignore;
//     }
// }

// //ContractResolver which is used to specify only the specific subclass properties should be added when serializing
// public class IgnoreParentPropertiesResolver : DefaultContractResolver
// {
//     bool IgnoreBase = false;
//     public IgnoreParentPropertiesResolver(bool ignoreBase)
//     {
//         IgnoreBase = ignoreBase;
//     }
//     protected override System.Collections.Generic.IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
//     {
//         var allProps = base.CreateProperties(type, memberSerialization);
//         if (!IgnoreBase) return allProps;

//         //Choose the properties you want to serialize/deserialize
//         var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly;
//         // var props = type.GetProperties(~System.Reflection.BindingFlags.FlattenHierarchy); 
//         var props = type.GetFields(flags); 

//         return allProps.Where(p => props.Any(a => a.Name == p.PropertyName)).ToList();
//     }
// }