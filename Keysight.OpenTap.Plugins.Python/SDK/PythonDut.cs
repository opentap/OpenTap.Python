//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
using System.ComponentModel;
using System.Reflection;
using OpenTap;
using Keysight.Plugins.Python;
using Python.Runtime;

namespace Keysight.OpenTap.Plugins.Python
{
    /// <summary>
    /// The Python intermediate step that allows communication between TAP steps and Python.
    /// </summary>
    [Browsable(false)]
    [Obfuscation(Exclude = true)]
    public class PythonDut : Dut, IPythonProxy
    {
        
        public IPythonWrapper Wrapper { get; set; }

        PyObject pyObj
        {
            get { return ((IPythonWrapper)Wrapper).PythonObject; }
        }
        public override void Open()
        {
            base.Open();
            PyThread.Invoke(() => pyObj.InvokeMethod(nameof(Open)));
        }

        public override void Close()
        {
            base.Close();
            PyThread.Invoke(() => pyObj.InvokeMethod(nameof(Close)));
        }
    }
}