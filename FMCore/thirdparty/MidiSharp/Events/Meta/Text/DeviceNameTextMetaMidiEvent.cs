﻿//----------------------------------------------------------------------- 
// <copyright file="DeviceNameTextMetaMidiEvent.cs" company="Stephen Toub"> 
//     Copyright (c) Stephen Toub. All rights reserved. 
// </copyright> 
//----------------------------------------------------------------------- 

namespace MidiSharp.Events.Meta.Text
{
    /// <summary>A device name meta event.</summary>
    public sealed class DeviceNameTextMidiEvent : BaseTextMetaMidiEvent
    {
        /// <summary>The meta id for this event.</summary>
        internal const byte MetaId = 0x9;

        /// <summary>Initialize the device name meta event.</summary>
        /// <param name="deltaTime">The amount of time before this event.</param>
        /// <param name="text">The text associated with the event.</param>
        public DeviceNameTextMidiEvent(long deltaTime, string text) : base(deltaTime, MetaId, text) { }
    }
}
