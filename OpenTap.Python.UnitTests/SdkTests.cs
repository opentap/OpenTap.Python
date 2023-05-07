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

    [Display("test4", Group: "python")]
    public class PythonTest4 : ICliAction
    {
        private static TraceSource log = Log.CreateSource("test3");
        public int Execute(CancellationToken cancellationToken)
        {
            var pyStep = TypeData.GetTypeData("PythonExamples.BasicFunctionality.BasicFunctionality");
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
