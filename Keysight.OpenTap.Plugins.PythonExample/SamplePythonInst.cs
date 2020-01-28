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
using OpenTap;
using Python.Runtime;
using System.Reflection;
using Keysight.OpenTap.Plugins.PythonExample;

namespace TapPlugin.PythonNetTest
{
    [DisplayName("SamplePythonInst")]
    [Description("Insert a description here")]
    [ShortName("MyINST")]
    public class SamplePythonInst : Instrument
    {
        #region Settings
        public String Module { get; set; }
        [Browsable(false)]
        public dynamic WlanInstrument { get; set;  }

        public dynamic Instrument { get; set; }

        private IntPtr _allowThreads;
        #endregion
        //public SamplePythonInst()
        //{
        //    // ToDo: Set default values for properties / settings.
        //    Module = "graphyte.inst.sample_inst.sample_inst";
        //    PythonEngine.Initialize();

        //    //AssemblyLoader a = assemblyLoader;
        //    AssemblyLoader a = Al.assemblyLoader;

        //    _allowThreads = PythonEngine.BeginAllowThreads();
        //}

        //~SamplePythonInst()
        //{
        //    PythonEngine.EndAllowThreads(_allowThreads);
        //    PythonEngine.Shutdown();
        //}

        // Register a callback function to load embedded assmeblies.
        // (Python.Runtime.dll is included as a resource)
        

    /// <summary>
    /// Open procedure for the instrument.
    /// </summary>
    public override void Open()
        {
            base.Open();
            using (Py.GIL())
            {
                dynamic inst = Py.Import(Module);
                Instrument = inst.Inst();
                WlanInstrument = Instrument.WlanController(inst.Inst());
            }

        }

        /// <summary>
        /// Close procedure for the instrument.
        /// </summary>
        public override void Close()
        {
            base.Close();
        }

        public string GetName()
        {
            using (Py.GIL())
            {
                return Instrument.name;
            }
        }

        public void Initialize()
        {
            using (Py.GIL())
            {
                WlanInstrument.Initialize();
            }
        }

        public void Terminate()
        {
            using (Py.GIL())
            {
                WlanInstrument.Terminate();
            }
        }

        public void Wait()
        {
            using (Py.GIL())
            {
                WlanInstrument._WaitPeriod();
            }
        }

        public void TxMeasure()
        {
            using (Py.GIL())
            {
                WlanInstrument._TxMeasure();
            }
        }

        public Double[] TxGetResults()
        {
            using (Py.GIL())
            {
                return WlanInstrument._TxGetResults();
            }
        }

        public void RxConfig()
        {
            using (Py.GIL())
            {
                WlanInstrument._RxConfig();
            }
        }

        public void RxGenerate()
        {
            using (Py.GIL())
            {
                WlanInstrument._RxGenerate();
            }
        }

        public void RxStop()
        {
            using (Py.GIL())
            {
                WlanInstrument._RxStop();
            }
        }
    }
}
