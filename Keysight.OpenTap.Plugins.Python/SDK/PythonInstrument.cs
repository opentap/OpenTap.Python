using System.ComponentModel;
using System.Reflection;
using OpenTap;
using Keysight.Plugins.Python;
using Python.Runtime;

namespace Keysight.OpenTap.Plugins.Python
{
    [Browsable(false)]
    [Obfuscation(Exclude = true)]
    public class PythonInstrument : Instrument, IPythonProxy
    {
        public override string ToString()
        {
            return Name ?? "NULL";
        }

        public IPythonWrapper Wrapper { get;set; }
        PyObject pyObj => Wrapper.PythonObject;
        
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
