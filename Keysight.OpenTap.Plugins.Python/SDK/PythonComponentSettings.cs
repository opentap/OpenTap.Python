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
