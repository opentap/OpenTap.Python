using System;
using System.IO;
using System.Linq;
using System.Threading;
using OpenTap.Cli;
using Python.Runtime;
namespace OpenTap.Python.UnitTests
{
    [Display("test", Group:"python")]
    public class PythonTest : ICliAction
    {
        public int Execute(CancellationToken cancellationToken)
        {
            PythonInitializer.LoadPython();
            if (!PythonEngine.IsInitialized)
            {
                foreach (var env in Environment.GetEnvironmentVariables().Keys)
                {
                    log.Info("  {0} = {1}", env, Environment.GetEnvironmentVariable(env.ToString()));
                }
            }
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
            
            TestValidationErrors();
            
            new AttributeTests().TestOutputAvailability();

            return 0;
        }

        void TestValidationErrors()
        {
            InstrumentSettings.Current.Clear();
            DutSettings.Current.Clear();
            var td =TypeData.GetTypeData("PythonExamples.BasicFunctionality.BasicFunctionality");
            var step = (TestStep)td.CreateInstance();
            var err = step.Error;
            if (string.IsNullOrWhiteSpace(err) == false)
            {
                throw new Exception("Error should be null");
            }
            var frequencyMember = TypeData.GetTypeData(step).GetMember("Frequency");
            frequencyMember.SetValue(step, -1000.0);
            err = step.Error;
            if (string.IsNullOrWhiteSpace(err))
            {
                throw new Exception("Error should be set.");
            }
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
                Assert.Assignable(type, ins2);
            }
            
            var pyStep3 = TypeData.GetTypeData("TestModule.BasicStepTest.StepWithNoNamespace");
            var teststep2 = (TestStep)pyStep3.CreateInstance();
            Assert.IsTrue(teststep2.ChildTestSteps != null);
            var td2 = TypeData.GetTypeData(teststep2);
            var freq = td2.GetMember("Frequency");
            freq.SetValue(teststep2, 10.0);
            var val = (double)freq.GetValue(teststep2);
            Assert.AreEqual(val, 10.0);
            
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

                Assert.AreEqual((double)cv, 1.0);
                compsetType.GetMember("A").SetValue(set2, 10.0);
                set2.Save();
                set2.Invalidate();
                set2 = ComponentSettings.GetCurrent(compsetType);
                cv = compsetType.GetMember("A").GetValue(set2);
                Assert.AreEqual((double)cv, 10.0);
            }



        }

        readonly string[] pluginTypes = {
            "PythonExamples.CsvResultListener.CsvPythonResultListener",
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
}
