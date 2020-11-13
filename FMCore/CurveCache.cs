using Godot;
using System;
using System.Collections.Generic;


/// CurveCache provides a fast accessible way to determine curves for ADSR envelopes by precalculating them into a reference table.
public class CurveCache
{
    double curve;  //For reference only...
        public double Curve {get => curve;}  //Read-only

    float[] cache = new float[UInt16.MaxValue]; //65536, accurate enough for 16-bit audio.  Allocation is about 256kb per instance.

    /// Produces a new cache of the specified curve.  Curve is in Godot easing curve format.  See GD.Ease for details, or glue.cs easing funcs.
    CurveCache(double curve)  { RepopulateCache(curve); }

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

}