//  Copyright 2012-2022 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using OpenTap.Cli;
using Python.Runtime;

namespace OpenTap.Python.UnitTests
{
    class ValidationRule2 : ValidationRule
    {
        public ValidationRule2(IsValidDelegateDefinition isValid, string errorMessage, string propertyName) : base(
            isValid, errorMessage, propertyName)
        {
            
        }
    }
    class TestCls :TestStep
    {
        [Browsable(true)]
        public void A()
        {
            
        }
        
        public void B()
        {
            
        }

        public TestCls()
        {  
            
        }

        public override void Run()
        {
            
        }
    }

    public class TestResultListener : ResultListener
    {
        public override void OnTestPlanRunStart(TestPlanRun planRun)
        {
            base.OnTestPlanRunStart(planRun);
        }

        public override void OnTestPlanRunCompleted(TestPlanRun planRun, Stream logStream)
        {
            Log.Debug("2: OnTestPlanRunCompleted");
            base.OnTestPlanRunCompleted(planRun, logStream);
        }

        public override void Open()
        {
            base.Open();
        }
    }
    
    [Display("test", Group:"python")]
    public class PythonTest : ICliAction
    {
        public int Execute(CancellationToken cancellationToken)
        {
            PythonInitializer.LoadPython();
            Assert.IsTrue(PythonEngine.IsInitialized);
            PythonEngine.DebugGIL = true;

            var b = new TestCls();
            var bt = TypeData.GetTypeData(b);
            var bt_m = bt.GetMembers();
            
            var stepInterface = TypeData.FromType(typeof(ITestStep));
            var allSteps = stepInterface.DerivedTypes;
            var pyStep = TypeData.GetTypeData("TestModule.BasicStepTest");
            var pyDutType = TypeData.GetTypeData("TestModule.DutTest");
            Assert.IsTrue(pyDutType.DescendsTo(typeof(IDut)));
            var dut = (IDut) pyDutType.CreateInstance();
            DutSettings.Current.Add(dut);
            var isStep = pyStep.DescendsTo(typeof(ITestStep));
            Assert.IsTrue(isStep);
            var step = (ITestStep)pyStep.CreateInstance();
            //step.PrePlanRun();
            //step.PostPlanRun();
            pyStep.GetMember("Dut").SetValue(step, dut);
            var plan = new TestPlan();
            plan.ChildTestSteps.Add(step);
            step.GetType().GetMethod("MethodTest").Invoke(step, Array.Empty<object>());
            
            step.GetType().GetProperty("Frequency").SetValue(step, 100.0);
            step.GetType().GetProperty("Frequency").GetValue(step);
            Log.Flush();
            SessionLogs.Flush();
            if (true)
            {
                try
                {
                    var run = plan.Execute();
                    var postPlanRunExecuted = (bool) pyStep.GetMember("PostPlanRunExecuted").GetValue(step);
                    var prePlanRunExecuted = (bool) pyStep.GetMember("PrePlanRunExecuted").GetValue(step);
                    Assert.IsTrue(postPlanRunExecuted);
                    Assert.IsTrue(prePlanRunExecuted);
                    Assert.IsTrue(run.Verdict == Verdict.NotSet);
                }
                catch (Exception)
                {

                }
            }

            var a = AnnotationCollection.Annotate(step);
            //var members = step.GetType().GetMembers();
            var freq = step.GetType().GetProperty("Frequency").GetValue(step);
            var callable = a.GetMember("MethodTest");
            var method = callable.Get<IMethodAnnotation>();
            method.Invoke();

            Test2();
            TestEmbeddedProperties();
            return 0;
        }

        void TestEmbeddedProperties()
        {
            var td =TypeData.GetTypeData("Test.TestStep3");
            var step = (TestStep)td.CreateInstance();
            
            var a = TypeData.GetTypeData(step).GetMember("B.A");
            var c = TypeData.GetTypeData(step).GetMember("C");
            var b = td.GetMember("B");
            var test = a.GetValue(step);
            a.SetValue(step, 10.0);
            c.SetValue(step, 10.0);
            Assert.IsTrue(object.Equals(a.GetValue(step), c.GetValue(step)));

            var plan = new TestPlan();
            plan.ChildTestSteps.Add(step);
            var run = plan.Execute();
            Assert.AreEqual(Verdict.NotSet, run.Verdict);
        }

        private static TraceSource log = Log.CreateSource("test"); 
        void Test2()
        {
            foreach (var pluginName in pluginTypes)
            {
                log.Info("plugin: {0}", pluginName);
                var lckManagerType2 = TypeData.GetTypeData(pluginName);
                var ins2 = lckManagerType2.CreateInstance();
                Assert.IsNotNull(ins2);
            }
            foreach (var (name, type) in pluginTypes2)
            {
                log.Info("plugin: {0} is {1}.", name, type);
                var lckManagerType2 = TypeData.GetTypeData(name);
                var ins2 = lckManagerType2.CreateInstance();
                Assert.IsNotNull(ins2);
                Assert.IsTrue(type.IsAssignableFrom(ins2.GetType()));
            }
            
            var pyStep3 = TypeData.GetTypeData("TestModule.BasicStepTest.StepWithNoNamespace");
            var teststep2 = (TestStep)pyStep3.CreateInstance();
            Assert.IsTrue(teststep2.ChildTestSteps != null);
            var td2 = TypeData.GetTypeData(teststep2);
            var freq = td2.GetMember("Frequency");
            freq.SetValue(teststep2, 10.0);
            var val = (double)freq.GetValue(teststep2);
            Assert.IsTrue(val == 10.0);
            
            var instr1Type = TypeData.GetTypeData("Test.TestScpiInstrument");
            var instr1 = instr1Type.CreateInstance();
            Assert.IsTrue(instr1 is ScpiInstrument);
            
            var instr2Type = TypeData.GetTypeData("TestModule.BasicStepTest.TestScpiInstrument2");
            Assert.IsTrue(instr2Type != null);
            var instr2 = instr2Type.CreateInstance();
            Assert.IsTrue(instr2 is ScpiInstrument);
            
            {
                var cs= TypeData.GetDerivedTypes<ComponentSettings>().ToArray();
                var instr= TypeData.GetDerivedTypes<ITestStep>().ToArray();
            }
            
            {
                try
                {
                    File.Delete("Settings/Settings Test.xml");
                }
                catch
                {
                    
                }

                var compsetType = TypeData.GetTypeData("TestModule.BasicStepTest.SettingsTest");
                var set2 = ComponentSettings.GetCurrent(compsetType);
                var set4 = compsetType.CreateInstance();
                var cv = compsetType.GetMember("A").GetValue(set2);
                var settings = TypeData.GetDerivedTypes<ComponentSettings>();
                compsetType.GetMember("A").SetValue(set2, 10.0);
                set2.Invalidate();
                set2 = ComponentSettings.GetCurrent(compsetType);
                cv = compsetType.GetMember("A").GetValue(set2);

                Assert.AreEqual(cv, 1.0);
                compsetType.GetMember("A").SetValue(set2, 10.0);
                set2.Save();
                set2.Invalidate();
                set2 = ComponentSettings.GetCurrent(compsetType);
                cv = compsetType.GetMember("A").GetValue(set2);
                Assert.AreEqual(cv, 10.0);
            }



        }

        readonly string[] pluginTypes = {
            "Examples.CsvResultListener.CsvPythonResultListener",
            "TestModule.StepWithNoCtor",
            "TestModule.BasicStepTest.TestStep2",
            "TestModule.BasicStepTest.LockManager", 
            "Test.LockManager2", 
            "TestModule.BasicStepTest.TestScpiInstrument2",
            "Test.TestScpiInstrument"
        };
        readonly (string, Type)[] pluginTypes2 = {
            ("Test.LockManager2", typeof(ILockManager))
        };
    }

    [Display("test4", Group: "python")]
    public class PythonTest4 : ICliAction
    {
        private static TraceSource log = Log.CreateSource("test3");
        public int Execute(CancellationToken cancellationToken)
        {
            var pyStep = TypeData.GetTypeData("Examples.BasicFunctionality.BasicFunctionality");
            var x = pyStep.CreateInstance();
            if (pyStep.GetMember("Frequency") == null)
                throw new Exception("Expected Frequency to be defined.");
            if (pyStep.GetMember("FrequencyIsDefault") == null)
                throw new Exception("Expected FrequencyIsDefault to be defined.");
            var freqDefault = pyStep.GetMember("FrequencyIsDefault");
            if (freqDefault.Writable || freqDefault.Readable == false) throw new Exception("Expected FrequencyIsDefault to be read-only.");
            if (freqDefault.GetAttribute<BrowsableAttribute>() == null)
                throw new Exception("Expected FrequencyIsDefault to be set.");
            var m = x.GetType().GetMethod("resetFrequency");
            Assert.IsTrue(m.GetCustomAttributes(false).Count() == 3);
            log.Info("OK");
            var pl = x.ToPython();
            using(Py.GIL())
                pl.SetAttr("Frequency".ToPython(), 1.0.ToPython());
            return 0;   
        }
    }

    [Display("list-installations", Group: "python")]
    public class ListPythonInstallations  : ICliAction
    {
        static readonly TraceSource log = Log.CreateSource("python");
        public int Execute(CancellationToken cancellationToken)
        {
            foreach (var ins in new PythonDiscoverer().GetAvailablePythonInstallations())
            {
               log.Info("{0} {1}", ins.library ?? "(null)", ins.pyPath ?? "(null)"); 
            }

            return 0;
        }
    }
    static class AnnotationExtensions
    {
        public static AnnotationCollection GetMember(this AnnotationCollection a, string name)
        {
            return a.Get<IMembersAnnotation>().Members.First(x => x.Get<IMemberAnnotation>().Member.Name == name);
        }
    }

    class Assert
    {
        public static void IsTrue(bool expr)
        {
            if (!expr)
                throw new Exception("Assertion failed.");
        }

        public static void IsNotNull(object ins)
        {
            if(ins == null) 
                throw new Exception("Assertion failed.");
        }

        public static void AreEqual(object cv, object d)
        {
            if (!object.Equals(cv, d))
                throw new Exception("Assertion failed!");
        }
    }
}
