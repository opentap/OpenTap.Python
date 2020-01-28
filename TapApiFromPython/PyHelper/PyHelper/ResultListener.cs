using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTap.Diagnostic;
using System.Windows;
using OpenTapRL = OpenTap;

namespace Keysight.OpenTap.Plugins.Python
{
    public partial class PyHelper
    {
        public class ResultListener : OpenTapRL.ResultListener //Keysight.Tap.IResultListener
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public delegate void TestStepRunCallback(TestStepRun stepRun);

            public delegate void TestPlanRunStartCallback(TestPlanRun planRun);

            public delegate void TestPlanRunCompletedCallback(TestPlanRun planRun, Stream logStream);

            public delegate void ResultPublishedCallback(Guid stepRunID, ResultTable result);

            public event Action WOpen;
            public event ResultPublishedCallback WOnResultPublished;
            public event TestPlanRunCompletedCallback WOnTestPlanRunCompleted;
            public event TestPlanRunStartCallback WOnTestPlanRunStart;
            public event TestStepRunCallback WOnTestStepRunCompleted;
            public event TestStepRunCallback WOnTestStepRunStart;
            public event Action WClose;

            public ResultListener()
            {

            }

            public string Name { get; set; }

            public bool IsConnected { get; set; }

            public override void Open()
            {
                base.Open();
                WOpen();
            }

            public override void OnResultPublished(Guid stepRunID, ResultTable result)
            {
                base.OnResultPublished(stepRunID, result);
                WOnResultPublished(stepRunID, result);
            }

            public override void OnTestPlanRunCompleted(TestPlanRun planRun, Stream logStream)
            {
                base.OnTestPlanRunCompleted(planRun, logStream);
                WOnTestPlanRunCompleted(planRun, logStream);
            }

            public override void OnTestPlanRunStart(TestPlanRun planRun)
            {
                base.OnTestPlanRunStart(planRun);
                WOnTestPlanRunStart(planRun);
            }

            public override void OnTestStepRunCompleted(TestStepRun stepRun)
            {
                base.OnTestStepRunCompleted(stepRun);
                WOnTestStepRunCompleted(stepRun);
            }

            public override void OnTestStepRunStart(TestStepRun stepRun)
            {
                base.OnTestStepRunStart(stepRun);
                WOnTestStepRunStart(stepRun);
            }

            public override void Close()
            {
                base.Close();
                WClose();
            }
        }
    }
}
