using Godot;
using System;
using System.Collections.Generic;
using MidiSharp;

namespace MidiDemo
{
    static class Clock
    {
        public static double sample_rate = 44100;  //Be sure to change this if in your own project you use a different sample rate
        public static int frames;  //Frame counter.  Samples elapsed.
        public static int beatLen = 480000;  //beat length, in microseconds.  Set by tempo events.
        public static int ticksPerBeat = 48;  //Beat divider.  Use this with beatLen to calculate a tick length.
        public static double tickLen=10;  // Tick length in ms;  set automatically. Equal to beatLen/ticksPerBeat/1000


        //Tick timer iterated by the tempo value (microsecs/beat) divided by the tick divider.
        //When this value exceeds the delta time of the next event, the delta time is subtracted and the event executed.
        public static double[] deltaTicks = new double[16];  //Tick time accumulator, in ticks.  This is a delta value since last event.
        public static double ticksElapsed;  //Master tick counter.
        static int[] eventPos = new int[16];  //Which event are we looking at?

        static bool[] noEventsLeft = new bool[16]; //Is true if there's no more events to process in the track.

        public static void Reset()
        {
            frames=0; beatLen = 480000; ticksPerBeat = 48;
            deltaTicks = new double[16];
            ticksElapsed=0;
            eventPos = new int[16];
            noEventsLeft = new bool[16];
        }

        public static void SetTempo(int beatLength, int divider)
        {
            beatLen = beatLength;  ticksPerBeat = divider;
            tickLen = (beatLen / (double)ticksPerBeat) / 1000.0;  //Tick length in ms
        }

        // After each iteration, check if timeElapsed[channel] >= events[eventPos].DeltaTime. 
        // If so, subtract DeltaTime from timeElapsed[channel] and move eventPos[channel]+=1.
        public static double Iterate(int nFrames=1, int channel=0)
        {
            frames += nFrames;
            var frameLen =  nFrames*1000 / (double)sample_rate; //Iteration length in ms
            var totalTime = frameLen / tickLen ;  //Amount of ticks elapsed.
            ticksElapsed += totalTime;

            deltaTicks[channel] += totalTime; 

            return tickLen;
        }

        // TODO:  consider making the value a stack instead so multiple event misses per channel can be added and processed at once.
        /// Returns a dictionary containing the next set of events to process for each channel.
        public static Dictionary<int, MidiSharp.Events.MidiEvent> CheckForEvents(MidiSequence sequence)
        {
            var output = new Dictionary<int, MidiSharp.Events.MidiEvent>();
            for (int i=0; i < Math.Min(16, sequence.Tracks.Count); i++)
            {
                if (noEventsLeft[i]) continue;

                var ev = sequence.Tracks[i].Events[eventPos[i]];
                if (deltaTicks[i] >= ev.DeltaTime)  
                {
                    output.Add(i, ev);
                    noEventsLeft[i] = !ReadyNextEvent(i, ev.DeltaTime, sequence);  
                }
            }
            return output;
            //If output.Empty then nothing new to process, otherwise switch() for the event processor and do appropriate thing there.
        }

        /// Takes the leftover time between checks of the ticksElapsed timer where our buffer missed
        /// and adds it to a newly reset timer.  Moves the track event position forward.
        static bool ReadyNextEvent(int channel, double timeSinceLastEvent, MidiSequence sequence)
        {
            deltaTicks[channel] -= timeSinceLastEvent;
            eventPos[channel] += 1;

            //Return false if there are no more events to process on this track.
            return eventPos[channel] < sequence.Tracks[channel].Events.Count;
        }

        public static float SecondsElapsed {get => BeatsElapsed * beatLen / 1000000f;}
        public static float BeatsElapsed {get => (float)ticksElapsed / (float)ticksPerBeat;}


    }
}