//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
using System;
using System.Linq;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using OpenTap;

using Keysight.Plugins.Python;

namespace Keysight.OpenTap.Plugins.Python
{
    /// <summary>
    /// Wrapper class to have a python step object inside it.
    /// </summary>
    [WrappedType(typeof(PythonStep))]
    [Obfuscation(Exclude = true)]
    public abstract class PythonStepWrapper : PythonWrapper, ITestStep
    {
        PythonStep step => PythonObject.As<PythonStep>();
            
        TestStepList childTestSteps;
        [Browsable(false)]
        public TestStepList ChildTestSteps {
            get => childTestSteps;
            set
            {
                childTestSteps = value;
                childTestSteps.Parent = this;
                OnPropertyChanged("ChildTestSteps");
            }
        }

        /// <summary>
        /// Gets or sets boolean indicating whether this step is enabled in the TestPlan
        /// </summary>
        [ColumnDisplayName("", Order: -101)]
        [Display("Enabled", "Enabling/Disabling the test step decides if" +
                            " it should be used when the test plan is executed. " +
                            "This value should not be changed during test plan run.", Group: "Common", Order: 20000, Collapsed: true)]
        [Unsweepable]
        public bool Enabled
        {
            get => step.Enabled;
            set
            {
                if (step.Enabled == value)
                    return;
                step.Enabled = value;
                OnPropertyChanged(nameof(Enabled));
            }
        }

        [XmlAttribute("Id")]
        [Browsable(false)]
        public Guid Id
        {
            get => step.Id;
            set => step.Id = value;
        }
        [Browsable(false)]
        [XmlIgnore]
        public bool IsReadOnly
        {
            get => step.IsReadOnly;
            set => step.IsReadOnly = value;
        }

        /// <summary>
        /// Gets or sets the name of the TestStep instance. Not allowed to be null.
        /// In many cases the name is unique within a test plan, but this should not be assumed, use <see cref="Id"/>for an unique identifier.
        /// May not be null.
        /// </summary>
        [ColumnDisplayName(nameof(Name), Order: -100)]
        [Display("Step Name", "The name of the test step, this value can be used to identifiy a test step. " +
                              "Test step names are not guaranteed to be unique. " +
                              "Name can include names of a setting of the step, this property will dynamically be " +
                              "replaced with it's current value in some views.", Group: "Common", Order: 20001, Collapsed: true)]
        [Unsweepable]
        [MetaData(Frozen = true)]
        public string Name
        {
            get => step.Name;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value), "TestStep.Name cannot be null.");
                if (value == step.Name)
                    return;
                step.Name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        [XmlIgnore]
        public ITestStepParent Parent { get; set; }
        
        [Browsable(false)]
        [XmlIgnore]
        public TestPlanRun PlanRun
        {
            get => step.PlanRun;
            set => step.PlanRun = value;
        }
        [XmlIgnore]
        [Browsable(false)]
        public TestStepRun StepRun
        {
            get => step.StepRun;
            set => step.StepRun = value;
        }

        [Browsable(false)]
        [ColumnDisplayName(Order: -99, IsReadOnly: true)]
        [XmlIgnore]
        [Output]
        public Verdict Verdict
        {
            get => step.Verdict;
            set => step.Verdict = value;
        }

        public void PostPlanRun()
        {
            step.PostPlanRun();
        }

        public void PrePlanRun()
        {
            step.PrePlanRun();
        }

        public void Run()
        {
            step.Results = new ResultSource(StepRun, PlanRun);
            step.Run();
        }

        protected override void load(string name, string moduleName)
        {
            base.load(name, moduleName);
            lock (loaderLookup)
            {
                if (!loaderLookup.ContainsKey(GetType()))
                {
                    loaderLookup[GetType()] = loadDefaultResources(GetType());
                }
                foreach (var loader in loaderLookup[GetType()])
                    loader(this);
            }
            Name = TestStep.GenerateDefaultNames(GetType()).LastOrDefault() ?? Name;
        }

        static List<Action<object>> loadDefaultResources(Type t)
        {
            List<Action<object>> loaders = new List<Action<object>>();
            var props = t.GetProperties();
            foreach (var prop in props)
            {
                object value = null;
                Type propType = prop.PropertyType;
                {
                    if (ComponentSettingsList.GetContainer(prop.PropertyType) != null)
                    {
                        loaders.Add(x =>
                        {
                            IList list = ComponentSettingsList.GetContainer(prop.PropertyType);
                            if (list != null)
                            {
                                value = list.Cast<object>().Where(o => o != null && propType.IsAssignableFrom(o.GetType())).FirstOrDefault();
                                try
                                {
                                    prop.SetValue(x, value, null);
                                }
                                catch (Exception)
                                {

                                }
                            }
                        });
                    }
                }
            }
            return loaders;
        }
        static Dictionary<Type, List<Action<object>>> loaderLookup = new Dictionary<Type, List<Action<object>>>();

        public void RunChildSteps()
        {
            this.RunChildSteps(PlanRun, StepRun);
        }

        public void RunChildStep(ITestStep step)
        {
            step.Parent = this as ITestStepParent;
            TestStepExtensions.RunChildStep(this, step, PlanRun, StepRun);
        }

        public PythonStepWrapper()
        {
            ChildTestSteps = new TestStepList { Parent = this };
        }
    }
}
