using Godot;
using System;
using System.Collections.Generic;
using MidiSharp;

namespace MidiDemo
{
    static class Clock
    {
        public static double sample_rate = 44100;  //Be sure to change this if in your own project you use a different sample rate
        public static int frames;  //Frame counter.  Samples elapsed.  MaxValue is about 12 hours at 48000hz.
        public static int beatLen = 480000;  //beat length, in microseconds.  Set by tempo events.
        public static int ticksPerBeat = 48;  //Beat divider.  Use this with beatLen to calculate a tick length.
        // public static double tickLen=10;  // Tick length in ms;  set automatically. Equal to beatLen/ticksPerBeat/1000

        //Tick length in sample frames. Calculated based on sample_rate * beatLen/ticksPerBeat/1000000.
        //Useful to determine how many frames we can get away with skipping without worrying about an event miss.
        public static double tickFrameLen=441; 


        //Tick timer iterated by the tempo value (microsecs/beat) divided by the tick divider.
        //When this value exceeds the delta time of the next event, the delta time is subtracted and the event executed.
        public static double[] deltaTicks = new double[16];  //Tick time accumulator, in ticks.  This is a delta value since last event.
        static int[] eventPos = new int[16];  //Which event are we looking at?

        static bool[] noEventsLeft = new bool[16]; //Is true if there's no more events to process in the track.


        //Absolute number of frames that need to elapse before processing the next event in the track.
        static int[] lastEventTimeFrame = new int[16];
        static MidiSharp.Events.MidiEvent[] cachedEvent = new MidiSharp.Events.MidiEvent[16];  //The next event waiting to be processed.
        static double[] cachedDelta = new double[16]; //The absolute time, in frames, of the next event waiting to be processed.
        static bool[] eventsAreCached = new bool[16]; 

        static int threshold = 1;  //Any events with a delta time lower than this get added to the process queue.

        public static void Reset(int numTracks=16)
        {
            frames=0; beatLen = 480000; ticksPerBeat = 48;
            // deltaTicks = new double[numTracks];
            // ticksElapsed=0;
            eventPos = new int[numTracks];
            // noEventsLeft = new bool[numTracks];

            lastEventTimeFrame = new int[numTracks];
            cachedDelta = new double[numTracks];
            cachedEvent = new MidiSharp.Events.MidiEvent[numTracks];
            eventsAreCached = new bool[numTracks];
        }


        public static void SetTempo(int beatLength, int divider)
        {
            beatLen = beatLength;  ticksPerBeat = divider;
            // tickLen = (beatLen / (double)ticksPerBeat) / 1000.0;  //Tick length in ms
            tickFrameLen = (sample_rate * beatLen) / (double)ticksPerBeat / 1000000.0;

            //We need to force trigger a recalculation of the next delta event time for cached events.
            for (int i=0; i<eventsAreCached.Length; i++)  eventsAreCached[i] = false;
        }

        // After each iteration, check if timeElapsed[channel] >= events[eventPos].DeltaTime. 
        // If so, subtract DeltaTime from timeElapsed[channel] and move eventPos[channel]+=1.
        public static void Iterate(int nFrames=1, int channel=0)
        {
            frames += nFrames;
            // var frameLen =  nFrames*1000 / (double)sample_rate; //Iteration length in ms
            // var totalTime = frameLen / tickLen ;  //Amount of ticks elapsed.
            // ticksElapsed += totalTime;

            // deltaTicks[channel] += totalTime; 

            // return tickLen;
        }

        /// Returns a list containing the next set of events to process.
        public static List<MidiSharp.Events.MidiEvent> CheckForEvents(MidiSequence sequence)
        {
            var output = new List<MidiSharp.Events.MidiEvent>();

            // var currentTicks = frames / tickFrameLen;
            for(int i=0; i< sequence.Tracks.Count; i++)
            {
                while(true)  //Infinite loop until output return
                {
                    var eventsRemain = EventsRemain(sequence,i);
                    if (!eventsRemain) break;

                    //TODO:  Consider caching these between clock events until the next delta passes and THEN set them.
                    //      In order to do this, we need to wait until the previous events were proccessed!
                    var ev = sequence.Tracks[i].Events[eventPos[i]]; //Get current event.
                    var eventDelta = ev.DeltaTime * tickFrameLen; //The delta offset we need to pass to add this event to the queue.
                    var offset = frames-lastEventTimeFrame[i]; //The delta offset of the current frame counter.

                    // if (!eventsAreCached[i]) //Cache a new event. This lowers processing overhead for checking the next delta.
                    // {
                    //     Recache(i, sequence);
                    //     eventsAreCached[i] = true;
                    // }
                    // var offset = frames-lastEventTimeFrame[i];

                    //Now check if we've already exceeded the last event fime frame.
                    if (offset >= eventDelta)  //Then there's an event here we have to process.
                    // if (offset >= cachedDelta[i])  //Then there's an event here we have to process.
                    {

                        if (ev is MidiSharp.Events.Meta.TempoMetaMidiEvent) //Process immediately
                        {
                            var tmp= (MidiSharp.Events.Meta.TempoMetaMidiEvent) ev;
                            SetTempo(tmp.Value, sequence.TicksPerBeatOrFrame);
                        } else {
                            //Add the current event to the output queue.  Check for events remaining.
                            //We'll try checking if we need to process the next event on the next loop.
                            output.Add(ev);
                            // output.Add(cachedEvent[i]);

                        }
                        eventPos[i] += 1;

                        lastEventTimeFrame[i] = frames; //- (int)(offset-eventDelta);  //FIXME:  Math.Round may be unnecessary
                        // lastEventTimeFrame[i] = frames - (int)(offset - cachedDelta[i]);  

                        eventsAreCached[i] = false; //Need to cache new event on next loop.
                        cachedEvent[i] = null;
                        continue;
                    } else { 
                        break;  //No more events have passed in this track yet, proceed to next track.
                    }
                }
            }
            return output;  //List of events from all tracks that need to be triggered.  The MIDI channel is determined by the event.
        }

    public static void Recache(int i, MidiSequence sequence, bool fetchEvent=true)
    {
        if (fetchEvent) cachedEvent[i] = sequence.Tracks[i].Events[eventPos[i]]; //Get current event.

        if (cachedEvent[i] is MidiSharp.Events.Meta.TempoMetaMidiEvent)
            {
            var ev= (MidiSharp.Events.Meta.TempoMetaMidiEvent) cachedEvent[i];
                SetTempo(ev.Value, sequence.TicksPerBeatOrFrame);
            }

        cachedDelta[i] = (cachedEvent[i].DeltaTime * tickFrameLen);         
    }

        /// Checks if any events remain on the given track to be processed.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static bool EventsRemain(MidiSequence sequence, int ch) { return eventPos[ch] <= sequence.Tracks[ch].Events.Count-1; }

        /// Takes the leftover time between checks of the ticksElapsed timer where our buffer missed
        /// and adds it to a newly reset timer.  Moves the track event position forward.
        static bool ReadyNextEvent(int channel, double timeSinceLastEvent, MidiSequence sequence)
        {
            deltaTicks[channel] -= timeSinceLastEvent;
            eventPos[channel] += 1;

            //Return false if there are no more events to process on this track.
            return eventPos[channel] < sequence.Tracks[channel].Events.Count;
        }

        public static float SecondsElapsed {get => frames / (float)sample_rate;}
        // public static float BeatsElapsed {get => (float)ticksElapsed / (float)ticksPerBeat;}
        // public static float BeatsElapsed {get => frames / (float)tickFrameLen / ticksPerBeat;}
        public static float BeatsElapsed {get => (float) ((frames / sample_rate) / (beatLen/1000000.0))  ;}


    }
}