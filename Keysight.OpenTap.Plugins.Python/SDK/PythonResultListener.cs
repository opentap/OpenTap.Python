//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using OpenTap;
using Keysight.Plugins.Python;
using Python.Runtime;

namespace Keysight.OpenTap.Plugins.Python
{
    [Browsable(false)]
    [Obfuscation(Exclude = true)]
    public class PythonResultListener : ResultListener, IPythonProxy
    {
        public IPythonWrapper Wrapper { get; set; }
        public PythonResultListener()
        {
            Name = getShortName();
        }

        private string getShortName()
        {
            return this.Name;
        }
     
        PyObject pyObj
        {
            get { return ((IPythonWrapper)Wrapper).PythonObject; }
        }

        public override void Open()
        {
            base.Open();
            PyThread.Invoke(() => pyObj.InvokeMethod(nameof(Open)));
        }

        public override void Close()
        {
            base.Close();
            PyThread.Invoke(() => pyObj.InvokeMethod(nameof(Close)));
        }
        
        public override void OnResultPublished(Guid stepRunId, ResultTable result)
        {
            base.OnResultPublished(stepRunId, result);
            object id = stepRunId;
            PyThread.Invoke(() => pyObj.InvokeMethod(nameof(OnResultPublished),id.ToPython(), result.ToPython()));
        }

        public override void OnTestPlanRunCompleted(TestPlanRun planRun, Stream logStream)
        {
            base.OnTestPlanRunCompleted(planRun, logStream);
            PyThread.Invoke(() => pyObj.InvokeMethod(nameof(OnTestPlanRunCompleted), planRun.ToPython(), logStream.ToPython()));
        }

        public override void OnTestPlanRunStart(TestPlanRun planRun)
        {
            base.OnTestPlanRunStart(planRun);
            PyThread.Invoke(() => pyObj.InvokeMethod(nameof(OnTestPlanRunStart),planRun.ToPython()));
        }

        public override void OnTestStepRunCompleted(TestStepRun stepRun)
        {
            base.OnTestStepRunCompleted(stepRun);
            PyThread.Invoke(() => pyObj.InvokeMethod(nameof(OnTestStepRunCompleted),stepRun.ToPython()));
        }

        public override void OnTestStepRunStart(TestStepRun stepRun)
        {
            base.OnTestStepRunStart(stepRun);
            PyThread.Invoke(() => pyObj.InvokeMethod(nameof(OnTestStepRunStart), stepRun.ToPython()));
        }
    }
}
