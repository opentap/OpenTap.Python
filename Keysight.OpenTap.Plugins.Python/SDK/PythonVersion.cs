//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
namespace Keysight.OpenTap.Plugins.Python
{
    class PythonVersion
    {
        public static PythonVersion Unsupported = new PythonVersion("unsupported");
        public static PythonVersion Py37 = new PythonVersion("py37");
        public static PythonVersion Py36 = new PythonVersion("py36");
        public static PythonVersion Py27 = new PythonVersion("py27");
        public string Name { get; }
        PythonVersion(string name)
        {
            Name = name;
        }
        public static PythonVersion Parse(string version)
        {
            string[] versionParts = version.Trim().Split('.', ' ');
            int major = int.Parse(versionParts[0]);
            int minor = int.Parse(versionParts[1]);

            PythonVersion pyversion = Unsupported;
            if (major == 2)
            {
                if (minor == 7)
                    pyversion = Py27;
            }
            else if (major == 3)
            {
                if (minor == 7)
                {
                    pyversion = Py37;
                }
                else
                {
                    pyversion = Py36;
                }
            }
            return pyversion;
        }
    }
}
