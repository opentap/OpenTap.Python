using System.Collections;
using System.Collections.Generic;
using Python.Runtime;

namespace OpenTap.Python;

class PythonEnumValueProperty : IAvailableValuesAnnotation
{
    readonly List<PyObject> list;
    public PythonEnumValueProperty(List<PyObject> pyList)
    {
        list = pyList;
    }
    public IEnumerable AvailableValues => list;
}