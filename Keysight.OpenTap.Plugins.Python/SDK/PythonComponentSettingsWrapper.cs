using Python.Runtime;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Reflection;
using OpenTap;
using Keysight.Plugins.Python;

namespace Keysight.OpenTap.Plugins.Python
{

    [PythonWrapper.WrappedType(typeof(PythonComponentSettings))]
    public abstract class PythonComponentSettingsWrapper<T> : ComponentSettings<PythonComponentSettingsWrapper<T>>, IPythonWrapper
    {
        [Obfuscation(Exclude = true)]
        /// <summary> Overridden by the class build bu WrapperBuilder </summary>
        public abstract void load_instance();
        
        public PythonComponentSettingsWrapper()
        {
            load_instance();
        }

        static PythonComponentSettingsWrapper()
        {
            WrapperBuilder.LoadPython();
        }
        PyObject IPythonWrapper.PythonObject
        {
            get => PythonObject;
            set {
                this.PythonObject = value;
                IPythonProxy pyproxy = PythonObject.AsManagedObject(typeof(object)) as IPythonProxy;
                if (pyproxy != null)
                    pyproxy.Wrapper = this;

            }
        }

        [Browsable(false)]
        [XmlIgnore]
        PyObject PythonObject { get; set; }

        protected void load(string name, string moduleName)
        {
            PythonWrapperExtensions.Load(this, name, moduleName);
            PyThread.Invoke(() => PythonObject.GetAttr("__class__").SetAttr("__WrapperType__", GetType().ToPython()));
        }

        public override string ToString()
        {
            string str = "";
            PyThread.Invoke(() => str = this.PythonObject.ToString());
            return str;
        }
    }
}
