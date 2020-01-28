// Author: MyName
// Copyright:   Copyright 2016 Keysight Technologies
//              You have a royalty-free right to use, modify, reproduce and distribute
//              the sample application files (and/or any modified version) in any way
//              you find useful, provided that you agree that Keysight Technologies has no
//              warranty, obligations or liability for any sample application files.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using OpenTap;
using System.Reflection;
using Python.Runtime;
using Keysight.OpenTap.Plugins.Python;

namespace Keysight.Tap.Plugins.PythonExample
{
    [DisplayName("PythonExample\\SignalGeneratorStep")]
    [Description("Insert a description here")]
    public class SignalGeneratorStep : TestStep
    {
        #region Settings
        static TraceSource cpyLog = Keysight.Tap.Log.CreateSource("CPyLog");
        static private IntPtr _allowThreads = IntPtr.Zero;
        #endregion
        static SignalGeneratorStep()
        {
            // ToDo: Set default values for properties / settings.
            if (!PythonEngine.IsInitialized)
            {
                PythonEngine.Initialize();

                AssemblyLoader a = Al.assemblyLoader;

                _allowThreads = PythonEngine.BeginAllowThreads();
            }
        }

        /*~SignalGeneratorStep()
        {
            if ((_allowThreads != IntPtr.Zero) && (PythonEngine.IsInitialized))
            {
                PythonEngine.EndAllowThreads(_allowThreads);
                PythonEngine.Shutdown();
            }
        }*/

        public override void PrePlanRun()
        {
            base.PrePlanRun();  // Do not remove
            // ToDo: Optionally add any setup code this step needs to run before the testplan starts
        }

        public override void Run()
        {
            // ToDo: Add test case code here
            RunChildSteps(); //If step has child steps.

            object result = null;
            using (Py.GIL())
            {
                //                import SignalGeneratorLib

                //x = SignalGeneratorLib.SignalGenerator

                //print x.GenerateSine(x, 0, 1)

                //List<dynamic> testme = new List<dynamic>();
                //testme.AddRange(abc);
                //Type unknown = ((PyObject)sin).GetType();
                //Results.Publish("Hello", new List<string>() { "test" }, testme.ToArray());
                //var returnType = sin.GetType();

                dynamic sg = Py.Import("sig.sgl");
                dynamic signalGenerator = sg.SignalGenerator();
                dynamic sin = signalGenerator.GenerateSine(0, 1); //signalGenerator.TestMe3(); //.TestMe(); //signalGenerator.GenerateSine(0, 1); //signalGenerator.TestMe3(); //
                var x = sin.ToString();
                
                result = GetObjectFromDynamicType(sin);
            }

            try
            {
                var myArray = ((Array)result).Cast<IConvertible>().ToArray();
                for (int i = 0; i < myArray.Length; ++i)
                    Log.Info(myArray[i].ToString());
                if (myArray != null)
                    Results.Publish("Result Data", new List<string> { "Value" }, myArray);
            }
            catch
            {
                //var myarr = new object[] { result };
                //Log.Info(result.ToString());
                //Results.Publish("Result Data", new List<string> { "Value" }, myarr);
            }

            UpgradeVerdict(Verdict.Pass);
        }

        public enum ValidTypes : int
        {
            kDoubleArray = 1,
            kString = 2,
            kStringArray = 3,
            kDouble = 4
        }

        public object GetObjectFromDynamicType(dynamic abc)
        {

            object val = null;
            //Type currentType = typeof(double[]);
            
            for (ValidTypes currentType = ValidTypes.kDoubleArray; currentType <= ValidTypes.kDouble; ++currentType)
            {
                try
                {

                    if (currentType == ValidTypes.kDoubleArray)
                    {
                        val = (Double[])abc;
                    }
                    else if (currentType == ValidTypes.kString)
                        val = (String)abc;
                    else if (currentType == ValidTypes.kStringArray)
                    {
                        val = (String[])abc;
                    }
                    else if (currentType == ValidTypes.kDouble)
                        val = (Double)abc;
                    
                    if (val == null)
                        continue;
                    else
                        break;
                }
                catch
                {
                   continue;
                }
            }

            return val;
        }

        public override void PostPlanRun()
        {
            // ToDo: Optionally add any cleanup code this step needs to run after the entire testplan has finished
            base.PostPlanRun(); // Do not remove
        }

        // Register a callback function to load embedded assemblies.
        // (Python.Runtime.dll is included as a resource)
        //private sealed class AssemblyLoader
        //{
        //    Dictionary<string, Assembly> loadedAssemblies;

        //    public AssemblyLoader()
        //    {
        //        loadedAssemblies = new Dictionary<string, Assembly>();

        //        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        //        {
        //            string shortName = args.Name.Split(',')[0];
        //            String resourceName = shortName + ".dll";

        //            if (loadedAssemblies.ContainsKey(resourceName))
        //            {
        //                return loadedAssemblies[resourceName];
        //            }

        //            // looks for the assembly from the resources and load it
        //            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
        //            {
        //                if (stream != null)
        //                {
        //                    Byte[] assemblyData = new Byte[stream.Length];
        //                    stream.Read(assemblyData, 0, assemblyData.Length);
        //                    Assembly assembly = Assembly.Load(assemblyData);
        //                    loadedAssemblies[resourceName] = assembly;
        //                    return assembly;
        //                }
        //            }

        //            return null;
        //        };
        //    }
        //};

        //private static AssemblyLoader assemblyLoader = new AssemblyLoader();
    }
}
