//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
using System;
using System.ComponentModel;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;
using OpenTap;
using OpenTapTraceSource = OpenTap;
using Keysight.Plugins.Python;

namespace Keysight.OpenTap.Plugins.Python
{
    [PythonWrapper.WrappedType(typeof(PythonResultListener))]
    [Obfuscation(Exclude = true)]
    public abstract class PythonResultListenerWrapper : PythonWrapper, IResultListener
    {
        protected override void load(string name, string moduleName)
        {
            base.load(name, moduleName);
            Name = getShortName();
        }
        
        private string getShortName()
        {
            return this.Name;
        }

        #region Members
        public new TraceSource log = OpenTapTraceSource.Log.CreateSource("ResultListener");
        protected PythonResultListener resultListener { get { return PythonObject.As<PythonResultListener>(); } }
        #endregion

        public override string ToString()
        {
            return resultListener.ToString();
        }

        #region IResultListener Implementation
        public virtual void OnResultPublished(Guid stepRunID, ResultTable result)
        {
            resultListener.OnResultPublished(stepRunID, result);
        }

        public virtual void OnTestPlanRunCompleted(TestPlanRun planRun, Stream logStream)
        {
            resultListener.OnTestPlanRunCompleted(planRun, logStream);
        }

        public virtual void OnTestPlanRunStart(TestPlanRun planRun)
        {
            resultListener.OnTestPlanRunStart(planRun);
        }

        public virtual void OnTestStepRunCompleted(TestStepRun stepRun)
        {
            resultListener.OnTestStepRunCompleted(stepRun);
        }

        public virtual void OnTestStepRunStart(TestStepRun stepRun)
        {
            resultListener.OnTestStepRunStart(stepRun);
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
            get { return resultListener.IsConnected; }
            set { resultListener.IsConnected = value; }
        }
        //
        // Summary:
        //     Default log that the resource object can write to. Typically used by instances
        //     and extensions of the Resource object.
        [XmlIgnore]
        public TraceSource Log
        {
            get { return resultListener.Log; }
        }
        //
        // Summary:
        //     A short name to display in the user interface in areas with limited space.
        [Browsable(false)]
        [Display("Name", null, "Common", -3, false, null)]
        public string Name
        {
            get { return resultListener.Name; }
            set {

               resultListener.Name = value;
                OnPropertyChanged("Name");
            }
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
            resultListener.Close();
        }
        
        //
        // Summary:
        //     Triggers the ActivityStateChanged event.
        public void OnActivity()
        {
            resultListener.OnActivity();
        }
        //
        // Summary:
        //     When overridden in a derived class, opens a connection to the resource represented
        //     by this class.
        public virtual void Open()
        {
            resultListener.Open();
        }

        #endregion

    }
}
