using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
// using System.Buffers;

using MidiSharp;
using MidiSharp.Events.Meta;
using MidiSharp.Events.Voice;
using MidiSharp.Events.Voice.Note;

namespace MidiDemo{

    public class MidiEventParser
    {
        /// Actions to take for each MIDI type on a given action. Used to automatically process actions on a given frame.
        //Lookup the action using the type of the midi event
        public Dictionary<System.Type, Action<MidiSharp.Events.MidiEvent> > ActionSet = new Dictionary<Type, Action<MidiSharp.Events.MidiEvent> >(16);

        // public Dictionary<int, List<MidiSharp.Events.MidiEvent> > eventsAtPosition = new Dictionary<int, List<MidiSharp.Events.MidiEvent>>(960);
        public EventMap eventsAtPosition = new EventMap();

        public void ParseSequence(MidiSequence s, double sample_rate=44100.0)
        {
            //First MIDI track should always contain the relevant tempo data.  We need to process this data to build a tempo map between ticks,
            //Translate each tick to a frame position, and push an event to a stack located at the given frame of our lookup dictionary.
            if (s.Tracks.Count==0) return;

            eventsAtPosition.Clear();
            ActionSet.Clear();

            //Initial tempo is == 120bpm
             int beatLen = 480000;  //beat length, in microseconds.  Set by tempo events.  
             int divider = s.TicksPerBeatOrFrame > 0?  s.TicksPerBeatOrFrame : 48;  //Beat divider.  Use this with beatLen to calculate a tick length.

            //Tick length in sample frames. Calculated based on sample_rate * beatLen/ticksPerBeat/1000000.
            //Useful to determine how many frames we can get away with skipping without worrying about an event miss.
             double tickLen=441; 

            TempoMap tempoMap = new TempoMap();  //List of frame positions for every tick. Use to stuff events into position by looking for closest index
            int frameOffset = 0;  //Update this every new tempo event found to account for shifts in deltas. The max value is ~12h at 48000hz.


            //Build the tempo map.
            foreach (MidiSharp.Events.MidiEvent m_event in s.Tracks[0])
            {
                if (!(m_event is TempoMetaMidiEvent)) continue;
                var ev = (TempoMetaMidiEvent) m_event;

                //Calculate the frames for each tick leading up to this next tempo event. Events on tick 0 will skip this and immediately update tempo.
                //Once the tempo map is built,  each tick index will return a corresponding frame until the end of track 0.  Any tick indices beyond
                //the list size should be calculated based on the last known tempo.
                //TODO:  When going through all track events and their deltas, should we extend the list further? Or will precalculating the 
                //      event map, while slower initially, make keeping the tempo map completely unnecessary?
                
                for(int i=0; i < ev.DeltaTime; i++)
                {
                    //Round to the nearest frame.
                    tempoMap.Add((int) Math.Round(frameOffset + i * tickLen));
                }

                //Update the frame offset to the frame where this event should exist.
                frameOffset = frameOffset + (int) Math.Round(ev.DeltaTime * tickLen);

                //Now update the actual tempo for the next operation.
                beatLen = ev.Value;
                tickLen = (sample_rate * beatLen) / (double)divider / 1000000.0; //Tick length in frames
            }

            // Now that all events on track 0 are processed, update the tempo map with the last known tempo value so we can calculate frames
            // for the rest of the events on the other tracks should their delta offset exceed the last tempo event.
            tempoMap.tickLen = tickLen;
            tempoMap.frameOffset = frameOffset;


            //Now, iterate through all tracks and events and push them to the event map.
            // foreach (MidiTrack track in s.Tracks) //Assume enumerator moves in order...
            for(int t=1; t<s.Tracks.Count; t++) //Assume enumerator moves in order...
            {
                MidiTrack track = s.Tracks[t];
                int offset = 0;
                for(int i = 0; i < track.Events.Count; i++)
                {
                    var ev = track.Events[i];
                    var frame = tempoMap.CalcFrame((int)(ev.DeltaTime + offset));

                    List<MidiSharp.Events.MidiEvent> list;
                    if (eventsAtPosition.ContainsKey(frame)) //Make a new list if this key needs it.
                        {
                           list = eventsAtPosition[frame]; 
                        } else { 
                            list = new List<MidiSharp.Events.MidiEvent>(); 
                            eventsAtPosition.Add(frame, list);
                        }

                    //Add the event to the list at this frame position.  TODO:  organize by MIDI channel?
                    list.Add(ev);

                    offset += (int) ev.DeltaTime;  //Move up the delta timer.
                }
            }
        }

    }

    class TempoMap : List<int>
    {
        public double tickLen=441; //Length (in frames) of a tick calculated from the last known tempo change event.
        public int frameOffset = 0;  //Frame offset of the last known tempo change event.


        /// Calculates a frame that a given tick would appear in the sequence using a provided tempo map.
        public int CalcFrame (int idx)
        {
            if (idx >= Count) //Manually calculate the frame index.
            {
                var ticksOver = (idx - Count) +1;  //Starts at 1 at the first tick index after the last tempo event.
                return (int) Math.Round(frameOffset + ticksOver * tickLen);
            } else {  //Use what we know from the previous tempo map.
                return this[idx];
            }
        }

    }

    /// Represents a map of events that exists at a given frame.
    public class EventMap : Dictionary<int, List<MidiSharp.Events.MidiEvent>>
    {
        public List<MidiSharp.Events.MidiEvent> Fetch(int frame)
        {
            List<MidiSharp.Events.MidiEvent> output;
            this.TryGetValue(frame, out output);

            return output?? (List<MidiSharp.Events.MidiEvent>) System.Linq.Enumerable.Empty<MidiSharp.Events.MidiEvent>();
        }
    }

}

