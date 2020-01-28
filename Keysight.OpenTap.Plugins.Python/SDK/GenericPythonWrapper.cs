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
