using Python.Runtime;
using System;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using System.Reflection;
using OpenTap;
using Keysight.Plugins.Python;
using System.Collections.Generic;

namespace Keysight.OpenTap.Plugins.Python
{
    /// <summary>
    /// The Python intermediate step that allows communication between TAP steps and Python.
    /// </summary>
    [Browsable(false)]
    [Obfuscation(Exclude = true)]
    [AllowAnyChild]
    public class PythonStep : ITestStep, IPythonProxy
    {
        /// <summary>
        /// Static constructor, ensures that Python is initialized on DLL load if not already initialized.
        /// </summary>
        static PythonStep()
        {
            WrapperBuilder.LoadPython();
        }
        
        public ResultSource Results { get;set; }

        public TraceSource Log { get; set; } = global::OpenTap.Log.CreateSource("TestStep");

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Allows access to Publish from Python.
        /// </summary>
        /// <param name="name">Name of results to publish in TAP</param>
        /// <param name="title">Names of axis' to use</param>
        /// <param name="values">Values to store with each axis</param>
        public void PublishResult(string name, string[] title, object[] values)
        {
            this.Results.Publish(name, title.ToList(), values.Cast<IConvertible>().ToArray());
        }
        
        /// <summary>
        /// Runs the TestStep by calling the run method within the underlying Python code.
        /// </summary>
        public void Run()
        {

            PyThread.Invoke(() =>
            {
                try
                {
                    pyobject.InvokeMethod(nameof(Run));
                }
                catch (PythonException e)
                {
                    // Convert PythonException to AbortExceptions if possible.
                    // Loosing some of the stacktrace, but its not really important.
                    Exception ex = null;

                    try
                    {
                        var inner = new PyObject(e.PyValue);

                        ex = inner.AsManagedObject(typeof(Exception)) as OperationCanceledException;
                    }
                    catch
                    {

                    }
                    
                    if (ex != null)
                         throw ex;
                    throw e;
                }
            });
        }
        
        public void RunChildSteps()
        {
            step.RunChildSteps();
        }

        public void RunChildStep(ITestStep childstep)
        {
            step.RunChildStep(childstep);
        }

        public IEnumerable<ITestStep> EnabledChildSteps => ChildTestSteps.Where(x => x.Enabled);

        public void UpgradeVerdict(Verdict v)
        {
            if (Verdict < v)
                Verdict = v;
        }

        /// <summary>
        /// Called once before the test plan starts.
        /// </summary>
        public void PrePlanRun()
        {
            PyThread.Invoke(() => pyobject.InvokeMethod(nameof(PrePlanRun)));
        }

        /// <summary>
        /// Called once after the test plan ends.
        /// </summary>
        public void PostPlanRun()
        {
            PyThread.Invoke(() => pyobject.InvokeMethod(nameof(PostPlanRun)));
        }

        public void OnPropertyChanged(string propertyName)
        {
            if (Wrapper != null)
                UserInput.NotifyChanged(Wrapper, propertyName);
        }

        [XmlIgnore]
        public PyObject pyobject => Wrapper.PythonObject;
        PythonStepWrapper step => Wrapper as PythonStepWrapper;

        public IPythonWrapper Wrapper { get; set; }
        public Verdict Verdict { get; set; }
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public TestPlanRun PlanRun { get; set; }
        public TestStepRun StepRun { get; set; }
        public bool IsReadOnly { get; set; }

        public string TypeName => GetType().FullName;

        public Guid Id { get; set; }
        public ITestStepParent Parent
        {
            get => step?.Parent;
            set => step.Parent = value;
        }

        public TestStepList ChildTestSteps => step.ChildTestSteps;

        public ValidationRuleCollection Rules { get; } = new ValidationRuleCollection();

        public string Error => null;

        public string this[string columnName] => null;
    }
}
