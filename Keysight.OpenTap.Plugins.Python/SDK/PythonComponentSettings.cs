//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
using OpenTap;
using Keysight.Plugins.Python;
using System.ComponentModel;
using System.Reflection;

namespace Keysight.OpenTap.Plugins.Python
{
    [Browsable(false)]
    [Obfuscation(Exclude = true)]
    public class PythonComponentSettings : ComponentSettings<PythonComponentSettings>, IPythonProxy
    {        
        [Browsable(false)]
        public IPythonWrapper Wrapper { get; set; }
    }
}
