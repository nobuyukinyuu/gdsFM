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
        public static double tickLen=441; 
        static double carryover;  //How much did we miss the tick by?  This amount is added to the last tick frame to estimate the next one.
        public static int threshold=441;  //Like tickLen, but in absolute frames.  Reset every tick based on previous offset.
        static int ticks;  //Counts number of ticks elapsed in the entire sequence
        static int nextTickFrame;  //The frame count the next tick needs to be before checking for event processing.

        static int[] lastEventTime = new int[16];  //Last tick counter reading when the previous event fired.
        static int[] eventPos = new int[16];  //Which event are we looking at?




        public static void Reset(int numTracks=16)
        {
            frames=0; beatLen = 480000; ticksPerBeat = 48;
            eventPos = new int[numTracks];
            lastEventTime = new int[numTracks];
            ticks=0; nextTickFrame=0;

        }


        public static void SetTempo(int beatLength, int divider)
        {
            beatLen = beatLength;  ticksPerBeat = divider;
            // tickLen = (beatLen / (double)ticksPerBeat) / 1000.0;  //Tick length in ms
            tickLen = (sample_rate * beatLen) / (double)ticksPerBeat / 1000000.0; //Tick length in frames
            threshold = (int) tickLen;
        }

        // After each iteration, check if timeElapsed[channel] >= events[eventPos].DeltaTime. 
        // If so, subtract DeltaTime from timeElapsed[channel] and move eventPos[channel]+=1.
        public static void Iterate(int nFrames)
        {
            frames += nFrames;
        }

        public static void Iterate()
        {
            frames++;
        }


        /// Returns a list containing the next set of events to process.
        public static List<MidiSharp.Events.MidiEvent> CheckForEvents(MidiSequence sequence)
        {
            if (frames != nextTickFrame) return null;
            var output = new List<MidiSharp.Events.MidiEvent>();

            for(int i=0; i< sequence.Tracks.Count; i++)
            {
                while(true)  //Infinite loop until output return
                {
                    var eventsRemain = EventsRemain(sequence,i);
                    if (!eventsRemain) break;  //No more events for this track.  Proceed to next track.

                    var ev = sequence.Tracks[i].Events[eventPos[i]]; //Get current event.
                    var eventDelta = ev.DeltaTime;
                    var offset = ticks - lastEventTime[i];

                    if (offset >= eventDelta)  //Then there's an event here we have to process.
                    // if (offset >= cachedDelta[i])  //Then there's an event here we have to process.
                    {

                        // if (ev is MidiSharp.Events.Meta.TempoMetaMidiEvent) //Process immediately
                        // {
                        //     var tmp= (MidiSharp.Events.Meta.TempoMetaMidiEvent) ev;
                        //     SetTempo(tmp.Value, sequence.TicksPerBeatOrFrame);
                        // } else {
                        //     //Add the current event to the output queue.  Check for events remaining.
                        //     //We'll try checking if we need to process the next event on the next loop.
                        //     output.Add(ev);
                        // }

                        output.Add(ev);

                        eventPos[i] += 1;
                        lastEventTime[i] = ticks; 

                        continue;
                    } else { 
                        break;  //No more events have passed in this track yet, proceed to next track.
                    }
                }
            }
            //All tracks processed.  Recalculate a new tick time frame for the next check.

            // if (ticks % ticksPerBeat == 0)  //Resync on beat
            // {
            //     nextTickFrame = (int)(frames + tickLen + carryover);  //Adding the carryover when it's >0 adds a frame or 2 to the next tick position.
            //     // carryover = 0;
            // } else {
                nextTickFrame = frames + threshold;
            // }

            // carryover = ((tickLen-(int)tickLen) + carryover) % 2;  //Always produce a value that rounds down to 2 at most.


            ticks++;  //Move up the tick counter.

            return output;  //List of events from all tracks that need to be triggered.  The MIDI channel is determined by the event.
        }


        /// Checks if any events remain on the given track to be processed.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static bool EventsRemain(MidiSequence sequence, int ch) { return eventPos[ch] <= sequence.Tracks[ch].Events.Count-1; }



        public static float SecondsElapsed {get => frames / (float)sample_rate;}
        // public static float BeatsElapsed {get => (float)ticksElapsed / (float)ticksPerBeat;}
        // public static float BeatsElapsed {get => frames / (float)tickFrameLen / ticksPerBeat;}
        public static float BeatsElapsed {get => (float) ((frames / sample_rate) / (beatLen/1000000.0))  ;}


    }
}