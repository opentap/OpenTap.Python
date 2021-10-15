//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
using OpenTap;
using Keysight.Plugins.Python;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Python.Runtime;

namespace Keysight.OpenTap.Plugins.Python
{
    [Browsable(false)]
    [Obfuscation(Exclude = true)]
    public class PythonComponentSettings : ComponentSettings<PythonComponentSettings>, IPythonProxy
    {        
        [Browsable(false)]
        public IPythonWrapper Wrapper { get; set; }

        static public void EnsureLoaded(PyObject value)
        {
            PyThread.Invoke(() =>
            {
                var name = value.GetAttr("__name__").ToString();
                var x = TypeData.GetDerivedTypes<ComponentSettings>()
                    .Where(xy => xy.DescendsTo(typeof(IPythonWrapper))
                                  && xy.CanCreateInstance
                    ).ToArray();
                foreach (var tp in x)
                    if(tp.Name.Contains("." + name))
                        ComponentSettings.GetCurrent(tp);
            });
        }
    }
}
