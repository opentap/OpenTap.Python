using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTap;

namespace Keysight.OpenTap.Plugins.Python.UnitTests
{
    public class TestMain
    {
        public static void Main()
        {
            var tests = new SdkTests();
            tests.TestPyThread();
            tests.TestBuildExampleWrapper();
        }
    }

    
    public class SdkTests
    {
        public void TestBuildExampleWrapper()
        {
            string str = @"PluginExample";
            string modulename = "PluginExample";
            var outdll = Path.Combine(str, modulename + ".dll");
            File.Delete(outdll);
            new WrapperBuilder().Build(new List<string> { modulename }, outdll, false, modulename);
            Debug.Assert(File.Exists(outdll));
            var asm = Assembly.LoadFile(Path.GetFullPath(outdll));

            string[] expectedExportedTypes = new[] { "ChargeStep", "DischargeStep", "BasicFunctionality", "ErrorExample", "BasicInstrument", "BasicDut", "PowerAnalyzer", "BasicSettings" };
            var exportedTypes = asm.ExportedTypes;
            foreach (var t in expectedExportedTypes)
            {
                Debug.Assert(exportedTypes.Any(x => x.Name == t));
            }
            var chargeStepType = asm.GetType("PluginExample.ChargeStep");
            var displayAttribute = chargeStepType.GetCustomAttribute<DisplayAttribute>();
            //Assert.IsNotNull(displayAttribute);
            //Assert.AreEqual("Charge", displayAttribute.Name);
            var comp = ComponentSettings.GetCurrent(asm.GetType("PluginExample.BasicSettings"));
            //Assert.AreEqual<int>(600, comp.NumberOfPoints);
        }


        public void TestPyThread()
        {
            int a = 0;
            PyThread.Invoke(() => a = 4);
            //Assert.IsTrue(a == 4);
        }
    }
}
