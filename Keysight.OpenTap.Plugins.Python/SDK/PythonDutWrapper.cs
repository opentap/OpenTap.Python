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
    [PythonWrapper.WrappedType(typeof(PythonDut))]
    [Obfuscation(Exclude = true)]
    public abstract class PythonDutWrapper : PythonWrapper, IDut
    {
        protected PythonDut pythonDut => PythonObject.As<PythonDut>();

        protected override void load(string name, string moduleName)
        {
            base.load(name, moduleName);
            Name = getShortName();
        }

        private string getShortName()
        {
            return this.Name;
        }

        #region Override, Implementation Methods

        public override string ToString()
        {
            return pythonDut.ToString();
        }

        #region Dut
        //
        // Summary:
        //     A comment to associate with this DUT. Prompted for at test plan run.
        [Display("Comment", "A comment related to the DUT associated with results in the database.", "Common", 1, false,
             null)]
        [MetaData(true)]
        public string Comment
        {
            get { return pythonDut.Comment; }
            set { pythonDut.Comment = value; }
        }
        //
        // Summary:
        //     DUT name uniquely identifying it (e.g. its IMEI number). Prompted for at test
        //     plan run.
        [Display("ID", "ID identifying the DUT associated with results in the database.", "Common", -10000, false, null) ]
        [MetaData(true)]
        public string ID
        {
            get { return pythonDut.ID;  }
            set { pythonDut.ID = value; }
        }
        
        #endregion

        #region Resource

        //
        // Summary:
        //     Indicates whether this DUT is currently connected. This value should be set by
        //     Open() and Close().
        [Browsable(false)]
        [XmlIgnore]
        public bool IsConnected
        {
            get { return pythonDut.IsConnected; }
            set { pythonDut.IsConnected = value; }
        }
        //
        // Summary:
        //     Default log that the resource object can write to. Typically used by instances
        //     and extensions of the Resource object.
        [XmlIgnore]
        public TraceSource Log
        {
            get { return pythonDut.Log; }
        }
        //
        // Summary:
        //     A short name to display in the user interface in areas with limited space.
        [Browsable(false)]
        [Display("Name", null, "Common", -3, false, null)]
        public string Name
        {
            get { return pythonDut.Name; } 
            set { pythonDut.Name = value; }
        }

        //
        // Summary:
        //     Invoked on activity.
        public event EventHandler<EventArgs> Activity;
        
        //
        // Summary:
        //     When overridden in a derived class, closes the connection made to the resource
        //     represented by this class.
        public virtual void Close()
        {
            pythonDut.Close();
        }
        //
        // Summary:
        //     Closes the resource.
        public void Dispose()
        {
        }
        //
        // Summary:
        //     Triggers the ActivityStateChanged event.
        public void OnActivity()
        {
            pythonDut.OnActivity();
        }
        //
        // Summary:
        //     When overridden in a derived class, opens a connection to the resource represented
        //     by this class.
        public virtual void Open()
        {
            pythonDut.Open();
        }

        #endregion


        #endregion
    }
}
