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
