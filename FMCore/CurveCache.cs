using Godot;
using System;
using System.Collections.Generic;


/// CurveCache provides a fast accessible way to determine curves for ADSR envelopes by precalculating them into a reference table.
public class CurveCache
{
    double curve;  //For reference only...
        public double EaseValue {get => curve;}  //Read-only

    float[] cache = new float[UInt16.MaxValue]; //65536, accurate enough for 16-bit audio.  Allocation is about 256kb per instance.

    /// Produces a new cache of the specified curve.  Curve is in Godot easing curve format.  See GD.Ease for details, or glue.cs easing funcs.
    public CurveCache(double curve)  { RepopulateCache(curve); }

    public void RepopulateCache(double curve)
    {
        this.curve = curve;
        for(int i=0; i < cache.Length; i++)
        {
            var percent = i / ((double)cache.Length-1) ;
            cache[i] = (float) GDSFmFuncs.Ease(percent, curve);
        }
    }

    //Indexer.  Retrieves the easing value for the specified percent range.  Read-only.
    public float this [double percent]
    {
        get
        {
            const double SIZE= UInt16.MaxValue - Double.Epsilon;  //Value which will never round up to an overflow
            return cache[ (int) (SIZE*percent) ];
        }
    }

    /// Maps the curve value to a value between first and last.
    public double MappedTo(double first, double last, double percent)
    {
        if(percent > 1.0 || percent < 0.0) GD.Print("WARNING, CACHE CURVE ENVELOPE IS ", percent, ". first=", first, "  last=", last);
        double amt = last-first;
        return this[percent] * amt + first;
    }

}



    //Default cached curves for ADSR Envelopes
    public struct DefaultCurves
    {
        public const double A = -2.00;  //Attack curve.  In-out.
        public const double D =  0.75;  //Decay curve.  75% Linear, 25% Ease-out.
        public const double S =  0.50;  //Sustain curve.  50% Linear, 50% Ease-out.
        public const double R =  0.25;  //Release curve.  75% Ease-out.

        public static readonly CurveCache Attack = new CurveCache(A);
        public static readonly CurveCache Decay = new CurveCache(D);
        public static readonly CurveCache Sustain = new CurveCache(S);
        public static readonly CurveCache Release = new CurveCache(R);

        public static readonly CurveCache[] ADSR = {Attack, Decay, Sustain, Release};
    }
