// Responsible: TEAM (bdirenzo)
// Copyright:   Copyright 2016 Keysight Technologies
//              You have a royalty-free right to use, modify, reproduce and distribute
//              the sample application files (and/or any modified version) in any way
//              you find useful, provided that you agree that Keysight Technologies has no
//              warranty, obligations or liability for any sample application files
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using OpenTap;  // Use Platform infrastructure/core components (log,TestStep definition, etc)

namespace TapPlugin.PythonNetTest
{
    [DisplayName("PythonNetTest\\Step")]
    [Description("Insert description here")]
    public class Step : TestStep
    {
        #region Settings
        static TraceSource cpyLog = Keysight.Tap.Log.CreateSource("CPyLog");
        public SamplePythonInst Inst { get; set; }
        // ToDo: Add property here for each parameter the end user should be able to change.
        #endregion

        public Step()
        {
            
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            cpyLog.Info(Inst.GetName());
            Inst.Initialize();
            TestPlan.Sleep();
            //Inst.Wait();
            Inst.Terminate();
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }


    }
}
