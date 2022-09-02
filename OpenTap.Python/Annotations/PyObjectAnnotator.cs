using System.Collections.Generic;
using Python.Runtime;

namespace OpenTap.Python;

// PyObject properties can be anything, but 
// we have modified PythonNet to add PythonTypeAttribute
// to properties which are python types, so we are able
// to resolve the type and make decisions on how to handle them.
// Currently only enum-types are supported.
public class PyObjectAnnotator : IAnnotator
{
    public void Annotate(AnnotationCollection annotations)
    {
        var mem = annotations.Get<IMemberAnnotation>();
        if (mem == null) return;
        if (mem.ReflectionInfo.DescendsTo(typeof(PyObject)) == false) return;
        var pythonTypeAttr = mem.Member.GetAttribute<PythonTypeAttribute>();
        if (pythonTypeAttr == null) return;
        using (Py.GIL())
        {
            PyObject mod = Py.Import(pythonTypeAttr.Module);
            var type = new PyType(mod.GetAttr(pythonTypeAttr.TypeName));
            if (type.HasAttr("_member_map_"))
            {
                // Handling enum: enums has _member_map_.
                var member_map = type.GetAttr("_member_map_");

                var dict = new PyDict(member_map);
                List<PyObject> list = new List<PyObject>();
                foreach (var pair in dict.Values())
                {
                    list.Add(type.GetAttr(pair.GetAttr("name")));
                }

                annotations.Add(new PythonEnumValueProperty(list));
            }

            // classes that implement describe() can provide a description string for a value.
            // see for example EnumUsage.
            if (type.HasAttr("describe"))
                annotations.Add(new PythonDescribeValueAnnotation(annotations));
        }

    }

    public double Priority => 0.0;
}