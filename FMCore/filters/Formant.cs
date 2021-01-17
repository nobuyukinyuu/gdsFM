//Formant filter based on stacked bandpass filters.
using System;

public class FormantFilter
{
	const int PEAKCOUNT=3;
	RbjFilter[] peaks = new RbjFilter[PEAKCOUNT];

	public float peak0, peak1;  //Peak frequencies
	public float q, gain;

	public FormantFilter(float mixRate=44100.0f)
	{
		for(int i=0; i<PEAKCOUNT; i++)
		{
			peaks[i] = new RbjFilter(mixRate);
		}
	}

	public void Recalc()
	{
		peaks[0].Recalc(FilterType.BANDPASS_CSG, peak0, q, gain, false);
		peaks[1].Recalc(FilterType.BANDPASS_CSG, peak1, q, gain, false);
	}

	public float Filter(float in0)
	{
		return (peaks[0].Filter(in0) + peaks[1].Filter(in0)) / 2.0f;
	}

	public void Reset()
	{
		for(int i=0; i<PEAKCOUNT; i++)  peaks[i].Reset();
	}
}

