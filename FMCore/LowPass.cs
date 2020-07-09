using System;
using Godot;


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

    //Applies the filter to a single sample
    public static double Filter(double sample, ref double vibrapos, ref double vibraspeed)
    {

    }
}