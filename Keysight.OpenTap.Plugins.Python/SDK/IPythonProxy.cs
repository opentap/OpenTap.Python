//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
using Keysight.Plugins.Python;
using System.Reflection;
namespace Keysight.OpenTap.Plugins.Python

{
    [Obfuscation(Exclude = true)]
    /// <summary> A class that is used in python, but wrapped by an object in TAP engine. </summary>
    public interface IPythonProxy
    {
        IPythonWrapper Wrapper { get; set; }
    }
}
