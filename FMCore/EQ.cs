


/*************************************************************************/
/*  eq.cpp                                                               */
/*************************************************************************/
/* Copyright (c) 2007-2020 Juan Linietsky, Ariel Manzur.                 */
/* Copyright (c) 2014-2020 Godot Engine contributors (cf. AUTHORS.md).   */
/*                                                                       */
/* Permission is hereby granted, free of charge, to any person obtaining */
/* a copy of this software and associated documentation files (the       */
/* "Software"), to deal in the Software without restriction, including   */
/* without limitation the rights to use, copy, modify, merge, publish,   */
/* distribute, sublicense, and/or sell copies of the Software, and to    */
/* permit persons to whom the Software is furnished to do so, subject to */
/* the following conditions:                                             */
/*                                                                       */
/* The above copyright notice and this permission notice shall be        */
/* included in all copies or substantial portions of the Software.       */
/*                                                                       */
/* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,       */
/* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF    */
/* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.*/
/* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY  */
/* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,  */
/* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE     */
/* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.                */
/*************************************************************************/

// Original Author: reduzio@gmail.com (C) 2006
// C# adaptation:   nobuyuki.nyuu@gmail.com (2020).  Please refer to LICENSE.md included with gdsFM.

using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Godot;

class EQ
{
    static double mix_rate = 44100.0;
    const double Math_SQRT12 = 0.7071067811865475244008443621048490;
    const double Math_PI = 3.1415926535897932384626433833;


    public static double[] bands; //Set in set_preset_band_mode().  In godot version this field is const
    public enum Preset{
		PRESET_6_BANDS,
		PRESET_8_BANDS,
		PRESET_10_BANDS,
		PRESET_21_BANDS,
		PRESET_31_BANDS        
    }

	public class BandProcess 
    {
		public float c1, c2, c3;
		protected struct History 
        {
			public float a1, a2, a3;
			public float b1, b2, b3;
		} 
         History history;

        public BandProcess() 
        {
            c1 = c2 = c3 = history.a1 = history.a2 = history.a3 = 0;
            history.b1 = history.b2 = history.b3 = 0;
        }


        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void process_one(float p_data)
        {
            history.a1 = p_data;

            history.b1 = c1 * (history.a1 - history.a3) + c3 * history.b2 - c2 * history.b3;

            p_data = history.b1;

            history.a3 = history.a2;
            history.a2 = history.a1;
            history.b3 = history.b2;
            history.b2 = history.b1;
        }
    }

    private struct Band
    {
        public float freq;
        public float c1,c2,c3;
    }

    List<Band> band;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    double POW2(double v) => v*v;

    /* Helper */
    static int solve_quadratic(double a, double b, double c, double r1, double r2) {
        //solves quadractic and returns number of roots (X-intercepts)

        double _base = 2 * a;
        if (_base == 0.0f)
            return 0;

        double squared = b * b - 4 * a * c;
        if (squared < 0.0)
            return 0;

        squared = Math.Sqrt(squared);

        r1 = (-b + squared) / _base;
        r2 = (-b - squared) / _base;

        if (r1 == r2)
            return 1;
        else
            return 2;
    }

    void recalculate_band_coefficients() 
    {

        Func<double, double> BAND_LOG = m_f=> (Math.Log((m_f)) / Math.Log(2.0));
    
        for (int i = 0; i < band.Count; i++) 
        {
            double octave_size;
            double frq = band[i].freq;

            if (i == 0) 
            {
                octave_size = BAND_LOG(band[1].freq) - BAND_LOG(frq);
            } else if (i == (band.Count - 1)) {

                octave_size = BAND_LOG(frq) - BAND_LOG(band[i - 1].freq);
            } else {

                double next = BAND_LOG(band[i + 1].freq) - BAND_LOG(frq);
                double prev = BAND_LOG(frq) - BAND_LOG(band[i - 1].freq);
                octave_size = (next + prev) / 2.0;
            }

            double frq_l = Math.Round(frq / Math.Pow(2.0, octave_size / 2.0));

            double side_gain2 = POW2(Math_SQRT12);
            double th = 2.0 * Math_PI * frq / mix_rate;
            double th_l = 2.0 * Math_PI * frq_l / mix_rate;

            double c2a = side_gain2 * POW2(Math.Cos(th)) - 2.0 * side_gain2 * Math.Cos(th_l) * Math.Cos(th) + side_gain2 - POW2(Math.Sin(th_l));

            double c2b = 2.0 * side_gain2 * POW2(Math.Cos(th_l)) + side_gain2 * POW2(Math.Cos(th)) - 2.0 * side_gain2 * Math.Cos(th_l) * Math.Cos(th) - side_gain2 + POW2(Math.Sin(th_l));

            double c2c = 0.25 * side_gain2 * POW2(Math.Cos(th)) - 0.5 * side_gain2 * Math.Cos(th_l) * Math.Cos(th) + 0.25 * side_gain2 - 0.25 * POW2(Math.Sin(th_l));

            //printf("band %i, precoefs = %f,%f,%f\n",i,c2a,c2b,c2c);

            // Default initializing to silence compiler warning about potential uninitialized use.
            // Both variables are properly set in _solve_quadratic before use, or we continue if roots == 0.
            double r1 = 0, r2 = 0; //roots
            int roots = solve_quadratic(c2a, c2b, c2c, r1, r2);

            if (roots==0) System.Diagnostics.Debug.Print("Condition 'roots==0' is true.");

            Band b = band[i];
            b.c1 = (float)(2.0 * ((0.5 - r1) / 2.0));
            b.c2 = (float)(2.0 * r1);
            b.c3 = (float)(2.0 * (0.5 + r1) * Math.Cos(th));
            band[i] = b;

            // band.write[i].c1 = 2.0 * ((0.5 - r1) / 2.0);
            // band.write[i].c2 = 2.0 * r1;
            // band.write[i].c3 = 2.0 * (0.5 + r1) * Math.Cos(th);
            //printf("band %i, coefs = %f,%f,%f\n",i,(float)bands[i].c1,(float)bands[i].c2,(float)bands[i].c3);
        }
    }

    void PUSH_BANDS(double m_bands)
    {
        for (int i = 0; i < m_bands; i++) 
        { 
            Band b = new Band();
            b.freq = (float) bands[i];
            band.Add(b);
        }
    }

    void set_preset_band_mode(Preset p_preset) {

        band.Clear();

        switch (p_preset) 
        {
            case Preset.PRESET_6_BANDS: 
                bands = new double[]{ 32, 100, 320, 1e3, 3200, 10e3 };
                PUSH_BANDS(6);
                break;

            case Preset.PRESET_8_BANDS: 
                bands = new double[]{ 32, 72, 192, 512, 1200, 3000, 7500, 16e3 };
                PUSH_BANDS(8);
                break;

            case Preset.PRESET_10_BANDS: 
                bands = new double[]{ 31.25, 62.5, 125, 250, 500, 1e3, 2e3, 4e3, 8e3, 16e3 };
                PUSH_BANDS(10);
                break;

            case Preset.PRESET_21_BANDS: 
                bands = new double[]{ 22, 32, 44, 63, 90, 125, 175, 250, 350, 500, 700, 1e3, 1400, 2e3, 2800, 4e3, 5600, 8e3, 11e3, 16e3, 22e3 };
                PUSH_BANDS(21);
                break;

            case Preset.PRESET_31_BANDS: 
                bands = new double[]{ 20, 25, 31.5, 40, 50, 63, 80, 100, 125, 160, 200, 250, 315, 400, 500, 630, 800, 1e3, 
                                        1250, 1600, 2e3, 2500, 3150, 4e3, 5e3, 6300, 8e3, 10e3, 12500, 16e3, 20e3 };
                PUSH_BANDS(31);
                break;
        }

        recalculate_band_coefficients();
    }

    public int get_band_count {get => band.Count;}

    float get_band_frequency(int p_band) 
    {
        try
        {
            return band[p_band].freq;
        } catch (IndexOutOfRangeException e) {
            System.Diagnostics.Debug.Print("EQ.get_band_frequency(): " + e.ToString());
            return 0;
        }
    }

    void set_bands(List<float> p_bands) {

        band.Resize(p_bands.Count);
        for (int i = 0; i < p_bands.Count; i++) {
            Band b = band[i];
            b.freq = p_bands[i];
            band[i] = b;
            // band.write[i].freq = p_bands[i];
        }

        recalculate_band_coefficients();
    }

    void set_mix_rate(float p_mix_rate) {

        mix_rate = p_mix_rate;
        recalculate_band_coefficients();
    }

    public BandProcess get_band_processor(int p_band) {

        BandProcess band_proc=new BandProcess();

        // ERR_FAIL_INDEX_V(p_band, band.Count, band_proc);
        if(p_band >= band.Count)
        {
            System.Diagnostics.Debug.Print("EQ.get_band_processor(): Index 'p_band' out of range");
            return null;
        }


        band_proc.c1 = band[p_band].c1;
        band_proc.c2 = band[p_band].c2;
        band_proc.c3 = band[p_band].c3;

        return band_proc;
    }
}


class EQInstance
{
    List<EQ.BandProcess> bands = new List<EQ.BandProcess>(2);
    List<float> gains;

    EQ eq;
    List<float> gain;
    Dictionary<String, int> prop_band_map;
    List<String> band_names;

    EQInstance(EQ eq)  //Initializer
    {
        // for (int i=0; i < bands.Length; i++)
        // {
        //     bands[i] = new EQ.BandProcess[2];
        // }

        this.eq = eq;

        gains.Resize(eq.get_band_count);
        bands.Resize(eq.get_band_count);
        for (int j = 0; j < bands.Count; j++) 
        {
            bands[j] = eq.get_band_processor(j);
        }        
    }

    public void Process(double[] p_src_frames, in double[] p_dst_frames, int p_frame_count)
    {
        int band_count = bands.Count;
        var proc_l = new List<EQ.BandProcess>(bands);
        List<float> bgain = new List<float>(gains);
        
        //Convert the gains to linear levels
        for (int i = 0; i < band_count; i++) { bgain[i] = GD.Db2Linear(gain[i]); }

        //Cycle thru frames
        for (int i = 0; i < p_frame_count; i++) {

            double src = p_src_frames[i];
            double dst = 0;

            for (int j = 0; j < band_count; j++) 
            {
                double l = src;

                proc_l[j].process_one( (float) l );

                dst += l * bgain[j];
            }

            p_dst_frames[i] = dst;
        }
    }


}