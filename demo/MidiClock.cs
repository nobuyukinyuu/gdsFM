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
        public static double tickLen=10;  // Tick length in ms;  set automatically. Equal to beatLen/ticksPerBeat/1000

        //Tick length in sample frames. Calculated based on sample_rate * beatLen/ticksPerBeat/1000000.
        //Useful to determine how many frames we can get away with skipping without worrying about an event miss.
        public static double tickFrameLen=441; 


        //Tick timer iterated by the tempo value (microsecs/beat) divided by the tick divider.
        //When this value exceeds the delta time of the next event, the delta time is subtracted and the event executed.
        public static double[] deltaTicks = new double[16];  //Tick time accumulator, in ticks.  This is a delta value since last event.
        public static double ticksElapsed;  //Master tick counter.
        static int[] eventPos = new int[16];  //Which event are we looking at?

        static bool[] noEventsLeft = new bool[16]; //Is true if there's no more events to process in the track.


        //Absolute number of frames that need to elapse before processing the next event in the track.
        static int[] nextEventTimeFrame = new int[16];

        static int threshold = 1;  //Any events with a delta time lower than this get added to the process queue.

        public static void Reset(int numTracks=16)
        {
            frames=0; beatLen = 480000; ticksPerBeat = 48;
            deltaTicks = new double[numTracks];
            ticksElapsed=0;
            eventPos = new int[numTracks];
            noEventsLeft = new bool[numTracks];

            nextEventTimeFrame = new int[numTracks];
        }


        public static void SetTempo(int beatLength, int divider)
        {
            beatLen = beatLength;  ticksPerBeat = divider;
            tickLen = (beatLen / (double)ticksPerBeat) / 1000.0;  //Tick length in ms
            tickFrameLen = (sample_rate * beatLen) / (double)ticksPerBeat / 1000000.0;
        }

        // After each iteration, check if timeElapsed[channel] >= events[eventPos].DeltaTime. 
        // If so, subtract DeltaTime from timeElapsed[channel] and move eventPos[channel]+=1.
        public static double Iterate(int nFrames=1, int channel=0)
        {
            frames += nFrames;
            // var frameLen =  nFrames*1000 / (double)sample_rate; //Iteration length in ms
            // var totalTime = frameLen / tickLen ;  //Amount of ticks elapsed.
            // ticksElapsed += totalTime;

            // deltaTicks[channel] += totalTime; 

            return tickLen;
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
                    // if (!eventsRemain) break;  //Exit early and proceed to next track.  No more events on this track.

                    var ev = sequence.Tracks[i].Events[eventPos[i]]; //Get current event.
                    if (eventPos[i]==0) //We're at the first event, so set the next event time frame to the current event.
                        nextEventTimeFrame[i] = (int) (ev.DeltaTime * tickFrameLen);

                    //Now check if we've already exceeded the next event fime frame.
                    if (frames >= nextEventTimeFrame[i])  //Then there's an event here we have to process.
                    {
                        //Add the current event to the output queue.  Check for events remaining.
                        //We'll try checking if we need to process the next event on the next loop.
                        output.Add(ev);

                        //If events are left on this track, calculate the next event timeframe.
                        if (eventsRemain)
                        {
                            eventPos[i] += 1;
                            ev = sequence.Tracks[i].Events[eventPos[i]];  //Get the next event so we can get the next event time.

                            if (ev is MidiSharp.Events.Meta.TempoMetaMidiEvent)
                            {
                                //Process immediately. We need to change the tempo before calculating the next event frame.
                                //Otherwise, the number of frames per tick will be wrong and mess up the next event time.
                                var tempo = (MidiSharp.Events.Meta.TempoMetaMidiEvent) ev;
                                SetTempo(tempo.Value, sequence.TicksPerBeatOrFrame);
                            }

                            // if (ev is MidiSharp.Events.Voice.Note.OnNoteVoiceMidiEvent)
                            //     System.Diagnostics.Debugger.Break();

                            //Figure out the number of frames we missed the previously-processed event by.
                            //Then, set the next event time frame to where the frame counter needs to be.
                            var offset = frames - nextEventTimeFrame[i]; //nextEventTimeFrame here still references the previous event time.
                            nextEventTimeFrame[i] = frames - offset + (int)(ev.DeltaTime*tickFrameLen); //now it references the next event time.
                        } else {  //Unless there's no events left, then just exit the loop and proceed to next track.
                            nextEventTimeFrame[i] = int.MaxValue;
                            break;
                        }
                    } else { //If we haven't crossed the next event timeframe, then...
                        break;  //No more events have passed in this channel, so no more need to be queued and we can leave the loop.
                    }
                    //Continue to the next event on this track and see if we've passed it or landed on it.
                }
            }
            return output;  //List of events from all tracks that need to be triggered.  The MIDI channel is determined by the event.
        }

        /// Checks if any events remain on the given track to be processed.
        static bool EventsRemain(MidiSequence sequence, int ch) { return eventPos[ch] < sequence.Tracks[ch].Events.Count-1; }

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