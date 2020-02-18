//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
using Keysight.OpenTap.Plugins.Python;
using Python.Runtime;

namespace Keysight.Plugins.Python
{
    public abstract class GenericPythonWrapper : PythonWrapper
    {
    }

    public class GenericPythonObject : IPythonProxy
    {
        public IPythonWrapper Wrapper { get; set; }
    }
}
