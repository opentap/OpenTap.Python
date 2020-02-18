//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
using System;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Serialization;
using OpenTap;
using Keysight.Plugins.Python;

namespace Keysight.OpenTap.Plugins.Python
{
    [PythonWrapper.WrappedType(typeof(PythonInstrument))]
    [Obfuscation(Exclude = true)]
    public abstract class PythonInstrumentWrapper : PythonWrapper, IInstrument
    {
        
        PythonInstrument instr { get { return PythonObject.As<PythonInstrument>(); } }

        public override string ToString()
        {
            return instr.ToString();
        }

        //
        // Summary:
        //     Indicates whether this DUT is currently connected. This value should be set by
        //     Open() and Close().
        [Browsable(false)]
        [XmlIgnore]
        public bool IsConnected
        {
            get { return instr.IsConnected; }
            set { instr.IsConnected = value; }
        }
        //
        // Summary:
        //     Default log that the resource object can write to. Typically used by instances
        //     and extensions of the Resource object.
        [XmlIgnore]
        public TraceSource Log
        {
            get { return instr.Log; }
        }
        //
        // Summary:
        //     A short name to display in the user interface in areas with limited space.
        [Browsable(false)]
        [Display("Name", null, "Common", -3, false, null)]
        public string Name
        {
            get { return instr.Name; }
            set { instr.Name = value; }
        }

        //
        // Summary:
        //     Invoked on activity.
        public event EventHandler<EventArgs> Activity;

        //
        // Summary:
        //     When overridden in a derived class, closes the connection made to the resource
        //     represented by this class.
        public virtual void Close() { instr.Close(); }
        //
        // Summary:
        //     Triggers the ActivityStateChanged event.
        public void OnActivity() { instr.OnActivity(); }
        //
        // Summary:
        //     When overridden in a derived class, opens a connection to the resource represented
        //     by this class.
        public virtual void Open() { instr.Open(); }

        protected override void load(string name, string moduleName)
        {
            base.load(name, moduleName);
            Name = getShortName();
        }

        private string getShortName()
        {
            return this.Name;
        }

    }
}
