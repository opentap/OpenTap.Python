// Example code. Do with it what you like.
// no rights reserved.
using OpenTap;
using System.Collections.Generic;

namespace OpenTap.Python.ProjectTemplate.Api
{
    // this API can be implemented in python and used from C#.
    public interface PythonInstrumentApi : IInstrument
    {
        double DoMeasurement();
    }

    // This class is an example of somebody using the ITestApi1 interface.
    [Display("Step using instrument API", Group: "Example")]
    public class StepUsingInstrumentApi : TestStep
    {
        // this instrument is implemented in Python
        public PythonInstrumentApi Instrument { get; set; }

        public override void Run()
        {
            var measurement = Instrument.DoMeasurement();
            Results.Publish("Measurement", new List<string>{"X"}, measurement);
            Log.Info("Measured: {0}", measurement);
        }
    }
}