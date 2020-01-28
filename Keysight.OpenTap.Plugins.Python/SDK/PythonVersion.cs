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
            string[] versionParts = version.Split('.');
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
