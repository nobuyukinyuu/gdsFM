using System;
using Godot;

//Adapted from https://www.musicdsp.org/en/latest/Filters/27-resonant-iir-lowpass-12db-oct.html
public static class GDSFmLowPass{

static double vibrapos, vibraspeed;  //Used for global low-pass.  Move to Patch?

    //Fills an entire buffer with lowpass data.
    public static void FillBuffer(Vector2[] bufferdata, double resofreq=5000, double amp=1.0, double sample_rate=44100.0)
    {
        int streamofs;
        double w = 2.0 * Math.PI * resofreq/sample_rate; // Pole angle
        double q = 1.0 - w/(2.0*(amp + 0.5/(1.0+w)) + w - 2.0); // Pole magnitude
        double r = q*q;
        double c = r + 1.0 - 2.0*Math.Cos(w) * q;  //Update to use lookup table


        int streamsize = bufferdata.Length;

        /* Main loop */
        for (streamofs = 0; streamofs < streamsize; streamofs++) {

        /* Accelerate vibra by signal-vibra, multiplied by lowpasscutoff */
        vibraspeed += (bufferdata[streamofs].x - vibrapos) * c;

        /* Add velocity to vibra's position */
        vibrapos += vibraspeed;

        /* Attenuate/amplify vibra's velocity by resonance */
        vibraspeed *= r;

        /* Check clipping */
        float temp = (float) vibrapos;
        Mathf.Clamp(temp, -1.0f, 1.0f);

        /* Store new value */
        bufferdata[streamofs] = new Vector2(temp,temp);
        }
    }

    //Applies the filter to a single sample.  Modifies references to the note's cutoff history buffer provided by vibra___ vars.
    public static double Filter(double sample, FilterData data, ref double vibrapos, ref double vibraspeed)
    {
        /* Accelerate vibra by signal-vibra, multiplied by lowpasscutoff */
        vibraspeed += (sample - vibrapos) * data.c;

        /* Add velocity to vibra's position */
        vibrapos += vibraspeed;

        /* Attenuate/amplify vibra's velocity by resonance */
        vibraspeed *= data.r;

        /* Check clipping */
        //TODO
        // var temp = Mathf.Clamp(vibrapos, -1.0, 1.0);
        // return temp;

        double temp = Math.Min(Math.Max(vibrapos, -1.0), 1.0);  //Clamp it to something reasonable
        
        return temp;


    }

    //To reduce calculations for a single sample filter, calculations (Q factor, etc) that don't need to be made multiple times can be stored in a data packet.
    public class FilterData
    {
        public bool enabled = false;  //Used by Envelope to determine whether to pass the sample to the cutoff filter.
        public double cutoff=44100;  //Cutoff frequency.  Should probably default to sample_rate.
        public double resonanceAmp=1.0;  //Resonance amplitude.  MUST BE >= 1.0, NO EXCEPTIONS.
        public double w; // Pole angle
        public double q; // Pole magnitude
        public double r;  //res
        public double c; //cut


        public FilterData() {Recalc(44100, 1.0);}
        public FilterData(double sample_rate) {Recalc(44100, 1.0, sample_rate);}
        public FilterData(double resofreq, double amp) {Recalc(resofreq, amp);}


        //Recalculates the appopriate vars whenever the cutoff or resonance changes.
        public void Recalc (double resofreq, double amp, double sample_rate = 44100.0)
        {
            this.cutoff = resofreq;
            this.resonanceAmp = amp;
            Recalc (sample_rate);
        }
        public void Recalc(double sample_rate=44100.0) 
        {
            this.w = 2.0 * Math.PI * this.cutoff/sample_rate; // Pole angle
            this.q = 1.0 - w/(2.0*(this.resonanceAmp + 0.5/(1.0+w)) + w - 2.0); // Pole magnitude
            this.r = q*q;
            this.c = r + 1.0 - 2.0*Math.Cos(w) * q;  //Update to use lookup table
        }

    }


}