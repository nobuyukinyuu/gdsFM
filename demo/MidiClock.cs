using Godot;
using System;
using System.Collections.Generic;
using MidiSharp;

namespace MidiDemo
{
    static class Clock
    {
        static int frames;  //Frame counter.  Samples elapsed.
        static int beatLen = 480000;  //beat length, in microseconds.  Set by tempo events.
        static int ticksPerBeat = 480;  //Beat divider.  Use this with beatLen to calculate a tick length.


        //Tick timer iterated by the tempo value (microsecs/beat) divided by the tick divider.
        //When this value exceeds the delta time of the next event, the delta time is subtracted and the event executed.
        static double[] ticksElapsed = new double[16];  //Tick time accumulator, in ticks.
        static int[] eventPos = new int[16];  //Which event are we looking at?

        static bool[] noEventsLeft = new bool[16]; //Is true if there's no more events to process in the track.

        static void Reset()
        {
            frames=0; beatLen = 480000; ticksPerBeat = 480;
            ticksElapsed = new double[16];
            eventPos = new int[16];
            noEventsLeft = new bool[16];
        }

        public static void SetTempo(int beatLength, int divider)
        {
            beatLen = beatLength;  ticksPerBeat = divider;
        }

        // After each iteration, check if timeElapsed[channel] >= events[eventPos].DeltaTime. 
        // If so, subtract DeltaTime from timeElapsed[channel] and move eventPos[channel]+=1.
        public static double Iterate(int nFrames=1, int sample_rate=44100)
        {
            var tickLen = (beatLen / (double)ticksPerBeat) / 1000.0;  //Tick length in ms
            var frameLen =  nFrames/ (double)sample_rate; //Audio frame length in ms
            var totalTime = frameLen / tickLen;  //Amount of ticks elapsed.  A frame will always be a partial tick.
            for (int i=0; i < 16; i++)
            {
                ticksElapsed[i] += totalTime; 
            }

            return tickLen;
        }

        public static Dictionary<int, MidiSharp.Events.MidiEvent> CheckForEvents(MidiSequence sequence)
        {
            var output = new Dictionary<int, MidiSharp.Events.MidiEvent>();
            for (int i=0; i < sequence.Tracks.Count; i++)
            {
                var ev = sequence.Tracks[i].Events[eventPos[i]];
                if (ticksElapsed[i] >= ev.DeltaTime)  
                {
                    output.Add(i, ev);
                    noEventsLeft[i] = !NextEvent(i, ev.DeltaTime, sequence);
                }
            }
            return output;
            //If output.Empty then nothing new to process, otherwise switch() for the event processor and do appropriate thing there.
        }

        /// Takes the leftover time between checks of the ticksElapsed timer where our buffer missed
        /// and adds it to a newly reset timer.  Moves the track event position forward.
        static bool NextEvent(int channel, double deltaTicks, MidiSequence sequence)
        {
            // var deltaTime = deltaTicksPassed * (beatLen / (double)ticksPerBeat) / 1000.0;  //Amount of time before next event, in ms.
            ticksElapsed[channel] -= deltaTicks;
            eventPos[channel] += 1;

            //Return false if there are no more events to process on this track.
            return eventPos[channel] < sequence.Tracks[channel].Events.Count;
        }
    }

}