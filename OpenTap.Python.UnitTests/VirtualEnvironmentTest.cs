using System;
using System.IO;
using System.Threading;
using OpenTap.Cli;
using Python.Runtime;

namespace OpenTap.Python.UnitTests
{
    [Display("test-venv", "Verify virtual environment imports", "python")]
    public class VirtualEnvironmentTest : ICliAction
    {
        public int Execute(CancellationToken cancellationToken)
        {
            if (!PythonInitializer.LoadPython())
                throw new Exception("Python initialization failed.");

            using (Py.GIL())
            {
                var marker = Py.Import("opentap_venv_marker");
                var expected = Path.GetFullPath(Environment.GetEnvironmentVariable("OPENTAP_TEST_VENV"));
                var actual = Path.GetFullPath(marker.GetAttr("__file__").As<string>());
                if (!actual.StartsWith(expected + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Marker was not loaded from the virtual environment: " + actual);
                if (marker.GetAttr("VALUE").As<int>() != 42)
                    throw new Exception("Unexpected virtual environment marker value.");
                Py.Import("opentap_pth_marker");
                if (Environment.GetEnvironmentVariable("OPENTAP_TEST_EXTERNAL") == "1")
                    Py.Import("opentap_external_marker");
            }
            return 0;
        }
    }
}
