using Python.Runtime;
using System;
using System.Reflection;
using System.Collections;

namespace Keysight.Plugins.Python
{
    /// <summary> Convert for various IList types. </summary>
    [Obfuscation(Exclude = true)]
    public class ListPythonConverter : IPythonConverter
    {
        public double Order => 1;

        public object FromPythonObject(PyObject value, Type type)
        {
            if (type.IsArray)
                return value.AsManagedObject(type);
            Type genericType = typeof(object);
            if (typeof(IList).IsAssignableFrom(type))
            {
                var lst = (IList) Activator.CreateInstance(type);
                var pylist = new PyList(value);
                foreach(PyObject item in pylist)
                {
                    var result = PythonConverter.FromPythonObject(item, genericType);
                    lst.Add(result);
                }
                return lst;
            }
            return UnableToConvert.Instance;
        }

        public PyObject ToPythonObject(object value)
        {
            if(value is IList)
                return value.ToPython();
            return null;
        }
    }

}
